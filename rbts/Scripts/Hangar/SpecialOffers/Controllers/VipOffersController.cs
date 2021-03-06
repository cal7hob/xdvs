using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System;

public class VipOffersController : SpecialOffersController<VipSpecialOffer>
{
    public Dictionary<string, VipSpecialOffer> vipSpecialOffers = new Dictionary<string, VipSpecialOffer>();
    public static VipOffersController Instance { get; private set; }
    public override Dictionary<int, VipSpecialOffer> Offers { get { return null; } }

    protected override void Awake()
    {
        Instance = this;
        Messenger.Subscribe(EventId.VipOffersInstantiated, Init);
        Messenger.Subscribe(EventId.ProfileInfoLoadedFromServer, ProfileInfoChanged);
    }

    protected override void OnDestroy()
    {
        Instance = null;
        Messenger.Unsubscribe(EventId.VipOffersInstantiated, Init);
        Messenger.Unsubscribe(EventId.ProfileInfoLoadedFromServer, ProfileInfoChanged);
    }

    protected override bool IsOfferedItemIsLocked(int id)
    {
        return false;
    }

    public override bool AnyItemIsOnSale
    {
        get
        {
            return vipSpecialOffers.Count > 0 && vipSpecialOffers.Any(offer => offer.Value.Remain > 0 && !offer.Value.IsLimited);
        }
    }

    protected override void ParseOffers()
    {
        if (GameData.vipsOffersList == null)
        {
            initialized = true;
            return;
        }

        vipSpecialOffers.Clear();
        foreach (var t in GameData.vipsOffersList)
        {
            var prefs = new JsonPrefs(t);
            var endTime = prefs.ValueDouble("endTime", -1);
            var diffDurationDays = prefs.ValueInt("durationDiff");
            var xDevsKey = prefs.ValueString("id");
            VipPrice.StoreId currentStoreId = null;

            foreach (var vipPrice in VipManager.Instance.VipPrices.Where(vipPrice => vipPrice.UnibillerId == xDevsKey))
            {
                currentStoreId = VipManager.GetCurrentStoreId(vipPrice);
                break;
            }

            if (VipManager.Instance.VipPrices.All(vipPrice => vipPrice.UnibillerId != xDevsKey) || currentStoreId == null || !VipManager.Instance.vipOffers.ContainsKey(currentStoreId.Id))
                continue;

            var vipSpecialOfferFrame = Instantiate(SpecialOffersPage.Instance.vipOfferPrefab);
            var vipSpecialOffer = vipSpecialOfferFrame.gameObject.AddComponent<VipSpecialOffer>();
            vipSpecialOffer.Initialize(currentStoreId, diffDurationDays, endTime);      
            vipSpecialOffer.transform.SetParent(SpecialOffersPage.ScrollArea.contentContainer.transform);
            
            SpecialOffersPage.SpecialOfferFrames.Add(vipSpecialOfferFrame);
        }

        RefreshOffers();
        initialized = true;
    }

    protected override void SetSpecialOffersList()
    {
        foreach (var vipSpecialOffer in vipSpecialOffers.Values)
        {
            HangarController.Instance.allOffersList.Add(vipSpecialOffer);
        }
    }

    public override void RefreshOffers()
    {
        foreach (var offer in vipSpecialOffers)
        {
            offer.Value.UnsubscribeFromTimer();
            offer.Value.SubscribeOnTimer();
            offer.Value.UpdateItem();
        }
    }

    public bool CheckIfVipOfferOnSale(string currentStoreId)
    {
        return vipSpecialOffers.ContainsKey(currentStoreId) && vipSpecialOffers[currentStoreId].Remain > 0;
    }

    public override bool CheckIfItemOnSale(int itemId)
    {
        return Offers.ContainsKey(itemId) && Offers[itemId].Remain > 0;
    }

    private void ProfileInfoChanged (EventId id, EventInfo info)
    {
        RefreshOffers ();
    }
}
