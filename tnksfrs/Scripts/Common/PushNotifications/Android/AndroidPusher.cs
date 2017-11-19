#if UNITY_ANDROID && !UNITY_EDITOR

using System.Collections;
using UnityEngine;

public class AndroidPusher : PushNotificationsAndroid, IPusher
{
    public void Initialize()
    {
    }

    public IEnumerator SchedulingLocalNotifications(NotificationParams notifParms)
    {
        Debug.Log(string.Format("notification parameters: {0}", notifParms));

        if (notifParms.ModuleUpgradeRemain > 0)
            ScheduleLocalNotification(Localizer.GetText("lblItemDelivered"), notifParms.ModuleUpgradeRemain);
        if (notifParms.DailyBonusRemain > 0)
            ScheduleLocalNotification(Localizer.GetText("lblDailyBonusNotification"), notifParms.DailyBonusRemain);
        
        yield break;
    }

    public IEnumerator RemovingOldNotifications()
    {
        ClearLocalNotifications();

        yield break;
    }
}
#endif
