using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TabFileTool
{
    class CSFile
    {
        private const string DATA_INSTANCE_STR = "data";
        private class CSVaribles
        {
            public string type = null;
            public string name = null;
            public CSStruct csStruct = null;
            public CSListStruct csListStruct = null;

            public static bool operator ==(CSVaribles a, CSVaribles b)
            {
                return a.type == b.type && a.name == b.name;
            }

            public static bool operator !=(CSVaribles a, CSVaribles b)
            {
                return a.type != b.type || a.name != b.name;
            }

            public override int GetHashCode()
            {
                return type.GetHashCode() ^ name.GetHashCode();
            }

            public override bool Equals(object obj)
            {
                return this == obj as CSVaribles;
            }

            public string ExportIsNull()
            {
                switch (type)
                {
                    case TypeDef.IntType:
                    case TypeDef.FloatType:
                        return name + " == 0";
                    case TypeDef.StringType:
                    case TypeDef.LuaTableType:
                        return "string.IsNullOrEmpty(" + name + ")";
                    case TypeDef.ListIntType:
                    case TypeDef.ListFloatType:
                    case TypeDef.ListStringType:
                        return "(" + name + "== null || " + name + ".Count == 0)";
                }
                return "";

            }

            public void ExportEvaluate(StringBuilder result, string prefix, string parent)
            {
                switch (type)
                {
                    case TypeDef.IntType:
                        result.Append(string.Format("{0}int.TryParse(paramList[paramIndex++], out {1}.{2});\r\n", prefix, parent, name));
                        break;
                    case TypeDef.BoolType:
                        result.Append(string.Format("{0}{1}.{2} = paramList[paramIndex++] == \"1\";\r\n", prefix, parent, name));
                        break;
                    case TypeDef.FloatType:
                        result.Append(string.Format("{0}float.TryParse(paramList[paramIndex++], out {1}.{2});\r\n", prefix, parent, name));
                        break;
                    case TypeDef.LuaTableType:
                    case TypeDef.StringType:
                        result.Append(string.Format("{0}{1}.{2} = paramList[paramIndex++];\r\n", prefix, parent, name));
                        break;
                    case TypeDef.ListIntType:
                        result.Append(string.Format("{0}string {1}ParamStr = paramList[paramIndex++];\r\n", prefix, name));
                        result.Append(string.Format("{0}if (!string.IsNullOrEmpty({1}ParamStr))\r\n", prefix, name));
                        result.Append(prefix + "{\r\n");
                        result.Append(string.Format("{0}\tstring[] {1}Param = {1}ParamStr.Split(',');\r\n", prefix, name));
                        result.Append(string.Format("{0}\t{1}.{2} = new List<int>({2}Param.Length);\r\n", prefix, parent, name));
                        result.Append(string.Format("{0}\tfor (int k = 0; k < {1}Param.Length; k++)\r\n", prefix, name));
                        result.Append(prefix + "\t{\r\n");
                        result.Append(string.Format("{0}\t\tint {1}Tmp = 0;\r\n", prefix, name));
                        result.Append(string.Format("{0}\t\tint.TryParse({1}Param[k], out {2}Tmp);\r\n", prefix, name, name));
                        result.Append(string.Format("{0}\t\t{1}.{2}.Add({2}Tmp);\r\n", prefix, parent, name));
                        result.Append(prefix + "\t}\r\n");
                        result.Append(prefix + "}\r\n");
                        result.Append(prefix + "else\r\n");
                        result.Append(string.Format("{0}\t{1}.{2} = DefaultValue.ListInt;\r\n", prefix, parent, name));
                        break;
                    case TypeDef.ListFloatType:
                        result.Append(string.Format("{0}string {1}ParamStr = paramList[paramIndex++];\r\n", prefix, name));
                        result.Append(string.Format("{0}if (!string.IsNullOrEmpty({1}ParamStr))\r\n", prefix, name));
                        result.Append(prefix + "{\r\n");
                        result.Append(string.Format("{0}\tstring[] {1}Param = {1}ParamStr.Split(',');\r\n", prefix, name));
                        result.Append(string.Format("{0}\t{1}.{2} = new List<float>({2}Param.Length);\r\n", prefix, parent, name));
                        result.Append(string.Format("{0}\tfor (int k = 0; k < {1}Param.Length; k++)\r\n", prefix, name));
                        result.Append(prefix + "\t{\r\n");
                        result.Append(string.Format("{0}\t\tfloat {1}Tmp = 0;\r\n", prefix, name));
                        result.Append(string.Format("{0}\t\tfloat.TryParse({1}Param[k], out {2}Tmp);\r\n", prefix, name, name));
                        result.Append(string.Format("{0}\t\t{1}.{2}.Add({2}Tmp);\r\n", prefix, parent, name));
                        result.Append(prefix + "\t}\r\n");
                        result.Append(prefix + "}\r\n");
                        result.Append(prefix + "else\r\n");
                        result.Append(string.Format("{0}\t{1}.{2} = DefaultValue.ListFloat;\r\n", prefix, parent, name));
                        break;
                    case TypeDef.ListStringType:
                        result.Append(string.Format("{0}string {1}ParamStr = paramList[paramIndex++];\r\n", prefix, name));
                        result.Append(string.Format("{0}if (!string.IsNullOrEmpty({1}ParamStr))\r\n", prefix, name));
                        result.Append(prefix + "{\r\n");
                        result.Append(string.Format("{0}\tstring[] {1}Param = {1}ParamStr.Split(',');\r\n", prefix, name));
                        result.Append(string.Format("{0}\t{1}.{2} = new List<string>({2}Param.Length);\r\n", prefix, parent, name));
                        result.Append(string.Format("{0}\tfor (int k = 0; k < {1}Param.Length; k++)\r\n", prefix, name));
                        result.Append(string.Format("{0}\t\t{1}.{2}.Add({2}Param[k]);\r\n", prefix, parent, name));
                        result.Append(prefix + "}\r\n");
                        result.Append(prefix + "else\r\n");
                        result.Append(string.Format("{0}\t{1}.{2} = DefaultValue.ListString;\r\n", prefix, parent, name));
                        break;
                    case TypeDef.StructType:
                        csStruct.ExportEvaluate(result, prefix, "data." + csStruct.name);
                        break;
                    case TypeDef.ListType:
                        result.Append(string.Format("{0}{1}.{2} = new List<{3}>();\r\n", prefix, parent, csListStruct.name, csListStruct.csStruct.structName));
                        result.Append(string.Format("{0}for (int j = 0; j < {1}; j++)\r\n", prefix, csListStruct.count));
                        result.Append(prefix + "{\r\n");
                        result.Append(string.Format("{0}\t{1} {2} = new {1}();\r\n", prefix, csListStruct.csStruct.structName, csListStruct.name, parent));
                        csListStruct.csStruct.ExportEvaluate(result, prefix + "\t", csListStruct.name);
                        result.Append(string.Format("{0}\tif (!{1}.isNull)\r\n", prefix, csListStruct.name));
                        result.Append(string.Format("{0}\t\t{1}.{2}.Add({2});\r\n", prefix, parent, csListStruct.name));
                        result.Append(prefix + "}\r\n");
                        break;
                }
            }

            public void ExportCloneEvaluate(StringBuilder result, string prefix, string parent)
            {
                string selfParent = parent.Replace(DATA_INSTANCE_STR, "this");
                switch (type)
                {
                    case TypeDef.IntType:
                    case TypeDef.BoolType:
                    case TypeDef.FloatType:
                    case TypeDef.LuaTableType:
                    case TypeDef.StringType:
                        result.Append(string.Format("{0}{1}.{2} = {3}.{2};\r\n", prefix, parent, name, selfParent));
                        break;
                    case TypeDef.ListIntType:
                        result.Append(string.Format("{0}{1}.{2} = new List<int>({3}.{2}.ToArray());\r\n", prefix, parent, name, selfParent));
                        break;
                    case TypeDef.ListFloatType:
                        result.Append(string.Format("{0}{1}.{2} = new List<float>({3}.{2}.ToArray());\r\n", prefix, parent, name, selfParent));
                        break;
                    case TypeDef.ListStringType:
                        result.Append(string.Format("{0}{1}.{2} = new List<string>({3}.{2}.ToArray());\r\n", prefix, parent, name, selfParent));
                        break;
                    case TypeDef.StructType:
                        csStruct.ExportCloneEvaluate(result, prefix, DATA_INSTANCE_STR + "." + csStruct.name);
                        break;
                    case TypeDef.ListType:
                        result.Append(string.Format("{0}{1}.{2} = new List<{3}>();\r\n", prefix, parent, csListStruct.name, csListStruct.csStruct.structName));
                        result.Append(string.Format("{0}for (int j = 0; j < {1}.{2}.Count; j++)\r\n", prefix, selfParent, csListStruct.name));
                        result.Append(prefix + "{\r\n");
                        result.Append(string.Format("{0}\t{1} {2} = new {1}();\r\n", prefix, csListStruct.csStruct.structName, DATA_INSTANCE_STR + "_" + csListStruct.name, parent));
                        result.Append(string.Format("{0}\t{1} {2} = {3}.{4}[j];\r\n", prefix, csListStruct.csStruct.structName, "this_" + csListStruct.name, selfParent, csListStruct.name));
                        csListStruct.csStruct.ExportCloneEvaluate(result, prefix + "\t", DATA_INSTANCE_STR + "_" + csListStruct.name);
                        result.Append(string.Format("{0}\t{1}.{2}.Add({3});\r\n", prefix, parent, csListStruct.name, DATA_INSTANCE_STR + "_" + csListStruct.name));
                        result.Append(prefix + "}\r\n");
                        break;
                }
            }

            public string ExportDef(string vaibleTemplate)
            {
                int startIndex = vaibleTemplate.IndexOf("\n") + 1;
                int endIndex = vaibleTemplate.IndexOf("\n", startIndex) + 1;
                vaibleTemplate = vaibleTemplate.Substring(startIndex, endIndex - startIndex);
                string ret = null;
                switch (type)
                {
                    case TypeDef.IntType:
                        ret = vaibleTemplate.Replace("$Varible", string.Format("public int {0};", name));
                        break;
                    case TypeDef.BoolType:
                        ret = vaibleTemplate.Replace("$Varible", string.Format("public bool {0};", name));
                        break;
                    case TypeDef.FloatType:
                        ret = vaibleTemplate.Replace("$Varible", string.Format("public float {0};", name));
                        break;
                    case TypeDef.LuaTableType:
                    case TypeDef.StringType:
                        ret = vaibleTemplate.Replace("$Varible", string.Format("public string {0};", name));
                        break;
                    case TypeDef.ListIntType:
                        ret = vaibleTemplate.Replace("$Varible", string.Format("public List<int> {0};", name));
                        break;
                    case TypeDef.ListFloatType:
                        ret = vaibleTemplate.Replace("$Varible", string.Format("public List<float> {0};", name));
                        break;
                    case TypeDef.ListStringType:
                        ret = vaibleTemplate.Replace("$Varible", string.Format("public List<string> {0};", name));
                        break;
                    case TypeDef.StructType:
                        ret = vaibleTemplate.Replace("$Varible", string.Format("public {0} {1};", csStruct.structName, csStruct.name));
                        break;
                    case TypeDef.ListType:
                        ret = vaibleTemplate.Replace("$Varible", string.Format("public List<{0}> {1};", csListStruct.csStruct.structName, csListStruct.name));
                        break;
                }
                return ret;
            }
        }

        private class CSStruct
        {
            public string name = null;
            public string structName
            {
                get { return "st_" + name; }
            }
            public List<CSVaribles> varibles = new List<CSVaribles>();

            public bool NotContains(CSVaribles varible)
            {
                for (int i = 0; i < varibles.Count; i++)
                {
                    if (varibles[i] == varible)
                        return false;
                }
                return true;
            }

            public string ExportDef(string structTemplate, bool needIsNullFun = false)
            {
                int startIndex = structTemplate.IndexOf("\n") + 1;
                int endIndex = structTemplate.IndexOf("}\n", startIndex) + 2;
                structTemplate = structTemplate.Substring(startIndex, endIndex - startIndex);
                structTemplate = structTemplate.Replace("$StructName", structName);

                string structVaribleTemplate = GetStructVaribleTemplate(structTemplate);
                string ret = "";
                for (int i = 0; i < varibles.Count; i++)
                    ret += varibles[i].ExportDef(structVaribleTemplate);

                if (needIsNullFun)
                {
                    startIndex = structVaribleTemplate.IndexOf("\n") + 1;
                    endIndex = structVaribleTemplate.IndexOf("\n", startIndex) + 1;
                    string isNullFunTemplate = structVaribleTemplate.Substring(startIndex, endIndex - startIndex);

                    string isNullRet = "";
                    for (int i = 0; i < varibles.Count; i++)
                    {
                        string descRet = varibles[i].ExportIsNull();
                        bool descRetIsNull = string.IsNullOrEmpty(descRet);
                        if (!descRetIsNull)
                            isNullRet += descRet;
                        if (!descRetIsNull && i != varibles.Count - 1)
                            isNullRet += " && ";
                    }

                    ret += isNullFunTemplate.Replace("$Varible", "public bool isNull{ get { return " + isNullRet + "; } }");
                }

                structTemplate = structTemplate.Replace(structVaribleTemplate, ret);
                return structTemplate;
            }

            public void ExportEvaluate(StringBuilder result, string prefix, string parent)
            {
                for (int i = 0; i < varibles.Count; i++)
                    varibles[i].ExportEvaluate(result, prefix, parent);
            }

            public void ExportCloneEvaluate(StringBuilder result, string prefix, string parent)
            {
                for (int i = 0; i < varibles.Count; i++)
                    varibles[i].ExportCloneEvaluate(result, prefix, parent);
            }
        }

        private class CSListStruct
        {
            public string name = null;
            public CSStruct csStruct = null;
            public int count = 0;
        }

        public string fileName = null;
        private TabFile curTabFile = null;
        private List<CSVaribles> varibles = new List<CSVaribles>();
        private List<CSStruct> structs = new List<CSStruct>();
        private List<CSListStruct> structLists = new List<CSListStruct>();

        public bool UnpackFromTabFile(string fileName, TabFile tabfile)
        {
            curTabFile = tabfile;
            this.fileName = fileName.Substring(0, 1).ToUpper() + fileName.Substring(1);
            CSStruct curStruct = null;
            CSListStruct curListSturct = null;
            for (int i = 0; i < tabfile.columns.Count; i++)
            {
                TabFile.Column column = tabfile.columns[i];
                if (!column.head.clientExport)
                    continue;

                switch (column.head.type)
                {
                    case TypeDef.IntType:
                    case TypeDef.BoolType:
                    case TypeDef.FloatType:
                    case TypeDef.StringType:
                    case TypeDef.ListIntType:
                    case TypeDef.ListFloatType:
                    case TypeDef.ListStringType:
                    case TypeDef.LuaTableType:
                        CSVaribles varible = new CSVaribles();
                        varible.type = column.head.type;
                        varible.name = column.head.name;
                        if (curStruct != null)
                        {
                            if (curStruct.NotContains(varible))
                                curStruct.varibles.Add(varible);
                        }
                        else
                            varibles.Add(varible);
                        break;
                    case TypeDef.StructType:
                        if (!string.IsNullOrEmpty(column.head.name))
                        {
                            if (curStruct != null)
                            {
                                System.Console.WriteLine("struct异常，列：" + (i + 1));
                                return false;
                            }
                            if (curListSturct == null)
                            {
                                curStruct = new CSStruct();
                                curStruct.name = column.head.name;
                                structs.Add(curStruct);
                            }
                            else
                            {
                                if (curListSturct.csStruct == null)
                                {
                                    curStruct = new CSStruct();
                                    curStruct.name = curListSturct.name;
                                    curListSturct.csStruct = curStruct;
                                }
                                curStruct = curListSturct.csStruct;
                                curListSturct.count++;
                            }
                        }
                        else
                        {
                            if (curStruct == null)
                            {
                                System.Console.WriteLine("struct异常，列：" + (i + 1));
                                return false;
                            }

                            if (curListSturct == null)
                            {
                                varible = new CSVaribles();
                                varible.type = column.head.type;
                                varible.csStruct = curStruct;
                                varibles.Add(varible);
                            }
                            curStruct = null;
                        }
                        break;
                    case TypeDef.ListType:
                        if (!string.IsNullOrEmpty(column.head.name))
                        {
                            if (curListSturct != null)
                            {
                                System.Console.WriteLine("list异常，列：" + (i + 1));
                                return false;
                            }
                            curListSturct = new CSListStruct();
                            curListSturct.name = column.head.name;
                            structLists.Add(curListSturct);
                        }
                        else
                        {
                            if (curListSturct == null)
                            {
                                System.Console.WriteLine("struct异常，列：" + (i + 1));
                                return false;
                            }

                            varible = new CSVaribles();
                            varible.type = column.head.type;
                            varible.csListStruct = curListSturct;
                            varibles.Add(varible);
                            curListSturct = null;
                        }
                        break;
                    default:
                        System.Console.WriteLine(i+":不支持的数据类型：" + column.head.type + "," + column.head.name);
                        break;
                }
            }
            return true;
        }

        public string ExportStructCSFile(string csTemplate)
        {
            string ret = csTemplate.Replace("$ClassName", fileName);

            string structTemplate = GetStructTemplate(csTemplate);
            if (structs.Count == 0 && structLists.Count == 0)
                ret = ret.Replace(structTemplate, "");
            else
            {
                string structDefRet = "";
                for (int i = 0; i < structs.Count; i++)
                    structDefRet += structs[i].ExportDef(structTemplate);

                for (int i = 0; i < structLists.Count; i++)
                    structDefRet += structLists[i].csStruct.ExportDef(structTemplate, true);

                ret = ret.Replace(structTemplate, structDefRet);
            }

            string varibleTemplate = GetVaribleTemplate(csTemplate);
            string varibleRet = "";
            StringBuilder evaluate = new StringBuilder();
            StringBuilder cloneEvaluate = new StringBuilder();
            for (int i = 0; i < varibles.Count; i++)
            {
                varibleRet += varibles[i].ExportDef(varibleTemplate);
                varibles[i].ExportEvaluate(evaluate, "\t\t\t\t", DATA_INSTANCE_STR);
                varibles[i].ExportCloneEvaluate(cloneEvaluate, "\t\t\t", DATA_INSTANCE_STR);
            }
                
            ret = ret.Replace(varibleTemplate, varibleRet);
            ret = ret.Replace("$ParamDataSet", evaluate.ToString());
            ret = ret.Replace("$CloneDataSet", cloneEvaluate.ToString());
            return ret;
        }

        private string GetStructTemplate(string csTemplate)
        {
            string prefix = "\t\t$StructDefStart";
            string postfix = "$StructDefEnd";
            int startIndex = csTemplate.IndexOf(prefix);
            int endIndex = csTemplate.IndexOf(postfix) + postfix.Length;
            return csTemplate.Substring(startIndex, endIndex - startIndex);
        }

        private string GetVaribleTemplate(string csTemplate)
        {
            string prefix = "\t\t$VaribleDefStart";
            string postfix = "$VaribleDefEnd";
            int startIndex = csTemplate.IndexOf(prefix);
            int endIndex = csTemplate.IndexOf(postfix) + postfix.Length;
            return csTemplate.Substring(startIndex, endIndex - startIndex);
        }

        private static string GetStructVaribleTemplate(string structTemplate)
        {
            string prefix = "\t\t\t$StructVaribleDefStart";
            string postfix = "$StructVaribleDefEnd";
            int startIndex = structTemplate.IndexOf(prefix);
            int endIndex = structTemplate.IndexOf(postfix) + postfix.Length;
            return structTemplate.Substring(startIndex, endIndex - startIndex);
        }
    }
}
