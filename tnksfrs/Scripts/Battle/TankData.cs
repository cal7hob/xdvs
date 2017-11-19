using System;
using System.Text;
using CodeStage.AntiCheat.ObscuredTypes;
using ExitGames.Client.Photon;
using UnityEngine;
using System.Collections.Generic;
using Disconnect;
using XD;

[Serializable]
public class TankData : IUnitData
{
    /// <summary> Id игрока от Photon'а внутри комнаты </summary>
	public int              playerId = 0;
	public string           playerName = "";
	public int              playerLevel = 0;
	public bool             newbie = false;
	public SocialPlatform   socialPlatform = new SocialPlatform();
	public string           socialUID = "";
	public string           country = "";
	public int              patternId = 0;
    public int              decalId = 0;
	public ObscuredInt      maxArmor;
	public ObscuredInt      armor;
	public ObscuredInt      attack;
    public ObscuredInt      unitId;
    public ObscuredFloat    critChance = 0;
    public ObscuredFloat    critFactor = 0;
	public float            movingSpeed;
    public float            turretSpeed;
	public float            rof;
    public float            ircmRof;
	public int              teamId;
	public bool             hideMyFlag;
    /// <summary>
    /// Id игрока на сервере
    /// </summary>
	public int              innerId;
	public bool             vip;
	public string           clanName;

    private float           damageAbsorption = 0;
    private float           damageAbsorptionProbability = 0;

    private IUnitBattle     unitBattle = null;

    public int PlayerLevel
    {
        get
        {
            return playerLevel;
        }
    }

    public float DamageAbsorption
    {
        get
        {
            return unitBattle != null ? unitBattle.Settings[Setting.DamageAbsorption].Current : damageAbsorption;
        }

        set
        {
            if (unitBattle != null)
            {
                unitBattle.Settings[Setting.DamageAbsorption] = new Clamper(value);
            }
            else
            {
                damageAbsorption = value;
            }
        }
    }
    
    public float DamageAbsorptionProbability
    {
        get
        {
            return unitBattle != null ? unitBattle.Settings[Setting.DamageAbsorptionProbability].Current : damageAbsorptionProbability;
        }

        set
        {
            if (unitBattle != null)
            {
                unitBattle.Settings[Setting.DamageAbsorptionProbability] = new Clamper(value);
            }
            else
            {
                damageAbsorptionProbability = value;
            }
        }
    }

    public IUnitBattle UnitBattle
    {
        get
        {
            return unitBattle;
        }
    }

    public int Team
    {
        get
        {
            return teamId;
        }
    }

    public int ID
    {
        get
        {
            return playerId;
        }
    }

    public string Nick
    {
        get
        {
            return playerName;
        }
    }
    
	public TankData() { }

	public TankData(
        string                                                  playerName,
        int                                                     playerLevel,
        string                                                  country,
        bool                                                    newbie,
        SocialPlatform                                          socialPlatform,
        string                                                  socialUID,
        XD.Settings                                             parameters,
        int                                                     patternId,
        int                                                     decalId,
        int                                                     teamId,
        bool                                                    hideMyFlag,
        int                                                     innerId,
        bool                                                    vip,
        string                                                  clanName,
        IUnitBattle                                             unitBattle
)
	{
	    this.playerName = playerName;
		this.playerLevel = playerLevel;
		this.country = country;
		this.newbie = newbie;
		this.socialPlatform = socialPlatform;
		this.socialUID = socialUID;
        
		maxArmor = (int)parameters[Setting.HP].Max;
		armor = maxArmor;
        attack = (int)parameters[Setting.Damage].Max;
        unitId = unitBattle.ID;
        movingSpeed = parameters[Setting.MovingSpeed].Max;
        turretSpeed = parameters[Setting.TurretSpeed].Max;
        rof = parameters[Setting.RPM].Max;
        ircmRof = parameters[Setting.RPM].Max;
		this.patternId = patternId;
	    this.decalId = decalId;
		this.teamId = teamId;
		this.hideMyFlag = hideMyFlag;
		this.innerId = innerId;
		this.vip = vip;
		this.clanName = clanName;
	    this.unitBattle = unitBattle;//TEMP?

        //Debug.LogError("New TankData: " + playerName + ", maxArmor: " + maxArmor + ", armor: " + armor);
	}

	public static byte[] Serialize(object customObject)
	{
		int index = 0;

		TankData data = (TankData)customObject;

		byte[] nameBytes = Encoding.UTF8.GetBytes(data.playerName);
		byte[] clanBytes = Encoding.UTF8.GetBytes(data.clanName);
		byte[] countryBytes = Encoding.UTF8.GetBytes(data.country);
		byte[] socialUIDBytes = Encoding.UTF8.GetBytes(data.socialUID);

		byte[] bytes
            = new byte
                [4 + 4 + 4 + 4
                + nameBytes.Length
                + 4
                + countryBytes.Length
                + 9 /* – инкрементить, если добавляем новые боевые статы */ * 4
                + 4 + 4 + 4
                + socialUIDBytes.Length
                + 4 + 4 + 4 + 4 + 4 + 4
                + clanBytes.Length];

		Protocol.Serialize(data.playerId, bytes, ref index);
		Protocol.Serialize(data.playerLevel, bytes, ref index);
		Protocol.Serialize(nameBytes.Length, bytes, ref index);

		nameBytes.CopyTo(bytes, index);

		index += nameBytes.Length;
		Protocol.Serialize(countryBytes.Length, bytes, ref index);

		countryBytes.CopyTo(bytes, index);

		index += countryBytes.Length;

		Protocol.Serialize(data.maxArmor, bytes, ref index);
		Protocol.Serialize(data.armor, bytes, ref index);
		Protocol.Serialize(data.attack, bytes, ref index);
        Protocol.Serialize(data.unitId, bytes, ref index);
        Protocol.Serialize(data.movingSpeed, bytes, ref index);
		Protocol.Serialize(data.rof, bytes, ref index);
        Protocol.Serialize(data.ircmRof, bytes, ref index);
        Protocol.Serialize(data.DamageAbsorption, bytes, ref index);
        Protocol.Serialize(data.DamageAbsorptionProbability, bytes, ref index);

		Protocol.Serialize(data.newbie ? 1 : 0, bytes, ref index);
		Protocol.Serialize((int)data.socialPlatform, bytes, ref index);
		Protocol.Serialize(socialUIDBytes.Length, bytes, ref index);
        socialUIDBytes.CopyTo(bytes, index);
        index += socialUIDBytes.Length;

        Protocol.Serialize(data.patternId, bytes, ref index);
		Protocol.Serialize(data.teamId, bytes, ref index);
        Protocol.Serialize(data.decalId, bytes, ref index);
		Protocol.Serialize(data.hideMyFlag ? 1 : 0, bytes, ref index);
		Protocol.Serialize(data.innerId, bytes, ref index);
		Protocol.Serialize(data.vip ? 1 : 0, bytes, ref index);
		Protocol.Serialize(clanBytes.Length, bytes, ref index);

		clanBytes.CopyTo(bytes, index);
        index += clanBytes.Length;
        
		return bytes;
	}

	public static TankData Deserialize(byte[] bytes)
	{
		int index = 0;

		TankData data = new TankData();

		Protocol.Deserialize(out data.playerId, bytes, ref index);
		Protocol.Deserialize(out data.playerLevel, bytes, ref index);

		int length, temp;

		Protocol.Deserialize(out length, bytes, ref index);

		byte[] stringBytes = new byte[length];

		Array.Copy(bytes, index, stringBytes, 0, length);

		index += length;

        data.playerName = Encoding.UTF8.GetString (stringBytes, 0, stringBytes.Length);

		Protocol.Deserialize(out length, bytes, ref index);

		stringBytes = new byte[length];

		Array.Copy(bytes, index, stringBytes, 0, length);

		index += length;

        data.country = Encoding.UTF8.GetString (stringBytes, 0, stringBytes.Length);

		int maxArmor, armor, attack, unitid, platform;

		Protocol.Deserialize(out maxArmor, bytes, ref index);
		Protocol.Deserialize(out armor, bytes, ref index);
		Protocol.Deserialize(out attack, bytes, ref index);
        Protocol.Deserialize(out unitid, bytes, ref index);

		data.maxArmor = maxArmor;
		data.armor = armor;
		data.attack = attack;
        data.unitId = unitid;

        Protocol.Deserialize(out data.movingSpeed, bytes, ref index);
		Protocol.Deserialize(out data.rof, bytes, ref index);
        Protocol.Deserialize(out data.ircmRof, bytes, ref index);
        Protocol.Deserialize(out data.damageAbsorption, bytes, ref index);
        Protocol.Deserialize(out data.damageAbsorptionProbability, bytes, ref index);

		Protocol.Deserialize(out temp, bytes, ref index);

		data.newbie = temp == 1;

		Protocol.Deserialize(out platform, bytes, ref index);

		data.socialPlatform = (SocialPlatform)platform;

		Protocol.Deserialize(out length, bytes, ref index);

		stringBytes = new byte[length];

		Array.Copy(bytes, index, stringBytes, 0, length);

        data.socialUID = Encoding.UTF8.GetString (stringBytes, 0, stringBytes.Length);

		index += length;

		Protocol.Deserialize(out data.patternId, bytes, ref index);
		Protocol.Deserialize(out data.teamId, bytes, ref index);
        Protocol.Deserialize(out data.decalId, bytes, ref index);
		Protocol.Deserialize(out temp, bytes, ref index);

	    data.hideMyFlag = temp == 1;

		Protocol.Deserialize(out data.innerId, bytes, ref index);
		Protocol.Deserialize(out temp, bytes, ref index);

		data.vip = temp == 1;

		Protocol.Deserialize(out length, bytes, ref index);

		if (length > 0)
		{
			stringBytes = new byte[length];
			Array.Copy(bytes, index, stringBytes, 0, length);
			index += length;
			data.clanName = Encoding.UTF8.GetString(stringBytes, 0, stringBytes.Length);
		}

		return data;
	}
}
