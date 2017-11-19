using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using StateMachines;
using XDevs.LiteralKeys;

public partial class SoldierController
{
    private Transform cannonEnd;

    protected override void SetShootingMode()
    {
        shootingController.ShootingStateMachine.SetState(ProfileInfo.isAutoFire ? ShootingStates.automatic : ShootingStates.manual);
      //  GUIControlSpriteGroups.SetActiveSpriteGroups(GUIControlSpriteGroups.PrimaryFireBtnSprites, true); // кнопка выстрела должна быть всегда, в т.ч. и при Auto_fire(Антон сказал)
        //!ProfileInfo.isAutoFire);
    }

    [PunRPC]
    public override void Shoot(int victimId, int attackerId, Vector3 hitPosition, Vector3 normal, int damage, bool hasHit)
    {
        if (IsDead)
        {
            return;
        }
        if (!isAvailable)
        {
            return;
        }

        if (IsVisible)
        {
            PlayShotEffect(hitPosition);
        }

        PlayShotSound();

        if (!hasHit)
        {
            return;
        }

        VehicleController victim = null;
        if (victimId > 0 && BattleController.allVehicles.TryGetValue(victimId, out victim))
        {
            PlayVictimHitEffect(hitPosition, normal);
            PlaySound(victim.blowSound, hitPosition);
        }
        else
        {
            Dispatcher.Send(EventId.TankShotMissed, new EventInfo_I(attackerId));
            PlayHitEffect(hitPosition, normal);
            return;
        }

        // NO FRIENDLY FIRE!!!
        if (!victim.IsAvailable || StaticContainer.AreFriends(attackerId, victimId))
        {
            return;
        }

        if (attackerId != data.playerId)
        {
            return;
        }
        Dispatcher.Send(EventId.TankTakesDamage, new EventInfo_U(victimId, damage, attackerId, (int)StaticContainer.DefaultShellType, hitPosition), Dispatcher.EventTargetType.ToAll);
    }

    protected override void OnVehicleTakesDamage(EventId id, EventInfo ei)
    {
        EventInfo_U info = (EventInfo_U)ei;

        int victimId = (int)info[0];
        if (victimId != data.playerId)//только если тот же самый игрок
        {
            return;
        }
        // 
        if (Armor <= 0) //Уже мертвы 
        {
            return;
        }

        if (!IsMine)
        {
            return;
        }

        Armor -= (int)info[1];//damage

        //Dispatcher.Send(EventId.TankHealthChanged, new EventInfo_II(data.playerId, Armor));
        SetCustomProperties(StatisticKey.Health, Armor);

        if (IsAvailable && Armor <= 0)
        {
            Dispatcher.Send(EventId.TankKilledInfo, new EventInfo_II(data.playerId, (int)info[3]), Dispatcher.EventTargetType.ToAll);
            Dispatcher.Send(EventId.HideEnemy, new EventInfo_I(data.playerId));
            Dispatcher.Send(EventId.TankKilled, new EventInfo_II(data.playerId, (int)info[2]), Dispatcher.EventTargetType.ToAll);
            PhotonView.RPC("Death", PhotonTargets.All, (int)data.playerId);
            SetAiming(false);
        }
    }

}
