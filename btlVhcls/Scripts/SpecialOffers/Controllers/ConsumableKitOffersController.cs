using System.Linq;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class ConsumableKitOffersController : SpecialOffersController<ConsumableKitOffer>
{
    private Dictionary<int, ConsumableKitOffer> consumableKitOffers = new Dictionary<int, ConsumableKitOffer>();
    public override Dictionary<int, ConsumableKitOffer> Offers { get { return consumableKitOffers; } }

    public static ConsumableKitOffersController Instance { get; private set; }

    protected override void Awake()
    {
        base.Awake();
        Instance = this;
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
        Instance = null;
    }

    public override bool AnyItemIsOnSale
    {
        get 
        {
            return Offers != null && Offers.Any(kitOffer => kitOffer.Value.Remain > 0);
        }
    }

    public override bool CheckIfItemOnSale(int itemId)
    {
        return Offers.ContainsKey(itemId) && Offers[itemId].Remain > 0;
    }

    protected override void ParseOffers()
    {
        if (GameData.consumableKitOffersList == null)
        {
            initialized = true;
            return;
        }

        consumableKitOffers.Clear();

        foreach (var t in GameData.consumableKitOffersList)
        {
            var prefs = new JsonPrefs(t);
            var endTime = prefs.ValueDouble("endTime", -1);
            var discount = prefs.ValueInt("discount", -1);
            var id = prefs.ValueInt("id", -1);

            if(!GameData.consumableKitInfos.ContainsKey(id) || consumableKitOffers.ContainsKey(id))
                continue;

            var consumableKitOfferFrame = Instantiate(SpecialOffersPage.Instance.consumableKitOfferPrefab);
            var consKitOffer = consumableKitOfferFrame.gameObject.AddComponent<ConsumableKitOffer>();

            consKitOffer.Initialize(id, discount, endTime);
            consumableKitOfferFrame.transform.SetParent(SpecialOffersPage.ScrollArea.contentContainer.transform);

            consumableKitOffers.Add(id, consKitOffer);
            SpecialOffersPage.SpecialOfferFrames.Add(consumableKitOfferFrame);
        }

        RefreshOffers();
        initialized = true;
    }

    protected override bool IsOfferedItemIsLocked(int id)
    {
        return false;
    }

    protected override void SetSpecialOffersList()
    {
        foreach (var consumableKitOffer in consumableKitOffers.Values)
        {
            HangarController.Instance.allOffersList.Add(consumableKitOffer);
        }
    }
}
