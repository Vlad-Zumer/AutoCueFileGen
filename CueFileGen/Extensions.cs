using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace CueFileGen
{

    public static class Extensions
    {
        public static string ArrToString<T>(this T[] arr)
        {
            string ret = "[";
            foreach (var item in arr)
            {
                ret += " " + item.ToString()+",";
            }
            ret = ret.TrimEnd(',');
            ret += " ]";
            return ret;
        }

        public static string ArrToString(this string[] arr)
        {
            string ret = "[";
            foreach (var item in arr)
            {
                ret += " " + Regex.Escape(item.ToString()) + ",";
            }
            ret = ret.TrimEnd(',');
            ret += " ]";
            return ret;
        }
    }
}
