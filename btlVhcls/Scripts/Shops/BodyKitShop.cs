using System;
using System.Collections.Generic;
using System.Linq;
using CodeStage.AntiCheat.ObscuredTypes;
using Http;

public abstract class BodyKitShop : Shop<BodykitShopItemCell>
{
    protected int bodykitInViewId;
    protected double rentingRemainingTimeSec;

    protected string rentPath = "/shop/rentBodykit";
    protected string installPath = "/shop/installBodykit";
    protected string bodykitFieldName = "bodykitId";

    protected ShopOffer shopSpecialOfferInView;

    public override string OnToggleMethodName { get { return "OnBodykitSelect"; } }

    public int BodyKitInViewId { get { return bodykitInViewId; } }

    protected abstract int CurrentBodykitId { get; }

    private Bodykit BodykitInView { get { return Selectors[bodykitInViewId].GetItem<Bodykit>(); } }

    protected override void OnEnable()
    {
        GUIController.ListenButtonClick("btnRent", OnRentClick);
        GUIController.ListenButtonClick("btnResumeRenting", OnResumeRentingClick);

        FillPanel();

        HangarController.OnTimerTick += ShowRentingInfo;
        HangarController.OnTimerTick += UpdateActionBoxState;

        HangarController.Instance.RecalcMaxStats(ComparingMode);

        ShopItemCell actualCell;

        if (Selectors.ContainsKey(BodyKitInViewId))
            actualCell = Selectors[BodyKitInViewId];
        else if (Selectors.ContainsKey(CurrentBodykitId))
            actualCell = Selectors[CurrentBodykitId];
        else
            actualCell = Selectors.First().Value;

        SelectBodykit(
            toggle:         actualCell.toggle,
            userVehicle:    null,
            immediately:    true,
            playSound:      false);
    }

    protected override void OnDisable()
    {
        GUIController.RemoveButtonClickListener("btnRent", OnRentClick);
        GUIController.RemoveButtonClickListener("btnResumeRenting", OnResumeRentingClick);

        HangarController.OnTimerTick -= ShowRentingInfo;
        HangarController.OnTimerTick -= UpdateActionBoxState;

        if (CurrentBodykitId != bodykitInViewId)
        {
            EndBodykitTrialWith(
                Selectors.ContainsKey(CurrentBodykitId)
                    ? Selectors[CurrentBodykitId].GetItem<Bodykit>()
                    : null);
        }
    }

    protected static void ForcedFillPanel<TBodykitShopItemCell>(IShopItem[] shopItems, TBodykitShopItemCell bodykitShopItemCellPrefab)
        where TBodykitShopItemCell : BodykitShopItemCell
    {
        Selectors = new Dictionary<int, BodykitShopItemCell>(shopItems.Length);

        if (Selectors != null && Selectors.Count > 0)
        {
            TBodykitShopItemCell firstItemCell = (TBodykitShopItemCell)Selectors.First().Value;

            if (firstItemCell != null)
                firstItemCell.transform.parent.DestroyChildren();
        }

        ShopItemCell.ResetCellOffset();

        foreach (IShopItem shopItem in shopItems)
        {
            if (shopItem.HideCondition)
                continue;

            TBodykitShopItemCell shopItemCell = Instantiate(bodykitShopItemCellPrefab);
            shopItemCell.Set<BodykitShopItemCell>(shopItem);

            Selectors[shopItem.Id] = shopItemCell;
        }
    }

    #region Server methods (static)

    /// <summary>
    /// Покупка кита на сервере.
    /// </summary>
    protected static void RequestBodykitRent(
        string                      rentPath,
        string                      bodykitFieldName,
        UserVehicle                 userVehicle,
        Bodykit                     bodykit,
        Action<Http.Response, bool> finishCallback)
    {
        HangarController.Instance.isWaitingForSaving = true;

        Request request = Http.Manager.Instance().CreateRequest(rentPath);

        request.Form.AddField("tankId", (int)userVehicle.Info.id);
        request.Form.AddField(bodykitFieldName, bodykit.id.ToString());

        Http.Manager.StartAsyncRequest(
            request: request,
            successCallback: delegate(Http.Response result)
            {
                HangarController.Instance.isWaitingForSaving = false;
                finishCallback(result, true);
            },
            failCallback: delegate(Http.Response result)
            {
                HangarController.Instance.isWaitingForSaving = false;
                finishCallback(result, false);
            });
    }

    /// <summary>
    /// Установка кита на сервере.
    /// </summary>
    protected static void RequestBodykitInstall(
        string                      installPath,
        string                      bodykitFieldName,
        UserVehicle                 userVehicle,
        Bodykit                     bodykit,
        Action<Http.Response, bool> finishCallback)
    {
        HangarController.Instance.isWaitingForSaving = true;

        var request = Http.Manager.Instance().CreateRequest(installPath);
        request.Form.AddField("tankId", (int)userVehicle.Info.id);
        request.Form.AddField(bodykitFieldName, bodykit.id.ToString());

        Http.Manager.StartAsyncRequest(
            request: request,
            successCallback: delegate(Http.Response result)
            {
                if (HangarController.Instance == null)
                    return;

                HangarController.Instance.isWaitingForSaving = false;
                finishCallback(result, true);
            },
            failCallback: delegate(Http.Response result)
            {
                if (HangarController.Instance == null)
                    return;

                HangarController.Instance.isWaitingForSaving = false;
                finishCallback(result, false);
            });
    }

    #endregion

    protected abstract bool IsOwned(int bodykitId);

    protected abstract PurchasedPattern GetOwned(int bodykitId);

    protected abstract Dictionary<VehicleInfo.VehicleParameter, ObscuredFloat> GetBodykitParameters(int bodykitId);

    protected abstract void TryOnBodykit(Bodykit bodykit);

    protected abstract void EndBodykitTrialWith(Bodykit bodykit);

    #region Callbacks

    protected virtual void OnRentNotEnoughMoney(Bodykit bodykit, ProfileInfo.Price price) { }

    protected virtual void OnRentSuccess(Bodykit bodykit, ProfileInfo.Price price)
    {
        HangarController.Instance.PlaySound(HangarController.Instance.moduleInstallSound);
        ShowRentingBox();
    }

    protected void ShowRentingBox()
    {
        HangarController.Instance.SetActionBoxType(HangarController.ActionBoxType.Renting);
        RentingBox.SetBonusStatusText(BodykitInView);
    }

    protected virtual void OnRentFailed(Http.Response response, Bodykit bodykit, ProfileInfo.Price price)
    {
        MessageBox.Show(new MessageBox.Data(MessageBox.Type.Info, Localizer.GetText("ApplicationError", response.ServerError), () => { }));
    }

    protected virtual void OnInstallSuccess(Bodykit bodykit)
    {
        HangarController.Instance.PlaySound(HangarController.Instance.moduleInstallSound);
        ShowRentingBox();
    }

    protected virtual void OnInstallFailed(Http.Response response, Bodykit bodykit)
    {
        MessageBox.Show(new MessageBox.Data(MessageBox.Type.Info, Localizer.GetText("ApplicationError", response.ServerError), () => { }));
    }

    #endregion

    #region Click callbacks

    /// <summary>
    /// Покупка нового кита.
    /// </summary>
    private void OnRentClick(string buttonName)
    {
        if (HangarController.Instance.isWaitingForSaving)
            return;

        if (CurrentBodykitId == bodykitInViewId)
            return;

        // check if common (not VIP) player clicked on Vip pattern
        if (BodykitInView.isVip && !ProfileInfo.IsPlayerVip)
        {
            HangarController.Instance.NavigateToVipShop(showMessageBox: true);
            return;
        }

        ProfileInfo.Price price = shopSpecialOfferInView == null ? BodykitInView.Price : shopSpecialOfferInView.DiscountPrice;   

        // check if player has enough currency
        if (!ProfileInfo.CanBuy(price))
        {
            HangarController.Instance.GoToBank(Bank.CurrencyToTab(price.currency), voiceRequired: true);
            OnRentNotEnoughMoney(BodykitInView, price);

            return;
        }

        BuyBodykit(
            userVehicle:    Shop.CurrentVehicle,
            bodykit:        BodykitInView,
            finishCallback: delegate(Http.Response response, bool result)
                            {
                                if (result)
                                    OnRentSuccess(BodykitInView, price);
                                else
                                    OnRentFailed(response, BodykitInView, price);
                            });
    }

    /// <summary>
    /// Установка уже купленного кита.
    /// </summary>
    private void OnResumeRentingClick(string buttonName)
    {
        if (CurrentBodykitId == bodykitInViewId)
            return;

        if (!IsOwned(bodykitInViewId))
        {
            //if (currentVehicle.Upgrades.OwnedCamouflages.All (camo => camo.id != bodykitInViewId)) {
            HangarController.Instance.SetActionBoxType(HangarController.ActionBoxType.Rent);
            return;
        }

        InstallBodykit(
            userVehicle:    Shop.CurrentVehicle,
            bodykit:        BodykitInView,
            finishCallback: delegate(Http.Response response, bool result)
                            {
                                if (result)
                                    OnInstallSuccess(BodykitInView);
                                else
                                    OnInstallFailed(response, BodykitInView);
                            });

        //currentVehicle.SetCamouflageById (bodykitInViewId);

        //HangarController.Instance.SetActionBoxType (HangarController.ActionBoxType.Renting);
        //rentingBox.SetBonusStatusText (GetBodykitById (bodykitInViewId));
        //ShowRentingInfo (GameData.CurrentTimeStamp);

        //HangarController.Instance.PlaySound (HangarController.Instance.moduleInstallSound);
        //ShowBodykit (bodykitInViewId, false, true);
        //HangarController.Instance.ShowUserInfo ();
        //ProfileInfo.SaveToServer ();
    }

    #endregion

    #region Server methods

    /// <summary>
    /// Покупка кита на сервере.
    /// </summary>
    private void BuyBodykit(UserVehicle userVehicle, Bodykit bodykit, Action<Http.Response, bool> finishCallback)
    {
        HangarController.Instance.isWaitingForSaving = true;
        RentBox.SetButtonActivated(false);

        Request request = Http.Manager.Instance().CreateRequest(rentPath);

        request.Form.AddField("tankId", (int)userVehicle.Info.id);
        request.Form.AddField(bodykitFieldName, bodykit.id.ToString());

        Http.Manager.StartAsyncRequest(
            request:            request,
            successCallback:    delegate(Http.Response result)
                                {
                                    if (HangarController.Instance == null)
                                        return;

                                    HangarController.Instance.isWaitingForSaving = false;
                                    RentBox.SetButtonActivated();
                                    ShowBodykit(bodykitId: bodykit.id, playSound: false, showPrice: true);
                                    finishCallback(result, true);
                                },
            failCallback:       delegate(Http.Response result)
                                {
                                    if (HangarController.Instance == null)
                                        return;

                                    HangarController.Instance.isWaitingForSaving = false;
                                    RentBox.SetButtonActivated();
                                    finishCallback(result, false);
                                });
    }

    /// <summary>
    /// Установка кита на сервере.
    /// </summary>
    private void InstallBodykit(UserVehicle userVehicle, Bodykit bodykit, Action<Http.Response, bool> finishCallback)
    {
        HangarController.Instance.isWaitingForSaving = true;
        RentedBox.SetButtonActivated(false);

        var request = Http.Manager.Instance().CreateRequest(installPath);
        request.Form.AddField("tankId", (int)userVehicle.Info.id);
        request.Form.AddField(bodykitFieldName, bodykit.id.ToString());

        Http.Manager.StartAsyncRequest(
            request:            request,
            successCallback:    delegate(Http.Response result)
                                {
                                    if (HangarController.Instance == null)
                                        return;

                                    HangarController.Instance.isWaitingForSaving = false;
                                    RentedBox.SetButtonActivated();
                                    ShowBodykit(bodykitId: bodykit.id, playSound: false, showPrice: true);
                                    finishCallback(result, true);
                                },
            failCallback:       delegate(Http.Response result)
                                {
                                    if (HangarController.Instance == null)
                                        return;

                                    HangarController.Instance.isWaitingForSaving = false;
                                    RentedBox.SetButtonActivated();
                                    finishCallback(result, false);
                                });
    }

    #endregion

    #region Panel rendering methods

    /// <summary>
    /// Инициализировать / обновить панель.
    /// </summary>
    protected override void FillPanel()
    {
        base.FillPanel();

        if (bodykitInViewId == 0 || GUIPager.ActivePageName != GuiPageName)
            return;

        SelectBodykit(
            toggle:         Selectors[bodykitInViewId].toggle,
            userVehicle:    null,
            immediately:    true,
            playSound:      false);

        ShowPrice(bodykitInViewId);
    }

    protected void OnBodykitSelect(tk2dUIToggleControl toggle)
    {
        SelectBodykit(
            toggle:         toggle,
            userVehicle:    null, 
            immediately:    false,
            playSound:      true);

        Dispatcher.Send(EventId.BodyKitSelected, new EventInfo_I(bodykitInViewId));
    }

    /// <summary>
    /// Обработка выбора кита в списке и скроллирование до него.
    /// </summary>
    protected virtual void SelectBodykit(tk2dUIToggleControl toggle, UserVehicle userVehicle, bool immediately, bool playSound)
    {
        bool allowedToPlaySound;

        SelectToggle(
            /*control:*/            toggle,
            /*immediately:*/        immediately,
            /*allowToPlaySound:*/   out allowedToPlaySound);

        int bodykitId;

        if (userVehicle != null)
            bodykitId = CurrentBodykitId;
        else
            Int32.TryParse(toggle.name.Substring(toggle.name.LastIndexOf('_') + 1), out bodykitId);

        bodykitInViewId = bodykitId;
        SetOfferInView(bodykitInViewId);

        ShowBodykit(
            bodykitId:  bodykitId,
            playSound:  playSound && allowedToPlaySound,
            showPrice:  true);
    }

    protected abstract void SetOfferInView(int offerId);

    /// <summary>
    /// Натягивание кита на танк и отображение изменений характеристик танка с ним.
    /// </summary>
    private void ShowBodykit(int bodykitId, bool playSound, bool showPrice)
    {
        if (bodykitId == 0)
            return;

        if (!Selectors.ContainsKey(bodykitId))
            return;

        if (playSound)
            HangarController.Instance.PlaySound(SelectSound);

        TryOnBodykit(Selectors[bodykitId].GetItem<Bodykit>());

        Dictionary<VehicleInfo.VehicleParameter, ObscuredFloat> primaryParameters = GetBodykitParameters(CurrentBodykitId);
        Dictionary<VehicleInfo.VehicleParameter, ObscuredFloat> secondaryParameters = GetBodykitParameters(bodykitId);

        HangarController.Instance.FillParameterDelta(
            bar:    HangarController.Instance.armorBar,
            max:    HangarController.Instance.ArmorMax[Shop.CurrentVehicle.Info.vehicleGroup],
            prim:   primaryParameters[VehicleInfo.VehicleParameter.Armor],
            sec:    secondaryParameters[VehicleInfo.VehicleParameter.Armor]);

        HangarController.Instance.FillParameterDelta(
            bar:    HangarController.Instance.attackBar,
            max:    HangarController.Instance.DamageMax[Shop.CurrentVehicle.Info.vehicleGroup],
            prim:   primaryParameters[VehicleInfo.VehicleParameter.Damage],
            sec:    secondaryParameters[VehicleInfo.VehicleParameter.Damage]);

        HangarController.Instance.FillParameterDelta(
            bar:    HangarController.Instance.rocketAttackBar,
            max:    HangarController.Instance.RocketDamageMax[Shop.CurrentVehicle.Info.vehicleGroup],
            prim:   primaryParameters[VehicleInfo.VehicleParameter.RocketDamage],
            sec:    secondaryParameters[VehicleInfo.VehicleParameter.RocketDamage]);

        HangarController.Instance.FillParameterDelta(
            bar:    HangarController.Instance.speedBar,
            max:    HangarController.Instance.SpeedMax[Shop.CurrentVehicle.Info.vehicleGroup],
            prim:   primaryParameters[VehicleInfo.VehicleParameter.Speed],
            sec:    secondaryParameters[VehicleInfo.VehicleParameter.Speed]);

        HangarController.Instance.FillParameterDelta(
            bar:    HangarController.Instance.rofBar,
            max:    HangarController.Instance.ROFMax[Shop.CurrentVehicle.Info.vehicleGroup],
            prim:   primaryParameters[VehicleInfo.VehicleParameter.RoF],
            sec:    secondaryParameters[VehicleInfo.VehicleParameter.RoF]);

        HangarController.Instance.FillParameterDelta(
            bar:    HangarController.Instance.ircmRofBar,
            max:    HangarController.Instance.IRCMROFMax[Shop.CurrentVehicle.Info.vehicleGroup],
            prim:   primaryParameters[VehicleInfo.VehicleParameter.IRCMRoF],
            sec:    secondaryParameters[VehicleInfo.VehicleParameter.IRCMRoF]);

        if (showPrice)
            ShowPrice(bodykitId);
    }

    /// <summary>
    /// Отображение Action Box'а с информацией о выбранном ките.
    /// </summary>
    private void ShowPrice(int bodykitId)
    {
        if (CurrentBodykitId == bodykitId)
        {
            HangarController.Instance.SetActionBoxType(HangarController.ActionBoxType.Renting);
            RentingBox.SetBonusStatusText(BodykitInView);
            ShowRentingInfo(GameData.CurrentTimeStamp);

            return;
        }

        if (IsOwned(bodykitId))
        {
            HangarController.Instance.SetActionBoxType(HangarController.ActionBoxType.Rented);
            RentedBox.SetBonusStatusText(BodykitInView);
            ShowRentingInfo(GameData.CurrentTimeStamp);

            return;
        }

        if (Selectors[bodykitId].GetItem<Bodykit>().availabilityLevel > ProfileInfo.Level)
        {
            HangarController.Instance.SetActionBoxType(HangarController.ActionBoxType.Inaccessible);
            HangarController.Instance.InaccessibleBoxLabel.Parameter = Selectors[bodykitId].GetItem<Bodykit>().availabilityLevel.ToString();

            return;
        }

        if (CheckForSaleActionBoxes())
            return;

        HangarController.Instance.SetActionBoxType(HangarController.ActionBoxType.Rent);

        RentBox.Price = new ProfileInfo.Price(Selectors[bodykitId].GetItem<Bodykit>().Price);

        RentBox.SetBonusStatusText(BodykitInView);
    }

    /// <summary>
    /// Обновление таймера оставшегося времени аренды кита
    /// </summary>
    private void ShowRentingInfo(double timestamp)
    {
        PurchasedPattern selectedOwnedBodykit = GetOwned(bodykitInViewId);

        if (selectedOwnedBodykit == null)
            return;

        rentingRemainingTimeSec = selectedOwnedBodykit.Deathtime - GameData.CurrentTimeStamp;

        RentingBox.Remain = (long)rentingRemainingTimeSec;
        RentedBox.Remain = (long)rentingRemainingTimeSec;
        RentingBox.Progress = (float)(1 + ((rentingRemainingTimeSec / selectedOwnedBodykit.Lifetime / 60 / 60) - 1));
    }

    private void UpdateActionBoxState(double timestamp)
    {
        if (!IsOwned(bodykitInViewId) && ProfileInfo.Level > BodykitInView.availabilityLevel)
        {
            if (CheckForSaleActionBoxes())
                return;

            HangarController.Instance.SetActionBoxType(HangarController.ActionBoxType.Rent);

            RentBox.Price = new ProfileInfo.Price(BodykitInView.Price);

            RentBox.SetBonusStatusText(BodykitInView);
        }
    }

    private bool CheckForSaleActionBoxes()
    {
        if ((this is PatternShop && PatternOffersController.Instance.CheckIfItemOnSale(bodykitInViewId)) ||
            this is DecalShop && DecalOffersController.Instance.CheckIfItemOnSale(bodykitInViewId))
        {
            HangarController.Instance.SetActionBoxType(HangarController.ActionBoxType.RentSale);

            SaleRentBox.Price = new ProfileInfo.Price(BodykitInView.Price);
            SaleRentBox.SetBonusStatusText(BodykitInView);

            return true;
        }

        return false;
    }

    #endregion
}
