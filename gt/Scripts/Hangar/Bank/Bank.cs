using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
#if !(UNITY_WSA || UNITY_WEBGL)
using TapjoyUnity;
#endif
using UnityEngine;

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using DG.Tweening;
#if !(UNITY_WSA || UNITY_WEBGL)
using TapjoyUnity;
#endif
using UnityEngine;

public class Bank : MonoBehaviour
{
    //!!!!! Порядок ссылок в tk2dUIToggleButtonGroup должен быть такой же как в этом енаме !!!!!
    public enum Tab
    {
        Silver = 0,
        Gold = 1,
        Vip = 2,
        Kits = 3
    }

    public GameObject goldPage;
    public GameObject silverPage;
    public GameObject kitsPage;
    public tk2dUIScrollableArea goldScrollArea;
    public tk2dUIScrollableArea silverScrollArea;
    public tk2dUIScrollableArea kitsScrollArea;
    public tk2dUIItem goldMenuBtn;
    public tk2dUIItem silverMenuBtn;
    public tk2dUIItem oneTimeOfferMenuBtn;
    public tk2dTextMesh lblAdsFree;
    public tk2dUIToggleButtonGroup tabsGroup;

    [SerializeField]
    private GameObject tapJoyOfferPrefab;
    [SerializeField]
    private GameObject freeTankOfferPrefab;

    [Header("Frame prefab settings:")]
    [SerializeField]
    private BankLot bankLotPrefab;
    [SerializeField]
    private float framesPosY;
    [SerializeField]
    private Vector2 frameDimensions;
    [SerializeField]
    private float framesOffset;

    [Header("Kits")]
    [SerializeField]
    private tk2dTextMesh[] vipKitPriceLabels;
    [SerializeField]
    private tk2dTextMesh[] goldKitPriceLabels;
    [SerializeField]
    private tk2dTextMesh[] vehicleKitPriceLabels;
    [SerializeField]
    private tk2dTextMesh lblOneTimeOfferVIP30DaysDetails;
    [SerializeField]
    private tk2dTextMesh lblOneTimeOfferGoldSubscriptionDetails;
    [SerializeField]
    private tk2dTextMesh lblOneTimeOfferGoldSubscriptionDetails1;
    [SerializeField]
    private tk2dTextMesh lblOneTimeOfferGoldSubscriptionDetails2;
    [SerializeField]
    private tk2dTextMesh lblOneTimeOfferGoldSubscriptionDetailsAwardAmount;

    [SerializeField]
    private tk2dTextMesh lblOneTimeOfferStarterPackDetails;
    [SerializeField]
    private tk2dTextMesh vehicleKitDetails_Vehicle;
    [SerializeField]
    private tk2dBaseSprite vehicleKitDetails_PatternImage;
    [SerializeField]
    private tk2dTextMesh vehicleKitDetails_PatternStats;
    [SerializeField]
    private tk2dBaseSprite vehicleKitDetails_DecalImage;
    [SerializeField]
    private tk2dTextMesh vehicleKitDetails_DecalStats;

    public List<Transform> goldPanelTransforms = new List<Transform>();
    public List<Transform> silverPanelTransforms = new List<Transform>();
    private List<Transform> kitTransforms = new List<Transform>();
    private Dictionary<BankKits.Type, Dictionary<string, BankKits.Data>> activeKits = new Dictionary<BankKits.Type, Dictionary<string, BankKits.Data>>(4);
    private Dictionary<BankKits.Type, Dictionary<string, BankKits.Data>> tempActiveKits = new Dictionary<BankKits.Type, Dictionary<string, BankKits.Data>>(4);
    
    private bool isInitialized;

    private GameObject tapjoyGold;
    private GameObject tapjoySilver;
    private GameObject freeTankGold;
    private GameObject freeTankSilver;

    private static int counterForShowingNewbieKitsAfterBattle = 0;
    private static bool ItsTimeToShowNewbieKits { get { return counterForShowingNewbieKitsAfterBattle >= GameData.newbieKitsAfterBattleShowingRate; } }

    public static GameObject FreeTankGold { get { return Instance.freeTankGold; } }
    public static GameObject FreeTankSilver { get { return Instance.freeTankSilver; } }

    public bool IsInitialized
    {
        get { return isInitialized; }
        private set { isInitialized = value; }
    }

    public static Bank Instance { get; private set; }

    void Awake()
    {
        Dispatcher.Subscribe(EventId.AfterHangarInit, BankInit);
        Dispatcher.Subscribe(EventId.AfterHangarInit, SetAdsFreeText);
        Dispatcher.Subscribe(EventId.OnLanguageChange, OnLanguageChange);
        Dispatcher.Subscribe(EventId.ProfileInfoLoadedFromServer, OnLanguageChange);
        Dispatcher.Subscribe(EventId.BankInitialized, OnBankInitialized);

        Instance = this;
    }

    void OnDestroy()
    {
        Dispatcher.Unsubscribe(EventId.AfterHangarInit, BankInit);
        Dispatcher.Unsubscribe(EventId.AfterHangarInit, SetAdsFreeText);
        Dispatcher.Unsubscribe(EventId.OnLanguageChange, OnLanguageChange);
        Dispatcher.Unsubscribe(EventId.ProfileInfoLoadedFromServer, OnLanguageChange);
        Dispatcher.Unsubscribe(EventId.HangarTimerTick, OnHangarTimerTick);
        Dispatcher.Unsubscribe(EventId.BankInitialized, OnBankInitialized);
        IapManager.Instance.OnReady -= CreateBankFrames;

        foreach (var pair in BankData.prices)//При уходе из ангара обнуляем ссылки на лоты банка
            pair.Value.bankLot = null;

        Instance = null;
    }

    public void SetBankLotsOfGivenCurrency(ProfileInfo.PriceCurrency currency)
    {
        foreach (var lot in BankData.pricesSortedByCurrency[currency])
            if (lot.bankLot)
                lot.bankLot.Init();
    }

    
    public void SetNeededBankLot(ProfileInfo.PriceCurrency currency, string unibillId) // TODO: сделать что-то такое для разовых предложений, т.е. открывать окно после боя.
    {
        Transform neededBankLot = null;
        tk2dUIScrollableArea currentScrollableArea = null;

        if (currency == ProfileInfo.PriceCurrency.Gold)
        {
            goldMenuBtn.SimulateClick();
            neededBankLot = goldScrollArea.contentContainer.transform.Find(unibillId);
            currentScrollableArea = goldScrollArea;
        }
        else if (currency == ProfileInfo.PriceCurrency.Silver)
        {
            silverMenuBtn.SimulateClick();
            neededBankLot = silverScrollArea.contentContainer.transform.Find(unibillId);
            currentScrollableArea = silverScrollArea;
        }
      if (neededBankLot != null) NeededBankLotToView(neededBankLot, currentScrollableArea);
    }

    public static void SetActiveBankLot(tk2dUIScrollableArea scrollArea, IList<Transform> lotTransforms, int lotIndex, bool activate)
    {
        if (lotTransforms.Count == 0)
            return;

        if (lotTransforms[lotIndex].gameObject.activeSelf != activate)
        {
            lotTransforms[lotIndex].gameObject.SetActive(activate);
            Instance.FramesReposition(lotTransforms, scrollArea);
        }
    }

    public static void SetActiveBankLot(tk2dUIScrollableArea scrollArea, IList<Transform> lotTransforms, GameObject lot, bool activate)
    {
        if (lot.activeSelf != activate)
        {
            lot.SetActive(activate);
            Instance.FramesReposition(lotTransforms, scrollArea);
        }
    }

    public void OnBankModeChanged(tk2dUIToggleButtonGroup buttonGroup)
    {
        silverPage.SetActive(buttonGroup.SelectedIndex == (int)Tab.Silver);
        goldPage.SetActive(buttonGroup.SelectedIndex == (int)Tab.Gold);

        if (buttonGroup.SelectedIndex == (int)Tab.Vip)
            HangarController.Instance.NavigateToVipShop();

        if (kitsPage != null)
            kitsPage.SetActive(buttonGroup.SelectedIndex == (int)Tab.Kits);
    }

    private void NeededBankLotToView(Transform trans, tk2dUIScrollableArea scrollableArea)
    {
        scrollableArea.Value = 0;
        while (trans.position.x > 960 && scrollableArea.Value < 1)
        {
            scrollableArea.Value += 0.01f;
        }
    }

    private void BankInit(EventId id, EventInfo info)
    {
#if !(UNITY_WEBGL || UNITY_WEBPLAYER)

        if (IapManager.IsInitialized() && !IsInitialized)
        {
            CreateBankFrames();
        }
        else
        {
            IapManager.Instance.OnReady += CreateBankFrames;
        }
#else

        CreateBankFrames();

#endif

        lblAdsFree.gameObject.SetActive(!GameData.adsFree);
    }

    private static GameObject InstantiateFrame(GameObject prefab, string name, IList<Transform> transformsList)
    {
        GameObject bankFrame = Instantiate(prefab);
        bankFrame.name = name;
        transformsList.Add(bankFrame.transform);

        return bankFrame;
    }

    private static BankLot InstantiateFrame(BankLot prefab, string unibillId, IList<Transform> transformsList)
    {
        BankLot bankFrame = Instantiate(prefab);
        bankFrame.name = unibillId;
        transformsList.Add(bankFrame.transform);
        bankFrame.UnibillerItemId = unibillId;

        return bankFrame;
    }

    private static void SetFrameBtn(GameObject frame, Action<tk2dUIItem> action)
    {
        var uiItem = frame.GetComponent<tk2dUIItem>();
        uiItem.OnClickUIItem += item => action(uiItem);
    }

    public static void ShowTapJoyOfferWall(tk2dUIItem tk2dUiItem)
    {
#if (UNITY_ANDROID || UNITY_IOS) && !UNITY_EDITOR
        TapjoyHangar.BankPlacement.ShowContent();
#endif
    }

    public void CreateTapJoyFrames()
    {
        if (!TapjoyHangar.IsTapjoyeble)
            return;

        tapjoyGold = InstantiateFrame(tapJoyOfferPrefab, "TapJoyOfferGold", goldPanelTransforms);
        tapjoySilver = InstantiateFrame(tapJoyOfferPrefab, "TapJoyOfferSilver", silverPanelTransforms);

        tapjoyGold.transform.SetParent(goldScrollArea.contentContainer.transform);
        tapjoySilver.transform.SetParent(silverScrollArea.contentContainer.transform);

        SetFrameBtn(tapjoyGold, ShowTapJoyOfferWall);
        SetFrameBtn(tapjoySilver, ShowTapJoyOfferWall);

        ShowTapjoyFrames(TapjoyHangar.IsTapjoyInitialized);
    }

    public void ShowTapjoyFrames(bool activate)
    {
        SetActiveBankLot(goldScrollArea, goldPanelTransforms, tapjoyGold, activate);
        SetActiveBankLot(silverScrollArea, silverPanelTransforms, tapjoySilver, activate);
    }

    private void CreateFreeVehicleFrames()
    {
        if (VehicleOffersController.IsOwnFreeVehicle)
        {
            return;
        }

        freeTankGold = InstantiateFrame(freeTankOfferPrefab, "btnFreeTankDetails", goldPanelTransforms);
        freeTankSilver = InstantiateFrame(freeTankOfferPrefab, "btnFreeTankDetails", silverPanelTransforms);

        SetFrameBtn(freeTankGold, VehicleOffersController.Instance.ShowFreeVehicleDetails);
        SetFrameBtn(freeTankSilver, VehicleOffersController.Instance.ShowFreeVehicleDetails);
    }

    private void CreatePurchasableFrames(IList<BankItemPrice> bankItemPriceList, IList<Transform> lotTransforms)
    {
        for (int i = bankItemPriceList.Count - 1; i >= 0; i--)
        {
            var unibillId = bankItemPriceList[i].xdevsId;
            BankData.prices[unibillId].bankLot = InstantiateFrame(bankLotPrefab, unibillId, lotTransforms);
            BankData.prices[unibillId].bankLot.Init();
        }
    }

    private void FramesReposition(IEnumerable<Transform> lotTransforms, tk2dUIScrollableArea scrollArea)
    {
        var firstFramePos = new Vector3(frameDimensions.x * 0.5f + framesOffset, framesPosY, 0);

        int i = 0;
        foreach (var lotTransform in lotTransforms.Where(lotTransform => lotTransform.gameObject.activeSelf))
        {
            lotTransform.localPosition = Vector3.right * (framesOffset + frameDimensions.x) * i++ + firstFramePos;
        }

        if (i > 0)
        {
            MeasureScrollArea(scrollArea);
        }
    }

    private void MeasureScrollArea(tk2dUIScrollableArea scrollArea)
    {
        scrollArea.ContentLength = scrollArea.MeasureContentLength() + 2 * framesOffset;
    }

    private void CreateBankFrames()
    {
        CreateTapJoyFrames();
        CreateFreeVehicleFrames();

        CreatePurchasableFrames(BankData.pricesSortedByCurrency[ProfileInfo.PriceCurrency.Gold], goldPanelTransforms);
        CreatePurchasableFrames(BankData.pricesSortedByCurrency[ProfileInfo.PriceCurrency.Silver], silverPanelTransforms);

        SetFramesParent(goldPanelTransforms, goldScrollArea.contentContainer.transform);
        SetFramesParent(silverPanelTransforms, silverScrollArea.contentContainer.transform);

        FramesReposition(goldPanelTransforms, goldScrollArea);
        FramesReposition(silverPanelTransforms, silverScrollArea);

        ReсreateKitsIfNeeded();
        UpdateKitsTabState();
        SetupKitsWindows();

        IsInitialized = true;

        Dispatcher.Subscribe(EventId.HangarTimerTick, OnHangarTimerTick);
        Dispatcher.Send(EventId.BankInitialized, new EventInfo_SimpleEvent());
    }

    private void SetupKitsWindows()
    {
        //Присваиваем ценники вне банка (в окнах описания одноразовых наборов)
#if UNITY_WEBGL || UNITY_WEBPLAYER
        UpdatePriceLabels();

#else
        if (IapManager.IsInitialized())//Все равно мы не можем зайти в банк, если он не инициализирован
        {
            UpdatePriceLabels();
        }
#endif

        if (activeKits.ContainsKey(BankKits.Type.Newbie))
        {
            if (activeKits[BankKits.Type.Newbie].ContainsKey("xdevs.gold_kit"))
            {
                lblOneTimeOfferGoldSubscriptionDetailsAwardAmount.text = activeKits[BankKits.Type.Newbie]["xdevs.gold_kit"].displayedAmount.ToString();
                lblOneTimeOfferGoldSubscriptionDetails1.text = Localizer.GetText("lblOneTimeOfferGoldSubscriptionDetails_1", BankKits.Data.GOLD_KIT_INAPP_CURRENCY_VAL.ToString());
                lblOneTimeOfferGoldSubscriptionDetails2.text = Localizer.GetText("lblOneTimeOfferGoldSubscriptionDetails_2", activeKits[BankKits.Type.Newbie]["xdevs.gold_kit"].dailyReward.ToString());
            }

            if (activeKits[BankKits.Type.Newbie].ContainsKey("xdevs.vehicle_kit") && activeKits[BankKits.Type.Newbie]["xdevs.vehicle_kit"].HasContent)
            {
                if (activeKits[BankKits.Type.Newbie]["xdevs.vehicle_kit"].content.ContainsKey(BankKits.Content.Vehicle))
                {
                    vehicleKitDetails_Vehicle.transform.parent.gameObject.SetActive(true);
                    VehicleInfo vehicleInfo = VehiclePool.Instance.GetItemById(activeKits[BankKits.Type.Newbie]["xdevs.vehicle_kit"].content[BankKits.Content.Vehicle]);
                    vehicleKitDetails_Vehicle.text = vehicleInfo.vehicleName;
                }
                else
                    vehicleKitDetails_Vehicle.transform.parent.gameObject.SetActive(false);

                if (activeKits[BankKits.Type.Newbie]["xdevs.vehicle_kit"].content.ContainsKey(BankKits.Content.Pattern))
                {
                    vehicleKitDetails_PatternStats.transform.parent.gameObject.SetActive(true);
                    Pattern pattern = PatternPool.Instance.GetItemById(activeKits[BankKits.Type.Newbie]["xdevs.vehicle_kit"].content[BankKits.Content.Pattern]);
                    if (pattern != null)
                    {
                        vehicleKitDetails_PatternStats.text = pattern.GetBonusesText();
                        vehicleKitDetails_PatternImage.SetSprite(pattern.IdString);
                    }
                    else
                        Debug.LogError("Can't show vehicle_kit's camo!");
                }
                else
                    vehicleKitDetails_PatternStats.transform.parent.gameObject.SetActive(false);

                if (activeKits[BankKits.Type.Newbie]["xdevs.vehicle_kit"].content.ContainsKey(BankKits.Content.Decal))
                {
                    vehicleKitDetails_DecalStats.transform.parent.gameObject.SetActive(true);
                    Decal decal = DecalPool.Instance.GetItemById(activeKits[BankKits.Type.Newbie]["xdevs.vehicle_kit"].content[BankKits.Content.Decal]);
                    if (decal != null)
                    {
                        vehicleKitDetails_DecalStats.text = decal.GetBonusesText();
                        vehicleKitDetails_DecalImage.SetSprite(decal.IdString);
                    }
                    else
                        Debug.LogError("Can't show vehicle_kit's decal!");
                }
                else
                    vehicleKitDetails_DecalStats.transform.parent.gameObject.SetActive(false);
            }
        }
    }

    private void UpdatePriceLabels()
    {
        //Вип кит может быть сделан подпиской, поэтому берем айдишник из вип манагера
        string vipKitIdString = "xdevs.vip_kit";
        VipPrice.StoreId currentStoreId = VipManager.GetCurrentStoreId(vipKitIdString);
        if (currentStoreId != null)
            vipKitIdString = currentStoreId.Id;

        HelpTools.SetTextToAllLabelsInCollection(vipKitPriceLabels,
            PriceLocalizationAgent.GetLocalizedString(vipKitIdString, vipKitPriceLabels[0], "lblBuyVIPKit"));
        lblOneTimeOfferVIP30DaysDetails.text = Localizer.GetText("lblOneTimeOfferVIP30DaysDetails",
            PriceLocalizationAgent.GetLocalizedString(vipKitIdString, lblOneTimeOfferVIP30DaysDetails, "lblBuyVIPKit"));

        HelpTools.SetTextToAllLabelsInCollection(goldKitPriceLabels,
            PriceLocalizationAgent.GetLocalizedString("xdevs.gold_kit", goldKitPriceLabels[0], "lblBuyGoldKit"));
        lblOneTimeOfferGoldSubscriptionDetails.text = Localizer.GetText("lblOneTimeOfferGoldSubscriptionDetails",
            PriceLocalizationAgent.GetLocalizedString("xdevs.gold_kit", lblOneTimeOfferGoldSubscriptionDetails, "lblBuyGoldKit"));
        HelpTools.SetTextToAllLabelsInCollection(vehicleKitPriceLabels,
            PriceLocalizationAgent.GetLocalizedString("xdevs.vehicle_kit", vehicleKitPriceLabels[0], "lblBuyVehicleKit"));
        lblOneTimeOfferStarterPackDetails.text = Localizer.GetText("lblOneTimeOfferStarterPackDetails",
            PriceLocalizationAgent.GetLocalizedString("xdevs.vehicle_kit", lblOneTimeOfferStarterPackDetails, "lblBuyVehicleKit"));
    }

    private static void SetFramesParent(IEnumerable<Transform> frames, Transform parent)
    {
        foreach (var frame in frames)
            frame.transform.SetParent(parent, false);
    }

    private void SetAdsFreeText(EventId id, EventInfo info)
    {
        lblAdsFree.text = Localizer.GetText("lblAdsFree", GameData.adsFreeDaysQuantity);
        if (GameData.adsFreeDaysQuantity == 0)
        {
            lblAdsFree.gameObject.SetActive(false);
        }
    }

    public static Bank.Tab CurrencyToTab(ProfileInfo.PriceCurrency currency)
    {
        switch (currency)
        {
            case ProfileInfo.PriceCurrency.Silver: return Tab.Silver;
            case ProfileInfo.PriceCurrency.Gold: return Tab.Gold;
            default:
                Debug.LogError("Unknown currency!");
                return Tab.Gold;
        }
    }

    private void OnItemPurchaseClick(tk2dUIItem btn)
    {
        if (goldScrollArea.IsSwipeScrollingInProgress || silverScrollArea.IsSwipeScrollingInProgress || kitsScrollArea.IsSwipeScrollingInProgress)
            return;

        string curStoreIdString = btn.name;
        if (btn.name == "xdevs.vip_kit")
        {
            VipPrice.StoreId currentStoreId = VipManager.GetCurrentStoreId(btn.name);

            if (currentStoreId != null)
                curStoreIdString = currentStoreId.Id;
        }

        IapManager.BuyProductId(XdevsSplashScreen.Instance.waitingIndicator.GetParent(WaitingIndicatorBase.ParentType.Kits), curStoreIdString, () =>
        {
            if (btn.name.Contains("_kit"))
            {
                foreach (var kitTypePair in GameData.bankKits)
                {
                    if (kitTypePair.Key != BankKits.Type.Newbie)//Пока на клиенте нет префабов для других наборов - игнорируем их
                        continue;

                    foreach (var kit in GameData.bankKits[kitTypePair.Key])
                        if (kit.Value.unibillId == btn.name)
                            kit.Value.needToShow = false;
                }
            }
        });
    }

    private void OnHangarTimerTick(EventId id, EventInfo info)
    {
        //TODO: может быть сделать проверку раз в минуту, а не раз в секунду
        if (IsInitialized)
            ReсreateKitsIfNeeded();
    }

    /// <summary>
    /// Сравниваем список действующих китов со списком китов которые должны существовать в данный момент
    /// </summary>
    private bool IsNeededToRecreateKits()
    {
        if (GameData.bankKits == null || !GameData.bankKits.ContainsKey(BankKits.Type.Newbie))
            return false;

        tempActiveKits.Clear();
        foreach (var kitTypePair in GameData.bankKits)
        {
            if (kitTypePair.Key != BankKits.Type.Newbie)//Пока на клиенте нет префабов для других наборов - игнорируем их
                continue;

            foreach (var kit in GameData.bankKits[kitTypePair.Key])
            {
                if (!activeKits.ContainsKey(kitTypePair.Key))
                    return true;
                //Если время действия пака не наступило / прошло - не добавляем в словарь
                if (!kit.Value.IsActive)
                {
                    if (activeKits[kitTypePair.Key].ContainsKey(kit.Key))
                        return true;//если время жизни закончилось - удаляем
                    else
                        continue;
                }

                if (!tempActiveKits.ContainsKey(kitTypePair.Key))
                    tempActiveKits.Add(kitTypePair.Key, new Dictionary<string, BankKits.Data>());
                tempActiveKits[kitTypePair.Key][kit.Key] = kit.Value;

                if (!activeKits[kitTypePair.Key].ContainsKey(kit.Key))
                    return true;
            }

            if (tempActiveKits.ContainsKey(kitTypePair.Key) && activeKits.ContainsKey(kitTypePair.Key) && tempActiveKits[kitTypePair.Key].Count != activeKits[kitTypePair.Key].Count)
                return true;
        }

        return false;
    }

    private int FillActiveKitsList()
    {
        int sum = 0;
        activeKits.Clear();
        foreach (var kitTypePair in GameData.bankKits)
        {
            if (kitTypePair.Key != BankKits.Type.Newbie)//Пока на клиенте нет префабов для других наборов - игнорируем их
                continue;
            activeKits.Add(kitTypePair.Key, new Dictionary<string, BankKits.Data>());
            foreach (var kit in GameData.bankKits[kitTypePair.Key])
                if (kit.Value.IsActive)
                {
                    activeKits[kitTypePair.Key][kit.Key] = kit.Value;
                    sum++;
                }

        }

        return sum;
    }

    private void UpdateKitsTabState()
    {
        if (tabsGroup != null && tabsGroup.ToggleBtns.Length >= ((int)Tab.Kits) + 1)
            tabsGroup.ToggleBtns[(int)Tab.Kits].gameObject.SetActive(kitTransforms.Count > 0);
    }

    private void ReсreateKitsIfNeeded()
    {
        if (!IsNeededToRecreateKits())
            return;

        //Удаляем старые плашки
        foreach (var kitTypePair in activeKits)
            foreach (var kitPair in kitTypePair.Value)
                if (kitPair.Value != null && kitPair.Value.goodsKit)
                {
                    if (GUIPager.ActivePageName == kitPair.Value.Page)
                        GUIPager.SetActivePage("MainMenu");
                    Destroy(kitPair.Value.goodsKit.gameObject);
                }

        kitTransforms.Clear();

        int itemsCount = FillActiveKitsList();

        //Если нет ни одного активного набора, но мы еще в банке - выходим в главное меню и дизейблим кнопку наборов
        if (GUIPager.ActivePageName == "Bank" && tabsGroup != null && tabsGroup.SelectedIndex == (int)Tab.Kits && itemsCount == 0)
        {
            GUIPager.SetActivePage("MainMenu");
        }
        else if (itemsCount > 0)
        {
            //Создаем новые
            foreach (var kitTypePair in activeKits)
                foreach (var kitPair in kitTypePair.Value)
                {
                    GameObject go = Resources.Load<GameObject>(kitPair.Value.PrefabPath);
                    if (go == null)
                    {
                        DT.LogError("Cant create Bank kit lot {0}", kitPair.Value.PrefabPath);
                        continue;
                    }

                    kitPair.Value.goodsKit = InstantiateFrame(go, go.name, kitTransforms).GetComponent<BankKits.GoodsKit>();
                    kitPair.Value.goodsKit.Initialize(kitPair.Value);
                }

            //Располагаем созданные плашки
            if (kitsScrollArea != null) // TODO: убрать проверку, когда вкладка будет доделана.
                SetFramesParent(kitTransforms, kitsScrollArea.contentContainer.transform);

            if (kitsScrollArea != null) // TODO: убрать проверку, когда вкладка будет доделана.
                FramesReposition(kitTransforms, kitsScrollArea);
        }

        UpdateKitsTabState();
    }

    private void OnBankInitialized(EventId id, EventInfo info)
    {
        GUIPager.EnqueueAction(() =>
        {
            if (!HangarController.FirstEnter)
            {
                if (IsInitialized && kitTransforms.Count > 0 && ItsTimeToShowNewbieKits)
                {
                    HangarController.Instance.GoToBank(Tab.Kits, true);
                    counterForShowingNewbieKitsAfterBattle = 0;
                }
                else
                    counterForShowingNewbieKitsAfterBattle++;
            }
        });
    }

    public void OnLanguageChange(EventId id, EventInfo info)
    {
        SetAdsFreeText(EventId.OnLanguageChange, null);
        SetBankLotsOfGivenCurrency(ProfileInfo.PriceCurrency.Silver);
        SetBankLotsOfGivenCurrency(ProfileInfo.PriceCurrency.Gold);

        SetupKitsWindows();
    }
}
