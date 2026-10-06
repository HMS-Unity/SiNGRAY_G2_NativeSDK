

namespace Singray.Foundation
{
    using UnityEngine;
    using System;

    public static class VersionInfo
    {

        public static string version = "4.2.0";

        public static void Print()
        {
            string msg = string.Format("ver_{0}", version);

            Debug.Log("VersionInfo:" + msg);
        }


        private static string getDate()
        {
            string str = DateTime.Now.Year.ToString();
            if (DateTime.Now.Month.ToString().Length == 1)
            {
                str += "0" + DateTime.Now.Month;
            }
            else
            {
                str += DateTime.Now.Month;
            }
            if (DateTime.Now.Day.ToString().Length == 1)
            {
                str += "0" + DateTime.Now.Day;
            }
            else
            {
                str += DateTime.Now.Day;
            }
            if (DateTime.Now.Hour.ToString().Length == 1)
            {
                str += "0" + DateTime.Now.Hour;
            }
            else
            {
                str += DateTime.Now.Hour;
            }
            if (DateTime.Now.Minute.ToString().Length == 1)
            {
                str += "0" + DateTime.Now.Minute;
            }
            else
            {
                str += DateTime.Now.Minute;
            }
            return str;
        }
    }
}