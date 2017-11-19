using UnityEngine;
using System.Collections;
using System.Threading;

public class DT
{
    public static bool[] debugsEnabled =
    {
        true,   // Filtering status
        
        false,   // DT1
        false,  // DT2
        true,  // DT3
        false,  // DT4
        false,  // DT5
        false   // DT6
    };
    
    
    protected virtual int DebugId { get; set; }
    
    public static bool useDebugPanelForLogging = false;
    public static void Log(string messageText, params object[] parameters)
    {
        if (useDebugPanelForLogging)
            DebugPanel.Log (string.Format(messageText, parameters));
        else
            Debug.Log(string.Format(messageText, parameters));
    }

    public static void Log(GameObject context, string messageText, params object[] parameters)
    {
        if (useDebugPanelForLogging)
            DebugPanel.Log (string.Format(messageText.ToString(), parameters));
        else
            Debug.Log(string.Format(messageText, parameters), context);
    }

    public static void LogWarning(string messageText, params object[] parameters)
    {
        if (useDebugPanelForLogging)
            DebugPanel.Log (string.Format(messageText.ToString(), parameters));
        else
            Debug.LogWarning(string.Format(messageText, parameters));
    }

    public static void LogWarning(GameObject context, string messageText, params object[] parameters)
    {
        if (useDebugPanelForLogging)
            DebugPanel.Log (string.Format(messageText.ToString(), parameters));
        else
            Debug.LogWarning(string.Format(messageText, parameters), context);
    }

    public static void LogError(string messageText, params object[] parameters)
    {
        if (useDebugPanelForLogging)
            DebugPanel.Log (string.Format(messageText.ToString(), parameters));
        else
            Debug.LogError(string.Format(messageText, parameters));
    }

    public static void LogError(GameObject context, string messageText, params object[] parameters)
    {
        if (useDebugPanelForLogging)
            DebugPanel.Log(string.Format(messageText, parameters));
        else
            Debug.LogError(string.Format(messageText, parameters), context);
    }

    public static void CheckLog(int id, string messageText, params object[] parameters)
    {
        if (!debugsEnabled[0] || debugsEnabled[id])
            Log(messageText, parameters);
    }

    public static void CheckLog(int id, GameObject context, string messageText, params object[] parameters)
    {
        if (!debugsEnabled[0] || debugsEnabled[id])
            Log(context, messageText, parameters);
    }

    public static void CheckLogWarning(int id, string messageText, params object[] parameters)
    {
        if (!debugsEnabled[0] || debugsEnabled[id])
            LogWarning(messageText, parameters);
    }

    public static void CheckLogWarning(int id, GameObject context, string messageText, params object[] parameters)
    {
        if (!debugsEnabled[0] || debugsEnabled[id])
            Log(context, messageText, parameters);
    }

    public static void CheckLogError(int id, string messageText, params object[] parameters)
    {
        if (!debugsEnabled[0] || debugsEnabled[id])
            LogError(messageText, parameters);
    }

    public static void CheckLogError(int id, GameObject context, string messageText, params object[] parameters)
    {
        if (!debugsEnabled[0] || debugsEnabled[id])
            LogError(context, messageText, parameters);
    }
}
