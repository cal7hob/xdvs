using System.Linq;
using UnityEngine;
using System;
using System.Collections.Generic;
using UnityEngine.SocialPlatforms;

public static class AchievementsIds
{
    public enum PlayServiceType
    {
        Disabled,
        GooglePlay,
        IOSGameCenter,
        MACGameCenter,
        SocialNetworks
    }

    public enum Id
    {
        ReachedLevel2 = 2,
        ReachedLevel3,
        ReachedLevel4,
        ReachedLevel5,
        ReachedLevel6,
        ReachedLevel7,
        ReachedLevel8,
        ReachedLevel9,
        ReachedLevel10,
        ReachedLevel11,
        ReachedLevel12,
        ReachedLevel13,
        ReachedLevel14,
        ReachedLevel15,
        ReachedLevel16,
        ReachedLevel17,
        ReachedLevel18,
        ReachedLevel19,
        ReachedLevel20,
        ReachedLevel21,
        ReachedLevel22,
        ReachedLevel23,
        ReachedLevel24,
        ReachedLevel25,
        ReachedLevel26,
        ReachedLevel27,
        ReachedLevel28,
        ReachedLevel29,
        ReachedLevel30,
        ReachedLevel31,
        ReachedLevel32,
        ReachedLevel33,
        ReachedLevel34,
        ReachedLevel35,
        ReachedLevel36,
        NewbieTankMan,
        ExperiencedTanker,
        ProfessionalTanker,
        Shredder,
        Killer,
        Terminator,
        Ordinary,
        Sergeant,
        Colonel,
        Gambler,
        Admirer,
        Fans,
        Scout,
        Travelers,
        Wanderer,
        Survivor
    }

    public static PlayServiceType CurrentPlayService
    {
        get
        {
            switch (Application.platform)
            {
                case RuntimePlatform.IPhonePlayer: return PlayServiceType.IOSGameCenter;
                case RuntimePlatform.OSXPlayer: return PlayServiceType.MACGameCenter;
                case RuntimePlatform.Android: return PlayServiceType.GooglePlay;
                case RuntimePlatform.WindowsWebPlayer:
                case RuntimePlatform.WebGLPlayer: return PlayServiceType.SocialNetworks;
                default:
                    return PlayServiceType.Disabled;
            }
        }
    }

    public static bool isInited = false;
    public static Dictionary<string, Id> storeIdToEnum = new Dictionary<string, Id>();
    public static Dictionary<string, Id> enumStringToEnum = new Dictionary<string, Id>();

    public static readonly Dictionary<Interface, Dictionary<PlayServiceType, Dictionary<Id, string>>> data = new Dictionary<Interface, Dictionary<PlayServiceType, Dictionary<Id, string>>>
	{
        {
            Interface.Armada2, new Dictionary<PlayServiceType, Dictionary<Id, string>>
            {
                {
                    PlayServiceType.GooglePlay, new Dictionary<Id, string>
                    {
                        { Id.ReachedLevel2,  "CggI2cKEumwQAhAU" }, { Id.ReachedLevel3,  "CggI2cKEumwQAhAV" }, { Id.ReachedLevel4,  "CggI2cKEumwQAhAW" },
                        { Id.ReachedLevel5,  "CggI2cKEumwQAhAX" }, { Id.ReachedLevel6,  "CggI2cKEumwQAhAY" }, { Id.ReachedLevel7,  "CggI2cKEumwQAhAZ" },
                        { Id.ReachedLevel8,  "CggI2cKEumwQAhAa" }, { Id.ReachedLevel9,  "CggI2cKEumwQAhAb" }, { Id.ReachedLevel10, "CggI2cKEumwQAhAc" },
                        { Id.ReachedLevel11, "CggI2cKEumwQAhAd" }, { Id.ReachedLevel12, "CggI2cKEumwQAhAe" }, { Id.ReachedLevel13, "CggI2cKEumwQAhAf" },
                        { Id.ReachedLevel14, "CggI2cKEumwQAhAg" }, { Id.ReachedLevel15, "CggI2cKEumwQAhAh" }, { Id.ReachedLevel16, "CggI2cKEumwQAhAi" },
                        { Id.ReachedLevel17, "CggI2cKEumwQAhAj" }, { Id.ReachedLevel18, "CggI2cKEumwQAhAk" }, { Id.ReachedLevel19, "CggI2cKEumwQAhAl" },
                        { Id.ReachedLevel20, "CggI2cKEumwQAhAm" }, { Id.ReachedLevel21, "CggI2cKEumwQAhAn" }, { Id.ReachedLevel22, "CggI2cKEumwQAhAo" },
                        { Id.ReachedLevel23, "CggI2cKEumwQAhAp" }, { Id.ReachedLevel24, "CggI2cKEumwQAhAq" }, { Id.ReachedLevel25, "CggI2cKEumwQAhAr" },
                        { Id.ReachedLevel26, "CggI2cKEumwQAhAs" }, { Id.ReachedLevel27, "CggI2cKEumwQAhAt" }, { Id.ReachedLevel28, "CggI2cKEumwQAhAu" },
                        { Id.ReachedLevel29, "CggI2cKEumwQAhAv" }, { Id.ReachedLevel30, "CggI2cKEumwQAhAw" }, { Id.ReachedLevel31, "CggI2cKEumwQAhAx" },
                        { Id.ReachedLevel32, "CggI2cKEumwQAhAy" }, { Id.ReachedLevel33, "CggI2cKEumwQAhAz" }, { Id.ReachedLevel34, "CggI2cKEumwQAhA0" },
                        { Id.ReachedLevel35, "CggI2cKEumwQAhA1" }, { Id.ReachedLevel36, "CggI2cKEumwQAhA2" },

                        { Id.NewbieTankMan, "CggI2cKEumwQAhAB"}, { Id.ExperiencedTanker, "CggI2cKEumwQAhAF" }, { Id.ProfessionalTanker, "CggI2cKEumwQAhAG" },
                        { Id.Shredder,      "CggI2cKEumwQAhAH"}, { Id.Killer,            "CggI2cKEumwQAhAI" }, { Id.Terminator,         "CggI2cKEumwQAhAJ" },

                        { Id.Ordinary, "CggI2cKEumwQAhAK" }, { Id.Sergeant,  "CggI2cKEumwQAhAL" }, {Id.Colonel,  "CggI2cKEumwQAhAM"},
                        { Id.Gambler,  "CggI2cKEumwQAhAN" }, { Id.Admirer,   "CggI2cKEumwQAhAO" }, {Id.Fans,     "CggI2cKEumwQAhAP"},
                        { Id.Scout,    "CggI2cKEumwQAhAQ" }, { Id.Travelers, "CggI2cKEumwQAhAR" }, {Id.Wanderer, "CggI2cKEumwQAhAS"},
                        { Id.Survivor, "CggI2cKEumwQAhAT" }
                    }
                },
                {
                    PlayServiceType.IOSGameCenter,new Dictionary<Id,string>
                    {
                        { Id.ReachedLevel2,  "grp.1100" }, { Id.ReachedLevel3,  "grp.1101" }, { Id.ReachedLevel4,  "grp.1102" }, { Id.ReachedLevel5,  "grp.1103" }, { Id.ReachedLevel6,  "grp.1104" },
                        { Id.ReachedLevel7,  "grp.1105" }, { Id.ReachedLevel8,  "grp.1106" }, { Id.ReachedLevel9,  "grp.1107" }, { Id.ReachedLevel10, "grp.1108" }, { Id.ReachedLevel11, "grp.1109" },
                        { Id.ReachedLevel12, "grp.1110" }, { Id.ReachedLevel13, "grp.1112" }, { Id.ReachedLevel14, "grp.1113" }, { Id.ReachedLevel15, "grp.1114" }, { Id.ReachedLevel16, "grp.1115" },
                        { Id.ReachedLevel17, "grp.1116" }, { Id.ReachedLevel18, "grp.1117" }, { Id.ReachedLevel19, "grp.1118" }, { Id.ReachedLevel20, "grp.1119" }, { Id.ReachedLevel21, "grp.1120" },
                        { Id.ReachedLevel22, "grp.1121" }, { Id.ReachedLevel23, "grp.1122" }, { Id.ReachedLevel24, "grp.1123" }, { Id.ReachedLevel25, "grp.1124" }, { Id.ReachedLevel26, "grp.1125" },
                        { Id.ReachedLevel27, "grp.1126" }, { Id.ReachedLevel28, "grp.1127" }, { Id.ReachedLevel29, "grp.1128" }, { Id.ReachedLevel30, "grp.1129" }, { Id.ReachedLevel31, "grp.1130" },
                        { Id.ReachedLevel32, "grp.1131" }, { Id.ReachedLevel33, "grp.1132" }, { Id.ReachedLevel34, "grp.1133" }, { Id.ReachedLevel35, "grp.1134" }, { Id.ReachedLevel36, "grp.1151" },

                        { Id.NewbieTankMan, "grp.1135"}, { Id.ExperiencedTanker, "grp.1136" }, { Id.ProfessionalTanker, "grp.1137" },
                        { Id.Shredder,      "grp.1138"}, { Id.Killer,            "grp.1139" }, { Id.Terminator,         "grp.1140" },

                        { Id.Ordinary,  "grp.1141"}, { Id.Sergeant,  "grp.1142"},  { Id.Colonel,  "grp.1143"},
                        { Id.Gambler,   "grp.1144"}, { Id.Admirer,   "grp.1145"},  { Id.Fans,     "grp.1146"},
                        { Id.Scout,     "grp.1147"}, { Id.Travelers, "grp.1148"}, { Id.Wanderer, "grp.1149"},
                        { Id.Survivor,  "grp.1150"}
                    }
                },
                {
                    PlayServiceType.MACGameCenter,new Dictionary<Id,string>
                    {
                        { Id.ReachedLevel2,  "grp.1100" }, { Id.ReachedLevel3,  "grp.1101" }, { Id.ReachedLevel4,  "grp.1102" }, { Id.ReachedLevel5,  "grp.1103" }, { Id.ReachedLevel6,  "grp.1104" },
                        { Id.ReachedLevel7,  "grp.1105" }, { Id.ReachedLevel8,  "grp.1106" }, { Id.ReachedLevel9,  "grp.1107" }, { Id.ReachedLevel10, "grp.1108" }, { Id.ReachedLevel11, "grp.1109" },
                        { Id.ReachedLevel12, "grp.1110" }, { Id.ReachedLevel13, "grp.1112" }, { Id.ReachedLevel14, "grp.1113" }, { Id.ReachedLevel15, "grp.1114" }, { Id.ReachedLevel16, "grp.1115" },
                        { Id.ReachedLevel17, "grp.1116" }, { Id.ReachedLevel18, "grp.1117" }, { Id.ReachedLevel19, "grp.1118" }, { Id.ReachedLevel20, "grp.1119" }, { Id.ReachedLevel21, "grp.1120" },
                        { Id.ReachedLevel22, "grp.1121" }, { Id.ReachedLevel23, "grp.1122" }, { Id.ReachedLevel24, "grp.1123" }, { Id.ReachedLevel25, "grp.1124" }, { Id.ReachedLevel26, "grp.1125" },
                        { Id.ReachedLevel27, "grp.1126" }, { Id.ReachedLevel28, "grp.1127" }, { Id.ReachedLevel29, "grp.1128" }, { Id.ReachedLevel30, "grp.1129" }, { Id.ReachedLevel31, "grp.1130" },
                        { Id.ReachedLevel32, "grp.1131" }, { Id.ReachedLevel33, "grp.1132" }, { Id.ReachedLevel34, "grp.1133" }, { Id.ReachedLevel35, "grp.1134" }, { Id.ReachedLevel36, "grp.1151" },

                        { Id.NewbieTankMan, "grp.1135"}, { Id.ExperiencedTanker, "grp.1136" }, { Id.ProfessionalTanker, "grp.1137" },
                        { Id.Shredder,      "grp.1138"}, { Id.Killer,            "grp.1139" }, { Id.Terminator,         "grp.1140" },

                        { Id.Ordinary,  "grp.1141"}, { Id.Sergeant,  "grp.1142"},  { Id.Colonel,  "grp.1143"},
                        { Id.Gambler,   "grp.1144"}, { Id.Admirer,   "grp.1145"},  { Id.Fans,     "grp.1146"},
                        { Id.Scout,     "grp.1147"}, { Id.Travelers, "grp.1148"}, { Id.Wanderer, "grp.1149"},
                        { Id.Survivor,  "grp.1150"}
                    }
                },
            }
        },

    };

    public static readonly Dictionary<Interface, Dictionary<PlayServiceType, string>> leaderBoardIds = new Dictionary<Interface, Dictionary<PlayServiceType, string>>
	{
        {
        Interface.Armada2, new Dictionary<PlayServiceType,string>
            {
                { PlayServiceType.GooglePlay,    "CggI2cKEumwQAhA3" },
                { PlayServiceType.IOSGameCenter, "grp.armada2_leaderboard"},
                { PlayServiceType.MACGameCenter, "grp.armada2_leaderboard"},
            }
        },      
    };

    public static void Init()
    {
        if (isInited)
            return;

        if (!data.ContainsKey(GameData.CurInterface) || !data[GameData.CurInterface].ContainsKey(CurrentPlayService))
        {
            Debug.LogError("!!! YOU MUST DEFINE ACHIEVEMENTS IDs !!!");
            return;
        }

        storeIdToEnum.Clear();
        enumStringToEnum.Clear();
        foreach (var ach in data[GameData.CurInterface][CurrentPlayService])
        {
            storeIdToEnum[ach.Value] = ach.Key;
            enumStringToEnum[ach.Key.ToString()] = ach.Key;
        }

        isInited = true;
    }

    public static bool GetAchievementIdFromStoreId(string stringId, ref Id _id)
    {
        if(!storeIdToEnum.ContainsKey(stringId))
        {
            Debug.LogErrorFormat("!!! storeIdToEnum not contains key {0} !!!", stringId);
            return false;
        }

        _id = storeIdToEnum[stringId];
        return true;
    }

    public static bool GetAchieveIdFromEnumString(string stringId, ref Id _id)
    {
        if (!enumStringToEnum.ContainsKey(stringId))
        {
            Debug.LogErrorFormat("!!! enumStringToEnum not contains key {0} !!!", stringId);
            return false;
        }

        _id = enumStringToEnum[stringId];
        return true;
    }

    public static string GetLeaderboardId()
    {
        if (!leaderBoardIds.ContainsKey(GameData.CurInterface) || !leaderBoardIds[GameData.CurInterface].ContainsKey(CurrentPlayService))
        {
            Debug.LogError("!!! YOU MUST DEFINE LEADERBOARD ID !!!");
            return "";
        }

        return leaderBoardIds[GameData.CurInterface][CurrentPlayService];
    }

    public static string GetAchievementIdFromDataDic(Id id)
    {
        //Debug.Log (GameData.IsGame(Game.IronTanks) + " " + curPlayService.ToString() + " achiev id " + id.ToString());
        return data[GameData.CurInterface][CurrentPlayService][id];
    }
}
