using BackupService;
using ServiceBackup.BackupServices;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.Tab;

namespace WindowsFormsApp
{
    public partial class Main : Form
    {
        private BackupServices backupService;
        private ServiceController serviceController;
        private TimeSpan timeoutService = TimeSpan.FromMilliseconds(15000);
        private string executablePath = Path.Combine(Path.GetDirectoryName(Application.ExecutablePath)) + "\\" + "Data_Backup.xml";
        private Boolean runDequeueRestartService = true;
        private Thread threadRestartService;
        public enum ServiceStatus
        {
            NotInstalled = 0,
            Stopped = 1,
            Running = 2,
        }


        public Main()
        {
            backupService = new BackupServices();
            InitializeComponent();
            threadRestartService = new Thread(RestartServiceThread);
            threadRestartService.Start();
        }

        private void btnConfig_Click(object sender, EventArgs e)
        {
            BackUpConfig backUpConfig = new BackUpConfig();
            backUpConfig.ShowDialog();
            //this.Hide();
        }

        private void Main_FormClosing(object sender, FormClosingEventArgs e)
        {
            DialogResult result = MessageBox.Show("Bạn có muốn thoát khỏi ứng dụng?", "Xác nhận thoát", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (result == DialogResult.No)
            {
                e.Cancel = true;
            }
            else
            {
                Application.ExitThread();
            }
        }

        private void btnStartStop_Click(object sender, EventArgs e)
        {
            Button btn = (Button)sender;
            this.StartStopService(btn);
        }

        private void StartStopService(Button btn)
        {
            //AccessibleName: Đặt bằng tên của service
            string serviceName = btn.AccessibleName;

            ServiceStatus serviceStatus = StatusService(serviceName);

            if (serviceStatus == ServiceStatus.NotInstalled)
            {
                MessageBox.Show("Service chưa được cài đặt.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            serviceController = new ServiceController(serviceName);

            try
            {
                if (serviceStatus == ServiceStatus.Running)
                {
                    serviceController.Stop();
                    serviceController.WaitForStatus(ServiceControllerStatus.Stopped, timeoutService);
                }
                else
                {
                    if (!CheckConfigStartService(serviceName))
                    {
                        return;
                    }

                    serviceController.Start();
                    serviceController.WaitForStatus(ServiceControllerStatus.Running, timeoutService);
                }
            }
            catch (Exception ex)
            {

                MessageBox.Show($"Không {(serviceStatus == ServiceStatus.Running ? "Stop" : "Start")} được service.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

            //Hiển thị trạng thái service
            this.SetStatusService(serviceName);
        }

        private bool CheckConfigStartService(string serviceName)
        {
            switch (serviceName)
            {
                case "ServiceBackupName":
                    //kiểm tra thông tin cấu hình service dọn dẹp dữ liệu
                    return this.CheckCleanData();
            }
            return true;
        }

          private bool CheckCleanData()
        {
            try
            {
                //Lấy các thông số cấu hình của service
                BackupData serviceConfig = new GlobalConfig<BackupData>().ReadConfigPath(executablePath);
                if (serviceConfig == null) {
                    MessageBox.Show("Chưa cấu hình cho hệ thống!", "Thông báo lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return false;
                } 
                
            }
            catch (Exception ex)
            {
                return false;
            }
            return true;
        }

        /// <summary>
        /// Hiển thị trạng thái service theo tên
        /// </summary>
        /// <param name="serviceName"></param>
        private void SetStatusService(string serviceName)
        {
            if (StatusService(serviceName) == ServiceStatus.Running)
            {
                btnStartStop.Text = "Stop";
                btnStartStop.ForeColor = Color.Red;
                lblStatus.Text = "Đang hoạt động";
                lblStatus.ForeColor = Color.Green;
            }
            else if (StatusService(serviceName) == ServiceStatus.Stopped)
            {
                btnStartStop.Text = "Start";
                btnStartStop.ForeColor = Color.Blue;
                lblStatus.Text = "Không hoạt động";
                lblStatus.ForeColor = Color.Red;
            }
            else
            {
                btnStartStop.Text = "Start";
                btnStartStop.ForeColor = Color.Black;
                lblStatus.Text = "Chưa cài đặt";
                lblStatus.ForeColor = Color.Black;
            }
        }
        /// <summary>
        /// Lấy trạng thái hoạt động của service
        /// </summary>
        /// <param name="serviceName">Tên service</param>
        /// <returns></returns>
        private ServiceStatus StatusService(string serviceName)
        {
            try
            {
                serviceController = new ServiceController(serviceName);
                //Nếu đang Running quy về đang hoạt động
                if (serviceController.Status == ServiceControllerStatus.Running)
                {
                    return ServiceStatus.Running;
                }
                else
                {
                    //Ngược lại thì quy về đang dừng
                    return ServiceStatus.Stopped;
                }
            }
            catch
            {
                //Xảy ra lỗi thì service chưa được cài
                return ServiceStatus.NotInstalled;
            }
        }

        private void RestartServiceThread()
        {
            while (runDequeueRestartService)
            {
                try
                {
                    SetStatusService("ServiceBackupName");
                }
                catch (Exception ex)
                {

                }
                //Thread.Sleep(5000);
            }
        }
    }
}
