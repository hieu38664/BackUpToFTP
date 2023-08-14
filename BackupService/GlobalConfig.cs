using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml.Serialization;

namespace BackupService
{
    public class GlobalConfig<T>
    {
        public T ReadConfigPath(string configFilePath)
        {
            Stream fs = null;
            T systemConfig = default(T);
            try
            {
                XmlSerializer serializer = new XmlSerializer(typeof(T));

                if (!File.Exists(configFilePath))
                    return systemConfig;

                FileInfo configFile = new FileInfo(configFilePath);
                fs = configFile.OpenRead();
                systemConfig = (T)serializer.Deserialize(fs);
                fs.Close();
            }
            catch (Exception ex)
            {

            }
            finally
            {
                if (fs != null)
                    fs.Close();
            }
            return systemConfig;
        }
        public bool WriteConfig(string filePath, T configObject)
        {
            bool isSuccess = false;
            TextWriter textWriter = null;
            try
            {
                XmlSerializer serializer = new XmlSerializer(typeof(T));
                //Nếu không tồn tại thì tạo file
                if (!File.Exists(filePath))
                {
                    File.Create(filePath).Close();
                }

                textWriter = new StreamWriter(filePath);
                serializer.Serialize(textWriter, configObject);
                isSuccess = true;
            }
            catch (Exception ex)
            {

            }
            finally
            {
                if (textWriter != null)
                    textWriter.Close();
            }
            return isSuccess;
        }
        public string GetPathExecute()
        {
            return Path.GetDirectoryName(Application.StartupPath);
        }
    }
}
