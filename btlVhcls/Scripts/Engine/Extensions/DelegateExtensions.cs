using System;

#if UNITY_WSA && !UNITY_EDITOR
using System.Reflection;
#endif

public static class DelegateExtensions
{
    public static string GetMethodName(this Action action)
    {
        #if UNITY_WSA && !UNITY_EDITOR
        return action.GetMethodInfo().Name;
        #else
        return action.Method.Name;
        #endif
    }
}
