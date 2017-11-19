using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using CodeStage.AntiCheat.ObscuredTypes;
using Pool;

public class Shell : PoolObject
{
    public ShellItem shellItem;
    public string shellName;

    protected static int currentId;

    protected ShellType shellType;
    protected int id;
    protected int victimId;
    protected float deltaPos;
    protected float flightDistance;

    private static readonly ObscuredFloat SHELL_RADIUS = 0.050f;

    private static readonly Dictionary<int, List<Shell>> allActiveShells = new Dictionary<int, List<Shell>>();

    private List<Shell> ownerActiveShells;

    public bool IsLocal
    {
        get
        {
            return ownerId == BattleController.MyPlayerId ||
                   (BotOwns && PhotonNetwork.isMasterClient);
        }
    }

    public bool BotOwns
    {
        get
        {
            return BotDispatcher.IsBotId(ownerId);
        }
    }

    public int Damage { get; protected set; }

    public float OwnerSpeed { get; set; }

    protected int ownerId = 0;
    protected int hitMask = 0;

    /* UNITY SECTION */

    protected virtual void Awake()
    {
        shellName = name.Remove(name.IndexOf("(Clone)"));
    }

    protected virtual void OnDestroy()
    {
        if (!shellItem.ownerTracking)
        {
            return;
        }

        if (allActiveShells.TryGetValue(ownerId, out ownerActiveShells))
        {
            ownerActiveShells.Remove(this);
        }
    }

    protected virtual void Update()
    {
        if (flightDistance > shellItem.maxDistance)
        {
            ReturnObject();
            return;
        }

        VehicleController victim
            = victimId != BattleController.DEFAULT_TARGET_ID
                ? BattleController.allVehicles.GetValueOrDefault(victimId)
                : null;

        deltaPos = (OwnerSpeed + shellItem.speed) * Time.deltaTime;

        RaycastHit hit;

        if (Physics.SphereCast(
            /* origin:      */  transform.position,
            /* radius:      */  SHELL_RADIUS,
            /* direction:   */  transform.forward,
            /* hitInfo:     */  out hit,
            /* maxDistance: */  deltaPos,
            /* layerMask:   */  hitMask))
        {
            CrashTriggerBase crash = null;

            if (hit.transform.parent != null)
                crash = hit.transform.parent.GetComponentInChildren<CrashTriggerBase>();

            if (crash != null)
            {
                crash.CallCrash();
                transform.position = hit.point + transform.forward * 0.01f; // искуственно проталкиваем снаряд дальше, просто чтобы летел дальше
                return;
            }

            VehicleController hitVictim;

            if (IsLocal && (hitVictim = hit.transform.GetComponentInParent<VehicleController>()) != null)
            {
                VehicleController owner;

                if (BattleController.allVehicles.TryGetValue(ownerId, out owner) &&
                    !StaticContainer.AreFriends(owner, hitVictim))
                {
                    bool isCritHit = hit.collider.tag == "CritZone";
                    int damage = hitVictim.CalcDamage(Damage, isCritHit);

                    ShellHitDispatcher.AccumulateDamage(
                        victimId: hitVictim.data.playerId,
                        attackerId: ownerId,
                        damage: damage,
                        position: hitVictim.transform.InverseTransformPoint(hit.point),
                        shellType: shellType);

                    if (hitVictim.Armor > 0 && !hitVictim.IsMine)
                    {
                        Dispatcher.Send(
                           EventId.TankTakesDamage,
                           new EventInfo_U(hitVictim.data.playerId, damage, ownerId, (int)shellType, hit.point), 
                           Dispatcher.EventTargetType.ToAll);
                    }
                    if (!hitVictim.IsMine)
                    {
                        hitVictim.Armor -= damage;
                        Dispatcher.Send(EventId.TankHealthChanged, new EventInfo_II(hitVictim.data.playerId, hitVictim.Armor));
                    }
                }
            }

            Explosion(position: hit.point, hitsVehicle: true);


            return;
        }

        // Летим дальше.
        if (!IsLocal && victim != null)
        {
            transform.LookAt(victim.TargetPoint);
        }

        transform.Translate(transform.forward * deltaPos, Space.World);

        flightDistance += deltaPos;
    }

#if UNITY_EDITOR
    protected virtual void OnDrawGizmos()
    {
        Gizmos.color = Color.magenta;
        Gizmos.DrawSphere(center: transform.position, radius: deltaPos * SHELL_RADIUS);
    }
#endif

    protected void OnEnable()
    {
        id = currentId++;

        if (!shellItem.ownerTracking)
            return;

        if (allActiveShells.TryGetValue(ownerId, out ownerActiveShells))
        {
            ownerActiveShells.Add(this);
        }
        else
        {
            allActiveShells[ownerId] = new List<Shell> { this };
        }
    }

    protected void OnDisable()
    {
        if (!shellItem.ownerTracking)
        {
            return;
        }

        if (allActiveShells.TryGetValue(ownerId, out ownerActiveShells))
        {
            ownerActiveShells.Remove(this);
        }
    }

    /* PUBLIC SECTION */

    public override string ToString()
    {
        return string.Format(
            "Shell {{ shellName = {0}, shellType = {1}, ownerId = {2} }}",
            shellName,
            shellType,
            ownerId);
    }

    public void Explosion(Vector3 position, bool hitsVehicle = false)
    {
        shellItem.Explosion(position, hitsVehicle);
        ReturnObject();
    }

    public virtual void Activate(
        VehicleController owner,
        int damage,
        int hitMask,
        int victimId = BattleController.DEFAULT_TARGET_ID,
        ShellType shellType = ShellType.Usual)
    {
        ownerId = owner.data.playerId;
        Damage = damage;
        this.hitMask = hitMask;
        this.victimId = victimId;
        this.shellType = shellType;

        gameObject.SetActive(true);

        if (IsLocal && !BotOwns)
            Dispatcher.Send(EventId.MyTankShots, new EventInfo_I((int)shellType));
    }

    protected static TShell SelectFirstShellOrDefault<TShell>(int ownerId)
        where TShell : Shell
    {
        List<Shell> ownerActiveShells = allActiveShells.GetValueOrDefault(ownerId);

        if (ownerActiveShells == null)
            return default(TShell);

        return ownerActiveShells.OfType<TShell>().FirstOrDefault();
    }

    public override void OnTakenFromPool()
    {
        gameObject.SetActive(true);
    }

    public override void OnReturnedToPool()
    {
        flightDistance = 0;
        gameObject.SetActive(false);
    }

    public override void OnPreWarm()
    {
        ReturnObject();
    }
}