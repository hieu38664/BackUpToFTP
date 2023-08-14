using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace BackupService
{
    [XmlRoot("BackupData")]
    public class BackupData
    {
        public string File { get; set; }
        public string Host { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
        public int FileCount { get; set; }
    }
}
