using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class VipShopPage : MonoBehaviour
{
    public tk2dBaseSprite icoVipFuelBar;
    public tk2dBaseSprite icoVipUserLevel;
    public FuelBarManager fuelBarManagerFromTopPanel;
    public tk2dTextMesh lblVipTimer;
    public UniAlignerBase headerAligner;
    public SimpleItemsPanel vipsPanel;

    private List<tk2dBaseSprite> blinkingSprites = new List<tk2dBaseSprite>();
    private List<IEnumerator> blinkingRoutines = new List<IEnumerator>();
    private List<Color> initialColors = new List<Color>();
    private bool isInited = false;

    public static VipShopPage Instance { get; private set; }
    public static bool IsOnScreen { get; private set; }
    public bool IsInitialized { get; private set; }

    void Awake()
    {
        Instance = this;

        Dispatcher.Subscribe(EventId.PageChanged, OnPageChanged);
        Dispatcher.Subscribe(EventId.VipStatusUpdated, OnVipStatusUpdated);
        Dispatcher.Subscribe(EventId.AfterHangarInit, InitVipOffers);
        InitShopPage();
    }

    public void SetVipOffers()
    {
        VipManager.Instance.vipOffers = new Dictionary<string, VipOfferPrefab>(4);

        Dictionary<int, int> durationToNum = new Dictionary<int, int> { { 1, 0 }, { 3, 1 }, { 7, 2 }, { 30, 3 } };
        var vipPrices = VipManager.Instance.VipPrices;

        for (int i = 0; i < vipPrices.Length; i++)
        {
            if (vipPrices[i].IapId == "xdevs.vip_kit")
                continue;

            var currentStoreId = VipManager.GetCurrentStoreId(vipPrices[i]);

            if (currentStoreId == null)
                continue;

            var vipOffer = vipsPanel.CreateItem<VipOfferPrefab>();
            VipManager.Instance.vipOffers.Add(currentStoreId.Id, vipOffer);
            var localizedPrice = PriceLocalizationAgent.GetLocalizedString(currentStoreId.Id, vipOffer.localizedPrice);
            vipOffer.Initialize(durationToNum[vipPrices[i].VipDurationDays], currentStoreId.Id, vipPrices[i].VipDurationDays, localizedPrice);
        }

        vipsPanel.ScrollableItemsBehaviour.UpdateContentLength();

        IsInitialized = true;
        Dispatcher.Send(EventId.VipOffersInstantiated, new EventInfo_SimpleEvent());
    }

    public void GoToNeededOffer(string iapId)
    {
        var storeId = VipManager.GetCurrentStoreId(iapId);

        if (storeId == null || !VipManager.Instance.vipOffers.ContainsKey(storeId.Id))
            return;

        var vipOffer = VipManager.Instance.vipOffers[storeId.Id];
        var scrollArea = vipsPanel.ScrollableItemsBehaviour.ScrollableArea;

        scrollArea.Value
            = Mathf.Clamp01((vipOffer.transform.localPosition.x - scrollArea.VisibleAreaLength * 0.5f) /
              (scrollArea.ContentLength - scrollArea.VisibleAreaLength));
    }

    private void InitVipOffers(EventId id, EventInfo info)
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

    private void InitShopPage()
    {
        if (isInited)
            return;
        //Весь этот гемор с функцией инициализации пришлось сделать из-за того, что мы обращаемся к fuelBarManagerFromTopPanel
        //до его Awake. Это происходит например при дейли бонусе - когда верхняя панель отключена
        List<tk2dBaseSprite> list = fuelBarManagerFromTopPanel.GetFuelBonusBars();
        if (list == null)
            return;

        blinkingSprites.AddRange(fuelBarManagerFromTopPanel.GetFuelBonusBars());
        blinkingSprites.Add(icoVipFuelBar);
        blinkingSprites.Add(icoVipUserLevel);

        //Сохраняем начальное состояние спрайтов
        foreach (var spr in blinkingSprites)
            initialColors.Add(spr.color);

        isInited = true;
    }

    private void OnDestroy()
    {
        Instance = null;

        Dispatcher.Unsubscribe(EventId.VipStatusUpdated, OnVipStatusUpdated);
        Dispatcher.Unsubscribe(EventId.PageChanged, OnPageChanged);
        Dispatcher.Unsubscribe(EventId.AfterHangarInit, InitVipOffers);

        IapManager.Instance.OnReady -= SetVipOffers;
    }

    private void OnVipStatusUpdated(EventId eventId, EventInfo eventInfo)
    {
        UpdateHeader();
    }

    private void OnPageChanged(EventId eventId, EventInfo eventInfo)
    {
        if (GUIPager.ActivePage.Contains("VipAccountShop"))
        {
            InitShopPage();
            IsOnScreen = true;
            UpdateHeader();
            if (ProfileInfo.IsPlayerVip)
                return;

            //Создаем корутниы мигания и запускаем их
            blinkingRoutines.Clear();
            foreach (var spr in blinkingSprites)
            {
                IEnumerator routine = MiscTools.BlinkingRoutine(spr, 1);
                blinkingRoutines.Add(routine);
                StartCoroutine(routine);
            }
        }
        else
        {
            IsOnScreen = false;

            if (!isInited || ProfileInfo.IsPlayerVip)
                return;

            //Останавливаем корутины и возвращаем начальное состояние спрайтов
            if (blinkingRoutines.Count > 0)
            {
                foreach (var routine in blinkingRoutines)
                    StopCoroutine(routine);
                for (int i = 0; i < blinkingSprites.Count; i++)
                    blinkingSprites[i].color = initialColors[i];
            }
        }
    }

    private void UpdateHeader()
    {
        if (lblVipTimer)//Сам лейбл устанавливается скриптом VipManager
            lblVipTimer.gameObject.SetActive(ProfileInfo.IsPlayerVip);
        if (headerAligner != null)
            headerAligner.Align();
    }
}
