using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using MiniJSON;
using UnityEngine;

public class BattleStatisticsManager : MonoBehaviour
{
    public const int IGNORED_SHELL_ID = 1000;


    public static bool isRevengeDone;
    public static bool isNotSingleRoom;
    public List<PlayerStat> AllVehiclesStatsSorted { get; private set; }
    private Dictionary<int, int> hitVehicles = new Dictionary<int, int>();
    public static BattleStatisticsManager Instance { get; private set; }

    public class Maps : AETools.ListInt
    {
        public Maps(int size, int value) : base(size, value) { }

        public override void Add(int value)
        {
            if (values.Contains(value)) return;
            base.Add(value);
        }
    }

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
        { "ConnectionFailed", 0 },
        { "KillRobots", 0 },
        { "KillTanks", 0 },
        { "DamageMyRobot", 0 },
        { "DamageMyTank", 0 },
        { "BonusTaken", 0 },
        { "RevengeDoneCount", 0 },
        { "KillWithOneShot", 0 },
        { "FuelLastOne", 0 },
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
        { "MaxKillsInARow", 0 },
        { "TotalKillRobots", 0 },
        { "TotalKillTanks", 0 },
        { "TotalDamageMyRobot", 0 },
        { "TotalDamageMyTank", 0 },
        { "WinBattlesCount", 0 },
        { "TotalBonusTaken", 0 },
        { "TotalRevengeDone", 0 },
        { "TotalKillWithOneShot", 0 },
        { "VisitedMaps", -1 },
        { "VehicleUpgrades", 0 },
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
        if (Instance != null)
        {
            Debug.LogError("There are more than one BattleStatisticsManager on scene");
            Debug.LogError("First one", Instance.gameObject);
            Debug.LogError("Second one", gameObject);
            return;
        }

        Instance = this;

        Messenger.Subscribe(EventId.MyTankShoots, CountShoots);
        Messenger.Subscribe(EventId.PortionOfDamage, CountGivenDamage);
        Messenger.Subscribe(EventId.VehicleKilled, CountFragsInARawPerBattle);
        Messenger.Subscribe(EventId.TankOutOfTime, SetProperBattleEnd);
        Messenger.Subscribe(EventId.MainTankAppeared, SaveRoomLevel);
        Messenger.Subscribe(EventId.LeftRoom, SavePlayedTime);
        Messenger.Subscribe(EventId.TankJoinedBattle, OnVehicleConnected);
        Messenger.Subscribe(EventId.RevengeDone, RevengeComplete);
        Messenger.Subscribe(EventId.ItemTaken, CountTakenItems, 4);
        Messenger.Subscribe(EventId.LeftRoom, SetMainStats);
        Messenger.Subscribe(EventId.GoldAcquired, OnGoldAcquired);
        Messenger.Subscribe(EventId.SilverAcquired, OnSilverAcquired);
        Messenger.Subscribe(EventId.TroubleDisconnect, OnFailureQuit);
        Messenger.Subscribe(EventId.PlayerFled, OnPlayerFled);
        Messenger.Subscribe(EventId.TankOutOfTime, OnTankOutOfTime);
        Messenger.Subscribe(EventId.FuelUpdated, FuelUpdated);
        Messenger.Subscribe(EventId.VehicleTakesDamage, OnVehicleTakesDamage);

        OutOfTimeVehicles = new Dictionary<int, PlayerStat>();

        isNotSingleRoom = false;
        isRevengeDone = false;//В начале боя сбрасываем признак совершенной мести, если мы уже ранее выполнили этот квест. Вдруг такой квест будет еще раз...
    }

    void OnDestroy()
    {
        Messenger.Unsubscribe(EventId.MyTankShoots, CountShoots);
        Messenger.Unsubscribe(EventId.PortionOfDamage, CountGivenDamage);
        Messenger.Unsubscribe(EventId.VehicleKilled, CountFragsInARawPerBattle);
        Messenger.Unsubscribe(EventId.TankOutOfTime, SetProperBattleEnd);
        Messenger.Unsubscribe(EventId.MainTankAppeared, SaveRoomLevel);
        Messenger.Unsubscribe(EventId.LeftRoom, SavePlayedTime);
        Messenger.Unsubscribe(EventId.TankJoinedBattle, OnVehicleConnected);
        Messenger.Unsubscribe(EventId.RevengeDone, RevengeComplete);
        Messenger.Unsubscribe(EventId.ItemTaken, CountTakenItems);
        Messenger.Unsubscribe(EventId.LeftRoom, SetMainStats);
        Messenger.Unsubscribe(EventId.TankOutOfTime, OnTankOutOfTime);
        Messenger.Unsubscribe(EventId.GoldAcquired, OnGoldAcquired);
        Messenger.Unsubscribe(EventId.SilverAcquired, OnSilverAcquired);
        Messenger.Unsubscribe(EventId.TroubleDisconnect, OnFailureQuit);
        Messenger.Unsubscribe(EventId.PlayerFled, OnPlayerFled);
        Messenger.Unsubscribe(EventId.FuelUpdated, FuelUpdated);
        Messenger.Unsubscribe(EventId.VehicleTakesDamage, OnVehicleTakesDamage);

        Instance = null;
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
        AddDataToOverallBattleStats("TotalKillRobots", "KillRobots");
        AddDataToOverallBattleStats("TotalKillTanks", "KillTanks");
        AddDataToOverallBattleStats("TotalDamageMyRobot", "DamageMyRobot");
        AddDataToOverallBattleStats("TotalDamageMyTank", "DamageMyTank");
        AddDataToOverallBattleStats("TotalBonusTaken", "BonusTaken");
        AddDataToOverallBattleStats("TotalRevengeDone", "RevengeDoneCount");
        AddDataToOverallBattleStats("TotalKillWithOneShot", "KillWithOneShot");
        //Debug.Log("!!!!!!!!!!!!!!!!!!!!ScoreCounter.FriendTeamScore > ScoreCounter.EnemyTeamScore " + ScoreCounter.FriendTeamScore + " > " + ScoreCounter.EnemyTeamScore);
        if (ScoreCounter.FriendTeamScore > ScoreCounter.EnemyTeamScore)
        {
            ++OverallBattleStats["WinBattlesCount"];
        }

        if (OverallBattleStats.ContainsKey("MaxKillsInARow"))
            OverallBattleStats["MaxKillsInARow"] = Mathf.Max(BattleStats["MaxKillsInARowPerBattle"], OverallBattleStats["MaxKillsInARow"]);
        else
            OverallBattleStats.Add("MaxKillsInARow", 0);

        if (BattleStats["ProperEndBattle"] == 1) AddDataToOverallBattleStats("BattlesCount", 1);

        Messenger.Send(EventId.FullCalcBattleStatistics, new EventInfo_SimpleEvent());
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
        {
            if (OverallBattleStatisticsObjDict.Count != overallBattleStats.Count) //fix add new values
            {
                foreach (KeyValuePair<string, int> item in overallBattleStats)
                {
                    if (!OverallBattleStatisticsObjDict.ContainsKey(item.Key)) OverallBattleStatisticsObjDict.Add(item.Key, item.Value);
                }
            }
            overallBattleStats = OverallBattleStatisticsObjDict.ToDictionary(x => x.Key, x => Convert.ToInt32(x.Value));
        }
    }

    public static string GetOverallBattleStatStringSafely(string key)
    {
        if (OverallBattleStats.ContainsKey(key))
            return OverallBattleStats[key].ToString("N0", GameData.instance.cultureInfo.NumberFormat);

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

        
        int alreadyRegisteredId = 0;

        if (!thisVehicleController.IsBot)
        {
            int thisVehicleInnerId = thisVehicleController.data.profileId;

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

    private void FuelUpdated(EventId eventId, EventInfo eventInfo)
    {
        if (((EventInfo_I)eventInfo).int1 < 1) BattleStats["FuelLastOne"] = 1;
    }

    private void CountFragsInARawPerBattle(EventId id, EventInfo info)
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
            VehicleInfo.VehicleType victimType = BattleController.allVehicles[victimId].VehicleType;

            switch (victimType)
            {
                case VehicleInfo.VehicleType.Robot:
                    ++BattleStats["KillRobots"];
                    break;
                case VehicleInfo.VehicleType.Tank:
                    ++BattleStats["KillTanks"];
                    break;
            }

            if (hitVehicles.ContainsKey(victimId))
            {
                if (hitVehicles[victimId] == 1)
                {
                    ++BattleStats["KillWithOneShot"];
                }

                hitVehicles.Remove(victimId);
            }
        }
    }

    private void OnVehicleTakesDamage(EventId eid, EventInfo ei)
    {
        EventInfo_U info = (EventInfo_U) ei;

        if ((int) info[0] != BattleController.MyPlayerId || (DamageSource)info[3] == DamageSource.HenchmanDoes)
            return;

        BattleStats["TakenDamage"] += (int)info[1];
        ++BattleStats["TakenHits"];
    }

    private void CountGivenDamage(EventId id, EventInfo ei)
    {
        EventInfo_IIIIV info = (EventInfo_IIIIV)ei;

        int victimId = info.int1;
        int damage = info.int2;
        int attackerId = info.int3;
        DamageSource damageSource = (DamageSource)info.int4;

        if (attackerId != BattleController.MyPlayerId || damageSource == DamageSource.HenchmanDoes)
            return;

        if (hitVehicles.ContainsKey(victimId))
        {
            hitVehicles[victimId]++;
        }
        else
        {
            hitVehicles.Add(victimId, 1);
        }

        if (BattleController.MyVehicle.VehicleType == VehicleInfo.VehicleType.Robot)
        {
            BattleStats["DamageMyRobot"] += damage;
        }
        else
        {
            BattleStats["DamageMyTank"] += damage;
        }

        if (info.int4 != IGNORED_SHELL_ID)
        {
            BattleStats["GivenDamage"] += damage;
            ++BattleStats["Hits"];
        }
    }

    private static void CountShoots(EventId id, EventInfo ei)
    {
        ++BattleStats["Shoots"];
    }

    private static void CountTakenItems(EventId id, EventInfo info)
    {
        var ei = (EventInfo_III)info;

        if (ei.int3 != BattleController.MyPlayerId)
            return;

        ++BattleStats["BonusTaken"];

        switch (ei.int1)
        {
            case (int)BonusItem.BonusType.Boost:
                ++BattleStats["EarnedMoveSpeedBoosters"];
                break;
            case (int)BonusItem.BonusType.Reload:
                ++BattleStats["EarnedReloadSpeedBoosters"];
                break;
            case (int)BonusItem.BonusType.Attack:
                ++BattleStats["EarnedAttackBoosters"];
                break;
            //case (int)BonusItem.BonusType.RocketAttack:
            //    BattleStats["EarnedRocketAttackBoosters"]++;
            //    break;
            case (int)BonusItem.BonusType.Health:
                ++BattleStats["EarnedHeals"];
                break;
            case (int)BonusItem.BonusType.Landmine:
                ++BattleStats["EarnedMines"];
                break;
            case (int)BonusItem.BonusType.MissileShell:
                ++BattleStats["EarnedMissiles"];
                break;
            case (int)BonusItem.BonusType.Fuel:
                ++BattleStats["EarnedFuel"];
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
        if (BattleController.allVehicles.Count >= 2)
            isNotSingleRoom = true;
    }

    private static void RevengeComplete(EventId id, EventInfo info)
    {
        isRevengeDone = true;
        ++BattleStats["RevengeDoneCount"];
    }
    private static void SavePlayedTime(EventId id, EventInfo info)
    {
        BattleStats["PlayedTime"] = (int)BattleController.TimeInBattleReal;
    }

    private static void SaveRoomLevel(EventId id, EventInfo info)
    {
        BattleStats["RoomLevel"] = (int)PhotonNetwork.room.CustomProperties["lv"];
        Maps visitedMaps = new Maps(10, OverallBattleStats["VisitedMaps"]);
        visitedMaps.Add((int)PhotonNetwork.room.CustomProperties["mp"]);
        OverallBattleStats["VisitedMaps"] = visitedMaps.Serialize();
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

        StatTable.IsEnoughPlayers = AllVehiclesStatsSorted.Count > 1;

        if (GameData.Mode == GameData.GameMode.Team && ScoreCounter.FriendTeamScore <= ScoreCounter.EnemyTeamScore)
        {
            StatTable.MyVehicleRank = 99; //Наша команда проиграла
        }

        BattleStats["ProperEndBattle"] = 1;
        BattleStats["PhotonDisconnect"] = 0;

        return true;
    }
}
