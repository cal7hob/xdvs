using CodeStage.AntiCheat.ObscuredTypes;
using System.Collections.Generic;
using UnityEngine;
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

    public static void LoadConsumableKitInfos(JsonPrefs options)
    {
        if (!GameData.vehiclesDataStorage.ContainsKey("kits"))
        {
            Debug.LogError("No consumableKits info presented in data received from server!");
            return;
        }

        List<object> consKitsList = GameData.vehiclesDataStorage["kits"] as List<object>;
        GameData.vehiclesDataStorage.Remove("kits");

        if (consKitsList == null)
        {
            Debug.LogError("Invalid consKitsList info format in server data");
            return;
        }

        GameData.consumableKitInfos = new Dictionary<int, ConsumableKitInfo>(consKitsList.Count);

        foreach (Dictionary<string, object> consKitsDict in consKitsList)
        {
            ConsumableKitInfo newConsumableKit = new ConsumableKitInfo(consKitsDict);
            GameData.consumableKitInfos.Add(newConsumableKit.id, newConsumableKit);
        }

        // Грузим акции
        GameData.consumableKitOffersList = options.Contains("DiscountOfferItems/KitOffers") ? options.ValueObjectList("DiscountOfferItems/KitOffers") : null;

        if(GameData.consumableKitOffersList != null)
        {
            foreach (var t in GameData.consumableKitOffersList)
            {
                var prefs = new JsonPrefs(t);
                var id = prefs.ValueInt("id", -1);

                if(GameData.consumableKitInfos.ContainsKey(id))
                    GameData.consumableKitInfos[id].discount = new XDevs.DiscountInfo(prefs);
            }
        }
    }
}
