using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using CodeStage.AntiCheat.ObscuredTypes;
using ExitGames.Client.Photon;
using UnityEngine;

using ModifierType = VehicleEffect.ModifierType;
using ParameterType = VehicleEffect.ParameterType;

[Serializable]
public struct VehicleEffectData
{
    private const int DATA_SIZE = 18;

    private static readonly byte[] memVehEffectData = new byte[DATA_SIZE];
    public static short Serialize(StreamBuffer outStream, object customObject)
    {
        VehicleEffectData data = (VehicleEffectData)customObject;

        lock (memVehEffectData)
        {
            byte[] bytes = memVehEffectData;
            int index = 0;

            bytes[index++] = (byte)data.paramType;
            bytes[index++] = (byte)data.modType;

            Protocol.Serialize(data.modValue, bytes, ref index);
            Protocol.Serialize(data.duration, bytes, ref index);

            byte[] endTimeBytes = BitConverter.GetBytes(data.endTime);
            endTimeBytes.CopyTo(bytes, index);

            outStream.Write(bytes, 0, DATA_SIZE);
        }

        return DATA_SIZE;
    }

    public static object Deserialize(StreamBuffer inStream, short length)
    {
        ParameterType pType;
        ModifierType mType;
        float mValue;
        float duration;
        double endTime;
        
        lock (memVehEffectData)
        {
            inStream.Read(memVehEffectData, 0, DATA_SIZE);
            pType = (ParameterType)memVehEffectData[0];
            mType = (ModifierType)memVehEffectData[1];

            int index = 2;
            Protocol.Deserialize(out mValue, memVehEffectData, ref index);
            Protocol.Deserialize(out duration, memVehEffectData, ref index);
            endTime = BitConverter.ToDouble(memVehEffectData, index);
        }

        return new VehicleEffectData(pType, mType, mValue, duration, endTime);
    }

    public VehicleEffectData(ParameterType parameterType, ModifierType modifierType, float modifierValue, float duration, double endTime = -1)
    {
        paramType = parameterType;
        modType = modifierType;
        modValue = modifierValue;
        this.duration = duration;
        this.endTime = endTime;
    }

    public ParameterType ParameterType
    {
        get { return paramType; }
    }
    public ModifierType ModifierType
    {
        get { return modType;}
    }
    public float ModifierValue
    {
        get { return modValue; }
    }
    public float Duration
    {
        get { return duration; }
    }
    [HideInInspector] public double endTime;

    public string BonusString
    {
        get
        {
            string bonusLocKey = paramType.ToString();
            switch (paramType)
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
                bonusLocKey = paramType.ToString();

            string sign;
            float val;
            string postfix;
            GetValueForBonusString(out sign, out val, out postfix);

            return string.Format("{0}: {1}{2}{3}", Localizer.ContainsKey(bonusLocKey) ? Localizer.GetText(bonusLocKey) : bonusLocKey, sign, val, postfix);
        }
    }

    public bool IsPositive()
    {
        switch (paramType)
        {
            case ParameterType.Armor:
            case ParameterType.Attack:
            case ParameterType.MaxArmor:
            case ParameterType.RoF:
            case ParameterType.Speed:
            case ParameterType.Regeneration:
                return (modType == ModifierType.Product && modValue >= 1) ||
                       (modType == ModifierType.Sum && modValue > 0);
            case ParameterType.TakenDamageRatio:
                return modValue < 1;
            case ParameterType.Blindness:
            case ParameterType.Stun:
                return false;
            case ParameterType.None:
                Debug.LogError("No parameter specified to get if it is positive or not");
                return false;
            default:
                return false;
        }
    }

    private void GetValueForBonusString(out string sign, out float val, out string postfix)
    {
        sign = "";
        val = 0;
        postfix = "%";
        switch (paramType)
        {
            case ParameterType.Armor:
                sign = HelpTools.GetSignString(modValue);
                val = Mathf.RoundToInt(modValue * 100);
                break;
            case ParameterType.Speed:
            case ParameterType.RoF:
            case ParameterType.TakenDamageRatio:
            case ParameterType.Attack:
                val = modValue - 1f;
                sign = HelpTools.GetSignString(val);
                val = Mathf.RoundToInt(Mathf.Abs(val) * 100);
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

    [SerializeField] private ParameterType paramType;
    [SerializeField] private ModifierType modType;
    [SerializeField] private ObscuredFloat modValue;
    [SerializeField] private ObscuredFloat duration;
}
