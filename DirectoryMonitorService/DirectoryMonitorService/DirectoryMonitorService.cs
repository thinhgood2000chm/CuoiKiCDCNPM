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
using iTextSharp.text.pdf;
using iTextSharp.text.pdf.parser;
using Path = System.IO.Path;

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

        FileSystemWatcher[] fileSystemWatchers;
        // folders you don't want to apply file system watcher
        static private string[] pathIgnore = { "\\$RECYCLE.BIN\\", "C:\\ProgramData\\", "elasticsearch", "kibana", "C:\\Users" };
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
                    if (!Directory.Exists(strDrive))
                    {
                        continue;
                    }
                    try
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
                    catch (ArgumentException)
                    {

                    }
                }
            }
        }// End OnStart

        protected override void OnStop()
        {
            //EventLog.WriteEntry("Directory Monitor Service Stopped!");
        }


        //--- file system watcher
        private static void OnChanged(object sender, FileSystemEventArgs e)
        {
            try
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
                            if (path.Contains(".pdf"))
                                f.content = GetTextFromPDF(path);
                            else
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
            catch (FileNotFoundException err)
            {
                LogError("OnChanged --- " + err.ToString()+"\n");
            }
        } // End OnChanged

        private static void OnCreated(object sender, FileSystemEventArgs e)
        {
            try
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
                            if (path.Contains(".pdf"))
                                fileUpload.content = GetTextFromPDF(path);
                            else
                                fileUpload.content = ReadFile(path);

                            dao.Add(fileUpload);
                            // End Create

                            fileWriteTime[path] = currentLastWriteTime;
                        }
                    }
                }
            }
            catch (FileNotFoundException err)
            {
                LogError("OnCreated --- " + err.ToString() + "\n");
            }
        }// End OnCreated


        private static void OnDeleted(object sender, FileSystemEventArgs e)
        {
            try
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
            catch (FileNotFoundException err)
            {
                LogError("OnDeleted --- " + err.ToString() + "\n");
            }
        }// End OnDeleted

        private static void OnRenamed(object sender, RenamedEventArgs e)
        {
            try
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

                            if (path.Contains(".pdf"))
                                fileUpload.content = GetTextFromPDF(path);
                            else
                                fileUpload.content = ReadFile(path);


                            var id = dao.GetId(e.OldFullPath);
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
            catch (FileNotFoundException err)
            {
                LogError("OnRenamed --- " + err.ToString() + "\n");
            }
        }// End OnRenamed

        // END file system watcher

        //--- Support function
        // ReadFile Text
        public static string ReadFile(string path)
        {
            try
            {
                string content = "";
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
            catch (FileNotFoundException err)
            {
                LogError("ReadFile text --- " + err.ToString() + "\n");
            }
            catch (UnauthorizedAccessException err)
            {
                LogError("ReadFile text --- " + err.ToString() + "\n");
            }
            catch (IOException err)
            {
                LogError("ReadFile text --- " + err.ToString() + "\n");
            }
            return "";
        }// End ReadFile

        private static string GetTextFromPDF(string path)
        {
            try
            {
                string content = "";
                Thread thread = new Thread(() =>
                {
                    PdfReader reader = new PdfReader(path);
                    for (int page = 1; page <= reader.NumberOfPages; page++)
                    {
                        content += PdfTextExtractor.GetTextFromPage(reader, page);
                    }
                    reader.Close();
                });
                thread.Start();
                thread.Join();

                return content;
            }
            catch (FileNotFoundException err)
            {
                LogError("GetTextFromPDF --- " + err.ToString() + "\n");
            }
            catch (UnauthorizedAccessException err)
            {
                LogError("GetTextFromPDF --- " + err.ToString() + "\n");
            }
            catch (IOException err)
            {
                LogError("GetTextFromPDF --- " + err.ToString() + "\n");
            }
            return "";
        }

        private static void LogError(string err)
        {
            var serviceLocation = Path.GetDirectoryName(System.Reflection.Assembly.GetEntryAssembly().Location);
            File.AppendAllText($"{serviceLocation}\\LogError.txt", err);
        }



    }// End Class DirectoryMonitorService
}
