using UnityEngine;

public class VoiceDispatcher : MonoBehaviour
{
    void Awake()
    {
         
        Dispatcher.Subscribe(EventId.SACLOSLaunchRequired, OnSACLOSLaunchRequired);
        Dispatcher.Subscribe(EventId.MissileThreat, OnMissileThreat);
        Dispatcher.Subscribe(EventId.IRCMLaunchRequired, OnIRCMLaunchRequired);
        Dispatcher.Subscribe(EventId.VehicleCrashing, OnVehicleCrashing);
        Dispatcher.Subscribe(EventId.ShellHit, OnShellHit);
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
        Dispatcher.Subscribe(EventId.BattleChatCommand, OnBattleChatCommand);
    }

    void OnDestroy()
    {
        Dispatcher.Unsubscribe(EventId.SACLOSLaunchRequired, OnSACLOSLaunchRequired);
        Dispatcher.Unsubscribe(EventId.MissileThreat, OnMissileThreat);
        Dispatcher.Unsubscribe(EventId.IRCMLaunchRequired, OnIRCMLaunchRequired);
        Dispatcher.Unsubscribe(EventId.VehicleCrashing, OnVehicleCrashing);
        Dispatcher.Unsubscribe(EventId.ShellHit, OnShellHit);
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
        Dispatcher.Unsubscribe(EventId.BattleChatCommand, OnBattleChatCommand);
    }

    private void OnBattleChatCommand(EventId id, EventInfo info)
    {
        EventInfo_U eventData = (EventInfo_U)info;
        int soundId_ = System.Convert.ToInt32(eventData[1]);
        VoiceManager.Play(BattleChatCommands.chatVoiceDict[soundId_]);
    }

    private void OnSACLOSLaunchRequired(EventId id, EventInfo ei)
    {
        if (((EventInfo_B)ei).bool1)
        {
            VoiceManager.Play(VoiceEventKey.SACLOSLaunchRequired);
        }
    }

    private void OnMissileThreat(EventId id, EventInfo ei)
    {
        if (((EventInfo_B)ei).bool1)
        {
            VoiceManager.Play(VoiceEventKey.MissileThreat);
        }
    }

    private void OnIRCMLaunchRequired(EventId id, EventInfo ei)
    {
        if (((EventInfo_B)ei).bool1)
        {
            VoiceManager.Play(VoiceEventKey.IRCMLaunchRequired);
        }
    }

    private void OnVehicleCrashing(EventId id, EventInfo ei)
    {
        if (((EventInfo_I)ei).int1 == BattleController.MyPlayerId)
        {
            VoiceManager.Play(VoiceEventKey.Crashing);
        }
    }

    private void OnShellHit(EventId id, EventInfo ei)
    {
        if (((EventInfo_IIIIV)ei).int1 == BattleController.MyPlayerId)
        {
            VoiceManager.Play(VoiceEventKey.ShellHit);
        }
    }

    private void OnHelicopterKilled(EventId id, EventInfo ei)
    {
        if (((EventInfo_IIV)ei).int2 == BattleController.MyPlayerId)
        {
            VoiceManager.Play(VoiceEventKey.EnemyKilled);
        }
    }

    private void OnItemTaken(EventId id, EventInfo ei)
    {
        EventInfo_III info = (EventInfo_III)ei;

        int playerId = info.int3;
        BonusItem.BonusType bonusType = (BonusItem.BonusType)info.int1;

        if (playerId != BattleController.MyPlayerId)
        {
            return;
        }

        switch (bonusType)
        {
            case BonusItem.BonusType.Health:
                VoiceManager.Play(VoiceEventKey.PickedUpBonusArmor);
                break;

            case BonusItem.BonusType.Attack:
                VoiceManager.Play(VoiceEventKey.PickedUpBonusDamage);
                break;

            case BonusItem.BonusType.Reload:
                VoiceManager.Play(VoiceEventKey.PickedUpBonusROF);
                break;

            case BonusItem.BonusType.Fuel:
                VoiceManager.Play(VoiceEventKey.PickedUpBonusFuel);
                break;

            case BonusItem.BonusType.Gold:
                VoiceManager.Play(VoiceEventKey.PickedUpBonusGold);
                break;

            case BonusItem.BonusType.Silver:
                VoiceManager.Play(VoiceEventKey.PickedUpBonusSilver);
                break;

            case BonusItem.BonusType.Experience:
                VoiceManager.Play(VoiceEventKey.PickedUpBonusExperience);
                break;
        }
    }

    private void OnVehicleOutOfTime(EventId id, EventInfo ei)
    {
        if (((EventInfo_I)ei).int1 == BattleController.MyPlayerId)
        {
            VoiceManager.Play(VoiceEventKey.BattleEndTimeouted);
        }
    }

    private void OnQuestCompleted(EventId id, EventInfo ei)
    {
        VoiceManager.Play(VoiceEventKey.QuestCompleted);
    }

    private void OnHangarVoiceRequired(EventId id, EventInfo ei)
    {
        var voiceEventId = ((EventInfo_I)ei).int1;
        VoiceManager.Play((VoiceEventKey)voiceEventId);
    }

    private void OnModuleDelivered(EventId id, EventInfo ei)
    {
        if (!VoiceManager.UseDelay)
        {
            PlayModuleDelivered();
            return;
        }
        Invoke("PlayModuleDelivered", VoiceManager.DelayBefore);
    }

    private void PlayModuleDelivered() 
    {
        VoiceManager.Play(VoiceEventKey.ModuleDelivered);
    }


    private void OnVehicleInstalled(EventId id, EventInfo ei)
    {
        VoiceManager.Play(VoiceEventKey.VehicleInstall);
    }

    private void OnGameUpdateRequired(EventId id, EventInfo ei)
    {
        VoiceManager.Play(VoiceEventKey.UpdateGameRequired);
    }

    private void OnTargetAimed(EventId id, EventInfo ei)
    {
        EventInfo_IIB info = ei as EventInfo_IIB;
        if (info.int1 != BattleController.MyPlayerId)
        {
            return;
        }

        if (info.bool1 && BattleController.MyVehicle.GetWeapon(ShellType.Usual).IsReady)
        {
            VoiceManager.Play(VoiceEventKey.ShotRequired);
        }
    }

    private void OnTankTakesDamage(EventId id, EventInfo ei)
    {
        EventInfo_U info = (EventInfo_U)ei;
        
       if ((int)(info)[2] == BattleController.MyPlayerId)
        {
            //убил
            if (BattleController.allVehicles[(int)(info)[0]].Armor <= 0) 
            {
                return;
            }
            VoiceManager.Play(VoiceEventKey.GoodShot);
        }
    }

    private void OnWeaponOverheated(EventId id, EventInfo ei)
    {
        VoiceManager.Play(VoiceEventKey.WeaponOverheated);
    }

    private void OnTankKilled(EventId id, EventInfo ei)
    {
        EventInfo_II info = (EventInfo_II)ei;
        if (info.int1 == BattleController.MyPlayerId)
        {
            VoiceManager.Play(VoiceEventKey.MyTankDestroyed);
            return;
        }

        //попал по своему или самому себе
        if (BattleController.allVehicles[info.int1].Statistics.teamId == BattleController.allVehicles[info.int2].Statistics.teamId)
        {
            return;
        }

        if (((EventInfo_II)ei).int2 == BattleController.MyPlayerId) 
        {
            VoiceManager.Play(VoiceEventKey.EnemyKilled);
        }
    }

    private void OnPatternExpired(EventId id, EventInfo ei)
    {
      /*  if (!VoiceManager.UseDelay) 
        {
            Invoke("PlayPatternExpired", VoiceManager.DelayBefore);
            return;
        }
        PlayPatternExpired();
    }
    private void PlayPatternExpired()
    {*/
        VoiceManager.Play(VoiceEventKey.PatternConsumed);
    }

    private void OnDecalExpired(EventId id, EventInfo ei)
    {
       /* if (!VoiceManager.UseDelay)
        {
            Invoke("PlayDecalExpired", VoiceManager.DelayBefore);
            return;
        }
        PlayDecalExpired();
    }
    private void PlayDecalExpired()
    {*/
        VoiceManager.Play(VoiceEventKey.DecalConsumed);
    }

    private void OnTankShotMissed(EventId id, EventInfo ei)
    {
        if (((EventInfo_I)ei).int1 == BattleController.MyPlayerId)
        {
            VoiceManager.Play(VoiceEventKey.MissedShot);
        }
    }
}
