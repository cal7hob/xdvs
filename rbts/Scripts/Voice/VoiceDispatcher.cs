using System;
using UnityEngine;

public class VoiceDispatcher : MonoBehaviour
{
    void Awake()
    {
        Messenger.Subscribe(EventId.SACLOSLaunchRequired, OnSACLOSLaunchRequired);
        Messenger.Subscribe(EventId.MissileThreat, OnMissileThreat);
        Messenger.Subscribe(EventId.IRCMLaunchRequired, OnIRCMLaunchRequired);
        Messenger.Subscribe(EventId.VehicleCrashing, OnVehicleCrashing);
        Messenger.Subscribe(EventId.ShellHit, OnShellHit);
        Messenger.Subscribe(EventId.HelicopterKilled, OnHelicopterKilled);
        Messenger.Subscribe(EventId.ItemTaken, OnItemTaken);
        Messenger.Subscribe(EventId.TankOutOfTime, OnVehicleOutOfTime);
        Messenger.Subscribe(EventId.VoiceRequired, OnHangarVoiceRequired);
        Messenger.Subscribe(EventId.ModuleReceived, OnModuleDelivered);
        Messenger.Subscribe(EventId.VehicleInstalled, OnVehicleInstalled);
        Messenger.Subscribe(EventId.GameUpdateRequired, OnGameUpdateRequired);
        Messenger.Subscribe(EventId.QuestCompleted, OnQuestCompleted);
        Messenger.Subscribe(EventId.TargetAimed, OnTargetAimed);
        Messenger.Subscribe(EventId.VehicleTakesDamage, OnTankTakesDamage);
        Messenger.Subscribe(EventId.WeaponOverheated, OnWeaponOverheated);
        Messenger.Subscribe(EventId.VehicleKilled, OnTankKilled);
        Messenger.Subscribe(EventId.PatternExpired, OnPatternExpired);
        Messenger.Subscribe(EventId.DecalExpired, OnDecalExpired);
        Messenger.Subscribe(EventId.TankShotMissed, OnTankShotMissed);
        Messenger.Subscribe(EventId.BattleChatCommand, OnBattleChatCommand);
    }

    void OnDestroy()
    {
        Messenger.Unsubscribe(EventId.SACLOSLaunchRequired, OnSACLOSLaunchRequired);
        Messenger.Unsubscribe(EventId.MissileThreat, OnMissileThreat);
        Messenger.Unsubscribe(EventId.IRCMLaunchRequired, OnIRCMLaunchRequired);
        Messenger.Unsubscribe(EventId.VehicleCrashing, OnVehicleCrashing);
        Messenger.Unsubscribe(EventId.ShellHit, OnShellHit);
        Messenger.Unsubscribe(EventId.HelicopterKilled, OnHelicopterKilled);
        Messenger.Unsubscribe(EventId.ItemTaken, OnItemTaken);
        Messenger.Unsubscribe(EventId.TankOutOfTime, OnVehicleOutOfTime);
        Messenger.Unsubscribe(EventId.VoiceRequired, OnHangarVoiceRequired);
        Messenger.Unsubscribe(EventId.ModuleReceived, OnModuleDelivered);
        Messenger.Unsubscribe(EventId.VehicleInstalled, OnVehicleInstalled);
        Messenger.Unsubscribe(EventId.GameUpdateRequired, OnGameUpdateRequired);
        Messenger.Unsubscribe(EventId.QuestCompleted, OnQuestCompleted);
        Messenger.Unsubscribe(EventId.TargetAimed, OnTargetAimed);
        Messenger.Unsubscribe(EventId.VehicleTakesDamage, OnTankTakesDamage);
        Messenger.Unsubscribe(EventId.WeaponOverheated, OnWeaponOverheated);
        Messenger.Unsubscribe(EventId.VehicleKilled, OnTankKilled);
        Messenger.Unsubscribe(EventId.PatternExpired, OnPatternExpired);
        Messenger.Unsubscribe(EventId.DecalExpired, OnDecalExpired);
        Messenger.Unsubscribe(EventId.TankShotMissed, OnTankShotMissed);
        Messenger.Unsubscribe(EventId.BattleChatCommand, OnBattleChatCommand);
    }

    private void OnSACLOSLaunchRequired(EventId id, EventInfo ei)
    {
        if (((EventInfo_B)ei).bool1)
            VoiceManager.Play(VoiceEventKey.SACLOSLaunchRequired);
    }

    private void OnMissileThreat(EventId id, EventInfo ei)
    {
        if (((EventInfo_B)ei).bool1)
            VoiceManager.Play(VoiceEventKey.MissileThreat);
    }

    private void OnIRCMLaunchRequired(EventId id, EventInfo ei)
    {
        if (((EventInfo_B)ei).bool1)
            VoiceManager.Play(VoiceEventKey.IRCMLaunchRequired);
    }

    private void OnVehicleCrashing(EventId id, EventInfo ei)
    {
        if (((EventInfo_I)ei).int1 == BattleController.MyPlayerId)
            VoiceManager.Play(VoiceEventKey.Crashing);
    }

    private void OnShellHit(EventId id, EventInfo ei)
    {
        if (((EventInfo_IIIIV)ei).int1 == BattleController.MyPlayerId)
            VoiceManager.Play(VoiceEventKey.ShellHit);
    }

    private void OnHelicopterKilled(EventId id, EventInfo ei)
    {
        if (((EventInfo_IIV)ei).int2 == BattleController.MyPlayerId)
            VoiceManager.Play(VoiceEventKey.EnemyKilled);
    }

    private void OnItemTaken(EventId id, EventInfo ei)
    {
        EventInfo_III info = (EventInfo_III)ei;

        int playerId = info.int3;
        BonusItem.BonusType bonusType = (BonusItem.BonusType)info.int1;

        if (playerId != BattleController.MyPlayerId)
            return;

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
            VoiceManager.Play(VoiceEventKey.BattleEndTimeouted);
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
            return;

        if (info.bool1 && BattleController.MyVehicle.WeaponController.IsReady)
            VoiceManager.Play(VoiceEventKey.ShotRequired);
    }

    private void OnTankTakesDamage(EventId id, EventInfo ei)
    {
        if ((int)((EventInfo_U)ei)[2] == BattleController.MyPlayerId)
            VoiceManager.Play(VoiceEventKey.GoodShot);
    }

    private void OnWeaponOverheated(EventId id, EventInfo ei)
    {
        VoiceManager.Play(VoiceEventKey.WeaponOverheated);
    }

    private void OnTankKilled(EventId id, EventInfo ei)
    {
        if (((EventInfo_II)ei).int1 == BattleController.MyPlayerId &&
            GameData.IsGame(Game.IronTanks | Game.FutureTanks | Game.ToonWars | Game.Armada | Game.SpaceJet))
        {
            VoiceManager.Play(VoiceEventKey.MyTankDestroyed);
        }
    }

    private void OnPatternExpired(EventId id, EventInfo ei)
    {
        VoiceManager.Play(VoiceEventKey.PatternConsumed);
    }

    private void OnDecalExpired(EventId id, EventInfo ei)
    {
        VoiceManager.Play(VoiceEventKey.DecalConsumed);
    }

    private void OnTankShotMissed(EventId id, EventInfo ei)
    {
        if (((EventInfo_I)ei).int1 == BattleController.MyPlayerId)
            VoiceManager.Play(VoiceEventKey.MissedShot);
    }

    private void OnBattleChatCommand(EventId id, EventInfo ei)
    {
        EventInfo_U info = (EventInfo_U)ei;

        int playerId = Convert.ToInt32(info[0]);
        BattleChatCommands.Id chatMessage = (BattleChatCommands.Id)Convert.ToInt32(info[1]);

        bool isMyMessage = playerId == BattleController.MyPlayerId;

        VoiceEventKey voiceEventKey;

        switch (chatMessage)
        {
            case BattleChatCommands.Id.Attack:
                voiceEventKey = VoiceEventKey.ChatMyAttack;
                break;
            case BattleChatCommands.Id.Affirmative:
                voiceEventKey = VoiceEventKey.ChatMyAffirmative;
                break;
            case BattleChatCommands.Id.HelpMe:
                voiceEventKey = VoiceEventKey.ChatMyHelpMe;
                break;
            case BattleChatCommands.Id.NotInterfere:
                voiceEventKey = VoiceEventKey.ChatMyNotInterfere;
                break;
            case BattleChatCommands.Id.Negative:
                voiceEventKey = VoiceEventKey.ChatMyNegative;
                break;
            case BattleChatCommands.Id.Thanks:
                voiceEventKey = VoiceEventKey.ChatMyThanks;
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }

        VoiceManager.Play(
            eventKey:   voiceEventKey,
            volume:     isMyMessage ? 1.0f : 0.75f); // Пока так, вместо запечённого эффекта рации.
    }
}
