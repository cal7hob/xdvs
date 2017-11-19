using System;
using System.Collections.Generic;
using Http;
using UnityEngine;
using System.Linq;


[Serializable]
public class VipPrice
{
    [Serializable]
    public class StoreId
    {
        [SerializeField] private Store store;
        [SerializeField] private string id;

        public Store Store { get { return store; } }
        public string Id { get { return id; } }
    }

    [SerializeField] private string unibillerId;
    [SerializeField] private int vipDurationDays;
    [SerializeField] private StoreId[] storeIds;

    public string UnibillerId { get { return unibillerId; } }
    public int VipDurationDays { get { return vipDurationDays; } }
    public StoreId[] StoreIds { get { return storeIds; }}
}


public class VipManager : MonoBehaviour
{
    private const string BUY_BUTTON_NAME_STRING = "BuyVipButton";
    private const float TRANSPARENCY_RATE = 0.3f;
    public Transform btnBuyVip;

    public Dictionary<string, VipOfferPrefab> vipOffers;

    public VipPrice[] VipPrices;
    public GameObject[] VipIcons;
    public tk2dTextMesh[] TimeLabels;
    public GameObject[] CommonIcons;
    public tk2dBaseSprite[] VipDimmedIcons;
    public GameObject VipShopContainer;
    public VipOfferPrefab VipOfferPrefab;
    public tk2dUIScrollableArea VipShopScrollableArea;
    public tk2dTextMesh lblBuyVip;

    public static VipManager Instance
    {
        get { return _instance ?? new VipManager(); }
    }

    public static bool IsHangarReloadRequired { get; set; }

    public static float VipExpRate
    {
        get { return ProfileInfo.VipExpRate; }
    }

    public static float VipSilverRate
    {
        get { return ProfileInfo.VipSilverRate; }
    }

    public static bool IsPlayerVip
    {
        get { return ProfileInfo.IsPlayerVip; }
    }

    public static bool LastSessionVipStatus
    {
        get { return ProfileInfo.LastSessionVipStatus; }
        set { ProfileInfo.LastSessionVipStatus = value; }
    }

    public long ExpirationTime
    {
        get { return _expirationTime; }
        private set { _expirationTime = value < 0 ? 0 : value; }
    }

    public bool IsInitialized { get; private set; }

    private long _expirationTime;

    public string ExpirationString { get; private set; }

    private bool _isTimerTicking;

    private bool _isFirstVipPurchase;

    private static VipManager _instance;

    private void EvaluateExpirationTime()
    {
        _isTimerTicking = true;
        if (ExpirationTime > 0)
        {
            // get expiration string
            ExpirationTime--;
            ExpirationString = Clock.GetTimerString(ExpirationTime);
            // apply expiration string
            foreach (var label in TimeLabels)
            {
                if (label == null) continue;
                label.text = Localizer.GetText("lblVipTimer", ExpirationString);
            }
            // repeate evaluation in a second
            this.InvokeRepeating(EvaluateExpirationTime, 1, 0);
            return;
        }
        // update vip status
        ProfileInfo.IsPlayerVip = _isTimerTicking = false;
        UpdateVipStatus(false);
    }

    void Awake()
    {
        _instance = this;

        Dispatcher.Subscribe(EventId.VipStatusUpdated, VipStatusUpdated_Handler);
        Dispatcher.Subscribe(EventId.ProfileInfoLoadedFromServer, VipStatusUpdated_Handler);
        Dispatcher.Subscribe(EventId.PageChanged, PageChanged_Handler);
        Dispatcher.Subscribe(EventId.OnLanguageChange, LanguageChanged_Handler);
        Dispatcher.Subscribe(EventId.AfterHangarInit, VipManagerInit);

        ExpirationTime = ProfileInfo.VipExpirationDate - (int)GameData.CorrectedCurrentTimeStamp;
        Dispatcher.Send(EventId.VipStatusUpdated, new EventInfo_B(ProfileInfo.IsPlayerVip));
    }

    void Start()
    {
        // localizing lblBuyVip
        UpdateL10NAgents();
    }

    void OnDestroy()
    {
        Dispatcher.Unsubscribe(EventId.VipStatusUpdated, VipStatusUpdated_Handler);
        Dispatcher.Unsubscribe(EventId.ProfileInfoLoadedFromServer, VipStatusUpdated_Handler);
        Dispatcher.Unsubscribe(EventId.PageChanged, PageChanged_Handler);
        Dispatcher.Unsubscribe(EventId.OnLanguageChange, LanguageChanged_Handler);
        Dispatcher.Unsubscribe(EventId.AfterHangarInit, VipManagerInit);

        IapManager.Instance.OnReady -= SetVipOffers;

        _instance = null;
    }

    private void VipManagerInit(EventId id, EventInfo info)
    {
#if !(UNITY_WEBGL || UNITY_WEBPLAYER)
        if (IapManager.IsInitialized() && !IsInitialized)
        {
            SetVipOffers();
        }
        else
        {
            IapManager.Instance.OnReady += SetVipOffers;
        }
#else
        SetVipOffers();
#endif
    }

    private void PageChanged_Handler(EventId eventId, EventInfo eventInfo)
    {
        btnBuyVip.gameObject.SetActive(!IsPlayerVip && GUIPager.ActivePageName == "MainMenu");

        // hide vip icon on current buying box
        if (HangarController.Instance != null && HangarController.CurrentActionBox != null)
            HangarController.CurrentActionBox.IsProductVip = false;
        // clear expiration time after battle (if vip was expired)
        if (ProfileInfo.IsPlayerVip && ExpirationTime == 0)
        {
            ProfileInfo.IsPlayerVip = false;
            ExpirationTime = 0;
            ExpirationString = Clock.GetTimerString(ExpirationTime);
            // apply expiration string
            foreach (var label in TimeLabels)
            {
                if (label == null) continue;
                label.text = Localizer.GetText("lblVipTimer", ExpirationString);
            }
        }
        // update expiration string
        if (GUIPager.ActivePageName.Contains("VipShop"))
        {
            if (ExpirationTime > 0)
            {
                // get expiration string
                ExpirationTime--;
                ExpirationString = Clock.GetTimerString(ExpirationTime);
                // apply expiration string
                foreach (var label in TimeLabels)
                {
                    if (label == null) continue;
                    label.text = Localizer.GetText("lblVipTimer", ExpirationString);
                }
            }
        }
    }

    private void LanguageChanged_Handler(EventId id, EventInfo info)
    {
        UpdateL10NAgents();
    }

    private void UpdateL10NAgents()
    {
        lblBuyVip.text = Localizer.GetText("lblBuy") + " VIP";
        lblBuyVip.Commit();
    }

    private void VipStatusUpdated_Handler(EventId eventId, EventInfo eventInfo)
    {
        if (ProfileInfo.VipExpirationDate <= GameData.CorrectedCurrentTimeStamp)
            return;
        // get new expiration time
        ExpirationTime = ProfileInfo.VipExpirationDate - (int)GameData.CorrectedCurrentTimeStamp;
        // update vip icons
        if ((eventInfo as EventInfo_B) != null)
            UpdateVipStatus(ProfileInfo.IsPlayerVip);
        // check if quests need to be reinitialized
        if (LastSessionVipStatus != IsPlayerVip && HangarController.FirstEnter)
        {
            LastSessionVipStatus = IsPlayerVip;
        }
    }

    /// <summary>
    /// start timer and enable all vip icons if new status == Vip
    /// </summary>
    /// <param name="newStatus">new player vip status</param>
    public void UpdateVipStatus(bool newStatus)
    {
        // start vip timer evaluation
        if (newStatus && !_isTimerTicking)
            EvaluateExpirationTime();

        // show/hide vip icons
        foreach (var icon in VipIcons)
            if(icon != null)
                icon.SetActive(newStatus);
        // hide/show common icons
        foreach (var commonIcon in CommonIcons)
            if (commonIcon != null)
                commonIcon.SetActive(!newStatus);

        // make transparent/opaque vip icons
        foreach (tk2dBaseSprite icon in VipDimmedIcons)
        {
            if(icon != null)
                icon.color = new Color(
                    icon.color.r,
                    icon.color.g,
                    icon.color.b,
                    newStatus ? 1f : TRANSPARENCY_RATE);
        }

        // update fuel
        if (!newStatus && !ProfileInfo.vehicleUpgrades.ContainsKey(GameData.EXTRA_FUEL_VEHICLE_ID) &&
            ProfileInfo.MaxFuel == GameData.MAX_GAME_FUEL_AMOUNT)
        {
            ProfileInfo.MaxFuel = GameData.STANDART_FUEL_CAN_AMOUNT;
            if (ProfileInfo.Fuel > ProfileInfo.MaxFuel)
                ProfileInfo.Fuel = ProfileInfo.MaxFuel;
        }
    }

    public void OnVipPurchased()
    {
        _isFirstVipPurchase = !ProfileInfo.IsPlayerVip;
        OnVipPurchaseSucceeded();
    }

    //private void VipPurchaseFailCallback(Response result)
    //{
    //    Debug.LogError("): Vip purchase failed.");
    //    // not enough currency server responce -> go to bank
    //    if (result.text.Contains("\"error\":3000"))
    //        GUIPager.SetActivePage("Bank", true, true);
    //    // hide waiting indicator
    //    XdevsSplashScreen.SetActiveWaitingIndicator(false);
    //}

    private void OnVipPurchaseSucceeded()
    {
        Dispatcher.Send(EventId.VipAccountPurchased, new EventInfo_SimpleEvent());

        Debug.LogWarning("Congratulations! You are VIP now.");
        // hide waiting indicator
        XdevsSplashScreen.SetActiveWaitingIndicator(false);

        if (_isFirstVipPurchase)
        {
            ProfileInfo.MaxFuel = GameData.MAX_GAME_FUEL_AMOUNT;
            ProfileInfo.Fuel = GameData.MAX_GAME_FUEL_AMOUNT;
            IsHangarReloadRequired = true;
            // reload hangar
            Loading.gotoLoadingScene();
        }
    }

    public static VipPrice.StoreId GetCurrentStoreId(VipPrice vipPrice)
    {
        return vipPrice.StoreIds.FirstOrDefault(item => item.Store == IapManager.Instance.CurrentStore);
    }

    public static VipPrice.StoreId GetCurrentStoreId(string storeIndependentId)
    {
        VipPrice vipPrice = GetVipPriceByStoreIndependentId(storeIndependentId);
        if (vipPrice == null)
            return null;
        return vipPrice.StoreIds.FirstOrDefault(item => item.Store == IapManager.Instance.CurrentStore);
    }

    public static VipPrice GetVipPriceByStoreIndependentId(string storeIndependentId)
    {
        if (Instance == null)
            return null;
        return Instance.VipPrices.FirstOrDefault(item => item.UnibillerId == storeIndependentId);
    }

    /// <summary>
    /// instantiate all offers in vip account shop
    /// </summary>
    /// <param name="offers">vip account offers collection</param>
    public void SetVipOffers()
    {
        // check container and it's components
        if (VipShopContainer == null)
            throw new NullReferenceException("Vip shop container was not set!");
        if (VipOfferPrefab.GetComponent<VipOfferPrefab>() == null)
            throw new NullReferenceException("No VipOfferPrefab component was found in vip shop container");

        VipShopContainer.SetActive(true);

        vipOffers = new Dictionary<string, VipOfferPrefab>(4);

        VipShopScrollableArea.ContentLength = 100;
        // instantiate offers
        Dictionary<int,int> durationToNum = new Dictionary<int,int>{{1,0},{3,1},{7,2},{30,3}};
        int offersCounter = 0; //для определения позиции в scrollable area
        for (int i = 0; i < VipPrices.Length; i++)
        {
            if (VipPrices[i].UnibillerId == "xdevs.vip_kit")
                continue;

            var currentStoreId = GetCurrentStoreId(VipPrices[i]);

            if (currentStoreId == null)
                continue;
                
            // instantiate new button
            int num = durationToNum[VipPrices[i].VipDurationDays];
            VipOfferPrefab.expirationLable.name = "lblVipOfferTime" + num;
            VipOfferPrefab offerItem = Instantiate(VipOfferPrefab) as VipOfferPrefab;
            // set button parameters
            vipOffers.Add(currentStoreId.Id, offerItem);
            //offerItem.localizedPrice.name = "lblBuyVip" + num;
            offerItem.OfferUnibillerId = currentStoreId.Id;
            offerItem.localizedPrice.text = PriceLocalizationAgent.GetLocalizedString(currentStoreId.Id, offerItem.localizedPrice, "lblBuyVip"+num);
            offerItem.ShopPosition = offersCounter++;
            offerItem.VipDurationDays = VipPrices[i].VipDurationDays;
            // set objet visible
            offerItem.gameObject.SetActive(true);
            // insert button inside vip shop container
            offerItem.transform.SetParent(VipShopContainer.transform);
            offerItem.UpdateOfferInstance();
            // set scrollable area new size
            VipShopScrollableArea.ContentLength += offerItem.OfferWidth;
        }

        IsInitialized = true;
        Dispatcher.Send(EventId.VipOffersInstantiated, new EventInfo_SimpleEvent());
    }

    public void GoToNeededOffer(string unibillerId)
    {
        var storeId = GetCurrentStoreId(unibillerId);

        if (storeId == null || !vipOffers.ContainsKey(storeId.Id))
            return;

        var vipOffer = vipOffers[storeId.Id];

        VipShopScrollableArea.Value
            = Mathf.Clamp01((vipOffer.transform.localPosition.x - VipShopScrollableArea.VisibleAreaLength / 2f) /
              (VipShopScrollableArea.ContentLength - VipShopScrollableArea.VisibleAreaLength));
    }
}