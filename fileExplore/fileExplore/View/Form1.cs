using System;
using Nest;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Diagnostics;
using System.Collections;
using fileExplore.Dao;
using Microsoft.VisualBasic;
using iTextSharp.text.pdf;
using iTextSharp.text.pdf.parser;
using System.Text;
using System.Runtime.InteropServices;
using System.Threading;
using System.Linq;
using fileExplore.FileInfoBuilder;

namespace fileExplore
{

    // công việc cần làm ngày mai : đổi qua bên chạy ngầm check, trong mỗi chạy ngầm check bỏ file elastic, thêm lấy index , kiểm tra xem đã try catch đọc file pdf và word chưa
   // sửa nội dung file txt trong chạy ngầm ( đạt ) 
    public partial class Form1 : Form
    {
        // constant
        string dataCheck = "dataforCheck11231asasdasdqweadaw";
        static string index = "";
        static string dataLog = "";
        static string nameDriver = "";
        List<fileInfo> ListJson = new List<fileInfo>();

        // FileSystemWatcher
        FileSystemWatcher[] fileSystemWatchers;
        // fix duplicate change event
        static private Hashtable fileWriteTime = new Hashtable();
        static private string[] pathIgnore = { "\\$RECYCLE.BIN\\", "C:\\ProgramData\\", "\\iTProgram\\", "C:\\Users" };

        static fileDao dao = new fileDao();
        //list kiểm tra dã bấm vào cây hay chưa
        List<string> listPath = new List<string>();
        //biến dùng để luuw lại thông tin của parent root trước đó
        DirectoryInfo parentDirInfo;
        public Form1()
        {
            InitializeComponent();
            // lấy ra uuid của từng máy nếu ko có sẽ tạo sau đó sử dụng uuid này để làm index 
            //==> mỗi máy khác nhau sẽ có uuid khác nhau và sẽ được lưu vào 1 index riêng 
            //==> tránh trường hợp nhiều máy dùng chung 1 server nhưng đều truy suất vào 1 index 
           string systempath = Environment.GetEnvironmentVariable("SystemRoot");

            string[] pathIncludeName = systempath.Split('\\');// name này bao gồm cả folder trước nó nên cần tách ra lấy name 
             nameDriver = pathIncludeName[0];
            bool checkExitsData = false;

            try
            {
                index = File.ReadAllText($"{nameDriver}\\data_key\\key.txt");

                checkExitsData = dao.CheckExits(dataCheck, index);
            }
            catch
            {
                Guid g = Guid.NewGuid();
                index = g.ToString();
                string newFolderPath = $"{nameDriver}\\data_key";
                bool exists = System.IO.Directory.Exists(newFolderPath);
                if (!exists)
                {
                    var folder = Directory.CreateDirectory(newFolderPath);
                    if (folder.Exists)
                    {
                        File.WriteAllText($"{nameDriver}\\data_key\\key.txt", g.ToString());
                    }
                }
                else
                {
                    File.WriteAllText($"{nameDriver}\\data_key\\key.txt", g.ToString());
                }

            }
          

            PopulateTreeView();
          
            if (!checkExitsData)
            {
                InfoBuilder fileInfoBuilder = new InfoBuilder();
                fileInfo fileInfo = fileInfoBuilder.AddName(dataCheck).Build();
                ListJson.Add(fileInfo);

                Task subThreadForGetAllFile = new Task(() => getAllFileInDriver());

                subThreadForGetAllFile.Start(); // cho tiến trình tìm file chạy 1 thread khác 
            }

            else 
            {
                txtInfo.Visible = false;
                //MessageBox.Show(" data ton tai");
                //btnSearch.Enabled = true;
                Task CheckEventChangeFile = new Task(() => CheckChangeFile());
                CheckEventChangeFile.Start();
   
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
        public static string ReadFile(string path)
        {
            string content;
            try
            {
                if (!path.Contains(".tmp")){// bỏ qua file tmp khi word đang được thay đổi
                    content = File.ReadAllText(path);
                    return content;
                }
  
            }
            catch (FileNotFoundException)
            {

            }
            catch (UnauthorizedAccessException)
            {

            }
            catch (IOException)
            {

            }
            return "";
        }

        // tiến hành chạy để lấy file gửi lên server 
        public void getAllFileInDriver()
        {

            DirectoryInfo info = new DirectoryInfo(@"G:\test\");
            if (IsHandleCreated)
            {
                btnSearch.Invoke(new Action(() => { btnSearch.Enabled = false; })); //đồng bộ để có thể thiết lập disble cho button 
            }

            if (info.Exists)
            {
                Task task = new Task(() => RecursiveGetFile(info.GetDirectories()));
                task.Start();
                GetFileInFolder(info);
                task.Wait();
            }



            // dưới này là chạy tất cả file trên hệ thống, nếu muốn test có thể mở comment dưới này và đống đống code bên trên lại để thử, hiện tại thử trên 1 folder nào đó nhỏ cho nhanh
            /*     var ListDriverInfor = DriveInfo.GetDrives();
                 if (IsHandleCreated)
                 {
                     btnSearch.Invoke(new Action(() => { btnSearch.Enabled = false; })); //đồng bộ để có thể thiết lập disble cho button 
                 }
                 for (int i = 0; i < ListDriverInfor.Length; i++)
                 {
                     DirectoryInfo info = new DirectoryInfo(ListDriverInfor[i].Name);
                     //Debug.WriteLine(i+" "+ info.GetDirectories().Length);

                     if (info.Exists)
                     {

                         Task task = new Task(() => RecursiveGetFile(info.GetDirectories()));
                         task.Start();// trong thread của tiến trình lấy all file tạo ra 1 thread khác để có thể xử lý bất đồng bộ

                         GetFileInFolder(info);// riêng cho thread này để ko ảnh hưởng đến thread main 
                         task.Wait(); // xử lý bất đồng bộ, buộc phải đợi thread hiện tại trong subThreadForGetAllFile chạy xong mới tạo mới thread khác 

                     }


                 }*/

            if (IsHandleCreated) // check neeys như đã tiến hành chạy threadt thì mới chạy cá này để có thể đồng bộ được 
            {
                txtInfo.Invoke(new Action(() => txtInfo.Visible = false));
                btnSearch.Invoke(new Action(() => { btnSearch.Enabled = true; }));
            }


            var bulkIndexResponse = dao.AddList(ListJson, index); // gửi dữ liệu lên server elastic 
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
                var bulkIndexResponse = dao.AddList(ListJson, index);
                ListJson.Clear();
            }
            try
            {
                foreach (FileInfo file in subDir.GetFiles())
                {
                  
                    // đọc và lấy ra những path có định dạng file là txt, doc, pdf
                    if (file.Extension == ".txt" || file.Extension == ".docx" || file.Extension == ".pdf")
                    {
                        string content = "";
                        if(file.Extension == ".pdf")
                        {
                            Debug.WriteLine("da vao");
                            content = GetTextFromPDF(file.FullName);
                        }
                        else if(file.Extension == ".docx")
                        {
                            content = GetTextFromDocx(file.FullName);
                        }
                        else
                        {
                            content = File.ReadAllText(file.FullName);
                        }

                        InfoBuilder fileInfoBuilder = new InfoBuilder();
                        fileInfo fileInfo = fileInfoBuilder.AddName(file.Name).AddContent(file.FullName).AddPath(content).Build();
                        ListJson.Add(fileInfo);

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
            DirectoryInfo nodeDirInfo = (DirectoryInfo)newSelected.Tag;
            parentDirInfo = nodeDirInfo; // lưu lại parent root để truy suất ngược lại khi cần ( dùng khi muốn load lại listview)
            
            try
            {
                // kiểm tra xem nếu nhánh của cây đã được bấm vào rồi thì khi bấm vào lần 2 trở lên sẽ ko gọi để quy nữa ( tránh tạo ra nhiều nhánh trùng)
                if (!listPath.Contains(nodeDirInfo.FullName))
                {
                    // nếu không có trong list thì add vào list
                    listPath.Add(nodeDirInfo.FullName);
                    // bấm vào sẽ tìm kiếm các file và folder con 
                    GetDirectories(nodeDirInfo.GetDirectories(), newSelected);
                }

                // gắn file vào folder con vào list vỉew
                AddItemToListView(nodeDirInfo);

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

        // hiện tại sẽ viết tạm ở phần dưới này các chức năng như xóa sửa 

        //--- file system watcher
        private static void OnChanged(object sender, FileSystemEventArgs e)
        {
            try
            {
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

                            string content = "";

                            // get content
                            if (path.Contains(".txt"))
                                content = ReadFile(path);
                            if (path.Contains(".pdf"))
                                content = GetTextFromPDF(path);
                            if (path.Contains(".doc") || path.Contains(".docx"))
                                content = ReadFile(path);
                            InfoBuilder fileInfoBuilder = new InfoBuilder();
                            fileInfo fileInfo = fileInfoBuilder.AddName(name).AddContent(path).AddPath(content).Build();
                            // update elastic
                            var id = dao.GetId(e.FullPath, index);
                            if (id != null)
                            {
                                dao.Update(fileInfo, id, index);
                            }

                            // End Change

                            fileWriteTime[path] = currentLastWriteTime;
                        }
                    }
                }

            }
            catch (FileNotFoundException)
            {

            }

        }

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

                            string content = "";
                            if (path.Contains(".txt"))
                                content = ReadFile(path);
                            if (path.Contains(".pdf"))
                               content = GetTextFromPDF(path);
                            if (path.Contains(".doc") || path.Contains(".docx"))
                                content = ReadFile(path);

                            InfoBuilder fileInfoBuilder = new InfoBuilder();
                            fileInfo fileInfo = fileInfoBuilder.AddName(name).AddContent(path).AddPath(content).Build();
                            dao.Add(fileInfo, index);
                            // End Create

                            fileWriteTime[path] = currentLastWriteTime;
                        }
                    }
                }
            }
            catch (FileNotFoundException)
            {

            }

        }


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
                            var id = dao.GetId(path, index);
                            if (id != null)
                            {
                                dao.Deleted(id, index);
                            }
                            // End Delete

                            fileWriteTime[path] = currentLastWriteTime;
                        }
                    }
                }
            }
            catch (FileNotFoundException)
            {

            }

        }

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

                            string content = "";
                            if (path.Contains(".txt"))
                                content = ReadFile(path);
                            if (path.Contains(".pdf"))
                               content = GetTextFromPDF(path);
                            if (path.Contains(".doc") || path.Contains(".docx"))
                                content = ReadFile(path);


                            InfoBuilder fileInfoBuilder = new InfoBuilder();
                            fileInfo fileInfo = fileInfoBuilder.AddName(name).AddContent(path).AddPath(content).Build();
                            var id = dao.GetId(e.OldFullPath, index);
                            if (id != null)
                            {
                                dao.Update(fileInfo, id, index);
                            }
                            // End Rename

                            fileWriteTime[path] = currentLastWriteTime;
                        }
                    }
                }
            }
            catch (FileNotFoundException)
            {

            }
            catch (IOException)
            {

            }

        }

        private void label1_Click(object sender, EventArgs e)
        {

        }

        // Viết chức năng tìm kiếm
        private void btnSearch_Click(object sender, EventArgs e)
        {
            listView1.Items.Clear();
            string text = txtFindText.Text;

            ListViewItem item = null;
            ListViewItem.ListViewSubItem[] subItems;

            var searchDatas = dao.Search(text, index);
            foreach (var data in searchDatas)
            {

                item = new ListViewItem(data.Name, 1);
                subItems = new ListViewItem.ListViewSubItem[]
                    {new ListViewItem.ListViewSubItem(item, "File"),
                        new ListViewItem.ListViewSubItem(item,
                           ""),
                        new ListViewItem.ListViewSubItem(item,data.Path)};
                item.SubItems.AddRange(subItems);
                listView1.Items.Add(item);
            }
        }

        private void txtPath_TextChanged(object sender, EventArgs e)
        {

        }

        private void renameToolStripMenuItem_Click(object sender, EventArgs e)
        {

        }

        private void renameToolStripMenuItem_Click_1(object sender, EventArgs e)
        {
            if (listView1.SelectedItems.Count > 0)
            {
                listView1.SelectedItems[0].BeginEdit();// cho phép edit trên listview
            }
        }

        private void deleteToolStripMenuItem_Click(object sender, EventArgs e)
        {
            DialogResult dialogResult = MessageBox.Show("Bạn có muốn xóa?",
                   "Delete",
                   MessageBoxButtons.YesNo,
                   MessageBoxIcon.Exclamation
               );
            if (dialogResult == DialogResult.Yes)
            {
                if (listView1.SelectedItems.Count > 0)
                {
                    Debug.WriteLine(parentDirInfo);
                    int index = listView1.SelectedItems[0].Index;
                    string path = listView1.Items[index].SubItems[3].Text;
                    string type = listView1.Items[index].SubItems[1].Text;
                    if (type == "Directory")
                    {
                        System.IO.Directory.Delete(path, true);
                        listView1.Refresh();
                        AddItemToListView(parentDirInfo);
                    }
                    else
                    {
                        File.Delete(path);
                        AddItemToListView(parentDirInfo);
                    }
                }
            }
         
        }

        private void listView1_AfterLabelEdit(object sender, LabelEditEventArgs e)
        {
            string oldname = listView1.Items[listView1.SelectedIndices[0]].SubItems[0].Text;
            string oldPath = listView1.Items[listView1.SelectedIndices[0]].SubItems[3].Text;
            string pathNotIncludeName = oldPath.Substring(0, oldPath.Length - oldname.Length);
            string newName = e.Label;
            if (string.IsNullOrEmpty(newName))
            {
                e.CancelEdit = true;
                MessageBox.Show("Please enter a valid value.");
                return;
            }

            Debug.WriteLine(newName);
            System.IO.File.Move(@"" + oldPath, @"" + pathNotIncludeName + newName);
            AddItemToListView(parentDirInfo);
        }
        
        public void AddItemToListView(DirectoryInfo nodeDirInfo)
        {
            listView1.Items.Clear();
            ListViewItem.ListViewSubItem[] subItems;
            ListViewItem item = null;

            foreach (DirectoryInfo dir in parentDirInfo.GetDirectories())
            {
                item = new ListViewItem(dir.Name, 0);
                subItems = new ListViewItem.ListViewSubItem[]
                    {new ListViewItem.ListViewSubItem(item, "Directory"),
                        new ListViewItem.ListViewSubItem(item,
                            dir.LastAccessTime.ToShortDateString()),
                        new ListViewItem.ListViewSubItem(item,dir.FullName)}; // thêm dòng này
                item.SubItems.AddRange(subItems);
                listView1.Items.Add(item);
            }
            foreach (FileInfo file in parentDirInfo.GetFiles())
            {
                item = new ListViewItem(file.Name, 1);
                subItems = new ListViewItem.ListViewSubItem[]
                    { new ListViewItem.ListViewSubItem(item, "File"),
                        new ListViewItem.ListViewSubItem(item,
                        file.LastAccessTime.ToShortDateString()),
                        new ListViewItem.ListViewSubItem(item,file.FullName)};

                item.SubItems.AddRange(subItems);
                listView1.Items.Add(item);
            }
        }
        // tạo file
        private void newFileToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if(parentDirInfo != null)
            {
                string path = parentDirInfo.FullName;
                string fileName = Interaction.InputBox("Enter file's name", "Create new file", "New Text Document.txt", 400, 300);
                if (fileName != null)
                {
                    // kiểm tra xem có file nào có tên vừa nhập vào new file hay chưa 
                    foreach (FileInfo file in parentDirInfo.GetFiles())
                    {
              
                        if (file.Name == fileName)
                        {   
                            // nếu có thì dừng 
                            MessageBox.Show("File's name is exist");
                            return;
                        
                        }
                    }
                    if (!string.IsNullOrEmpty(fileName))
                    {
                        var created = File.Create($@"{path}\{fileName}");
                        created.Close(); // create file bằng File bắt buộc phải close nếu koi thì sẽ ko thể sử dụng ở nơi khác do process đang được sử dụng
                        AddItemToListView(parentDirInfo);
                    }
                    else 
                    {
                        return;
                    }

                }

            }

        }
        // tạo folder
        private void newFolderToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (parentDirInfo != null)
            {
                string path = parentDirInfo.FullName;
                string fileName = Interaction.InputBox("Enter file's name", "Create new file", "New folder", 400, 300);
                string newFolderPath = $@"{path}\{fileName}";
                bool exists = System.IO.Directory.Exists(newFolderPath);
                if (!exists)
                {
                   var folder =  Directory.CreateDirectory(newFolderPath);
                    AddItemToListView(parentDirInfo);
                }
                else
                {
                    MessageBox.Show("Folder exists");
                    return;
                }
            }
        }

        private void listView1_DoubleClick(object sender, EventArgs e)
        {
            int index = listView1.SelectedItems[0].Index;
            string type = listView1.Items[index].SubItems[1].Text;
            string path = listView1.Items[index].SubItems[3].Text;
 
            if (type == "File")
            {
                Process.Start(path);
            }
  

        }
        private static string GetTextFromPDF(string path)
        {
            try
            {
                PdfReader reader = new PdfReader(path);
                string text = string.Empty;
                for (int page = 1; page <= reader.NumberOfPages; page++)
                {
                    text += PdfTextExtractor.GetTextFromPage(reader, page);
                }
                reader.Close();

                return text;
            }
            catch (IOException)
            {

            }
            return "";
      
        }
        private void CheckChangeFile()
        {
            try
            {

                using (StreamReader file = new StreamReader($"{nameDriver}\\data_key\\log.txt"))
                {
                    Debug.WriteLine("da vao");
                    string ln;

                    while ((ln = file.ReadLine()) != null)
                    {
                        if (ln.Contains("CREATE"))
                        {
                            string[] lnData = ln.Split(' ');
                            string path = lnData[lnData.Length - 1];
                            string[] listPathName = path.Split('\\'); // name này bao gồm cả folder trước nó nên cần tách ra lấy name 
                            string namefile = listPathName[listPathName.Length - 1];
                            Debug.WriteLine(path, namefile);
                            InfoBuilder fileInfoBuilder = new InfoBuilder();
                            string content = "";
                       /*     if (namefile.Contains(".pdf"))
                            {
                                content = GetTextFromPDF(path);
                            }
                            else if (namefile.Contains(".docx"))
                            {
                                content = GetTextFromDocx(path);
                            }
                            else
                            {
                                content = File.ReadAllText(path);
                            }*/
                            fileInfo fileInfo = fileInfoBuilder.AddName(namefile).AddContent("").AddPath(path).Build();
                            Thread threadAdd = new Thread(()=>dao.Add(fileInfo, index));
                            threadAdd.Start();
                


                        }
                        if (ln.Contains("DELETE"))
                        {
                            string[] lnData = ln.Split(' ');
                            string path = lnData[lnData.Length - 1];
                            string[] listPathName = path.Split('\\'); // name này bao gồm cả folder trước nó nên cần tách ra lấy name 
                            string namefile = listPathName[listPathName.Length - 1];
                            Debug.WriteLine(path, namefile);
                        }
                        if (ln.Contains("RENAME"))
                        {
                            string[] lnData = ln.Split(' ');
                            string oldPath = lnData[1];
                            string newPath = lnData[2];
                            Debug.WriteLine(lnData.Length);
                            string[] listPathName = newPath.Split('\\'); // name này bao gồm cả folder trước nó nên cần tách ra lấy name 
                            string namefile = listPathName[listPathName.Length - 1];
                            Debug.WriteLine("old " + oldPath + "new " + newPath + "new " + namefile);
                        }
                    }
                    file.Close();
                }

            }
            catch (IOException)
            {

            }
        }
        private static string GetTextFromDocx(object path)
        {

                 //the whole document
            try
            {
                string totaltext = "";
                Microsoft.Office.Interop.Word.Application word = new Microsoft.Office.Interop.Word.Application();
                object miss = System.Reflection.Missing.Value;
                object readOnly = true;
                Microsoft.Office.Interop.Word.Document docs = word.Documents.Open(ref path, ref miss, ref readOnly, ref miss, ref miss,
                            ref miss, ref miss, ref miss, ref miss, ref miss, ref miss, ref miss, ref miss, ref miss, ref miss, ref miss);
                for (int i = 0; i < docs.Paragraphs.Count; i++)
                {
                    totaltext += docs.Paragraphs[i + 1].Range.Text.ToString();
                }
                docs.Close();
                word.Quit();
                return totaltext;

            }
            catch (COMException)
            {

            }
            catch (IOException)
            {

            }


            return "";
       
        }


    }
}
