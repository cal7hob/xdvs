using System;
using System.Linq;
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

    public tk2dUIItem btnAfterBattleScreen;
    public ActivatedUpDownButton btnDeliverFree;
    public tk2dTextMesh moduleInfoText;

    private ThirdPartyAdsService service;
    private State state = State.Idle;
    private JsonPrefs prefs;

    void Start()
    {
        prefs = new JsonPrefs(ProfileInfo.adsDict);
        Dispatcher.Subscribe(EventId.ProfileInfoLoadedFromServer, OnProfileInfoLoaded);
        btnAfterBattleScreen.OnClick += BtnAfterBattleScreenOnOnClick;
        btnDeliverFree.uiItem.OnClick += BtnDeliveryOnClick;//!
        btnDeliverFree.gameObject.SetActive(false);//!
        if (prefs.Contains("deliveryScreen/show"))
        {
            ShowOnDeliveryScreen = prefs.ValueBool("deliveryScreen/show");
            Debug.Log("!!!prefs " + ShowOnDeliveryScreen);
        }

        //int adCount = prefs.ValueInt("deliveryScreen/timeToShow");//должно быть не timeToShow, а что то другое. Означающее кол-во раз, сколько может быть показана кнопка


        service = ThirdPartyAdsManager.GetServiceWithName(Platform);

        if (service && service.HasRewardedVideos)
        {
            if (ShowOnAfterBattleScreen)
            {
                btnAfterBattleScreen.gameObject.SetActive(true);
            }
            if (ShowOnDeliveryScreen)
            {
                HangarController.OnTimerTick += HangarControllerOnTimerTick;
            }

            service.OnRewardedVideoLoaded += OnRewardedVideoLoaded;
            service.OnRewardedVideoClosed += OnRewardedVideoClosed;
            service.OnRewardedVideoShown += OnRewardedVideoShown;
            service.OnRewardedVideoFinished += OnRewardedVideoFinished;
            service.OnRewardedVideoFailedToLoad += OnRewardedVideoFailedToLoad;
        }
    }

    private void BtnDeliveryOnClick()//бесплатная ускоренная доставка
    {
        Debug.Log("BtnDeliveryOnClick");
        state = State.Delivery;
        service.ShowRewardedVideo();
        Dispatcher.Send(EventId.RewardedVideoClicked, new EventInfo_I((int)state));//для googleanalytics
        //добавлено
        prefs = new JsonPrefs(ProfileInfo.adsDict);
        if (prefs.Contains("deliveryScreen/show"))
        {
            ShowOnDeliveryScreen = prefs.ValueBool("deliveryScreen/show");
           // Debug.Log("!!!prefs " + ShowOnDeliveryScreen);
        }
    }

    private void BtnAfterBattleScreenOnOnClick()//получение двойного опыта
    {
        state = State.AfterBattle;
        service.ShowRewardedVideo();
        Dispatcher.Send(EventId.RewardedVideoClicked, new EventInfo_I((int)state));
    }

    public void OnDisable()
    {
        Dispatcher.Unsubscribe(EventId.ProfileInfoLoadedFromServer, OnProfileInfoLoaded);
        if (!service) 
        { 
            return; 
        }

        if (ShowOnDeliveryScreen)
        {
            HangarController.OnTimerTick -= HangarControllerOnTimerTick;
        }

        service.OnRewardedVideoLoaded -= OnRewardedVideoLoaded;
        service.OnRewardedVideoClosed -= OnRewardedVideoClosed;
        service.OnRewardedVideoShown -= OnRewardedVideoShown;
        service.OnRewardedVideoFinished -= OnRewardedVideoFinished;
        service.OnRewardedVideoFailedToLoad -= OnRewardedVideoFailedToLoad;
    }

    private void OnProfileInfoLoaded(EventId id, EventInfo info)
    {
        prefs = new JsonPrefs(ProfileInfo.adsDict);
    }

    private void HangarControllerOnTimerTick(double d)
    {
        VehicleUpgrades upgrades = Shop.CurrentVehicle.Upgrades;
        var upgradeRamainingTimeSec = upgrades.moduleReadyTime - GameData.CurrentTimeStamp;

        bool show = ShowOnDeliveryScreen && (upgradeRamainingTimeSec <= TimeToShowOnDeliveryScreen);//!
        btnDeliverFree.gameObject.SetActive(show);//!

        if (moduleInfoText != null)
        {
            moduleInfoText.gameObject.SetActive(!show);
        }
    }

    public string Platform
    {
        get
        {
            return prefs.ValueString("platform", "ChartBoost"); ;
        }
    }

    public bool ShowOnAfterBattleScreen
    {
        get { return prefs.ValueBool("afterBattleScreen/show"); }
    }

    public bool ShowOnDeliveryScreen = false;
    //{
        
        //get { return prefs.ValueBool("deliveryScreen/show"); }
   // }

    public int TimeToShowOnDeliveryScreen
    {
        get { return prefs.ValueInt("deliveryScreen/timeToShow"); }
    }

    private void OnRewardedVideoFailedToLoad()
    {
    }

    private void OnRewardedVideoFinished()//!
    {
        var request = Http.Manager.Instance().CreateRequest("/ads/watching/finished");
        request.Form.AddField("screen", state == State.AfterBattle ? "afterBattle" : "delivery");

        Http.Manager.StartAsyncRequest(request, delegate (Response result)
        {
            if (state == State.Delivery)
            {
                ModuleShop.Instance.ModuleReceived(Shop.CurrentVehicle, ModuleShop.Instance.ModuleInView.type);
            }
            else
            {
                btnAfterBattleScreen.gameObject.SetActive(false);
            }
        });
    }

    private void OnRewardedVideoShown()//!
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
