using System;
using UnityEngine;
using System.Collections.Generic;
using CodeStage.AntiCheat.ObscuredTypes;
using Pool;
using UnityEngine.Profiling;

public class Shell : PoolObject, IDamageInflicter
{
    public interface IShellDamageable
    {
        void Damage(int damage);
    }

    public bool trackingOwnersEnabled;
    public float speed = 1000;
    public float maxDistance = 1500;
    public AudioClip shotSound;
    public AudioClip[] shotSounds;
    public AudioClip blowSound;
    public AudioClip[] blowSounds;
    public string hitFX;
    public FXInfo hitFXInfo;
    public string terrainHitFX;
    public FXInfo terrainHitFXInfo;

    protected static int currentId;

    [SerializeField]
    protected int shellId;
    protected int victimId;
    protected float flightDistance;

    public static readonly ObscuredFloat SHELL_RADIUS = 0.050f;

    protected int ownerId = 0;
    protected int hitMask = 0;
    private DamageSource _damageSource;
    public DamageSource DamageSource { get { return _damageSource; } }

    private static Dictionary<int, string> shellPaths = new Dictionary<int, string>();

    private ParticleSystem ps;
    
    public bool IsLocal { get; private set; }

    public bool BotOwns
    {
        get
        {
            return BotDispatcher.IsBotId(ownerId);
        }
    }

    public int OwnerId { get { return ownerId; } }

    public VehicleController OwnerVehicle
    {
        get
        {
            VehicleController owner;
            BattleController.allVehicles.TryGetValue(ownerId, out owner);
            return owner;
        }
    }

    public int Damage { get; protected set; }

    public float OwnerSpeed { get; set; }

    public AudioClip ShotSound
    {
        get { return shotSounds != null && shotSounds.Length > 0 ? shotSounds.GetRandomItem() : shotSound; }
    }

    public static void Init()
    {
        Messenger.Subscribe(EventId.QualityLevelChanged, OnQualityChange);
    }

    void Awake()
    {
        ps = GetComponentInChildren<ParticleSystem>(true);
    }

    void Update()
    {
        if (flightDistance > maxDistance)
        {
            Disactivate();
            return;
        }

        float deltaPos = (OwnerSpeed + speed) * Time.deltaTime;

        RaycastHit hit;
        if (!Physics.Raycast(
            transform.position,
            transform.forward,
            out hit,
            deltaPos,
            hitMask,
            QueryTriggerInteraction.Collide)            )
        {
            MoveAhead(deltaPos);
            return;
        }

        IDamageable victim = hit.collider.GetComponent<IDamageable>() ?? hit.transform.GetComponentInParent<IDamageable>();
        if (victim != null)
        {
            victim.TakeDamage(Damage, this, hit.point);
            if (!victim.Solid)
            {
                MoveAhead(Vector3.Distance(transform.position, hit.point) - 0.01f);
                return;
            }
        }

        Explosion(hit.point, ownerId == BattleController.MyPlayerId, true);
    }

    private void Explosion(Vector3 position, bool mainVehOwns, bool hitsVehicle)
    {
        AudioDispatcher.PlayClipAtPosition(blowSounds.Length > 0 ? blowSounds.GetRandomItem() : blowSound, position);
        PoolManager.GetObject<PoolEffect>(hitsVehicle ? hitFXInfo.GetResourcePath(mainVehOwns) : terrainHitFXInfo.GetResourcePath(mainVehOwns), position, Quaternion.identity);
        Disactivate();
	}

    public virtual void Activate(
        VehicleController       owner,
        int                     damage,
        int                     hitMask,
        DamageSource     damageSource)
    {
        IsLocal =
            owner.PhotonView.isMine;
        ownerId = owner.data.playerId;
        Damage = damage;
        this._damageSource = damageSource;
        this.hitMask = hitMask;

        gameObject.SetActive(true);
        if (ps != null)
        {
            ps.Play(true);
        }
    }

    public virtual void Disactivate()
    {
        ReturnObject();
    }

    public override void ReturnObject()
    {
        flightDistance = 0f;
        base.ReturnObject();
    }

    private static void OnQualityChange(EventId id, EventInfo ei)
    {
        shellPaths.Clear();
    }

    private void MoveAhead(float deltaPos)
    {
        transform.Translate(transform.forward * deltaPos, Space.World);
        flightDistance += deltaPos;
    }
}