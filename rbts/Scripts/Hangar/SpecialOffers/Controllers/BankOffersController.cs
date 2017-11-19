using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class BankOffersController : SpecialOffersController<BankOffer>
{
    private Dictionary<string, BankOffer> silverBankOffers = new Dictionary<string, BankOffer>(); 
    private Dictionary<string, BankOffer> goldBankOffers = new Dictionary<string, BankOffer>();
    

    public static BankOffersController Instance { get; private set; }
    public static Dictionary<string, BankOffer> SilverBankOffers { get { return Instance.silverBankOffers; } }
    public static Dictionary<string, BankOffer> GoldBankOffers { get { return Instance.goldBankOffers; } }


    public static bool GoldSale
    {
        get
        {
            return Instance.AnyItemOfGivenBankOffersOnSale(GoldBankOffers);
        }
    }

    public static bool SilverSale
    {
        get
        {
            return Instance.AnyItemOfGivenBankOffersOnSale(SilverBankOffers);
        }
    }

    public override bool AnyItemIsOnSale
    {
        get { return GoldSale | SilverSale; } 
    }

    protected override void Awake()
    {
        Messenger.Subscribe(EventId.BankInitialized, Init);

        Instance = this;
    }

    protected override void OnDestroy()
    {
        Messenger.Unsubscribe(EventId.BankInitialized, Init);

        Instance = null;
    }

    protected override bool IsOfferedItemIsLocked(int id)
    {
        return false;
    }

    protected override void SetSpecialOffersList()
    {
        foreach (var goldBankOffer in goldBankOffers.Values)
        {
            HangarController.Instance.allOffersList.Add(goldBankOffer);
        }

        foreach (var silverBankOffer in silverBankOffers.Values)
        {
            HangarController.Instance.allOffersList.Add(silverBankOffer);
        }
    }

    public override Dictionary<int, BankOffer> Offers { get { return null; } }

    public override void Init(EventId id, EventInfo info)
    {
        base.Init(id, info);
        if(GoldSale)
        {
            Bank.Instance.SetBankLotsOfGivenCurrency(ProfileInfo.PriceCurrency.Gold);
            HangarController.OnTimerTick += CheckIfGoldSale;
        }

        if (SilverSale)
        {
            Bank.Instance.SetBankLotsOfGivenCurrency(ProfileInfo.PriceCurrency.Silver);
            HangarController.OnTimerTick += CheckIfSilverSale;
        }   
    }

    public void CheckIfSilverSale(double d)
    {
        if(SilverSale) return;

        HangarController.OnTimerTick -= CheckIfSilverSale;
        Bank.Instance.SetBankLotsOfGivenCurrency(ProfileInfo.PriceCurrency.Silver);
    }

    public void CheckIfGoldSale(double d)
    {
        if (GoldSale) return;

        HangarController.OnTimerTick -= CheckIfGoldSale;
        Bank.Instance.SetBankLotsOfGivenCurrency(ProfileInfo.PriceCurrency.Gold);
    }

    public override bool CheckIfItemOnSale(int itemId)
    {
        return Offers.ContainsKey(itemId) && Offers[itemId].Remain > 0;
    }

    protected override void ParseOffers()
    {
        if(GameData.bankOffersList == null)
        {
            initialized = true;
            return;
        }

        SilverBankOffers.Clear();
        GoldBankOffers.Clear();

        foreach (var t in GameData.bankOffersList)
        {
            var prefs = new JsonPrefs(t);
            var endTime = prefs.ValueDouble("endTime", -1);
            var price = prefs.ValuePrice("price");
            var xDevsKey = prefs.ValueString("id");

            if(!BankData.prices.ContainsKey(xDevsKey))
                continue;

            var bankOfferFrame = Instantiate(SpecialOffersPage.Instance.bankOfferPrefab);
            var bankOffer = bankOfferFrame.gameObject.AddComponent<BankOffer>();

            bankOffer.Initialize(xDevsKey, price, endTime);  
            bankOfferFrame.transform.SetParent(SpecialOffersPage.ScrollArea.contentContainer.transform);

            switch (bankOffer.Price.currency)
            {
                case ProfileInfo.PriceCurrency.Gold: GoldBankOffers.Add(xDevsKey, bankOffer); break;
                case ProfileInfo.PriceCurrency.Silver: SilverBankOffers.Add(xDevsKey, bankOffer); break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            SpecialOffersPage.SpecialOfferFrames.Add(bankOfferFrame);
        }

        RefreshOffers();
        initialized = true;
    }

    public bool AnyItemOfGivenBankOffersOnSale(Dictionary<string, BankOffer> offers)
    {
        return offers != null && offers.Count > 0 && offers.Any(offer => offer.Value.Remain > 0 && !offer.Value.IsLimited);
    }

    public override void RefreshOffers()
    {
        foreach (var offer in GoldBankOffers)
        {
            offer.Value.UnsubscribeFromTimer();
            offer.Value.SubscribeOnTimer();
        }

        foreach (var offer in SilverBankOffers)
        {
            offer.Value.UnsubscribeFromTimer();
            offer.Value.SubscribeOnTimer();
        }
    }

    public static Dictionary<string, BankOffer> GetBankOffersByCurrency(ProfileInfo.PriceCurrency currency)
    {
        return currency == ProfileInfo.PriceCurrency.Silver ? SilverBankOffers : GoldBankOffers;
    }
}
