using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace TabFileTool
{
    class Config
    {
        public static string SrcTabFilePath = "SrcTabFilePath";
        public static string OutputCSFilePath = "OutputCSFilePath";
        public static string OutputTabFilePath = "OutputTabFilePath";
        public static string OutputLuaFilePath = "OutputLuaFilePath";
        public static string CMDCONFIG = "CMD";
        private const string DataCheckerFilter = "DataCheckerFilter";
        private const string ExportPrefix = "_Export";
        private const string DefaultExport = "Default_Export";
        private const string CSTemplatePrefix = "_CSTemplate";
        private const string DefaultCSTemplate = "Default_CSTemplate";

        public enum ExportType
        {
            Default = 0,
            CSFile,
            DataTabFile,
            Lua,
        }

        public class ExportInfo
        {
            public ExportType type = ExportType.Default;
            public string param = null;
            public string dirName = null;

            public void SetData(string typeStr, string exportDirName, string value)
            {
                switch (typeStr)
                {
                    case "cs":
                        type = ExportType.CSFile;
                        break;
                    case "tab":
                        type = ExportType.DataTabFile;
                        break;
                    case "lua":
                        type = ExportType.Lua;
                        break;
                }
                dirName = exportDirName;
                param = value;
            }
        }

        private static Config _instance = null;
        public static Config Instance
        {
            get
            {
                if (_instance == null)
                    _instance = new Config();
                return _instance;
            }
        }

        private Dictionary<string, string> configMap = null;
        private Dictionary<string, List<ExportInfo>> exportInfoMap = null;
        private Dictionary<string, string> csTemplateFileMap = null;
        private List<string> dataCheckerFilterList = null;

        public void Load()
        {
            configMap = new Dictionary<string, string>();
            exportInfoMap = new Dictionary<string, List<ExportInfo>>();
            csTemplateFileMap = new Dictionary<string, string>();

            string configPath = System.Environment.CurrentDirectory + "\\Config.ini";
            System.Console.WriteLine("工具配置为:\n" + configPath);
            if (!File.Exists(configPath))
            {
                System.Console.WriteLine("工具配置 is null ");
                return;
            }

            string content = File.ReadAllText(configPath, new UTF8Encoding(false));
            string[] lines = content.Split('\n');
            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i];
                line = line.Replace('\r', ' ').Trim();
                if (string.IsNullOrEmpty(line))
                    continue;

                if (line.StartsWith("\\"))
                    continue;

                string[] paramList = line.Split('=');
                if (line.Contains(ExportPrefix))
                {
                    SetExportInfo(paramList);
                    continue;
                }
                
                if (line.Contains(CSTemplatePrefix))
                {
                    SetCSTemplateInfo(paramList);
                    continue;
                }

                if (line.Contains(DataCheckerFilter))
                {
                    SetDataCheckerFilter(paramList[1]);
                    continue;
                }
                configMap[paramList[0]] = paramList[1];
            }
        }

        public string GetParam(string key)
        {
            string ret = "";
            configMap.TryGetValue(key, out ret);
            return ret;
        }

        private void SetExportInfo(string[] paramList)
        {
            string key = paramList[0];
            string valueStr = paramList[1];
            List<ExportInfo> exportInfoList = null;
            exportInfoMap.TryGetValue(key, out exportInfoList);
            if (exportInfoList == null)
            {
                exportInfoList = new List<ExportInfo>();
                exportInfoMap[key] = exportInfoList;
            }
            string[] valueList = valueStr.Split(',');
            for (int i = 0; i < valueList.Length; i++)
            {
                string value = valueList[i];
                ExportInfo info = new ExportInfo();
                if (value.Contains('_'))
                {
                    string[] tmp = value.Split('_');
                    if (tmp.Length == 2)
                        info.SetData(tmp[0], tmp[1], null);
                    else if (tmp.Length == 3)
                        info.SetData(tmp[0], tmp[1], tmp[2]);
                }
                else
                    info.SetData(value, null, null);
                exportInfoList.Add(info);
            }
        }

        public List<ExportInfo> GetExportInfo(string dirName)
        {
            string key = dirName + ExportPrefix;
            List<ExportInfo> exportInfoList = null;
            exportInfoMap.TryGetValue(key, out exportInfoList);
            if (exportInfoList == null)
                exportInfoMap.TryGetValue(DefaultExport, out exportInfoList);
            return exportInfoList;
        }

        private void SetCSTemplateInfo(string[] paramList)
        {
            string configPath = System.Environment.CurrentDirectory + "/" + paramList[1] + ".txt";
            string csTemplate = File.ReadAllText(configPath);
            csTemplateFileMap[paramList[0]] = csTemplate;
        }

        public string GetCSTemplate(string fileName)
        {
            string key = fileName + CSTemplatePrefix;
            string csTemplate = null;
            csTemplateFileMap.TryGetValue(key, out csTemplate);
            if (string.IsNullOrEmpty(csTemplate))
                csTemplateFileMap.TryGetValue(DefaultCSTemplate, out csTemplate);
            return csTemplate;
        }

        private void SetDataCheckerFilter(string filterStr)
        {
            if (dataCheckerFilterList == null)
                dataCheckerFilterList = new List<string>();
            string[] filterArray = filterStr.Split(',');
            for (int i = 0; i < filterArray.Length; i++)
                dataCheckerFilterList.Add(filterArray[i]);
        }

        public bool IsInDataCheckerFilter(string fileName)
        {
            fileName = fileName.Replace(".txt", "");
            return dataCheckerFilterList.IndexOf(fileName) != -1;
        }
    }
}
