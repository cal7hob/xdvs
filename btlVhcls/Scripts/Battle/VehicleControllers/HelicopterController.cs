#if !(UNITY_STANDALONE_OSX || UNITY_WEBPLAYER || UNITY_WEBGL)
    #define TOUCH_SCREEN
#endif

using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using CodeStage.AntiCheat.ObscuredTypes;
using XDevs.LiteralKeys;

public class HelicopterController : FlightController
{
    [Header("Настройки для вертолёта")]

    [Header("Ссылки")]
    public Transform indicatorPoint;
    public Transform ircmLaunchPoint;
    public Transform targetPoint;
    public List<Transform> rocketLaunchPoints = null;
    public HelicopterAnimationController animationController;

    [Header("Управление")]
    public ObscuredFloat xSpeedRatio = 0.7f;
    public ObscuredFloat ySpeedRatio = 0.55f;
    public ObscuredFloat zSpeedRatio = 0.7f;
    public ObscuredFloat yAccelerationRatio = 0.15f;
    public ObscuredFloat xInertionRatio = 0.035f;
    public ObscuredFloat yInertionRatio = 0.25f;
    public ObscuredFloat zInertionRatio = 0.015f;
    public ObscuredFloat rotationInertionRatio = 0.2f;
    public ObscuredFloat rotationInclineRatio = 0.5f;
    public ObscuredFloat maxInclineRatio = 0.4f;
    public ObscuredFloat inclineSmooth = 0.03f;
    public ObscuredFloat backToMapForceValue = 100.0f;
    public ObscuredFloat minDistanceToObstacle = 10.0f;
    public ObscuredFloat shakingVelocity = 0.002f;
    public ObscuredFloat xShakingVelocity = 0.0005f;

    [Header("Повреждения эффекты")]
    public GameObject fireEffect;
    public GameObject smokeEffect;

    [Header("Звуки")]
    public AudioClip rocketGuidanceSound;
    public AudioClip rocketThreatSound;
    public AudioClip shootingSound;
    public AudioClip shootingEndSound;

    private const float FIRE_START_HEALTH_RATIO_THRESHOLD = 0.5f;
    private const float SMOKE_START_HEALTH_RATIO_THRESHOLD = 0.4f;
    private const float WATER_EFFECT_DISTANCE = 35.0f;
    private const float CRASHING_Y_ROTATION_QUALIFIER = 250.0f;
    private const float CRASHING_Z_ROTATION_QUALIFIER = 150.0f;
    private const float CRASH_SMOOTHING_RATIO = 0.75f;
    private const float INERTION_MULTIPLIER = 8.0f;
    private const float AIM_EXTENTS_RATIO = 0.25f;
    private const float AIM_MAX_INACCURACY_ANGLE = 0.75f;
    private static readonly ObscuredFloat SPEED_RATIO = 7.5f;
    private static readonly ObscuredFloat ODOMETER_RATIO = 0.025f;
    private static readonly ObscuredFloat CORRECTION_TIME = 0.5f;
    private static readonly ObscuredFloat MAX_SHOOT_ANGLE = 25.0f;
    private static readonly ObscuredFloat AIM_DISTANCE_RATIO = 4.0f;
    private static readonly ObscuredFloat FALL_FORCE = 1160.0f;
    private static readonly ObscuredFloat MORTAL_ARMOR_RATIO = 0.0f;
    private static readonly Color FIRE_START_COLOR = Color.black;
    private static readonly Color FIRE_FINAL_COLOR = new Color(1.0f, 0.47f, 0);
    private static readonly Color SMOKE_START_COLOR = new Color(0.35f, 0.35f, 0.35f);
    private static readonly Color SMOKE_FINAL_COLOR = Color.black;

    private readonly Dictionary<int, int> threats = new Dictionary<int, int>();

    private bool isCrashing;
    private int qualityLevel;
    private int currentRocketLaunchPointIndex;
    private int lastAttackerId;
    private int currentVictimId;
    private float xAxisAcceleration;
    private float xAxisAltAcceleration;
    private float yAxisAcceleration;
    private float yAxisAltAcceleration;
    private float input;
    private float currentAcceleration;
    private Vector3 requiredLocalVelocity;
    private Transform cannonEnd;
    private RaycastHit obstacleHitInfo;
    private ParticleSystemWrapper waterTrailEffect;
    private AudioSource rocketGuidanceAudio;
    private AudioSource rocketThreatAudio;
    private AudioSource shootingAudio;
    private IEnumerator crashingRoutine;
    private int layerMask;
    private float sqrMaxAimDistance;
    private float minAimAngleCos;

    public override bool IsCrashing
    {
        get
        {
            if (!PhotonView.isMine)
                return isCrashing = IsAvailable && Armor <= MaxArmor * MORTAL_ARMOR_RATIO;

            return isCrashing;
        }
        set
        {
            if (!PhotonView.isMine)
                return;

            isCrashing = value;

            rb.useGravity = isCrashing;

            if (!isCrashing)
                return;

            Dispatcher.Send(EventId.VehicleCrashing, new EventInfo_I(data.playerId));
        }
    }

    public override float ThrottleLevelInputAxis
    {
        get { return 0.0f; }
    }

    public override Transform Turret
    {
        get { return turret; }
    }

    public override Transform ShotPoint
    {
        get { return shotPoint = shotPoint ?? (transform.Find("Turret/ShotPoint") ?? transform.Find("ShotPoint")); }
    }

    public override Transform CannonEnd
    {
        get { return cannonEnd = cannonEnd ?? (transform.Find("Turret/CannonEnd") ?? transform.Find("CannonEnd")); }
    }

    public override Vector3 TargetPoint
    {
        get { return targetPoint.position; }
    }

    public override Renderer Renderer
    {
        get { return renderer ?? (renderer = shipTransform.GetComponent<Renderer>()); }
    }

    public Vector3 LastPositionAlive
    {
        get; private set;
    }

    protected override bool FireButtonPressed
    {
        get
        {
            return XDevs.Input.GetButton("Fire Primary");
        }
    }

    public override bool IsRequirePrimaryFire
    {
        get
        {
            return FireButtonPressed && PrimaryFireIsOn && !BattleGUI.IsWindowOnScreen && !IsBot;
        }
    }

    public override bool IsRequireSecondaryFire
    {
        get
        {
            return TargetAimed && FireButtonPressed && PrimaryFireIsOn && !BattleGUI.IsWindowOnScreen && !IsBot;
        }
    }

    // Turn Horizontal.
    public override float XAxisControl
    {
        get
        {
            if (BattleGUI.IsWindowOnScreen)
                return 0;

            if (ProfileInfo.ControlOption == ControlOption.gyroscope)
                return GetAccelerometerValForScreenDimmension(isHorizontal: true);
            else
                return XDevs.Input.GetAxis("Turn Left/Right");
        }
    }

    // Move Forward/Backward.
    public override float YAxisControl
    {
        get
        {
            if (BattleGUI.IsWindowOnScreen)
                return 0;

            if (ProfileInfo.ControlOption == ControlOption.gyroscope)
                return GetAccelerometerValForScreenDimmension(isHorizontal: false);
            else
                return XDevs.Input.GetAxis("Move Forward/Backward");
        }
    }

    // Strafe Horizontal.
    public override float XAxisAltControl
    {
        get
        {
            if (BattleGUI.IsWindowOnScreen)
                return 0;

            float rInp = XDevs.Input.GetAxis("Strafe Left/Right");

            if (!Mathf.Approximately(rInp, 0))
            {
                return rInp;
            }
            else if (Mathf.Abs(Input.acceleration.x) > 0.5)
            {
                return Mathf.Clamp(Input.acceleration.x, -1, 1);
            }

            return 0;
        }
    }

    // Move Vertical.
    public override float YAxisAltControl
    {
        get
        {
            if (BattleGUI.IsWindowOnScreen)
                return 0;

            float rInp = XDevs.Input.GetAxis("Move Up/Down");

            if (ProfileInfo.isSliderControl)
            {
                // В случае наэкранного слайдера приоритет у другого управления:
                if (Mathf.Approximately(rInp, 0))
                    rInp = ThrottleLevel.Value;
            }

            return rInp;
        }
    }

    protected override float OdometerRatio
    {
        get { return ODOMETER_RATIO; }
    }

    protected override float SpeedRatio
    {
        get { return SPEED_RATIO; }
    }

    public override float MaxShootAngle
    {
        get { return MAX_SHOOT_ANGLE; }
    }

    protected override bool NeedCorrectAimY
    {
        get { return false; }
    }

    protected override float CorrectionTime
    {
        get { return CORRECTION_TIME; }
    }

    public override float MaxAimDistance
    {
        get { return RadarController.MaxVisibleDistance * AIM_DISTANCE_RATIO; }
    }

    protected override Vector3 IndicatorDeltaOffset
    {
        get { return Vector3.up * 5.0f; }
    }

    protected override Transform IndicatorPoint
    {
        get { return indicatorPoint = indicatorPoint ?? transform.Find("IndicatorPoint"); }
    }

    private bool IsTargetOfSACLOS
    {
        get { return threats.Count > 0; }
    }

    private bool IRCMButtonPressed
    {
        get
        {
            return  XDevs.Input.GetButtonDown("Launch IRCM");
        }
    }

    private bool IsRequireIRCMLaunch
    {
        get
        {
            bool multipleClicks = InputWrapper.GetButtonDown(buttonName: "Launch IRCM", times: 3);

            return (IsTargetOfSACLOS || ProfileInfo.IsBattleTutorial || multipleClicks) &&
                   IRCMButtonPressed &&
                   !BattleGUI.IsWindowOnScreen &&
                   weapons[GunShellInfo.ShellType.IRCM].IsReady;
        }
    }

    public override float XAxisAcceleration
    {
        get
        {
            return xAxisAcceleration
                = Accelerate(
                    oldSpeed:       xAxisAcceleration,
                    newSpeed:       XAxisControl,
                    step:           acceleration * Time.deltaTime,
                    inertionRatio:  rotationInertionRatio);
        }
    }

    private float XAxisAltAcceleration
    {
        get
        {
            return xAxisAltAcceleration
                = Accelerate(
                    oldSpeed:       xAxisAltAcceleration,
                    newSpeed:       XAxisAltControl,
                    step:           acceleration * Time.deltaTime,
                    inertionRatio:  xInertionRatio);
        }
    }

    public override float YAxisAcceleration
    {
        get
        {
            return yAxisAcceleration
                = Accelerate(
                    oldSpeed:       yAxisAcceleration,
                    newSpeed:       YAxisControl,
                    step:           acceleration * Time.deltaTime,
                    inertionRatio:  zInertionRatio);
        }
    }

    private float YAxisAltAcceleration
    {
        get
        {
            return yAxisAltAcceleration
                = Accelerate(
                    oldSpeed:       yAxisAltAcceleration,
                    newSpeed:       IsCrashing ? 0 : YAxisAltControl,
                    step:           acceleration * yAccelerationRatio * Time.deltaTime,
                    inertionRatio:  yInertionRatio);
        }
    }

    /* UNITY SECTION */

    public override void OnPhotonInstantiate(PhotonMessageInfo info)
    {
        Dispatcher.Subscribe(EventId.SecondaryWeaponUsed, OnSecondaryWeaponUsed);
        Dispatcher.Subscribe(EventId.TankHealthChanged, OnHelicopterHealthChanged);
        Dispatcher.Subscribe(EventId.ShellStateChanged, OnShellStateChanged);
        Dispatcher.Subscribe(EventId.MissileThreat, OnMissileThreat);
        Dispatcher.Subscribe(EventId.StartBurstAimedFire, OnStartBurstAimedFire);
        Dispatcher.Subscribe(EventId.StopBurstAimedFire, OnStopBurstAimedFire);
        Dispatcher.Subscribe(EventId.VehicleCrashing, OnVehicleCrashing);
        Dispatcher.Subscribe(EventId.IRCMLaunched, OnIRCMLaunched);
        Dispatcher.Subscribe(EventId.WeaponReloaded, OnSACLOSReloaded);

        base.OnPhotonInstantiate(info);

        qualityLevel = PhotonView.isMine ? QualitySettings.GetQualityLevel() : 1;

        SetRocketGuidanceAudio();
        SetRocketThreatAudio();
        SetShootingAudio();

        SetDamageEffects();
        AssignEnvironmentEffects();

        StartCoroutine(Shaking());
        layerMask = MiscTools.GetLayerMask(Layer.Key.Terrain) + MiscTools.GetLayerMask(Layer.Key.OutOfMap);

        sqrMaxAimDistance = MaxAimDistance * MaxAimDistance;
        minAimAngleCos = Mathf.Cos(Mathf.Deg2Rad * MaxShootAngle);
    }

    protected override void OnDestroy()
    {
        Dispatcher.Unsubscribe(EventId.SecondaryWeaponUsed, OnSecondaryWeaponUsed);
        Dispatcher.Unsubscribe(EventId.TankHealthChanged, OnHelicopterHealthChanged);
        Dispatcher.Unsubscribe(EventId.ShellStateChanged, OnShellStateChanged);
        Dispatcher.Unsubscribe(EventId.MissileThreat, OnMissileThreat);
        Dispatcher.Unsubscribe(EventId.StartBurstAimedFire, OnStartBurstAimedFire);
        Dispatcher.Unsubscribe(EventId.StopBurstAimedFire, OnStopBurstAimedFire);
        Dispatcher.Unsubscribe(EventId.VehicleCrashing, OnVehicleCrashing);
        Dispatcher.Unsubscribe(EventId.IRCMLaunched, OnIRCMLaunched);
        Dispatcher.Unsubscribe(EventId.WeaponReloaded, OnSACLOSReloaded);

        base.OnDestroy();
    }

    protected override void Update()
    {
        base.Update();

        if (!IsAvailable)
            return;

        if (PhotonView.isMine && !IsBot)
        {
            MoveStaticGunsight();

            input
                = SafeLinq.Max(
                    new float[]
                    {
                        Mathf.Abs(YAxisControl),
                        Mathf.Abs(YAxisAltControl),
                        Mathf.Abs(XAxisAltControl)
                    });

            currentAcceleration
                = SafeLinq.Max(
                    new float[]
                    {
                        Mathf.Abs(YAxisAcceleration),
                        Mathf.Abs(YAxisAltAcceleration),
                        Mathf.Abs(XAxisAltAcceleration)
                    });

            requiredSpeed = MaxSpeed * input;

            currentSpeed = MaxSpeed * currentAcceleration;
        }
        else
        {
            float correctSpeed = correctVelocity.magnitude;

            if (!Mathf.Approximately(currentSpeed, correctSpeed))
                currentSpeed
                    = Mathf.MoveTowards(
                        current:    currentSpeed,
                        target:     correctVelocity.magnitude,
                        maxDelta:   acceleration * Time.deltaTime);

            if (burst)
                PrimaryFire();
        }

        if (!HelpTools.Approximately(accelerationDirection, 0))
            MarkActivity();

        if (!IsAvailable)
            return;

        if (PhotonView.isMine && !IsBot)
        {
            if (IsRequireSecondaryFire)
                UseSecondaryWeapon(GunShellInfo.ShellType.Missile_SACLOS);

            if (IsRequireIRCMLaunch)
                UseSecondaryWeapon(GunShellInfo.ShellType.IRCM);
        }

        if (burst)
            PrimaryFire();

        if (PhotonView.isMine && !IsBot && !IsRequirePrimaryFire && shootingAudio.isPlaying)
        {
            shootingAudio.Stop();

            AudioDispatcher.PlayClipAtPosition(
                clip:       shootingEndSound,
                position:   transform.position);
        }
    }

    protected override void FixedUpdate()
    {
        SetWaterEffects();

        MovePlayer();
    }

    void OnCollisionStay(Collision collision)
    {
        if (!PhotonView.isMine)
            return;

        if (!IsCrashing)
        {
            rb.freezeRotation = true;
        }
        else
        {
            Dispatcher.Send(
                id:     EventId.TankKilled,
                info:   new EventInfo_III(data.playerId, lastAttackerId, (int)GunShellInfo.ShellType.Usual),//TODO: !!!!!! paste correct variable instead of ShellType.Usual
                target: Dispatcher.EventTargetType.ToAll);
        }
    }

    void OnCollisionExit(Collision collision)
    {
        if (!PhotonView.isMine)
            return;

        if (!IsCrashing)
            rb.freezeRotation = false;
    }

    //void OnDrawGizmos()
    //{
    //    Gizmos.color = Color.yellow;
    //    Gizmos.DrawSphere(obstacleHitInfo.point, 2);
    //}

    /* PUBLIC SECTION */

    public override void MovePlayer()
    {
        animationController.Receive();

        if (!PhotonView.isMine)
            return;

        if (!Mathf.Approximately(XAxisControl + XAxisAltControl + YAxisControl + YAxisAltControl, 0))
            MarkActivity();

        requiredLocalVelocity = LocalVelocity;

        requiredLocalVelocity.x = xSpeedRatio * MaxSpeed * XAxisAltAcceleration;
        requiredLocalVelocity.z = zSpeedRatio * MaxSpeed * YAxisAcceleration;

        rb.velocity = rb.transform.TransformDirection(requiredLocalVelocity);

        rb.rotation
            = Quaternion.Lerp(
                a: rb.rotation,
                b: Quaternion.LookRotation(
                        forward: transform.forward.GetHorizontalIdentity()
                                        + (-transform.up.GetVerticalIdentity()
                                            * maxInclineRatio
                                            * YAxisAcceleration)
                                        + (transform.right.GetHorizontalIdentity()
                                            * XAxisAcceleration
                                            * Mathf.Sign(YAxisAcceleration)
                                            * (Mathf.Sign(YAxisAcceleration) > 0 ? 1 : ProfileInfo.isInvert ? 1 : -1)),
                        upwards: Quaternion.identity * Vector3.up
                                        + (transform.right.GetHorizontalIdentity()
                                            * maxInclineRatio
                                            * (XAxisAltAcceleration + (XAxisAcceleration * rotationInclineRatio)))),
                t: inclineSmooth);

        rb.velocity
            = new Vector3(
                x: rb.velocity.x,
                y: ySpeedRatio * MaxSpeed * YAxisAltAcceleration,
                z: rb.velocity.z);

        SetEngineNoise(Mathf.Abs(currentSpeed / MaxSpeed));

        if (Physics.Raycast(
                /* ray:         */  new Ray(transform.position, Vector3.ProjectOnPlane(rb.velocity, Vector3.up)),
                /* hitInfo:     */  out obstacleHitInfo,
                /* maxDistance: */  minDistanceToObstacle,
                /* layerMask:   */  layerMask))
        {
            rb.velocity += Vector3.Project(-rb.velocity, obstacleHitInfo.normal);
        }

        if (Physics.Raycast(
                /* ray:         */  new Ray(transform.position, Vector3.up),
                /* hitInfo:     */  out obstacleHitInfo,
                /* maxDistance: */  minDistanceToObstacle,
                /* layerMask:   */  layerMask))
        {
            var vel = rb.velocity;
            vel.y = vel.y > 0 ? 0 : vel.y;
            rb.velocity = vel;
        }

        StoreVehiclePosition();
    }

    [PunRPC]
    public override void Respawn(Vector3 position, Quaternion rotation, bool restoreLife, bool firstTime)
    {
        base.Respawn(position, rotation, restoreLife, firstTime);
        threats.Clear();
        IsCrashing = false;
    }

    public override bool PrimaryFire()
    {
        MarkActivity();

        if (!weapons[DefaultShellType].IsReady)
            return false;

        if (PhotonView.isMine)
            BattleGUI.FireButtons[DefaultShellType].SimulateReloading();

        weapons[DefaultShellType].RegisterShot();

        if (shootAnimation)
            shootAnimation.Play();

        Transform currentShotEffectPoint = shootEffectPoints[currentShotPointIndex];

        currentShotPointIndex = (int)Mathf.Repeat(++currentShotPointIndex, shootEffectPoints.Count);

        Quaternion rotation = currentShotEffectPoint.rotation;

        if (PhotonView.isMine)
        {
            rotation
                = Quaternion.Euler(
                    x:  currentShotEffectPoint.rotation.eulerAngles.x + UnityEngine.Random.Range(-AIM_MAX_INACCURACY_ANGLE, AIM_MAX_INACCURACY_ANGLE),
                    y:  currentShotEffectPoint.rotation.eulerAngles.y + UnityEngine.Random.Range(-AIM_MAX_INACCURACY_ANGLE, AIM_MAX_INACCURACY_ANGLE),
                    z:  currentShotEffectPoint.rotation.eulerAngles.z + UnityEngine.Random.Range(-AIM_MAX_INACCURACY_ANGLE, AIM_MAX_INACCURACY_ANGLE));

            if (TargetAimed && aimPointInfo.target != null)
            {
                Vector3 victimPosition = aimPointInfo.point;

                Vector3 victimBounds = aimPointInfo.target.Renderer.bounds.extents * AIM_EXTENTS_RATIO;

                Vector3 victimPoint
                    = new Vector3(
                        x:  victimPosition.x + UnityEngine.Random.Range(-victimBounds.x, victimBounds.x),
                        y:  victimPosition.y + UnityEngine.Random.Range(-victimBounds.y, victimBounds.y),
                        z:  victimPosition.z + UnityEngine.Random.Range(-victimBounds.z, victimBounds.z));

                rotation
                    = Quaternion.LookRotation(
                        forward:    (victimPoint - currentShotEffectPoint.position).normalized,
                        upwards:    currentShotEffectPoint.up);
            }
        }

        EffectPoolDispatcher.GetFromPool(
            _effect:        shotPrefab,
            _position:      currentShotEffectPoint.position,
            _rotation:      currentShotEffectPoint.rotation,
            useEffectMover: true,
            moverTarget:    currentShotEffectPoint);

        Shell shell
            = ShellPoolManager.GetShell(
                shellName:  primaryShellInfo.shellPrefabName,
                position:   currentShotEffectPoint.position,
                rotation:   rotation);

        shell.OwnerSpeed = Mathf.Abs(currentSpeed);
        continuousFire = primaryShellInfo.continuousFire;

        shell.Activate(this, data.attack, hitMask, currentVictimId);

        if (PhotonView.isMine && !IsBot && !shootingAudio.isPlaying)
            shootingAudio.Play();

        return true;
    }

    public override void SecondaryFire(GunShellInfo.ShellType shellType, int targetId, Vector3 aimPointLocalToTarget)
    {
        MarkActivity();

        if (PhotonView.isMine)
            BattleGUI.FireButtons[DefaultShellType].SimulateReloading();

        weapons[shellType].RegisterShot();

        if (shootAnimation)
            shootAnimation.Play();

        Transform currentLaunchPoint;

        switch (shellType)
        {
            case GunShellInfo.ShellType.Missile_SACLOS:
                currentLaunchPoint = rocketLaunchPoints[currentRocketLaunchPointIndex];
                currentRocketLaunchPointIndex = (int)Mathf.Repeat(++currentRocketLaunchPointIndex, rocketLaunchPoints.Count);
                break;

            case GunShellInfo.ShellType.IRCM:
                currentLaunchPoint = ircmLaunchPoint;
                break;

            default:
                currentLaunchPoint = shootEffectPoints[currentShotPointIndex];
                currentShotPointIndex = (int)Mathf.Repeat(++currentShotPointIndex, shootEffectPoints.Count);
                break;
        }

        Quaternion rotation
            = !TargetAimed
                ? currentLaunchPoint.rotation
                : Quaternion.LookRotation(
                    forward:    (aimPointInfo.point - currentLaunchPoint.position).normalized,
                    upwards:    currentLaunchPoint.up);

        EffectPoolDispatcher.GetFromPool(
            _effect:        shotPrefab,
            _position:      currentLaunchPoint.position,
            _rotation:      currentLaunchPoint.rotation,
            useEffectMover: true,
            moverTarget:    currentLaunchPoint);

        GunShellInfo shellInfo = GunShellInfo.GetShellInfoForType(shellType);

        Shell shell
            = ShellPoolManager.GetShell(
                shellName:  shellInfo.shellPrefabName,
                position:   currentLaunchPoint.position,
                rotation:   rotation);

        shell.OwnerSpeed = Mathf.Abs(currentSpeed);
        continuousFire = shellInfo.continuousFire;

        shell.Activate(this, data.rocketAttack, hitMask, targetId, shellType);

        AudioDispatcher.PlayClipAtPosition(shell.ShotSound, currentLaunchPoint.position);

        if (PhotonView.isMine && shellType == GunShellInfo.ShellType.IRCM)
            Dispatcher.Send(EventId.IRCMLaunched, new EventInfo_SimpleEvent());

        if (PhotonView.isMine && shellType == GunShellInfo.ShellType.Missile_SACLOS)
            Dispatcher.Send(EventId.SACLOSLaunched, new EventInfo_SimpleEvent());
    }

    /* PRIVATE SECTION */

    public override void UpdateBotPrefabs(VehicleController nativeController)
    {
    }

    protected override void OnTargetAimed(EventId id, EventInfo ei)
    {
        if (!IsMain)
            return;

        Dispatcher.Send(
            id:     EventId.SACLOSLaunchRequired,
            info:   new EventInfo_B(TargetAimed && weapons[GunShellInfo.ShellType.Missile_SACLOS].IsReady));
    }

    protected override void OnVehicleTakesDamage(EventId id, EventInfo ei)
    {
        base.OnVehicleTakesDamage(id, ei);

        if (!PhotonView.isMine)
            return;

        EventInfo_U info = (EventInfo_U)ei;

        if (!IsCrashing)
            lastAttackerId = (int)info[2];

        if (IsAvailable && Armor <= MaxArmor * MORTAL_ARMOR_RATIO && !IsCrashing)
            IsCrashing = true;
    }

    protected override void OnStartBurstFire(EventId eid, EventInfo ei) { }

    protected override void OnStopBurstFire(EventId eid, EventInfo ei) { }

    public override void Aiming()
    {
        if (IsCrashing)
        {
            ResetGunsight();
            return;
        }

        float sqrTargetDistance = sqrMaxAimDistance;
        bool aimPointFound = false;

        if (TargetCanBeCaptured(Target, ref sqrTargetDistance))
        {
            aimPointInfo = new AimPointInfo(Target.transform.position, Target);
            aimPointFound = true;
        }
        else
        {
            foreach (var vehicle in BattleController.allVehicles.Values)
            {
                if (TargetCanBeCaptured(vehicle, ref sqrTargetDistance))
                {
                    aimPointInfo = new AimPointInfo(point: vehicle.transform.position, target: vehicle);
                    aimPointFound = true;
                }
            }
        }

        if (!aimPointFound)
        {
            ResetGunsight();
            return;
        }

        TargetPosition = aimPointInfo.point;

        if (!IsBot)
            BattleGUI.ShowGunSightForWorld(TargetPosition, Mathf.Sqrt(sqrTargetDistance));

        if (Target == aimPointInfo.target)
            return;

        if (Target != null)
            ResetGunsight();

        Target = aimPointInfo.target;

        if (!IsBot)
            Target.SetMarkedStatus(true);

        Dispatcher.Send(EventId.TargetAimed, new EventInfo_IIB(data.playerId, Target.data.playerId, true));
    }

    protected override void ApplyAvailability()
    {
        base.ApplyAvailability();

        if (shootingAudio != null && !isAvailable && shootingAudio.isPlaying)
            shootingAudio.Stop();
    }

    public override void StartBurst()
    {
        Dispatcher.Send(
            id:     EventId.StartBurstAimedFire,
            info:   new EventInfo_III(
                        data.playerId,
                        (int)primaryShellInfo.type,
                        TargetId),
            target: Dispatcher.EventTargetType.ToAll);
    }

    public override void StopBurst()
    {
        Dispatcher.Send(
            id:     EventId.StopBurstAimedFire,
            info:   new EventInfo_III(
                        data.playerId,
                        (int)primaryShellInfo.type,
                        TargetId),
            target: Dispatcher.EventTargetType.ToAll);
    }

    public override IEnumerator CheckOutOfMap()
    {
        yield break; // отключено по просьбе Славы
#pragma warning disable 162 // Ureachable code detected
        while (transform)
        {
            yield return new WaitForSeconds(checkOutOfMapDelay);

            worldMapCenterDirection = Map.MapCenterPos - transform.position;

            if (!Map.OutOfMapCol.bounds.Contains(transform.position))
                StartCoroutine(RotateToMapCenter());

            if (!Map.OutOfMapWarningCol.bounds.Contains(transform.position))
            {
                if (Vector3.Dot(transform.forward, worldMapCenterDirection) < 0 ||
                    transform.position.y > Map.OutOfMapCol.bounds.max.y)
                {
                    Notifier.Instance.ShowOutOfMapNotify();
                }
                else
                {
                    Notifier.Instance.StopOutOfMapNotify();
                }
            }
            else
            {
                Notifier.Instance.StopOutOfMapNotify();
            }
        }

        yield return null;
#pragma warning restore 162
    }

    protected override IEnumerator RotateToMapCenter()
    {
        // не используется, когда отключена корутина CheckOutOfMap
        while (!Map.OutOfMapCol.bounds.Contains(transform.position))
        {
            if (transform.position.y > Map.OutOfMapCol.bounds.max.y)
            {
                rb.AddForce(Vector3.down * backToMapForceValue, ForceMode.Force);
            }
            else
            {
                var dotProdForward = Vector3.Dot(worldMapCenterDirection, Vector3.forward);
                var dotProdRight = Vector3.Dot(worldMapCenterDirection, Vector3.right);

                Vector3 forceDir;

                if (Mathf.Abs(dotProdForward) > Mathf.Abs(dotProdRight))
                    forceDir = Mathf.Sign(dotProdForward) * Vector3.forward;
                else
                    forceDir = Mathf.Sign(dotProdRight) * Vector3.right;

                rb.AddForce(forceDir * backToMapForceValue, ForceMode.Force);

                rb.rotation
                    = Quaternion.RotateTowards(
                        from:               rb.rotation,
                        to:                 Quaternion.LookRotation(worldMapCenterDirection, Vector3.up),
                        maxDegreesDelta:    stabilizationSpeed * Time.deltaTime);
            }

            yield return null;
        }
    }

    private void OnSecondaryWeaponUsed(EventId id, EventInfo ei)
    {
        EventInfo_IIIV info = (EventInfo_IIIV)ei;

        int playerId = info.int1;
        int targetId = info.int3;
        GunShellInfo.ShellType shellType = (GunShellInfo.ShellType)info.int2;
        Vector3 aimPointLocalToTarget = info.vector;

        targetId = shellType == GunShellInfo.ShellType.IRCM ? BattleController.DEFAULT_TARGET_ID : targetId;

        if (playerId == PhotonView.ownerId)
            SecondaryFire(shellType, targetId, aimPointLocalToTarget);
    }

    private void OnHelicopterHealthChanged(EventId id, EventInfo ei)
    {
        SetDamageEffects();
    }

    private void OnShellStateChanged(EventId id, EventInfo ei)
    {
        EventInfo_BIIII info = (EventInfo_BIIII)ei;

        if ((GunShellInfo.ShellType)info.int3 != GunShellInfo.ShellType.Missile_SACLOS || info.int1 != PhotonView.ownerId)
            return;

        int shellId = info.int4;

        int foundThreat;

        if (!threats.TryGetValue(shellId, out foundThreat))
        {
            if (info.bool1)
            {
                threats[shellId] = info.int2;

                if (info.int1 != BattleController.MyPlayerId)
                    return;

                bool ircmIsReady = weapons[GunShellInfo.ShellType.IRCM].IsReady;

                //Notifier.ShowBonus(
                //    sprite:     "bonus_attack", // TODO: заменить спрайт на подходящий.
                //    topText:    Localizer.GetText(ircmIsReady ? "lblUseProtection" : "lblMissileThreat"),
                //    amount:     0);

                Dispatcher.Send(EventId.MissileThreat, new EventInfo_B(true));

                if (ircmIsReady)
                    Dispatcher.Send(EventId.IRCMLaunchRequired, new EventInfo_B(true));
            }
        }
        else
        {
            if (info.bool1)
                return;

            threats.Remove(shellId);

            Dispatcher.Send(EventId.IRCMLaunchRequired, new EventInfo_B(false));

            if (threats.Count > 0)
            {
                //Notifier.ShowBonus(
                //    sprite:     "bonus_attack", // TODO: заменить спрайт на подходящий.
                //    topText:    Localizer.GetText("lblMissileThreat"),
                //    amount:     0);

                return;
            }

            Dispatcher.Send(EventId.MissileThreat, new EventInfo_B(false));
        }
    }

    private void OnMissileThreat(EventId id, EventInfo ei)
    {
        bool threaten = ((EventInfo_B)ei).bool1;

        // Сейчас сигнал отрубается и после пуска вспышки, и если ракетная угроза прекратилась.
        if (!threaten && rocketThreatAudio != null && rocketThreatAudio.isPlaying)
            rocketThreatAudio.Stop();

        if (threaten && rocketThreatAudio != null && !rocketThreatAudio.isPlaying)
            rocketThreatAudio.Play();
    }

    private void OnIRCMLaunched(EventId id, EventInfo ei)
    {
        if (!PhotonView.isMine)
            return;

        if (rocketThreatAudio != null && rocketThreatAudio.isPlaying)
            rocketThreatAudio.Stop();
    }

    private void OnSACLOSReloaded(EventId id, EventInfo ei)
    {
        var eii = (EventInfo_I)ei;
        if ((GunShellInfo.ShellType)(eii.int1) != GunShellInfo.ShellType.Missile_SACLOS)
            return;
        if (TargetAimed && weapons[GunShellInfo.ShellType.Missile_SACLOS].IsReady)
            Dispatcher.Send(EventId.SACLOSLaunchRequired, new EventInfo_B(true));
    }

    private void OnStartBurstAimedFire(EventId id, EventInfo ei)
    {
        EventInfo_III info = (EventInfo_III)ei;

        if (info.int1 != PhotonView.ownerId)
            return;

        burst = true;

        currentVictimId = info.int3;

        if ((int)primaryShellInfo.type != info.int2 && PhotonView.isMine)
            primaryShellInfo = GunShellInfo.GetShellInfoForType((GunShellInfo.ShellType)info.int2);
    }

    private void OnStopBurstAimedFire(EventId id, EventInfo ei)
    {
        EventInfo_III info = (EventInfo_III)ei;

        if (info.int1 != PhotonView.ownerId)
            return;

        burst = false;

        currentVictimId = info.int3;
    }

    private void OnVehicleCrashing(EventId id, EventInfo ei)
    {
        EventInfo_I info = (EventInfo_I)ei;

        if (info.int1 != data.playerId)
            return;

        rb.freezeRotation = false;

        LastPositionAlive = transform.position;

        crashingRoutine = Crashing();

        StartCoroutine(crashingRoutine);

        Dispatcher.Send(
            id:     EventId.HelicopterKilled,
            info:   new EventInfo_IIV(data.playerId, lastAttackerId, LastPositionAlive),
            target: Dispatcher.EventTargetType.ToAll);
    }

    private IEnumerator Crashing()
    {
        float strenghtRatio = 0;

        while (!IsExploded)
        {
            strenghtRatio += Time.deltaTime * CRASH_SMOOTHING_RATIO;

            strenghtRatio = Mathf.Clamp01(strenghtRatio);

            rb.AddForce(
                force:  Vector3.down * FALL_FORCE * Time.deltaTime * strenghtRatio,
                mode:   ForceMode.VelocityChange);

            rb.transform.Rotate(
                axis:       Vector3.up,
                angle:      CRASHING_Y_ROTATION_QUALIFIER * Time.deltaTime * strenghtRatio,
                relativeTo: Space.Self);

            rb.transform.Rotate(
                axis:       Vector3.right * Mathf.Sign(XAxisAcceleration),
                angle:      CRASHING_Z_ROTATION_QUALIFIER * Time.deltaTime * strenghtRatio,
                relativeTo: Space.Self);

            yield return null;
        }
    }

    private IEnumerator Shaking()
    {
        while (gameObject)
        {
            rb.angularVelocity
                += Vector3.right * UnityEngine.Random.Range(-xShakingVelocity, xShakingVelocity)
                    + Vector3.up * UnityEngine.Random.Range(-shakingVelocity, shakingVelocity)
                    + Vector3.forward * UnityEngine.Random.Range(-shakingVelocity, shakingVelocity);

            yield return new WaitForFixedUpdate();
        }
    }

    public void UseSecondaryWeapon(GunShellInfo.ShellType shellType)
    {
        if (!weapons[shellType].IsReady)
            return;

        Dispatcher.Send(
            id:     EventId.SecondaryWeaponUsed,
            info:   new EventInfo_IIIV(
                        PhotonView.ownerId,
                        (int)shellType,
                        TargetId,
                        AimPointLocalToTarget),
            target: Dispatcher.EventTargetType.ToAll);
    }

    private void SetRocketGuidanceAudio()
    {
        if (!PhotonView.isMine)
            return;

        rocketGuidanceAudio = rocketGuidanceAudio ?? gameObject.AddComponent<AudioSource>();

        rocketGuidanceAudio.clip = rocketGuidanceSound;
        rocketGuidanceAudio.loop = true;
        rocketGuidanceAudio.rolloffMode = AudioRolloffMode.Linear;
        rocketGuidanceAudio.volume = Settings.SoundVolume;
        rocketGuidanceAudio.spatialBlend = PhotonView.isMine ? 0 : 1;
        rocketGuidanceAudio.maxDistance = DEFAULT_SOUND_DISTANCE;
        rocketGuidanceAudio.dopplerLevel = 0;
    }

    private void SetRocketThreatAudio()
    {
        if (!PhotonView.isMine)
            return;

        rocketThreatAudio = rocketThreatAudio ?? gameObject.AddComponent<AudioSource>();

        rocketThreatAudio.clip = rocketThreatSound;
        rocketThreatAudio.loop = true;
        rocketThreatAudio.rolloffMode = AudioRolloffMode.Linear;
        rocketThreatAudio.volume = Settings.SoundVolume;
        rocketThreatAudio.spatialBlend = PhotonView.isMine ? 0 : 1;
        rocketThreatAudio.maxDistance = DEFAULT_SOUND_DISTANCE;
        rocketThreatAudio.dopplerLevel = 0;
    }

    private void SetShootingAudio()
    {
        if (!PhotonView.isMine || IsBot)
            return;

        shootingAudio = shootingAudio ?? gameObject.AddComponent<AudioSource>();

        shootingAudio.clip = shootingSound;
        shootingAudio.loop = true;
        shootingAudio.rolloffMode = AudioRolloffMode.Linear;
        shootingAudio.volume = Settings.SoundVolume;
        shootingAudio.spatialBlend = PhotonView.isMine ? 0 : 1;
        shootingAudio.maxDistance = DEFAULT_SOUND_DISTANCE;
        shootingAudio.dopplerLevel = 0;
    }

    private float Accelerate(float oldSpeed, float newSpeed, float step, float inertionRatio)
    {
        if (newSpeed < oldSpeed && newSpeed >= 0 && oldSpeed > 0 ||
            newSpeed > oldSpeed && newSpeed <= 0 && oldSpeed < 0 /*||
            !HelpTools.Approximately(Mathf.Sign(newSpeed), Mathf.Sign(oldSpeed)) && Mathf.Abs(oldSpeed) > 0 && Mathf.Abs(newSpeed) > 0)*/)
        {
            step *= inertionRatio;
        }
        else
        {
            step *= inertionRatio * INERTION_MULTIPLIER;
        }

        return Mathf.MoveTowards(oldSpeed, newSpeed, step);
    }

    private void SetDamageEffects()
    {
        float healthRatio = Armor / (float)MaxArmor;

        if (fireEffect)
        {
            foreach (var fire in fireEffect.GetComponentsInChildren<ParticleSystem> ()) {
                var ma = fire.main;
                ma.startColor
                    = Color.Lerp (
                        a: FIRE_START_COLOR,
                        b: FIRE_FINAL_COLOR,
                        t: 1 - healthRatio);
            }

            fireEffect.gameObject.SetActive(
                PhotonView.isMine &&
                !IsExploded &&
                qualityLevel > 1 &&
                healthRatio <= FIRE_START_HEALTH_RATIO_THRESHOLD);
        }

        if (smokeEffect)
        {
            foreach (var smoke in smokeEffect.GetComponentsInChildren<ParticleSystem> ()) {
                var ma = smoke.main;
                ma.startColor
                    = Color.Lerp (
                        a: SMOKE_START_COLOR,
                        b: SMOKE_FINAL_COLOR,
                        t: 1 - healthRatio);
            }

            smokeEffect.gameObject.SetActive(
                PhotonView.isMine &&
                !IsExploded &&
                qualityLevel > 1 &&
                healthRatio <= SMOKE_START_HEALTH_RATIO_THRESHOLD);
        }
    }

    private void SetWaterEffects()
    {
        if (MapParticles.Instance.WaterTrail == null || !PhotonView.isMine || IsExploded || qualityLevel <= 1)
            return;

        RaycastHit hit;

        if (Physics.Raycast(
            /* origin:      */  transform.position,
            /* direction:   */  -transform.up,
            /* hitInfo:     */  out hit,
            /* maxDistance: */  WATER_EFFECT_DISTANCE,
            /* layerMask:   */  MiscTools.GetLayerMask(Layer.Key.Water)))
        {
            waterTrailEffect.gameObject.SetActive(true);

            waterTrailEffect.transform.position = hit.point;

            waterTrailEffect.transform.LookAt(
                worldPosition:  hit.normal,
                worldUp:        Vector3.right);

            waterTrailEffect.SetChildrenAlpha(1 - (hit.distance / WATER_EFFECT_DISTANCE));

            return;
        }

        waterTrailEffect.gameObject.SetActive(false);
    }

    private void AssignEnvironmentEffects()
    {
        if (MapParticles.Instance.WaterTrail == null)
            return;

        waterTrailEffect = Instantiate(MapParticles.Instance.WaterTrail);

        waterTrailEffect.transform.parent = shipTransform;
        waterTrailEffect.transform.localPosition = Vector3.zero;
        waterTrailEffect.transform.localRotation = Quaternion.identity;

        waterTrailEffect.gameObject.SetActive(false);
    }

    private bool TargetCanBeCaptured(VehicleController vehicle, ref float checkedSqrTargetDistance)
    {
        if (vehicle == null || vehicle == this || !vehicle.IsAvailable)
            return false;

        Vector3 vehDeltaPos = vehicle.transform.position - transform.position;
        float sqrTargetDistance = Vector3.SqrMagnitude(vehDeltaPos);

        if (sqrTargetDistance > checkedSqrTargetDistance || Vector3.Dot(ShotPoint.forward, vehDeltaPos.normalized) < minAimAngleCos)
            return false;

        checkedSqrTargetDistance = sqrTargetDistance;

        return true;
    }
}
