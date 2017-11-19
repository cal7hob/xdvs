using System.Linq;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class DecalOffersController : SpecialOffersController<DecalOffer>
{
    private Dictionary<int, DecalOffer> decalOffers = new Dictionary<int, DecalOffer>();
    public override Dictionary<int, DecalOffer> Offers { get { return decalOffers; } }

    public static DecalOffersController Instance { get; private set; }

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
            return Offers != null
                && Offers.Any(decalOffer => decalOffer.Value.Remain > 0 &&
                Shop.CurrentVehicle.Upgrades.OwnedDecals.All(camo => camo.id != decalOffer.Value.Id));
        }
    }

    public override bool CheckIfItemOnSale(int itemId)
    {
        return Shop.CurrentVehicle.Upgrades.OwnedDecals.All(camo => camo.id != itemId) && Offers.ContainsKey(itemId) && Offers[itemId].Remain > 0;
    }

    protected override void ParseOffers()
    {
        if (GameData.decalOffersList == null)
        {
            initialized = true;
            return;
        }

        decalOffers.Clear();

        foreach (var t in GameData.decalOffersList)
        {
            var prefs = new JsonPrefs(t);
            var endTime = prefs.ValueDouble("endTime", -1);
            var discount = prefs.ValueInt("discount", -1);
            var id = prefs.ValueInt("id", -1);

            var decal = DecalPool.Instance.GetItemById(id);
            if(decal == null || decalOffers.ContainsKey(id))
                continue;

            var decalOfferFrame = Instantiate(SpecialOffersPage.Instance.decalOfferPrefab);
            var decalOffer = decalOfferFrame.gameObject.AddComponent<DecalOffer>();

            decalOffer.Initialize(id, discount, endTime);
            decalOfferFrame.transform.SetParent(SpecialOffersPage.ScrollArea.contentContainer.transform);

            decalOffers.Add(id, decalOffer);
            SpecialOffersPage.SpecialOfferFrames.Add(decalOfferFrame);
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
        foreach (var decalOffer in decalOffers.Values)
        {
            HangarController.Instance.allOffersList.Add(decalOffer);
        }
    }
}
