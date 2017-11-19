using System;
using System.CodeDom;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using CodeStage.AntiCheat.ObscuredTypes;
using Facebook.Unity;

public struct SimplePrice
{
    public ProfileInfo.PriceCurrency currency;
    public ObscuredInt value;
    
    public SimplePrice(int value, ProfileInfo.PriceCurrency currency)
    {
        this.currency = currency;
        this.value = value;
    }

    public static SimplePrice FromDictionary(Dictionary<string, object> dict)
    {
        JsonPrefs data = new JsonPrefs(dict);
        return new SimplePrice(data.ValueInt("value"),
            dict.ExtractOrDefault("currency", ProfileInfo.PriceCurrency.Silver));
    }

    public ProfileInfo.Price ToPrice()
    {
        return new ProfileInfo.Price(value, currency);
    }
}

public struct ConsumableInventoryCell
{
    public ObscuredInt consumableId;
    public ObscuredInt amount;

    public ConsumableInventoryCell(int id, int amount)
    {
        consumableId = id;
        this.amount = amount;
    }
}

public class ConsumableInfo
{
    public readonly ObscuredInt id;
    public readonly int position;
    public readonly ConsumableTargetType target;
    //private readonly ObscuredInt availabilityLevel;
    public readonly bool isHidden;
    public readonly bool isVip;
    public readonly string name;
    public readonly SimplePrice price;
    public readonly ObscuredInt countToBuy;
    public readonly ObscuredInt maxInBattle;
    public readonly ObscuredFloat firstDelay;
    public readonly ObscuredFloat reloadTime;
    public readonly ObscuredFloat duration;
    public readonly ConsumableTargetType targetType = ConsumableTargetType.None;
    public readonly string icon;

    public readonly List<VehicleEffectData> effects;
    public readonly string prefabName;

    public readonly ObscuredFloat radius;
    public readonly ObscuredFloat range;
    public readonly ObscuredFloat activationTime;
    public readonly ObscuredFloat activationRadius;
    public readonly DamageCalcContext powerContext = DamageCalcContext.None;
    public readonly VehicleEffect.ModifierType powerModifier;
    public readonly VehicleEffect.ParameterType powerParameter;
    public readonly ObscuredFloat powerValue;

    public string IconWithFrame { get { return string.Format("{0}{1}", icon, GameData.CONSUMABLES_SPRITE_FRAMED_VERSION_SUFFIX); } }
    public string GetIcon(bool withFrame) { return withFrame ? IconWithFrame : icon; }

    public ConsumableInfo(Dictionary<string, object> initDict)
    {
        float firstDelayValue = 0f;
        float roFValue = 0f;
        float durationValue = 0f;

        int intId = 0;
        int intPosition = 0;
        //int intAvailLevel = 0;
        int intCountToBuy = 0;
        int intMaxInBattle = 0;

        Dictionary<string, object> priceDict = null;

        if (!initDict.Extract("id", ref intId) ||
            !initDict.Extract("name", ref name) ||
            !initDict.Extract("position", ref intPosition) ||
            //!initDict.Extract("availabilityLevel", ref intAvailLevel) ||
            !initDict.Extract("isHidden", ref isHidden) ||
            !initDict.Extract("isVip", ref isVip) ||
            !initDict.Extract("price", ref priceDict) ||
            !initDict.Extract("countToBuy", ref intCountToBuy) ||
            !initDict.Extract("maxInBattle", ref intMaxInBattle) ||
            !initDict.Extract("firstDelay", ref firstDelayValue) ||
            !initDict.Extract("reloadTime", ref roFValue) ||
            !initDict.Extract("duration", ref durationValue) ||
            !initDict.Extract("icon", ref icon) ||
            !initDict.Extract("target", ref target)
            )
        {
            Debug.LogError("Hangar supply parsing errors!!!");
        }

        id = intId;
        position = intPosition;
        //availabilityLevel = intAvailLevel;
        firstDelay = firstDelayValue;
        reloadTime = roFValue;
        duration = durationValue;
        countToBuy = intCountToBuy;
        maxInBattle = intMaxInBattle;

        price = SimplePrice.FromDictionary(priceDict);
        effects = LoadEffects(initDict.ExtractOrDefault<List<object>>("effects"));
        prefabName = initDict.ExtractOrDefault<string>("prefabName");
        targetType = initDict.ExtractOrDefault("target", ConsumableTargetType.None);

        if (!initDict.Extract("item", ref initDict))
            return;

        if (initDict.ExtractOrDefault<bool>("enabled"))
        {
            radius = initDict.ExtractOrDefault("radius", 0f);
            range = initDict.ExtractOrDefault("range", 0f);
            activationTime = initDict.ExtractOrDefault("activationTime", 0f);
            activationRadius = initDict.ExtractOrDefault("activationRadius", 0f);
            powerContext = initDict.ExtractOrDefault("context", DamageCalcContext.None);
            powerParameter = initDict.ExtractOrDefault("parameter", VehicleEffect.ParameterType.None);
            powerModifier = initDict.ExtractOrDefault("modifier", VehicleEffect.ModifierType.Sum);
            powerValue = initDict.ExtractOrDefault("value", 0f);
        }
    }

    private List<VehicleEffectData> LoadEffects(List<object> effectsList)
    {
        if (effectsList == null)
            return null;

        List<VehicleEffectData> output = new List<VehicleEffectData>(effectsList.Count);

        foreach (Dictionary<string, object> dict in effectsList)
        {
            VehicleEffect.ParameterType paramType = dict.ExtractOrDefault("parameter", VehicleEffect.ParameterType.None);
            VehicleEffect.ModifierType modType = dict.ExtractOrDefault("modifier", VehicleEffect.ModifierType.Sum);
            float modValue = dict.ExtractOrDefault("value", 0f);
            output.Add(new VehicleEffectData(paramType, modType, modValue, duration, -1));
        }

        return output;
    }

    public string LocalizedDescription
    {
        get
        {
            StringBuilder bonusString = new StringBuilder(64);
            for (int i = 0; i < effects.Count; i++)
            {
                bonusString.Append(bonusString.Length > 0 ? "\n" : "").Append(effects[i].BonusString);
            }

            string key = "ConsumableName_" + id;
            string bonusStr = bonusString.ToString();

            return Localizer.ContainsKey(key) ? Localizer.GetText(key, bonusStr) : bonusStr;
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
