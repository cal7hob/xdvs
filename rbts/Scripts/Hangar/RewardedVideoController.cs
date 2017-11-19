using System;
using Http;
using UnityEngine;
using System.Collections.Generic;

public class RewardedVideoController : MonoBehaviour
{
    public enum State
    {
        Idle,
        Delivery,
        AfterBattle
    }

    public static RewardedVideoController Instance { get; private set; }

    public static bool ServiceHasAds
    {
        get
        {
            var serviceHasAds = Instance != null && Instance.service != null && Instance.service.HasRewardedVideos;
            //Debug.LogErrorFormat("RewardedVideoController.ServiceHasAds == {0}", serviceHasAds);
            return serviceHasAds;
        }
    }

    private ThirdPartyAdsService service;
    private State state = State.Idle;

    public static bool showOnAfterBattleScreen = false;
    public static bool showOnModuleDeliveryScreen = false;
    public static int timeToShowOnDeliveryScreen = 0;
    public static string adsPlatform = "ChartBoost";

    public static bool ShowBtnModuleDeliveryForAdsViewing
    {
        get
        {
            if (!ServiceHasAds || Shop.CurrentVehicle == null || Shop.CurrentVehicle.Upgrades == null || Shop.CurrentVehicle.Upgrades.awaitedModule == TankModuleInfos.ModuleType.None)
            {
                //Debug.LogErrorFormat("ShowBtnModuleDeliveryForAdsViewing: ServiceHasAds=={0}, Shop.CurrentVehicle=={1}, Shop.CurrentVehicle.Upgrades=={2}, Shop.CurrentVehicle.Upgrades.awaitedModule=={3}",
                //    ServiceHasAds, Shop.CurrentVehicle, Shop.CurrentVehicle.Upgrades, Shop.CurrentVehicle.Upgrades.awaitedModule);

                return false;
            }

            var upgradeRemainingTimeSec = Shop.CurrentVehicle.Upgrades.moduleReadyTime - GameData.CurrentTimeStamp;

            var show = showOnModuleDeliveryScreen && (upgradeRemainingTimeSec <= timeToShowOnDeliveryScreen);

            //Debug.LogErrorFormat("ShowBtnModuleDeliveryForAdsViewing == {0}", show);

            return show;
        }
    }

    public static bool ShowBtnGetDoubleRewardForAdsViewing
    {
        get
        {
            var show = showOnAfterBattleScreen && !ProfileInfo.IsPlayerVip && ServiceHasAds;
            //Debug.LogErrorFormat("ShowBtnGetDoubleRewardForAdsViewing == {0}", show);
            return show;
        }
    }

    private void Awake()
    {
        Instance = this;
        Messenger.Subscribe(EventId.AfterHangarInit, OnProfileInfoLoaded);
        Messenger.Subscribe(EventId.ProfileInfoLoadedFromServer, OnProfileInfoLoaded);
    }

    private void OnDestroy()
    {
        if (service)
        {
            service.OnRewardedVideoClosed -= OnRewardedVideoClosed;
            service.OnRewardedVideoFinished -= OnRewardedVideoFinished;
            service.OnRewardedVideoFailedToLoad -= OnRewardedVideoFailedToLoad;
        }

        Messenger.Unsubscribe(EventId.AfterHangarInit, OnProfileInfoLoaded);
        Messenger.Unsubscribe(EventId.ProfileInfoLoadedFromServer, OnProfileInfoLoaded);

        Instance = null;
    }

    void Start()
    {
        service = ThirdPartyAdsManager.GetServiceWithName(adsPlatform);

        if (service && service.HasRewardedVideos)
        {
            service.OnRewardedVideoClosed += OnRewardedVideoClosed;
            service.OnRewardedVideoFinished += OnRewardedVideoFinished;
            service.OnRewardedVideoFailedToLoad += OnRewardedVideoFailedToLoad;
        }
    }

    private void OnProfileInfoLoaded(EventId id, EventInfo info)
    {
        JsonPrefs prefs = new JsonPrefs(ProfileInfo.adsDict);

        showOnAfterBattleScreen = prefs.ValueBool("afterBattleScreen/show");
        showOnModuleDeliveryScreen = prefs.ValueBool("deliveryScreen/show");
        timeToShowOnDeliveryScreen = prefs.ValueInt("deliveryScreen/timeToShow");
        adsPlatform = prefs.ValueString("platform", "ChartBoost");
    }

    public void BtnDeliveryOnClick()
    {
        if (!service || !service.HasRewardedVideos)
            return;

        state = State.Delivery;
        service.ShowRewardedVideo();
        Messenger.Send(EventId.RewardedVideoClicked, new EventInfo_I((int)state));
    }

    public void BtnGetDoubleRewardForAdsViewingOnClick()
    {
        if (!service || !service.HasRewardedVideos)
            return;

        state = State.AfterBattle;
        service.ShowRewardedVideo();
        Messenger.Send(EventId.RewardedVideoClicked, new EventInfo_I((int)state));
    }

    private void OnRewardedVideoFailedToLoad()
    {
    }

    private void OnRewardedVideoFinished()
    {
        var request = Manager.Instance().CreateRequest("/ads/watching/finished");
        request.Form.AddField("screen", state == State.AfterBattle ? "afterBattle" : "delivery");
        Manager.StartAsyncRequest(request, delegate (Response result)
        {
            if (state == State.Delivery)
            {
                ModuleShop.Instance.ModuleReceived(Shop.CurrentVehicle, ModuleShop.Instance.ModuleInView.type);
            }
            else
            {
                AfterBattleStatistic.Instance.btnGetDoubleRewardForAdsViewing.gameObject.SetActive(false);
            }
        });
    }

    private void OnRewardedVideoClosed()
    {
        var request = Manager.Instance().CreateRequest("/ads/watching/canceled");
        request.Form.AddField("screen", state == State.AfterBattle ? "afterBattle" : "delivery");
        Manager.StartAsyncRequest(request);
    }
}
