using System;
using UnityEngine;

public class GoogleAnalyticsWrapper
{
    public static void LogEvent(CustomEventHitBuilder customEventHitBuilder)
    {
        if (Debug.isDebugBuild)
            return;

        GoogleAnalyticsV4.getInstance().LogEvent(customEventHitBuilder.ToEventHitBuilder());
    }

    public static void LogScreen(Enum screenKey)
    {
        LogScreen(screenKey.ToFriendlyString());
    }

    public static void LogScreen(string title)
    {
        if (Debug.isDebugBuild)
            return;

        GoogleAnalyticsV4.getInstance().LogScreen(title);
    }

    public static void LogItem(ItemHitBuilder itemHitBuilder)
    {
        if (Debug.isDebugBuild)
            return;

        GoogleAnalyticsV4.getInstance().LogItem(itemHitBuilder);
    }

    public static void LogTiming(TimingHitBuilder timingHitBuilder)
    {
        if (Debug.isDebugBuild)
            return;

        GoogleAnalyticsV4.getInstance().LogTiming(timingHitBuilder);
    }
}
