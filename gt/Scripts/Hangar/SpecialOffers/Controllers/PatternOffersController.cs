using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class PatternOffersController : SpecialOffersController<PatternOffer>
{
    public tk2dTextMesh lblFreeCamo;

    public static Dictionary<Game, int> FreeVehicleCamos = new Dictionary<Game, int>
    {
        { Game.WWT2,  11 },
    };

    public static PatternOffersController Instance { get; private set; }

    public static int VipBodykitPosY
    {
        get
        {
            var posY = 0;

            switch (GameData.ClearGameFlags(GameData.CurrentGame))
            {
                //case Game.IronTanks:
                //    posY = 215;
                //    break;
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
        Dispatcher.Subscribe(EventId.AfterHangarInit, SetFreePatternDetails);
        Dispatcher.Subscribe(EventId.OnLanguageChange, SetFreePatternDetails);

        Instance = this;
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
        Dispatcher.Unsubscribe(EventId.AfterHangarInit, SetFreePatternDetails);
        Dispatcher.Unsubscribe(EventId.OnLanguageChange, SetFreePatternDetails);

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

        var gain =  freeCamo.damageGain * 100;

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
            GameData.allOffersList.Add(patternOffer);
        }
    }
}
