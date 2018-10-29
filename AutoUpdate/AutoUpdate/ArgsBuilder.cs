using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AutoUpdate
{
    public class ArgsBuilder
    {
        StringBuilder _arg = new StringBuilder();
        public ArgsBuilder()
        {
            _arg.Append("");
        }

        /// <summary>
        /// 添加參數
        /// </summary>
        /// <param name="str"></param>
        public void Add(string str)
        {
            if (str.EndsWith("\\"))  //處理最後若為“\\”，會被轉義成“\”，然後變成轉義符。
            {
                str += "\\";
            }
            _arg.AppendFormat("\"{0}\"", str);
            _arg.Append(" "); //參數分割符號
        }
        public override string ToString()
        {
            return _arg.ToString();
        }
    }
}
