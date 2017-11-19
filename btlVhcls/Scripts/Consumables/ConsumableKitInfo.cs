using System;
using UnityEngine;
using System.Collections.Generic;
using CodeStage.AntiCheat.ObscuredTypes;
using XDevs;

public class ConsumableKitInfo
{
    public readonly ObscuredInt id;
    public readonly ObscuredBool isHidden;
    public readonly ObscuredBool isVip;
    public readonly int position;
    public readonly ProfileInfo.Price price;
    public readonly string icon;
    public readonly string localizationKey;
    public List<Entity> items;//not sorted, as server sends

    public DiscountInfo discount = null;
    public ProfileInfo.Price CurPrice { get { return discount != null && discount.IsActive ? discount.GetDiscountPrice(price) : price; } }

    public ConsumableKitInfo(Dictionary<string, object> initDict)
    {
        int intId = 0;
        Dictionary<string, object> priceDict = null;
        bool boolIsHidden = false;
        bool boolIsVip = false;

        bool allDataReceived = true;
        allDataReceived &= initDict.Extract("id", ref intId);
        allDataReceived &= initDict.Extract("price", ref priceDict);
        allDataReceived &= initDict.Extract("position", ref position);
        allDataReceived &= initDict.Extract("icon", ref icon);
        allDataReceived &= initDict.Extract("localizationKey", ref localizationKey);
        allDataReceived &= initDict.Extract("isHidden", ref boolIsHidden);
        allDataReceived &= initDict.Extract("isVip", ref boolIsVip);

        if (!allDataReceived)
            Debug.LogErrorFormat("ConsumableKitInfo parsing errors!!! id  = {0}", intId);

        id = intId;
        isHidden = boolIsHidden;
        isVip = boolIsVip;
        if (priceDict != null)
            price = ProfileInfo.Price.FromDictionary(priceDict);

        JsonPrefs prefs = new JsonPrefs(initDict);
        if (prefs.Contains("items"))
        {
            items = new List<Entity>();
            List<object> list = prefs.ValueObjectList("items");
            foreach (var item in list)
                items.Add(new Entity((Dictionary<string, object>)item));
        }
    }

    public bool IsEnabledByVipStatus
    {
        get
        {
            return ProfileInfo.IsPlayerVip ? true : !isVip;
        }
    }
}
