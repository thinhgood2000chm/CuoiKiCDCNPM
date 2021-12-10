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
using System.Threading.Tasks;

namespace DirectoryMonitorService
{
    public partial class DirectoryMonitorService : ServiceBase
    {
        //---- Install window service ----------------------------------------------------------|
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
        static private string[] pathIgnore = { "\\$RECYCLE.BIN\\", "\\ProgramData\\" , "\\iTProgram\\" };
        // fix duplicate change event
        static private Hashtable fileWriteTime = new Hashtable();

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
            // write to file
            var msg = $"{e.ChangeType} --- {e.FullPath} {System.Environment.NewLine}";
            // get service location
            var serviceLocation = Path.GetDirectoryName(System.Reflection.Assembly.GetEntryAssembly().Location);
            // ignore monitor diretory write log AND recycle bin
            bool ignoreFolder = e.FullPath.Contains(serviceLocation)
                                || pathIgnore.Any(e.FullPath.Contains);
            if (!ignoreFolder)
            {
                // fix duplicate change event
                string path = e.FullPath.ToString();
                string currentLastWriteTime = File.GetLastWriteTime(e.FullPath).ToString();
                if (!fileWriteTime.ContainsKey(path) ||
                    fileWriteTime[path].ToString() != currentLastWriteTime
                    )
                {
                    //-- write log
                    File.AppendAllText($"{serviceLocation}\\log.txt", msg);

                    fileWriteTime[path] = currentLastWriteTime;
                }
            }
        }

        private static void OnCreated(object sender, FileSystemEventArgs e)
        {
            var msg = $"Created --- {e.FullPath} {System.Environment.NewLine}";

            var serviceLocation = Path.GetDirectoryName(System.Reflection.Assembly.GetEntryAssembly().Location);
            bool ignoreFolder = e.FullPath.Contains(serviceLocation)
                                || pathIgnore.Any(e.FullPath.Contains);
            if (!ignoreFolder)
            {
                string path = e.FullPath.ToString();
                string currentLastWriteTime = File.GetLastWriteTime(e.FullPath).ToString();
                if (!fileWriteTime.ContainsKey(path) ||
                    fileWriteTime[path].ToString() != currentLastWriteTime
                    )
                {
                    File.AppendAllText($"{serviceLocation}\\log.txt", msg);
                    fileWriteTime[path] = currentLastWriteTime;
                }
            }
        }

        private static void OnDeleted(object sender, FileSystemEventArgs e)
        {
            var msg = $"Deleted --- {e.FullPath} {System.Environment.NewLine}";


            var serviceLocation = Path.GetDirectoryName(System.Reflection.Assembly.GetEntryAssembly().Location);
            bool ignoreFolder = e.FullPath.Contains(serviceLocation)
                                || pathIgnore.Any(e.FullPath.Contains);
            if (!ignoreFolder)
            {
                string path = e.FullPath.ToString();
                string currentLastWriteTime = File.GetLastWriteTime(e.FullPath).ToString();
                if (!fileWriteTime.ContainsKey(path) ||
                    fileWriteTime[path].ToString() != currentLastWriteTime
                    )
                {
                    File.AppendAllText($"{serviceLocation}\\log.txt", msg);
                    fileWriteTime[path] = currentLastWriteTime;
                }
            }
        }

        private static void OnRenamed(object sender, RenamedEventArgs e)
        {
            var msg = $"Renamed --- Old: {e.OldFullPath} New: {e.FullPath} {System.Environment.NewLine}";


            var serviceLocation = Path.GetDirectoryName(System.Reflection.Assembly.GetEntryAssembly().Location);
            bool ignoreFolder = e.FullPath.Contains(serviceLocation)
                                || pathIgnore.Any(e.FullPath.Contains);
            if (!ignoreFolder)
            {
                string path = e.FullPath.ToString();
                string currentLastWriteTime = File.GetLastWriteTime(e.FullPath).ToString();
                if (!fileWriteTime.ContainsKey(path) ||
                    fileWriteTime[path].ToString() != currentLastWriteTime
                    )
                {
                    File.AppendAllText($"{serviceLocation}\\log.txt", msg);
                    fileWriteTime[path] = currentLastWriteTime;
                }
            }
        }
        // END file system watcher
    }
}
