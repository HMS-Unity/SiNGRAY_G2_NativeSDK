using UnityEngine;
using System.Collections;

namespace Singray.utils
{
    public class XvXRLog
    {

        public static bool LogEnable { get; set; }

        public static void LogError(object message)
        {
            if (LogEnable)
            {
                MyDebugTool.LogError("vr_log:unity:LogError:" + message);
            }
        }


        public static void LogInfo(object message)
        {
            if (LogEnable)
            {
                MyDebugTool.Log("vr_log:unity:LogInfo:" + message);
            }
        }


        private static bool internalLog = true;
        internal static void InternalXvXRLog(object message)
        {

            if (internalLog)
            {
                LogInfo("internal:" + message);
            }
        }
        internal static void InternalXvXRLogError(object message)
        {

            if (internalLog)
            {
                LogError("internal" + message);
            }
        }


        public static void LogException(System.Exception e)
        {
            if (LogEnable)
            {
                MyDebugTool.LogError(e);
            }
        }
    }
}

