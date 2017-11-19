using UnityEngine;
using System.Collections.Generic;
using XD;

public class VoiceDispatcher : MonoBehaviour, ISender, ISubscriber
{
    #region ISubscriber
    public void Reaction(Message message, params object[] parameters)
    {
        switch (message)
        {
            case Message.ChatMessage:
            case Message.Button:
                string label = parameters.Get<string>();
                switch (parameters.Get<ButtonKey>())
                {
                    case ButtonKey.Chat:
                        if (!label.IsNullOrEmpty())
                        {
                            Event(Message.VoiceRequest, parameters.Get<VoiceEventKey>());
                            Debug.LogWarningFormat("'{0}' reaction on '{1}'; Parameters: {2}", name, message, parameters.ToFullString());
                        }
                        break;                
                }
                break;
        }
    }
    #endregion

    #region ISender
    public string Description
    {
        get
        {
            return "[VoiceDispatcher] " + name;
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

    private void Awake()
    {
        StaticType.Input.AddSubscriber(this);

        AddSubscriber(GetComponent<IVoiceManager>());

        Dispatcher.Subscribe(EventId.SACLOSLaunchRequired, OnSACLOSLaunchRequired);
        Dispatcher.Subscribe(EventId.MissileThreat, OnMissileThreat);
        Dispatcher.Subscribe(EventId.IRCMLaunchRequired, OnIRCMLaunchRequired);
        Dispatcher.Subscribe(EventId.VehicleCrashing, OnVehicleCrashing);
        Dispatcher.Subscribe(EventId.ShellHit, OnShellHit);
        Dispatcher.Subscribe(EventId.HelicopterKilled, OnHelicopterKilled);
        Dispatcher.Subscribe(EventId.ItemTaken, OnItemTaken);
        Dispatcher.Subscribe(EventId.TankOutOfTime, OnVehicleOutOfTime);
        Dispatcher.Subscribe(EventId.VoiceRequired, OnHangarVoiceRequired);
        Dispatcher.Subscribe(EventId.ModuleReceived, OnModuleDelivered);
        Dispatcher.Subscribe(EventId.VehicleInstalled, OnVehicleInstalled);
        Dispatcher.Subscribe(EventId.GameUpdateRequired, OnGameUpdateRequired);
        Dispatcher.Subscribe(EventId.QuestCompleted, OnQuestCompleted);
        Dispatcher.Subscribe(EventId.TargetAimed, OnTargetAimed);
        Dispatcher.Subscribe(EventId.TankTakesDamage, OnTankTakesDamage);
        Dispatcher.Subscribe(EventId.WeaponOverheated, OnWeaponOverheated);
        Dispatcher.Subscribe(EventId.TankKilled, OnTankKilled);
        Dispatcher.Subscribe(EventId.PatternExpired, OnPatternExpired);
        Dispatcher.Subscribe(EventId.DecalExpired, OnDecalExpired);
        Dispatcher.Subscribe(EventId.TankShotMissed, OnTankShotMissed);

        DontDestroyOnLoad(this);
    }

    private void OnDestroy()
    {
        StaticType.Input.RemoveSubscriber(this);

        Dispatcher.Unsubscribe(EventId.SACLOSLaunchRequired, OnSACLOSLaunchRequired);
        Dispatcher.Unsubscribe(EventId.MissileThreat, OnMissileThreat);
        Dispatcher.Unsubscribe(EventId.IRCMLaunchRequired, OnIRCMLaunchRequired);
        Dispatcher.Unsubscribe(EventId.VehicleCrashing, OnVehicleCrashing);
        Dispatcher.Unsubscribe(EventId.ShellHit, OnShellHit);
        Dispatcher.Unsubscribe(EventId.HelicopterKilled, OnHelicopterKilled);
        Dispatcher.Unsubscribe(EventId.ItemTaken, OnItemTaken);
        Dispatcher.Unsubscribe(EventId.TankOutOfTime, OnVehicleOutOfTime);
        Dispatcher.Unsubscribe(EventId.VoiceRequired, OnHangarVoiceRequired);
        Dispatcher.Unsubscribe(EventId.ModuleReceived, OnModuleDelivered);
        Dispatcher.Unsubscribe(EventId.VehicleInstalled, OnVehicleInstalled);
        Dispatcher.Unsubscribe(EventId.GameUpdateRequired, OnGameUpdateRequired);
        Dispatcher.Unsubscribe(EventId.QuestCompleted, OnQuestCompleted);
        Dispatcher.Unsubscribe(EventId.TargetAimed, OnTargetAimed);
        Dispatcher.Unsubscribe(EventId.TankTakesDamage, OnTankTakesDamage);
        Dispatcher.Unsubscribe(EventId.WeaponOverheated, OnWeaponOverheated);
        Dispatcher.Unsubscribe(EventId.TankKilled, OnTankKilled);
        Dispatcher.Unsubscribe(EventId.PatternExpired, OnPatternExpired);
        Dispatcher.Unsubscribe(EventId.DecalExpired, OnDecalExpired);
        Dispatcher.Unsubscribe(EventId.TankShotMissed, OnTankShotMissed);
    }

    private void OnSACLOSLaunchRequired(EventId id, EventInfo ei)
    {
        if (((EventInfo_B)ei).bool1)
        {
            Event(Message.VoiceRequest, VoiceEventKey.SACLOSLaunchRequired);            
        }
    }

    private void OnMissileThreat(EventId id, EventInfo ei)
    {
        if (((EventInfo_B)ei).bool1)
        {
            Event(Message.VoiceRequest, VoiceEventKey.MissileThreat);
        }
    }

    private void OnIRCMLaunchRequired(EventId id, EventInfo ei)
    {
        if (((EventInfo_B)ei).bool1)
        {
            Event(Message.VoiceRequest, VoiceEventKey.IRCMLaunchRequired);
        }
    }

    private void OnVehicleCrashing(EventId id, EventInfo ei)
    {
        if (((EventInfo_I)ei).int1 == StaticType.BattleController.Instance<IBattleController>().MyPlayerId)
        {
            Event(Message.VoiceRequest, VoiceEventKey.Crashing);
        }
    }

    private void OnShellHit(EventId id, EventInfo ei)
    {
        if (((EventInfo_IIIIV)ei).int1 == StaticType.BattleController.Instance<IBattleController>().MyPlayerId)
        {
            Event(Message.VoiceRequest, VoiceEventKey.ShellHit);
        }
    }

    private void OnHelicopterKilled(EventId id, EventInfo ei)
    {
        if (((EventInfo_IIV)ei).int2 == StaticType.BattleController.Instance<IBattleController>().MyPlayerId)
        {
            Event(Message.VoiceRequest, VoiceEventKey.EnemyKilled);
        }
    }

    private void OnItemTaken(EventId id, EventInfo ei)
    {
        EventInfo_III info = (EventInfo_III)ei;

        int playerId = info.int3;
        BonusItem.BonusType bonusType = (BonusItem.BonusType)info.int1;

        if (playerId != StaticType.BattleController.Instance<IBattleController>().MyPlayerId)
        {
            return;
        }

        switch (bonusType)
        {
            case BonusItem.BonusType.Health:
                Event(Message.VoiceRequest, VoiceEventKey.PickedUpBonusArmor);
                break;

            case BonusItem.BonusType.Attack:
                Event(Message.VoiceRequest, VoiceEventKey.PickedUpBonusDamage);
                break;

            case BonusItem.BonusType.Reload:
                Event(Message.VoiceRequest, VoiceEventKey.PickedUpBonusROF);
                break;

            case BonusItem.BonusType.Fuel:
                Event(Message.VoiceRequest, VoiceEventKey.PickedUpBonusFuel);
                break;

            case BonusItem.BonusType.Gold:
                Event(Message.VoiceRequest, VoiceEventKey.PickedUpBonusGold);
                break;

            case BonusItem.BonusType.Silver:
                Event(Message.VoiceRequest, VoiceEventKey.PickedUpBonusSilver);
                break;

            case BonusItem.BonusType.Experience:
                Event(Message.VoiceRequest, VoiceEventKey.PickedUpBonusExperience);
                break;
        }
    }

    private void OnVehicleOutOfTime(EventId id, EventInfo ei)
    {
        if (((EventInfo_I)ei).int1 == StaticType.BattleController.Instance<IBattleController>().MyPlayerId)
        {
            Event(Message.VoiceRequest, VoiceEventKey.BattleEndTimeouted);
        }
    }

    private void OnQuestCompleted(EventId id, EventInfo ei)
    {
        Event(Message.VoiceRequest, VoiceEventKey.QuestCompleted);
    }

    private void OnHangarVoiceRequired(EventId id, EventInfo ei)
    {
        var voiceEventId = ((EventInfo_I)ei).int1;
        Event(Message.VoiceRequest, (VoiceEventKey)voiceEventId);
    }

    private void OnModuleDelivered(EventId id, EventInfo ei)
    {
        Event(Message.VoiceRequest, VoiceEventKey.ModuleDelivered);
    }

    private void OnVehicleInstalled(EventId id, EventInfo ei)
    {
        Event(Message.VoiceRequest, VoiceEventKey.VehicleInstall);
    }

    private void OnGameUpdateRequired(EventId id, EventInfo ei)
    {
        Event(Message.VoiceRequest, VoiceEventKey.UpdateGameRequired);
    }

    private void OnTargetAimed(EventId id, EventInfo ei)
    {
        EventInfo_IIB info = ei as EventInfo_IIB;
        if (info.int1 != StaticType.BattleController.Instance<IBattleController>().MyPlayerId)
        {
            return;
        }

        if (info.bool1 && XD.StaticContainer.BattleController.CurrentUnit.GetWeapon(GunShellInfo.ShellType.Usual).IsReady)
        {
            Event(Message.VoiceRequest, VoiceEventKey.ShotRequired);
        }
    }

    private void OnTankTakesDamage(EventId id, EventInfo ei)
    {
        if ((int)((EventInfo_U)ei)[2] == StaticType.BattleController.Instance<IBattleController>().MyPlayerId)
        {
            Event(Message.VoiceRequest, VoiceEventKey.GoodShot);
        }
    }

    private void OnWeaponOverheated(EventId id, EventInfo ei)
    {
        Event(Message.VoiceRequest, VoiceEventKey.WeaponOverheated);
    }

    private void OnTankKilled(EventId id, EventInfo ei)
    {
        if (((EventInfo_II)ei).int1 == StaticType.BattleController.Instance<IBattleController>().MyPlayerId &&
            GameData.IsGame(Game.Armada2))
        {
            Event(Message.VoiceRequest, VoiceEventKey.MyTankDestroyed);
        }
    }

    private void OnPatternExpired(EventId id, EventInfo ei)
    {
        Event(Message.VoiceRequest, VoiceEventKey.PatternConsumed);
    }

    private void OnDecalExpired(EventId id, EventInfo ei)
    {
        Event(Message.VoiceRequest, VoiceEventKey.DecalConsumed);
    }

    private void OnTankShotMissed(EventId id, EventInfo ei)
    {
        if (((EventInfo_I)ei).int1 == StaticType.BattleController.Instance<IBattleController>().MyPlayerId)
        {
            Event(Message.VoiceRequest, VoiceEventKey.MissedShot);
        }
    }
}
