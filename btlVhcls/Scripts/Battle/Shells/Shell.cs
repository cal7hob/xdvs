using UnityEngine;
using System.Collections.Generic;
using CodeStage.AntiCheat.ObscuredTypes;
using Disconnect;
using XDevs.LiteralKeys;

public class Shell : MonoBehaviour
{
    public string shellName;
    public bool trackingOwnersEnabled;
    public float speed = 1000;
    public float maxDistance = 1500;
    public AudioClip shotSound;
    public AudioClip[] shotSounds;
    public AudioClip blowSound;
    public AudioClip[] blowSounds;
	public GameObject hitPrefab;
    public GameObject terrainHitPrefab;

    protected static int currentId;

    protected GunShellInfo.ShellType shellType;
    protected int id;
    protected int victimId;
    protected float deltaPos;
    protected float flightDistance;
    protected Vector3 aimPointLocalToTarget;

    private static readonly ObscuredFloat SHELL_RADIUS = 0.050f;
    private static readonly Dictionary<int, List<Shell>> allActiveShells = new Dictionary<int, List<Shell>>();

    private List<Shell> ownerActiveShells;

    public bool IsMain
    {
        get { return ownerId == BattleController.MyPlayerId; }
    }

    public bool IsLocal
    {
        get { return IsMain || (BotOwns && BattleConnectManager.IsMasterClient); }
    }

    public bool BotOwns
    {
        get { return BotDispatcher.IsBotId(ownerId); }
    }

    public int Damage
    {
        get; protected set;
    }

    public float OwnerSpeed
    {
        get; set;
    }

    public AudioClip ShotSound
    {
        get { return shotSounds != null && shotSounds.Length > 0 ? shotSounds.GetRandomItem() : shotSound; }
    }

    private bool UseEffectMover
    {
        get
        {
            if (GameData.IsGame(Game.BattleOfWarplanes | Game.WingsOfWar) && !(this is SACLOSMissile))
                return true;

            return false;
        }
    }

    protected int ownerId = 0;
    protected int hitMask = 0;
    protected bool isExploded;

    /* UNITY SECTION */

    protected virtual void Awake()
    {
        shellName = name.Remove(name.IndexOf("(Clone)"));
    }

    protected virtual void OnDestroy()
    {
        if (!trackingOwnersEnabled)
            return;

        if (allActiveShells.TryGetValue(ownerId, out ownerActiveShells))
            ownerActiveShells.Remove(this);
    }

    protected virtual void Update()
    {
        if (flightDistance > maxDistance)
        {
            Disactivate();
            return;
        }

        VehicleController victim
            = victimId != BattleController.DEFAULT_TARGET_ID
                ? BattleController.allVehicles.GetValueOrDefault(victimId)
                : null;

        deltaPos = (OwnerSpeed + speed) * Time.deltaTime;

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
                crash.CallCrash(null);
                transform.position = hit.point + transform.forward * 0.01f;
                return;
            }
            
            VehicleController hitVictim = null;

            if (IsLocal && VehicleController.TryGetHitTarget(hit, out hitVictim))
            {
                VehicleController owner;

                if (BattleController.allVehicles.TryGetValue(ownerId, out owner) &&
                    !VehicleController.AreFriends(owner, hitVictim))
                {
                    bool isCritHit = hit.collider.tag == Tag.Items[Tag.Key.CritZone];
                    int damage = hitVictim.CalcDamage(Damage, isCritHit);

                    if (isCritHit && hitVictim.IsBot)
                        hitVictim.BotAI.CurrentBehaviour.OnCritHit(owner);

                    ShellHitDispatcher.AccumulateDamage(
                        victimId:   hitVictim.data.playerId,
                        attackerId: ownerId,
                        damage:     damage,
                        position:   hitVictim.transform.InverseTransformPoint(hit.point),
                        shellType:  shellType);

                    if (hitVictim.Armor > 0 && !hitVictim.PhotonView.isMine)
                        Dispatcher.Send(
                            id:     EventId.TankTakesDamage,
                            info:   new EventInfo_U(
                                        /* victimId */      hitVictim.data.playerId,
                                        /* damage */        damage,
                                        /* attackerId */    ownerId,
                                        /* shellType */     (int)shellType,
                                        /* hits */          1,
                                        /* hitPosition */   hit.point));

                    if (!hitVictim.PhotonView.isMine)
                    {
                        hitVictim.Armor -= (int)(damage * hitVictim.TakenDamageRatio);
                        Dispatcher.Send(EventId.TankHealthChanged, new EventInfo_II(hitVictim.data.playerId, hitVictim.Armor));
                    }
                }
            }

            if (hitVictim == null)
                Dispatcher.Send(EventId.TankShotMissed, new EventInfo_II(ownerId, (int)shellType));

            Explosion(
                hit:            hit,
                hitsVehicle:    hitVictim != null,
                victim:         hitVictim);

            return;
        }
        
        // Летим дальше.
        if (!IsLocal && victim != null)
            transform.LookAt(victim.TargetPoint);

        transform.Translate(transform.forward * deltaPos, Space.World);

        flightDistance += deltaPos;
    }

    #if UNITY_EDITOR
    protected virtual void OnDrawGizmos()
    {
        Gizmos.color = Color.magenta;
        Gizmos.DrawSphere(center: transform.position, radius: SHELL_RADIUS);
    }
    #endif

    protected void OnEnable()
    {
        id = currentId++;

        if (!trackingOwnersEnabled)
            return;

        if (allActiveShells.TryGetValue(ownerId, out ownerActiveShells))
            ownerActiveShells.Add(this);
        else
            allActiveShells[ownerId] = new List<Shell> { this };
    }

    protected void OnDisable()
    {
        if (!trackingOwnersEnabled)
            return;

        if (allActiveShells.TryGetValue(ownerId, out ownerActiveShells))
            ownerActiveShells.Remove(this);
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

    public void Explosion(Vector3 position, bool hitsVehicle, VehicleController victim)
    {
        isExploded = true;
        AudioDispatcher.PlayClipAtPosition(blowSounds.Length > 0 ? blowSounds.GetRandomItem() : blowSound, position);
        PlayEffect(hitsVehicle, position, Quaternion.identity, victim);
        Disactivate();
	}

    public void Explosion(RaycastHit hit, bool hitsVehicle, VehicleController victim)
    {
        AudioDispatcher.PlayClipAtPosition(blowSounds.Length > 0 ? blowSounds.GetRandomItem() : blowSound, hit.point);
        PlayEffect(hitsVehicle, hit.point, Quaternion.LookRotation(hit.normal), victim);
        Disactivate();
    }

    public virtual void Activate(VehicleController owner, int damage, int hitMask,  int victimId = BattleController.DEFAULT_TARGET_ID, GunShellInfo.ShellType shellType = GunShellInfo.ShellType.Usual)
    {
        ownerId = owner.data.playerId;
        Damage = damage;

        if (owner.data.newbie)
            Damage = (int)(damage * GameManager.NEWBIE_DAMAGE_RATIO);

        this.hitMask = hitMask;
        this.victimId = victimId;
        this.shellType = shellType;

        isExploded = false;

        gameObject.SetActive(true);

        if (IsLocal && !BotOwns)
            Dispatcher.Send(EventId.MyTankShots, new EventInfo_I((int)shellType));
    }

    public virtual void Disactivate()
    {
        flightDistance = 0;
        gameObject.SetActive(false);
        ShellPoolManager.ReturnToPool(this);
    }

    public void SetPositionAndRotation(Vector3 position, Quaternion rotation)
    {
        transform.position = position;
        transform.rotation = rotation;
    }

    public void SetAimPoint(Vector3 aimPointLocalToTarget)
    {
        this.aimPointLocalToTarget = aimPointLocalToTarget;
    }

    protected static TShell SelectFirstShellOrDefault<TShell>(int ownerId)
        where TShell : Shell
    {
        List<Shell> ownerActiveShells = allActiveShells.GetValueOrDefault(ownerId);

        if (ownerActiveShells == null)
            return default(TShell);

        foreach (var shell in ownerActiveShells)
            if (shell is TShell)
                return (TShell)shell;

        return default(TShell);
    }

    private void PlayEffect(bool hitsVehicle, Vector3 position, Quaternion rotation, VehicleController victim)
    {
        if (hitsVehicle && UseEffectMover)
        {
            EffectPoolDispatcher.GetFromPool(
                _effect:        hitPrefab,
                _position:      position,
                _rotation:      rotation,
                useEffectMover: true,
                moverTarget:    victim.Body);
        }
        else
        {
            EffectPoolDispatcher.GetFromPool(
                _effect:    terrainHitPrefab,
                _position:  position,
                _rotation:  rotation);
        }
    }
}