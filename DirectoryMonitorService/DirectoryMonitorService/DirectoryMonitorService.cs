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
using Nest;
using iTextSharp.text.pdf;
using iTextSharp.text.pdf.parser;
using Path = System.IO.Path;
using System.Runtime.InteropServices;

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
        static private string[] pathIgnore = { "\\$RECYCLE.BIN\\", "C:\\ProgramData\\", "elasticsearch", "kibana", "C:\\Users", "\\data_key\\" };
        // fix duplicate change event
        static private Hashtable fileWriteTime = new Hashtable();
        
        static string changeLogPath = "";

        public DirectoryMonitorService()
        {
            InitializeComponent();
        }
        protected override void OnStart(string[] args)
        {
            // get changeLogPath
            string systempath = Environment.GetEnvironmentVariable("SystemRoot");
            string[] pathIncludeName = systempath.Split('\\');// name này bao gồm cả folder trước nó nên cần tách ra lấy name 
            string name = pathIncludeName[0];
            changeLogPath = name + "\\data_key\\ChangeLog.txt";

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
                    catch (ArgumentException err)
                    {
                        LogError("FileSystemWatcher --- " + err.ToString() + "\n");
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
                            //-- Log Change 
                            var msg = $"OnChanged {e.FullPath} {System.Environment.NewLine}";
                            LogWatcher(msg);

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
                            //-- Log Create
                            var msg = $"OnCreated {e.FullPath} {System.Environment.NewLine}";
                            LogWatcher(msg);

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
                            //-- Log Delete 
                            var msg = $"OnDeleted {e.FullPath} {System.Environment.NewLine}";
                            LogWatcher(msg);

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
                            //-- Log Renamed
                            var msg = $"OnRenamed {e.OldFullPath} {e.FullPath} {System.Environment.NewLine}";
                            LogWatcher(msg);
                            // End Renamed

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
        //=== END file system watcher

        //--- Support function

        private static void LogError(string err)
        {
            var serviceLocation = Path.GetDirectoryName(System.Reflection.Assembly.GetEntryAssembly().Location);
            File.AppendAllText($"{serviceLocation}\\LogError.txt", err);
        }

        private static void LogWatcher(string msg)
        {
            File.AppendAllText(changeLogPath, msg);
        }
    }// End Class DirectoryMonitorService
}
