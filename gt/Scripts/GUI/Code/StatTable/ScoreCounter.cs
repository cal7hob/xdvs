using UnityEngine;
using Hashtable = ExitGames.Client.Photon.Hashtable;
using XDevs.LiteralKeys;
public class ScoreCounter : MonoBehaviour
{
    public static int FriendTeamScore { get; private set; }
    public static int EnemyTeamScore { get; private set; }

    private static ScoreCounter instance;

    void Awake()
    {
        FriendTeamScore = 0;
        EnemyTeamScore = 0;
        instance = this;
    }

    void OnDestroy()
    {
        instance = null;
    }

    #region PUBLIC SECTION
    public static void DeathInto(VehicleController vehicle, int amount = 1)
    {
        if (!BattleController.CheckPlayersCount())
            return;
        switch (GameData.Mode)
        {
            case GameData.GameMode.Deathmatch:
                DeathFreeForAll(vehicle, amount);
                break;
            case GameData.GameMode.Team:
                DeathFreeForAll(vehicle, amount);
                break;
        }
    }

    public static void KillInto(VehicleController vehicle, int amount = 1)
    {
        if (!BattleController.CheckPlayersCount())
            return;

        switch (GameData.Mode)
        {
            case GameData.GameMode.Deathmatch:
                KillFreeForAll(vehicle, amount);
                break;
            case GameData.GameMode.Team:
                KillFreeForAll(vehicle, amount);
                break;
        }
    }

    public static void ScoreInto(VehicleController attacker, int amount = 1)
    {
        if (!BattleController.MyVehicle || !BattleController.CheckPlayersCount() || amount == 0)
            // Не добавлять командный счет, пока неизвестно, за кого ты играешь
            return;

        switch (GameData.Mode)
        {
            case GameData.GameMode.Deathmatch:
                ScoreFreeForAll(attacker, amount);
                break;
            case GameData.GameMode.Team:
                ScoreFreeForAll(attacker, amount);
                break;
        }
    }

    public static void RecalcTeamScore(int teamId, int scoreDelta)
    {
        if (GameData.Mode != GameData.GameMode.Team || !BattleController.MyVehicle)
            return;

        bool friendScore = teamId == BattleController.MyVehicle.data.teamId;
        if (friendScore)
            FriendTeamScore += scoreDelta;
        else
            EnemyTeamScore += scoreDelta;

        Dispatcher.Send(EventId.TeamScoreChanged, new EventInfo_SimpleEvent());
    }
    #endregion

    #region PRIVATE SECTION

    private static void DeathFreeForAll(VehicleController vehicle, int amount)
    {
        if (!vehicle.IsMine)
        {
            return;
        }

        vehicle.SetCustomProperties(StatisticKey.Deaths, vehicle.Statistics.deaths + amount);
    }

   
    private static void KillFreeForAll(VehicleController vehicle, int amount)
    {
        if (!vehicle.IsMine)
        {
            return;
        }
        vehicle.SetCustomProperties(StatisticKey.Kills, vehicle.Statistics.kills + amount);
    }

    private static void ScoreFreeForAll(VehicleController vehicle, int amount)
    {
        if (!vehicle.IsMine)
        {
            return;
        }
        vehicle.SetCustomProperties(StatisticKey.Score, vehicle.Statistics.score + amount);
    }
    #endregion
}
