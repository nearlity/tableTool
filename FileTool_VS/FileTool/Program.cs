using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Forms;
using LuaInterface;

namespace TabFileTool
{
    class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            Console.Title = "TabFile导出工具";
            Config.Instance.Load();

            var cmd = Config.Instance.GetParam(Config.CMDCONFIG);
            System.Console.WriteLine("CMD:" + cmd);
            if (string.IsNullOrEmpty(cmd) && args.Length == 0)
            {
                SelectAndExport();
                return;
            }

            switch (cmd)
            {
                case "batch":
                    BatchExport();
                    break;
                case "check":
                    CheckAllTabFileDataWithLua();
                    break;
            }
            System.Console.WriteLine("按任意键...");
            System.Console.ReadLine();
        }

        static void CheckAllTabFileDataWithLua()
        {
            Lua luaVm = new Lua();

            DirectoryInfo dir = new DirectoryInfo(Config.Instance.GetParam(Config.SrcTabFilePath));
            FileInfo[] files = dir.GetFiles("*.txt", SearchOption.AllDirectories);
            int filesCount = files.Length;
            for (int i = 0; i < filesCount; i++)
            {
                FileInfo fileInfo = files[i];
                bool isInFilter = Config.Instance.IsInDataCheckerFilter(fileInfo.Name);
                if (isInFilter)
                    continue;

                System.Console.WriteLine("(" + i + "/" + filesCount + ")加载原始表格---->" + fileInfo.Name);
                FileStream fs = new FileStream(fileInfo.FullName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                StreamReader sr = new StreamReader(fs);
                string content = sr.ReadToEnd();
                sr.Close();
                fs.Close();

                TabFile tabFile = new TabFile();
                string ret = tabFile.Check(content);
                if (!string.IsNullOrEmpty(ret))
                {
                    System.Console.WriteLine(fileInfo.Name + "---->表格存在错误，无法检查！错误：" + ret);
                    return;
                }

                LuaFile luaFile = new LuaFile();
                luaFile.UnpackFromTabFile(tabFile, true);
                string fileName = fileInfo.Name.Replace(".txt", "");
                string exportContent = luaFile.ExportDataLuaFile(fileName, "Data");
                UTF8Encoding utf8Encoding = new UTF8Encoding();
                string utf8ExportContent = utf8Encoding.GetString(Encoding.ASCII.GetBytes(exportContent));
                luaVm.DoString(utf8ExportContent);
            }
            System.Console.WriteLine("加载数据完成");
            luaVm.DoFile(System.Environment.CurrentDirectory + "\\CheckList\\checklist.lua");
        }

        static void BatchExport()
        {
            ClearDirectory(Config.Instance.GetParam(Config.OutputTabFilePath), "*.txt");
            ClearDirectory(Config.Instance.GetParam(Config.OutputCSFilePath), "*.cs");
            ClearDirectory(Config.Instance.GetParam(Config.OutputLuaFilePath), "*.lua");
            DirectoryInfo dir = new DirectoryInfo(Config.Instance.GetParam(Config.SrcTabFilePath));
            FileInfo[] files = dir.GetFiles("*.txt", SearchOption.AllDirectories);
            for (int i = 0; i < files.Length; i++)
                SingleExport(files[i].FullName);
        }

        static void ClearDirectory(string dirPath, string extention)
        {
            DirectoryInfo dir = new DirectoryInfo(dirPath);
            FileInfo[] fileInfos = dir.GetFiles(extention, SearchOption.TopDirectoryOnly);
            for (int i = 0; i < fileInfos.Length; i++)
                File.Delete(fileInfos[i].ToString());
        }

        static void SelectAndExport()
        {
            while (true)
            {
                OpenFileDialog dialog = new OpenFileDialog();
                dialog.Multiselect = true;
                dialog.Title = "选择你要导出的Tab表";
                dialog.DefaultExt = "*.txt";
                dialog.InitialDirectory = Config.Instance.GetParam(Config.SrcTabFilePath);
                DialogResult dialogRet = dialog.ShowDialog();
                if (dialogRet == DialogResult.OK)
                {
                    string[] filePaths = dialog.FileNames;
                    for (int i = 0; i < filePaths.Length; i++)
                    {
                        string filePath = filePaths[i];
                        if (filePath.Contains(".meta"))
                            continue;
                        SingleExport(filePaths[i]);
                    }

                }

                System.Console.WriteLine("按任意键选择文件...");
                System.Console.ReadLine();
            }
        }

        static void SingleExport(string filePath)
        {
            FileInfo fileInfo = new FileInfo(filePath);
            FileStream fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            byte[] temp = new byte[fs.Length];
            fs.Read(temp, 0, (int)fs.Length);

            StreamReader sr = new StreamReader(fs);
            string content = GetText(temp);
            sr.Close();
            fs.Close();
            TabFile tabFile = new TabFile();
            string ret = tabFile.Check(content);
            if (!string.IsNullOrEmpty(ret))
            {
                System.Console.WriteLine(fileInfo.Name + "---->导表失败！错误：" + ret);
                return;
            }

            List<Config.ExportInfo> exportInfoList = Config.Instance.GetExportInfo(fileInfo.Directory.Name);
            for (int i = 0; i < exportInfoList.Count; i++)
            {
                Config.ExportInfo info = exportInfoList[i];
                string fileName = fileInfo.Name.Replace(".txt", "");
                string dirName = "";
                if (!string.IsNullOrEmpty(info.dirName))
                    dirName = "/" + info.dirName;

                string exportContent = null;
                switch (info.type)
                {
                    case Config.ExportType.CSFile:
                        if (!string.IsNullOrEmpty(info.param))
                            fileName = info.param;
                        CSFile csFile = new CSFile();
                        csFile.UnpackFromTabFile(fileName, tabFile);
                        string csTemplate = Config.Instance.GetCSTemplate(csFile.fileName);
                        exportContent = csFile.ExportStructCSFile(csTemplate);
                        string csFilePath = Config.Instance.GetParam(Config.OutputCSFilePath) + dirName + "/" + csFile.fileName + ".cs";
                        SaveFile(csFilePath, exportContent, Encoding.UTF8);
                        break;
                    case Config.ExportType.DataTabFile:
                        exportContent = tabFile.ExportDataTabFile();
                        string tabFilePath = Config.Instance.GetParam(Config.OutputTabFilePath) + dirName + "/" + fileName + ".txt";
                        SaveFile(tabFilePath, exportContent, Encoding.UTF8);
                        break;
                    case Config.ExportType.Lua:
                        string moduleName = null;
                        if (!string.IsNullOrEmpty(info.param))
                            moduleName = info.param;
                        LuaFile luaFile = new LuaFile();
                        luaFile.UnpackFromTabFile(tabFile);
                        exportContent = luaFile.ExportDataLuaFile(fileName, moduleName);
                        string luaFilePath = Config.Instance.GetParam(Config.OutputLuaFilePath) + dirName + "/" + fileName + ".lua";
                        SaveFile(luaFilePath, exportContent, Encoding.UTF8);
                        break;
                }
            }
            System.Console.WriteLine(fileInfo.Name + "---->导表完成!");
        }
        private static bool isUtf8(byte[] buff)
        {
            for (int i = 0; i < buff.Length; i++)
            {
                if ((buff[i] & 0xE0) == 0xC0) // 110x xxxx 10xx xxxx
                {
                    if ((buff[i + 1] & 0x80) != 0x80)
                    {
                        return false;
                    }
                }
                else if ((buff[i] & 0xF0) == 0xE0) // 1110 xxxx 10xx xxxx 10xx xxxx
                {
                    if ((buff[i + 1] & 0x80) != 0x80 || (buff[i + 2] & 0x80) != 0x80)
                    {
                        return false;
                    }
                }
                else if ((buff[i] & 0xF8) == 0xF0) // 1111 0xxx 10xx xxxx 10xx xxxx 10xx xxxx
                {
                    if ((buff[i + 1] & 0x80) != 0x80 || (buff[i + 2] & 0x80) != 0x80 || (buff[i + 3] & 0x80) != 0x80)
                    {
                        return false;
                    }
                }
            }
            return true;
        }
        // news.sohu.com
        private static bool isGBK(byte[] buff)
        {
            return false;
        }
        public static string GetText(byte[] buff)
        {
            string strReslut = string.Empty;
            if (buff.Length > 3)
            {
                if (buff[0] == 239 && buff[1] == 187 && buff[2] == 191)
                {// utf-8
                    strReslut = Encoding.UTF8.GetString(buff);
                }
                else if (buff[0] == 254 && buff[1] == 255)
                {// big endian unicode
                    strReslut = Encoding.BigEndianUnicode.GetString(buff);
                }
                else if (buff[0] == 255 && buff[1] == 254)
                {// unicode
                    strReslut = Encoding.Unicode.GetString(buff);
                }
                else if (isUtf8(buff))
                {// utf-8
                    strReslut = Encoding.UTF8.GetString(buff);
                }
                else
                {// ansi
                    strReslut = Encoding.Default.GetString(buff);
                }
            }
            return strReslut;
        }
        public static string get_uft8(string unicodeString)
        {
            UTF8Encoding utf8 = new UTF8Encoding();
            Byte[] encodedBytes = utf8.GetBytes(unicodeString);
            String decodedString = utf8.GetString(encodedBytes);
            return decodedString;
        }
        static void SaveFile(string filePath, string content, Encoding encode)
        {
            string dirPath = null;
            int index = filePath.LastIndexOf("/");
            if (index >= 0)
                dirPath = filePath.Substring(0, index);
            if (!Directory.Exists(dirPath))
                Directory.CreateDirectory(dirPath);

            FileStream fs = new FileStream(filePath, FileMode.Create);
            StreamWriter sw = new StreamWriter(fs, encode);
            sw.Write(content);
            sw.Close();
            fs.Close();
        }
    }
}
