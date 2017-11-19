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
        initializePushManager(PushNotifications.PUSHWOOSH_APP, Application.productName);
        setListenerName(gameObject.name);
    }

    public IEnumerator RemovingOldNotifications()
    {
        NotificationServices.CancelAllLocalNotifications();
        yield break;
    }

    public IEnumerator SchedulingLocalNotifications(NotificationParams notifParams)
    {
        var localNotificationsIOS = new List<LocalNotification>();

        if (notifParams.ModuleUpgradeRemain > 0)
        {
            localNotificationsIOS.Add(CreateLocalNotificationData(notifParams.ModuleUpgradeRemain, "lblItemDelivered"));
        }

        if (notifParams.SendFuelNotification)
        {
            //localNotificationsIOS.Add(CreateLocalNotificationData(RefillGasTank.RefillSecondsRemainingFull, "lblGasTankRefilled"));
        }

        if (notifParams.DailyBonusRemain > 0)
        {
            localNotificationsIOS.Add(CreateLocalNotificationData(notifParams.DailyBonusRemain, "lblDailyBonusNotification"));
        }

        foreach (var localNotification in localNotificationsIOS)
        {
            localNotification.alertAction = notifParams.AlertActionText;
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
