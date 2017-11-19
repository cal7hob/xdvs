using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class StaticContainer
{
    public const ShellType DEFAULT_SHELL_TYPE = ShellType.Usual;
    public static int EnemyLayerMask = MiscTools.GetLayerMask("Enemy");
    public static int ParallelWorldLayer = LayerMask.NameToLayer("ParallelWorld");
    public static int terrainLayer;


    public static ShellType DefaultShellType
    {
        get { return DEFAULT_SHELL_TYPE; }
    }



    public static bool IsFriendOfMain(VehicleController vehicle) 
    {
        if (BattleController.MyVehicle == null)
        {
            return false;
        }
        return vehicle == BattleController.MyVehicle || (BattleController.Instance.BattleMode != GameData.GameMode.Deathmatch &&
                vehicle.data.teamId == BattleController.MyVehicle.data.teamId);
    }
    /// <summary>
    /// Друзья ли владельцы Vehicl'ов в данном бою.
    /// Внимание! Если транспорт сравнивается сам с собой, возвращается FALSE.
    /// </summary>
    /// <returns></returns>
    /// 
    public static bool AreFriends(int player1, int player2)
    {
        if (player1 < 1 || player2 < 1 || player1 == player2)
        {
            return false;
        }
        return AreFriends(BattleController.allVehicles[player1], BattleController.allVehicles[player2]);
    }

    public static bool AreFriends(VehicleController player1, VehicleController player2)
    {
        if (player1 == null || player2 == null || player1 == player2)
        {
            return false;
        }

        switch (GameData.Mode)
        {
            case GameData.GameMode.Deathmatch:
                return !string.IsNullOrEmpty(player1.data.clanName) && player1.data.clanName == player2.data.clanName;
            case GameData.GameMode.Team:
                return player1.data.teamId == player2.data.teamId;
            default:
                return false;
        }
    }

    public static bool AreClanmates(VehicleController player1, VehicleController player2)
    {
        if (player1 == null || player2 == null || player1 == player2)
        {
            return false;
        }

        string clan1 = player1.data.clanName;
        string clan2 = player2.data.clanName;

        return !string.IsNullOrEmpty(clan1) && clan1 == clan2;
    }


 /*   public static void TakeExperienceBonus(VehicleController vehicle, int amount)
    {
        ScoreCounter.ScoreInto(vehicle, amount);
        Dispatcher.Send(EventId.ExperienceAcquired, new EventInfo_I(amount));
    }*/

    public static void TakeGoldBonus(int amount)
    {
        Dispatcher.Send(EventId.GoldAcquired, new EventInfo_I(amount));
    }

    public static void TakeSilverBonus(int amount)
    {
        Dispatcher.Send(EventId.SilverAcquired, new EventInfo_I(amount));
    }

    public static void TakeFuelBonus()
    {
        Dispatcher.Send(EventId.FuelAcquired, new EventInfo_I(1));
    }

    
  /*  public void TakeGoldRushBonus(VehicleController vehicle)
    {
        if (!GameData.IsGoldRushEnabled)
        {
            return;
        }

        if (BattleController.TimeRemaining < GameData.GoldRushMinTime)
        {
            BattleController.Instance.ProlongGameForFree(GameData.GoldRushMinTime - BattleController.TimeRemaining);
            TopPanelValues.ShowCriticalTime(false);
        }

        GoldRush.AwardPermission = true;

        PhotonNetwork.room.SetCustomProperties(new Hashtable {{"goldLeader", vehicle.data.playerId}});

        vehicle.MakeRespawn(false, false, false);
    } // TODO: Если включат зотолую лихорадку, то проверить, как работает для ботов
*/
}