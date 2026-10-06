using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using UnityEngine;
using Singray.utils;
using Object = UnityEngine.Object;

public sealed class MyDebugTool
{
    private const string TAG = "SingrayG2_Unity:";
    private const string DiagnosticTag = "[SINGRAY-G2]";
    private static readonly object DiagnosticLock = new object();
    private static readonly Dictionary<string, long> DiagnosticNextLogTicks = new Dictionary<string, long>();
    
    public static bool logEnable = true;
    public static bool diagnosticsEnable = true;

    private static bool ShouldWriteDiagnostic(string category, string key, float minIntervalSeconds)
    {
        if (!logEnable || !diagnosticsEnable)
        {
            return false;
        }

        string diagnosticKey = category + ":" + key;
        long nowTicks = DateTime.UtcNow.Ticks;
        long intervalTicks = (long)(Math.Max(0f, minIntervalSeconds) * TimeSpan.TicksPerSecond);

        lock (DiagnosticLock)
        {
            long nextTicks;
            if (DiagnosticNextLogTicks.TryGetValue(diagnosticKey, out nextTicks) && nowTicks < nextTicks)
            {
                return false;
            }

            DiagnosticNextLogTicks[diagnosticKey] = nowTicks + intervalTicks;
            return true;
        }
    }

    public static void LogDiagnostic(string category, string key, object message, float minIntervalSeconds = 1f)
    {
        if (ShouldWriteDiagnostic(category, key, minIntervalSeconds))
        {
            Debug.Log(TAG + DiagnosticTag + "[" + category + "] " + message);
        }
    }

    public static void LogDiagnosticWarning(string category, string key, object message, float minIntervalSeconds = 1f)
    {
        if (ShouldWriteDiagnostic(category, key, minIntervalSeconds))
        {
            Debug.LogWarning(TAG + DiagnosticTag + "[" + category + "] " + message);
        }
    }

    private static bool TryWriteLegacySensorDiagnostic(object message)
    {
        string text = message == null ? string.Empty : message.ToString();
        string category = null;
        string key = null;

        if (text.IndexOf("xslam_set_bright", StringComparison.Ordinal) >= 0)
        {
            category = "EYE_LED";
            key = text.IndexOf("left", StringComparison.OrdinalIgnoreCase) >= 0
                ? "left"
                : text.IndexOf("right", StringComparison.OrdinalIgnoreCase) >= 0 ? "right" : "all";
        }
        else if (text.StartsWith("Create TOF IR Update", StringComparison.Ordinal))
        {
            category = "TOF";
            key = "legacy-ir-size";
        }
        else if (text.StartsWith("xslam_get_tof_image", StringComparison.Ordinal))
        {
            category = "TOF";
            key = "legacy-frame";
        }
        else if (text.StartsWith("Invalid TOF", StringComparison.Ordinal))
        {
            category = "TOF";
            key = "legacy-no-frame";
        }
        else if (text.Equals("Invalid texture", StringComparison.Ordinal))
        {
            category = "RGB";
            key = "legacy-no-frame";
        }

        if (category == null)
        {
            return false;
        }

        if (ShouldWriteDiagnostic(category, key, 2f))
        {
            Debug.Log(TAG + DiagnosticTag + "[" + category + "] " + text);
        }
        return true;
    }


    public static void Log(object message)
    {
        XvXRLog.LogEnable = logEnable;
        if (!logEnable)
        {
            return;
        }

        if (TryWriteLegacySensorDiagnostic(message))
        {
            return;
        }

        Debug.Log(TAG + message);
    }

    public static void Log(object message, Object context)
    {

        if (!logEnable)
        {
            return;
        }

        Debug.Log(TAG + message, context);
    }

    public static void LogError(object message)
    {
        if (!logEnable)
        {
            return;
        }

        Debug.LogError(TAG + message);


    }

    public static void LogError(object message, Object context)
    {
        if (!logEnable)
        {
            return;
        }

        Debug.LogError(TAG + message, context);
    }

    public static void LogWarning(object message)
    {
        if (!logEnable)
        {
            return;
        }

        Debug.LogWarning(TAG + message);
    }
    public static void LogWarning(object message, Object context)
    {
        if (!logEnable)
        {
            return;
        }

        Debug.LogWarning(TAG + message, context);
    }
}
