using ServiceBackup.BackupServices;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;

namespace BackupService
{
    public partial class ServiceBackup : ServiceBase
    {
        private BackupServices backupService = new BackupServices();
        public ServiceBackup()
        {
            InitializeComponent();
#if DEBUG      
            backupService = new BackupServices();
            backupService.Start();
#endif
        }

        protected override void OnStart(string[] args)
        {
            if(!backupService.Start())
                this.Stop();
        }

        protected override void OnStop()
        {
            if(!backupService.Started)
                backupService.Stop();
        }
    }
}
