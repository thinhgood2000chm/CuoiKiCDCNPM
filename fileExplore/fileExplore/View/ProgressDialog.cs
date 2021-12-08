using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace fileExplore
{
    public partial class ProgressDialog : Form
    {
        public ProgressDialog()
        {
            InitializeComponent();
        }
        public void UpdateProgress(int progress, int maxValue)
        {
            if (progressBar.InvokeRequired)
                progressBar.BeginInvoke(new Action(() => {
                    progressBar.Maximum = maxValue;
                    progressBar.Value = progress; 
                    
                }));
            else
                progressBar.Value = progress;

        }

        public void SetIndeterminate(bool isIndeterminate)
        {
            if (progressBar.InvokeRequired)
            {
                progressBar.BeginInvoke(new Action(() =>
                {
                    if (isIndeterminate)
                        progressBar.Style = ProgressBarStyle.Marquee;
                    else
                        progressBar.Style = ProgressBarStyle.Blocks;
                }
                ));
            }
            else
            {
                if (isIndeterminate)
                    progressBar.Style = ProgressBarStyle.Marquee;
                else
                    progressBar.Style = ProgressBarStyle.Blocks;
            }
        }

        private void progressBar_Click(object sender, EventArgs e)
        {

        }
    }
}
