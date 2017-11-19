using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using MiniJSON;
using UnityEngine;

public class BattleStatisticsManager : MonoBehaviour
{
    public static bool isRevengeDone;
    public static bool isRoomFull;
    public List<PlayerStat> AllVehiclesStatsSorted { get; private set; }

    public static BattleStatisticsManager Instance { get; private set; }

    private static readonly Dictionary<string, int> battleStats = new Dictionary<string, int>
    {
        { "EarnedReloadSpeedBoosters", 0 },
        { "EarnedMoveSpeedBoosters", 0 },
        { "EarnedAttackBoosters", 0 },
        { "EarnedHeals", 0 },
        { "EarnedMines", 0 },
        { "EarnedGold", 0 },
        { "EarnedSilver", 0 },
        { "EarnedExperience", 0 },
        { "EarnedMissiles", 0 },
        { "Shoots", 0 },
        { "Shoots_SACLOS", 0 },
        { "Shoots_IRCM", 0 },
        { "Hits", 0 },
        { "Hits_SACLOS", 0 },
        { "TakenHits", 0 },
        { "TakenHits_SACLOS", 0 },
        { "GivenDamage", 0 },
        { "TakenDamage", 0 },
        { "GivenDamage_SACLOS", 0 },
        { "TakenDamage_SACLOS", 0 },
        { "Deaths", 0 },
        { "Frags", 0 },
        { "Accuracy", 0 },
        { "Accuracy_SACLOS", 0 },
        { "Mileage", 0 },
        { "EarnedFuel", 0 },
        { "MyPlayerId", 0 },
        { "RoomLevel", 0 },
        { "ProperEndBattle", 0 },
        { "PhotonDisconnect", 0 },
        { "TanksInRoomQuantity", 0 },
        { "PlayedTime", 0 },
        { "MaxKillsInARowPerBattle", 0 },
        { "ConnectionFailed", 0 }
    };

    private static Dictionary<string, int> overallBattleStats = new Dictionary<string, int>()
	{
		{ "TotalEarnedReloadSpeedBoosters", 0 },
		{ "TotalEarnedMoveSpeedBoosters", 0 },
		{ "TotalEarnedAttackBoosters", 0 },
		{ "TotalEarnedHeals", 0 },
		{ "TotalEarnedMines", 0 },
		{ "TotalEarnedGold", 0 },
		{ "TotalEarnedSilver", 0 },
		{ "TotalEarnedExperience", 0 },
		{ "TotalShoots", 0 },
		{ "TotalHits", 0 },
        { "TotalShoots_SACLOS", 0 },
		{ "TotalHits_SACLOS", 0 },
        { "TotalShoots_IRCM", 0 },
		{ "TotalGivenDemage", 0 },
        { "TotalGivenDamage_SACLOS", 0 },
		{ "TotalDeaths", 0 },
		{ "TotalFrags", 0 },
		{ "TotalMileage", 0 },
		{ "TotalEarnedFuel", 0 },
		{ "BattlesCount", 0 },
		{ "OverralAccuracy", 0 },
        { "OverralAccuracy_SACLOS", 0 },
        { "TotalPlayedTime", 0 },
		{ "MaxKillsInARow", 0 }
	};

    // For FT only, becomes true when all vehicles (10) in the room
    private int killsInARow;

    public static Dictionary<string, int> BattleStats
	{
	    get { return battleStats; }
	}

    public Dictionary<int, PlayerStat> OutOfTimeVehicles
    {
        get; set;
    }

    public static Dictionary<string, int> OverallBattleStats
	{
		get { return overallBattleStats; }
	}

    void Awake()
    {
        Instance = this;
        Dispatcher.Subscribe(EventId.MyTankShots, CountShoots);
        Dispatcher.Subscribe(EventId.TankTakesDamage, CountGivenDamage);
        Dispatcher.Subscribe(EventId.TankKilled, CountFragsInARawPerBattle);
        Dispatcher.Subscribe(EventId.TankOutOfTime, SetProperBattleEnd);
        Dispatcher.Subscribe(EventId.MainTankAppeared, SaveRoomLevel);
        Dispatcher.Subscribe(EventId.LeftRoom, SavePlayedTime);
        Dispatcher.Subscribe(EventId.TankJoinedBattle, OnVehicleConnected);
        Dispatcher.Subscribe(EventId.RevengeDone, RevengeComplete);
        Dispatcher.Subscribe(EventId.ItemTaken, CountTakenItems, 4);
        Dispatcher.Subscribe(EventId.LeftRoom, SetMainStats);
        Dispatcher.Subscribe(EventId.GoldAcquired, OnGoldAcquired);
        Dispatcher.Subscribe(EventId.SilverAcquired, OnSilverAcquired);
        Dispatcher.Subscribe(EventId.TroubleDisconnect, OnFailureQuit);
        Dispatcher.Subscribe(EventId.PlayerFled, OnPlayerFled);
        Dispatcher.Subscribe(EventId.TankOutOfTime, OnTankOutOfTime);

        OutOfTimeVehicles = new Dictionary<int, PlayerStat>();

        isRoomFull = false;
        isRevengeDone = false;//В начале боя сбрасываем признак совершенной мести, если мы уже ранее выполнили этот квест. Вдруг такой квест будет еще раз...
    }

    void OnDestroy()
    {
        Dispatcher.Unsubscribe(EventId.MyTankShots, CountShoots);
        Dispatcher.Unsubscribe(EventId.TankTakesDamage, CountGivenDamage);
        Dispatcher.Unsubscribe(EventId.TankKilled, CountFragsInARawPerBattle);
        Dispatcher.Unsubscribe(EventId.TankOutOfTime, SetProperBattleEnd);
        Dispatcher.Unsubscribe(EventId.MainTankAppeared, SaveRoomLevel);
        Dispatcher.Unsubscribe(EventId.LeftRoom, SavePlayedTime);
        Dispatcher.Unsubscribe(EventId.TankJoinedBattle, OnVehicleConnected);
        Dispatcher.Unsubscribe(EventId.RevengeDone, RevengeComplete);
        Dispatcher.Unsubscribe(EventId.ItemTaken, CountTakenItems);
        Dispatcher.Unsubscribe(EventId.LeftRoom, SetMainStats);
        Dispatcher.Unsubscribe(EventId.TankOutOfTime, OnTankOutOfTime);
        Dispatcher.Unsubscribe(EventId.GoldAcquired, OnGoldAcquired);
        Dispatcher.Unsubscribe(EventId.SilverAcquired, OnSilverAcquired);
        Dispatcher.Unsubscribe(EventId.TroubleDisconnect, OnFailureQuit);
        Dispatcher.Unsubscribe(EventId.PlayerFled, OnPlayerFled);
    }

    public static void CalcOverallBattleStatistics()
    {
        AddDataToOverallBattleStats("TotalEarnedGold", "EarnedGold");
        AddDataToOverallBattleStats("TotalEarnedSilver", "EarnedSilver");
        AddDataToOverallBattleStats("TotalEarnedExperience", "EarnedExperience");
        AddDataToOverallBattleStats("TotalEarnedReloadSpeedBoosters", "EarnedReloadSpeedBoosters");
        AddDataToOverallBattleStats("TotalEarnedMoveSpeedBoosters", "EarnedMoveSpeedBoosters");
        AddDataToOverallBattleStats("TotalEarnedAttackBoosters", "EarnedAttackBoosters");
        AddDataToOverallBattleStats("TotalEarnedHeals", "EarnedHeals");
        AddDataToOverallBattleStats("TotalEarnedMines", "EarnedMines");
        AddDataToOverallBattleStats("TotalShoots", "Shoots");
        AddDataToOverallBattleStats("TotalHits", "Hits");
        AddDataToOverallBattleStats("TotalShoots_SACLOS", "Shoots_SACLOS");
        AddDataToOverallBattleStats("TotalHits_SACLOS", "Hits_SACLOS");
        AddDataToOverallBattleStats("TotalShoots_IRCM", "Shoots_IRCM");
        AddDataToOverallBattleStats("TotalGivenDemage", "GivenDamage");
        AddDataToOverallBattleStats("TotalGivenDamage_SACLOS", "GivenDamage_SACLOS");
        AddDataToOverallBattleStats("TotalDeaths", "Deaths");
        AddDataToOverallBattleStats("TotalFrags", "Frags");
        AddDataToOverallBattleStats("TotalMileage", "Mileage");
        AddDataToOverallBattleStats("TotalEarnedFuel", "EarnedFuel");
        OverallBattleStats["OverralAccuracy"] = CalcOverallAccuracy(OverallBattleStats["TotalShoots"], OverallBattleStats["TotalHits"]);
        OverallBattleStats["OverralAccuracy_SACLOS"] = CalcOverallAccuracy(OverallBattleStats["TotalShoots_SACLOS"], OverallBattleStats["TotalHits_SACLOS"]);
        AddDataToOverallBattleStats("TotalPlayedTime", "PlayedTime");


        if (OverallBattleStats.ContainsKey("MaxKillsInARow"))
            OverallBattleStats["MaxKillsInARow"] = Mathf.Max(BattleStats["MaxKillsInARowPerBattle"], OverallBattleStats["MaxKillsInARow"]);
        else
            OverallBattleStats.Add("MaxKillsInARow", 0);

        if (BattleStats["ProperEndBattle"] == 1)
            AddDataToOverallBattleStats("BattlesCount", 1);
    }

    public static void ResetBattleStats()
    {
        foreach (var key in battleStats.Keys.ToList())
            battleStats[key] = 0;
    }

    public static void SetOtherBattleStats()
    {
        if (BattleController.MyVehicle == null)
            return;

        WriteMileage();
        CalcAccuracy();
        SaveMyVehicleId();
    }

    public static void SetOverallBattleStatsDictionary(string OverallBattleStatisticsJson)
    {
        if (string.IsNullOrEmpty(OverallBattleStatisticsJson))
            return;

        var OverallBattleStatisticsObjDict = Json.Deserialize(OverallBattleStatisticsJson) as Dictionary<string, object>;

        if (OverallBattleStatisticsObjDict != null)
            overallBattleStats = OverallBattleStatisticsObjDict.ToDictionary(x => x.Key, x => Convert.ToInt32(x.Value));
    }

    public static string GetOverallBattleStatStringSafely(string key)
    {
        if (OverallBattleStats.ContainsKey(key))
            return OverallBattleStats[key].ToString("N0", GameData.instance.cultureInfo.NumberFormat);

        Debug.LogWarningFormat("Key \"{0}\" not found in OverallBattleStats.");

        return Localizer.GetText("NoData");
    }

    private void OnTankOutOfTime(EventId id, EventInfo info)
    {
        var ei = (EventInfo_I)info;
        var thisVehicleId = ei.int1;

        if (thisVehicleId == BattleController.MyPlayerId)
        {
            Instance.SetAccomplishedBattleStats();
            return;
        }

        VehicleController thisVehicleController;
        if (!BattleController.allVehicles.TryGetValue(thisVehicleId, out thisVehicleController) ||
            thisVehicleController == null)
            return;

        int thisVehicleInnerId = thisVehicleController.data.profileId;
        int alreadyRegisteredId = 0;
        if (!thisVehicleController.IsBot)
        {
            foreach (var outOfTimeVeh in OutOfTimeVehicles.Values)
            {
                if (outOfTimeVeh.profileId == thisVehicleInnerId)
                {
                    alreadyRegisteredId = outOfTimeVeh.playerId;
                    break;
                }
            }
        }

        if (alreadyRegisteredId > 0)
            OutOfTimeVehicles[alreadyRegisteredId] = thisVehicleController.Statistics;
        else
            OutOfTimeVehicles[thisVehicleId] = thisVehicleController.Statistics;
    }

    private static void CountFragsInARawPerBattle(EventId id, EventInfo info)
    {
        var ei = (EventInfo_II)info;

        int victimId = ei.int1;
        int attackerId = ei.int2;


        if (victimId == BattleController.MyPlayerId)
        {
            Instance.killsInARow = 0;
        }
        else if (attackerId == BattleController.MyPlayerId)
        {
            Instance.killsInARow++;
            BattleStats["MaxKillsInARowPerBattle"] = Mathf.Max(Instance.killsInARow, BattleStats["MaxKillsInARowPerBattle"]);
        }
    }

    private static void CountGivenDamage(EventId id, EventInfo ei)
    {
        EventInfo_U info = (EventInfo_U)ei;

        int damage = (int)info[1];
        int attackerId = (int)info[2];

        if (attackerId != BattleController.MyPlayerId)
            return;

        switch ((ShellType)(int)info[3])
        {
            case ShellType.Missile_SACLOS:
                BattleStats["GivenDamage_SACLOS"] += damage;
                BattleStats["Hits_SACLOS"]++;
                return;

            case ShellType.Usual:
                BattleStats["GivenDamage"] += damage;
                BattleStats["Hits"]++;
                return;

            default:
                return;
        }
    }

    private static void CountShoots(EventId id, EventInfo ei)
    {
        EventInfo_I info = (EventInfo_I)ei;

        switch ((ShellType)info.int1)
        {
            case ShellType.Missile_SACLOS:
                BattleStats["Shoots_SACLOS"]++;
                return;

            case ShellType.IRCM:
                BattleStats["Shoots_IRCM"]++;
                return;

            case ShellType.Usual:
                BattleStats["Shoots"]++;
                return;

            default:
                return;
        }
    }

    private static void CountTakenItems(EventId id, EventInfo info)
    {
        var ei = (EventInfo_III)info;

        if (ei.int3 != BattleController.MyPlayerId)
            return;

        switch (ei.int1)
        {
            case (int)BonusItem.BonusType.Boost:
                BattleStats["EarnedMoveSpeedBoosters"]++;
                break;
            case (int)BonusItem.BonusType.Reload:
                BattleStats["EarnedReloadSpeedBoosters"]++;
                break;
            case (int)BonusItem.BonusType.Attack:
                BattleStats["EarnedAttackBoosters"]++;
                break;
            //case (int)BonusItem.BonusType.RocketAttack:
            //    BattleStats["EarnedRocketAttackBoosters"]++;
            //    break;
            case (int)BonusItem.BonusType.Health:
                BattleStats["EarnedHeals"]++;
                break;
            case (int)BonusItem.BonusType.Landmine:
                BattleStats["EarnedMines"]++;
                break;
            case (int)BonusItem.BonusType.MissileShell:
                BattleStats["EarnedMissiles"]++;
                break;
            case (int)BonusItem.BonusType.Fuel:
                BattleStats["EarnedFuel"]++;
                break;
        }
    }

    private static void OnGoldAcquired(EventId id, EventInfo info)
    {
        var ei = (EventInfo_I)info;
        BattleStats["EarnedGold"] += ei.int1;
    }

    private static void OnSilverAcquired(EventId id, EventInfo info)
    {
        var ei = (EventInfo_I)info;
        BattleStats["EarnedSilver"] += ei.int1;
    }

    private static void OnVehicleConnected(EventId id, EventInfo info)
    {
        if (BattleController.allVehicles.Count == GameData.maxPlayers)
            isRoomFull = true;
    }

    private static void RevengeComplete(EventId id, EventInfo info)
    {
        isRevengeDone = true;
    }
    private static void SavePlayedTime(EventId id, EventInfo info)
    {
        BattleStats["PlayedTime"] = (int)BattleController.TimeInBattlePhoton;
    }

    private static void SaveRoomLevel(EventId id, EventInfo info)
    {
        BattleStats["RoomLevel"] = (int)PhotonNetwork.room.CustomProperties["lv"];
    }

    private static void SetMainStats(EventId id, EventInfo info)
    {
        if (BattleController.MyVehicle == null)
            return;

        BattleStats["EarnedExperience"] = BattleController.MyVehicle.Statistics.score;
        BattleStats["Frags"] = BattleController.MyVehicle.Statistics.kills;
        BattleStats["Deaths"] = BattleController.MyVehicle.Statistics.deaths;
    }

    private static void SetProperBattleEnd(EventId id, EventInfo info)
    {
        if (((EventInfo_I)info).int1 != BattleController.MyPlayerId)
            return;

        BattleStats["ProperEndBattle"] = 1;
        BattleStats["PhotonDisconnect"] = 0;
    }

    private static void AddDataToOverallBattleStats(string overallDicKey, int value, bool replace = false)
    {
        if (OverallBattleStats.ContainsKey(overallDicKey))
        {
            if (replace)
                OverallBattleStats[overallDicKey] = value;
            else
                OverallBattleStats[overallDicKey] += value;
        }
        else
        {
            OverallBattleStats.Add(overallDicKey, value);
        }
    }

    private static void AddDataToOverallBattleStats(string overallDicKey, string lastBattleDicKey)
    {
        if (OverallBattleStats.ContainsKey(overallDicKey))
            OverallBattleStats[overallDicKey] += BattleStats[lastBattleDicKey];
        else
            OverallBattleStats.Add(overallDicKey, BattleStats[lastBattleDicKey]);
    }

    private static int CalcOverallAccuracy(int shoots, int hits)
    {
        return shoots > 0 ? Convert.ToInt32((float)hits * 100 / shoots) : 0;
    }

    private static void CalcAccuracy()
    {
        if (BattleStats["Shoots"] > 0)
            BattleStats["Accuracy"] = (int)(BattleStats["Hits"] * 100f / BattleStats["Shoots"]);

        if (BattleStats["Shoots_SACLOS"] > 0)
            BattleStats["Accuracy_SACLOS"] = (int)(BattleStats["Hits_SACLOS"] * 100f / BattleStats["Shoots_SACLOS"]);
    }

    private static void SaveMyVehicleId()
    {
        BattleStats["MyPlayerId"] = BattleController.MyPlayerId;
    }

    private static void WriteMileage()
    {
        BattleStats["Mileage"] = (int)BattleController.MyVehicle.Odometer;
    }

    private void OnFailureQuit(EventId id, EventInfo info)
    {
        if (SetAccomplishedBattleStats())
            return;

        StatTable.MyVehicleRank = 100;
    }

    private void OnPlayerFled(EventId id, EventInfo info)
    {
        if (SetAccomplishedBattleStats())
            return;

        StatTable.MyVehicleRank = 99;
    }

    private bool SetAccomplishedBattleStats()
    {
        if (!BattleController.MyVehicle || !BattleController.BattleAccomplished)
            return false;

        var examinedStats = BattleController.GameStat.Concat(OutOfTimeVehicles);

        AllVehiclesStatsSorted
            = examinedStats
                .Select(x => x.Value)
                .OrderBy(x => x.teamId)
                .ThenByDescending(x => x.score)
                .ThenByDescending(x => x.kills)
                .ThenBy(x => x.deaths)
                .ToList();
        
        int rank = 1;
        int myTeamId = BattleController.MyVehicle.data.teamId;
        foreach (var playerStat in AllVehiclesStatsSorted)
        {
            if (playerStat.playerId == BattleController.MyPlayerId)
            {
                StatTable.MyVehicleRank = rank;
                break;
            }

            if (playerStat.teamId == myTeamId)
                rank++;
        }

        if (AllVehiclesStatsSorted.Count() > 1)
        StatTable.IsEnoughPlayers = true;

        if (GameData.Mode == GameData.GameMode.Team && ScoreCounter.FriendTeamScore <= ScoreCounter.EnemyTeamScore)
        {
            StatTable.MyVehicleRank = 99; //Наша команда проиграла
        }

        BattleStats["ProperEndBattle"] = 1;
        BattleStats["PhotonDisconnect"] = 0;

        return true;
    }
}
