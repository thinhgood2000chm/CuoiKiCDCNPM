using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace fileExplore.View
{
    public partial class RenameFile : Form
    {
        string pathname;
        public RenameFile(string name, string path)
        {
            InitializeComponent();
            pathname = path;
            txtOldName.Text = name;
        }

        private void txtOldPath_TextChanged(object sender, EventArgs e)
        {

        }

        private void button1_Click(object sender, EventArgs e)
        {
            string newName = txtNewName.Text;
            string oldName = txtOldName.Text;
            if(newName.Trim()!=oldName.Trim())
            {
                Debug.WriteLine(@""+pathname + oldName);
                Debug.WriteLine(@""+pathname + newName);
                System.IO.File.Move(@""+pathname + oldName,@""+pathname + newName);
                this.Close();
            }
        }
    }
}
