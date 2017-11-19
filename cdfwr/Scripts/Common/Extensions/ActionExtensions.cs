using System;

public static class ActionExtensions
{
    public static void SafeInvoke(this Action action)
    {
        if (action != null)
        {
            action();
        }
    }

    public static void SafeInvoke<T>(this Action<T> action, T parameter)
    {
        if (action != null)
        {
            action(parameter);
        }
    }

    public static void SafeInvoke<T1, T2>(this Action<T1, T2> action, T1 parameter, T2 secondParameter)
    {
        if (action != null)
        {
            action(parameter, secondParameter);
        }
    }
}
