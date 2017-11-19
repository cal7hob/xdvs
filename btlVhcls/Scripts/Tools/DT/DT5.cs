using UnityEngine;
using System.Collections;

public class DT5
{
    private const int ID = 5;

    public static void Log(string messageText, params object[] parameters)
    {
        DT.CheckLog(ID, messageText, parameters);
    }

    public static void Log(GameObject context, string messageText, params object[] parameters)
    {
        DT.CheckLog(ID, context, messageText, parameters);
    }

    public static void LogWarning(string messageText, params object[] parameters)
    {
        DT.CheckLogWarning(ID, messageText, parameters);
    }

    public static void LogWarning(GameObject context, string messageText, params object[] parameters)
    {
        DT.CheckLogWarning(ID, context, messageText, parameters);
    }

    public static void LogError(string messageText, params object[] parameters)
    {
        DT.CheckLogError(ID, messageText, parameters);
    }

    public static void LogError(GameObject context, string messageText, params object[] parameters)
    {
        DT.CheckLogError(ID, context, messageText, parameters);
    }
}
