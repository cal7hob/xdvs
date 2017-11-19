using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using UnityEngine;

public class PushNotifications : MonoBehaviour
{
    [SerializeField] private PushwooshRequestManager pwRequestManager;

    public static PushwooshRequestManager PwRequestManager { get { return Instance.pwRequestManager; } }

    public delegate void PushesScheduled();

    public static PushesScheduled OnPushesScheduled = delegate { };

    private static IPusher pusher;

    public static IPusher Pusher { get { return pusher; } }

    public static PushNotifications Instance { get; private set; }

    public static bool IsPushWoooshInitialized { get; private set; }

    public static bool IsScheduled { get; private set; }

    public static int ModuleUpgradeRemain { get; private set; }

    public static int DailyBonusRemain { get; private set; }

    // пока не удаляю это свойство
    public static bool SendFuelNotification { get { return Settings.Instance.pushForFuel.IsOn && !ProfileInfo.IsFullTank; } }

    public static string AlertActionText { get { return GameData.CurrentGame.ToString(); } }

    void Awake()
    {
        if(Instance != null)
            return;

        Instance = this;
    }

    void Start()
    {
        PushwooshInit();
    }

    void OnApplicationFocus(bool focused)
    {

#if !UNITY_EDITOR && !(UNITY_WP8 || UNITY_WP8_1 || UNITY_WSA || UNITY_WSA_8_0 || UNITY_WSA_8_1 || UNITY_WSA_10_0)

        if (Time.realtimeSinceStartup < 1)
        {
            return;
        }


        if (focused)
        {
            IsScheduled = false; 
        }
        else
        {
            ScheduleLocalNotifications();
        }

#endif
    }

    void OnApplicationPause(bool paused)
    {

#if !UNITY_EDITOR && !(UNITY_WP8 || UNITY_WP8_1 || UNITY_WSA || UNITY_WSA_8_0 || UNITY_WSA_8_1 || UNITY_WSA_10_0)

        if (Time.realtimeSinceStartup < 1)
        {
            return;
        }

        if (paused)
            ScheduleLocalNotifications();

#endif
    }

    private static int GetModuleUpgradeRemain()
    {
        if (Settings.Instance.pushForUpgrade.IsOn)
        {
            var upgradingTanks = new List<VehicleUpgrades>();

            foreach (var tank in ProfileInfo.vehicleUpgrades)
            {
                if (tank.Value.moduleReadyTime > 0 && tank.Value.awaitedModule.ToString() != "None")
                {
                    upgradingTanks.Add(tank.Value);
                }
            }

            if (upgradingTanks.Count == 0)
            {
                return -1;
            }

            var minTime = int.MaxValue;

            foreach (var tank in upgradingTanks)
            {
                if (tank.moduleReadyTime < minTime)
                {
                    minTime = (int)(tank.moduleReadyTime - GameData.CurrentTimeStamp);
                }
            }

            Debug.LogFormat("moduleUpgradeRemain: {0}", minTime);
            return minTime;
        }

        return -1;
    }

    private static int GetDailyBonusRemain()
    {
        if (Settings.Instance.pushForDailyBonus.IsOn)
        {
            return 60 * 60 * 24; //(int)(ProfileInfo.nextDayServerTime - GameData.CorrectedCurrentTimeStamp);
        }

        return -1;
    }

    public void ScheduleLocalNotifications()
    {
        if (HangarController.Instance != null && HangarController.Instance.IsInitialized && !IsScheduled)
        {
            StartCoroutine(SettingLocalNotifications());
            IsScheduled = true;
        }
    }

    public IEnumerator SettingLocalNotifications()
    {
        if (pusher == null)
            yield break;

        ModuleUpgradeRemain = GetModuleUpgradeRemain();
        DailyBonusRemain = GetDailyBonusRemain();

        yield return StartCoroutine(pusher.RemovingOldNotifications());
        yield return StartCoroutine(pusher.SchedulingLocalNotifications());

        Debug.Log("On pushes scheduled, Quit game");
        OnPushesScheduled();
    }

    private IEnumerator SettingTags()
    {
        var waiter = 0.2f;

        while (!IsPushWoooshInitialized)
        {
            yield return new WaitForSeconds(waiter);
        }

        
        yield return StartCoroutine(pusher.RemovingOldNotifications());
        yield return StartCoroutine(pwRequestManager.SetTags(pusher.HWID));
    }

    public void PushwooshInit()
    {
        Pushwoosh.ApplicationCode = GameData.pushwooshAppCode;
        Pushwoosh.GcmProjectNumber = GameData.pushwooshFCMSenderId;

#if UNITY_ANDROID && !UNITY_EDITOR
        pusher = gameObject.AddComponent<AndroidPusher>();
#elif UNITY_IPHONE && !UNITY_EDITOR
		pusher = gameObject.AddComponent<IOSPusher>();
#elif (UNITY_WP8 || UNITY_WP8_1 || UNITY_WSA || UNITY_WSA_8_0 || UNITY_WSA_8_1 || UNITY_WSA_10_0) && !UNITY_EDITOR
        pusher = gameObject.AddComponent<WP8Pusher>();
#endif

#if !UNITY_EDITOR && (UNITY_ANDROID || UNITY_IPHONE || UNITY_WP8 || UNITY_WP8_1 || UNITY_WSA || UNITY_WSA_8_0 || UNITY_WSA_8_1 || UNITY_WSA_10_0)
        pusher.Initialize();
        pusher.RegisterForPushNotifications();
        pusher.OnRegisteredForPushNotifications += payload => OnPushwooshInitialized();
        
#endif

        StartCoroutine(SettingTags());
    }

    public void OnPushwooshInitialized()
    {
        IsPushWoooshInitialized = true;
        Debug.Log(string.Format("Pushwoosh HWID: {0}, Token: {1}", pusher.HWID, pusher.PushToken));
    }
}