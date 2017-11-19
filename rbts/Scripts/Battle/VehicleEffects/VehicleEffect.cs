using System;
using System.Collections.Generic;
using UnityEngine;
using CodeStage.AntiCheat.ObscuredTypes;

using Hashtable = ExitGames.Client.Photon.Hashtable;

[Serializable]
public class VehicleEffect : IComparable<VehicleEffect>
{
    public enum ModifierType
    {
        Sum,
        Product,
        Fixed
    }

    [Serializable]
	public enum ParameterType
	{
		None = 0,

        // Parameters change
		Armor,
		MaxArmor,
		Attack,
		Speed,
		RoF,
		Regeneration,
        RocketAttack,
        TakenDamageRatio,

        // Influences
        Stun,
        Blindness
    }

    public readonly string effectPropertyKey;
    private VehicleEffectData effectData;
    private readonly int consumableId;
    private double endTime;
    private ObscuredFloat storedValue = 0f;
    private readonly VehicleController owner;

    /// <summary>
    /// Creates and immediately applies effect to owner vehicle
    /// </summary>
    public VehicleEffect(VehicleEffectData effectData, VehicleController owner, string effectPropertyKey)
    {
        this.effectData = effectData;
        endTime = effectData.endTime > 0 ? effectData.endTime : PhotonNetwork.time + effectData.Duration;
        this.owner = owner;
        this.effectPropertyKey = effectPropertyKey;

        if (owner.PhotonView.isMine)
        {
            ChangeOwnerParams(false);
        }
    }

    public ParameterType ParamType
    {
        get { return effectData.ParameterType; }
    }

    public ModifierType ModType
    {
        get { return effectData.ModifierType; }
    }

    public float ModValue
    {
        get { return effectData.ModifierValue; }
    }

    public double EndTime
    {
        get { return endTime; }
    }

    public float Duration
    {
        get { return effectData.Duration; }
    }

    public float Remain
    {
        get { return Mathf.Clamp((float)(endTime - PhotonNetwork.time), 0, float.MaxValue); }
    }

    public bool IsPositive
    {
        get { return effectData.IsPositive(); }
    }

    public bool MustBeReturned { get { return effectData.Duration > 0; } }

    public override string ToString()
    {
        return string.Format(
            "Effect: parameter:{0}, modT={1}, modVal={2}, dur={3}, endTime={4}, effectPropertyKey = {5}, owner = {6}",
            ParamType,
            ModType,
            ModValue,
            Duration,
            endTime,
            effectPropertyKey,
            owner.name);
    }

    public float GetAffectedValue(float value, bool invertedEffect)
    {
        switch (ModType)
        {
            case ModifierType.Product:
                return invertedEffect ? value / ModValue : value * ModValue;
            case ModifierType.Sum:
                return invertedEffect ? value - ModValue : value + ModValue;
            case ModifierType.Fixed:
                if (invertedEffect)
                    return storedValue;

                storedValue = value;
                return ModValue;
        }

        return ModValue;
    }

    int IComparable<VehicleEffect>.CompareTo(VehicleEffect other)
    {
        return endTime.CompareTo(other.endTime);
    }

    public void ChangeData(VehicleEffectData newData)
    {
        if (owner.PhotonView.isMine)
        {
            GetProperties(true);
            effectData = newData;
            endTime = effectData.endTime > 0 ? effectData.endTime : PhotonNetwork.time + effectData.Duration;
            Hashtable properties = GetProperties(false);
            if (properties != null)
            {
                SetPlayerProperties(properties);
            }
        }
        else
        {
            effectData = newData;
            endTime = effectData.endTime > 0 ? effectData.endTime : PhotonNetwork.time + effectData.Duration;
        }
    }

    public void Cancel()
    {
        if (!owner.PhotonView.isMine)
            return;

        if (owner.PhotonView.isMine)
        {
            ChangeOwnerParams(true);
        }
    }


    private void ChangeOwnerParams(bool invertedEffect)
    {
        Hashtable properties = GetProperties(invertedEffect);
        if (properties != null)
        {
            SetPlayerProperties(properties);
        }
    }

    private Hashtable GetProperties(bool invertedEffect)
    {
        object value = null;
        string key = null;

        switch (ParamType)
        {
            // Для Armor особый случай - при умножении использовать для расчёта MaxArmor
            case ParameterType.Armor:
                if (ModType == ModifierType.Product)
                    value = owner.Armor = owner.Armor + Mathf.RoundToInt(GetAffectedValue(owner.MaxArmor, invertedEffect));
                else
                    value = owner.Armor = Mathf.RoundToInt(GetAffectedValue(owner.Armor, invertedEffect));
                key = owner.KeyForHealth;
                break;
            case ParameterType.Attack:
                value = owner.Attack = Mathf.RoundToInt(GetAffectedValue(owner.Attack, invertedEffect));
                key = owner.KeyForAttack;
                break;
            case ParameterType.MaxArmor:
                value = owner.MaxArmor = Mathf.RoundToInt(GetAffectedValue(owner.MaxArmor, invertedEffect));
                key = owner.KeyForMaxArmor;
                break;
            case ParameterType.Regeneration:
                value = owner.Regeneration = Mathf.RoundToInt(GetAffectedValue(owner.Regeneration, invertedEffect));
                key = owner.KeyForRegen;
                break;
            case ParameterType.RoF:
                value = owner.RoF = GetAffectedValue(owner.RoF, invertedEffect);
                key = owner.KeyForRoF;
                break;
            case ParameterType.Speed:
                value = owner.Speed = GetAffectedValue(owner.Speed, invertedEffect);
                key = owner.KeyForSpeed;
                break;
            case ParameterType.TakenDamageRatio:
                value = owner.TakenDamageRatio = GetAffectedValue(owner.TakenDamageRatio, invertedEffect);
                key = owner.KeyForDamageRatio;
                break;
            case ParameterType.Stun:
                owner.Stunned = !invertedEffect;
                break;
            case ParameterType.Blindness:
                owner.Blinded = !invertedEffect;
                break;
        }

        return key == null ? null : new Hashtable() { { key, value } };
    }

    private void SetPlayerProperties(Hashtable properties)
    {
        if (owner.IsBot)
        {
            PhotonNetwork.room.SetCustomProperties(properties);
        }
        else
        {
            PhotonNetwork.player.SetCustomProperties(properties);
        }
    }

    private void OwnershipErrorMsg()
    {
        Debug.LogError("Trying to do some work in another's effect (only owner can do that)");
    }
}