using System;
using Nest;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Diagnostics;
using System.Collections;
using fileExplore.Dao;
namespace fileExplore
{
    public partial class Form1 : Form
    {

         
        //FileStream fileStream, fileStreamRoot, fileStreamRead;
/*        ConnectionSettings connectionSettings;
        static ElasticClient elasticClient;*/
        //List<string> pathFiles = new List<string>();
        List<fileInfo> myJson = new List<fileInfo>();
        // FileSystemWatcher
        FileSystemWatcher[] fileSystemWatchers;
        // fix duplicate change event
        static private Hashtable fileWriteTime = new Hashtable();
        //static string[] ignoreFolders = { "$RECYCLE.BIN", "\\elasticsearch\\", "\\kibana-elasticsearch\\" };

        bool isProcessRunning = false;
        ProgressDialog progressBar = new ProgressDialog();
        fileDao dao = new fileDao();
        public Form1()
        {
            InitializeComponent();
            PopulateTreeView();
            Task subThreadForGetAllFile = new Task(()=>getAllFileInDriver());
            subThreadForGetAllFile.Start(); // cho tiến trình tìm file chạy 1 thread khác 
            this.treeViewEx.NodeMouseClick += new TreeNodeMouseClickEventHandler(this.treeViewEx_NodeMouseClick);
        }


        private void Form1_Load(object sender, EventArgs e)
        {
          
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
        // tiến hành chạy để lấy file gửi lên server 
        public void getAllFileInDriver()
        {
         
            DirectoryInfo info = new DirectoryInfo(@"D:\");
            btnSearch.Invoke(new Action(()=> { btnSearch.Enabled = false; })); //đồng bộ để có thể thiết lập disble cho button 
            if (info.Exists)
            {
                Task task = new Task(() => RecursiveGetFile(info.GetDirectories()));
                task.Start();
                GetFileInFolder(info);
                task.Wait();
            }
            txtInfo.Invoke(new Action(() => txtInfo.Visible = false));
            btnSearch.Invoke(new Action(() => { btnSearch.Enabled = true; }));

            // dưới này là chạy tất cả file trên hệ thống, nếu muốn test có thể mở comment dưới này và đống đống code bên trên lại để thử, hiện tại thử trên 1 folder nào đó nhỏ cho nhanh
            /*   var ListDriverInfor = DriveInfo.GetDrives();
               for (int i = 0; i < ListDriverInfor.Length; i++)
               {
                   DirectoryInfo info = new DirectoryInfo(ListDriverInfor[i].Name);
                   progressBar.UpdateProgress(i, info.GetDirectories().Length);

                   if (info.Exists)
                   { 

                       Task task = new Task(() => RecursiveGetFile(info.GetDirectories()));
                       task.Start();// trong thread của tiến trình lấy all file tạo ra 1 thread khác để có thể xử lý bất đồng bộ
                       progressBar.ShowDialog();        //GetFileInFolder(info);// riêng cho thread này để ko ảnh hưởng đến thread main 
                       task.Wait(); // xử lý bất đồng bộ, buộc phải đợi thread hiện tại trong subThreadForGetAllFile chạy xong mới tạo mới thread khác 

                   }

                   if (progressBar.InvokeRequired)
                       progressBar.BeginInvoke(new Action(() => progressBar.Close()));

                   isProcessRunning = false;


        }*/
            var bulkIndexResponse = dao.Add(myJson);
            if (bulkIndexResponse)
            {
                MessageBox.Show("them thanh cong");
            }


        }

        public void RecursiveGetFile(DirectoryInfo[] subDirs)
        {
            DirectoryInfo[] subSubDirs;

            int MaxValue = subDirs.Length;
            //foreach (DirectoryInfo subDir in subDirs) // bắt đầu tìm kiếm trong từng ổ đĩa 
            for (int i = 0; i< subDirs.Length;i++)
            {

                GetFileInFolder(subDirs[i]);
                try
                {
                    subSubDirs = subDirs[i].GetDirectories();

                    if (subSubDirs.Length != 0)
                    {

                        RecursiveGetFile(subSubDirs);// cái này gọi là đệ quy sau khi tìm xong 1 folder sẽ tiếp tục tìm kiếm lại trong folder con của folder đó xem có còn file hay folder nào nữa ko 
                                                   // bắt sự kiện click thì mới gọi đệ quy 
                    }

                }
                catch (UnauthorizedAccessException)
                {

                }
                catch (IOException)
                {

                }
            }

        }

        private List<fileInfo> GetFileInFolder(DirectoryInfo subDir)
        {
            try
            {
                foreach (FileInfo file in subDir.GetFiles())
                {
                    // đọc và lấy ra những path có định dạng file là txt, doc, pdf
                    if (file.Extension == ".txt" || file.Extension == ".docx" || file.Extension == ".pdf")
                    {
                        string content = File.ReadAllText(file.FullName);
                        myJson.Add(new fileInfo()
                        {
                            name = file.Name,
                            path = file.FullName,
                            content = content // cái chỗ này sẽ đọc nội dung file ra nhưng chưa làm tới 
                        });

                        Debug.WriteLine(file.Name + "path = " + file.FullName);

                        
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

        // hàm của tree view
        private void PopulateTreeView()
        {
            // khởi tạo root gốc trong tree node 
            TreeNode rootNode;

            var ListDriverInfor = DriveInfo.GetDrives();// lây tất cả các ổ đĩa ( các ổ đia trong máy, ko bao gồm các file trong ổ đĩa)
            foreach (DriveInfo drive in ListDriverInfor) // bắt đầu tìm kiếm trong các ổ đĩa để lấy ra các folder và các file 
            {
                string path = drive.Name.ToString();
                DirectoryInfo info = new DirectoryInfo(path);

                if (info.Exists)
                {
                    GetFileInFolder(info);
                    rootNode = new TreeNode(info.Name);// nếu như có tồn tại thư mục con năm trong path ( path là đường dẫn vd khi bắt đầu với ổ c path sẽ là C) 
                    rootNode.ImageIndex = 2;// gắn image cho root node ( đây là image dành cho ổ đĩa c d e ... , các folder được gắn mặc định ) 
                    rootNode.Tag = info;
                    treeViewEx.Nodes.Add(rootNode);// thêm root node vào tree view để tạo ra nhánh của 1 ổ đĩa 
                }
            }
        }

        // hàm lấy tất cả các file và folder con( ko đệ quy )
        private void GetDirectories(DirectoryInfo[] subDirs, TreeNode nodeToAddTo)
        {
         
            TreeNode aNode;
            //DirectoryInfo[] subSubDirs;
         
            foreach (DirectoryInfo subDir in subDirs) // bắt đầu tìm kiếm trong từng ổ đĩa 
            {
                aNode = new TreeNode(subDir.Name, 0, 0);
                aNode.Tag = subDir;
                aNode.ImageKey = "folder";

                try
                {
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



        // thiết lập tree view mỗi khi bấm vào thì list view sẽ chuyển theo ứng vs tree view
        // khi bấm vào thì đồng thời gọi hàm GetDirectories để tìm kiếm các hàm con bên trong
        private void treeViewEx_NodeMouseClick(object sender, TreeNodeMouseClickEventArgs e)
        {
            TreeNode newSelected = e.Node;
            listView1.Items.Clear();
            ListViewItem.ListViewSubItem[] subItems;
            ListViewItem item = null;

            DirectoryInfo nodeDirInfo = (DirectoryInfo)newSelected.Tag;
            try
            {
                // bấm vào sẽ tìm kiếm các file và folder con 
                GetDirectories(nodeDirInfo.GetDirectories(), newSelected);

                // gắn file vào folder con vào list vỉew
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
            catch(UnauthorizedAccessException)
            {

            }
            catch (IOException)
            {

            }

        }



        private void treeViewEx_AfterSelect(object sender, TreeViewEventArgs e)
        {

        }

        private void listView1_SelectedIndexChanged(object sender, EventArgs e)
        {

        }


        // hiện tại sẽ viết tạm ở phần dưới này các chức năng như xóa sửa 

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
                    //var response = elasticClient.Index(myJson, i => i.Index("filedatasearch"));
                    /*var bulkIndexResponse = elasticClient.Bulk(b => b
                                                                 .Index("filedatasearch")
                                                                 .IndexMany(myJson)
             *//*                                                   );*//*
                    if (response.IsValid)
                    {
                        MessageBox.Show(e.FullPath + " Create success");
                    }
                    else
                    {
                        MessageBox.Show(e.FullPath + " Create not success");
                    }*/

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

        private void label1_Click(object sender, EventArgs e)
        {

        }

        // Viết chức năng tìm kiếm
        private void btnSearch_Click(object sender, EventArgs e)
        {

        }

        private void txtPath_TextChanged(object sender, EventArgs e)
        {

        }
        // END file system watcher
    }
}
