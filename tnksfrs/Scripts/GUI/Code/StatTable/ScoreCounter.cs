using UnityEngine;
using Hashtable = ExitGames.Client.Photon.Hashtable;
using XD;

public class ScoreCounter : MonoBehaviour
{
    public static int FriendTeamScore { get; private set; }
    public static int EnemyTeamScore { get; private set; }

	private static ScoreCounter instance;

	private void Awake()
	{
        FriendTeamScore = 0;
		EnemyTeamScore = 0;
		instance = this;
	}

	private void OnDestroy()
	{
        instance = null;
    }

    #region PUBLIC SECTION
    public static void DeathInto(VehicleController vehicle, int amount = 1)
	{
        if (!BattleController.CheckPlayersCount())
        {
            return;
        }

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
        {
            return;
        }
        //Debug.Log(string.Format("ScoreCounter.KillInto: '{0}', amount: '{1}'", vehicle.name, amount).RichString("color:green"), vehicle);
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
        if (!XD.StaticContainer.BattleController.CurrentUnit || !BattleController.CheckPlayersCount())
        {
            // Не добавлять командный счет, пока неизвестно, за кого ты играешь
            return;
        }

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
        if (GameData.Mode != GameData.GameMode.Team || !XD.StaticContainer.BattleController.CurrentUnit)
        {
            return;
        }

        bool friendScore = teamId == XD.StaticContainer.BattleController.CurrentUnit.data.teamId;
        if (friendScore)
        {
            FriendTeamScore += scoreDelta;
        }
        else
        {
            EnemyTeamScore += scoreDelta;
        }

        Dispatcher.Send(EventId.TeamScoreChanged, new EventInfo_SimpleEvent());
    }
#endregion

    #region PRIVATE SECTION
	
	private static void DeathFreeForAll(VehicleController vehicle, int amount)
	{
        if (!vehicle.PhotonView.isMine)
        {
            return;
        }

        if (!vehicle.IsBot)
        {
            vehicle.Player.SetCustomProperties(new Hashtable { { "dt", vehicle.Statistics.Stats[StatisticParameter.Deaths] + amount } });
        }
        else
        {
            Hashtable properties = new Hashtable { { vehicle.KeyForBotDeaths, vehicle.Statistics.Stats[StatisticParameter.Deaths] + amount } };
            PhotonNetwork.room.SetCustomProperties(properties);
        }
	}

	private static void KillFreeForAll(VehicleController vehicle, int amount)
	{
        if (!vehicle.PhotonView.isMine)
        {
            return;
        }

        if (!vehicle.IsBot)
        {
            vehicle.Player.SetCustomProperties(new Hashtable { { "kl", vehicle.Statistics.Stats[StatisticParameter.Kills] + amount } });
        }
        else
        {
            Hashtable properties = new Hashtable { { vehicle.KeyForBotKills, vehicle.Statistics.Stats[StatisticParameter.Kills] + amount } };
            PhotonNetwork.room.SetCustomProperties(properties);
        }
	}

    private static void ScoreFreeForAll(VehicleController vehicle, int amount)
    {
        if (!vehicle.PhotonView.isMine)
        {
            return;
        }

        if (!vehicle.IsBot)
        {
            vehicle.Player.SetCustomProperties(new Hashtable {{"sc", vehicle.Statistics.Stats[StatisticParameter.Experience] + amount}});
        }
        else
        {
            Hashtable properties = new Hashtable { {vehicle.KeyForBotScore, vehicle.Statistics.Stats[StatisticParameter.Experience] + amount } };
            PhotonNetwork.room.SetCustomProperties(properties);
        }
    }
    #endregion
}
