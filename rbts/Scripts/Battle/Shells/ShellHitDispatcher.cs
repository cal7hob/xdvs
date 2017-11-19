using UnityEngine;
using System.Collections.Generic;
using UnityEngine.Profiling;

public class ShellHitDispatcher : MonoBehaviour
{
    private class ShellHitInfo
    {
        public ShellHitInfo(int damage, Vector3 lastHitPosition)
        {
            Damage = damage;
            LastHitPosition = lastHitPosition;
        }

        internal int Damage { get; set; }

        internal Vector3 LastHitPosition { get; set; }
    }

    private static readonly Dictionary<
        int, Dictionary<int, Dictionary<int, ShellHitInfo>>> shellHits
                = new Dictionary<int, Dictionary<int, Dictionary<int, ShellHitInfo>>>(GameData.maxPlayers - 1);

    private static ShellHitDispatcher instance;

    void Awake()
    {
        if (instance != null)
        {
            Debug.LogError("There is more than one ShellHitDispatcher on scene. Last found deleted.", gameObject);
            Destroy(this);
        }

        instance = this;
        Messenger.Subscribe(EventId.PortionOfDamage, OnPortionOfDamage);
    }
    
    void Start()
    {
        this.InvokeRepeating(SyncShellHits, 0, GameData.shellHitSyncInterval);
    }

    void OnDestroy()
    {
        shellHits.Clear();
        Messenger.Unsubscribe(EventId.PortionOfDamage, OnPortionOfDamage);

        instance = null;
    }

    private void OnPortionOfDamage(EventId eid, EventInfo ei)
    {
        EventInfo_IIIIV info = ei as EventInfo_IIIIV;
        int attackerId = info.int3;
        if (attackerId != BattleController.MyPlayerId && (!PhotonNetwork.isMasterClient || !BotDispatcher.IsBotId(attackerId)))
            return;

        AccumulateDamage(info.int1, info.int2, info.int3, info.int4, info.vector);
    }

    private static void AccumulateDamage(
        int                     victimId,
        int                     damage,
        int                     attackerId,
        int                     damageInflicterType,
        Vector3                 position)
    {
        Dictionary<int, Dictionary<int, ShellHitInfo>> attackerShellHits;
        Dictionary<int, ShellHitInfo> victimShellHits;

        if (!shellHits.TryGetValue(attackerId, out attackerShellHits))
        {
            attackerShellHits = new Dictionary<int, Dictionary<int, ShellHitInfo>>();
            shellHits.Add(attackerId, attackerShellHits);
        }

        if (!attackerShellHits.TryGetValue(victimId, out victimShellHits))
        {
            victimShellHits = new Dictionary<int, ShellHitInfo>();
            attackerShellHits.Add(victimId, victimShellHits);
        }

        ShellHitInfo info;

        if (!victimShellHits.TryGetValue(damageInflicterType, out info))
        {
            victimShellHits.Add(damageInflicterType, new ShellHitInfo(damage, position));
        }
        else
        {
            info.Damage += damage;
            info.LastHitPosition = position;
        }
    }

    private void SyncShellHits()
    {
        foreach (var attackerShellHits in shellHits)
        {
            foreach (var victimShellHits in attackerShellHits.Value)
            {
                VehicleController victim;

                if (!BattleController.allVehicles.TryGetValue(victimShellHits.Key, out victim))
                    continue;

                foreach (var victimHitInfo in victimShellHits.Value)
                {
                    int victimId = victimShellHits.Key;
                    int attackerId = attackerShellHits.Key;
                    int damageInflicterType = victimHitInfo.Key;

                    EventInfo_IIIIV eventInfo
                        = new EventInfo_IIIIV(
                            /* victim: */       victimId,
                            /* damage: */       victimHitInfo.Value.Damage,
                            /* owner: */        attackerId,
                            /* damageInflicterType: */    damageInflicterType,
                            /* hitPosition: */  victimHitInfo.Value.LastHitPosition);

                    if (victim.IsBot)
                    {
                        Messenger.Send(
                            id: EventId.ShellHit,
                            info: eventInfo,
                            target: Messenger.EventTargetType.ToMaster);
                    }
                    else
                    {
                        Messenger.Send(
                            id: EventId.ShellHit,
                            info: eventInfo,
                            target: Messenger.EventTargetType.ToSpecific,
                            specificId: victimId);
                    }
                }
            }
        }

        shellHits.Clear();
    }
}
