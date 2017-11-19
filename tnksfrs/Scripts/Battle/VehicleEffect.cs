using System; 
using UnityEngine;
using CodeStage.AntiCheat.ObscuredTypes;
using XD;
using Hashtable = ExitGames.Client.Photon.Hashtable;

[Serializable]
public class VehicleEffect : IComparable<VehicleEffect>
{
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
        IRCMRoF
	}

    [SerializeField]
    private ObscuredFloat duration;

    [SerializeField]
    private BonusItem.BonusType fromBonus;

    [SerializeField]
    private IECell.IEIcon icon;

    [SerializeField]
    private ObscuredFloat modifierValue;

    [SerializeField]
    private ModifierType modType;

    [SerializeField]
    private ParameterType type;

    private int id;
    private double startTime;
    private double endTime;

    public VehicleEffect(
        int                 _id,
        ParameterType       efType,
        ModifierType        modType,
        float               modValue,
        float               _duration,
        double              _startTime,
        BonusItem.BonusType bonusType,
        IECell.IEIcon       _icon)
    {
        id = _id;
        type = efType;
        this.modType = modType;
        modifierValue = modValue;
        duration = _duration;
        startTime = _startTime;
        endTime = startTime + duration;
        fromBonus = bonusType;
        icon = _icon;
    }

    public int Id
    {
        get { return id; }
    }

    public ParameterType Type
    {
        get { return type; }
    }

    public ModifierType ModType
    {
        get { return modType; }
    }

    public XD.VehicleParameter RelatedVehicleParameter
    {
        get
        {
            switch (type)
            {
                case ParameterType.MaxArmor:
                    return XD.VehicleParameter.Armor;
                case ParameterType.Attack:
                    return XD.VehicleParameter.Damage;
                case ParameterType.RocketAttack:
                    return XD.VehicleParameter.RocketDamage;
                case ParameterType.RoF:
                    return XD.VehicleParameter.RoF;
                case ParameterType.IRCMRoF:
                    return XD.VehicleParameter.IRCMRoF;
                case ParameterType.Speed:
                    return XD.VehicleParameter.Speed;
            }

            Debug.LogWarningFormat(
                "Trying to pick up VehicleParameter "
                    + "which is not related to VehicleEffect parameter type \"{0}\"!",
                    type);

            return XD.VehicleParameter.None;
        }
    }

    public BonusItem.BonusType FromBonus
    {
        get { return fromBonus; }
    }

    public IECell.IEIcon Icon
    {
        get { return icon; }
    }

    public float ModValue
    {
        get { return modifierValue; }
    }

    public double StartTime
    {
        get { return startTime; }
    }

    public double EndTime
    {
        get { return endTime; }
    }

    public float Duration
    {
        get { return duration; }
    }

    public float Remain
    {
        get { return Mathf.Clamp((float)(endTime - PhotonNetwork.time), 0, float.MaxValue); }
    }

    public static int GetNewId()
	{
		int id = (int)PhotonNetwork.room.CustomProperties["eid"];

		PhotonNetwork.room.SetCustomProperties(new Hashtable{{"eid", id + 1}});

		return id;
	}

    int IComparable<VehicleEffect>.CompareTo(VehicleEffect other)
    {
        return endTime.CompareTo(other.endTime);
    }

    public override string ToString()
    {
        return string.Format(
            "Effect: type:{0}, modT={1}, modVal={2}, dur={3}, endTime={4}, fromBonus={5}",
            type,
            modType,
            modifierValue,
            duration,
            endTime,
            fromBonus);
    }

    public void SetBonus(BonusItem.BonusType _fromBonus)
    {
        fromBonus = _fromBonus;
    }

    public void SetEndTime(double time)
    {
        endTime = time;
    }

    public void SetId(int newId)
	{
		id = newId;
	}
}