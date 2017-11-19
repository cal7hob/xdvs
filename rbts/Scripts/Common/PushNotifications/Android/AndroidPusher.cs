#if UNITY_ANDROID && !UNITY_EDITOR

using System.Collections;
using UnityEngine;

public class AndroidPusher : PushNotificationsAndroid, IPusher
{
    public IEnumerator SchedulingLocalNotifications()
    {
        if (PushNotifications.ModuleUpgradeRemain > 0)
            ScheduleLocalNotification(Localizer.GetText("lblItemDelivered"), PushNotifications.ModuleUpgradeRemain);
        if (PushNotifications.SendFuelNotification)
            ScheduleLocalNotification(Localizer.GetText("lblGasTankRefilled"), RefillGasTank.RefillSecondsRemainingFull);
        if (PushNotifications.DailyBonusRemain > 0)
            ScheduleLocalNotification(Localizer.GetText("lblDailyBonusNotification"), PushNotifications.DailyBonusRemain);
        
        yield return null;
    }

    public IEnumerator RemovingOldNotifications()
    {
        ClearLocalNotifications();

        yield return null;
    }
}
#endif
