using UnityEngine;
using System.Collections.Generic;

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
        int,        // attackerId (для id ботов)
        Dictionary<
            int,    // victimId
            Dictionary<GunShellInfo.ShellType, ShellHitInfo>>> shellHits
                = new Dictionary<int, Dictionary<int, Dictionary<GunShellInfo.ShellType, ShellHitInfo>>>(GameData.maxPlayers - 1);

    private static ShellHitDispatcher instance;

    private void Awake()
    {
        if (instance != null)
        {
            Debug.LogError("There is more than one ShellHitDispatcher on scene. Last found deleted.", gameObject);
            Destroy(this);
        }

        instance = this;
    }
    
    private void Start()
    {
        this.InvokeRepeating(SyncShellHits, 0, GameData.shellHitSyncInterval);
    }

    private void OnDestroy()
    {
        instance = null;
    }
    
    public static void AccumulateDamage(int victimId, int attackerId, int damage, Vector3 position, GunShellInfo.ShellType shellType)
    {        
        Dictionary<int, Dictionary<GunShellInfo.ShellType, ShellHitInfo>> attackerShellHits;
        Dictionary<GunShellInfo.ShellType, ShellHitInfo> victimShellHits;

        if (!shellHits.TryGetValue(attackerId, out attackerShellHits))
        {
            shellHits.Add(attackerId, attackerShellHits = new Dictionary<int, Dictionary<GunShellInfo.ShellType, ShellHitInfo>>());
        }

        if (!attackerShellHits.TryGetValue(victimId, out victimShellHits))
        {
            attackerShellHits.Add(victimId, victimShellHits = new Dictionary<GunShellInfo.ShellType, ShellHitInfo>());
        }

        ShellHitInfo info;
        if (!victimShellHits.TryGetValue(shellType, out info))
        {
            victimShellHits.Add(shellType, new ShellHitInfo(damage, position));
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

                if (!XD.StaticContainer.BattleController.Units.TryGetValue(victimShellHits.Key, out victim))
                {
                    continue;
                }

                foreach (var victimHitInfo in victimShellHits.Value)
                {
                    int victimId = victimShellHits.Key;
                    int attackerId = attackerShellHits.Key;
                    int shellType = (int)victimHitInfo.Key;
                    
                    EventInfo_IIIIV eventInfo
                        = new EventInfo_IIIIV(
                            /* victim: */       victimId,
                            /* damage: */       victimHitInfo.Value.Damage,
                            /* owner: */        attackerId,
                            /* shellType: */    shellType,
                            /* hitPosition: */  victimHitInfo.Value.LastHitPosition);

                    if (victim.IsBot)
                    {
                        //Debug.LogError("ShellHit: " + victimHitInfo.Value.Damage + ", victim: " + victimId);
                        Dispatcher.Send(EventId.ShellHit, eventInfo, Dispatcher.EventTargetType.ToMaster);
                    }
                    else
                    {
                        Dispatcher.Send(EventId.ShellHit, eventInfo, Dispatcher.EventTargetType.ToSpecific, victimId);
                    }
                }
            }
        }

        shellHits.Clear();
    }
}
