using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Reflection;

namespace TabFileTool
{
    class TabFileDataChecker
    {
        private static TabFileDataChecker _instance = null;
        public static TabFileDataChecker Instance
        {
            get
            {
                if (_instance == null)
                    _instance = new TabFileDataChecker();
                return _instance;
            }
        }
        
        private CheckList checkList = new CheckList();
        private Dictionary<string, TabFile> checkDataSource = new Dictionary<string, TabFile>();

        //public void AddToCheck(string filePath)
        //{
        //    FileInfo fileInfo = new FileInfo(filePath);
        //    FileStream fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        //    StreamReader sr = new StreamReader(fs);
        //    string content = sr.ReadToEnd();
        //    sr.Close();
        //    fs.Close();

        //    TabFile tabFile = new TabFile();
        //    string ret = tabFile.Check(content);
        //    if (!string.IsNullOrEmpty(ret))
        //    {
        //        System.Console.WriteLine(fileInfo.Name + "---->表格存在错误，无法检查！错误：" + ret);
        //        return;
        //    }
        //    string fileName = fileInfo.Name.Replace(".txt", "").ToLower();
        //    tabFile.SetColumnHeadParent();
        //    checkDataSource[fileName] = tabFile;
        //}

        public void StartCheckAll()
        {
            string checkListDir = System.Environment.CurrentDirectory + "\\CheckList";
            if (!Directory.Exists(checkListDir))
            {
                System.Console.WriteLine("检查配置目录不存在！" + checkListDir);
                return;
            }
            DirectoryInfo dir = new DirectoryInfo(checkListDir);
            FileInfo[] files = dir.GetFiles("*.txt", SearchOption.AllDirectories);
            for (int i = 0; i < files.Length; i++)
                StartCheck(files[i].FullName);
        }

        public void StartCheck(string filePath)
        {
            FileInfo fileInfo = new FileInfo(filePath);
            int index = fileInfo.Name.IndexOf('_') + 1;
            if (index == -1)
            {
                System.Console.WriteLine("检查配置命名错误，应为checklist_目标表名：" + fileInfo.Name);
                return;
            }
            string curTargetFileName = fileInfo.Name.Substring(index).Replace(".txt", "").ToLower();
            System.Console.WriteLine("检查完成--->" + curTargetFileName);
            FileStream fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            StreamReader sr = new StreamReader(fs);
            string content = sr.ReadToEnd();
            sr.Close();
            fs.Close();

            string[] lines = content.Split('\n');
            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i];
                line = line.Trim('\r');
                string[] commandDesc = line.Split('\t');
                if (commandDesc.Length < 3)
                    continue;
                string methodName = commandDesc[2];
                Type t = checkList.GetType();
                MethodInfo methodInfo = t.GetMethod(methodName);
                if (methodInfo == null)
                {
                    System.Console.WriteLine("检查配置命令不存在：" + methodName);
                    return;
                }

                string key = commandDesc[1];
                List<TabFile.Column> columns = GetTabFileColumn(curTargetFileName, key);
                if (columns == null || columns.Count == 0)
                    return;

                string ret = null;
                for (int j = 0; j < columns.Count; j++)
                {
                    TabFile.Column column = columns[j];
                    List<object> paramList = new List<object>();
                    paramList.Add(column);
                    for (int k = 3; k < commandDesc.Length; k++)
                    {
                        string param = commandDesc[k];
                        if (string.IsNullOrEmpty(param))
                            break;
                        paramList.Add(param);
                    }
                    ret = methodInfo.Invoke(checkList, paramList.ToArray()) as string;
                    if (string.IsNullOrEmpty(ret))
                        continue;
                    System.Console.WriteLine(line + "检查不通过!");
                    System.Console.WriteLine(ret);
                    break;
                }
            }
        }

        public TabFile GetTabFile(string name)
        {
            TabFile ret = null;
            checkDataSource.TryGetValue(name.ToLower(), out ret);
            return ret;
        }

        public List<TabFile.Column> GetTabFileColumn(string name, string key)
        {
            TabFile tabFile = GetTabFile(name);
            if (tabFile == null)
                return null;

            string columnParent = null;
            string columnName = null;
            int index = key.IndexOf('.');
            if (index == -1)
            {
                columnParent = null;
                columnName = key;
            }  
            else
            {
                columnParent = key.Substring(0, index);
                columnName = key.Substring(index + 1);
            }

            List<TabFile.Column> ret = new List<TabFile.Column>();
            for (int i = 0; i < tabFile.columns.Count; i++)
            {
                TabFile.Column column = tabFile.columns[i];
              //  if (column.head.parent == columnParent && column.head.name == columnName)
                if (column.head.name == columnName)
                    ret.Add(column);
            }

            if (ret == null || ret.Count == 0)
                System.Console.WriteLine(key + "标识不存在，请检查checklist_" + name);
            return ret;
        }
    }
}
