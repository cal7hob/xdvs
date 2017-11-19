using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using CodeStage.AntiCheat.ObscuredTypes;
using System.Linq;
using Disconnect;

using Hashtable = ExitGames.Client.Photon.Hashtable;

public class GoldRush
{
	public const int MAX_AWARD = 90;
	
	private static GoldRush instance = null;
	private static bool activated = false;
	
	private ObscuredInt currentStake = 0;
	private ObscuredInt leader = 0;
	private ObscuredBool awardPermission = false;
	
	
	public static bool AwardPermission
	{
		get { return activated && instance.awardPermission; }
		set
		{
			if (!activated)
				return;

			instance.awardPermission = value;
			TopPanelValues.SwitchGoldRush(instance.leader == BattleController.MyPlayerId && instance.awardPermission);
		}
	}

	public static int Leader
	{
		get { return activated ? (int)instance.leader : 0; }
		set
		{
			if (!activated)
				return;

			instance.leader = value;
			TankIndicators.SetGoldRushLeader(value);
			TopPanelValues.SwitchGoldRush(instance.leader == BattleController.MyPlayerId && instance.awardPermission);
		}
	}
	
	public static void Disactivate()
	{
		activated = false;

		Dispatcher.Unsubscribe(EventId.GoldRushPlayerStakes, OnPlayerStakes);
		Dispatcher.Unsubscribe(EventId.TankOutOfTime, OnVehicleOutOfTime);
	}
	
	public static void ActivateForBattle()
	{
		instance = new GoldRush();
		activated = true;
		Dispatcher.Subscribe(EventId.GoldRushPlayerStakes, OnPlayerStakes);
		Dispatcher.Subscribe(EventId.TankOutOfTime, OnVehicleOutOfTime);
	}

	public static void PlayerStake(int stake)
	{
		if (!activated)
			return;
		Dispatcher.Send(EventId.GoldRushPlayerStakes, new EventInfo_I(stake), Dispatcher.EventTargetType.ToMaster);
	}

	public static int TotalStake
	{
		get { return activated ? (int)instance.currentStake : 0; }
		set
		{
			if (!activated)
				return;
			instance.currentStake = value;
			TankIndicators.SetGoldRushAward(value);
			TopPanelValues.SetGoldRushAward(value);
			if (value == 0)
			{
				AwardPermission = false;
				Leader = 0;
			}
		}
	}

	private static void OnVehicleOutOfTime(EventId id, EventInfo ei)
	{
		if (instance.currentStake == 0)
			return;

        EventInfo_I info = (EventInfo_I)ei;

		int vehicleId = info.int1;

		if (vehicleId == BattleController.MyPlayerId && instance.leader == BattleController.MyPlayerId && instance.awardPermission)
		{
			//ProfileInfo.ReplenishBalance(new ProfileInfo.Price(instance.currentStake, ProfileInfo.PriceCurrency.Gold));

			Dispatcher.Send(EventId.GoldAcquired, new EventInfo_I(instance.currentStake));

		    Hashtable properties = new Hashtable { { "stake", 0 } };

		    PhotonNetwork.room.SetCustomProperties(properties);

			return;
		}
		
		if (BattleConnectManager.IsMasterClient && instance.leader == vehicleId && !instance.awardPermission)
			BonusDispatcher.GenerateKillBonus(BattleController.allVehicles[vehicleId], null);
	}
	
	private static void OnPlayerStakes(EventId id, EventInfo ei)
	{
		if (!BattleConnectManager.IsMasterClient || BattleController.allVehicles.Count < 2)
			return;

        EventInfo_I info = (EventInfo_I)ei;

		Hashtable properties = new Hashtable();

		if (instance.currentStake == 0)
		{
			instance.leader = FindLeader(BattleController.GameStat);

			properties.Add("goldLeader", (int)instance.leader);
			properties.Add("awardPermission", false); // Is gold rush award activated
		}

		instance.currentStake = Mathf.Clamp(instance.currentStake + info.int1, 0, MAX_AWARD);

		properties.Add("stake", (int)instance.currentStake);

		PhotonNetwork.room.SetCustomProperties(properties);
	}

	private static int FindLeader(Dictionary<int, PlayerStat> statistics)
	{
		IEnumerable statSorted =
		statistics.Select(x => x.Value)
		.OrderBy(x => x.deaths)
		.OrderByDescending(x => x.kills)
		.OrderByDescending(x => (int)x.score)
		.Where(x => x.playerId != BattleController.MyPlayerId);

		IEnumerator en = statSorted.GetEnumerator();
		if (!en.MoveNext())
			return 0;

		return (en.Current as PlayerStat).playerId;
	}
}
