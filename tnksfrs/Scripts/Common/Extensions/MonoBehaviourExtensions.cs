using System;
using UnityEngine;

public static class MonoBehaviourExtensions
{
    public static void Invoke(this MonoBehaviour source, Action action, float time)
    {
        source.Invoke(action.GetMethodName(), time);
    }

    public static void InvokeRepeating(this MonoBehaviour source, Action action, float time, float repeatRate)
    {
        source.InvokeRepeating(action.GetMethodName(), time, repeatRate);
    }

    public static void CancelInvoke(this MonoBehaviour source, Action action)
    {
        source.CancelInvoke(action.GetMethodName());
    }

    public static bool IsInvoking(this MonoBehaviour source, Action action)
    {
        return source.IsInvoking(action.GetMethodName());
    }
}
