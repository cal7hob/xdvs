using UnityEngine;
using System.Collections.Generic;
using CodeStage.AntiCheat.ObscuredTypes;
using XD;

public class Shell : MonoBehaviour
{
    public string                                           shellName;
    public bool                                             trackingOwnersEnabled;
    public float                                            speed = 1000;
    public float                                            maxDistance = 1500;

    public AudioClip                                        shotSound;
    public AudioClip[]                                      shotSounds;

    public IConsumableBattle                                myConsumable = null;

    [SerializeField]
    protected HitType                                       hitType = HitType.BulletSmall;

    protected static int                                    currentId;

    protected GunShellInfo.ShellType                        shellType;
    protected int                                           id;
    protected int                                           victimId;
    protected float                                         deltaPos;
    protected float                                         flightDistance;
    protected bool                                          critDamage = false;

    private ISoundSettings                                  soundSettings = null;

    private static readonly ObscuredFloat                   SHELL_RADIUS = 0.050f;
    private static readonly Dictionary<int, List<Shell>>    allActiveShells = new Dictionary<int, List<Shell>>();

    private List<Shell> ownerActiveShells;

    public bool IsLocal
    {
        get
        {
            return ownerId == StaticType.BattleController.Instance<IBattleController>().MyPlayerId || (BotOwns && PhotonNetwork.isMasterClient);
        }
    }

    public bool CritDamage
    {
        get
        {
            return critDamage;
        }
    }

    public bool BotOwns
    {
        get
        {
            return BotDispatcher.IsBotId(ownerId);
        }
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
        get
        {
            return shotSounds != null && shotSounds.Length > 0 ? shotSounds.GetRandomItem() : shotSound;
        }
    }

    protected int       ownerId = 0;
    [SerializeField]
    protected LayerMask hitMask = 0;

    /* UNITY SECTION */

    protected virtual void OnDestroy()
    {
        if (!trackingOwnersEnabled)
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
        if (flightDistance > maxDistance)
        {
            Disactivate();
            return;
        }

        VehicleController victim = victimId != BattleController.DEFAULT_TARGET_ID ? XD.StaticContainer.BattleController.Units.GetValueOrDefault(victimId) : null;
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
            //Debug.LogError(name + " SphereCast", this);
            CrashTriggerBase crash = hit.collider.GetComponentInParent<CrashTriggerBase>();

            if (crash != null)
            {
                crash.CallCrash();
                transform.position = hit.point + transform.forward * 0.01f;
                return;
            }

            if (ShellHit(hit))
            {
                Explosion(hit);
                return;
            }
        }
        
        // Летим дальше.
        if (!IsLocal && victim != null)
        {
            transform.LookAt(victim.TargetPoint);
        }

        transform.Translate(transform.forward * deltaPos, Space.World);
        flightDistance += deltaPos;
    }

    private bool ShellHit(RaycastHit hit)
    {
        IUnitBehaviour hitVictim = hit.collider.GetComponentInParent<IUnitBehaviour>();
                
        if (!IsLocal || hitVictim == null)
        {
            return true;
        }       

        VehicleController owner;

        if (!StaticContainer.BattleController.Units.TryGetValue(ownerId, out owner))
        {
            return true;
        }

        if (hitVictim.Data.playerId == owner.Data.playerId)
        {
            return false;
        }

        if (!StaticContainer.BattleController.FriendlyFire)
        {
            if (owner.Team == hitVictim.Team)
            {
                return true;
            }
        }

        int damage = hitVictim.CalcDamage(Damage, hit.collider);

        if (hitVictim.Settings.Contains(Setting.DamageAbsorptionProbability))
        {
            if (hitVictim.Settings[Setting.DamageAbsorptionProbability].Current > 0)
            {
                if (Random.Range(0, 1f) < hitVictim.Settings[Setting.DamageAbsorptionProbability].Current)
                {
                    damage -= (int)(damage * hitVictim.Settings[Setting.DamageAbsorption].Current);
                }
            }
        }
        else
        {
            if (hitVictim.Settings.Contains(Setting.DamageAbsorption))
            {
                damage -= (int)(damage * hitVictim.Settings[Setting.DamageAbsorption].Current);
            }
        }

        if (hitVictim.HPSystem.Armor < damage)
        {
            damage = hitVictim.HPSystem.Armor;
        }

        hitVictim.ChangeHitPoints(ownerId, damage, hit.point, shellType, critDamage);
        ApplyBuffs(hitVictim);

        owner.GiveDamage(hitVictim.UnitBattle.ID, damage, myConsumable == null ? -1 : myConsumable.ID);

        if (critDamage)
        {
            owner.Event(Message.StatisticUpdate, StatisticParameter.Crits, hitVictim.UnitBattle.ID, 1f);
        }
        return true;
    }

    private void Start()
    {
        //specialID = Random.Range(1, 1000);
        //Debug.LogError("Spawn Shell: " + specialID, gameObject);
        soundSettings = GetComponent<ISoundSettings>();
    }

    private void ApplyBuffs(IUnitBehaviour victim)
    {
        if (myConsumable == null)
        {
            return;
        }
        
        Clamper probability = new Clamper(1);
        for (int i = 0; i < myConsumable.Buffs.Count; i++)
        {
            Buff buff = myConsumable.Buffs[i].CreateNew();

            bool nextBuff = false;
            if (buff != null)
            {
                probability = null;
                for (int j = 0; j < buff.Settings.Count; j++)
                {
                    //Debug.LogError("ApplyBuffs: " + buff.Settings.GetName(j) + ", buff: " + buff.name + ", val: " + buff.Settings[j]);
                    
                    switch (buff.Settings.GetName(j))
                    {
                        case Setting.Duration:
                            continue;
                        case Setting.BurningDamage:
                            probability = buff.Settings[Setting.BurnProbability];
                            break;
                        case Setting.TurretSpeed:
                            probability = buff.Settings[Setting.TurretSpeedChangeProbability];
                            break;
                        case Setting.MovingSpeed:
                            probability = buff.Settings[Setting.MoveSpeedChangeProbability];
                            //Debug.LogError("ApplyBuffs: " + buff.Settings.GetName(j) + ", buff: " + buff.name + ", val: " + buff.Settings[j] + ", chance: " + probability);
                            break;
                        case Setting.RPM:
                            probability = buff.Settings[Setting.RPMChangeProbability];
                            break;
                        case Setting.OneShotProbability:
                            probability = buff.Settings[Setting.OneShotProbability];
                            break;
                        case Setting.TurnSpeed:
                            probability = buff.Settings[Setting.TurnSpeedChangeProbability];
                            break;
                    }

                    //Debug.LogError("ApplyBuffs: " + buff.Settings.GetName(j) + ", buff: " + buff.name + ", val: " + buff.Settings[j] + ", chance: " + probability);

                    if (probability != null && Random.Range(0, 1f) > probability.Current)
                    {
                        nextBuff = true;
                        break;
                    }
                }
            }

            if (nextBuff)
            {
                continue;
            }

            if (buff != null /*&& buff.Notification*/)
            {
                //Buff buff = BuffDispatcher.GetNewBuff(buff.ConsumableOwnerID, (Setting)buff.ID, myConsumable.ID);
                buff.performerID = ownerId;
                buff.forShells = false;//КОСТЫЛЬ?
                buff.ConsumableOwnerID = buff.ConsumableOwnerID;

                StaticType.BuffDispatcher.Instance<IBuffDispatcher>().AddBuff((VehicleController)victim, buff);

                //Debug.LogError("timer: " + myConsumable.Settings[Setting.Duration]);

                Dispatcher.Send(
                    EventId.StartBuff,
                    new EventInfo_U(
                        victim.Data.playerId,
                        ownerId,
                        new[] { buff.ConsumableOwnerID, buff.ID, myConsumable.ID, (int)myConsumable.Settings[Setting.Duration].Max }),
                        Dispatcher.EventTargetType.ToOthers,
                    //victim.IsBot ? Dispatcher.EventTargetType.ToMaster : Dispatcher.EventTargetType.ToSpecific,
                    victim.Data.playerId);
            }
        }
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

        if (!trackingOwnersEnabled)
        {
            return;
        }

        if (allActiveShells.TryGetValue(ownerId, out ownerActiveShells))
        {
            ownerActiveShells.Add(this);
        }
        else
        {
            allActiveShells[ownerId] = new List<Shell> {this};
        }
    }

    protected void OnDisable()
    {
        if (!trackingOwnersEnabled)
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

    public void Explosion(RaycastHit hit)
    {
        //Debug.LogError(name + " Explosion", this);
        //AudioDispatcher.PlayClipAtPosition(blowSounds.Length > 0 ? blowSounds.GetRandomItem() : blowSound, hit.point);
        if (soundSettings != null)
        {
            soundSettings.Reaction(Message.EffectRequest, EffectTarget.Hit);
        }
        StaticContainer.Effects.Play(hitType, hit);
        Disactivate();
    }

    public void Explosion(Vector3 position, bool hitsVehicle = false)
    {
        //Debug.LogError(name + " Explosion: " + position + "; " + hitsVehicle, this);
        //AudioDispatcher.PlayClipAtPosition(blowSounds.Length > 0 ? blowSounds.GetRandomItem() : blowSound, position);
        //EffectPoolDispatcher.GetFromPool(hitsVehicle ? hitPrefab : terrainHitPrefab, position, Quaternion.identity);
        if (soundSettings != null)
        {
            soundSettings.Reaction(Message.EffectRequest, EffectTarget.Hit);
        }
        Disactivate();
	}

    public virtual void Activate(
        VehicleController       owner,
        int                     damage,
        int                     hitMask,
        int                     victimId  = BattleController.DEFAULT_TARGET_ID,
        GunShellInfo.ShellType  shellType = GunShellInfo.ShellType.Usual,
        IConsumableBattle       consumable = null,
        bool crit = false)
    {
        ownerId = owner.data.playerId;
        this.hitMask = hitMask;
        this.victimId = victimId;
        this.shellType = shellType;
        critDamage = crit;
        myConsumable = consumable;
        Damage = damage;
        

        //Damage = myConsumable != null ? (int)myConsumable.Settings[Setting.Damage].RandomValue() : damage;

        gameObject.SetActive(true);

        if (IsLocal && !BotOwns)
        {
            Dispatcher.Send(EventId.MyTankShots, new EventInfo_I((int) shellType));
        }
    }

    public virtual void Disactivate()
    {
        flightDistance = 0;
        gameObject.SetActive(false);
        ShellPoolManager.ReturnToPool(this);
    }

    protected static TShell SelectFirstShellOrDefault<TShell>(int ownerId) where TShell : Shell
    {
        List<Shell> ownerActiveShells = allActiveShells.GetValueOrDefault(ownerId);

        if (ownerActiveShells == null)
        {
            return default(TShell);
        }

        foreach (var shell in ownerActiveShells)
        {
            if (shell is TShell)
            {
                return (TShell) shell;
            }
        }

        return default(TShell);
    }
}

/*** Shell pool manager class ***/
public static class ShellPoolManager
{
    private static readonly Dictionary<string, ShellPool> pools;

    static ShellPoolManager()
    {
        pools = new Dictionary<string, ShellPool>(10);
    }

    public static Shell GetShell(string shellName, Vector3 position, Quaternion rotation)
    {
        ShellPool shellPool = pools.GetValueOrDefault(shellName);

        if (shellPool == null)
        {
            pools.Add(shellName, new ShellPool(shellName));
            shellPool = pools.GetValueOrDefault(shellName);
        }

        Shell shell = shellPool.FromPool();
        shell.transform.position = position;
        shell.transform.rotation = rotation;

        return shell;
    }

    public static void ReturnToPool(Shell shell)
    {
        ShellPool shellPool = pools.GetValueOrDefault(shell.shellName);

        if (shellPool == null)
        {
            DT.LogError("Pool ({0}) doesn't exist", shell.shellName);
            return;
        }

        shellPool.Return(shell);
    }

    public static void ClearAllPools()
    {
        pools.Clear();
    }
}

/*** Shell pool class ***/
public class ShellPool
{
    private readonly Shell shellPrefab;
    private readonly Queue<Shell> shells;
    
    public ShellPool(string shellName)
    {
        shells = new Queue<Shell>(10);
        shellPrefab = Resources.Load<Shell>(QualityManager.GetShellResPath(shellName));
    }

    ~ShellPool()
    {
        shells.Clear();
    }

    public Shell FromPool()
    {
        Shell shell = shells.Count == 0 ? AddShell() : shells.Dequeue();
        return shell;
    }

    public void Return(Shell shell)
    {
        shells.Enqueue(shell);
    }

    private Shell AddShell()
    {
        Shell shell = Object.Instantiate(shellPrefab);
        return shell;
    }
}