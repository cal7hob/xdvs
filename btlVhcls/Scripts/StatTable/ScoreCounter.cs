using UnityEngine;
using Hashtable = ExitGames.Client.Photon.Hashtable;

public class ScoreCounter : MonoBehaviour
{
    public static int FriendTeamScore { get; private set; }
    public static int EnemyTeamScore { get; private set; }

	void Awake()
	{
        FriendTeamScore = 0;
		EnemyTeamScore = 0;
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
	    if (!vehicle.PhotonView.isMine)
	        return;

        if (!vehicle.IsBot)
            vehicle.Player.SetCustomProperties(new Hashtable { { "dt", vehicle.Statistics.deaths + amount } });
        else
        {
            Hashtable properties = new Hashtable { { vehicle.KeyForDeaths, vehicle.Statistics.deaths + amount } };
            PhotonNetwork.room.SetCustomProperties(properties);
        }
	}

	private static void KillFreeForAll(VehicleController vehicle, int amount)
	{
	    if (!vehicle.PhotonView.isMine)
	        return;

        if (!vehicle.IsBot)
            vehicle.Player.SetCustomProperties(new Hashtable { { "kl", vehicle.Statistics.kills + amount } });
        else
        {
            Hashtable properties = new Hashtable { { vehicle.KeyForKills, (int)vehicle.Statistics.kills + amount } };
            PhotonNetwork.room.SetCustomProperties(properties);
        }
	}

    private static void ScoreFreeForAll(VehicleController vehicle, int amount)
    {
        if (!vehicle.PhotonView.isMine)
            return;
        if (!vehicle.IsBot)
            vehicle.Player.SetCustomProperties(new Hashtable {{"sc", vehicle.Statistics.score + amount}});
        else
        {
            Hashtable properties = new Hashtable { {vehicle.KeyForScore, (int)vehicle.Statistics.score + amount } };
            PhotonNetwork.room.SetCustomProperties(properties);
        }
    }
    #endregion
}
