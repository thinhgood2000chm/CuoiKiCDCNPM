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
        // constant
        string dataCheck = "dataforCheck11231asasdasdqweadaw";

        List<fileInfo> ListJson = new List<fileInfo>();

        // FileSystemWatcher
        FileSystemWatcher[] fileSystemWatchers;
        // fix duplicate change event
        static private Hashtable fileWriteTime = new Hashtable();
        //static string[] ignoreFolders = { "$RECYCLE.BIN", "\\elasticsearch\\", "\\kibana-elasticsearch\\" };
        bool isProcessRunning = false; // cái này là của process bar 
        ProgressDialog progressBar = new ProgressDialog();// cái này là của process bar 
        static fileDao dao = new fileDao();
        public Form1()
        {
            InitializeComponent();
            PopulateTreeView();
            bool checkExitsData = dao.CheckExits(dataCheck);
            if (!checkExitsData)
            {
                ListJson.Add(new fileInfo()
                {
                    name = dataCheck,
                    path = "",
                    content = "" // cái chỗ này sẽ đọc nội dung file ra nhưng chưa làm tới 
                });

                Task subThreadForGetAllFile = new Task(() => getAllFileInDriver());

                subThreadForGetAllFile.Start(); // cho tiến trình tìm file chạy 1 thread khác 
                //progressBar.ShowDialog();
            }
            else // choox này sẽ xoắ đi khi hoàn tất #################################################
            {
                txtInfo.Visible = false;
                MessageBox.Show(" data da ton taij");
            }

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
                        //watcher.Created += OnCreated;
                        //watcher.Deleted += OnDeleted;
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

            DirectoryInfo info = new DirectoryInfo(@"D:\CDCNPM\Test2");
            btnSearch.Invoke(new Action(() => { btnSearch.Enabled = false; })); //đồng bộ để có thể thiết lập disble cho button 
            if (info.Exists)
            {
                Task task = new Task(() => RecursiveGetFile(info.GetDirectories()));
                task.Start();
                GetFileInFolder(info);
                task.Wait();
            }



            // dưới này là chạy tất cả file trên hệ thống, nếu muốn test có thể mở comment dưới này và đống đống code bên trên lại để thử, hiện tại thử trên 1 folder nào đó nhỏ cho nhanh
            /*  var ListDriverInfor = DriveInfo.GetDrives();
              btnSearch.Invoke(new Action(() => { btnSearch.Enabled = false; }));
              for (int i = 0; i < ListDriverInfor.Length; i++)
              {
                  DirectoryInfo info = new DirectoryInfo(ListDriverInfor[i].Name);
                  //Debug.WriteLine(i+" "+ info.GetDirectories().Length);
                  //progressBar.UpdateProgress(i, info.GetDirectories().Length);

                  if (info.Exists)
                  {

                      Task task = new Task(() => RecursiveGetFile(info.GetDirectories()));
                      task.Start();// trong thread của tiến trình lấy all file tạo ra 1 thread khác để có thể xử lý bất đồng bộ

                      GetFileInFolder(info);// riêng cho thread này để ko ảnh hưởng đến thread main 
                      task.Wait(); // xử lý bất đồng bộ, buộc phải đợi thread hiện tại trong subThreadForGetAllFile chạy xong mới tạo mới thread khác 

                  }


              }*/
            /* if (progressBar.InvokeRequired)
                 progressBar.BeginInvoke(new Action(() => progressBar.Close()));

             isProcessRunning = false;*/
            txtInfo.Invoke(new Action(() => txtInfo.Visible = false));
            btnSearch.Invoke(new Action(() => { btnSearch.Enabled = true; }));

            var bulkIndexResponse = dao.AddList(ListJson);
            if (bulkIndexResponse)
            {
                txtInfo.Invoke(new Action(() => txtInfo.Visible = false));
                btnSearch.Invoke(new Action(() => { btnSearch.Enabled = true; }));
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
            // cứ khoảng 100 data thì gửi lên elasstic và xóa data trong list ( tránh tràn bộ nhớ nếu gửi lên 1 lần) 
            if (ListJson.Count > 100)
            {
                var bulkIndexResponse = dao.AddList(ListJson);
                ListJson.Clear();
            }
            try
            {
                foreach (FileInfo file in subDir.GetFiles())
                {
                  
                    // đọc và lấy ra những path có định dạng file là txt, doc, pdf
                    if (file.Extension == ".txt" || file.Extension == ".docx" || file.Extension == ".pdf")
                    {
                        string content = File.ReadAllText(file.FullName);
                        ListJson.Add(new fileInfo()
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
            return ListJson;

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
                                || e.FullPath.Contains("D:\\Server\\elasticsearch-7.16.0-windows-x86_64")
                                || e.FullPath.Contains("D:\\Server\\kibana-7.16.0-windows-x86_64")
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
                    //var id = "3AOh0n0BIYH7gHs0gySL";
                    var name = e.Name;
                    fileInfo fileUpload = new fileInfo();
                    fileUpload.name = name;
                    fileUpload.path = path;
                    var id = dao.GetId(e.FullPath);
                    fileUpload.content = File.ReadAllText(path);
                    //fileUpload.content = File.ReadAllText(path);
                    var a = dao.Update(fileUpload, id);                  
                    fileWriteTime[path] = currentLastWriteTime;
                }
            }
            
        }

        /* private static void OnCreated(object sender, FileSystemEventArgs e)
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
                     fileInfo fileUpload = new fileInfo();
                     fileUpload.name = name;
                     fileUpload.path = path;
                     fileUpload.content = File.ReadAllText(path);
                     dao.Add(fileUpload);

                     fileWriteTime[path] = currentLastWriteTime;
                 }
             }

         }*/

        //private static void OnDeleted(object sender, FileSystemEventArgs e)
        //{
        //    var serviceLocation = Path.GetDirectoryName(System.Reflection.Assembly.GetEntryAssembly().Location);
        //    bool ignoreFolder = e.FullPath.Contains(serviceLocation) 
        //                        || e.FullPath.Contains("$RECYCLE.BIN")
        //                        || e.FullPath.Contains("\\elasticsearch\\")
        //                        || e.FullPath.Contains("\\kibana-elasticsearch\\")
        //                        || e.FullPath.Contains("G:\\elasticsearch-7.15.1");
        //    if (!ignoreFolder)
        //    {
        //        var path = e.FullPath;
        //        string currentLastWriteTime = File.GetLastWriteTime(e.FullPath).ToString();
        //        if (!fileWriteTime.ContainsKey(path) ||
        //            fileWriteTime[path].ToString() != currentLastWriteTime
        //            )
        //        {
        //            // xóa trên elastic ở đây
        //            MessageBox.Show(e.FullPath + " Delete");

        //            //---
        //            fileWriteTime[path] = currentLastWriteTime;
        //        }
        //    }
        //}

        private static void OnRenamed(object sender, RenamedEventArgs e)
        {
            var msg = $"Renamed: Old: {e.OldFullPath} New: {e.FullPath} {System.Environment.NewLine}";


            var serviceLocation = Path.GetDirectoryName(System.Reflection.Assembly.GetEntryAssembly().Location);
            bool ignoreFolder = e.FullPath.Contains(serviceLocation)
                                || e.FullPath.Contains("$RECYCLE.BIN")
                                || e.FullPath.Contains("\\Admin\\AppData\\")
                                || e.FullPath.Contains("D:\\Server\\elasticsearch-7.16.0-windows-x86_64")
                                || e.FullPath.Contains("D:\\Server\\kibana-7.16.0-windows-x86_64");
            if (!ignoreFolder)
            {
                var path = e.FullPath;
                string currentLastWriteTime = File.GetLastWriteTime(e.FullPath).ToString();
                if (!fileWriteTime.ContainsKey(path) ||
                    fileWriteTime[path].ToString() != currentLastWriteTime
                    )
                {
                    // đổi tên e.OldFullPath thành e.FullPath trên elastic ở đây
                    var name = e.Name;
                    fileInfo fileUpload = new fileInfo();
                    fileUpload.name = name;
                    fileUpload.path = path;
                    var id = dao.GetId(e.OldFullPath);
                    //System.Diagnostics.Process.Start(path);
                    fileUpload.content = File.ReadAllText(path);
                    var a = dao.Update(fileUpload, id);
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

        private void button1_Click(object sender, EventArgs e)
        {
            //var id = "2QOh0n0BIYH7gHs0gySL";
            ////var name = e.Name;
            //fileInfo fileUpload = new fileInfo();
            //fileUpload.name = "123";
            //fileUpload.path = "test";
            ////fileUpload.content = File.ReadAllText(path);
            //var a = dao.Update(fileUpload,id);
            //if (a)
            //{
            //    Debug.WriteLine("123");
            //}
            //else
            //{
            //    Debug.WriteLine("loi");
            //}
        }

        // END file system watcher
    }
}
