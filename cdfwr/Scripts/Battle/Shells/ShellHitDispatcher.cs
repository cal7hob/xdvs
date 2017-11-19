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

    
   
    private static ShellHitDispatcher instance;

    void Awake()
    {
        if (instance != null)
        {
            Debug.LogError("There is more than one ShellHitDispatcher on scene. Last found deleted.", gameObject);
            Destroy(this);
        }

        instance = this;
    }
    
    void Start()
    {
        this.InvokeRepeating(SyncShellHits, 0, GameData.shellHitSyncInterval);
    }

    void OnDestroy()
    {
        instance = null;
    }

    private static List<VehicleHit> vehicleShellHits = new List<VehicleHit>();

    private class VehicleHit 
    {
        public int attackerId;
        public int victimId;
        public ShellType sType;
        public int damage;
        public Vector3 hitPosition;

        public VehicleHit(int victimId, int attackerId, int damage, Vector3 position, ShellType shellType) 
        {
            this.attackerId = attackerId;
            this.victimId = victimId;
            this.sType = shellType;
            this.damage = damage;
            this.hitPosition = position;
        }
        public EventInfo_IIIIV GetEI_IIIIV()
        {
            return new EventInfo_IIIIV(
                victimId,
                damage,
                attackerId,
                (int)sType,
                hitPosition);
        }
    }
    
    //-------------------------------------------------------------------------
    public static void AccumulateDamage(int victimId, int attackerId, int damage, Vector3 position, ShellType shellType)
    {
        vehicleShellHits.Add(new VehicleHit(victimId, attackerId, damage, position, shellType));
    }

    private void SyncShellHits()
    {
        VehicleController victim;
        foreach (var vehicleHit in vehicleShellHits)
        {
            if (!BattleController.allVehicles.TryGetValue(vehicleHit.victimId, out victim))
            {
                continue;
            }
            EventInfo_IIIIV eventInfo = vehicleHit.GetEI_IIIIV();

            if (victim.IsBot)
            {
                Dispatcher.Send(
                    id: EventId.ShellHit,
                    info: eventInfo,
                    target: Dispatcher.EventTargetType.ToMaster);
            }
            else
            {
                Dispatcher.Send(
                    id: EventId.ShellHit,
                    info: eventInfo,
                    target: Dispatcher.EventTargetType.ToSpecific,
                    specificId: vehicleHit.victimId);
            }
        }
        vehicleShellHits.Clear();
    }
}
