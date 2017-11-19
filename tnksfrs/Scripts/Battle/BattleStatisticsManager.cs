using System;
using System.Collections.Generic;
using System.Linq;
using MiniJSON;
using UnityEngine;
using XD;

public class BattleStatisticsManager : MonoBehaviour, IBattleStatistics
{
    #region IStatic
    public bool IsEmpty
    {
        get
        {
            return false;
        }
    }

    public StaticType StaticType
    {
        get
        {
            return StaticType.BattleStatistics;
        }
    }

    public void SaveInstance()
    {
        StaticContainer.Set(StaticType, this);
    }

    public void DeleteInstance()
    {
        StaticContainer.Set(StaticType, null);
    }
    #endregion

    #region ISender
    public string Description
    {
        get
        {
            return "[BattleStatisticsManager] " + name;
        }

        set
        {
            name = value;
        }
    }

    private List<ISubscriber> subscribers = null;

    public List<ISubscriber> Subscribers
    {
        get
        {
            if (subscribers == null)
            {
                subscribers = new List<ISubscriber>();
            }
            return subscribers;
        }
    }

    public void AddSubscriber(ISubscriber subscriber)
    {
        if (Subscribers.Contains(subscriber))
        {
            return;
        }
        Subscribers.Add(subscriber);
    }

    public void RemoveSubscriber(ISubscriber subscriber)
    {
        Subscribers.Remove(subscriber);
    }

    public void Event(Message message, params object[] parameters)
    {
        for (int i = 0; i < Subscribers.Count; i++)
        {
            Subscribers[i].Reaction(message, parameters);
        }
    }
    #endregion

    #region ISubscriber       
    public void Reaction(Message message, params object[] parameters)
    {
        switch (message)
        {
            case Message.PrepareToEndBattle:
                SetMainStats();
                CalcOverallBattleStatistics(parameters.Get<BattleResult>() == BattleResult.Victory);
                break;

            case Message.StatisticUpdate:
                if (parameters.Get<StatisticParameter>() == StatisticParameter.Damage)
                {
                    BattleStats["GivenDamage"] += (int)parameters.Get<float>();
                }
                break;
        }
    }
    #endregion

    public static bool isRevengeDone;

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
        { "Shoots_IRCM", 0 },
        { "Hits", 0 },
        { "TakenHits", 0 },
        { "GivenDamage", 0 },
        { "TakenDamage", 0 },
        { "Deaths", 0 },
        { "Frags", 0 },
        { "Accuracy", 0 },
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
        { "TotalShoots_IRCM", 0 },
        { "TotalGivenDamage", 0 },
        { "TotalDeaths", 0 },
        { "TotalFrags", 0 },
        { "TotalMileage", 0 },
        { "TotalEarnedFuel", 0 },
        { "BattlesCount", 0 },
        { "OverralAccuracy", 0 },
        { "TotalPlayedTime", 0 },
        { "MaxKillsInARow", 0 }
    };

    // For FT only, becomes true when all vehicles (10) in the room
    private int killsInARow;

    public static Dictionary<string, int> BattleStats
    {
        get
        {
            return battleStats;
        }
    }

    public static Dictionary<int, PlayerStat> OutOfTimeVehicles
    {
        get; set;
    }

    public static Dictionary<string, int> OverallBattleStats
    {
        get
        {
            //CalcOverallBattleStatistics();
            return overallBattleStats;
        }
    }

    private void Init()
    {

    }

    private void Awake()
    {
        SaveInstance();
        DontDestroyOnLoad(this);
        Dispatcher.Subscribe(EventId.MyTankShots, CountShoots);
        Dispatcher.Subscribe(EventId.TankTakesDamage, CountGivenDamage);
        Dispatcher.Subscribe(EventId.TankKilled, CountFragsInARawPerBattle);
        Dispatcher.Subscribe(EventId.TankOutOfTime, SetProperBattleEnd);
        Dispatcher.Subscribe(EventId.MainTankAppeared, SaveRoomLevel);
        Dispatcher.Subscribe(EventId.LeftRoom, SavePlayedTime);
        Dispatcher.Subscribe(EventId.TankJoinedBattle, OnVehicleConnected);
        Dispatcher.Subscribe(EventId.RevengeDone, RevengeComplete);
        Dispatcher.Subscribe(EventId.ItemTaken, CountTakenItems, 4);
        Dispatcher.Subscribe(EventId.GoldAcquired, OnGoldAcquired);
        Dispatcher.Subscribe(EventId.SilverAcquired, OnSilverAcquired);
        Dispatcher.Subscribe(EventId.TroubleDisconnect, OnFailureQuit);
        Dispatcher.Subscribe(EventId.PlayerFled, OnPlayerFled);
        Dispatcher.Subscribe(EventId.TankOutOfTime, SaveThisVehicleScores);

        OutOfTimeVehicles = new Dictionary<int, PlayerStat>();

        isRevengeDone = false;//В начале боя сбрасываем признак совершенной мести, если мы уже ранее выполнили этот квест. Вдруг такой квест будет еще раз...
    }

    private void Start()
    {
        StaticType.UI.AddSubscriber(this);
        StaticType.GameController.AddSubscriber(this);
    }

    private void OnDestroy()
    {
        DeleteInstance();

        Dispatcher.Unsubscribe(EventId.MyTankShots, CountShoots);
        Dispatcher.Unsubscribe(EventId.TankTakesDamage, CountGivenDamage);
        Dispatcher.Unsubscribe(EventId.TankKilled, CountFragsInARawPerBattle);
        Dispatcher.Unsubscribe(EventId.TankOutOfTime, SetProperBattleEnd);
        Dispatcher.Unsubscribe(EventId.MainTankAppeared, SaveRoomLevel);
        Dispatcher.Unsubscribe(EventId.LeftRoom, SavePlayedTime);
        Dispatcher.Unsubscribe(EventId.TankJoinedBattle, OnVehicleConnected);
        Dispatcher.Unsubscribe(EventId.RevengeDone, RevengeComplete);
        Dispatcher.Unsubscribe(EventId.ItemTaken, CountTakenItems);
        Dispatcher.Unsubscribe(EventId.TankOutOfTime, SaveThisVehicleScores);
        Dispatcher.Unsubscribe(EventId.GoldAcquired, OnGoldAcquired);
        Dispatcher.Unsubscribe(EventId.SilverAcquired, OnSilverAcquired);
        Dispatcher.Unsubscribe(EventId.TroubleDisconnect, OnFailureQuit);
        Dispatcher.Unsubscribe(EventId.PlayerFled, OnPlayerFled);

        StaticType.UI.RemoveSubscriber(this);
        StaticType.GameController.RemoveSubscriber(this);
    }

    public void CalcOverallBattleStatistics(bool victory)
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
        AddDataToOverallBattleStats("TotalShoots_IRCM", "Shoots_IRCM");
        AddDataToOverallBattleStats("TotalGivenDamage", "GivenDamage");
        AddDataToOverallBattleStats("TotalDeaths", "Deaths");
        AddDataToOverallBattleStats("TotalFrags", "Frags");
        AddDataToOverallBattleStats("TotalMileage", "Mileage");
        AddDataToOverallBattleStats("TotalEarnedFuel", "EarnedFuel");
        OverallBattleStats["OverralAccuracy"] = CalcOverallAccuracy(OverallBattleStats["TotalShoots"], OverallBattleStats["TotalHits"]);
        AddDataToOverallBattleStats("TotalPlayedTime", "PlayedTime");


        if (OverallBattleStats.ContainsKey("MaxKillsInARow"))
        {
            OverallBattleStats["MaxKillsInARow"] = Mathf.Max(BattleStats["MaxKillsInARowPerBattle"], OverallBattleStats["MaxKillsInARow"]);
        }
        else
        {
            OverallBattleStats.Add("MaxKillsInARow", 0);
        } 

        AddDataToOverallBattleStats("BattlesCount", 1);
        

        if (victory)
        {
            AddDataToOverallBattleStats("Victories", 1);
        }

    }

    public static void ResetBattleStats()
    {
        foreach (var key in battleStats.Keys.ToList())
        {
            battleStats[key] = 0;
        }
    }

    public void SetOtherBattleStats()
    {
        if (StaticContainer.BattleController.CurrentUnit == null)
            return;

        WriteMileage();
        CalcAccuracy();
        SaveMyVehicleId();
    }

    public void SetOverallStats(string OverallBattleStatisticsJson)
    {
        if (string.IsNullOrEmpty(OverallBattleStatisticsJson))
        {
            Debug.LogError("OverallBattleStatisticsJson is NULL!");
            return;
        }

        Dictionary<string, object> OverallBattleStatisticsObjDict = Json.Deserialize(OverallBattleStatisticsJson) as Dictionary<string, object>;

        if (OverallBattleStatisticsObjDict == null)
        {
            Debug.LogError("OverallBattleStatisticsObjDict is NULL!");
            return;
        }

        //Debug.LogError("SetOverallStats", this);
        overallBattleStats = OverallBattleStatisticsObjDict.ToDictionary(x => x.Key, x => Convert.ToInt32(x.Value));
    }

    public int GetStat(string key)
    {
        int result = 0;
        if (!OverallBattleStats.TryGetValue(key, out result))
        {
            Debug.LogError("Statistics '" + key + "' was not found! ", this);
        }
        return result;
    }

    public static string GetOverallBattleStatStringSafely(string key)
    {
        if (OverallBattleStats.ContainsKey(key))
            return OverallBattleStats[key].ToString("N0", GameData.instance.cultureInfo.NumberFormat);

        Debug.LogWarningFormat("Key \"{0}\" not found in OverallBattleStats.");

        return Localizer.GetText("NoData");
    }

    private void SaveThisVehicleScores(EventId id, EventInfo info)
    {
        EventInfo_I ei = (EventInfo_I)info;
        int thisVehicleId = ei.int1;

        if (StaticContainer.BattleController.Units == null)
        {
            //Debug.LogError(name + " StaticContainer.BattleController.Units == null!", this);
            return;
        }

        VehicleController thisVehicleController;
        if (!StaticContainer.BattleController.Units.TryGetValue(thisVehicleId, out thisVehicleController) || thisVehicleController == null)
        {
            return;
        }

        int thisVehicleInnerId = thisVehicleController.data.innerId;

        if (thisVehicleId == StaticType.BattleController.Instance<IBattleController>().MyPlayerId)
        {
            return;
        }

        foreach (var outOfTimeVehicle in OutOfTimeVehicles)
        {
            if (outOfTimeVehicle.Value.InnerID == thisVehicleInnerId)
            {
                OutOfTimeVehicles[thisVehicleId] = XD.StaticContainer.BattleController.GameStat[thisVehicleId]; //если такой танк уже есть, то обновить статистику по нему
                return;
            }
        }

        OutOfTimeVehicles.Add(thisVehicleId, thisVehicleController.Statistics); //если нет, то добавить
    }

    private void CountFragsInARawPerBattle(EventId id, EventInfo info)
    {
        var ei = (EventInfo_II)info;

        int victimId = ei.int1;
        int attackerId = ei.int2;
        int myPlayerID = StaticType.BattleController.Instance<IBattleController>().MyPlayerId;

        if (victimId == myPlayerID)
        {
            killsInARow = 0;
        }
        else if (attackerId == myPlayerID)
        {
            killsInARow++;
            BattleStats["MaxKillsInARowPerBattle"] = Mathf.Max(killsInARow, BattleStats["MaxKillsInARowPerBattle"]);
        }
    }

    private static void CountGivenDamage(EventId id, EventInfo ei)
    {
        EventInfo_U info = (EventInfo_U)ei;

        int damage = (int)info[1];
        int attackerId = (int)info[2];

        if (attackerId != StaticType.BattleController.Instance<IBattleController>().MyPlayerId)
        {
            return;
        }

        switch ((GunShellInfo.ShellType)(int)info[3])
        {
            case GunShellInfo.ShellType.Usual:
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
        switch ((GunShellInfo.ShellType)info.int1)
        {
            case GunShellInfo.ShellType.IRCM:
                BattleStats["Shoots_IRCM"]++;
                return;

            case GunShellInfo.ShellType.Usual:
                BattleStats["Shoots"]++;
                return;

            default:
                return;
        }
    }

    private static void CountTakenItems(EventId id, EventInfo info)
    {
        var ei = (EventInfo_III)info;

        if (ei.int3 != StaticType.BattleController.Instance<IBattleController>().MyPlayerId)
        {
            return;
        }

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
        if (XD.StaticContainer.BattleController.Units.Count == GameData.maxPlayers)
        {
        }
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

    private static void SetMainStats()
    {
        if (StaticContainer.BattleController.CurrentUnit == null)
        {
            return;
        }
        PlayerStat stat = StaticContainer.BattleController.CurrentUnit.Statistics;

        BattleStats["EarnedExperience"] = stat.Stats[StatisticParameter.Experience];
        BattleStats["Frags"] = stat.Stats[StatisticParameter.Kills];
        BattleStats["Deaths"] = stat.Stats[StatisticParameter.Deaths];
        //BattleStats["Damage"] = stat.Stats[StatisticParameter.Damage];
    }

    private static void SetProperBattleEnd(EventId id, EventInfo info)
    {
        if (((EventInfo_I)info).int1 != StaticType.BattleController.Instance<IBattleController>().MyPlayerId)
        {
            return;
        }

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
        {
            BattleStats["Accuracy"] = (int)(BattleStats["Hits"] * 100f / BattleStats["Shoots"]);
        }
    }

    private static void SaveMyVehicleId()
    {
        BattleStats["MyPlayerId"] = StaticType.BattleController.Instance<IBattleController>().MyPlayerId;
    }

    private static void WriteMileage()
    {
        BattleStats["Mileage"] = (int)StaticContainer.BattleController.CurrentUnit.Odometer;
    }

    private void OnFailureQuit(EventId id, EventInfo info)
    {
        if (SetAccomplishedBattleStats())
        {
            return;
        }

        StaticContainer.BattleController.Rank = 100;
    }

    private void OnPlayerFled(EventId id, EventInfo info)
    {
        if (SetAccomplishedBattleStats())
        {
            return;
        }

        StaticContainer.BattleController.Rank = 99;
    }

    private bool SetAccomplishedBattleStats()
    {
        if (!BattleController.BattleAccomplished)
        {
            return false;
        }

        var allVehiclesStats = XD.StaticContainer.BattleController.GameStat.Concat((OutOfTimeVehicles).ToDictionary(x => x.Key, x => x.Value));

        var allVehiclesStatsSorted
            = allVehiclesStats
                .Select(x => x.Value)
                .OrderBy(x => x.Stats[StatisticParameter.Deaths])
                .OrderByDescending(x => x.Stats[StatisticParameter.Kills])
                .OrderByDescending(x => x.Stats[StatisticParameter.Experience]);

        var rank = 0;

        foreach (var playerStat in allVehiclesStatsSorted)
        {
            rank++;

            if (playerStat.InnerID == StaticType.BattleController.Instance<IBattleController>().MyPlayerId)
            {
                StaticContainer.BattleController.Rank = rank;
            }
        }

        StaticContainer.BattleController.IsEnoughPlayers = true;

        BattleStats["ProperEndBattle"] = 1;
        BattleStats["PhotonDisconnect"] = 0;

        return true;
    }
}
