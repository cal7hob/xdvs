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
                case RuntimePlatform.Android:  return GameData.IsGame(Game.AmazonBuild) ? PlayServiceType.Disabled : PlayServiceType.GooglePlay;
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
            Interface.WWT2, new Dictionary<PlayServiceType, Dictionary<Id, string>>
            {
                {
                    PlayServiceType.GooglePlay, new Dictionary<Id, string>
                    {
                        { Id.ReachedLevel2,  "CgkI7Kr4h_oNEAIQEA" }, { Id.ReachedLevel3,  "CgkI7Kr4h_oNEAIQEQ" }, { Id.ReachedLevel4,  "CgkI7Kr4h_oNEAIQEg" },
                        { Id.ReachedLevel5,  "CgkI7Kr4h_oNEAIQEw" }, { Id.ReachedLevel6,  "CgkI7Kr4h_oNEAIQFA" }, { Id.ReachedLevel7,  "CgkI7Kr4h_oNEAIQFQ" },
                        { Id.ReachedLevel8,  "CgkI7Kr4h_oNEAIQFg" }, { Id.ReachedLevel9,  "CgkI7Kr4h_oNEAIQFw" }, { Id.ReachedLevel10, "CgkI7Kr4h_oNEAIQGA" },
                        { Id.ReachedLevel11, "CgkI7Kr4h_oNEAIQGQ" }, { Id.ReachedLevel12, "CgkI7Kr4h_oNEAIQGg" }, { Id.ReachedLevel13, "CgkI7Kr4h_oNEAIQGw" },
                        { Id.ReachedLevel14, "CgkI7Kr4h_oNEAIQHA" }, { Id.ReachedLevel15, "CgkI7Kr4h_oNEAIQHQ" }, { Id.ReachedLevel16, "CgkI7Kr4h_oNEAIQHg" },
                        { Id.ReachedLevel17, "CgkI7Kr4h_oNEAIQHw" }, { Id.ReachedLevel18, "CgkI7Kr4h_oNEAIQIA" }, { Id.ReachedLevel19, "CgkI7Kr4h_oNEAIQIQ" },
                        { Id.ReachedLevel20, "CgkI7Kr4h_oNEAIQIg" }, { Id.ReachedLevel21, "CgkI7Kr4h_oNEAIQIw" }, { Id.ReachedLevel22, "CgkI7Kr4h_oNEAIQJA" },
                        { Id.ReachedLevel23, "CgkI7Kr4h_oNEAIQJQ" }, { Id.ReachedLevel24, "CgkI7Kr4h_oNEAIQJg" }, { Id.ReachedLevel25, "CgkI7Kr4h_oNEAIQJw" },
                        { Id.ReachedLevel26, "CgkI7Kr4h_oNEAIQKA" }, { Id.ReachedLevel27, "CgkI7Kr4h_oNEAIQKQ" }, { Id.ReachedLevel28, "CgkI7Kr4h_oNEAIQKg" },
                        { Id.ReachedLevel29, "CgkI7Kr4h_oNEAIQKw" }, { Id.ReachedLevel30, "CgkI7Kr4h_oNEAIQLA" }, { Id.ReachedLevel31, "CgkI7Kr4h_oNEAIQLQ" },
                        { Id.ReachedLevel32, "CgkI7Kr4h_oNEAIQLg" }, { Id.ReachedLevel33, "CgkI7Kr4h_oNEAIQLw" }, { Id.ReachedLevel34, "CgkI7Kr4h_oNEAIQMA" },
                        { Id.ReachedLevel35, "CgkI7Kr4h_oNEAIQMg" }, { Id.ReachedLevel36, "CgkI7Kr4h_oNEAIQMw" },

                        { Id.NewbieTankMan, "CgkI7Kr4h_oNEAIQAA"}, { Id.ExperiencedTanker, "CgkI7Kr4h_oNEAIQAQ" }, { Id.ProfessionalTanker, "CgkI7Kr4h_oNEAIQAg" },
                        { Id.Shredder,      "CgkI7Kr4h_oNEAIQAw"}, { Id.Killer,            "CgkI7Kr4h_oNEAIQBA" }, { Id.Terminator,         "CgkI7Kr4h_oNEAIQBQ" },

                        { Id.Ordinary, "CgkI7Kr4h_oNEAIQBg" }, { Id.Sergeant,  "CgkI7Kr4h_oNEAIQBw" }, {Id.Colonel,  "CgkI7Kr4h_oNEAIQCA"},
                        { Id.Gambler,  "CgkI7Kr4h_oNEAIQCQ" }, { Id.Admirer,   "CgkI7Kr4h_oNEAIQCg" }, {Id.Fans,     "CgkI7Kr4h_oNEAIQCw"},
                        { Id.Scout,    "CgkI7Kr4h_oNEAIQDA" }, { Id.Travelers, "CgkI7Kr4h_oNEAIQDQ" }, {Id.Wanderer, "CgkI7Kr4h_oNEAIQDg"},
                        { Id.Survivor, "CgkI7Kr4h_oNEAIQDw" }
                    }
                },
                {
                    PlayServiceType.IOSGameCenter,new Dictionary<Id,string>
                    {
                        { Id.ReachedLevel2, "grp.1502" },{ Id.ReachedLevel3, "grp.1503" },{ Id.ReachedLevel4, "grp.1504" },
                        { Id.ReachedLevel5, "grp.1505" },{ Id.ReachedLevel6, "grp.1506" },{ Id.ReachedLevel7, "grp.1507" },
                        { Id.ReachedLevel8, "grp.1508" },{ Id.ReachedLevel9, "grp.1509" },{ Id.ReachedLevel10, "grp.1510" },
                        { Id.ReachedLevel11, "grp.1511" },{ Id.ReachedLevel12, "grp.1512" },{ Id.ReachedLevel13, "grp.1513" },
                        { Id.ReachedLevel14, "grp.1514" },{ Id.ReachedLevel15, "grp.1515" },{ Id.ReachedLevel16, "grp.1516" },
                        { Id.ReachedLevel17, "grp.1517" },{ Id.ReachedLevel18, "grp.1518" },{ Id.ReachedLevel19, "grp.1519" },
                        { Id.ReachedLevel20, "grp.1520" },{ Id.ReachedLevel21, "grp.1521" },{ Id.ReachedLevel22, "grp.1522" },
                        { Id.ReachedLevel23, "grp.1523" },{ Id.ReachedLevel24, "grp.1524" },{ Id.ReachedLevel25, "grp.1525" },
                        { Id.ReachedLevel26, "grp.1526" },{ Id.ReachedLevel27, "grp.1527" },{ Id.ReachedLevel28, "grp.1528" },
                        { Id.ReachedLevel29, "grp.1529" },{ Id.ReachedLevel30, "grp.1530" },{ Id.ReachedLevel31, "grp.1531" },
                        { Id.ReachedLevel32, "grp.1532" },{ Id.ReachedLevel33, "grp.1533" },{ Id.ReachedLevel34, "grp.1534" },
                        { Id.ReachedLevel35, "grp.1535" },{ Id.ReachedLevel36, "grp.1536" },
                        
                        { Id.NewbieTankMan, "grp.1537"}, { Id.ExperiencedTanker, "grp.1538" }, { Id.ProfessionalTanker, "grp.1539" },
                        { Id.Shredder,      "grp.1540"}, { Id.Killer,            "grp.1541" }, { Id.Terminator,         "grp.1542" },

                        { Id.Ordinary,  "grp.1543"}, { Id.Sergeant,  "grp.1544"},  { Id.Colonel,  "grp.1545"},
                        { Id.Gambler,   "grp.1546"}, { Id.Admirer,   "grp.1547"},  { Id.Fans,     "grp.1548"},
                        { Id.Scout,     "grp.1549"}, { Id.Travelers, "grp.1550"}, { Id.Wanderer, "grp.1551"},
                        { Id.Survivor,  "grp.1552"}
                    }
                },
                {
                    PlayServiceType.MACGameCenter,new Dictionary<Id,string>
                    {
                        { Id.ReachedLevel2, "grp.1502" },{ Id.ReachedLevel3, "grp.1503" },{ Id.ReachedLevel4, "grp.1504" },
                        { Id.ReachedLevel5, "grp.1505" },{ Id.ReachedLevel6, "grp.1506" },{ Id.ReachedLevel7, "grp.1507" },
                        { Id.ReachedLevel8, "grp.1508" },{ Id.ReachedLevel9, "grp.1509" },{ Id.ReachedLevel10, "grp.1510" },
                        { Id.ReachedLevel11, "grp.1511" },{ Id.ReachedLevel12, "grp.1512" },{ Id.ReachedLevel13, "grp.1513" },
                        { Id.ReachedLevel14, "grp.1514" },{ Id.ReachedLevel15, "grp.1515" },{ Id.ReachedLevel16, "grp.1516" },
                        { Id.ReachedLevel17, "grp.1517" },{ Id.ReachedLevel18, "grp.1518" },{ Id.ReachedLevel19, "grp.1519" },
                        { Id.ReachedLevel20, "grp.1520" },{ Id.ReachedLevel21, "grp.1521" },{ Id.ReachedLevel22, "grp.1522" },
                        { Id.ReachedLevel23, "grp.1523" },{ Id.ReachedLevel24, "grp.1524" },{ Id.ReachedLevel25, "grp.1525" },
                        { Id.ReachedLevel26, "grp.1526" },{ Id.ReachedLevel27, "grp.1527" },{ Id.ReachedLevel28, "grp.1528" },
                        { Id.ReachedLevel29, "grp.1529" },{ Id.ReachedLevel30, "grp.1530" },{ Id.ReachedLevel31, "grp.1531" },
                        { Id.ReachedLevel32, "grp.1532" },{ Id.ReachedLevel33, "grp.1533" },{ Id.ReachedLevel34, "grp.1534" },
                        { Id.ReachedLevel35, "grp.1535" },{ Id.ReachedLevel36, "grp.1536" },
                        
                        { Id.NewbieTankMan, "grp.1537"}, { Id.ExperiencedTanker, "grp.1538" }, { Id.ProfessionalTanker, "grp.1539" },
                        { Id.Shredder,      "grp.1540"}, { Id.Killer,            "grp.1541" }, { Id.Terminator,         "grp.1542" },

                        { Id.Ordinary,  "grp.1543"}, { Id.Sergeant,  "grp.1544"},  { Id.Colonel,  "grp.1545"},
                        { Id.Gambler,   "grp.1546"}, { Id.Admirer,   "grp.1547"},  { Id.Fans,     "grp.1548"},
                        { Id.Scout,     "grp.1549"}, { Id.Travelers, "grp.1550"}, { Id.Wanderer, "grp.1551"},
                        { Id.Survivor,  "grp.1552"}
                    }
                },
            }
        },

    };

    public static readonly Dictionary<Interface, Dictionary<PlayServiceType, string>> leaderBoardIds = new Dictionary<Interface, Dictionary<PlayServiceType, string>>
	{
        {
        Interface.WWT2, new Dictionary<PlayServiceType,string>
            {
                { PlayServiceType.GooglePlay,    "CgkI7Kr4h_oNEAIQOA" },
                { PlayServiceType.IOSGameCenter, "grp.grandtanks_leaderboard"},
                { PlayServiceType.MACGameCenter, "grp.grandtanks_leaderboard"},
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
