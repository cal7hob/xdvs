using System;
using Http;
using UnityEngine;

public class RewardedVideoController : MonoBehaviour
{
    public enum State
    {
        Idle,
        Delivery,
        AfterBattle
    }

    public static RewardedVideoController Instance { get; private set; }
    public static bool ServiceHasAds { get { return Instance != null && Instance.service != null && Instance.service.HasRewardedVideos; } }

    private ThirdPartyAdsService service;
    private State state = State.Idle;

    private void Awake()
    {
        Instance = this;
    }

    private void OnDestroy()
    {
        if (service)
        {
            service.OnRewardedVideoLoaded -= OnRewardedVideoLoaded;
            service.OnRewardedVideoClosed -= OnRewardedVideoClosed;
            service.OnRewardedVideoShown -= OnRewardedVideoShown;
            service.OnRewardedVideoFinished -= OnRewardedVideoFinished;
            service.OnRewardedVideoFailedToLoad -= OnRewardedVideoFailedToLoad;
        }
        Instance = null;
    }

    void Start()
    {
        service = ThirdPartyAdsManager.GetServiceWithName(ProfileInfo.adsPlatform);
        
        if (service && service.HasRewardedVideos)
        {
            service.OnRewardedVideoLoaded += OnRewardedVideoLoaded;
            service.OnRewardedVideoClosed += OnRewardedVideoClosed;
            service.OnRewardedVideoShown += OnRewardedVideoShown;
            service.OnRewardedVideoFinished += OnRewardedVideoFinished;
            service.OnRewardedVideoFailedToLoad += OnRewardedVideoFailedToLoad;
        }
    }

    public void BtnDeliveryOnClick()
    {
        if (!service || !service.HasRewardedVideos)
            return;
        state = State.Delivery;
        service.ShowRewardedVideo();
        Dispatcher.Send(EventId.RewardedVideoClicked, new EventInfo_I((int)state));
    }

    public void BtnAfterBattleScreenOnClick()
    {
        if (!service || !service.HasRewardedVideos)
            return;
        state = State.AfterBattle;
        service.ShowRewardedVideo();
        Dispatcher.Send(EventId.RewardedVideoClicked, new EventInfo_I((int)state));
    }

    private void OnRewardedVideoFailedToLoad()
    {
    }

    private void OnRewardedVideoFinished()
    {
        var request = Http.Manager.Instance().CreateRequest("/ads/watching/finished");
        request.Form.AddField("screen",  state == State.AfterBattle ? "afterBattle" : "delivery");
        Http.Manager.StartAsyncRequest(request, delegate(Response result)
        {
            if (state == State.Delivery)
            {
                ModuleShop.Instance.ModuleReceived(Shop.CurrentVehicle, ModuleShop.Instance.ModuleInView.type);
            }
            else
            {
                AfterBattleStatistic.Instance.btnGetDoubleRewardForAdsViewing.SetActive(false);
            }
        });
    }

    private void OnRewardedVideoShown()
    {
    }

    private void OnRewardedVideoClosed()
    {
        var request = Http.Manager.Instance().CreateRequest("/ads/watching/canceled");
        request.Form.AddField("screen", state == State.AfterBattle ? "afterBattle" : "delivery");
        Http.Manager.StartAsyncRequest(request);
    }

    private void OnRewardedVideoLoaded()
    {
    }
}
