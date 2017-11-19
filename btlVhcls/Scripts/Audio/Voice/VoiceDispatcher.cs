using System;
using UnityEngine;

public class VoiceDispatcher : MonoBehaviour
{

    public static bool IsInitialized { get { return m_isInitialized; } }

    private static bool m_isInitialized = false;

    void Awake()
    {
        Dispatcher.Subscribe(EventId.SACLOSLaunchRequired, OnSACLOSLaunchRequired);
        Dispatcher.Subscribe(EventId.SACLOSAimed, OnSACLOSAimed);
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
        Dispatcher.Subscribe(EventId.TankDamageApplied, OnTankDamageApplied);
        Dispatcher.Subscribe(EventId.WeaponOverheated, OnWeaponOverheated);
        Dispatcher.Subscribe(EventId.TankKilled, OnTankKilled);
        Dispatcher.Subscribe(EventId.PatternExpired, OnPatternExpired);
        Dispatcher.Subscribe(EventId.DecalExpired, OnDecalExpired);
        Dispatcher.Subscribe(EventId.TankShotMissed, OnTankShotMissed);
        Dispatcher.Subscribe(EventId.BattleChatCommand, OnBattleChatCommand);
        m_isInitialized = true;
    }

    void OnDestroy()
    {
        Dispatcher.Unsubscribe(EventId.SACLOSLaunchRequired, OnSACLOSLaunchRequired);
        Dispatcher.Unsubscribe(EventId.SACLOSAimed, OnSACLOSAimed);
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
        Dispatcher.Unsubscribe(EventId.TankDamageApplied, OnTankDamageApplied);
        Dispatcher.Unsubscribe(EventId.WeaponOverheated, OnWeaponOverheated);
        Dispatcher.Unsubscribe(EventId.TankKilled, OnTankKilled);
        Dispatcher.Unsubscribe(EventId.PatternExpired, OnPatternExpired);
        Dispatcher.Unsubscribe(EventId.DecalExpired, OnDecalExpired);
        Dispatcher.Unsubscribe(EventId.TankShotMissed, OnTankShotMissed);
        Dispatcher.Unsubscribe(EventId.BattleChatCommand, OnBattleChatCommand);
        m_isInitialized = false;
    }

    private void OnSACLOSLaunchRequired(EventId id, EventInfo ei)
    {
        if (((EventInfo_B)ei).bool1)
            VoiceManager.Play(VoiceEventKey.SACLOSLaunchRequired);
    }

    private void OnSACLOSAimed(EventId id, EventInfo ei)
    {
        EventInfo_IB info = (EventInfo_IB)ei;

        int playerId = info.int1;
        bool aimed = info.bool1;

        if (playerId == BattleController.MyPlayerId && aimed)
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
        EventInfo_U info = (EventInfo_U)ei;

        int victimId = (int)info[0];
        int damage = (int)info[1];
        int ownerId = (int)info[2];
        GunShellInfo.ShellType shellType = (GunShellInfo.ShellType)(int)info[3];
        int hits = (int)info[4];
        Vector3 hitPosition = (Vector3)info[5];

        if (victimId == BattleController.MyPlayerId)
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
        if (GameData.IsGame(Game.BattleOfWarplanes))
            return;

        EventInfo_IIB info = (EventInfo_IIB)ei;

        if (info.int1 != BattleController.MyPlayerId)
            return;

        if (info.bool1 && BattleController.MyVehicle.GetWeapon(GunShellInfo.ShellType.Usual).IsReady)
            VoiceManager.Play(VoiceEventKey.ShotRequired);
    }

    private void OnTankDamageApplied(EventId id, EventInfo ei)
    {
        EventInfo_U info = (EventInfo_U)ei;

        int attackerId = (int)info[2];
        GunShellInfo.ShellType shellType = (GunShellInfo.ShellType)(int)info[3];

        if (attackerId == BattleController.MyPlayerId)
        {
            switch (shellType)
            {
                case GunShellInfo.ShellType.MachineGun:
                    VoiceManager.Play(VoiceEventKey.GoodShotMachineGun);
                    break;
                default:
                    VoiceManager.Play(VoiceEventKey.GoodShot);
                    break;
            }
        }
    }

    private void OnWeaponOverheated(EventId id, EventInfo ei)
    {
        VoiceManager.Play(VoiceEventKey.WeaponOverheated);
    }

    private void OnTankKilled(EventId id, EventInfo ei)
    {
        var info = (EventInfo_III)ei;
        int killer = info.int2;
        int victim = info.int1;

        if (victim == BattleController.MyPlayerId &&
            GameData.IsGame(Game.IronTanks | Game.FutureTanks | Game.ToonWars | Game.Armada | Game.SpaceJet | Game.MetalForce))
        {
            VoiceManager.Play(VoiceEventKey.MyTankDestroyed);
        }

        if (killer == victim) { // Игнорируем самоубийство
            return;
        }

        if (killer == BattleController.MyPlayerId) {
            VoiceManager.Play (VoiceEventKey.EnemyKilled);
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
        EventInfo_II info = (EventInfo_II)ei;

        int playerId = info.int1;
        GunShellInfo.ShellType shellType = (GunShellInfo.ShellType)info.int2;

        if (playerId == BattleController.MyPlayerId)
        {
            switch (shellType)
            {
                case GunShellInfo.ShellType.MachineGun:
                    VoiceManager.Play(VoiceEventKey.MissedShotMachineGun);
                    break;
                default:
                    VoiceManager.Play(VoiceEventKey.MissedShot);
                    break;
            }
        }
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
