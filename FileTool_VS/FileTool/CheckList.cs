using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TabFileTool
{
    class CheckList
    {
        public delegate string OnForeachDelegate(int index, object data);
        private string ForeachColumnData(TabFile.Column column, OnForeachDelegate callback)
        {
            string ret = null;
            switch (column.head.type)
            {
                case TypeDef.IntType:
                    for (int i = 0; i < column.data.Count; i++)
                    {
                        int tempIntData = 0;
                        int.TryParse(column.data[i], out tempIntData);
                        ret = callback.Invoke(i, tempIntData);
                        if (!string.IsNullOrEmpty(ret))
                            return ret;
                    }
                    break;
                case TypeDef.StringType:
                    for (int i = 0; i < column.data.Count; i++)
                    {
                        ret = callback.Invoke(i, column.data[i]);
                        if (!string.IsNullOrEmpty(ret))
                            return ret;
                    }
                    break;
                case TypeDef.ListIntType:
                    for (int i = 0; i < column.data.Count; i++)
                    {
                        string columnData = column.data[i];
                        columnData = columnData.TrimStart('"');
                        columnData = columnData.TrimEnd('"');
                        string[] columnDataList = columnData.Split(',');
                        for (int j = 0; j < columnDataList.Length; j++)
                        {
                            int tempIntData = 0;
                            int.TryParse(columnDataList[j], out tempIntData);
                            ret = callback.Invoke(i, tempIntData);
                            if (!string.IsNullOrEmpty(ret))
                                return ret;
                        }
                    }
                    break;
            }
            return null;
        }

        public string CheckUnique(TabFile.Column column)
        {
            if (column.head.type != TypeDef.IntType)
                return "检查命令不支持数据类型为" + column.head.type;

            List<int> existList = new List<int>();
            OnForeachDelegate checkAction = (dataIndex, data) =>
            {
                int intData = (int)data;
                int index = existList.IndexOf(intData);
                if (index == -1)
                    existList.Add(intData);
                else
                    return column.head.name + "=" + intData;
                return null;
            };
            return ForeachColumnData(column, checkAction);
        }

        public string CheckRange(TabFile.Column column, string min, string max)
        {
            if (column.head.type != TypeDef.IntType && column.head.type != TypeDef.FloatType && column.head.type != TypeDef.DoubleType)
                return "检查命令不支持数据类型为" + column.head.type;

            object objMinRet = null;
            object objMaxRet = null;
            switch (column.head.type)
            {
                case TypeDef.IntType:
                    int intMin = 0;
                    int.TryParse(min, out intMin);
                    objMinRet = intMin;
                    int intMax = 0;
                    int.TryParse(max, out intMax);
                    objMaxRet = intMax;
                    break;
                case TypeDef.FloatType:
                    float floatMin = 0;
                    float.TryParse(min, out floatMin);
                    objMinRet = floatMin;
                    float floatMax = 0;
                    float.TryParse(max, out floatMax);
                    objMaxRet = floatMax;
                    break;
                case TypeDef.DoubleType:
                    double doubleMin = 0;
                    double.TryParse(min, out doubleMin);
                    objMinRet = doubleMin;
                    double doubleMax = 0;
                    double.TryParse(max, out doubleMax);
                    objMaxRet = doubleMax;
                    break;
            }

            OnForeachDelegate checkAction = (dataIndex, data) =>
            {
                switch (column.head.type)
                {
                    case TypeDef.IntType:
                        if ((int)data >= (int)objMinRet && (int)data <= (int)objMaxRet)
                            return null;
                        break;
                    case TypeDef.FloatType:
                        if ((int)data >= (int)objMinRet && (int)data <= (int)objMaxRet)
                            return null;
                        break;
                    case TypeDef.DoubleType:
                        if ((int)data >= (int)objMinRet && (int)data <= (int)objMaxRet)
                            return null;
                        break;
                }
                return column.head.name + "=" + data;
            };
            return ForeachColumnData(column, checkAction);
        }

        public string CheckIDExistAndCanBeZero(TabFile.Column column, string tabFileName, string columnName)
        {
            if (column.head.type != TypeDef.IntType && column.head.type != TypeDef.ListIntType)
                return "检查命令不支持数据类型为" + column.head.type;

            OnForeachDelegate checkAction = (dataIndex, data) =>
            {
                string strData = data.ToString();
                List<TabFile.Column> checkColumns = TabFileDataChecker.Instance.GetTabFileColumn(tabFileName, columnName);
                if (checkColumns == null || checkColumns.Count == 0)
                    return tabFileName + "表不存在列" + columnName + ",无法进行检查";

                for (int j = 0; j < checkColumns.Count; j++)
                {
                    TabFile.Column checkColumn = checkColumns[j];
                    int index = checkColumn.data.IndexOf(strData);
                    if (index == -1 && (int)data != 0)
                        return column.head.name + "=" + strData + "不存在" + tabFileName + "表" + columnName + "列";
                }
                return null;
            };
            return ForeachColumnData(column, checkAction);
        }

        public string CheckIDExistAndCanBeZeroWithRelative(TabFile.Column column, string relative, string columnName)
        {
            if (column.head.type != TypeDef.IntType)
                return "检查命令不支持数据类型为" + column.head.type;

            string[] relativeList = relative.Split('=');
            OnForeachDelegate checkAction = (dataIndex, data) =>
            {
                if ((int)data == 0)
                    return null;

                string relativeTabFileName = FindRelativeColumn(dataIndex, relativeList);
                if (relativeTabFileName == null)
                    return "根据数据：" + data + ";无法找到关联列：" + relative;
                List<TabFile.Column> checkColumns = TabFileDataChecker.Instance.GetTabFileColumn(relativeTabFileName, columnName);
                if (checkColumns.Count != 1)
                    return "根据数据：" + data + ";无法找到关联列：" + relative;
                TabFile.Column relativeColumn = checkColumns[0];
                for (int j = 0; j < relativeColumn.data.Count; j++)
                {
                    int tempIntData = 0;
                    int.TryParse(relativeColumn.data[j], out tempIntData);
                    if (tempIntData == (int)data)
                        return null;
                }
                return "无法在关联列：" + relative + "找到数据:" + data;
            };
            return ForeachColumnData(column, checkAction);
        }

        private string FindRelativeColumn(int index, string[] relativeList)
        {
            string lastTabFileName = null;
            int relativeIndex = index;
            string reletiveValue = null;
            for (int i = 0; i < relativeList.Length; i++)
            {
                string relative = relativeList[i];
                int tmpIndex = relative.IndexOf('.');
                string tabFileName = relative.Substring(0, tmpIndex);
                string columnName = relative.Substring(tmpIndex + 1);

                bool tabFileChange = lastTabFileName != null && tabFileName != lastTabFileName;
                lastTabFileName = tabFileName;
                List<TabFile.Column> checkColumns = TabFileDataChecker.Instance.GetTabFileColumn(tabFileName, columnName);
                if (checkColumns.Count != 1)
                    return null;

                List<string> data = checkColumns[0].data;
                if (!tabFileChange)
                {
                    if (relativeIndex >= data.Count)
                        return null;
                    reletiveValue = data[relativeIndex];
                }
                else
                {
                    for (int j = 0; j < data.Count; j++)
                    {
                        if (data[j] == reletiveValue)
                        {
                            relativeIndex = j;
                            break;
                        }
                    }
                }
            }
            return reletiveValue;
        }
    }
}
