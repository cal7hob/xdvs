using System.Collections.Generic;
using System.Linq;
using CodeStage.AntiCheat.ObscuredTypes;
using UnityEngine;

public class VehicleOffersController : SpecialOffersController<VehicleOffer>
{
    public static Dictionary<Game, int> FreeVehicleIds = new Dictionary<Game, int>
    {
        { Game.IronTanks,       6 }, // Orbis
        { Game.FutureTanks,     6 }, // Spider sr16t
        { Game.ToonWars,        6 }, // KV2B
        { Game.SpaceJet,        6 }, // Harpy
        { Game.ApocalypticCars, 0 }, // Никакая
        { Game.BattleOfWarplanes, 6 },
        { Game.BattleOfHelicopters, 6 },
        { Game.Armada, 6 },
        { Game.WWR, 6 },
        { Game.FTRobotsInvasion, 6 },// Warrior Z27A
    };

    public tk2dTextMesh lblFreeTankDetails;

    private Dictionary<int, VehicleOffer> vehicleOffers = new Dictionary<int, VehicleOffer>();
    public override Dictionary<int, VehicleOffer> Offers { get { return vehicleOffers; } }
    public static VehicleOffersController Instance { get; private set; }

    public override bool AnyItemIsOnSale 
    {
        get
        {
            // вернет true, если есть хоть один танк, который на скидки и не скрыт
            return Offers != null &&
                        Offers.Any(
                            vehicleOffer =>
                                vehicleOffer.Value.Remain > 0 &&
                                ProfileInfo.vehicleUpgrades.All(tank => tank.Value.vehicleId != vehicleOffer.Value.Id && 
                                VehicleShop.Selectors.ContainsKey(vehicleOffer.Value.Id)));
        }
    }

    protected override void Awake()
    {
        Instance = this;
        base.Awake();
        Messenger.Subscribe(EventId.AfterHangarInit, SetFreeVehicleDetails);
        Messenger.Subscribe(EventId.BankInitialized, SetFreeVehicleDetails);
        Messenger.Subscribe(EventId.OnLanguageChange, SetFreeVehicleDetails);
        Messenger.Subscribe(EventId.ProfileInfoLoadedFromServer, OnProfileLoaded);
    }

    protected override void OnDestroy()
    {
        Instance = null;
        base.OnDestroy();
        Messenger.Unsubscribe(EventId.AfterHangarInit, SetFreeVehicleDetails);
        Messenger.Unsubscribe(EventId.BankInitialized, SetFreeVehicleDetails);
        Messenger.Unsubscribe(EventId.OnLanguageChange, SetFreeVehicleDetails);
        Messenger.Unsubscribe(EventId.ProfileInfoLoadedFromServer, OnProfileLoaded);
    }

    public override bool CheckIfItemOnSale(int itemId)
    {
        return !ProfileInfo.vehicleUpgrades.ContainsKey(itemId) && Offers.ContainsKey(itemId) && Offers[itemId].Remain > 0;
    }

    protected override void ParseOffers()
    {
        if (GameData.vehicleOffersList == null)
        {
            initialized = true;
            return;
        }

        vehicleOffers.Clear();
        foreach (var t in GameData.vehicleOffersList)
        {
            var prefs = new JsonPrefs(t);
            var endTime = prefs.ValueDouble("endTime", -1);
            var discount = prefs.ValueInt("discount", -1);
            var id = prefs.ValueInt("id", -1);
            
            if (!VehicleShop.Selectors.ContainsKey(id) 
                || id == FreeVehicleIds[GameData.ClearGameFlags(GameData.CurrentGame)] 
                || vehicleOffers.ContainsKey(id))
            {
                continue;
            }

            // 17.11.2016 закомментил, тк была поставлена задача показывать скидки на недоступной технике
            //if (IsOfferedItemIsLocked(id))
            //{
            //    continue;
            //}

            var vehicleOfferFrame = Instantiate(SpecialOffersPage.Instance.vehicleOfferPrefab);
            var vehicleOffer = vehicleOfferFrame.gameObject.GetComponent<VehicleOffer>();

            if (vehicleOffer == null)
            {
                Debug.LogError("VehicleOffersController.ParseOffers(): vehicleOffer == null");
                return;
            }

            vehicleOffer.Initialize(id, discount, endTime);
            vehicleOfferFrame.transform.SetParent(SpecialOffersPage.ScrollArea.contentContainer.transform);

            vehicleOffers.Add(id, vehicleOffer);
            SpecialOffersPage.SpecialOfferFrames.Add(vehicleOfferFrame);
        }

        RefreshOffers();
        initialized = true;
    }

    protected override bool IsOfferedItemIsLocked(int id)
    {
        return VehicleShop.Selectors[id].ShopItem.LockCondition;
    }

    protected override void SetSpecialOffersList()
    {
        foreach (var vehicleOffer in vehicleOffers.Values)
        {
            HangarController.Instance.allOffersList.Add(vehicleOffer);
        }
    }

    public static void OnProfileLoaded(EventId id, EventInfo info)
    {
        if (IsOwnFreeVehicle)
        {
            HideFreeVehicleFrames();   
        }
    }

    public static bool IsOwnFreeVehicle
    {
        get
        {
            return ProfileInfo.vehicleUpgrades.ContainsKey(FreeVehicleIds[GameData.ClearGameFlags(GameData.CurrentGame)]);
        }
    }

    public static void HideFreeVehicleFrames()
    {
        if (Bank.FreeTankGold)
        {
            Bank.SetActiveBankLot(Bank.Instance.goldScrollArea, Bank.Instance.goldPanelTransforms, Bank.FreeTankGold, false);
        }

        if (Bank.FreeTankSilver)
        {
            Bank.SetActiveBankLot(Bank.Instance.silverScrollArea, Bank.Instance.silverPanelTransforms, Bank.FreeTankSilver, false);
        }  
    }

    public void ShowFreeVehicleDetails(tk2dUIItem uiItem)
    {
        GUIPager.SetActivePage("FreeTankDetails", false, true);
    }

    public static void OnRecieveFreeVehicleClick(string btnName)
    {
        HangarController.Instance.GoToBank(Bank.Tab.Gold, false);

        #region Google Analytics: free tank offer acceptance

        GoogleAnalyticsWrapper.LogEvent(
            new CustomEventHitBuilder()
                .SetParameter(GAEvent.Category.SpecialOffer)
                .SetParameter<GAEvent.Action>()
                .SetSubject(GAEvent.Subject.FreeVehicle)
                .SetParameter(GAEvent.Label.Accepted)
                .SetValue(ProfileInfo.Level));

        #endregion
    }

    public static void OnCancelRecieveFreeVehicleClick(string btnName)
    {
        GUIPager.Back();
    }

    private void SetFreeVehicleDetails(EventId id, EventInfo info)
    {
        if (id == EventId.AfterHangarInit)
        {
            lblFreeTankDetails.text = Localizer.GetText("lblFreeTankDetails", 0);
        }

#if UNITY_WEBPLAYER || UNITY_WEBGL
        lblFreeTankDetails.text = Localizer.GetText("lblFreeTankDetails", SocialSettings.GetSocialService().GetPriceStringById("xdevs.250_gold"));
#else
        lblFreeTankDetails.text = Localizer.GetText("lblFreeTankDetails", PriceLocalizationAgent.GetLocalizedString("xdevs.250_gold", lblFreeTankDetails, "lblBuyGold4"));
#endif
    }
}
