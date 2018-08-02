using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TabFileTool
{
    class TabFile
    {
        public class Head
        {
            public string type = null;
            public string name = null;
            private string _exportTag = null;
            public string exportTag
            {
                get { return _exportTag; }
                set
                {
                    _exportTag = value;
                    switch (_exportTag)
                    {
                        case ExportTagDef.CS:
                            clientExport = true;
                            serverExport = true;
                            break;
                        case ExportTagDef.C:
                            clientExport = true;
                            serverExport = false;
                            break;
                        case ExportTagDef.S:
                            clientExport = false;
                            serverExport = true;
                            break;
                        case ExportTagDef.NO:
                            clientExport = false;
                            serverExport = false;
                            break;
                    }
                }
            }
            public bool clientExport = true;
            public bool serverExport = true;
        }

        public class Column
        {
            public Head head = new Head();
            public List<string> data = null;

            public Column(int dataCount)
            {
                data = new List<string>();
                for (int k = 0; k < dataCount; k++)
                    data.Add("");
            }
        }

        private const int HeadLineCount = 2;
        private List<string> notes = null;
        public List<Column> columns = null;

        public string Check(string content)
        {
            columns = new List<Column>();
            notes = new List<string>();
            int dataStartIndex = -1;

            string[] lines = content.Split('\n');
            List<string[]> validLines = new List<string[]>();
            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i];
                if (string.IsNullOrEmpty(line))
                    continue;

                if (line.StartsWith("//"))
                {
                    notes.Add(line);
                    continue;
                }

                line = line.TrimEnd(new char[]{'\r', ' ', '\t'});
                string ret = line.Trim('\t');
                if (string.IsNullOrEmpty(ret))
                    continue;

                string[] paramList = line.Split('\t');
                validLines.Add(paramList);
                dataStartIndex++;
                if (dataStartIndex != 0)
                    continue;
                string coloumTypeRet = CheckColumType(paramList);
                if (string.IsNullOrEmpty(coloumTypeRet))
                    continue;
                return coloumTypeRet;
            }

            dataStartIndex = -1;
            int dataCount = validLines.Count - HeadLineCount;
            for (int i = 0; i < validLines.Count; i++)
            {
                dataStartIndex++;
                string[] paramList = validLines[i];
                for (int j = 0; j < paramList.Length; j++)
                {
                    Column column = null;
                    if (j >= columns.Count)
                    {
                        column = new Column(dataCount);
                        columns.Add(column);
                    }
                    else
                        column = columns[j];

                    if (dataStartIndex == 0)
                    {
                        column.head.type = paramList[j];
                        continue;
                    }

                    if (dataStartIndex == 1)
                    {
                        string nameStr = paramList[j];
                        int prefixIndex = nameStr.IndexOf("_");
                        string exportTag = prefixIndex != -1 ? nameStr.Substring(0, prefixIndex).ToLower() : null;
                        if (ExportTagDef.IsValidTag(exportTag))
                        {
                            column.head.exportTag = exportTag;
                            column.head.name = nameStr.Substring(prefixIndex + 1);
                        }
                        else
                        {
                            column.head.exportTag = ExportTagDef.CS;
                            column.head.name = nameStr;
                        }
                        continue;
                    }
                    column.data[i - HeadLineCount] = paramList[j];
                }
            }

            string headNameRet = CheckColumHeadName();
            if (!string.IsNullOrEmpty(headNameRet))
                return headNameRet;
            return CheckColumData();
        }

        private string CheckColumType(string[] typeList)
        {
            Stack<string> typeStack = new Stack<string>();
            for (int i = 0; i < typeList.Length; i++)
            {
                string type = typeList[i];
                bool typeIsValid = false;
                for (int j = 0; j < TypeDef.TypesList.Length; j++)
                {
                    if (TypeDef.TypesList[j] == type)
                    {
                        typeIsValid = true;
                        break;
                    }
                }
                if (!typeIsValid)
                {
                    if (string.IsNullOrEmpty(type))
                        return "列" + i + "为空列，应删除";
                    else
                        return "列" + i + "的类型不对，当前类型为:" + type;
                }


                if (type != TypeDef.ListType && type != TypeDef.StructType)
                    continue;

                if (typeStack.Count != 0)
                {
                    if (typeStack.Peek() == type)
                        typeStack.Pop();
                    else
                        typeStack.Push(type);
                }
                else
                    typeStack.Push(type);
            }
            if (typeStack.Count != 0)
                return "List 或者 struct 标签没有成对出现！！";
            return null;
        }

        private string CheckColumHeadName()
        {
            bool isInlist = false;
            bool isInStruct = false;
            bool hasSetFirstStruct = false;
            int structIndex = -1;
            List<string> listStructHeadName = new List<string>();
            int lineOffset = HeadLineCount + notes.Count + 1;
            for (int i = 0; i < columns.Count; i++)
            {
                Column column = columns[i];
                if (i == 0 && column.head.name != "id" && column.head.type != TypeDef.IntType)
                    return "首列类型必须为int，名字必须为id，作为索引键值";

                if (column.head.type == TypeDef.ListType)
                {
                    if (!isInlist)
                    {
                        int nextColumIndex = i + 1;
                        bool isOk = false;
                        if (nextColumIndex < columns.Count)
                        {
                            Column nextColumn = columns[nextColumIndex];
                            if (nextColumn.head.type == TypeDef.StructType)
                                isOk = true;
                        }
                        if (!isOk)
                            return "列:" + i + ";List开始标签列后没有紧跟Struct标签列!";
                        hasSetFirstStruct = false;
                        listStructHeadName = new List<string>();
                    }
                    isInlist = !isInlist;
                    continue;
                }

                if (isInlist && column.head.type == TypeDef.StructType)
                {
                    if (isInStruct)
                        hasSetFirstStruct = true;
                    else
                        structIndex = 0;

                    isInStruct = !isInStruct;
                    continue;
                }

                if (isInlist && isInStruct)
                {
                    if (!hasSetFirstStruct)
                    {
                        if (column.head.exportTag == ExportTagDef.CS)
                            listStructHeadName.Add(column.head.name);
                        else
                            listStructHeadName.Add(column.head.exportTag + "_" + column.head.name);
                    } 
                    else
                    {
                        string targetHeadname = listStructHeadName[structIndex++];
                        string curHeadName = column.head.exportTag != ExportTagDef.CS ? column.head.exportTag + "_" + column.head.name : column.head.name;
                        if (targetHeadname != curHeadName)
                            return "列:" + i + "List中struct结构列表不匹配，现值为：" + curHeadName + "应为：" + targetHeadname;
                    }
                }
            }
            return null;
        }

        private string CheckColumData()
        {
            int lineOffset = HeadLineCount + notes.Count + 1;
            string listExportTag = null;
            string structExportTag = null;
            for (int i = 0; i < columns.Count; i++)
            {
                Column column = columns[i];
                List<string> existIDList = null;
                bool checkIDExist = i == 0;
                if (checkIDExist)
                    existIDList = new List<string>();

                if (column.head.type == TypeDef.ListType)
                {
                    if (string.IsNullOrEmpty(listExportTag) && column.head.exportTag != ExportTagDef.CS)
                        listExportTag = column.head.exportTag;
                    else
                    {
                        column.head.exportTag = listExportTag;
                        listExportTag = null;
                    }
                }
                else if (column.head.type == TypeDef.StructType && string.IsNullOrEmpty(listExportTag))
                {
                    if (string.IsNullOrEmpty(structExportTag) && column.head.exportTag != ExportTagDef.CS)
                        structExportTag = column.head.exportTag;
                    else
                    {
                        column.head.exportTag = structExportTag;
                        structExportTag = null;
                    }
                }
                else 
                {
                    if (!string.IsNullOrEmpty(listExportTag))
                        column.head.exportTag = listExportTag;

                    if (!string.IsNullOrEmpty(structExportTag))
                        column.head.exportTag = structExportTag;
                }

                for (int j = 0; j < column.data.Count; j++)
                {
                    string data = column.data[j];
                    if (string.IsNullOrEmpty(data))
                        continue;

                    if (checkIDExist)
                    {
                        int index = existIDList.IndexOf(data);
                        if (index != -1)
                            return "行:" + (j + lineOffset) + "列:" + i + ";ID重复，值为:" + data;
                        existIDList.Add(data);
                    }

                    switch (column.head.type)
                    {
                        case TypeDef.IntType:
                            int intData = 0;
                            int.TryParse(data, out intData);
                            if (intData.ToString() != data.Trim())
                                return "行:" + (j + lineOffset) + "列:" + i + ";int数值填写不对，原值为:" + data + "导出为:" + intData;
                            break;
                        case TypeDef.FloatType:
                            float floatData = 0;
                            bool canParseFloat = float.TryParse(data, out floatData);
                            if (!canParseFloat)
                                return "行:" + (j + lineOffset) + "列:" + i + ";float数值填写不对，原值为:" + data + "导出为:" + floatData;
                            break;
                        case TypeDef.ListIntType:
                            string listIntData = data.TrimStart('"');
                            listIntData = listIntData.TrimEnd('"');
                            string[] listIntDataList = listIntData.Split(',');
                            bool invalid = false;
                            for (int k = 0; k < listIntDataList.Length; k++)
                            {
                                string tempListIntData = listIntDataList[k];
                                int tempData = 0;
                                int.TryParse(tempListIntData, out tempData);
                                if (tempListIntData.Trim() != tempData.ToString())
                                {
                                    invalid = true;
                                    break;
                                }
                            }
                            if (invalid)
                                return "行:" + (j + lineOffset) + "列:" + i + ";list<int>数值填写不对，原值为:" + data;
                            break;
                        case TypeDef.ListFloatType:
                            string listFloatData = data.TrimStart('"');
                            listFloatData = listFloatData.TrimEnd('"');
                            string[] listFloatDataList = listFloatData.Split(',');
                            invalid = false;
                            for (int k = 0; k < listFloatDataList.Length; k++)
                            {
                                string tempListFloatData = listFloatDataList[k];
                                float tempData = 0;
                                canParseFloat = float.TryParse(tempListFloatData.Trim(), out tempData);
                                if (!canParseFloat)
                                {
                                    invalid = true;
                                    break;
                                }
                            }
                            if (invalid)
                                return "行:" + (j + lineOffset) + "列:" + i + ";list<float>数值填写不对，原值为:" + data;
                            break;
                        case TypeDef.LuaTableType:
                            Stack<char> luaTableStack = new Stack<char>();
                            char[] charList = data.ToCharArray();
//                             if (charList[0] != '{' && charList[charList.Length - 1] != '}')
//                                 return "行:" + (j + lineOffset) + "列:" + i + ";LuaTable填写不对，开始不为‘｛’或者结束不为‘｝’,原值为:" + data;
                            for (int k = 0; k < charList.Length; k++)
                            {
                                char charData = charList[k];
                                if (charData != '{' && charData != '}')
                                    continue;
                                if (charData == '{')
                                    luaTableStack.Push(charData);
                                else
                                {
                                    if (luaTableStack.Count <= 0)
                                        return "行:" + (j + lineOffset) + "列:" + i + ";LuaTable填写不对，缺少},原值为:" + data;
                                    luaTableStack.Pop();
                                }

                            }
                            if (luaTableStack.Count != 0)
                                return "行:" + (j + lineOffset) + "列:" + i + ";LuaTable填写不对， {,}符号不对等，原值为:" + data;
                            break;
                    }
                }
            }
            return null;
        }

        public string ExportDataTabFile()
        {
            StringBuilder result = new StringBuilder();
            int lineCount = 0;
            Column firstColumn = columns[0];
            if (firstColumn.data != null)
                lineCount += firstColumn.data.Count;

            for (int i = 0; i < lineCount; i++)
            {
                for (int j = 0; j < columns.Count; j++)
                {
                    Column column = columns[j];
                    if (!column.head.clientExport)
                        continue;

                    if (column.head.type == TypeDef.StructType || column.head.type == TypeDef.ListType)
                        continue;

                    string appendData = column.data[i];
                    if (column.head.type == TypeDef.ListIntType || column.head.type == TypeDef.ListFloatType ||
                        column.head.type == TypeDef.ListStringType || column.head.type == TypeDef.StringType)
                    {
                        appendData = appendData.TrimStart('"');
                        appendData = appendData.TrimEnd('"');
                        if (appendData == "0")//防止策划手贱
                            appendData = "";
                    }
                    else if (column.head.type == TypeDef.BoolType)
                    {
                        if (!string.IsNullOrEmpty(appendData) && appendData != "0" && appendData.ToLower() != "false")
                            appendData = "1";
                        else
                            appendData = "0";
                    }
                    if (j != 0)
                        result.Append("\t");
                    result.Append(appendData);
                }
                if (i < lineCount - 1)
                    result.Append("\n");
            }
            return result.ToString();
        }
    }
}
