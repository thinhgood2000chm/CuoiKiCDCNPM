using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using DirectoryMonitorService.DAO;
using Nest;

namespace DirectoryMonitorService
{
    public partial class DirectoryMonitorService : ServiceBase
    {
        //---- Install window service ----------------------------------------------------------|
        // Start project to create debug folder                                                 |
        // run as administrator: Developer Command Prompt for VS 2019                           |
        // cd to folder debug:   ...\DirectoryMonitorService\DirectoryMonitorService\bin\Debug  |
        // install service:      installutil.exe -i DirectoryMonitorService.exe                 |
        // window + R:           services.msc   ->    start Directory Monitor Service           |
        //                                                                                      |
        //                                                                                      |
        // uninstall service: stop service -> installutil.exe -u DirectoryMonitorService.exe    |
        //--------------------------------------------------------------------------------------|

        // TODO: singleton for write
        // *docx problem*

        FileSystemWatcher[] fileSystemWatchers;
        // folders you don't want to apply file system watcher
        static private string[] pathIgnore = { "\\$RECYCLE.BIN\\", "C:\\ProgramData\\", "\\iTProgram\\", "C:\\Users" };
        // fix duplicate change event
        static private Hashtable fileWriteTime = new Hashtable();
        // file dao
        static fileDao dao = new fileDao();

        public DirectoryMonitorService()
        {
            InitializeComponent();
        }
        protected override void OnStart(string[] args)
        {
            // get all drive in computer
            string[] drives = Environment.GetLogicalDrives();

            // filter file types
            string[] extensions = { "*.txt", "*.doc", "*.docx", "*.pdf" };

            // init fileSystemWatcher for each drive
            fileSystemWatchers = new FileSystemWatcher[drives.Length*extensions.Length];
            int i = 0;
            foreach (string strDrive in drives)
            {
                // will be a fileSystemWatcher of each file type. B/c fileSystemWatcher don't support Filters in .Net Framework
                foreach (string etx in extensions)
                {
                    FileSystemWatcher watcher = new FileSystemWatcher(strDrive)
                    {
                        Filter = etx,
                        EnableRaisingEvents = true,
                        IncludeSubdirectories = true
                    };
                    // Will blogging when there is a change
                    watcher.NotifyFilter = NotifyFilters.Attributes
                                     | NotifyFilters.CreationTime
                                     | NotifyFilters.DirectoryName
                                     | NotifyFilters.FileName
                                     | NotifyFilters.LastWrite
                                     | NotifyFilters.Security
                                     | NotifyFilters.Size;

                    watcher.Changed += OnChanged;
                    watcher.Created += OnCreated;
                    watcher.Deleted += OnDeleted;
                    watcher.Renamed += OnRenamed;

                    fileSystemWatchers[i] = watcher;
                    i++;
                }
            }
        }

        protected override void OnStop()
        {
            //EventLog.WriteEntry("Directory Monitor Service Stopped!");
        }


        //--- file system watcher
        private static void OnChanged(object sender, FileSystemEventArgs e)
        {
            // get service location
            bool ignoreFolder = pathIgnore.Any(e.FullPath.Contains);
            if (!ignoreFolder)
            {
                // fix duplicate change event
                string path = e.FullPath.ToString();
                if (!path.Contains("elastic"))
                {
                    string currentLastWriteTime = File.GetLastWriteTime(e.FullPath).ToString();
                    if (!fileWriteTime.ContainsKey(path) ||
                        fileWriteTime[path].ToString() != currentLastWriteTime
                        )
                    {
                        //-- Change on elastic
                        string[] pathIncludeName = e.Name.Split('\\'); // name này bao gồm cả folder trước nó nên cần tách ra lấy name 
                        string name = pathIncludeName[pathIncludeName.Length - 1];

                        // read file
                        fileInfo f = new fileInfo();
                        f.name = name;
                        f.path = path;
                        f.content = ReadFile(path);

                        // update elastic
                        var id = dao.GetId(e.FullPath);
                        if (id != null)
                        {
                            dao.Update(f, id);
                        }
                        // End Change

                        fileWriteTime[path] = currentLastWriteTime;
                    }
                }
            }
        }

        private static void OnCreated(object sender, FileSystemEventArgs e)
        {
            bool ignoreFolder = pathIgnore.Any(e.FullPath.Contains);
            if (!ignoreFolder)
            {
                string path = e.FullPath.ToString();
                if (!path.Contains("elastic"))
                {
                    string currentLastWriteTime = File.GetLastWriteTime(e.FullPath).ToString();
                    if (!fileWriteTime.ContainsKey(path) ||
                        fileWriteTime[path].ToString() != currentLastWriteTime
                        )
                    {
                        //-- Create on elastic
                        string[] pathIncludeName = e.Name.Split('\\');// name này bao gồm cả folder trước nó nên cần tách ra lấy name 
                        string name = pathIncludeName[pathIncludeName.Length - 1];

                        fileInfo fileUpload = new fileInfo();
                        fileUpload.name = name;
                        fileUpload.path = path;
                        fileUpload.content = ReadFile(path);

                        dao.Add(fileUpload);
                        // End Create

                        fileWriteTime[path] = currentLastWriteTime;
                    }
                }
            }
        }


        private static void OnDeleted(object sender, FileSystemEventArgs e)
        {
            bool ignoreFolder = pathIgnore.Any(e.FullPath.Contains);
            if (!ignoreFolder)
            {
                string path = e.FullPath.ToString();
                if (!path.Contains("elastic"))
                {
                    string currentLastWriteTime = File.GetLastWriteTime(e.FullPath).ToString();
                    if (!fileWriteTime.ContainsKey(path) ||
                        fileWriteTime[path].ToString() != currentLastWriteTime
                        )
                    {
                        //-- Delete on elastic
                        var id = dao.GetId(path);
                        if (id != null)
                        {
                            dao.Deleted(id);
                        }
                        // End Delete

                        fileWriteTime[path] = currentLastWriteTime;
                    }
                }
            }
        }

        private static void OnRenamed(object sender, RenamedEventArgs e)
        {
            bool ignoreFolder = pathIgnore.Any(e.FullPath.Contains);
            if (!ignoreFolder)
            {
                string path = e.FullPath.ToString();
                if (!path.Contains("elastic"))
                {
                    string currentLastWriteTime = File.GetLastWriteTime(e.FullPath).ToString();
                    if (!fileWriteTime.ContainsKey(path) ||
                        fileWriteTime[path].ToString() != currentLastWriteTime
                        )
                    {
                        //-- Rename on elastic
                        string[] pathIncludeName = e.Name.Split('\\');// name này bao gồm cả folder trước nó nên cần tách ra lấy name 
                        string name = pathIncludeName[pathIncludeName.Length - 1];// sau khi split sẽ ra được mảng chứa name( name luôn nằm ở vị trí cuối cùng )

                        fileInfo fileUpload = new fileInfo();
                        fileUpload.name = name;
                        fileUpload.path = path;
                        var id = dao.GetId(e.OldFullPath);
                        fileUpload.content = ReadFile(path);

                        if (id != null)
                        {
                            dao.Update(fileUpload, id);
                        }
                        // End Rename

                        fileWriteTime[path] = currentLastWriteTime;
                    }
                }
            }
        }
        // END file system watcher

        // Support function
        public static string ReadFile(string path)
        {
            string content = "";
            try
            {
                if (!path.Contains(".tmp"))
                {// bỏ qua file tmp khi word đang được thay đổi
                    Thread thread = new Thread(() =>
                    {
                        content = File.ReadAllText(path);
                    });
                    thread.Start();
                    thread.Join();
                    //content = 
                    return content;
                }

            }
            catch (FileNotFoundException)
            {
                File.AppendAllText($"C:\\log.txt", "\nFileNotFoundException\n" + path);
            }
            catch (UnauthorizedAccessException)
            {
                File.AppendAllText($"C:\\log.txt", "\nUnauthorizedAccessException\n" + path);
            }
            catch (IOException)
            {
                File.AppendAllText($"C:\\log.txt", "\nIOException\n" + path);
            }
            return "";
        }
    }
}
