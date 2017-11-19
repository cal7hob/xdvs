using System;
using System.Collections.Generic;
using System.Linq;
using CodeStage.AntiCheat.ObscuredTypes;

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

        HangarController.Instance.RecalcMaxStats(ComparingMode);

        if (Selectors != null)
        {
            var actualCell = Shop.VehicleInView != null ? Selectors[Shop.VehicleInView.Info.id] : Selectors.First().Value;

            SelectVehicle(
                toggle: actualCell.toggle,
                hangarVehicle: actualCell.UserVehicle.HangarVehicle,
                immediately: true,
                playSound: false);
        }

        Dispatcher.Send(EventId.VehicleShopFilled, null);
    }

    protected override void OnDisable()
    {
        GUIController.RemoveButtonClickListener("btnBuy", OnBuyClick);
        GUIController.RemoveButtonClickListener("btnInstall", OnInstallClick);

        if (Selectors != null)
            ShowVehicle(
                vehicleId: Shop.CurrentVehicle.Info.id,
                showPrice: false,
                playSound: false,
                onDisable: true);
    }

    public static void ForcedFillPanel()
    {
        IShopItem[] shopItems = VehiclePool.Instance.Items;

        foreach (var shopItem in shopItems)
        {
            VehicleInfo info = (VehicleInfo)shopItem;
            HangarVehiclesHolder.TryInitHangarVehicleSwitcher(info.id);
        }

        if (Selectors != null && Selectors.Count > 0)
        {
            VehicleShopItemCell firstItemCell = Selectors.First().Value;

            if (firstItemCell != null)
            {
                firstItemCell.transform.parent.DestroyChildren();
            }
        }

        Selectors = new Dictionary<int, VehicleShopItemCell>(shopItems.Length);
        ShopItemCell.ResetCellOffset();

        foreach (IShopItem shopItem in shopItems)
        {
            if (shopItem.HideCondition && !ProfileInfo.vehicleUpgrades.ContainsKey(shopItem.Id))
            {
                continue;
            }

            if (HangarVehiclesHolder.GetByIdOrDefault(shopItem.Id) == null)
            {
                continue;
            }

            VehicleShopItemCell shopItemCell = Instantiate(ShopManager.Instance.vehicleShopItemCellPrefab);
            shopItemCell.Set<VehicleShopItemCell>(shopItem);
            Selectors[shopItem.Id] = shopItemCell;
        }

        Shop.CurrentVehicle = Selectors.ContainsKey(ProfileInfo.CurrentVehicle)
                ? Selectors[ProfileInfo.CurrentVehicle].UserVehicle
                : null;

        if (Shop.CurrentVehicle != null)
            ForcedShowVehicle(Shop.CurrentVehicle.Info.id);
    }

    public static void ForcedShowVehicle(int vehicleId)
    {
        if (vehicleId == 0)
        {
            return;
        }

        if (!Selectors.ContainsKey(vehicleId))
        {
            return;
        }

        Shop.VehicleInView = Selectors[vehicleId].UserVehicle;

        foreach (VehicleShopItemCell vehicleItem in Selectors.Values)
        {
            vehicleItem.UserVehicle.HangarVehicle.gameObject.SetActive(vehicleItem.UserVehicle.Info.id == vehicleId);
        }
    }

    public void ShowVehicle(int vehicleId, bool showPrice, bool playSound, bool onDisable = false)
    {
        if (vehicleId == 0)
        {
            return;
        }

        if (!Selectors.ContainsKey(vehicleId))
        {
            return;
        }

        if (playSound)
        {
            MenuController.TankSelectionSound();
        }

        if (!onDisable)
        {
            Shop.VehicleInView = Selectors[vehicleId].UserVehicle;
        }

        foreach (VehicleShopItemCell vehicleItem in Selectors.Values)
        {
            if (vehicleItem != null && vehicleItem.UserVehicle != null && vehicleItem.UserVehicle.HangarVehicle != null && vehicleItem.UserVehicle.Info != null)
            {
                vehicleItem.UserVehicle.HangarVehicle.gameObject.SetActive(vehicleItem.UserVehicle.Info.id == vehicleId);
            }
        }

        HangarController.Instance.FillParameterDelta(
            bar: MenuController.Instance.armorBar,
            max: HangarController.Instance.ArmorMax[Shop.VehicleInView.Info.vehicleGroup],
            prim: Shop.CurrentVehicle.GetRealParameters()[VehicleInfo.VehicleParameter.Armor],
            sec: Shop.VehicleInView == Shop.CurrentVehicle
                    ? Shop.CurrentVehicle.GetRealParameters()[VehicleInfo.VehicleParameter.Armor] :
                    Shop.VehicleInView.GetParametersWith<Decal>(1)[VehicleInfo.VehicleParameter.Armor]);

        HangarController.Instance.FillParameterDelta(
            bar: MenuController.Instance.attackBar,
            max: HangarController.Instance.DamageMax[Shop.VehicleInView.Info.vehicleGroup],
            prim: Shop.CurrentVehicle.GetRealParameters()[VehicleInfo.VehicleParameter.Damage],
            sec: Shop.VehicleInView == Shop.CurrentVehicle
                    ? Shop.CurrentVehicle.GetRealParameters()[VehicleInfo.VehicleParameter.Damage] :
                    Shop.VehicleInView.GetParametersWith<Decal>(1)[VehicleInfo.VehicleParameter.Damage]);


        HangarController.Instance.FillParameterDelta(
            bar: MenuController.Instance.rocketAttackBar,
            max: HangarController.Instance.RocketDamageMax[Shop.VehicleInView.Info.vehicleGroup],
            prim: Shop.CurrentVehicle.Info.baseRocketDamage,
            sec: Shop.VehicleInView.Info.baseRocketDamage);

        HangarController.Instance.FillParameterDelta(
            bar: MenuController.Instance.speedBar,
            max: HangarController.Instance.SpeedMax[Shop.VehicleInView.Info.vehicleGroup],
            prim: Shop.CurrentVehicle.GetRealParameters()[VehicleInfo.VehicleParameter.Speed],
            sec:
                Shop.VehicleInView == Shop.CurrentVehicle
                    ? Shop.CurrentVehicle.GetRealParameters()[VehicleInfo.VehicleParameter.Speed] :
                    Shop.VehicleInView.GetParametersWith<Decal>(1)[VehicleInfo.VehicleParameter.Speed]);


        HangarController.Instance.FillParameterDelta(
            bar: MenuController.Instance.rofBar,
            max: HangarController.Instance.ROFMax[Shop.VehicleInView.Info.vehicleGroup],
            prim: Shop.CurrentVehicle.GetRealParameters()[VehicleInfo.VehicleParameter.RoF],
            sec: Shop.VehicleInView == Shop.CurrentVehicle
                    ? Shop.CurrentVehicle.GetRealParameters()[VehicleInfo.VehicleParameter.RoF] :
                    Shop.VehicleInView.GetParametersWith<Decal>(1)[VehicleInfo.VehicleParameter.RoF]);


        HangarController.Instance.FillParameterDelta(
            bar: MenuController.Instance.ircmRofBar,
            max: HangarController.Instance.IRCMROFMax[Shop.VehicleInView.Info.vehicleGroup],
            prim: Shop.CurrentVehicle.Info.baseIRCMROF,
            sec: Shop.VehicleInView.Info.baseIRCMROF);

        HangarController.Instance.FillParameterDelta(
            bar: MenuController.Instance.reloadSpeedBar,
            max: HangarController.Instance.ReloadMax[Shop.VehicleInView.Info.vehicleGroup],
            prim: Shop.CurrentVehicle.GetRealParameters()[VehicleInfo.VehicleParameter.ReloadTime],
            sec: Shop.VehicleInView == Shop.CurrentVehicle
                    ? Shop.CurrentVehicle.GetRealParameters()[VehicleInfo.VehicleParameter.ReloadTime] :
                    Shop.VehicleInView.GetParametersWith<Decal>(1)[VehicleInfo.VehicleParameter.ReloadTime]);


        HangarController.Instance.FillParameterDelta(
            bar: MenuController.Instance.magazineBar,
            max: HangarController.Instance.MagazineMax[Shop.VehicleInView.Info.vehicleGroup],
            prim: Shop.CurrentVehicle.GetRealParameters()[VehicleInfo.VehicleParameter.Magazine],
            sec: Shop.VehicleInView == Shop.CurrentVehicle
                    ? Shop.CurrentVehicle.GetRealParameters()[VehicleInfo.VehicleParameter.Magazine] :
                    Shop.VehicleInView.GetParametersWith<Decal>(1)[VehicleInfo.VehicleParameter.Magazine]);


        if (showPrice)
        {
            ShowVehiclePrice();
        }
    }

    protected override void FillPanel()
    {
        IShopItem[] shopItems = ShopItems;

        foreach (var shopItem in shopItems)
        {
            VehicleInfo info = (VehicleInfo)shopItem;
            HangarVehiclesHolder.TryInitHangarVehicleSwitcher(info.id);
        }

        Selectors = new Dictionary<int, VehicleShopItemCell>(shopItems.Length);
        ScrollableArea.contentContainer.transform.DestroyChildren();

        ShopItemCell.ResetCellOffset();

        foreach (IShopItem shopItem in shopItems)
        {
            if (shopItem.HideCondition && !ProfileInfo.vehicleUpgrades.ContainsKey(shopItem.Id))
            {
                continue;
            }

            if (HangarVehiclesHolder.GetByIdOrDefault(shopItem.Id) == null)
            {
                continue;
            }

            VehicleShopItemCell shopItemCell = Instantiate(shopItemCellPrefab);
            shopItemCell.Set<VehicleShopItemCell>(shopItem);

            Selectors[shopItem.Id] = shopItemCell;
        }

        ScrollableArea.ContentLength += (GUIController.halfScreenWidth / 2);

        if (Shop.VehicleInView != null && GUIPager.ActivePage == GuiPageName)
            SelectVehicle(
                toggle: Selectors[Shop.VehicleInView.Info.id].toggle,
                hangarVehicle: null,
                immediately: true,
                playSound: false);


    }

    private void ShowVehiclePrice()
    {
        MenuController.ShowVehiclePrice();
        /*if (Shop.VehicleInView.Info.isComingSoon)
        {
            MenuController.Instance.SetActionBoxType(ActionBoxType.ComingSoon);
            return;
        }

        if (Shop.CurrentVehicle.Info.id == Shop.VehicleInView.Info.id)
        {
            MenuController.Instance.SetActionBoxType(ActionBoxType.Installed);
            return;
        }

        if (ProfileInfo.vehicleUpgrades.ContainsKey(Shop.VehicleInView.Info.id))
        {
            MenuController.Instance.SetActionBoxType(ActionBoxType.Install);
            return;
        }

        if (Shop.VehicleInView.Info.id == VehicleOffersController.FreeVehicleIds[GameData.ClearGameFlags(GameData.CurrentGame)])
        {
            MenuController.Instance.SetActionBoxType(ActionBoxType.FreeTank);
            return;
        }

        if (Shop.VehicleInView.Info.availabilityLevel > ProfileInfo.Level && !Shop.VehicleInView.Info.isVip)
        {
            MenuController.Instance.SetActionBoxType(ActionBoxType.Inaccessible);
            InaccessibleBoxLabel.Parameter = Shop.VehicleInView.Info.availabilityLevel.ToString();
            return;
        }

        if (VehicleOffersController.Instance.AnyItemIsOnSale && VehicleOffersController.Instance.CheckIfItemOnSale(Shop.VehicleInView.Info.id))
        {
            MenuController.Instance.SetActionBoxType(ActionBoxType.Sale);
        }
        else
        {
            MenuController.Instance.SetActionBoxType(ActionBoxType.Buy);
        }

        MenuController.CurrentActionBox.Price = Shop.VehicleInView.Info.price;
        MenuController.CurrentActionBox.IsProductVip = Shop.VehicleInView.Info.isVip;*/
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
        {
            vehicleId = hangarVehicle.Info.id;
        }
        else
        {
            Int32.TryParse(toggle.name.Substring(toggle.name.LastIndexOf('_') + 1), out vehicleId);
        }
        ShowVehicle(
            vehicleId: vehicleId,
            showPrice: true,
            playSound: playSound && allowedToPlaySound);

        MenuController.SetActiveDoubleExpText(vehicleId);
        Dispatcher.Send(EventId.VehicleSelected, new EventInfo_I(vehicleId));
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
            toggle: toggle,
            hangarVehicle: null,
            immediately: false,
            playSound: true);
    }

    private void OnBuyClick(string buttonName)
    {
        if (HangarController.Instance.isWaitingForSaving)
        {
            return;
        }

        if (MenuController.CurrentActionBox.IsProductVip && !ProfileInfo.IsPlayerVip)
        {
            HangarController.Instance.NavigateToVipShop(showMessageBox: true);

            SelectVehicle(
                toggle: Selectors[Shop.VehicleInView.Info.id].toggle,
                hangarVehicle: null,
                immediately: true,
                playSound: false);

            return;
        }

        if (!ProfileInfo.CanBuy(MenuController.CurrentActionBox.Price))
        {
            MenuController.Instance.GoToBank(Bank.CurrencyToTab(Shop.VehicleInView.Info.price.currency), voiceRequired: true);

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
            userVehicle: Shop.VehicleInView,
            finishCallback: delegate (Http.Response response, bool result)
                            {
                                if (!result)
                                {
                                    return;
                                }

                                MenuController.TankSelectionSound();
                                MenuController.SetActiveDoubleExpText(Shop.VehicleInView.Info.id);

                                Selectors[Shop.VehicleInView.Info.id].sprDoubleExp.SetActive(true);

                                ShowVehiclePrice();

                                Dispatcher.Send(EventId.VehicleBought, new EventInfo_I(Shop.VehicleInView.Info.id));
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
        MenuController.TankSelectionSound();

        HangarController.Instance.isWaitingForSaving = true;

        var request = Http.Manager.Instance().CreateRequest("/shop/installTank");

        request.Form.AddField("tankId", (int)Shop.VehicleInView.Info.id);

        Http.Manager.StartAsyncRequest(
            request: request,
            successCallback: result =>
                             {
                                 HangarController.Instance.isWaitingForSaving = false;

                                 ShowVehicle(
                                     vehicleId: Shop.CurrentVehicle.Info.id,
                                     showPrice: true,
                                     playSound: false);

                                 Dispatcher.Send(EventId.VehicleInstalled, new EventInfo_I(Shop.CurrentVehicle.Info.id));
                                 ModuleShop.Instance.CheckIfModuleUpgradePossible();
                             },
            failCallback: result => HangarController.Instance.isWaitingForSaving = false);

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
            request: request,
            successCallback: delegate (Http.Response result)
                             {
                                 HangarController.Instance.isWaitingForSaving = false;
                                 BuyingBox.SetButtonActivated();
                                 finishCallback(result, true);
                             },
            failCallback: delegate (Http.Response result)
                          {
                              HangarController.Instance.isWaitingForSaving = false;
                              BuyingBox.SetButtonActivated();
                              finishCallback(result, false);
                          });
    }
}
