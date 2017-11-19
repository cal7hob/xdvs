using System;
using System.Collections.Generic;
using AimingStates;
using Pool;
using Rewired;
using UnityEngine;

public abstract class TurretController
{
    public const string autoAimingAxis = "AutoAimingAxis";
    public const int autoAimingSpeedQual = 3;

    protected Transform turret;
    protected VehicleController vehicle;
    protected AudioClip turretRotationSound;
    protected Animation shootAnimation;
    protected CustomController rewiredConroller;

    protected AutoAimingState normalAutoAimingState;
    protected AutoAimingState fullAutoAimingState;
    protected AutoAimingState withoutAutoAimingState;
    protected AutoAimingState currentAimingState;
    protected AutoAimingState defaultAimingState;
    protected AutoAimingState beforeCheatDefaultAimingState;

    protected VehicleController autoAimingTarget;

    private string shotPrefab;
    private VehicleController target;
    private Vector3 targetPosition;
    private float autoAimingAxisValue;

    private readonly Dictionary<int, VehicleEffect> effects = new Dictionary<int, VehicleEffect>(4);
    private readonly RaycastHit[] aimingHits = new RaycastHit[32];
    private const ShellType DEFAULT_SHELL_TYPE = ShellType.Usual;

    private float maxAutoAimingAngle = 7f;//максимальный суммарный угол доводки
    private float curAutoAimingAngle = 0;

    protected int currentVictimId;
    protected float lastTurretLocalRotationY;
    protected int currentRocketLaunchPointIndex;
    protected int currentShotPointIndex;
    protected AimPointInfo aimPointInfo;
    protected AudioSource turretAudio;
    protected bool isTurretIdleFrameBefore;
    protected readonly List<AimPointInfo> aimPoints = new List<AimPointInfo>(50);
    //protected Transform turret;
    protected float lastTouchTurretRotation = 0f;

    public Dictionary<ShellType, Weapon> weapons;

    public AimPointInfo AimPoint
    {
        get { return aimPointInfo; }
        set { aimPointInfo = value; }
    }

    public bool IsMine { get { return vehicle.IsMine; } }

    public bool IsBot { get { return vehicle.IsBot; } }

    public virtual float WeaponReloadingProgress
    {
        get { return weapons[DefaultShellType].ReloadingProgress; }
    }

    public bool TurretCentering { get; set; }

    public float LastTurretLocalRotationY { get { return lastTurretLocalRotationY; } }

    public bool IsTurretIdleFrameBefore { get { return isTurretIdleFrameBefore; } }

    public bool TargetAimed { get { return vehicle.aimingController.Target != null; } }

    public bool IsFullAutoAimingState { get { return ReferenceEquals(currentAimingState, fullAutoAimingState); } }

    public bool IsAutoAimingOn { get; private set; }

    /*
    public VehicleController Target
    {
        get { return vehicle.aimingController == null ? target : vehicle.aimingController.Target; }
        protected set { target = value; }
    }
   
    public Vector3 TargetPosition
    {
        get
        {
            return vehicle.aimingController == null || vehicle.aimingController.Target == null? targetPosition
                : (vehicle.aimingController.Target.transform.position); 
        }
        protected set { targetPosition = value; }
    }*/
    /*
        public virtual Transform ShotPoint
        {
            get { return vehicle.ShotPoint; }
            set { vehicle.ShotPoint = value; }
        }
    */
    public virtual Transform CannonEnd
    {
        get { return vehicle.CannonEnd; }
        // set { vehicle.GetCannonEnd = value; }
    }

    public virtual Vector3 TargetPoint
    {
        get { return vehicle.transform.position; }
    }

    protected virtual float TurretAxisControl
    {
        get { return vehicle.TurretAxisControl; }
    }

    protected virtual float AutoTurretAxisControl
    {
        get { return vehicle.AutoTurretAxisControl; }
    }

    protected LayerMask HitMask { get { return vehicle.HitMask; } }

    public float TurretRotationZoomSpeedQualifier
    {
        get
        {
            return TargetAimed ? Mathf.Clamp(BattleCamera.Instance.TurretIndicationZoomSqrDist / Vector3.SqrMagnitude(vehicle.TargetPosition - vehicle.transform.position), 0.2f, 1) * vehicle.turretRotationSpeedQualifier : vehicle.turretRotationSpeedQualifier * 0.5f;
        }
    }

    public virtual float TurretRotationSpeedQualifier
    {
        get
        {
            return BattleCamera.Instance.IsZoomed ? TurretRotationZoomSpeedQualifier : vehicle.turretRotationSpeedQualifier;
        }
    }

    public float MaxShootAngleCos { get; private set; }

    public virtual ShellType DefaultShellType
    {
        get { return DEFAULT_SHELL_TYPE; }
    }

    protected TurretController(VehicleController vehicle, Animation shootAnimation)
    {
        this.vehicle = vehicle;
        this.shotPrefab = vehicle.shotPrefabPath;
        this.turretRotationSound = vehicle.turretRotationSound;
        this.shootAnimation = shootAnimation;
        this.turret = vehicle.Turret;
        vehicle.aimingController.ResetTarget();
        WeaponsInit();
        SetMaxAngleCos(Mathf.Cos(Mathf.Deg2Rad * vehicle.MaxShootAngle));

        normalAutoAimingState = new NormalAutoAimingState(this);
        fullAutoAimingState = new FullAutoAimingState(this);
        withoutAutoAimingState = new WithoutAutoAimingState(this);

        IsAutoAimingOn = PlayerPrefs.GetInt("AutoAiming", 1) == 1;

        if (!vehicle.IsMain)
        {
            return;
        }

#if UNITY_EDITOR
        ReInput.InputSourceUpdateEvent += RewiredCheatHandler;
        rewiredConroller = XDevs.Input.TouchController;
        ReInput.InputSourceUpdateEvent += RewiredInputUpdateHandler;
#endif
        if (SystemInfo.deviceType != DeviceType.Handheld)
        {
            currentAimingState = withoutAutoAimingState;
        }
        else
        {
            if (BattleCamera.Instance.IsMouseControlled)
            {
                currentAimingState = withoutAutoAimingState;
            }
            else
            {
                currentAimingState = IsAutoAimingOn ? normalAutoAimingState : withoutAutoAimingState;
                rewiredConroller = XDevs.Input.TouchController;
                ReInput.InputSourceUpdateEvent += RewiredInputUpdateHandler;
            }

        }

        defaultAimingState = currentAimingState;

    }
#if UNITY_EDITOR
    private void RewiredCheatHandler()
    {
        if (Input.GetKeyDown(KeyCode.Alpha7))
        {
            if (currentAimingState != normalAutoAimingState)
            {
                beforeCheatDefaultAimingState = defaultAimingState;
                currentAimingState = normalAutoAimingState;
                defaultAimingState = normalAutoAimingState;
                Debug.Log("FULL AUTO AIM CHEAT ENABLED");
            }
            else
            {
                currentAimingState = beforeCheatDefaultAimingState;
                defaultAimingState = beforeCheatDefaultAimingState;
                Debug.Log("AUTO AIM DISABLED");
            }
        } // Опрашиваем тут читку, т.к. нет своего CheatAutoAim и корутину пришлось бы извне запускать. Лучше так

    }
#endif
    private void RewiredInputUpdateHandler()
    {
        rewiredConroller.SetAxisValue(autoAimingAxis, autoAimingAxisValue);
    }


    public void OnDestroy()
    {
#if UNITY_EDITOR
        ReInput.InputSourceUpdateEvent -= RewiredCheatHandler;
#endif
        ReInput.InputSourceUpdateEvent -= RewiredInputUpdateHandler;
    }

    public void SetAutoAimingTarget(VehicleController target)
    {
        if (!IsAutoAimingOn)
        {
            return;
        }

        autoAimingTarget = target;
        SetAutoAimingState(fullAutoAimingState);
    }

    protected void SetAutoAimingState(AutoAimingState state)
    {
        currentAimingState = state;
    }

    public void ResetAimingState()
    {
        SetAutoAimingState(defaultAimingState);
    }

    public void SetAudioVolume(float volume)
    {
        if (turretAudio != null)
        {
            turretAudio.volume = volume;
        }

    }
    public void StopTurretAudio()
    {
        if (turretAudio != null && !vehicle.IsAvailable && turretAudio.isPlaying)
        {
            turretAudio.Stop();
        }
    }

    public void SetVictimId(int id)
    {
        currentVictimId = id;
    }

    public void SetAnimation(Animation animation)
    {
        shootAnimation = animation;
    }

    protected Shell GetShell(Transform shotPoint, Quaternion rotation, float speed = -1)
    {
        return GetShell(shotPoint, rotation, vehicle.PrimaryShellInfo, speed);
    }

    protected Shell GetShell(Transform shotPoint, Quaternion rotation, Transform cannonEnd, float speed = -1)
    {
        return GetShell(shotPoint, rotation, cannonEnd, vehicle.PrimaryShellInfo, speed);
    }

    protected Shell GetShell(Transform shotPoint, Quaternion rotation, GunShellInfo shellInfo, float speed = -1)
    {
        return GetShell(shotPoint, rotation, shotPoint, shellInfo, speed);
    }

    protected Shell GetShell(Transform shotPoint, Quaternion rotation, Transform cannonEnd, GunShellInfo shellInfo, float speed = -1)
    {
        var shotEffect = PoolManager.GetObject<ParticleEffect>(shotPrefab);
        shotEffect.transform.position = cannonEnd.position;
        shotEffect.transform.forward = cannonEnd.forward;

        var shell = PoolManager.GetObject<Shell>(vehicle.shellPrefabPath);
        shell.transform.position = shotPoint.position;
        shell.transform.rotation = rotation;

        if (speed > 0)
        {
            shell.OwnerSpeed = speed;
        }

        return shell;
    }

    protected Transform GetNextShootPoint(List<Transform> points, ref int index)
    {
        Transform point = points[index];
        index = (int)Mathf.Repeat(++index, points.Count);
        return point;
    }

    protected virtual Quaternion AdditionShellRotation(Transform shotPoint)
    {
        return TargetAimed ?
            Quaternion.LookRotation((vehicle.TargetPosition - shotPoint.position).normalized, shotPoint.up) :
            shotPoint.rotation;
    }

    protected void FireWithoutShell(bool isBotCheck)
    {
        int passiveId = 0;
        int damage = 0;
        bool critHit = false;
        Vector3 hitPoint = Vector3.zero;
        RaycastHit hit;

        Vector3 direction = TargetAimed && IsMine ? (vehicle.TargetPosition - vehicle.ShotPoint.position).normalized : CannonEnd.forward;

        if (Physics.Raycast(vehicle.ShotPoint.position, direction, out hit, 500, HitMask))
        {
            VehicleController passive = null;

            if (hit.collider.tag == "CritZone")
            {
                passive = hit.collider.transform.GetComponentInParent<VehicleController>();
                critHit = true;

                if (isBotCheck && passive.IsBot)
                {
                    passive.BotAI.CurrentBehaviour.OnCritHit(vehicle);
                }
            }
            else if ((HitMask & 1 << hit.collider.gameObject.layer) > 0)
            {
                passive = hit.collider.transform.GetComponentInParent<VehicleController>();
            }

            if (passive && Physics.Raycast(hit.point - direction * 0.1f, -direction, hit.distance, BattleController.HitMask))
            {
                hitPoint = passive.transform.InverseTransformPoint(hit.point);
                passiveId = passive.data.playerId;
                damage = passive.CalcDamage(vehicle.data.attack, critHit);
            }
            else
            {
                hitPoint = hit.point;
            }
        }

        vehicle.PhotonView.RPC("Shoot", PhotonTargets.All, passiveId, hitPoint, damage);
    }

    public abstract bool PrimaryFire();
    public virtual bool BasePrimaryFire()
    {
        vehicle.MarkActivity();

        if (!weapons[StaticContainer.DefaultShellType].IsReady)
        {
            return false;
        }

        weapons[StaticContainer.DefaultShellType].RegisterShot();

        if (shootAnimation)
        {
            shootAnimation.Play();
        }

        return true;
    }

    public virtual void SecondaryFire(ShellType shellType, int targetId)
    {
        throw new System.NotImplementedException();
    }

    protected void BaseSecondaryFire(ShellType shellType)
    {
        vehicle.MarkActivity();
        weapons[shellType].RegisterShot();

        if (IsMine)
        {
            BattleGUI.FireButtons[StaticContainer.DefaultShellType].SimulateReloading();
        }

        if (shootAnimation)
        {
            shootAnimation.Play();
        }
    }

    public void FullRealoadingUpdate()
    {
        foreach (var weapon in weapons.Values)
        {
            weapon.UpdateReloadingProgress();
        }
    }

    public void WeaponsInit()
    {
        weapons = new Dictionary<ShellType, Weapon>
        {
            { ShellType.Usual,             new Weapon(vehicle, ShellType.Usual) },
            { ShellType.Missile_SACLOS,    new Weapon(vehicle, ShellType.Missile_SACLOS) },
            { ShellType.IRCM,              new Weapon(vehicle, ShellType.IRCM) }
        };

        foreach (var weapon in weapons.Values)
        {
            weapon.InstantReload();
        }
    }

    public void ResetLocalAngles()
    {
        if (turret)
        {
            turret.localEulerAngles = Vector3.zero;
        }
    }

    public void SetMaxAngleCos(float angleCos)
    {
        MaxShootAngleCos = angleCos;
    }

    public void FullAutoAim()
    {
        if (!BattleController.visibleTanks.ContainsKey(autoAimingTarget.data.playerId))
        {
            ResetAimingState();
            return;
        }

        var aimDir = (autoAimingTarget.transform.position - turret.position).normalized;
        autoAimingAxisValue = Vector3.Dot(turret.right, aimDir) * autoAimingSpeedQual;
    }

    public void DefaultAutoAim()
    {
        var aimDir = (vehicle.TargetPosition - turret.position).normalized;
        autoAimingAxisValue = Vector3.Dot(turret.right, aimDir) * autoAimingSpeedQual;
    }

    public virtual void TurretRotation()
    {
        if (!turret)
        {
            return;
        }

        float deltaForRotation = 0;
        float currentTurretRotationY = turret.localEulerAngles.y;
        bool turretIdle = HelpTools.Approximately(lastTurretLocalRotationY, currentTurretRotationY);

        if (turretIdle && !isTurretIdleFrameBefore)
        {
            Dispatcher.Send(EventId.StopTurretRotation, new EventInfo_I(vehicle.data.playerId));
        }
        else if (!turretIdle && isTurretIdleFrameBefore)
        {
            Dispatcher.Send(EventId.StartTurretRotation, new EventInfo_I(vehicle.data.playerId));
        }

        isTurretIdleFrameBefore = turretIdle;

        lastTurretLocalRotationY = turret.localEulerAngles.y;

        if (HelpTools.Approximately(Mathf.Abs(TurretAxisControl), 1f))
        {
            currentAimingState = withoutAutoAimingState;
        }

        if (!HelpTools.Approximately(TurretAxisControl + AutoTurretAxisControl, 0))
        {
            deltaForRotation = TurretAxisControl;
            TurretCentering = false;
        }
        else if (TurretCentering)
        {
            if (HelpTools.Approximately(turret.localEulerAngles.y, 0))
            {
                TurretCentering = false;
                if (HelpTools.Approximately(deltaForRotation, 0))
                {
                    return;
                }
                return;
            }

            deltaForRotation = Mathf.Clamp(Mathf.DeltaAngle(turret.localEulerAngles.y, 0), -1, 1);
        }

        if (!vehicle.IsMain)
        {
            return;
        }

        if ((TargetAimed && !vehicle.aimingController.CritZoneAimed) || ReferenceEquals(currentAimingState, fullAutoAimingState))
        {
            currentAimingState.AutoAim();
        }
        else
        {
            autoAimingAxisValue = 0;
            curAutoAimingAngle = 0;
        }

        var maxTurretRotationAngle = vehicle.Speed * TurretRotationSpeedQualifier * Time.deltaTime;
        var realRotation = Mathf.Clamp(
            value: (HelpTools.ApplySensitivity(deltaForRotation, BattleSettings.Instance.TurretRotationSensitivity) + AutoTurretAxisControl) * maxTurretRotationAngle,
            min: -maxTurretRotationAngle,
            max: maxTurretRotationAngle);

        if (TurretCentering && Mathf.Abs(realRotation) > Mathf.Abs(Mathf.DeltaAngle(turret.localEulerAngles.y, 0)))
        {
            turret.localEulerAngles = Vector3.zero;
        }
        else
        {
            turret.Rotate(0, realRotation, 0, Space.Self);
        }
    }

    public void ResetLocalRotation()
    {
        if (turret)
        {
            turret.localRotation = Quaternion.identity;
        }

        lastTurretLocalRotationY = 0;
    }

    public virtual void SetTurretAudio()
    {
        if (!vehicle.IsMain || turretRotationSound == null || GameData.IsGame(Game.WWT2))
        {
            return;
        }

        turretAudio = vehicle.gameObject.AddComponent<AudioSource>();

        turretAudio.clip = turretRotationSound;
        turretAudio.loop = true;
        turretAudio.rolloffMode = AudioRolloffMode.Linear;
        turretAudio.volume = Settings.SoundVolume * SoundControllerBase.TURRET_ROTATION_VOLUME;
        turretAudio.maxDistance = 25;
        turretAudio.dopplerLevel = 0;
    }
}