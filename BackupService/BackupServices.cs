using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Xml.Serialization;
using BackupService;
using Application = System.Windows.Forms.Application;
using System.Threading;
using System.Globalization;

namespace ServiceBackup.BackupServices
{
    public class BackupServices
    {
        private BackupData backupData;

        public string host { get; private set; }
        public string username { get; private set; }
        public string password { get; private set; }
        public string sourceFilePath { get; private set; }
        public int fileCount { get; private set; }


        public Boolean Started { get; private set; }
        private Thread threadBackupData;
        private Boolean runDequeueBackupData = true; //sửa là false, nên đê thế

        //public class FtpFileInfo
        //{
        //    public string FileName { get; set; }
        //    public DateTime CreationTime { get; set; }
        //    public FtpFileInfo(string line)
        //    {
        //        string[] tokens = line.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
        //        if (tokens.Length >= 4)
        //        {
        //            DateTime creationTime = ParseDateTime(tokens[5], tokens[6], tokens[7]);
        //            string[] nameTokens = new string[tokens.Length - 8]; Array.Copy(tokens, 8, nameTokens, 0, tokens.Length - 8);
        //            FileName = string.Join("", nameTokens); //DateTime.TryParse($" {tokens[5]} {tokens [6]} {tokens[7]}", out DateTime result);
        //            CreationTime = creationTime;
        //        }

        //    }
        //    static DateTime ParseDateTime(string month, string day, string time)
        //    {
        //        string forrmatDateTime = $"{month} {day} {time}";
        //        return DateTime.ParseExact(forrmatDateTime, "MMM dd HH:mm", CultureInfo.InvariantCulture);
        //    }
        //}
        public BackupServices()
        {
            Initialize();
        }
        private void Initialize()
        {
            try
            {
                //backupData = new GlobalConfig<BackupData>().ReadConfigPath(xmlFilePath);
                backupData = ReadConfig("Data_Backup.xml");
                if (backupData == null)
                {
                    backupData = new BackupData();
                }
                host = backupData.Host;
                username = backupData.Username;
                password = backupData.Password;
                sourceFilePath = backupData.File;
                fileCount = backupData.FileCount;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }
        public Boolean Start()
        {
            try
            {
                threadBackupData = new Thread(BackupDataThread);
                threadBackupData.Start();
                Console.WriteLine("OK - BackupServices");
                Started = true;
                return Started;
            }
            catch (Exception ex)
            {
                this.Stop();
                return false;
            }
        }
        private void BackupDataThread()
        {
            while (runDequeueBackupData)
            {
                try
                {
                    FtpUploader();
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Đã xảy ra lỗi: " + ex.Message);
                }
                Thread.Sleep(3000);
            }
        }
        public Boolean Stop()
        {
            try
            {
                if (runDequeueBackupData)
                {
                    runDequeueBackupData = false;
                    threadBackupData.Abort();
                }
                return true;
            }
            catch (Exception ex)
            {

                return false;
            }
        }
        public void FtpUploader()
        {
            try
            {
                string[] bakFiles = Directory.GetFiles(sourceFilePath, "*.bak").OrderByDescending(f => new FileInfo(f).CreationTime).ToArray();
                int fileCountLimited = Math.Min(fileCount, bakFiles.Length);
                for (int i = 0; i < fileCountLimited; i++)
                {
                    // Tạo tên tệp zip dựa vào tên tệp .bak
                    string bakFile = bakFiles[i];
                    string bakFileName = Path.GetFileNameWithoutExtension(bakFile);
                    string zipFileName = $"{DateTime.Now:yyyyMMdd_HHmmss}_{bakFileName}.zip";
                    string zipFilePath = Path.Combine(sourceFilePath, zipFileName);

                    // Kiểm tra xem tệp zip đã tồn tại hay chưa
                    if (File.Exists(zipFilePath))
                    {
                        continue;
                    }

                    // Tạo tệp zip mới
                    using (ZipArchive zipArchive = ZipFile.Open(zipFilePath, ZipArchiveMode.Create))
                    {
                        // Thêm tệp .bak vào tệp zip
                        zipArchive.CreateEntryFromFile(bakFiles[i], Path.GetFileName(bakFiles[i]));
                    }

                    // Xoá tệp .bak sau khi đã tạo tệp zip
                    File.Delete(bakFile);
                }

                // Tạo đối tượng FtpWebRequest
                FtpWebRequest request = (FtpWebRequest)WebRequest.Create(host + "/");
                request.Method = WebRequestMethods.Ftp.ListDirectory;
                request.Credentials = new NetworkCredential(username, password);

                // Lấy danh sách các tệp zip từ thư mục nguồn, sắp xếp theo thời gian tạo giảm dần
                string[] zipFiles = Directory.GetFiles(sourceFilePath, "*.zip")
                                            .OrderByDescending(f => new FileInfo(f).CreationTime)
                                            .ToArray();

                // Lấy danh sách các tệp zip đã có trên server
                //List<FtpFileInfo> existingZipFiles = new List<FtpFileInfo>();
                List<string> existingZipFiles = new List<string>();

                using (FtpWebResponse response = (FtpWebResponse)request.GetResponse())
                using (StreamReader reader = new StreamReader(response.GetResponseStream()))
                {
                    try
                    {
                        while (!reader.EndOfStream)
                        {
                            //string line = reader.ReadLine();
                            //while (!string.IsNullOrEmpty(line))
                            //{
                            //    var fileInfor = new FtpFileInfo(line);
                            //    existingZipFiles.Add(fileInfor);
                            //    line = reader.ReadLine();
                            //}

                            string fileName = reader.ReadLine();
                            if (fileName.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))
                            {
                                    existingZipFiles.Add(fileName);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("Đã xảy ra lỗi: " + ex.Message);
                    }
                }
                
                // Lấy danh sách các tệp zip mới cần upload lên server
                var filesToSend = zipFiles
                    .OrderByDescending(zipFile => File.GetLastWriteTime(zipFile)) // Sắp xếp theo thời gian sửa đổi giảm dần.
                                                                                  //.Where(zipFile => !existingZipFiles.Contains(Path.GetFileName(zipFile)))
                    .Take(fileCount);
                // Upload tất cả các tệp zip mới lên server
                foreach (var zipFile in filesToSend)
                {
                    string destinationFilePath = "/" + Path.GetFileName(zipFile);
                    if (!existingZipFiles.Contains(destinationFilePath))
                    //if (!existingZipFiles.Any(fileInfo => fileInfo.FileName.Equals(Path.GetFileName(destinationFilePath), StringComparison.OrdinalIgnoreCase)))
                    {
                        using (FileStream fileStream = File.OpenRead(zipFile))
                        {
                            request = (FtpWebRequest)WebRequest.Create(host + destinationFilePath);
                            request.Method = WebRequestMethods.Ftp.UploadFile;
                            request.Credentials = new NetworkCredential(username, password);
                            using (Stream requestStream = request.GetRequestStream())
                            {
                                fileStream.CopyTo(requestStream);
                            }
                        }
                    }
                    File.Delete(zipFile);
                }
                {
                    ////int fileCountLimited = Math.Min(fileCount, existingZipFiles.Count);
                    //if (existingZipFiles.Count > fileCount)
                    //{
                    //    // Sắp xếp các tệp zip đã có trên server theo thời gian tạo tăng dần
                    //    existingZipFiles = existingZipFiles.OrderBy(f => new FileInfo(f).CreationTime).ToList();
                    //    // Xoá các tệp zip cũ nhất trên server
                    //    for (int i = 0; i < existingZipFiles.Count - fileCount; i++)
                    //    {
                    //        var zipFiless = existingZipFiles.OrderBy(f => new FileInfo(f).CreationTime).FirstOrDefault();
                    //        string zipFileToDelete = existingZipFiles[i];
                    //        request = (FtpWebRequest)WebRequest.Create(host + "/" + zipFileToDelete);
                    //        request.Credentials = new NetworkCredential(username, password);
                    //        request.Method = WebRequestMethods.Ftp.DeleteFile;
                    //        using (FtpWebResponse deleteResponse = (FtpWebResponse)request.GetResponse()) { }

                    //        // Xoá tất cả các tệp zip đã xoá khỏi danh sách
                    //        existingZipFiles.RemoveAt(i);
                    //        i--;
                    //    }
                    //}
                }
                while (existingZipFiles.Count > fileCount)
                {
                    // Sắp xếp các tệp zip đã có trên server theo thời gian tạo tăng dần
                    //var oldestFile = existingZipFiles.OrderBy(f => f.CreationTime).FirstOrDefault();
                    var oldestFile = existingZipFiles.OrderBy(f => new FileInfo(f).FullName).FirstOrDefault();

                    if (oldestFile != null)
                    {
                        // Tiến hành xoá tệp tin từ máy chủ FTP
                        request = (FtpWebRequest)WebRequest.Create(host + "/" + oldestFile);
                        request.Credentials = new NetworkCredential(username, password);
                        request.Method = WebRequestMethods.Ftp.DeleteFile;
                        using (FtpWebResponse deleteResponse = (FtpWebResponse)request.GetResponse()) { }

                        // Xoá tệp tin cũ nhất khỏi danh sách
                        existingZipFiles.Remove(oldestFile);
                    }
                    else
                    {
                        break;
                    }
                }

            }
            catch (Exception ex)
            {
                Console.WriteLine("Đã xảy ra lỗi trong quá trình upload: " + ex.Message);
            }
        }
        /// <summary>
        /// Đọc file config theo tên file
        /// </summary>
        /// <param name="configFileName"></param>
        /// <returns></returns>
        public BackupData ReadConfig(string configFileName)
        {
            Stream fs = null;
            BackupData systemConfig = default(BackupData);
            try
            {
                XmlSerializer serializer = new XmlSerializer(typeof(BackupData));
                string filePath = FindFile(Path.GetDirectoryName(Application.StartupPath), configFileName, SearchOption.AllDirectories, "*.xml");

                if (string.IsNullOrEmpty(filePath))
                    return systemConfig;

                FileInfo configFile = new FileInfo(filePath);
                fs = configFile.OpenRead();
                systemConfig = (BackupData)serializer.Deserialize(fs);
                fs.Close();
            }
            catch (Exception ex)
            {
                //Log.WriteException("SuLyCauHinhNhac->Initialize->ReadConfig->Exception:", ex.Message);
            }
            finally
            {
                if (fs != null)
                    fs.Close();
            }
            return systemConfig;
        }

        /// <summary>
        /// Tìm file theo điểu kiện
        /// </summary>
        /// <param name="dir"></param>
        /// <param name="fileName"></param>
        /// <param name="searchOption"></param>
        /// <param name="extension">Loại file</param>
        /// <param name="loopLimit"></param>
        /// <returns></returns>
        public static string FindFile(string dir, string fileName, SearchOption searchOption, string extension = null, int loopLimit = 10)
        {
            if (--loopLimit < 0 || dir == null)
            {
                return null;
            }

            String filepath = null;
            filepath = ExistsFile(dir, fileName, searchOption, extension);
            if (filepath == null)
            {
                filepath = FindFile(System.IO.Path.GetDirectoryName(dir), fileName, searchOption, extension, loopLimit);
            }
            return filepath;
        }

        /// <summary>
        /// Kiểm tra xem file có tồn tại không
        /// </summary>
        /// <param name="dir"></param>
        /// <param name="fileName">Tên file tìm kiếm</param>
        /// <param name="searchOption"></param>
        /// <param name="extension">Loại file</param>
        /// <returns></returns>
        private static string ExistsFile(string dir, string fileName, SearchOption searchOption, string extension = null)
        {
            string[] allfiles = Directory.GetFiles(dir, string.IsNullOrEmpty(extension) ? "*.*" : extension, searchOption);

            string ret = null;
            if (allfiles != null && allfiles.Count() > 0)
            {
                ret = allfiles.Where(r => Path.GetFileName(r).Equals(fileName)).FirstOrDefault();
            }
            return ret;
        }
    }
}
