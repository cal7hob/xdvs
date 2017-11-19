using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class BankItemPrice
{
    public ProfileInfo.PriceCurrency currency;
    public string xdevsId;
    public int rawPrice;
    public int extraFreePercent;
    public int order;
    public BankLot bankLot;

    public int ExtraFreeValue { get { return rawPrice * extraFreePercent / 100; } }
    public int FullPriceValue { get { return rawPrice + ExtraFreeValue; } }
    public string SpriteName { get { return string.Format("{0}_{1}", currency.ToString().ToLower(), order); } }
    public string LocalizationKey { get { return "lblBuyGold" + (order+1); } } // Костыль с +1 из-за убранной покупки в 20 голды
    public ProfileInfo.Price FullPrice { get { return new ProfileInfo.Price(FullPriceValue, currency); } }
    public ProfileInfo.Price PriceWithoutBonus { get { return new ProfileInfo.Price(rawPrice, currency); } }
    


    public BankItemPrice(ProfileInfo.PriceCurrency _currency, string _xdevsId, int _rawPrice, int _extraFreePercent, int _order)
    {
        currency = _currency;
        xdevsId = _xdevsId;
        rawPrice = _rawPrice;
        extraFreePercent = _extraFreePercent;
        order = _order;
    }
}

public static class BankData
{
    private static bool isInited = false;
    public static Dictionary<string, BankItemPrice> prices = new Dictionary<string, BankItemPrice>()
    {
        {"xdevs.44_gold", new BankItemPrice(ProfileInfo.PriceCurrency.Gold, "xdevs.44_gold",40,10, 0)},
        {"xdevs.115_gold", new BankItemPrice(ProfileInfo.PriceCurrency.Gold, "xdevs.115_gold",100,15, 1)},
        {"xdevs.250_gold", new BankItemPrice(ProfileInfo.PriceCurrency.Gold, "xdevs.250_gold",200,25, 2)},
        {"xdevs.750_gold", new BankItemPrice(ProfileInfo.PriceCurrency.Gold, "xdevs.750_gold",500,50, 3)},
        {"xdevs.1_650_gold", new BankItemPrice(ProfileInfo.PriceCurrency.Gold, "xdevs.1_650_gold",1000,65, 4)},
        {"xdevs.3_400_gold", new BankItemPrice(ProfileInfo.PriceCurrency.Gold, "xdevs.3_400_gold",2000,70, 5)},

        {"xdevs.50_000_silver", new BankItemPrice(ProfileInfo.PriceCurrency.Silver, "xdevs.50_000_silver",40000,25, 0)},
        {"xdevs.150_000_silver", new BankItemPrice(ProfileInfo.PriceCurrency.Silver, "xdevs.150_000_silver",100000,50, 1)},
        {"xdevs.400_000_silver", new BankItemPrice(ProfileInfo.PriceCurrency.Silver, "xdevs.400_000_silver",200000,100, 2)},
        {"xdevs.1_250_000_silver", new BankItemPrice(ProfileInfo.PriceCurrency.Silver, "xdevs.1_250_000_silver",500000,150, 3)},
        {"xdevs.3_000_000_silver", new BankItemPrice(ProfileInfo.PriceCurrency.Silver, "xdevs.3_000_000_silver",1000000,200, 4)},
        {"xdevs.7_000_000_silver", new BankItemPrice(ProfileInfo.PriceCurrency.Silver, "xdevs.7_000_000_silver",2000000,250, 5)},
    };
    public static Dictionary<ProfileInfo.PriceCurrency, List<BankItemPrice>> pricesSortedByCurrency;

    public static void Init()
    {
        if (isInited)
            return;

        pricesSortedByCurrency = new Dictionary<ProfileInfo.PriceCurrency, List<BankItemPrice>>();
        pricesSortedByCurrency.Add(ProfileInfo.PriceCurrency.Gold, prices.Select(item => item.Value).Where(item => item.currency == ProfileInfo.PriceCurrency.Gold).OrderBy(item => item.order).ToList());
        pricesSortedByCurrency.Add(ProfileInfo.PriceCurrency.Silver, prices.Select(item => item.Value).Where(item => item.currency == ProfileInfo.PriceCurrency.Silver).OrderBy(item => item.order).ToList());



        isInited = true;
    }
    
}
