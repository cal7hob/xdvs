using System;
using System.Text;
using CodeStage.AntiCheat.ObscuredTypes;
using ExitGames.Client.Photon;
using UnityEngine;
using System.Collections.Generic;

[Serializable]
public class VehicleData
{
    /// <summary>
    /// Id игрока от Photon'а внутри комнаты
    /// </summary>
    public int playerId;
    public ObscuredInt playerLevel;
    public SocialPlatform socialPlatform;
    public ObscuredString playerName;
    public CachedObscuredString clanName;
    public CachedObscuredString country;
    public ObscuredString socialUID;
    public ObscuredInt patternId;
    public ObscuredInt decalId;
    public ObscuredInt maxArmor;
    public ObscuredInt armor;
    public ObscuredInt attack;
    public ObscuredFloat speed;
    public ObscuredFloat roF;
    public ObscuredInt magazine;
    public bool newbie;
    public int teamId;
    public bool hideMyFlag;
    /// <summary>
    /// Id игрока на сервере
    /// </summary>
    public int profileId;
    public bool vip;
    public ObscuredInt regeneration; // Лечение (ед./с)
    public ObscuredFloat takenDamageRatio; //Коэффициент к получаемому урону

    public VehicleData() { }

    public VehicleData(
        string                                                  playerName,
        int                                                     playerLevel,
        string                                                  country,
        bool                                                    newbie,
        SocialPlatform                                          socialPlatform,
        string                                                  socialUID,
        Dictionary<VehicleInfo.VehicleParameter, ObscuredFloat> hangarParameters,
        int                                                     patternId,
        int                                                     decalId,
        int                                                     teamId,
        bool                                                    hideMyFlag,
        int                                                     profileId,
        bool                                                    vip,
        string                                                  clanName,
        int                                                     regeneration,
        float                                                   takenDamageRatio)
    {
        this.playerName = playerName;
        this.playerLevel = playerLevel;
        this.country = country;
        this.newbie = newbie;
        this.socialPlatform = socialPlatform;
        this.socialUID = socialUID;
        maxArmor = Mathf.RoundToInt(hangarParameters[VehicleInfo.VehicleParameter.Armor]);
        armor = maxArmor;
        attack = Mathf.RoundToInt(hangarParameters[VehicleInfo.VehicleParameter.Damage]);
        magazine = (int)hangarParameters[VehicleInfo.VehicleParameter.Magazine];
        speed = hangarParameters[VehicleInfo.VehicleParameter.Speed];
        roF = hangarParameters[VehicleInfo.VehicleParameter.RoF];
        this.patternId = patternId;
        this.decalId = decalId;
        this.teamId = teamId;
        this.hideMyFlag = hideMyFlag;
        this.profileId = profileId;
        this.vip = vip;
        this.clanName = clanName;
        this.regeneration = regeneration;
        this.takenDamageRatio = takenDamageRatio;
    }

    public static byte[] Serialize(object customObject)
    {
        int index = 0;

        VehicleData data = (VehicleData)customObject;

        byte[] nameBytes = Encoding.UTF8.GetBytes(data.playerName);
        byte[] clanBytes = Encoding.UTF8.GetBytes(data.clanName);
        byte[] countryBytes = Encoding.UTF8.GetBytes(data.country);
        byte[] socialUIDBytes = Encoding.UTF8.GetBytes(data.socialUID);

        byte[] bytes
            = new byte
                [4 + 4
                + 4 + nameBytes.Length
                + 4 + countryBytes.Length
                + 10 * 4
                + 4 + socialUIDBytes.Length
                + 4 * 6
                + 4 + clanBytes.Length];

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
        Protocol.Serialize(data.speed, bytes, ref index);
        Protocol.Serialize(data.roF, bytes, ref index);
        Protocol.Serialize(data.magazine, bytes, ref index);
        Protocol.Serialize(data.newbie ? 1 : 0, bytes, ref index);
        Protocol.Serialize((int)data.socialPlatform, bytes, ref index);
        Protocol.Serialize(data.regeneration, bytes, ref index);
        Protocol.Serialize(data.takenDamageRatio, bytes, ref index);

        Protocol.Serialize(socialUIDBytes.Length, bytes, ref index);
        socialUIDBytes.CopyTo(bytes, index);
        index += socialUIDBytes.Length;

        Protocol.Serialize(data.patternId, bytes, ref index);
        Protocol.Serialize(data.teamId, bytes, ref index);
        Protocol.Serialize(data.decalId, bytes, ref index);
        Protocol.Serialize(data.hideMyFlag ? 1 : 0, bytes, ref index);
        Protocol.Serialize(data.profileId, bytes, ref index);
        Protocol.Serialize(data.vip ? 1 : 0, bytes, ref index);

        Protocol.Serialize(clanBytes.Length, bytes, ref index);

        clanBytes.CopyTo(bytes, index);
        index += clanBytes.Length;
        
        return bytes;
    }

    public static VehicleData Deserialize(byte[] bytes)
    {
        int index = 0;
        int intValue;

        VehicleData data = new VehicleData();

        Protocol.Deserialize(out data.playerId, bytes, ref index);
        Protocol.Deserialize(out intValue, bytes, ref index);
        data.playerLevel = intValue;

        data.playerName = bytes.StringFromPhotonBytes(ref index);

        data.country = bytes.StringFromPhotonBytes(ref index);

        int maxArmor, armor, attack, platform;
        Protocol.Deserialize(out maxArmor, bytes, ref index);
        Protocol.Deserialize(out armor, bytes, ref index);
        Protocol.Deserialize(out attack, bytes, ref index);
        data.maxArmor = maxArmor;
        data.armor = armor;
        data.attack = attack;

        float floatValue;
        Protocol.Deserialize(out floatValue, bytes, ref index);
        data.speed = floatValue;
        Protocol.Deserialize(out floatValue, bytes, ref index);
        data.roF = floatValue;
        Protocol.Deserialize(out intValue, bytes, ref index);
        data.magazine = intValue;
        Protocol.Deserialize(out intValue, bytes, ref index);
        data.newbie = intValue == 1;
        Protocol.Deserialize(out platform, bytes, ref index);
        data.socialPlatform = (SocialPlatform)platform;
        Protocol.Deserialize(out intValue, bytes, ref index);
        data.regeneration = intValue;
        Protocol.Deserialize(out floatValue, bytes, ref index);
        data.takenDamageRatio = floatValue;

        data.socialUID = bytes.StringFromPhotonBytes(ref index);

        Protocol.Deserialize(out intValue, bytes, ref index);
        data.patternId = intValue;
        Protocol.Deserialize(out data.teamId, bytes, ref index);
        Protocol.Deserialize(out intValue, bytes, ref index);
        data.decalId = intValue;

        Protocol.Deserialize(out intValue, bytes, ref index);
        data.hideMyFlag = intValue == 1;

        Protocol.Deserialize(out data.profileId, bytes, ref index);
        Protocol.Deserialize(out intValue, bytes, ref index);
        data.vip = intValue == 1;

        // Clan name
        data.clanName = bytes.StringFromPhotonBytes(ref index);

        return data;
    }
}
