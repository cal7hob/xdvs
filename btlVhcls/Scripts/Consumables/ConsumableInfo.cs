using System;
using System.CodeDom;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using CodeStage.AntiCheat.ObscuredTypes;

public enum SuperWeaponType
{
    None,
    MachineGun,
    AGS,
    ATGW
}

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
        if (dict == null)
            return  new SimplePrice(0, ProfileInfo.PriceCurrency.Silver);
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
    public readonly bool isSuperWeapon;
    public readonly string name;
    public readonly SimplePrice price;
    public readonly ObscuredInt countToBuy;
    public readonly ObscuredInt maxInBattle;
    public readonly ObscuredFloat firstDelay;
    public readonly ObscuredFloat reloadTime;
    public readonly ObscuredFloat duration;
    public readonly ObscuredInt lifetime;
    public readonly ConsumableTargetType targetType = ConsumableTargetType.None;
    public readonly string icon;

    public readonly List<VehicleEffect> effects;
    public readonly string prefabName;

    public readonly ObscuredFloat radius;
    public readonly ObscuredFloat range;
    public readonly ObscuredFloat activationTime;
    public readonly ObscuredFloat activationRadius;
    public readonly DamageCalcContext powerContext = DamageCalcContext.None;
    public readonly VehicleEffect.ModifierType powerModifier;
    public readonly VehicleEffect.ParameterType powerParameter;
    public readonly ObscuredFloat powerValue;

    private const string ATGW_PREFAB_NAME = "Missile";
    private const string AGS_PREFAB_NAME = "GrenadeLauncher";
    private const string MACHINEGUN_PREFAB_NAME = "MachineGun";

    public string IconWithFrame { get { return string.Format("{0}{1}", icon, GameData.CONSUMABLES_SPRITE_FRAMED_VERSION_SUFFIX); } }
    public string GetIcon(bool withFrame) { return withFrame ? IconWithFrame : icon; }
    public int Count { get { return (ProfileInfo.HaveConsumable(id)) ? (int)ProfileInfo.consumableInventory[id].count : 0; } }
    public bool IsAlive { get { return Count > 0 && (ProfileInfo.consumableInventory[id].deathTime - GameData.CurrentTimeStamp) > 0; } }

    public SuperWeaponType SuperWeaponType
    {
        get
        {
            switch (prefabName)
            {
                case ATGW_PREFAB_NAME:
                    return SuperWeaponType.ATGW;
                case AGS_PREFAB_NAME:
                    return SuperWeaponType.AGS;
                case MACHINEGUN_PREFAB_NAME:
                    return SuperWeaponType.MachineGun;
                default:
                    return SuperWeaponType.None;
            }
        }
    }

    public ConsumableInfo(Dictionary<string, object> initDict)
    {
        float firstDelayValue = 0f;
        float reloadTimeValue = 0f;
        float durationValue = 0f;

        int intId = 0;
        int intPosition = 0;
        //int intAvailLevel = 0;
        int intCountToBuy = 0;
        int intLifeTime = 0;
        int intMaxInBattle = 0;

        Dictionary<string, object> priceDict = null;

        bool allDataReceived = true;
        allDataReceived &= initDict.Extract("id", ref intId);
        allDataReceived &= initDict.Extract("position", ref intPosition);
        allDataReceived &= initDict.Extract("name", ref name);
        allDataReceived &= initDict.Extract("isHidden", ref isHidden);
        allDataReceived &= initDict.Extract("isVip", ref isVip);
        allDataReceived &= initDict.Extract("price", ref priceDict);
        allDataReceived &= initDict.Extract("countToBuy", ref intCountToBuy);
        allDataReceived &= initDict.Extract("maxInBattle", ref intMaxInBattle);
        allDataReceived &= initDict.Extract("firstDelay", ref firstDelayValue);
        allDataReceived &= initDict.Extract("reloadTime", ref reloadTimeValue);
        allDataReceived &= initDict.Extract("duration", ref durationValue);
        allDataReceived &= initDict.Extract("icon", ref icon);
        allDataReceived &= initDict.Extract("target", ref target);
        allDataReceived &= initDict.Extract("lifetime", ref intLifeTime);
        allDataReceived &= initDict.Extract("isSuperWeapon", ref isSuperWeapon);

        if (!allDataReceived)
            Debug.LogErrorFormat("Hangar consumable parsing errors!!! consumable id = {0}", intId);

        id = intId;
        position = intPosition;
        //availabilityLevel = intAvailLevel;
        firstDelay = firstDelayValue;
        reloadTime = reloadTimeValue;
        duration = durationValue;
        countToBuy = intCountToBuy;
        lifetime = intLifeTime;
        maxInBattle = intMaxInBattle;

        if (priceDict != null)
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
            powerParameter = VehicleEffect.ConvertToParameterType(initDict.ExtractOrDefault("parameter", VehicleEffect.PowerParameterType.None));
            powerModifier = initDict.ExtractOrDefault("modifier", VehicleEffect.ModifierType.Sum);
            powerValue = initDict.ExtractOrDefault("value", 0f);
        }
    }
    private List<VehicleEffect> LoadEffects(List<object> effectsList)
    {
        if (effectsList == null)
            return null;

        List<VehicleEffect> output = new List<VehicleEffect>(effectsList.Count);
        foreach (Dictionary<string, object> effectDict in effectsList)
        {
            VehicleEffect newEffect = new VehicleEffect(effectDict, BonusItem.BonusType.Consumable, id);
            newEffect.Duration = duration;
            output.Add(newEffect);
        }

        return output;
    }

    public static bool IsVip(int consumableId)
    {
        ConsumableInfo info;

        if (!GameData.consumableInfos.TryGetValue(consumableId, out info))
            return false;

        return info.isVip;
    }

    public static ConsumableInfo GetInfo(int consumableId)
    {
        if (GameData.consumableInfos == null)
            return null;

        ConsumableInfo info;

        GameData.consumableInfos.TryGetValue(consumableId, out info);

        return info;
    }

    public string LocalizedDescription
    {
        get
        {
            string bonusString = "";
            for (int i = 0; i < effects.Count; i++)
                bonusString += (bonusString.Length > 0 ? "\n" : "") + effects[i].BonusString;

            return Localizer.ContainsKey("ConsumableName_" + id) ? Localizer.GetText("ConsumableName_" + id, bonusString) : name;
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
