using System;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;

using CodeStage.AntiCheat.ObscuredTypes;

public class DecalShop : BodyKitShop
{
    private const string RENT_PATH = "/shop/buyDecal";
    private const string INSTALL_PATH = "/shop/installDecal";
    private const string BODYKIT_FIELD_NAME = "decalId";

    public static DecalShop Instance { get; private set; }

   // protected override AudioClip SelectSound { get { return HangarController.Instance.decalSelectSound; } }

    public override string GuiPageName { get { return "DecalShop"; } }

    protected override Shop.ItemType ComparingMode
    {
        get
        {
            return Shop.ItemType.Vehicle |
                   Shop.ItemType.Module |
                   Shop.ItemType.Decal;
        }
    }

    protected override int CurrentBodykitId
    {
        get { return Shop.CurrentVehicle.Upgrades.DecalId; }
    }

    protected override bool IsOwned(int bodykitId)
    {
        return Shop.CurrentVehicle.Upgrades.OwnedDecals.Any(decal => decal.id == bodykitId);
    }

    protected override PurchasedPattern GetOwned(int bodykitId)
    {
        return Shop.CurrentVehicle.Upgrades.OwnedDecals.FirstOrDefault(decal => decal.id == bodykitId);
    }

    protected override Dictionary<VehicleInfo.VehicleParameter, ObscuredFloat> GetBodykitParameters(int bodykitId)
    {
        return Shop.CurrentVehicle.GetParametersWith<Decal>(bodykitId);
    }

    protected override void TryOnBodykit(Bodykit bodykit)
    {
        Shop.CurrentVehicle.TryOnDecal((Decal)bodykit);
    }

    protected override void EndBodykitTrialWith(Bodykit bodykit)
    {
        Shop.CurrentVehicle.EndDecalTrialWith((Decal)bodykit);
    }

    protected override IShopItem[] ShopItems { get { return DecalPool.Instance.Items; } }

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
        BodyKitShop.ForcedFillPanel(DecalPool.Instance.Items, ShopManager.Instance.decalShopItemCellPrefab);
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

        #region Google Analytics: sticker buying failure "not enough money"
        GoogleAnalyticsWrapper.LogEvent(
            new CustomEventHitBuilder()
                .SetParameter(GAEvent.Category.StickerBuying)
                .SetParameter(GAEvent.Action.NotEnoughMoney)
                .SetSubject(GAEvent.Subject.StickerID, bodykitInViewId)
                .SetParameter<GAEvent.Label>()
                .SetSubject(GAEvent.Subject.VehicleID, Shop.CurrentVehicle.Info.id));

        GoogleAnalyticsWrapper.LogEvent(
            new CustomEventHitBuilder()
                .SetParameter(GAEvent.Category.StickerBuying)
                .SetParameter(GAEvent.Action.NotEnoughMoney)
                .SetSubject(GAEvent.Subject.StickerID, bodykitInViewId)
                .SetParameter<GAEvent.Label>()
                .SetSubject(GAEvent.Subject.PlayerLevel, ProfileInfo.Level));

        #endregion
    }

    protected override void OnRentSuccess(Bodykit bodykit, ProfileInfo.Price price)
    {
        base.OnRentSuccess(bodykit, price);

        MenuController.SetActionBoxType(ActionBoxType.Renting);

        #region Google Analytics: sticker bought
        GoogleAnalyticsWrapper.LogEvent(
            new CustomEventHitBuilder()
                .SetParameter(GAEvent.Category.StickerBuying)
                .SetParameter(GAEvent.Action.Bought)
                .SetSubject(GAEvent.Subject.StickerID, bodykitInViewId)
                .SetParameter<GAEvent.Label>()
                .SetSubject(GAEvent.Subject.VehicleID, Shop.CurrentVehicle.Info.id));

        GoogleAnalyticsWrapper.LogEvent(
            new CustomEventHitBuilder()
                .SetParameter(GAEvent.Category.StickerBuying)
                .SetParameter(GAEvent.Action.Bought)
                .SetSubject(GAEvent.Subject.StickerID, bodykitInViewId)
                .SetParameter<GAEvent.Label>()
                .SetSubject(GAEvent.Subject.PlayerLevel, ProfileInfo.Level));

        #endregion
    }

    #endregion

    protected override void SetOfferInView(int offerId)
    {
        shopSpecialOfferInView = DecalOffersController.Instance.Offers.ContainsKey(offerId) ? 
            DecalOffersController.Instance.Offers[offerId] : null;
    }

    public void SelectBodyKit(int decalId)
    {
        if (Selectors.ContainsKey(decalId))
        {
            SelectBodykit(
                toggle: Selectors[decalId].toggle,
                userVehicle: null,
                immediately: false,
                playSound: true);
        }
    }
}
