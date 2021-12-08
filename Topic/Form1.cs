using System;
using Nest;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Diagnostics;
using System.Threading;
using System.Collections;

namespace fileExplore
{
    public partial class Form1 : Form
    {
        
        //FileStream fileStream, fileStreamRoot, fileStreamRead;
        ConnectionSettings connectionSettings;
        static ElasticClient elasticClient;
        //List<string> pathFiles = new List<string>();
        List<file> myJson = new List<file>();
        // FileSystemWatcher
        FileSystemWatcher[] fileSystemWatchers;
        // fix duplicate change event
        static private Hashtable fileWriteTime = new Hashtable();
        //static string[] ignoreFolders = { "$RECYCLE.BIN", "\\elasticsearch\\", "\\kibana-elasticsearch\\" };


        public Form1()
        {
            InitializeComponent();
            PopulateTreeView();
            this.treeViewEx.NodeMouseClick += new TreeNodeMouseClickEventHandler(this.treeViewEx_NodeMouseClick);
        }


        private void Form1_Load(object sender, EventArgs e)
        {
            connectionSettings = new ConnectionSettings(new Uri("http://localhost:9200/")); //local PC           
            elasticClient = new ElasticClient(connectionSettings);

            // ghi những dữ liệu mà trên elastic chưa có lên server  
            var bulkIndexResponse = elasticClient.Bulk(b => b
             .Index("filedatasearch")
             .IndexMany(myJson)
               );

            //############################# chỗ này khi hoàn thành phảixóa đi ########################################
            if (bulkIndexResponse.IsValid)
            {
                MessageBox.Show("them thanh cong");
            }
            //############################# chỗ này khi hoàn thành phải xóa đi   ########################################

            //----------File system watcher: cập nhật thông tin khi có thay đổi file
            // get all drive in computer
            string[] drives = Environment.GetLogicalDrives();

            // filter file types
            string[] extensions = { "*.txt", "*.doc", "*.docx", "*.pdf" };

            // init fileSystemWatcher for each drive
            fileSystemWatchers = new FileSystemWatcher[drives.Length * extensions.Length];

            int i = 0;
            foreach (string strDrive in drives)
            {
                if (!Directory.Exists(strDrive))
                {
                    Debug.WriteLine("da vao");
                    continue;
                }
                   

                Debug.WriteLine(strDrive);
                // will be a fileSystemWatcher of each file type. B/c fileSystemWatcher don't support Filters in .Net Framework
                try
                {
                    foreach (string etx in extensions)
                    {
                        FileSystemWatcher watcher = new FileSystemWatcher(strDrive)
                        {
                            Filter = etx,
                            EnableRaisingEvents = true,
                            IncludeSubdirectories = true
                        };
                        // Will update when there is a change
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
                catch (ArgumentException)
                {

                }
              
            }
            //END File system watcher-------
        }

        private void PopulateTreeView()
        {
            // mở file txt và lưu giá trị vào 1 list, list này dùng để so sánh xem có file nào mới được tạo thêm trên máy khi mà 
            // chương trình đang tắt hay ko, nếu có thì thêm vào file txt, và gửi lên server elastic, nếu ko thì bỏ qua ko gửi gì lên hết 
            //################################# url chỗ này có thể sẽ sửa lại sau này thanh đường dẫn tương đối#####################
            /*StreamReader streamReader = new StreamReader(fileStreamRead);
            string pathFile = streamReader.ReadLine();
            while (pathFile != null)
            {
                if (pathFile.Contains("Created")){
                    Debug.WriteLine(pathFile);
                    pathFiles.Add(pathFile); // lưu giá trị đọc từ file vào list
                }
                
                pathFile = streamReader.ReadLine();
            }
            streamReader.Close();

            fileStreamRead.Close();
            Debug.WriteLine(pathFile);
            foreach(var e in pathFiles)
            {
                Debug.WriteLine(e);
            }*/
            // khởi tạo root gốc trong tree node 
            TreeNode rootNode;

            //###################################### ko được xóa những cái comment này ######################################
            /* var ListDriverInfor = DriveInfo.GetDrives();// lây tất cả các ổ đĩa ( các ổ đia trong máy, ko bao gồm các file trong ổ đĩa)
             foreach (DriveInfo drive in ListDriverInfor) // bắt đầu tìm kiếm trong các ổ đĩa để lấy ra các folder và các file 
             {
                 string path = drive.Name.ToString();
                 DirectoryInfo info = new DirectoryInfo(path);

                 if (info.Exists)
                 {
                     //GetFileInFolder(info);
                     rootNode = new TreeNode(info.Name);// nếu như có tồn tại thư mục con năm trong path ( path là đường dẫn vd khi bắt đầu với ổ c path sẽ là C) 
                     rootNode.ImageIndex = 2;// gắn image cho root node ( đây là image dành cho ổ đĩa c d e ... , các folder được gắn mặc định ) 
                     rootNode.Tag = info;
                     GetDirectories(info.GetDirectories(), rootNode);// tìm kiếm các folder bên trong ổ đĩa

                     treeViewEx.Nodes.Add(rootNode);// thêm root node vào tree view để tạo ra nhánh của 1 ổ đĩa 
                 }
             }
 */

            DirectoryInfo info = new DirectoryInfo(@"G:\");
            if (info.Exists)
            {

                GetFileInFolder(info);

                rootNode = new TreeNode(info.Name);
                rootNode.Tag = info;
                GetDirectories(info.GetDirectories(), rootNode);
                rootNode.ImageIndex = 2;
                treeViewEx.Nodes.Add(rootNode);
            }
        }

        private void GetDirectories(DirectoryInfo[] subDirs, TreeNode nodeToAddTo)
        {
         
            TreeNode aNode;
            DirectoryInfo[] subSubDirs;
         
            foreach (DirectoryInfo subDir in subDirs) // bắt đầu tìm kiếm trong từng ổ đĩa 
            {
    
                aNode = new TreeNode(subDir.Name, 0, 0);
                aNode.Tag = subDir;
                aNode.ImageKey = "folder";

                GetFileInFolder(subDir);

                try
                {
                    subSubDirs = subDir.GetDirectories();

                    if (subSubDirs.Length != 0)
                    {
                        GetDirectories(subSubDirs, aNode);// cái này gọi là đệ quy sau khi tìm xong 1 folder sẽ tiếp tục tìm kiếm lại trong folder con của folder đó xem có còn file hay folder nào nữa ko 
                    }
                 
                    nodeToAddTo.Nodes.Add(aNode);// add folder vào ổ đĩa 
              
                }
                catch (UnauthorizedAccessException)
                {

                }
                catch (IOException)
                {

                }
            }
        }

        private List<file> GetFileInFolder(DirectoryInfo subDir)
        {
            try
            {
                foreach (FileInfo file in subDir.GetFiles())
                {
                    // đọc và lấy ra những path có định dạng file là txt, doc, pdf
                    if (file.Extension == ".txt" || file.Extension == ".docx" || file.Extension == ".pdf")
                    {

                        {
                            myJson.Add(new file()
                            {
                                name = file.Name,
                                path = file.FullName,
                                content = "dư lieu tu c# bulk 123" // cái chỗ này sẽ đọc nội dung file ra nhưng chưa làm tới 
                            });

                        }
                    }

                }
            }
            catch (UnauthorizedAccessException)
            {

            }
            catch (IOException)
            {

            }
            return myJson;
        
    }

        // thiết lập tree view mỗi khi bấm vào thì list view sẽ chuyển theo ứng vs tree view
        private void treeViewEx_NodeMouseClick(object sender, TreeNodeMouseClickEventArgs e)
        {
            TreeNode newSelected = e.Node;
            listView1.Items.Clear();
            DirectoryInfo nodeDirInfo = (DirectoryInfo)newSelected.Tag;
            ListViewItem.ListViewSubItem[] subItems;
            ListViewItem item = null;

            foreach (DirectoryInfo dir in nodeDirInfo.GetDirectories())
            {
                item = new ListViewItem(dir.Name, 0);
                subItems = new ListViewItem.ListViewSubItem[]
                    {new ListViewItem.ListViewSubItem(item, "Directory"),
                        new ListViewItem.ListViewSubItem(item,
                            dir.LastAccessTime.ToShortDateString())};
                item.SubItems.AddRange(subItems);
                listView1.Items.Add(item);
            }
            foreach (FileInfo file in nodeDirInfo.GetFiles())
            {
                item = new ListViewItem(file.Name, 1);
                subItems = new ListViewItem.ListViewSubItem[]
                    { new ListViewItem.ListViewSubItem(item, "File"),
                        new ListViewItem.ListViewSubItem(item,
                        file.LastAccessTime.ToShortDateString())};

                item.SubItems.AddRange(subItems);
                listView1.Items.Add(item);
            }

            listView1.AutoResizeColumns(ColumnHeaderAutoResizeStyle.HeaderSize);
        }



        private void treeViewEx_AfterSelect(object sender, TreeViewEventArgs e)
        {

        }

        private void listView1_SelectedIndexChanged(object sender, EventArgs e)
        {

        }


        //--- file system watcher
        private static void OnChanged(object sender, FileSystemEventArgs e)
        {
            // không cập nhật trong những ignoreFolder ------ sẽ cập nhật cách kiểm tra "sạch hơn" sau
            var serviceLocation = Path.GetDirectoryName(System.Reflection.Assembly.GetEntryAssembly().Location);
            bool ignoreFolder = e.FullPath.Contains(serviceLocation)
                                || e.FullPath.Contains("$RECYCLE.BIN")
                                || e.FullPath.Contains("\\elasticsearch\\")
                                || e.FullPath.Contains("\\kibana-elasticsearch\\")
                                || e.FullPath.Contains("\\ASUS\\ASUS");
            Debug.WriteLine(serviceLocation);
            Debug.WriteLine(ignoreFolder);
            if (!ignoreFolder)
            {
                // sữa lỗi ghi 2 lần một thông tin
                var path = e.FullPath;
                string currentLastWriteTime = File.GetLastWriteTime(e.FullPath).ToString();
                if (!fileWriteTime.ContainsKey(path) ||
                    fileWriteTime[path].ToString() != currentLastWriteTime
                    )
                {
                    // sửa nội dung file trên elastic ở đây


                    //---                    
                    fileWriteTime[path] = currentLastWriteTime;
                }
            }
        }

        private static void OnCreated(object sender, FileSystemEventArgs e)
        {
            var serviceLocation = Path.GetDirectoryName(System.Reflection.Assembly.GetEntryAssembly().Location);
            bool ignoreFolder = e.FullPath.Contains(serviceLocation)
                                || e.FullPath.Contains("$RECYCLE.BIN")
                                || e.FullPath.Contains("\\elasticsearch\\")
                                || e.FullPath.Contains("\\kibana-elasticsearch\\")
                                || e.FullPath.Contains("\\ASUS\\ASUS")
                                || e.FullPath.Contains("G:\\elasticsearch-7.15.1");
            if (!ignoreFolder)
            {
                var path = e.FullPath;
                string currentLastWriteTime = File.GetLastWriteTime(e.FullPath).ToString();
                if (!fileWriteTime.ContainsKey(path) ||
                    fileWriteTime[path].ToString() != currentLastWriteTime
                    )
                {
                    // ghi lên elastic ở đây
                    var name = e.Name;

                    var myJson = new
                    {
                        name = name,
                        path = path,
                        content = "dư lieu tu c# bulk"
                    };
                    var response = elasticClient.Index(myJson, i => i.Index("filedatasearch"));
                    /*var bulkIndexResponse = elasticClient.Bulk(b => b
                                                                 .Index("filedatasearch")
                                                                 .IndexMany(myJson)
                                                                );*/
                    if (response.IsValid)
                    {
                        MessageBox.Show(e.FullPath + " Create success");
                    }
                    else
                    {
                        MessageBox.Show(e.FullPath + " Create not success");
                    }

                    //--
                    fileWriteTime[path] = currentLastWriteTime;
                }
            }

        }

        private static void OnDeleted(object sender, FileSystemEventArgs e)
        {
            var serviceLocation = Path.GetDirectoryName(System.Reflection.Assembly.GetEntryAssembly().Location);
            bool ignoreFolder = e.FullPath.Contains(serviceLocation) 
                                || e.FullPath.Contains("$RECYCLE.BIN")
                                || e.FullPath.Contains("\\elasticsearch\\")
                                || e.FullPath.Contains("\\kibana-elasticsearch\\")
                                || e.FullPath.Contains("G:\\elasticsearch-7.15.1");
            if (!ignoreFolder)
            {
                var path = e.FullPath;
                string currentLastWriteTime = File.GetLastWriteTime(e.FullPath).ToString();
                if (!fileWriteTime.ContainsKey(path) ||
                    fileWriteTime[path].ToString() != currentLastWriteTime
                    )
                {
                    // xóa trên elastic ở đây
                    MessageBox.Show(e.FullPath + " Delete");

                    //---
                    fileWriteTime[path] = currentLastWriteTime;
                }
            }
        }

        private static void OnRenamed(object sender, RenamedEventArgs e)
        {
            var msg = $"Renamed: Old: {e.OldFullPath} New: {e.FullPath} {System.Environment.NewLine}";


            var serviceLocation = Path.GetDirectoryName(System.Reflection.Assembly.GetEntryAssembly().Location);
            bool ignoreFolder = e.FullPath.Contains(serviceLocation)
                                || e.FullPath.Contains("$RECYCLE.BIN")
                                || e.FullPath.Contains("\\Admin\\AppData\\")
                                || e.FullPath.Contains("\\elasticsearch\\")
                                || e.FullPath.Contains("\\kibana-elasticsearch\\");
            if (!ignoreFolder)
            {
                var path = e.FullPath;
                string currentLastWriteTime = File.GetLastWriteTime(e.FullPath).ToString();
                if (!fileWriteTime.ContainsKey(path) ||
                    fileWriteTime[path].ToString() != currentLastWriteTime
                    )
                {
                    // đổi tên e.OldFullPath thành e.FullPath trên elastic ở đây

                    MessageBox.Show(e.FullPath + " Rename");


                    //---
                    fileWriteTime[path] = currentLastWriteTime;
                }
            }
        }
        // END file system watcher
    }
}
