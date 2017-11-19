using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class PatternOffersController : SpecialOffersController<PatternOffer>
{
    public tk2dTextMesh lblFreeCamo;

    public static Dictionary<Game, int> FreeVehicleCamos = new Dictionary<Game, int>
    {
        { Game.IronTanks,       11 },
        { Game.FutureTanks,     11 }, 
        { Game.ToonWars,        11 }, 
        { Game.SpaceJet,        11 }, 
        { Game.ApocalypticCars, 0  },
        { Game.BattleOfWarplanes,  11 },
        { Game.BattleOfHelicopters,  11 },
        { Game.Armada,  11 },
        { Game.WWR,  11 },
        { Game.FTRobotsInvasion,  5 },
    };

    public static PatternOffersController Instance { get; private set; }

    public static int VipBodykitPosY
    {
        get
        {
            var posY = 0;

            switch (GameData.ClearGameFlags(GameData.CurrentGame))
            {
                case Game.IronTanks:
                    posY = 215;
                    break;
                case Game.SpaceJet:
                    posY = 145;
                    break;
                case Game.BattleOfWarplanes:
                    posY = 145;
                    break;
                case Game.ToonWars:
                    posY = 140;
                    break;
                case Game.FutureTanks:
                    posY = 95;
                    break;
            }

            return posY;
        }
    }

    private Dictionary<int, PatternOffer> patternOffers = new Dictionary<int, PatternOffer>();
    public override Dictionary<int, PatternOffer> Offers { get { return patternOffers; } }

    public override bool AnyItemIsOnSale
    {
        get
        {
            return Offers != null &&
                        Offers.Any(patternOffer => patternOffer.Value.Remain > 0 && 
                            Shop.CurrentVehicle.Upgrades.OwnedCamouflages.All(camo => camo.id != patternOffer.Value.Id));
        }
    }

    protected override void Awake()
    {       
        base.Awake();
        Messenger.Subscribe(EventId.AfterHangarInit, SetFreePatternDetails);
        Messenger.Subscribe(EventId.OnLanguageChange, SetFreePatternDetails);

        Instance = this;
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
        Messenger.Unsubscribe(EventId.AfterHangarInit, SetFreePatternDetails);
        Messenger.Unsubscribe(EventId.OnLanguageChange, SetFreePatternDetails);

        Instance = null;
    }

    protected override bool IsOfferedItemIsLocked(int id)
    {
        return false;
    }

    public override bool CheckIfItemOnSale(int itemId)
    {
        return Shop.CurrentVehicle.Upgrades.OwnedCamouflages.All(camo => camo.id != itemId) && Offers.ContainsKey(itemId) && Offers[itemId].Remain > 0;
    }

    private void SetFreePatternDetails(EventId id, EventInfo info)
    {
        if(!HangarController.Instance.IsInitialized)
            return;

        var freeCamo = PatternPool.Instance.GetItemById(FreeVehicleCamos[GameData.ClearGameFlags(GameData.CurrentGame)]);

        if (freeCamo == null)
        {
            Debug.LogWarning("PatternShop doesn't contain pattern ID " + FreeVehicleCamos[GameData.ClearGameFlags(GameData.CurrentGame)]);
            return;
        }

        var gain
            = (GameData.IsGame(Game.BattleOfWarplanes | Game.ApocalypticCars | Game.BattleOfHelicopters)
                ? freeCamo.rocketDamageGain
                : freeCamo.damageGain)
                    * 100;

        lblFreeCamo.text = Localizer.GetText("lblFreeCamo", gain);
    }

    protected override void ParseOffers()
    {
        if (GameData.patternOffersList == null)
        {
            initialized = true;
            return;
        }

        patternOffers.Clear();

        foreach (var t in GameData.patternOffersList)
        {
            var prefs = new JsonPrefs(t);
            var endTime = prefs.ValueDouble("endTime", -1);
            var discount = prefs.ValueInt("discount", -1);
            var id = prefs.ValueInt("id", -1);

            var pattern = PatternPool.Instance.GetItemById(id);
            if (pattern == null || patternOffers.ContainsKey(id))
                continue;

            var patternOfferFrame = Instantiate(SpecialOffersPage.Instance.patternOfferPrefab);
            var patternOffer = patternOfferFrame.gameObject.AddComponent<PatternOffer>();

            patternOffer.Initialize(id, discount, endTime);
            patternOfferFrame.transform.SetParent(SpecialOffersPage.ScrollArea.contentContainer.transform);

            patternOffers.Add(id, patternOffer);
            SpecialOffersPage.SpecialOfferFrames.Add(patternOfferFrame);
        }

        RefreshOffers();
        initialized = true;
    }

    protected override void SetSpecialOffersList()
    {
        foreach (var patternOffer in patternOffers.Values)
        {
            HangarController.Instance.allOffersList.Add(patternOffer);
        }
    }
}
