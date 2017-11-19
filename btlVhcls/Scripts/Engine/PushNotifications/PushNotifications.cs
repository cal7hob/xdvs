using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using UnityEngine;

public class PushNotifications : MonoBehaviour
{
    [SerializeField] private PushwooshRequestManager pwRequestManager;

    public static PushNotifications Instance { get; private set; }
    public static PushwooshRequestManager PwRequestManager { get { return Instance.pwRequestManager; } }
    public static int ModuleUpgradeRemain { get; private set; }
    public static int DailyBonusRemain { get; private set; }
    public static bool SendFuelNotification { get { return ProfileInfo.isPushForFuel && !ProfileInfo.IsFullTank; } }
    public static bool IsScheduled { get { return isScheduled; } private set { isScheduled = value; /*Debug.LogErrorFormat("IsScheduled = {0}", IsScheduled);*/ } }
    
    private static IPusher pusher;
    private static bool IsPushWooshInitialized { get; set; }
    private static bool isScheduled = false;

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

    void OnApplicationPause(bool paused)
    {
#if !UNITY_EDITOR && !(UNITY_WP8 || UNITY_WP8_1 || UNITY_WSA || UNITY_WSA_8_0 || UNITY_WSA_8_1 || UNITY_WSA_10_0)
        if(paused)
            ScheduleLocalNotifications();
        else
            RemoveLocalNotification();
#endif
    }

    private static int GetModuleUpgradeRemain()
    {
        if (Settings.Instance && Settings.Instance.pushForUpgrade.IsOn)
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
        if (ProfileInfo.isPushForDailyBonus)
        {
            return (int)(ProfileInfo.nextDayServerTime - GameData.CorrectedCurrentTimeStamp);
        }

        return -1;
    }

    public void ScheduleLocalNotifications()
    {
        if (HangarController.Instance != null && HangarController.Instance.IsInitialized && IsPushWooshInitialized && !IsScheduled)
        {
            SettingLocalNotifications();
        }
        else
            Debug.LogErrorFormat("HangarController.Instance = {0}, HangarController.Instance.IsInitialized = {1}, IsScheduled = {2}, IsPushWooshInitialized = {3}",
                HangarController.Instance,
                HangarController.Instance ? HangarController.Instance.IsInitialized.ToString() : "NULL",
                IsScheduled,
                IsPushWooshInitialized);
    }

    private void RemoveLocalNotification()
    {
        if (pusher != null)
        {
            pusher.RemovingOldNotifications();
            IsScheduled = false;
        }
    }

    public void SettingLocalNotifications()
    {
        if (!IsPushWooshInitialized)
        {
            Debug.LogErrorFormat("SettingLocalNotifications(). IsPushWooshInitialized = false!");
            return;
        }

        IsScheduled = true;

        ModuleUpgradeRemain = GetModuleUpgradeRemain();
        DailyBonusRemain = GetDailyBonusRemain();

        pusher.RemovingOldNotifications();
        pusher.SchedulingLocalNotifications();

        Debug.Log("On pushes scheduled");
    }

    public void PushwooshInit()
    {
        if (IsPushWooshInitialized)
            return;

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
        pusher.OnRegisteredForPushNotifications += OnPushwooshInitialized;
        pusher.RegisterForPushNotifications();
#endif
    }

    public void OnPushwooshInitialized(string token)
    {
        if (IsPushWooshInitialized)
        {
            Debug.LogError("OnPushwooshInitialized. Ignore Second Initialization");
            return;
        }
        IsPushWooshInitialized = true;
        if (pusher != null)
            pusher.RemovingOldNotifications();
        IsScheduled = false;
        Debug.Log(string.Format("Pushwoosh HWID: {0}, Token: {1}", pusher.HWID, pusher.PushToken));

#if UNITY_WSA
        UnityEngine.WSA.Application.InvokeOnAppThread(() =>
        {
#endif
            StartCoroutine(pwRequestManager.SetTags(pusher.HWID));
#if UNITY_WSA
        }, false);
#endif
    }
}