using System;
using System.Collections.Generic;
using System.Linq;

public class VehicleShop : Shop<VehicleShopItemCell>
{
    public static VehicleShop Instance
    {
        get; private set;
    }

    public override string GuiPageName
    {
        get { return "VehicleShopWindow"; }
    }

    public override string OnToggleMethodName
    {
        get { return "OnVehicleSelect"; }
    }

    protected override Shop.ItemType ComparingMode
    {
        get
        {
            return Shop.ItemType.Vehicle |
                   Shop.ItemType.Module;
        }
    }

    protected override IShopItem[] ShopItems
    {
        get { return VehiclePool.Instance.Items; }
    }

    void Awake()
    {
        Instance = this;
    }

    void OnDestroy()
    {
        Instance = null;
    }

    protected override void OnEnable()
    {
        GUIController.ListenButtonClick("btnBuy", OnBuyClick);
        GUIController.ListenButtonClick("btnInstall", OnInstallClick);

        FillPanel();

        if (Selectors != null)
        {
            var actualCell = Shop.VehicleInView != null ? Selectors[Shop.VehicleInView.Info.id] : Selectors.First().Value;

            SelectVehicle(
                toggle:         actualCell.toggle,
                hangarVehicle:  actualCell.UserVehicle.HangarVehicle,
                immediately:    true,
                playSound:      false);
        }

        Messenger.Send(EventId.VehicleShopFilled, null);
    }

    protected override void OnDisable()
    {
        GUIController.RemoveButtonClickListener("btnBuy", OnBuyClick);
        GUIController.RemoveButtonClickListener("btnInstall", OnInstallClick);

        if (Selectors != null)
            ShowVehicle(
                vehicleId:  Shop.CurrentVehicle.Info.id,
                showPrice:  false,
                playSound:  false,
                onDisable:  true);
    }

    public static void ForcedFillPanel()
    {
        IShopItem[] shopItems = VehiclePool.Instance.Items;

        if (Selectors != null && Selectors.Count > 0)
        {
            VehicleShopItemCell firstItemCell = Selectors.First().Value;

            if (firstItemCell != null)
                firstItemCell.transform.parent.DestroyChildren();
        }

        Selectors = new Dictionary<int, VehicleShopItemCell>(shopItems.Length);

        ShopItemCell.ResetCellOffset();

        foreach (IShopItem shopItem in shopItems)
        {
            if (shopItem.HideCondition && !ProfileInfo.vehicleUpgrades.ContainsKey(shopItem.Id))
                continue;

            if (HangarVehiclesHolder.GetByIdOrDefault(shopItem.Id) == null)
                continue;

            VehicleShopItemCell shopItemCell = Instantiate(ShopManager.Instance.vehicleShopItemCellPrefab);
            shopItemCell.Set<VehicleShopItemCell>(shopItem, shopItem == shopItems.Last());

            Selectors[shopItem.Id] = shopItemCell;
        }

        Shop.CurrentVehicle
            = Selectors.ContainsKey(ProfileInfo.CurrentVehicle)
                ? Selectors[ProfileInfo.CurrentVehicle].UserVehicle
                : null;

        if (Shop.CurrentVehicle != null)
            ForcedShowVehicle(Shop.CurrentVehicle.Info.id);
    }

    public static void ForcedShowVehicle(int vehicleId)
    {
        if (vehicleId == 0)
            return;

        if (!Selectors.ContainsKey(vehicleId))
            return;

        Shop.VehicleInView = Selectors[vehicleId].UserVehicle;

        foreach (VehicleShopItemCell vehicleItem in Selectors.Values)
            vehicleItem.UserVehicle.HangarVehicle.gameObject.SetActive(vehicleItem.UserVehicle.Info.id == vehicleId);
    }

    public void ShowVehicle(int vehicleId, bool showPrice, bool playSound, bool onDisable = false)
    {
        if (vehicleId == 0)
            return;

        if (!Selectors.ContainsKey(vehicleId))
            return;

        if (playSound)
            HangarController.Instance.PlayGUISound(SelectSound);

        if (!onDisable)
            Shop.VehicleInView = Selectors[vehicleId].UserVehicle;

        foreach (VehicleShopItemCell vehicleItem in Selectors.Values)
            if(vehicleItem != null && vehicleItem.UserVehicle != null && vehicleItem.UserVehicle.HangarVehicle != null && vehicleItem.UserVehicle.Info != null)
                vehicleItem.UserVehicle.HangarVehicle.gameObject.SetActive(vehicleItem.UserVehicle.Info.id == vehicleId);

        HangarController.Instance.RecalcMaxStats(Shop.VehicleInView.Info.vehicleType, ComparingMode);

        HangarController.Instance.FillParameterDelta(
            bar:    HangarController.Instance.armorBar,
            max:    HangarController.Instance.ArmorMax[Shop.VehicleInView.Info.vehicleGroup],
            prim:   Shop.CurrentVehicle.Info.baseArmor,
            sec:    Shop.VehicleInView.Info.baseArmor);

        HangarController.Instance.FillParameterDelta(
            bar:    HangarController.Instance.attackBar,
            max:    HangarController.Instance.DamageMax[Shop.VehicleInView.Info.vehicleGroup],
            prim:   Shop.VehicleInView.Info.baseDamage,
            sec:    Shop.VehicleInView.Info.baseDamage);

        HangarController.Instance.FillParameterDelta(
            bar:    HangarController.Instance.speedBar,
            max:    HangarController.Instance.SpeedMax[Shop.VehicleInView.Info.vehicleGroup],
            prim:   Shop.CurrentVehicle.Info.baseSpeed,
            sec:    Shop.VehicleInView.Info.baseSpeed);

        HangarController.Instance.FillParameterDelta(
            bar:    HangarController.Instance.rofBar,
            max:    HangarController.Instance.RoFMax[Shop.VehicleInView.Info.vehicleGroup],
            prim:   Shop.VehicleInView.Info.baseRoF,
            sec:    Shop.VehicleInView.Info.baseRoF);

        HangarController.Instance.FillParameterDelta(
            bar:    HangarController.Instance.dpmBar,
            max:    HangarController.Instance.DPMMax[Shop.VehicleInView.Info.vehicleGroup],
            prim:   UserVehicle.CalcDPM(Shop.CurrentVehicle.Info.baseDamage, Shop.CurrentVehicle.Info.baseRoF, Shop.CurrentVehicle.Info.vehicleType),
            sec:    UserVehicle.CalcDPM(Shop.VehicleInView.Info.baseDamage, Shop.VehicleInView.Info.baseRoF, Shop.VehicleInView.Info.vehicleType));

        //if (Shop.VehicleInView.Info.vehicleType == VehicleInfo.VehicleType.Robot)
        //{
        //    var primaryValue = 
        //        Shop.CurrentVehicle.Info.vehicleType == VehicleInfo.VehicleType.Robot
        //            ? Shop.CurrentVehicle.Info.baseMagazine
        //            : Shop.VehicleInView.Info.baseMagazine;

        //    HangarController.Instance.FillParameterDelta(
        //        bar: HangarController.Instance.magazineBar,
        //        max: HangarController.Instance.MagazineMax[Shop.VehicleInView.Info.vehicleGroup],
        //        prim: primaryValue,
        //        sec: Shop.VehicleInView.Info.baseMagazine);

        //    HangarController.Instance.magazineBar.gameObject.SetActive(true);
        //}
        //else
        //{
        //    HangarController.Instance.magazineBar.gameObject.SetActive(false);
        //}

        if (showPrice)
            ShowVehiclePrice();
    }

    protected override void FillPanel()
    {
        IShopItem[] shopItems = ShopItems;

        Selectors = new Dictionary<int, VehicleShopItemCell>(shopItems.Length);

        ScrollableArea.contentContainer.transform.DestroyChildren();

        ShopItemCell.ResetCellOffset();

        foreach (IShopItem shopItem in shopItems)
        {
            if (shopItem.HideCondition && !ProfileInfo.vehicleUpgrades.ContainsKey(shopItem.Id))
                continue;

            if (HangarVehiclesHolder.GetByIdOrDefault(shopItem.Id) == null)
                continue;

            VehicleShopItemCell shopItemCell = Instantiate(shopItemCellPrefab);
            shopItemCell.Set<VehicleShopItemCell>(shopItem, shopItem == shopItems.Last());

            Selectors[shopItem.Id] = shopItemCell;
        }

        var itemLayout = shopItemCellPrefab.GetComponent<tk2dUILayout>();
        var itemWidth = (itemLayout.GetMaxBounds() - itemLayout.GetMinBounds()).x;
        
        // itemWidth потому что левая половина первого элемента и правая половина последнего
        ScrollableArea.ContentLength += ScrollableArea.VisibleAreaLength / 2 - itemWidth;

        if (Shop.VehicleInView != null && GUIPager.ActivePage == GuiPageName)
            SelectVehicle(
                toggle:         Selectors[Shop.VehicleInView.Info.id].toggle,
                hangarVehicle:  null,
                immediately:    true,
                playSound:      false);
    }

    private void ShowVehiclePrice()
    {
        if (Shop.VehicleInView.Info.isComingSoon)
        {
            HangarController.Instance.SetActionBoxType(HangarController.ActionBoxType.ComingSoon);
            return;
        }

        if (Shop.CurrentVehicle.Info.id == Shop.VehicleInView.Info.id)
        {
            HangarController.Instance.SetActionBoxType(HangarController.ActionBoxType.Installed);
            return;
        }

        if (ProfileInfo.vehicleUpgrades.ContainsKey(Shop.VehicleInView.Info.id))
        {
            HangarController.Instance.SetActionBoxType(HangarController.ActionBoxType.Install);
            return;
        }

        if (Shop.VehicleInView.Info.id == VehicleOffersController.FreeVehicleIds[GameData.ClearGameFlags(GameData.CurrentGame)])
        {
            HangarController.Instance.SetActionBoxType(HangarController.ActionBoxType.FreeTank);
            return;
        }

        if (Shop.VehicleInView.Info.availabilityLevel > ProfileInfo.Level && !Shop.VehicleInView.Info.isVip)
        {
            HangarController.Instance.SetActionBoxType(HangarController.ActionBoxType.Inaccessible);
            InaccessibleBoxLabel.Parameter = Shop.VehicleInView.Info.availabilityLevel.ToString();
            return;
        }

        if (VehicleOffersController.Instance.AnyItemIsOnSale && VehicleOffersController.Instance.CheckIfItemOnSale(Shop.VehicleInView.Info.id))
        {
            HangarController.Instance.SetActionBoxType(HangarController.ActionBoxType.Sale);
        }
        else
        {
            HangarController.Instance.SetActionBoxType(HangarController.ActionBoxType.Buy);
        }

        HangarController.CurrentActionBox.Price = Shop.VehicleInView.Info.price;
        HangarController.CurrentActionBox.IsProductVip = Shop.VehicleInView.Info.isVip;
    }

    private void SelectVehicle(tk2dUIToggleControl toggle, HangarVehicle hangarVehicle, bool immediately, bool playSound)
    {
        bool allowedToPlaySound;

        SelectToggle(
            /*toggle:*/             toggle,
            /*immediately:*/        immediately,
            /*allowToPlaySound:*/   out allowedToPlaySound);

        int vehicleId;

        if (hangarVehicle != null)
            vehicleId = hangarVehicle.Info.id;
        else
            Int32.TryParse(toggle.name.Substring(toggle.name.LastIndexOf('_') + 1), out vehicleId);

        ShowVehicle(
            vehicleId:  vehicleId,
            showPrice:  true,
            playSound:  playSound && allowedToPlaySound);

        HangarController.Instance.SetActiveDoubleExpText(vehicleId);
        Messenger.Send(EventId.VehicleSelected, new EventInfo_I(vehicleId));
    }

    public void SelectVehicle(int vehicleId)
    {
        if (Selectors.ContainsKey(vehicleId))
        {
            SelectVehicle(
                toggle: Selectors[vehicleId].toggle,
                hangarVehicle: null,
                immediately: false,
                playSound: true);
        }
    }

    private void OnVehicleSelect(tk2dUIToggleControl toggle)
    {
        SelectVehicle(
            toggle:         toggle,
            hangarVehicle:  null,
            immediately:    false,
            playSound:      true);
    }

    private void OnBuyClick(string buttonName)
    {
        if (HangarController.Instance.isWaitingForSaving)
            return;

        if (HangarController.CurrentActionBox.IsProductVip && !ProfileInfo.IsPlayerVip)
        {
            HangarController.Instance.NavigateToVipShop(showMessageBox: true);

            SelectVehicle(
                toggle:         Selectors[Shop.VehicleInView.Info.id].toggle,
                hangarVehicle:  null,
                immediately:    true,
                playSound:      false);

            return;
        }

        if (!ProfileInfo.CanBuy(HangarController.CurrentActionBox.Price))
        {
            HangarController.Instance.GoToBank(Bank.CurrencyToTab(Shop.VehicleInView.Info.price.currency), voiceRequired: true);

            #region Google Analytics: vehicle buying failure "not enough money"

            GoogleAnalyticsWrapper.LogEvent(
                new CustomEventHitBuilder()
                    .SetParameter(GAEvent.Category.VehicleBuying)
                    .SetParameter(GAEvent.Action.NotEnoughMoney)
                    .SetParameter<GAEvent.Label>()
                    .SetSubject(GAEvent.Subject.VehicleID, Shop.VehicleInView.Info.id.ToString())
                    .SetValue(ProfileInfo.Level));

            #endregion

            return;
        }

        BuyVehicle(
            userVehicle:    Shop.VehicleInView,
            finishCallback: delegate(Http.Response response, bool result)
                            {
                                if (!result)
                                    return;

                                HangarController.Instance.PlayGUISound(SelectSound);

                                HangarController.Instance.SetActiveDoubleExpText(Shop.VehicleInView.Info.id);

                                Selectors[Shop.VehicleInView.Info.id].sprDoubleExp.SetActive(true);

                                ShowVehiclePrice();

                                Messenger.Send(EventId.VehicleBought, new EventInfo_I(Shop.VehicleInView.Info.id));
                                PatternOffersController.Instance.SubscribeOnTickSaleSticker();
                                DecalOffersController.Instance.SubscribeOnTickSaleSticker();

                                #region Google Analytics: vehicle bought

                                GoogleAnalyticsWrapper.LogEvent(
                                    new CustomEventHitBuilder()
                                        .SetParameter(GAEvent.Category.VehicleBuying)
                                        .SetParameter(GAEvent.Action.Bought)
                                        .SetParameter<GAEvent.Label>()
                                        .SetSubject(GAEvent.Subject.VehicleID, Shop.VehicleInView.Info.id.ToString())
                                        .SetValue(ProfileInfo.Level));

                                #endregion
                            });
    }

    private void OnInstallClick(string buttonName)
    {
        HangarController.Instance.isWaitingForSaving = true;

        // Деактивируем installBox a-la RentBox.SetButtonActivated(false);
        var installBoxActivatedUpDownButton =
            HangarController.Instance.installBox.GetComponentInChildren<ActivatedUpDownButton>();

        installBoxActivatedUpDownButton.Activated = false;

        var request = Http.Manager.Instance().CreateRequest("/shop/installTank");

        request.Form.AddField("tankId", (int)Shop.VehicleInView.Info.id);

        Http.Manager.StartAsyncRequest(
            request:            request,
            successCallback:    result =>
                                {
                                    if (HangarController.Instance == null)
                                        return;

                                    HangarController.Instance.isWaitingForSaving = false;

                                    ShowVehicle(
                                        vehicleId:  Shop.CurrentVehicle.Info.id,
                                        showPrice:  true,
                                        playSound:  false);

                                    installBoxActivatedUpDownButton.Activated = true;

                                    Messenger.Send(EventId.VehicleInstalled, new EventInfo_I(Shop.CurrentVehicle.Info.id));
                                    ModuleShop.Instance.CheckIfModuleUpgradePossible();
                                },
            failCallback:       result =>
                                {
                                    if (HangarController.Instance == null)
                                        return;

                                    installBoxActivatedUpDownButton.Activated = true;

                                    HangarController.Instance.isWaitingForSaving = false;
                                });

        PatternOffersController.Instance.RefreshOffers();
        DecalOffersController.Instance.RefreshOffers();
    }

    private void BuyVehicle(UserVehicle userVehicle, Action<Http.Response, bool> finishCallback)
    {
        HangarController.Instance.isWaitingForSaving = true;
        BuyingBox.SetButtonActivated(false);

        Http.Request request = Http.Manager.Instance().CreateRequest("/shop/buyTank");

        request.Form.AddField("tankId", (int)userVehicle.Info.id);

        Http.Manager.StartAsyncRequest(
            request:            request,
            successCallback:    delegate(Http.Response result)
                                {
                                    if (HangarController.Instance == null)
                                        return;

                                    HangarController.Instance.isWaitingForSaving = false;
                                    BuyingBox.SetButtonActivated();
                                    finishCallback(result, true);
                                },
            failCallback:       delegate(Http.Response result)
                                {
                                    if (HangarController.Instance == null)
                                        return;

                                    HangarController.Instance.isWaitingForSaving = false;
                                    BuyingBox.SetButtonActivated();
                                    finishCallback(result, false);
                                });
    }
}
