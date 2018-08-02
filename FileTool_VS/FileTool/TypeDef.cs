using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TabFileTool
{
    public class TypeDef
    {
        public const string IntType = "int";
        public const string BoolType = "bool";
        public const string FloatType = "float";
        public const string DoubleType = "double";
        public const string StringType = "string";
        public const string ListIntType = "list<int>";
        public const string ListFloatType = "list<float>";
        public const string ListStringType = "list<string>";
        public const string StructType = "struct";
        public const string ListType = "list";
        public const string LuaTableType = "luaTable";
        public static string[] TypesList = new string[] { IntType, BoolType, FloatType, StringType, 
            ListIntType, ListFloatType, ListStringType, StructType, ListType, LuaTableType };
    }

    public class ExportTagDef
    {
        public const string CS = "cs";
        public const string C = "c";
        public const string S = "s";
        public const string NO = "no";

        public static bool IsValidTag(string tag)
        {
            return tag == CS || tag == C || tag == S || tag == NO;
        }
    }
}
