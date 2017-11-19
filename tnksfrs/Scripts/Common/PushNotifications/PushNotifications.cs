using System;
using System.Collections;
using System.ComponentModel;
using UnityEngine;

public struct NotificationParams
{
    public int ModuleUpgradeRemain { get; set; }
    public int DailyBonusRemain { get; set; }
    public bool SendFuelNotification { get; set; }
    public string AlertActionText { get; set; }
}

public class PushNotifications : MonoBehaviour
{
    [SerializeField] private PushwooshRequestManager pwRequestManager;

    public static PushwooshRequestManager PwRequestManager { get { return Instance.pwRequestManager; } }

    public delegate void PushesScheduled();
    public static PushesScheduled OnPushesScheduled;
    public const string PUSHWOOSH_AUTH_TOKEN = "b0012Z54AT43PKgBhiCUJoEg4ZDWesjXsjEe6VfClyTlt9tTS5XlWvKDOWvsNBJ5cQtytD8M6Muu3YxCIMep";

    [Header("Pushwoosh App Codes:")]
    public string armada2AppCode            = "204FC-F79CC";

    [Header("Google Cloud Project Numbers:")]
    public string GCM_AD2                   = "822691851180";


    private static NotificationParams notifParams;
    private static IPusher pusher;

    public static IPusher Pusher { get { return pusher; } }
    public static string PUSHWOOSH_APP { get; private set; }
    public static string GCM_PROJECT_NUMBER { get; private set; }

    public static PushNotifications Instance { get; private set; }
    public static bool IsPushWooshInitialized { get; private set; }
     
    void Awake()
    {
        if (Instance != null)
        {
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        SetCredentials();

        Dispatcher.Subscribe(EventId.ServerDataReceived, OnHangarInit);
    }

    void Start()
    {
        PushwooshInit();
    }

    private void SetCredentials()
    {
        if (GameData.IsGame(Game.Armada2))
        {
            PUSHWOOSH_APP = armada2AppCode;
            GCM_PROJECT_NUMBER = GCM_AD2;
        }
    }

    private static void InitLocalNotifications()
    {
        notifParams = new NotificationParams();
        SetDailyBonusRemainTime();
        SetModuleUpgradeRemainTime();
        SetSendFuelNotifFlag();
        notifParams.AlertActionText = GameData.CurrentGame.ToString();
    }

    public IEnumerator SettingLocalNotifications()
    {
        InitLocalNotifications();

        if (pusher == null)
            yield break;

#if (UNITY_WP8 || UNITY_WP8_1 || UNITY_WSA || UNITY_WSA_8_0 || UNITY_WSA_8_1 || UNITY_WSA_10_0) && !UNITY_EDITOR
        yield return StartCoroutine(pusher.RemovingOldNotifications());
        yield return StartCoroutine(pusher.SchedulingLocalNotifications(notifParams));
#elif !UNITY_EDITOR
        StartCoroutine(pusher.RemovingOldNotifications());
        StartCoroutine(pusher.SchedulingLocalNotifications(notifParams));
#endif

        if (OnPushesScheduled != null)
        {
            OnPushesScheduled();
        }
    }

    private static void SetModuleUpgradeRemainTime()
    {
        /*if (Settings.Instance.pushForUpgrade.IsOn)
        {
            foreach (var tank in ProfileInfo.vehicleUpgrades)
            {
                if (tank.Value.moduleReadyTime > 0 && tank.Value.awaitedModule.ToString() != "None")
                {
                    notifParams.ModuleUpgradeRemain = (int)(tank.Value.moduleReadyTime - GameData.CurrentTimeStamp);
                }
            }
        }*/
    }

    private static void SetSendFuelNotifFlag()
    {
    }

    private static void SetDailyBonusRemainTime()
    {
        /*if (Settings.Instance.pushForDailyBonus.IsOn)
        {
            notifParams.DailyBonusRemain = 60*60*24; //(int)(ProfileInfo.nextDayServerTime - GameData.CorrectedCurrentTimeStamp);
        } */
    }

    private void OnHangarInit(EventId id, EventInfo info)
    {
        Debug.Log("TF: PushNotification.OnHangarInit -->>");
        if (pusher == null)
        {
            Debug.Log("TF: PushNotification.OnHangarInit ->  if (pusher == null) RETURN");
            return;
        }

        if (IsPushWooshInitialized)
        {
            Debug.Log("TF: PushNotification.OnHangarInit ->  StartCoroutine(SettingTags());");
            StartCoroutine(SettingTags());
        }
        else
        {
            pusher.OnRegisteredForPushNotifications += payload => StartCoroutine(SettingTags());
        }
        Debug.Log("TF: PushNotification.OnHangarInit <<--");
    }

    private IEnumerator SettingTags()
    {
        yield return StartCoroutine(pusher.RemovingOldNotifications());
        //yield return StartCoroutine(pwRequestManager.SetTags(pusher.HWID));
    }

    public void PushwooshInit()
    {
        Debug.Log("TF: PushNotification.PushwooshInit() -->>");
        if (IsPushWooshInitialized)
        {
            Debug.Log("TF: PushNotification.PushwooshInit(), IsPushWooshInitialized: true; RETURN;");
            return;
        }
        Debug.Log("TF: PushNotification.PushwooshInit() 1");
        notifParams = new NotificationParams();
        Debug.Log("TF: PushNotification.PushwooshInit() 2");
        Pushwoosh.ApplicationCode = PUSHWOOSH_APP;
        Debug.Log("TF: PushNotification.PushwooshInit() 3");
        Pushwoosh.GcmProjectNumber = GCM_PROJECT_NUMBER;
        Debug.Log("TF: PushNotification.PushwooshInit() 4");
        //Pushwoosh.Instance.OnFailedToRegisteredForPushNotifications += OnFailedToRegisteredForPushNotifications;
        Debug.Log("TF: PushNotification.PushwooshInit() 5");
        //Pushwoosh.Instance.OnPushNotificationsReceived += OnPushNotificationsReceived;
        Debug.Log("TF: PushNotification.PushwooshInit() 6");

#if UNITY_ANDROID && !UNITY_EDITOR
        Debug.Log("TF: PushNotification.PushwooshInit() 7");
        pusher = gameObject.AddComponent<AndroidPusher>();
        Debug.Log("TF: PushNotification.PushwooshInit() 8");
#elif UNITY_IPHONE && !UNITY_EDITOR
		pusher = gameObject.AddComponent<IOSPusher>();
#elif (UNITY_WP8 || UNITY_WP8_1 || UNITY_WSA || UNITY_WSA_8_0 || UNITY_WSA_8_1 || UNITY_WSA_10_0) && !UNITY_EDITOR
        pusher = gameObject.AddComponent<WP8Pusher>();
#endif
        Debug.Log("TF: PushNotification.PushwooshInit() 9");
#if !UNITY_EDITOR && (UNITY_ANDROID || UNITY_IPHONE || UNITY_WP8 || UNITY_WP8_1 || UNITY_WSA || UNITY_WSA_8_0 || UNITY_WSA_8_1 || UNITY_WSA_10_0)
        Debug.Log("TF: PushNotification.PushwooshInit() 10");
        pusher.Initialize();
        Debug.Log("TF: PushNotification.PushwooshInit() 11");
        pusher.OnRegisteredForPushNotifications += OnPushwooshInitialized;
        Debug.Log("TF: PushNotification.PushwooshInit() 12");
        pusher.OnFailedToRegisteredForPushNotifications += OnFailedToRegisteredForPushNotifications;
        Debug.Log("TF: PushNotification.PushwooshInit() 13");
        pusher.OnPushNotificationsReceived += OnPushNotificationsReceived;
        Debug.Log("TF: PushNotification.PushwooshInit() 14");
        pusher.RegisterForPushNotifications();
        Debug.Log("TF: PushNotification.PushwooshInit() 15");
#endif
        //pusher.OnRegisteredForPushNotifications
        Debug.Log("TF: PushNotification.PushwooshInit() <<--");
    }

    public void OnPushwooshInitialized(string token)
    {
        if (IsPushWooshInitialized)
        {
            Debug.LogError("TF: PushNotification.OnPushwooshInitialized. Ignore Second Initialization");
            return;
        }
        IsPushWooshInitialized = true;
        if (pusher != null)
        {
            Debug.Log("TF: PushNotification.OnPushwooshInitialized. pusher != null -> pusher.RemovingOldNotifications();");
            pusher.RemovingOldNotifications();
        }
        //IsScheduled = false;
        Debug.Log(string.Format("TF: PushNotification.Pushwoosh HWID: {0}, Token: {1}", pusher.HWID, pusher.PushToken));

#if UNITY_WSA
        UnityEngine.WSA.Application.InvokeOnAppThread(() =>
        {
#endif
        //StartCoroutine(pwRequestManager.SetTags(pusher.HWID));
#if UNITY_WSA
        }, false);
#endif
    }

    public void OnFailedToRegisteredForPushNotifications(string error)
    {
        Debug.LogError("TF: PushNotification.OnFailedToRegisteredForPushNotifications. OnFailedToRegisteredForPushNotifications: " + error);
    }

    public void OnPushNotificationsReceived(string payload)
    {
        Debug.LogError("TF: PushNotification.OnPushNotificationsReceived. OnPushNotificationsReceived: " + payload);
    }

}