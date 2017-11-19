using UnityEngine;
using System;
using System.Collections.Generic;
using CodeStage.AntiCheat.ObscuredTypes;

public class TankIndicators : MonoBehaviour
{
	public TankIndicator tankIndicatorPrefab;

    private static TankIndicators instance;
	private static Dictionary<int, TankIndicator> indicators;
	private static ObscuredInt goldRushAward = 0;
	private static ObscuredInt goldLeaderId = 0;

    private int chatMessagePhotonPlayerId = 0;
    private int chatMessageId = 0;
    private int usedConsumablePhotonPlayerId = 0;
    private int usedConsumableId = 0;//consumable id

    void Awake()
	{
		indicators = new Dictionary<int, TankIndicator>(10);
		instance = this;

		Dispatcher.Subscribe(EventId.MainTankAppeared, OnMainTankAppeared);
		Dispatcher.Subscribe(EventId.TankTakesDamage, OnTankTakesDamage);
		Dispatcher.Subscribe(EventId.TankHealthChanged, OnTankHealthChange);
        Dispatcher.Subscribe(EventId.HideEnemy, OnHideEnemy);
        Dispatcher.Subscribe(EventId.ShowEnemy, OnShowEnemy);
        if (tankIndicatorPrefab.chatMessageWrapper)
        {
            Dispatcher.Subscribe(EventId.BattleChatCommand, OnBattleChatCommand);
        }
        if (tankIndicatorPrefab.usedConsumableWrapper)
        {
            Dispatcher.Subscribe(EventId.ConsumableUsed, OnUsingConsumable);
        }
    }

	void OnDestroy()
	{
		goldLeaderId = 0;
		goldRushAward = 0;
		indicators = null;
		instance = null;

		Dispatcher.Unsubscribe(EventId.MainTankAppeared, OnMainTankAppeared);
		Dispatcher.Unsubscribe(EventId.TankTakesDamage, OnTankTakesDamage);
		Dispatcher.Unsubscribe(EventId.TankHealthChanged, OnTankHealthChange);
        Dispatcher.Unsubscribe(EventId.BattleChatCommand, OnBattleChatCommand);
        Dispatcher.Unsubscribe(EventId.ConsumableUsed, OnUsingConsumable);
        Dispatcher.Unsubscribe(EventId.HideEnemy, OnHideEnemy);
        Dispatcher.Unsubscribe(EventId.ShowEnemy, OnShowEnemy);
    }


	public static TankIndicator GetIndicator(int playerId)
	{
		TankIndicator ind;
		indicators.TryGetValue(playerId, out ind);
		return ind;
	}

	public static TankIndicator AddIndicator(VehicleController tank)
	{
        TankIndicator newIndicator = Instantiate(instance.tankIndicatorPrefab) as TankIndicator;
        newIndicator.Vehicle = tank;
        newIndicator.name = tank.data.playerName;
        newIndicator.transform.parent = instance.transform;
        newIndicator.transform.localPosition = Vector3.zero;
        indicators.Add(tank.data.playerId, newIndicator);
        newIndicator.progressBar.Percentage = 1;
        newIndicator.Hidden = false;
        return newIndicator;
	}

	public static void RemoveIndicator(VehicleController tank)
	{
		RemoveIndicator(tank.data.playerId);
	}

	public static void RemoveIndicator(int playerId)
	{
        if (!instance)
        {
            return;
        }

        if (!indicators.ContainsKey(playerId))
        {
            return;
        }

		Destroy(indicators[playerId].gameObject);
		indicators.Remove(playerId);
	}

	public static void SetGoldRushAward(int award)
	{
		goldRushAward = award;

        if (indicators.ContainsKey(goldLeaderId))
        {
            indicators[goldLeaderId].Award = award;
        }
	}

	public static void SetGoldRushLeader(int newLeaderId)
	{
        if (indicators.ContainsKey(goldLeaderId))
        {
            indicators[goldLeaderId].Award = 0;
        }

		goldLeaderId = newLeaderId;

        if (indicators.ContainsKey(newLeaderId))
        {
            indicators[newLeaderId].Award = goldRushAward;
        }
	}

	private void OnMainTankAppeared(EventId id, EventInfo ei)
	{
        SetGoldRushAward(goldRushAward);
		SetGoldRushLeader(goldLeaderId);
	}

	private void OnTankTakesDamage(EventId id, EventInfo ei)
	{
        EventInfo_U info = (EventInfo_U)ei;

        int victimId = (int)info[0];
		int damage = (int)info[1];
        int attackerId = (int)info[2];

        Vector3 hitPoint = (Vector3)info[4];
        if (attackerId != BattleController.MyPlayerId)
        {
            return;
        }

        TankIndicator ti = GetIndicator(victimId);
        if (ti == null)
        {
            return;
        }

        ti.AnimateLblPopupDamage(damage, hitPoint);
	}

    private TankIndicator indicator;
	private void OnHideEnemy(EventId id, EventInfo ei)
	{
        EventInfo_I info = (EventInfo_I)ei;
        if (!indicators.TryGetValue(info.int1, out indicator) || indicator == null)
        {
            return;
        }
        indicator.Hidden = true;
	}

    private void OnShowEnemy(EventId id, EventInfo ei)
    {
        EventInfo_I info = (EventInfo_I)ei;
        if (!indicators.TryGetValue(info.int1, out indicator) || indicator == null)
        {
            return;
        }
        indicator.Hidden = false;
    }

	private void OnTankHealthChange(EventId id, EventInfo ei)
	{
        EventInfo_II info = (EventInfo_II)ei;
        if (!indicators.ContainsKey(info.int1))
        {
            return;
        }
		indicators[info.int1].RedrawHealthBar(info.int2);
	}

    private void OnBattleChatCommand(EventId id, EventInfo info)
    {
        EventInfo_U eventData = (EventInfo_U)info;
        chatMessagePhotonPlayerId = Convert.ToInt32(eventData[0]);
        chatMessageId = Convert.ToInt32(eventData[1]);

        TankIndicator ti = GetIndicator(chatMessagePhotonPlayerId);
        if (!ti || !ti.Vehicle || ti.Vehicle.IsBot || ti.Hidden)//��� IsBot � ������� �����������, �� �������� ������� �� ������ :-)
        { 
            return; 
        }

        ti.SetupChatMessage(new BattleChatPanelItemData(chatMessagePhotonPlayerId, (BattleChatCommands.Id)chatMessageId, Time.realtimeSinceStartup,false,false));
    }
    private void OnUsingConsumable(EventId id, EventInfo info)
    {
        EventInfo_II eventData = (EventInfo_II)info;
        usedConsumablePhotonPlayerId = eventData.int1;
        usedConsumableId = eventData.int2;

        TankIndicator ti = GetIndicator(usedConsumablePhotonPlayerId);
        if (!ti || !ti.Vehicle || ti.Vehicle.IsBot || ti.Hidden)
        {
            return;
        }

        ti.SetupUsedConsumable(new UsedConsumableData(usedConsumablePhotonPlayerId, usedConsumableId, Time.realtimeSinceStartup));
    }
}