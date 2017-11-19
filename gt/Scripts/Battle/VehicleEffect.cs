using System;
using System.Collections.Generic;
using UnityEngine;
using CodeStage.AntiCheat.ObscuredTypes;
using XDevs.LiteralKeys;
using Hashtable = ExitGames.Client.Photon.Hashtable;

[Serializable]
public class VehicleEffect : IComparable<VehicleEffect>
{
    public enum ModifierType
    {
        Sum,
        Product,
        Exact
    }

    [Serializable]
    public enum ParameterType
    {
        None = 0,
        Armor,
        MaxArmor,
        Attack,
        Speed,
        RoF,
        Regeneration,
        RocketAttack,
        IRCMRoF,
        TakenDamageRatio,
        Shield
    }

    [SerializeField]
    private ObscuredFloat duration;

    [SerializeField]
    private BonusItem.BonusType source;

    [SerializeField]
    private IECell.IEIcon icon;

    [SerializeField]
    private ObscuredFloat modifierValue;

    [SerializeField]
    private ModifierType modType;

    [SerializeField]
    private ParameterType type;

    private readonly int consumableId;

    private int id;
    private string iconName;
    private double startTime;
    private double endTime;
    private ObscuredFloat storedValue = 0f;
    private static int currentId = 0;

    public VehicleEffect(int id, ParameterType efType, ModifierType modType, float modValue, float _duration, double _startTime, BonusItem.BonusType bonusType, IECell.IEIcon _icon, int consumableId)
    {
        this.id = id > 0 ? id : GetNewId();
        type = efType;
        this.modType = modType;
        modifierValue = modValue;
        duration = _duration;
        startTime = _startTime;
        endTime = startTime + duration;
        source = bonusType;
        icon = _icon;
        this.consumableId = consumableId;

        if (consumableId >= 0)
        {
            ConsumableInfo consInfo;
            if (GameData.consumableInfos.TryGetValue(consumableId, out consInfo))
            {
                iconName = consInfo.icon;
            }
        }
    }

    public VehicleEffect(Dictionary<string, object> dict, BonusItem.BonusType source, int consumableId)
    {
        type = dict.ExtractOrDefault("parameter", ParameterType.None);
        modType = dict.ExtractOrDefault("modifier", ModifierType.Sum);
        modifierValue = dict.ExtractOrDefault("value", 0f);
        duration = dict.ExtractOrDefault("duration", 10f);
        this.consumableId = consumableId;
        if (consumableId >= 0)
        {
            ConsumableInfo consInfo;
            if (GameData.consumableInfos.TryGetValue(consumableId, out consInfo))
            {
                iconName = consInfo.icon;
            }
        }
        this.source = source;
    }

    public int Id
    {
        get { return id; }
    }

    public void SetId(int newId)
    {
        id = newId;
    }

    /// <summary>
    /// Ключ для использования в различных UI приблудах. Для расходки зависит ещё и от consumableId
    /// </summary>
    public int UI_Id
    {
        get
        {
            return source == BonusItem.BonusType.Consumable ? 1000 + consumableId : (int)type; // Для расходки ещё и добавить consumableId
        }
    }

    public ParameterType Type
    {
        get { return type; }
    }
   // Modifier modifierMode = Default;
    public ModifierType ModType
    {
        get { return modType; }
    }

    public BonusItem.BonusType Source
    {
        get { return source; }
    }

    public IECell.IEIcon Icon
    {
        get { return icon; }
    }

    public string IconName
    {
        get
        {
            if (string.IsNullOrEmpty(iconName))
            {
                return icon.ToString();
            }

            return iconName;
        }
    }
    public string BonusString
    {
        get
        {
            string bonusLocKey = type.ToString();
            switch (type)
            {
                case ParameterType.Armor:
                    bonusLocKey = "BaseArmor";
                    break;
                case ParameterType.Attack:
                    bonusLocKey = "BaseAttack";
                    break;
                case ParameterType.Speed:
                    bonusLocKey = "BaseSpeed";
                    break;
                case ParameterType.RoF:
                    bonusLocKey = "BaseROF";
                    break;
            }

            if (!Localizer.ContainsKey(bonusLocKey))
                bonusLocKey = type.ToString();

            string sign;
            float val;
            string postfix;
            GetValueForBonusString(out sign, out val, out postfix);

            return string.Format("{0}: {1}{2}{3}", Localizer.ContainsKey(bonusLocKey) ? Localizer.GetText(bonusLocKey) : bonusLocKey, sign, val, postfix);
        }
    }
    private string GetSignString(float val)
    {
        return val < 0 ? "-" : "+";
    }
    private void GetValueForBonusString(out string sign, out float val, out string postfix)
    {
        sign = "";
        val = 0;
        postfix = "%";
        switch (type)
        {
            case ParameterType.Armor:
                sign = GetSignString(modifierValue);
                val = Mathf.CeilToInt(modifierValue * 100);
                break;
            case ParameterType.Speed:
            case ParameterType.RoF:
            case ParameterType.TakenDamageRatio:
            case ParameterType.Attack:
                val = modifierValue - 1f;
                sign = GetSignString(val);
                val = Mathf.CeilToInt(Mathf.Abs(val) * 100);
                break;

                //case ParameterType.Shield:
                //    value = vehicle.Shield = (int)GetAffectedValue(vehicle.Shield, invertedEffect);
                //    break;

                //case ParameterType.IRCMRoF:
                //    value = vehicle.IRCMROF = GetAffectedValue(vehicle.IRCMROF, invertedEffect);
                //    break;
                //case ParameterType.MaxArmor:
                //    value = vehicle.MaxArmor = (int)GetAffectedValue(vehicle.MaxArmor, invertedEffect);
                //    break;
                //case ParameterType.Regeneration:
                //    value = vehicle.Regeneration = (int)GetAffectedValue(vehicle.Regeneration, invertedEffect);
                //    break;
        }
    }

    public float ModValue
    {
        get { return modifierValue; }
    }

    public double StartTime
    {
        get { return startTime; }
        set
        {
            startTime = value;
            endTime = startTime + duration;
        }
    }

    public double EndTime
    {
        get { return endTime; }
    }

    public float Duration
    {
        set
        {
            duration = value;
            endTime = startTime + duration;
        }
        get { return duration; }
    }

    public int ConsumableId
    {
        get { return consumableId; }
    }

    public float Remain
    {
        get { return Mathf.Clamp((float)(endTime - PhotonNetwork.time), 0, float.MaxValue); }
    }

    public bool MustBeReturned { get { return duration > 0; } }

    public static void Init()
    {
        currentId = 0;
    }

    public static int GetNewId()
    {
        return ++currentId;
    }

    int IComparable<VehicleEffect>.CompareTo(VehicleEffect other)
    {
        return endTime.CompareTo(other.endTime);
    }

    public override string ToString()
    {
        return string.Format(
            "Effect: type:{0}, modT={1}, modVal={2}, dur={3}, endTime={4}, Source={5}, ConsumableId={6}",
            type,
            modType,
            modifierValue,
            duration,
            endTime,
            source,
            consumableId);
    }

    public void SetSource(BonusItem.BonusType _fromBonus)
    {
        source = _fromBonus;
    }

    public void SetEndTime(double time)
    {
        endTime = time;
    }

    public float GetAffectedValue(float value, bool invertedEffect)
    {
        switch (ModType)
        {
            case ModifierType.Product:
                return invertedEffect ? value / ModValue : value * ModValue;
            case ModifierType.Sum:
                return invertedEffect ? value - ModValue : value + ModValue;
            case ModifierType.Exact:
                if (invertedEffect)
                {
                    return storedValue;
                }
                storedValue = value;
                return ModValue;
        }

        return ModValue;
    }

    public void ApplyToVehicle(VehicleController vehicle, bool invertedEffect)
    {
        object value = null;
        StatisticKey propertyKey = StatisticKey.Health;
        switch (type)
        {
            // Для Armor особый случай - при умножении использовать для расчёта MaxArmor
            case ParameterType.Armor:
                if (ModType == ModifierType.Product)
                {
                    value = vehicle.Armor = vehicle.Armor + (int)GetAffectedValue(vehicle.MaxArmor, invertedEffect);
                }
                else
                {
                    value = vehicle.Armor = (int)GetAffectedValue(vehicle.Armor, invertedEffect);
                }
				propertyKey = StatisticKey.Health;
                break;
            case ParameterType.Attack:
                value = vehicle.Attack = (int)GetAffectedValue(vehicle.Attack, invertedEffect);
                propertyKey = StatisticKey.Attack;
                break;
            case ParameterType.IRCMRoF:
                value = vehicle.IRCMROF = GetAffectedValue(vehicle.IRCMROF, invertedEffect);
                return;
                break;
            case ParameterType.MaxArmor:
                value = vehicle.MaxArmor = (int)GetAffectedValue(vehicle.MaxArmor, invertedEffect);
                propertyKey = StatisticKey.MaxArmor;
                break;
            case ParameterType.Regeneration:
                value = vehicle.Regeneration = (int)GetAffectedValue(vehicle.Regeneration, invertedEffect);
                propertyKey = StatisticKey.Regen;
                break;
            case ParameterType.RoF:
                value = vehicle.ROF = GetAffectedValue(vehicle.ROF, invertedEffect);
                propertyKey = StatisticKey.RoF;
				
                break;
            case ParameterType.Speed:
                value = vehicle.Speed = GetAffectedValue(vehicle.Speed, invertedEffect);
                propertyKey = StatisticKey.Speed;
                break;
            case ParameterType.TakenDamageRatio:
                value = vehicle.TakenDamageRatio = GetAffectedValue(vehicle.TakenDamageRatio, invertedEffect);
                propertyKey = StatisticKey.DamageRatio;
                break;
            case ParameterType.Shield:
                value = vehicle.Shield = (int)GetAffectedValue(vehicle.Shield, invertedEffect);
                propertyKey = StatisticKey.Shield;
                break;
        }

        if (vehicle.IsMine && value != null)
        {
            vehicle.SetCustomProperties(propertyKey, value);
        }
    }
}