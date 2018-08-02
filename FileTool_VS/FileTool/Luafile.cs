using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TabFileTool
{
    class LuaFile
    {
        private class LuaStruct
        {
            public bool hasData = false;
            public StringBuilder str = new StringBuilder();
        }
        private List<StringBuilder> lines = new List<StringBuilder>();
        public bool UnpackFromTabFile(TabFile tabfile, bool forceAll = false)
        {
            List<LuaStruct> structs = new List<LuaStruct>();
            for (int i = 0; i < tabfile.columns[0].data.Count; i++)
            {
                StringBuilder line = new StringBuilder();
                lines.Add(line);
                LuaStruct structTmp = new LuaStruct();
                structs.Add(structTmp);
            }
            bool isInList = false;
            for (int i = 0; i < tabfile.columns.Count; i++)
            {
                TabFile.Column column = tabfile.columns[i];
                for (int j = 0; j < column.data.Count; j++)
                {
                    StringBuilder line = lines[j];
                    LuaStruct structTmp = structs[j];
                    string data = column.data[j];
                    if (i == 0)
                        line.Append("[" + data + "] = {");

                    if (column.head.serverExport || forceAll)
                    {
                        switch (column.head.type)
                        {
                            case TypeDef.IntType:
                                int intData = 0;
                                int.TryParse(data, out intData);
                                if (!isInList)
                                    line.Append(column.head.name + "=" + intData + ",");
                                else
                                {
                                    if (intData != 0)
                                        structTmp.hasData = true;
                                    structTmp.str.Append(column.head.name + "=" + intData + ",");
                                }
                                break;
                            case TypeDef.BoolType:
                                string str = data.ToLower();
                                bool boolData = false;
                                if (!string.IsNullOrEmpty(str) && str != "0" && str != "false")
                                    boolData = true;
                                string boolStr = boolData.ToString().ToLower();
                                if (!isInList)
                                    line.Append(column.head.name + "=" + boolStr + ",");
                                else
                                {
                                    if (boolData)
                                        structTmp.hasData = true;
                                    structTmp.str.Append(column.head.name + "=" + boolStr + ",");
                                }
                                break;
                            case TypeDef.FloatType:
                                float floatData = 0;
                                float.TryParse(data, out floatData);
                                string floatStr = string.Format("{0}", floatData);
                                if (!isInList)
                                    line.Append(column.head.name + "=" + floatStr + ",");
                                else
                                {
                                    if (floatData != 0)
                                        structTmp.hasData = true;
                                    structTmp.str.Append(column.head.name + "=" + floatStr + ",");
                                }
                                break;
                            case TypeDef.StringType:
                                if (!string.IsNullOrEmpty(data))
                                {
                                    string strData = data.TrimStart('"');
                                    strData = strData.TrimEnd('"');
                                    strData = strData.Replace("\\", "\\\\");
                                    strData = strData.Replace("\"", "\\\"");
                                    if (!isInList)
                                        line.Append(column.head.name + "=\"" + strData + "\",");
                                    else
                                    {
                                        structTmp.hasData = true;
                                        structTmp.str.Append(column.head.name + "=\"" + strData + "\",");
                                    }
                                }
                                break;
                            case TypeDef.ListIntType:
                            case TypeDef.ListFloatType:
                                string strData2 = data.TrimStart('"');
                                strData2 = strData2.TrimEnd('"');
                                if (!isInList)
                                    line.Append(column.head.name + "={" + strData2 + "},");
                                else
                                {
                                    if (!string.IsNullOrEmpty(data))
                                    {
                                        structTmp.hasData = true;
                                        structTmp.str.Append(column.head.name + "={" + strData2 + "},");
                                    }
                                }
                                break;
                            case TypeDef.ListStringType:
                                if (!string.IsNullOrEmpty(data))
                                {
                                    string strData = data.TrimStart('"');
                                    strData = strData.TrimEnd('"');
                                    string[] param = strData.Split(',');
                                    string strDataResult = "";
                                    for (int k = 0; k < param.Length; k++)
                                    {
                                        strDataResult += "\"" + param[k] + "\"";
                                        if (k != param.Length - 1)
                                            strDataResult += ",";
                                    }
                                    if (!isInList)
                                        line.Append(column.head.name + "={" + strDataResult + "},");
                                    else
                                    {
                                        structTmp.hasData = true;
                                        structTmp.str.Append(column.head.name + "={" + strDataResult + "},");
                                    }
                                }
                                break;
                            case TypeDef.StructType:
                                if (!string.IsNullOrEmpty(column.head.name))
                                {
                                    structTmp.str = new StringBuilder();
                                    structTmp.hasData = false;
                                    if (isInList)
                                        structTmp.str.Append("{");
                                    else
                                        line.Append(column.head.name + "={");
                                }
                                else
                                {
                                    if (isInList)
                                    {
                                        structTmp.str.Replace(",", "", structTmp.str.Length - 1, 1);
                                        structTmp.str.Append("},");
                                        if (structTmp.hasData)
                                            line.Append(structTmp.str.ToString());
                                    }
                                    else
                                    {
                                        line.Replace(",", "", line.Length - 1, 1);
                                        line.Append("},");
                                    }
                                }
                                break;
                            case TypeDef.ListType:
                                if (!string.IsNullOrEmpty(column.head.name))
                                {
                                    line.Append(column.head.name + "={");
                                    isInList = true;
                                }
                                else
                                {
                                    line.Replace(",", "", line.Length - 1, 1);
                                    line.Append("},");
                                    isInList = false;
                                }
                                break;
                            case TypeDef.LuaTableType:
                                if (!string.IsNullOrEmpty(data))
                                {
                                    string strData = data.TrimStart('"');
                                    strData = strData.TrimEnd('"');
                                    if (!isInList)
                                        line.Append(column.head.name + "=" + strData + ",");
                                    else
                                    {
                                        structTmp.hasData = true;
                                        structTmp.str.Append(column.head.name + "=" + strData + ",");
                                    }
                                }
                                break;
                        }
                    }

                    if (i == tabfile.columns.Count - 1)
                    {
                        line.Replace(",", "", line.Length - 1, 1);
                        line.Append("},");
                    }
                }
            }
            return true;
        }

        public string ExportDataLuaFile(string fileName, string moduleName = "Data")
        {
            StringBuilder result = new StringBuilder();
            result.Append("module(\"" + moduleName + "\")\n");
            result.Append(fileName + " = {\n");
            for (int i = 0; i < lines.Count; i++)
                result.Append(lines[i] + "\n");
            result.Append("}\n");
            return result.ToString();
        }
    }
}
