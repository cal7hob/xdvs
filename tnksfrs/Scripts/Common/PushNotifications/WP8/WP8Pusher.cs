#if (UNITY_WP8 || UNITY_WP8_1 || UNITY_WSA || UNITY_WSA_8_0 || UNITY_WSA_8_1 || UNITY_WSA_10_0) && !UNITY_EDITOR

using System.Collections;
using UnityEngine;
using System;
using System.Collections.Generic;
using System.Globalization;

public enum WP8NotifTypes
{
    Toast,
    Tile
}

public class WP8Pusher : PushNotificationsWindows, IPusher
{
    public WP8NotifTypes WP8NotifType;

    private readonly List<object> wp8Notifications = new List<object>(3);
    private List<object> wp8NotificationConditions;
    private List<object> platformsList;
    private bool isPrevNotifsRemoved;
    
    public static WP8Pusher Instance { get; private set; }

    void Awake()
    {
        Instance = this;
    }

    void OnDestroy()
    {
        Instance = null;
    }

    private void CreateConditionsList()
    {
        wp8NotificationConditions = new List<object>
        {
            new List<object>()
            {
                "playerId",
                "EQ",
                ProfileInfo.playerId.ToString(CultureInfo.InvariantCulture)
            }
        };
    }

    private void CreatePlatformsList()
    {
        platformsList = new List<object>(1) {8};
    }

    private Dictionary<object, object> CreateLocalNotificationData(string lblName, int remainTime)
    {
        string str = "<toast>" +
                         "<visual>" +
                             "<binding template = 'ToastText01'>" +
                                 "<text id = '1'>" + Localizer.GetText(lblName) + "</text>" +
                             "</binding>" +
                         "</visual>" +
                     "</toast>";

        //string str = "<toast>" +
        //                 "<visual>" +
        //                     "<binding template = 'ToastImageAndText01'>" +
        //                         "<image id = '1' src = '/Assets/SmallLogo.target-256.jpg' />" +
        //                         "<text id = '1'>" + Localizer.GetText(lblName) + "</text>" +
        //                     "</binding>" +
        //                 "</visual>" +
        //             "</toast>";

        return new Dictionary<object, object>()
        {
            //{"content", Localizer.GetText(lblName).Substring(0, 36) + ".."},

            {"wns_content", MiscTools.Base64Encode(str)},    // Localizer.GetText(lblName)
            {"wns_type", WP8NotifTypes.Toast.ToString()},
            {"send_date", DateTime.UtcNow.AddSeconds(remainTime).ToString("yyyy-MM-dd HH:mm:ss")},
            {"conditions", wp8NotificationConditions},
            {"platforms", platformsList},
            {"ignore_user_timezone", true}
        };
    }

    public IEnumerator RemovingOldNotifications()
    {
        Debug.Log("removing old notifications");
        if (!PlayerPrefs.HasKey("prevPushwooshResponse") || string.IsNullOrEmpty(PlayerPrefs.GetString("prevPushwooshResponse")))
        {
            Debug.Log("no notifications to remove");
            isPrevNotifsRemoved = true;
            yield break;
        }

        JsonPrefs prefs = new JsonPrefs(PlayerPrefs.GetString("prevPushwooshResponse"));
        if (prefs.Contains("response"))
        {
            prefs.BeginGroup("response");
            var messages = prefs.ValueObjectList("Messages");
            foreach (var message in messages)
            {
                 yield return StartCoroutine(PushNotifications.PwRequestManager.DeleteMessageApiRequest(message.ToString().Trim()));
            }
            isPrevNotifsRemoved = true;
            PlayerPrefs.SetString("prevPushwooshResponse", string.Empty);
            prefs.EndGroup();
            Debug.Log("old notifications removed");
        }
        else
        {
            isPrevNotifsRemoved = true;
        }   
    }

    public IEnumerator SchedulingLocalNotifications(NotificationParams notifParams)
    {
        if (!isPrevNotifsRemoved)
        {
            yield break;
        }

        CreateConditionsList();
        CreatePlatformsList();

        if (notifParams.ModuleUpgradeRemain > 0)
        {
            wp8Notifications.Add(CreateLocalNotificationData("lblItemDelivered", notifParams.ModuleUpgradeRemain));
        }

        if (notifParams.DailyBonusRemain > 0)
        {
            wp8Notifications.Add(CreateLocalNotificationData("lblDailyBonusNotification", notifParams.DailyBonusRemain));
        }

        if (notifParams.SendFuelNotification)
        {
            //wp8Notifications.Add(CreateLocalNotificationData("lblGasTankRefilled", RefillGasTank.RefillSecondsRemainingFull));
        } 
       
        yield return StartCoroutine(PushNotifications.PwRequestManager.CreateMessage(wp8Notifications));

        isPrevNotifsRemoved = false;
    }
}
#endif