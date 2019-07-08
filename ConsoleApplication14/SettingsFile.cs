using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Text.RegularExpressions;

namespace VkDocsBrute
{
    class SettingsFile
    {
        string pathToSettings = "setting.ini";

        public bool IsExist()
        {
            if (File.Exists(pathToSettings))
                return true;
            return false;
        }

        public bool ReadSettingsFile(out Dictionary<string, int> settings)
        {
            settings = new Dictionary<string, int>();
            if (IsExist())
            {
                string[] linesFromFile = File.ReadAllLines(pathToSettings);

                Regex regex = new Regex(@"(\w*)=(\d*)", RegexOptions.Compiled);
                for (int i = 0; i < linesFromFile.Length; i++)
                {
                    if (regex.IsMatch(linesFromFile[i]))
                    {
                        string[] tmp = linesFromFile[i].Split(new char[] { '=' }, StringSplitOptions.RemoveEmptyEntries);
                        settings.Add(tmp[0], int.Parse(tmp[1]));
                    }
                }
                if (!settings.ContainsKey("id"))
                    return false;
                if (!settings.ContainsKey("num"))
                    return false;
                if (!settings.ContainsKey("minrange"))
                    return false;
                return true;
            }
            return false;
        }

        public void CreateOrUpdateSettingsFile(int num, int minrange, int id)
        {
            using (StreamWriter streamWriter = new StreamWriter(pathToSettings, false))
            {
                streamWriter.WriteLine("id=" + id);
                streamWriter.WriteLine("num=" + num);
                streamWriter.WriteLine("minrange=" + minrange);
            }
        }

    }
}
