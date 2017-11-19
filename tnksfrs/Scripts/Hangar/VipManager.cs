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

    /// <summary>
    /// icons to show for vip account
    /// </summary>
    public GameObject[] VipIcons;

    /// <summary>
    /// icons to hide for vip account
    /// </summary>
    public GameObject[] CommonIcons;

    /// <summary>
    /// gameobject that should contain Vip account offers (buttons)
    /// </summary>
    public GameObject VipShopContainer;

    /// <summary>
    /// Prefab to instatntiate offer.
    /// It MUST contain VipOfferPrefab component
    /// </summary>
    public VipOfferPrefab VipOfferPrefab;

    /// <summary>
    /// current vip manager instance
    /// </summary>
    public static VipManager Instance
    {
        get { return _instance ?? new VipManager(); }
    }

    /// <summary>
    /// set when vip account was purchased first time
    /// </summary>
    public static bool IsHangarReloadRequired { get; set; }

    /// <summary>
    /// after battle expirience multiplier for vip
    /// </summary>
    public static float VipExpRate
    {
        get { return ProfileInfo.VipExpRate; }
    }

    /// <summary>
    /// after battle earned silver multiplier for vip
    /// </summary>
    public static float VipSilverRate
    {
        get { return ProfileInfo.VipSilverRate; }
    }

    /// <summary>
    /// player vip flag
    /// </summary>
    public static bool IsPlayerVip
    {
        get { return ProfileInfo.IsPlayerVip; }
    }

    /// <summary>
    /// last session vip status (to reset quests if vip was expired)
    /// </summary>
    public static bool LastSessionVipStatus
    {
        get { return ProfileInfo.LastSessionVipStatus; }
        set { ProfileInfo.LastSessionVipStatus = value; }
    }

    /// <summary>
    /// time in seconds till the end of vip account rent
    /// </summary>
    public long ExpirationTime
    {
        get { return _expirationTime; }
        private set { _expirationTime = value < 0 ? 0 : value; }
    }

    public bool IsInitialized { get; private set; }

    private long _expirationTime;
    /// <summary>
    /// represents expiration time
    /// </summary>
    public string ExpirationString { get; private set; }

    /// <summary>
    /// this flag doesn't allow to run vip timer twice
    /// </summary>
    private bool _isTimerTicking;

    private bool _isFirstVipPurchase;

    private static VipManager _instance;

    /// <summary>
    /// vip timer counter, updates expiration string
    /// </summary>
    private void EvaluateExpirationTime()
    {
        _isTimerTicking = true;
        if (ExpirationTime > 0)
        {
            // get expiration string
            ExpirationTime--;
            ExpirationString = Clock.GetTimerString(ExpirationTime);
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
        btnBuyVip.gameObject.SetActive(!IsPlayerVip && GUIPager.ActivePage == "MainMenu");

        // clear expiration time after battle (if vip was expired)
        if (ProfileInfo.IsPlayerVip && ExpirationTime == 0)
        {
            ProfileInfo.IsPlayerVip = false;
            ExpirationTime = 0;
            ExpirationString = Clock.GetTimerString(ExpirationTime);
            
        }
        // update expiration string
        if (GUIPager.ActivePage.Contains("VipShop"))
        {
            if (ExpirationTime > 0)
            {
                // get expiration string
                ExpirationTime--;
                ExpirationString = Clock.GetTimerString(ExpirationTime);
                
            }
        }
    }

    private void LanguageChanged_Handler(EventId id, EventInfo info)
    {
        UpdateL10NAgents();
    }

    private void UpdateL10NAgents()
    {
       
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
        Debug.LogWarning("Congratulations! You are VIP now.");
        // hide waiting indicator
        XdevsSplashScreen.SetActiveWaitingIndicator(false);

        if (_isFirstVipPurchase)
        {
            IsHangarReloadRequired = true;
            // reload hangar
            //Loading.GoToLoadingScene();
        }
    }

    public static VipPrice.StoreId GetCurrentStoreId(VipPrice vipPrice)
    {
        return vipPrice.StoreIds.FirstOrDefault(item => item.Store == IapManager.Instance.CurrentStore);
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

        // instantiate offers
		Dictionary<int,int> durationToNum = new Dictionary<int,int>{{1,0},{3,1},{7,2},{30,3}};
		int offersCounter = 0;
        foreach (var offer in VipPrices)
        {
            var currentStoreId = GetCurrentStoreId(offer);

			if (currentStoreId == null)
                continue;
				
			// instantiate new button
			int num = durationToNum[offer.VipDurationDays];
            var offerPrefab = Instantiate(VipOfferPrefab);
            VipOfferPrefab offerInstance = offerPrefab.GetComponent<VipOfferPrefab>();
            // set button parameters
			vipOffers.Add(currentStoreId.Id, offerPrefab);
			offerInstance.OfferUnibillerId = currentStoreId.Id;
            offerInstance.ShopPosition = offersCounter;
            offerInstance.VipDurationDays = offer.VipDurationDays;
            // set objet visible
            offerPrefab.gameObject.SetActive(true);
            // insert button inside vip shop container
            offerPrefab.transform.SetParent(VipShopContainer.transform);
            offerInstance.UpdateOfferInstance();
            // set scrollable area new size
			offersCounter++;
        }

        IsInitialized = true;
        Dispatcher.Send(EventId.VipOffersInstantiated, new EventInfo_SimpleEvent());
    }

    /// <summary>
    /// purchase vip account click handler
    /// </summary>
    /// <param name="sender">offer sender button</param>
    //private void BuyButtonClicked_Handler(tk2dUIItem sender)
    //{
        //// get offer id
        //int offerId = int.Parse(sender.name.Remove(0, BUY_BUTTON_NAME_STRING.Length));
        //// check if not enough currency

        ////смотрим со скидкой он или нет
        //var vipDuration = VipOffersController.Instance.CheckIfItemOnSale(offerId)
        //    ? VipOffersController.Instance.Offers[offerId].DurationDays.ToString()
        //    : VipAccountOffers[offerId - 1].DurationInDays.ToString();



        //#region Google Analytics: VIP account buying failure "not enough money"

        //GoogleAnalyticsWrapper.LogEvent(
        //    new CustomEventHitBuilder()
        //        .SetParameter(GAEvent.Category.VIPAccountBuying)
        //        .SetParameter(GAEvent.Action.NotEnoughMoney)
        //        .SetSubject(GAEvent.Subject.VIPOfferID, VipAccountOffers[offerId - 1].Id)
        //        .SetParameter<GAEvent.Label>()
        //        .SetSubject(GAEvent.Subject.PlayerLevel, ProfileInfo.Level));

        //#endregion

        //#region Google Analytics: VIP account bought

        //GoogleAnalyticsWrapper.LogEvent(
        //    new CustomEventHitBuilder()
        //        .SetParameter(GAEvent.Category.VIPAccountBuying)
        //        .SetParameter(GAEvent.Action.Bought)
        //        .SetSubject(GAEvent.Subject.VIPOfferID, VipAccountOffers[offerId - 1].Id)
        //        .SetParameter<GAEvent.Label>()
        //        .SetSubject(GAEvent.Subject.PlayerLevel, ProfileInfo.Level));

        //#endregion

        //#region Google Analytics: VIP account buying canceled

        //GoogleAnalyticsWrapper.LogEvent(
        //    new CustomEventHitBuilder()
        //        .SetParameter(GAEvent.Category.VIPAccountBuying)
        //        .SetParameter(GAEvent.Action.Cancelled)
        //        .SetSubject(GAEvent.Subject.VIPOfferID, VipAccountOffers[offerId - 1].Id)
        //        .SetParameter<GAEvent.Label>()
        //        .SetSubject(GAEvent.Subject.PlayerLevel, ProfileInfo.Level));

        //#endregion
    //}
}

/*

"Shop": {
      "VIPList": [
        {
          "id": 1,
          "duration": 360,
          "price": {
            "currency": "gold",
            "value": 100
          }
        }
      ]
    }

 *
    request: shop/buyVip‏
 *
*/