using System;
using System.Text;
using CodeStage.AntiCheat.ObscuredTypes;
using ExitGames.Client.Photon;
using UnityEngine;
using System.Collections.Generic;
using Disconnect;

[Serializable]
public class TankData
{
    /// <summary>
    /// Id игрока от Photon'а внутри комнаты
    /// </summary>
    public int playerId;
    public ObscuredString playerName;
    public ObscuredInt playerLevel;
    public bool newbie;
    public SocialPlatform socialPlatform;
    public ObscuredString socialUID;
    public ObscuredString country;
    public ObscuredInt patternId;
    public ObscuredInt decalId;
    public ObscuredInt maxArmor;
    public ObscuredInt armor;
    public ObscuredInt attack;
    public ObscuredInt rocketAttack;
    public ObscuredFloat speed;
    public ObscuredFloat rof;
    public ObscuredFloat ircmRof;
    public int teamId;
    public bool hideMyFlag;
    /// <summary>
    /// Id игрока на сервере
    /// </summary>
    public int profileId;
    public bool vip;
    public string clanName;
    public ObscuredInt regeneration; // Лечение (ед./с)
    public ObscuredFloat takenDamageRatio; //Коэффициент к получаемому урону
    public ObscuredInt shield; // Щит - сколько единиц снимается из получаемого урона

    public TankData() { }

    public TankData(
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
        int                                                     shield,
        float                                                   takenDamageRatio
)
    {
        this.playerName = playerName;
        this.playerLevel = playerLevel;
        this.country = country;
        this.newbie = newbie;
        this.socialPlatform = socialPlatform;
        this.socialUID = socialUID;
        maxArmor = (int)hangarParameters[VehicleInfo.VehicleParameter.Armor];
        armor = maxArmor;
        attack = (int)hangarParameters[VehicleInfo.VehicleParameter.Damage];
        rocketAttack = (int)hangarParameters[VehicleInfo.VehicleParameter.RocketDamage];
        speed = hangarParameters[VehicleInfo.VehicleParameter.Speed];
        rof = hangarParameters[VehicleInfo.VehicleParameter.RoF];
        ircmRof = hangarParameters[VehicleInfo.VehicleParameter.IRCMRoF];
        this.patternId = patternId;
        this.decalId = decalId;
        this.teamId = teamId;
        this.hideMyFlag = hideMyFlag;
        this.profileId = profileId;
        this.vip = vip;
        this.clanName = clanName;
        this.regeneration = regeneration;
        this.takenDamageRatio = takenDamageRatio;
        this.shield = shield;
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
                + 10 /* – инкрементить, если добавляем новые боевые статы */ * 4
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
        Protocol.Serialize(data.rocketAttack, bytes, ref index);
        Protocol.Serialize(data.speed, bytes, ref index);
        Protocol.Serialize(data.rof, bytes, ref index);
        Protocol.Serialize(data.ircmRof, bytes, ref index);
        Protocol.Serialize(data.newbie ? 1 : 0, bytes, ref index);
        Protocol.Serialize((int)data.socialPlatform, bytes, ref index);
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

        Protocol.Serialize(data.regeneration, bytes, ref index);
        Protocol.Serialize(data.shield, bytes, ref index);
        Protocol.Serialize(data.takenDamageRatio, bytes, ref index);
        
        return bytes;
    }

    public static TankData Deserialize(byte[] bytes)
    {
        int index = 0;
        int length, intValue;

        TankData data = new TankData();

        Protocol.Deserialize(out data.playerId, bytes, ref index);
        Protocol.Deserialize(out intValue, bytes, ref index);
        data.playerLevel = intValue;

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

        int maxArmor, armor, attack, rocketAttack, platform;

        Protocol.Deserialize(out maxArmor, bytes, ref index);
        Protocol.Deserialize(out armor, bytes, ref index);
        Protocol.Deserialize(out attack, bytes, ref index);
        Protocol.Deserialize(out rocketAttack, bytes, ref index);

        data.maxArmor = maxArmor;
        data.armor = armor;
        data.attack = attack;
        data.rocketAttack = rocketAttack;

        float floatValue;
        Protocol.Deserialize(out floatValue, bytes, ref index);
        data.speed = floatValue;
        Protocol.Deserialize(out floatValue, bytes, ref index);
        data.rof = floatValue;
        Protocol.Deserialize(out floatValue, bytes, ref index);
        data.ircmRof = floatValue;
        Protocol.Deserialize(out intValue, bytes, ref index);

        data.newbie = intValue == 1;

        Protocol.Deserialize(out platform, bytes, ref index);

        data.socialPlatform = (SocialPlatform)platform;

        Protocol.Deserialize(out length, bytes, ref index);

        stringBytes = new byte[length];

        Array.Copy(bytes, index, stringBytes, 0, length);

        data.socialUID = Encoding.UTF8.GetString (stringBytes, 0, stringBytes.Length);

        index += length;

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

        Protocol.Deserialize(out length, bytes, ref index);

        if (length > 0)
        {
            stringBytes = new byte[length];
            Array.Copy(bytes, index, stringBytes, 0, length);
            data.clanName = Encoding.UTF8.GetString(stringBytes, 0, stringBytes.Length);
            index += length;
        }

        Protocol.Deserialize(out intValue, bytes, ref index);
        data.regeneration = intValue;
        Protocol.Deserialize(out intValue, bytes, ref index);
        data.shield = intValue;
        Protocol.Deserialize(out floatValue, bytes, ref index);
        data.takenDamageRatio = floatValue;

        return data;
    }
}
