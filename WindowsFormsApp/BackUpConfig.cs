using BackupService;
using ServiceBackup.BackupServices;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace WindowsFormsApp
{
    public partial class BackUpConfig : Form
    {
        private string configFilePath = Path.Combine(Path.GetDirectoryName(Application.ExecutablePath)) + "\\Data_Backup.xml";
        private BackupData backupData;
        public BackUpConfig()
        {
            InitializeComponent();
        }

        private void btnFile_Click(object sender, EventArgs e)
        {
            FolderBrowserDialog folderBrowserDialog = new FolderBrowserDialog();
            folderBrowserDialog.Description = "Chọn thư mục";
            folderBrowserDialog.RootFolder = Environment.SpecialFolder.MyComputer;

            if (folderBrowserDialog.ShowDialog() == DialogResult.OK)
            {
                string selectedPath = folderBrowserDialog.SelectedPath;
                txtFile.Text = selectedPath;
            }
        }

        private void txtFileCount_KeyPress(object sender, KeyPressEventArgs e)
        {
            // Chỉ cho phép người dùng nhập các ký tự số từ 0 đến 9, ký tự Back và ký tự Delete.
            if (!char.IsDigit(e.KeyChar) && e.KeyChar != '\b' && e.KeyChar != 127)
            {
                e.Handled = true; // Loại bỏ ký tự không hợp lệ.
            }
        }

        private void BackUpConfig_Load(object sender, EventArgs e)
        {
            try
            {
                backupData = new GlobalConfig<BackupData>().ReadConfigPath(configFilePath);
                if (backupData == null)
                {
                    backupData = new BackupData();
                }
                txtFile.Text = backupData.File;
                txtHost.Text = backupData.Host;
                txtTK.Text = backupData.Username;
                txtMK.Text = backupData.Password;
                txtFileCount.Text = backupData.FileCount.ToString();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void btnSubmit_Click(object sender, EventArgs e)
        {
            backupData.File = txtFile.Text.Trim();
            backupData.Host = txtHost.Text.Trim();
            backupData.Username = txtTK.Text.Trim();
            backupData.Password = txtMK.Text.Trim();
            backupData.FileCount = int.Parse(txtFileCount.Text.Trim());
            new GlobalConfig<BackupData>().WriteConfig(configFilePath, backupData);
            //new Main().Show();
            //this.Hide();
            this.Close();
        }

        private void BackUpConfig_FormClosing(object sender, FormClosingEventArgs e)
        {
            //DialogResult result = MessageBox.Show("Bạn có muốn thoát khỏi ứng dụng?", "Xác nhận thoát", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            //if (result == DialogResult.No)
            //{
            //    e.Cancel = true;
            //}
            //else
            //{
            //    Application.ExitThread();
            //}
        }

        private void txtHost_TextChanged(object sender, EventArgs e)
        {

        }
    }
}
