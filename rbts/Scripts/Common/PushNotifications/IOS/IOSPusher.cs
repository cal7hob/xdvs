#if UNITY_IPHONE && !UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using LocalNotification = UnityEngine.iOS.LocalNotification;
using NotificationServices = UnityEngine.iOS.NotificationServices;

public class IOSPusher : PushNotificationsIOS, IPusher 
{
    public override void Initialize()
    {
        base.Initialize();
        initializePushManager(ApplicationCode, Application.productName);
        setListenerName(gameObject.name);
    }

    public IEnumerator RemovingOldNotifications()
    {
        NotificationServices.CancelAllLocalNotifications();
        yield break;
    }

    public IEnumerator SchedulingLocalNotifications()
    {
        var localNotificationsIOS = new List<LocalNotification>();

        if (PushNotifications.ModuleUpgradeRemain > 0)
        {
            localNotificationsIOS.Add(CreateLocalNotificationData(PushNotifications.ModuleUpgradeRemain, "lblItemDelivered"));
        }

        if (PushNotifications.SendFuelNotification)
        {
            localNotificationsIOS.Add(CreateLocalNotificationData(RefillGasTank.RefillSecondsRemainingFull, "lblGasTankRefilled"));
        }

        if (PushNotifications.DailyBonusRemain > 0)
        {
            localNotificationsIOS.Add(CreateLocalNotificationData(PushNotifications.DailyBonusRemain, "lblDailyBonusNotification"));
        }

        foreach (var localNotification in localNotificationsIOS)
        {
            localNotification.alertAction = PushNotifications.AlertActionText;
            NotificationServices.ScheduleLocalNotification(localNotification);
        }
        
        yield break;
    }

    private LocalNotification CreateLocalNotificationData(int remainTime, string lblName)
    {
        return new LocalNotification()
        {
            fireDate = DateTime.Now.AddSeconds(remainTime),
            alertBody = Localizer.GetText(lblName)
        };
    }
}
#endif
