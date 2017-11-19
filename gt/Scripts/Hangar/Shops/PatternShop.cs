using System;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using CodeStage.AntiCheat.ObscuredTypes;
using GAEvent;

using Action = GAEvent.Action;

public class PatternShop : BodyKitShop
{
    private const string RENT_PATH = "/shop/buyCamouflage";
    private const string INSTALL_PATH = "/shop/installCamouflage";
    private const string BODYKIT_FIELD_NAME = "camouflageId";

    public static PatternShop Instance { get; private set; }

   // protected override AudioClip SelectSound { get { return HangarController.Instance.patternSelectSound; } }

    public override string GuiPageName { get { return "PatternShop"; } }

    protected override Shop.ItemType ComparingMode
    {
        get
        {
            return Shop.ItemType.Vehicle |
                   Shop.ItemType.Module |
                   Shop.ItemType.Pattern;
        }
    }

    protected override int CurrentBodykitId
    {
        get { return Shop.CurrentVehicle.Upgrades.CamouflageId; }
    }

    protected override bool IsOwned(int bodykitId)
    {
        return Shop.CurrentVehicle.Upgrades.OwnedCamouflages.Any(camo => camo.id == bodykitId);
    }

    protected override PurchasedPattern GetOwned(int bodykitId)
    {
        return Shop.CurrentVehicle.Upgrades.OwnedCamouflages.FirstOrDefault(camo => camo.id == bodykitId);
    }

    protected override Dictionary<VehicleInfo.VehicleParameter, ObscuredFloat> GetBodykitParameters(int bodykitId)
    {
        return Shop.CurrentVehicle.GetParametersWith<Pattern>(bodykitId);
    }

    protected override void TryOnBodykit(Bodykit bodykit)
    {
        Shop.CurrentVehicle.TryOnCamouflage((Pattern)bodykit);
    }

    protected override void EndBodykitTrialWith(Bodykit bodykit)
    {
        Shop.CurrentVehicle.EndCamouflageTrialWith((Pattern)bodykit);
    }

    protected override IShopItem[] ShopItems { get { return PatternPool.Instance.Items; } }

    void Awake()
    {
        rentPath = RENT_PATH;
        installPath = INSTALL_PATH;
        bodykitFieldName = BODYKIT_FIELD_NAME;

        Instance = this;
    }

    void OnDestroy()
    {
        Instance = null;
    }

    public static void ForcedFillPanel()
    {
        BodyKitShop.ForcedFillPanel(PatternPool.Instance.Items, ShopManager.Instance.patternShopItemCellPrefab);
    }

    public static void RequestRent(UserVehicle userVehicle, Bodykit bodykit, Action<Http.Response, bool> finishCallback)
    {
        BodyKitShop.RequestBodykitRent(RENT_PATH, INSTALL_PATH, userVehicle, bodykit, finishCallback);
    }

    public static void RequestInstall(UserVehicle userVehicle, Bodykit bodykit, Action<Http.Response, bool> finishCallback)
    {
        BodyKitShop.RequestBodykitInstall(INSTALL_PATH, INSTALL_PATH, userVehicle, bodykit, finishCallback);
    }

    #region Callbacks

    protected override void OnRentNotEnoughMoney(Bodykit bodykit, ProfileInfo.Price price)
    {
        base.OnRentNotEnoughMoney(bodykit, price);

        #region Google Analytics: camouflage buying failure "not enough money"
        GoogleAnalyticsWrapper.LogEvent(
            new CustomEventHitBuilder()
                .SetParameter(Category.CamouflageBuying)
                .SetParameter(Action.NotEnoughMoney)
                .SetSubject(Subject.CamouflageID, bodykit.id)
                .SetParameter<Label>()
                .SetSubject(Subject.VehicleID, Shop.CurrentVehicle.Info.id));

        GoogleAnalyticsWrapper.LogEvent(
            new CustomEventHitBuilder()
                .SetParameter(Category.CamouflageBuying)
                .SetParameter(Action.NotEnoughMoney)
                .SetSubject(Subject.CamouflageID, bodykit.id)
                .SetParameter<Label>()
                .SetSubject(Subject.PlayerLevel, ProfileInfo.Level));
        #endregion
    }

    protected override void OnRentSuccess(Bodykit bodykit, ProfileInfo.Price price)
    {
        base.OnRentSuccess(bodykit, price);
        Dispatcher.Send(EventId.CamouflageBought, new EventInfo_I(bodykit.id));

        #region Google Analytics: camouflage bought
        GoogleAnalyticsWrapper.LogEvent(
            new CustomEventHitBuilder()
                .SetParameter(Category.CamouflageBuying)
                .SetParameter(Action.Bought)
                .SetSubject(Subject.CamouflageID, bodykitInViewId)
                .SetParameter<Label>()
                .SetSubject(Subject.VehicleID, Shop.CurrentVehicle.Info.id));

        GoogleAnalyticsWrapper.LogEvent(
            new CustomEventHitBuilder()
                .SetParameter(Category.CamouflageBuying)
                .SetParameter(Action.Bought)
                .SetSubject(Subject.CamouflageID, bodykitInViewId)
                .SetParameter<Label>()
                .SetSubject(Subject.PlayerLevel, ProfileInfo.Level));
        #endregion
    }

    #endregion

    protected override void SetOfferInView(int offerId)
    {
        shopSpecialOfferInView = PatternOffersController.Instance.Offers.ContainsKey(offerId) ? 
            PatternOffersController.Instance.Offers[offerId] : null;
    }

    protected override void SelectBodykit(tk2dUIToggleControl toggle, UserVehicle userVehicle, bool immediately, bool playSound)
    {
        base.SelectBodykit(toggle, userVehicle, immediately, playSound);
        Dispatcher.Send(EventId.PatternSelected, new EventInfo_I(bodykitInViewId));
    }

    public void SelectBodyKit(int patternId)
    {
        if (Selectors.ContainsKey(patternId))
        {
            SelectBodykit(
                toggle: Selectors[patternId].toggle,
                userVehicle: null,
                immediately: false,
                playSound: true);
        }
    }
}
