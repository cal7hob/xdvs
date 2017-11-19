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
        Pilot = 10,                 //ReachedLevel10
        Foreman = 20,               //ReachedLevel20
        WarentOfficer = 30,         //ReachedLevel30
        LieutenantCommander = 40,   //ReachedLevel40
        Capitan = 50,               //ReachedLevel50

        Terminator,
        FullMetalJacket,
        Nemesis,
        DoubleAgent,
        NeutralSide,
        BigFan,

        EnemyOfRobots,
        EnemyOfTanks,
        SmasherOfTanks,
        SmasherOfRobots,
        Pathfinder,
        Collector,
        Commander,
        Artificer,
        Engineer,
        Winner,
        MasterCombat,
        ToTheLast,
        Fighter,
        Warrior,
        Veteran,
        Berserk,
        Saboteur,
        Avenger,
        Disintegrator,
        Peacemaker,
        SwiftOne,
        Gatherer,
        Technophobe,
        TotalAnnihilation
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
            Interface.FTRobotsInvasion, new Dictionary<PlayServiceType, Dictionary<Id, string>>
            {
                {
                    PlayServiceType.GooglePlay, new Dictionary<Id, string>
                    {
                        { Id.Pilot,                 "CgkIm5Sn99oaEAIQHA" }, { Id.Foreman,           "CgkIm5Sn99oaEAIQHQ" }, { Id.WarentOfficer,     "CgkIm5Sn99oaEAIQHg" }, { Id.LieutenantCommander,   "CgkIm5Sn99oaEAIQHw" }, { Id.Capitan,   "CgkIm5Sn99oaEAIQIA" },
                        { Id.Fighter ,              "CgkIm5Sn99oaEAIQDA" }, { Id.SwiftOne,          "CgkIm5Sn99oaEAIQGQ" }, { Id.Commander,         "CgkIm5Sn99oaEAIQBg" }, { Id.ToTheLast,             "CgkIm5Sn99oaEAIQCw" },

                        { Id.EnemyOfRobots,         "CgkIm5Sn99oaEAIQAA" }, { Id.EnemyOfTanks,      "CgkIm5Sn99oaEAIQAQ" }, { Id.SmasherOfTanks,    "CgkIm5Sn99oaEAIQAg" }, { Id.SmasherOfRobots,       "CgkIm5Sn99oaEAIQAw" },
                        { Id.Pathfinder,            "CgkIm5Sn99oaEAIQBA" }, { Id.Collector,         "CgkIm5Sn99oaEAIQBQ" }, { Id.Artificer,         "CgkIm5Sn99oaEAIQBw" }, { Id.Engineer,              "CgkIm5Sn99oaEAIQCA" },
                        { Id.Winner,                "CgkIm5Sn99oaEAIQCQ" }, { Id.MasterCombat,      "CgkIm5Sn99oaEAIQCg" }, { Id.Warrior,           "CgkIm5Sn99oaEAIQDQ" }, { Id.Veteran,               "CgkIm5Sn99oaEAIQDg" }, { Id.Berserk,   "CgkIm5Sn99oaEAIQDw" },
                        { Id.Terminator,            "CgkIm5Sn99oaEAIQEA" }, { Id.FullMetalJacket,   "CgkIm5Sn99oaEAIQEQ" }, { Id.Saboteur,          "CgkIm5Sn99oaEAIQEg" },
                        { Id.Avenger,               "CgkIm5Sn99oaEAIQEw" }, { Id.Nemesis,           "CgkIm5Sn99oaEAIQFA" }, { Id.Disintegrator,     "CgkIm5Sn99oaEAIQFQ" }, { Id.DoubleAgent,           "CgkIm5Sn99oaEAIQFg" },
                        { Id.NeutralSide,           "CgkIm5Sn99oaEAIQFw" }, { Id.Peacemaker,        "CgkIm5Sn99oaEAIQGA" }, { Id.Gatherer,          "CgkIm5Sn99oaEAIQGg" },
                        { Id.BigFan,                "CgkIm5Sn99oaEAIQGw" }, { Id.Technophobe,       "CgkIm5Sn99oaEAIQIQ" }, { Id.TotalAnnihilation, "CgkIm5Sn99oaEAIQIg" },
                    }
                },

                {
                    PlayServiceType.IOSGameCenter, new Dictionary<Id, string>
                    {
                        { Id.Pilot,                 "grp.1629" }, { Id.Foreman,           "grp.1630" }, { Id.WarentOfficer,     "grp.1631" }, { Id.LieutenantCommander,   "grp.1632" }, { Id.Capitan,   "grp.1633" },
                        { Id.Fighter ,              "grp.1613" }, { Id.SwiftOne,          "grp.1626" }, { Id.Commander,         "grp.1607" }, { Id.ToTheLast,             "grp.1612" },

                        { Id.EnemyOfRobots,         "grp.1600" }, { Id.EnemyOfTanks,      "grp.1601" }, { Id.SmasherOfTanks,    "grp.1602" }, { Id.SmasherOfRobots,       "grp.1603" },
                        { Id.Pathfinder,            "grp.1605" }, { Id.Collector,         "grp.1606" }, { Id.Artificer,         "grp.1608" }, { Id.Engineer,              "grp.1609" },
                        { Id.Winner,                "grp.1610" }, { Id.MasterCombat,      "grp.1611" }, { Id.Warrior,           "grp.1614" }, { Id.Veteran,               "grp.1615" }, { Id.Berserk,   "grp.1616" },
                        { Id.Terminator,            "grp.1617" }, { Id.FullMetalJacket,   "grp.1618" }, { Id.Saboteur,          "grp.1619" },
                        { Id.Avenger,               "grp.1620" }, { Id.Nemesis,           "grp.1621" }, { Id.Disintegrator,     "grp.1622" }, { Id.DoubleAgent,           "grp.1623" },
                        { Id.NeutralSide,           "grp.1624" }, { Id.Peacemaker,        "grp.1625" }, { Id.Gatherer,          "grp.1627" },
                        { Id.BigFan,                "grp.1628" }, { Id.Technophobe,       "grp.1634" }, { Id.TotalAnnihilation, "grp.1635" },
                    }
                },

                {
                    PlayServiceType.MACGameCenter, new Dictionary<Id, string>
                    {
                        { Id.Pilot,                 "grp.1629" }, { Id.Foreman,           "grp.1630" }, { Id.WarentOfficer,     "grp.1631" }, { Id.LieutenantCommander,   "grp.1632" }, { Id.Capitan,   "grp.1633" },
                        { Id.Fighter ,              "grp.1613" }, { Id.SwiftOne,          "grp.1626" }, { Id.Commander,         "grp.1607" }, { Id.ToTheLast,             "grp.1612" },

                        { Id.EnemyOfRobots,         "grp.1600" }, { Id.EnemyOfTanks,      "grp.1601" }, { Id.SmasherOfTanks,    "grp.1602" }, { Id.SmasherOfRobots,       "grp.1603" },
                        { Id.Pathfinder,            "grp.1605" }, { Id.Collector,         "grp.1606" }, { Id.Artificer,         "grp.1608" }, { Id.Engineer,              "grp.1609" },
                        { Id.Winner,                "grp.1610" }, { Id.MasterCombat,      "grp.1611" }, { Id.Warrior,           "grp.1614" }, { Id.Veteran,               "grp.1615" }, { Id.Berserk,   "grp.1616" },
                        { Id.Terminator,            "grp.1617" }, { Id.FullMetalJacket,   "grp.1618" }, { Id.Saboteur,          "grp.1619" },
                        { Id.Avenger,               "grp.1620" }, { Id.Nemesis,           "grp.1621" }, { Id.Disintegrator,     "grp.1622" }, { Id.DoubleAgent,           "grp.1623" },
                        { Id.NeutralSide,           "grp.1624" }, { Id.Peacemaker,        "grp.1625" }, { Id.Gatherer,          "grp.1627" },
                        { Id.BigFan,                "grp.1628" }, { Id.Technophobe,       "grp.1634" }, { Id.TotalAnnihilation, "grp.1635" },
                    }
                },
            }
        },
    };

    public static readonly Dictionary<Interface, Dictionary<PlayServiceType, string>> leaderBoardIds = new Dictionary<Interface, Dictionary<PlayServiceType, string>>
	{
        {
            Interface.FTRobotsInvasion, new Dictionary<PlayServiceType,string>
            {
                { PlayServiceType.GooglePlay,    "CgkIm5Sn99oaEAIQIw" },
                { PlayServiceType.IOSGameCenter, "grp.tanksvsrobots_leaderboard"},
                { PlayServiceType.MACGameCenter, "grp.tanksvsrobots_leaderboard"},
            }
        }
    };

    public static void Init()
    {
        if (isInited)
            return;

        if (!data.ContainsKey(GameData.CurInterface) || !data[GameData.CurInterface].ContainsKey(CurrentPlayService))
        {
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
            return false;
        }

        _id = storeIdToEnum[stringId];
        return true;
    }

    public static bool GetAchieveIdFromEnumString(string stringId, ref Id _id)
    {
        if (!enumStringToEnum.ContainsKey(stringId))
        {
            return false;
        }

        _id = enumStringToEnum[stringId];
        return true;
    }

    public static string GetLeaderboardId()
    {
        if (!leaderBoardIds.ContainsKey(GameData.CurInterface) || !leaderBoardIds[GameData.CurInterface].ContainsKey(CurrentPlayService))
        {
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
