#if !(UNITY_STANDALONE_OSX || UNITY_WEBPLAYER || UNITY_WEBGL)
    #define TOUCH_SCREEN
#endif

using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using CodeStage.AntiCheat.ObscuredTypes;
using XD;
using XDevs.LiteralKeys;

public class HelicopterController : FlightController
{
    [Header("Настройки для вертолёта")]

    [Header("Ссылки")]    
    public Transform ircmLaunchPoint;    
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

    private bool isShooting;
    private bool isCrashing;
    private int qualityLevel;
    private int currentRocketLaunchPointIndex;
    private int currentTargetId;
    private int lastAttackerId;
    private int currentVictimId;
    private float xAxisAcceleration;
    private float xAxisAltAcceleration;
    private float yAxisAcceleration;
    private float yAxisAltAcceleration;
    private float input;
    private float currentAcceleration;
    private Vector3 requiredLocalVelocity;
    private RaycastHit obstacleHitInfo;
    private ParticleSystemWrapper waterTrailEffect;
    private AudioSource rocketGuidanceAudio;
    private AudioSource rocketThreatAudio;
    private AudioSource shootingAudio;
    private IEnumerator crashingRoutine;
    private int layerMask;

    public override bool IsCrashing
    {
        get
        {
            if (!PhotonView.isMine)
                return isCrashing = IsAvailable && HPSystem.Armor <= MaxArmor * MORTAL_ARMOR_RATIO;

            return isCrashing;
        }
        set
        {
            if (!PhotonView.isMine)
                return;

            isCrashing = value;

            rigidbody.useGravity = isCrashing;

            if (!isCrashing)
                return;

            Dispatcher.Send(EventId.VehicleCrashing, new EventInfo_I(data.playerId));
        }
    }

    public override float ThrottleLevelInputAxis
    {
        get { return 0.0f; }
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
            return isShooting = FireButtonPressed && !StaticContainer.UI.IsWindowOnScreen && !IsBot;
        }
    }

    protected override bool IsRequireSecondaryFire
    {
        get
        {
            return TargetAimed && FireButtonPressed && !StaticContainer.UI.IsWindowOnScreen && !IsBot;
        }
    }

    // Turn Horizontal.
    public override float XAxisControl
    {
        get
        {
            if (StaticContainer.UI.IsWindowOnScreen)
                return 0;

            if (ProfileInfo.ControlOption == ControlOption.gyroscope)
                return Input.acceleration.x * JoystickManager.Instance.HorizontalGyroQualifier;

            return XDevs.Input.GetAxis("Turn Left/Right");
        }
    }

    // Move Forward/Backward.
    public override float YAxisControl
    {
        get
        {
            if (StaticContainer.UI.IsWindowOnScreen)
                return 0;

            if (ProfileInfo.ControlOption == ControlOption.gyroscope)
                return (Input.acceleration.y - global::Settings.InitialAcceleration.y) * JoystickManager.Instance.VerticalGyroQualifier;

            return XDevs.Input.GetAxis("Move Forward/Backward");
        }
    }

    // Strafe Horizontal.
    protected override float XAxisAltControl
    {
        get
        {
            if (StaticContainer.UI.IsWindowOnScreen)
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
    protected override float YAxisAltControl
    {
        get
        {
            float rInp = XDevs.Input.GetAxis("Move Up/Down");

            if (ProfileInfo.isSliderControl)
            {
                // В случае наэкранного слайдера приоритет у другого управления:
                if (Mathf.Approximately(rInp, 0))
                {
                    rInp = ThrottleLevel.Value;
                }
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
    
    protected override float CorrectionTime
    {
        get { return CORRECTION_TIME; }
    }
    
    protected override Vector3 IndicatorDeltaOffset
    {
        get { return Vector3.up * 5.0f; }
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

            return (IsTargetOfSACLOS || !StaticContainer.Profile.BattleTutorialCompleted || multipleClicks) &&
                   IRCMButtonPressed &&
                   !StaticContainer.UI.IsWindowOnScreen &&
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

    protected override void OnPhotonInstantiate(PhotonMessageInfo info)
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

    protected override void NormalUpdate()
    {
        base.NormalUpdate();

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

            requiredSpeed = Settings[Setting.MovingSpeed].Max * input;
            currentSpeed = Settings[Setting.MovingSpeed].Max * currentAcceleration;
        }
        else
        {
            float correctSpeed = correctVelocity.magnitude;

            if (!Mathf.Approximately(currentSpeed, correctSpeed))
            {
                currentSpeed = Mathf.MoveTowards(current: currentSpeed, target: correctVelocity.magnitude, maxDelta: acceleration * Time.deltaTime);
            }

            if (burst)
            {
                PrimaryFire(shotPoint.rotation);
            }
        }

        if (!HelpTools.Approximately(accelerationDirection, 0))
        {
            MarkActivity();
        }

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
            PrimaryFire(shotPoint.rotation);

        if (PhotonView.isMine && !IsBot && !isShooting && shootingAudio.isPlaying)
        {
            shootingAudio.Stop();

            AudioDispatcher.PlayClipAtPosition(
                clip:       shootingEndSound,
                position:   transform.position);
        }
    }

    protected override void PhysicsUpdate()
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
            rigidbody.freezeRotation = true;
        }
        else
        {
            Dispatcher.Send(id: EventId.TankKilled, info: new EventInfo_II(data.playerId, lastAttackerId), target: Dispatcher.EventTargetType.ToAll);
        }
    }

    void OnCollisionExit(Collision collision)
    {
        if (!PhotonView.isMine)
            return;

        if (!IsCrashing)
            rigidbody.freezeRotation = false;
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
        {
            return;
        }

        if (!Mathf.Approximately(XAxisControl + XAxisAltControl + YAxisControl + YAxisAltControl, 0))
        {
            MarkActivity();
        }

        requiredLocalVelocity = LocalVelocity;

        requiredLocalVelocity.x = xSpeedRatio * Settings[Setting.MovingSpeed].Max * XAxisAltAcceleration;
        requiredLocalVelocity.z = zSpeedRatio * Settings[Setting.MovingSpeed].Max * YAxisAcceleration;

        rigidbody.velocity = rigidbody.transform.TransformDirection(requiredLocalVelocity);

        rigidbody.rotation
            = Quaternion.Lerp(
                a: rigidbody.rotation,
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

        rigidbody.velocity
            = new Vector3(
                x: rigidbody.velocity.x,
                y: ySpeedRatio * Settings[Setting.MovingSpeed].Max * YAxisAltAcceleration,
                z: rigidbody.velocity.z);

        SetEngineNoise(Mathf.Abs(currentSpeed / Settings[Setting.MovingSpeed].Max));

        if (Physics.Raycast(
                /* ray:         */  new Ray(transform.position, Vector3.ProjectOnPlane(rigidbody.velocity, Vector3.up)),
                /* hitInfo:     */  out obstacleHitInfo,
                /* maxDistance: */  minDistanceToObstacle,
                /* layerMask:   */  layerMask))
        {
            rigidbody.velocity += Vector3.Project(-rigidbody.velocity, obstacleHitInfo.normal);
        }

        if (Physics.Raycast(
                /* ray:         */  new Ray(transform.position, Vector3.up),
                /* hitInfo:     */  out obstacleHitInfo,
                /* maxDistance: */  minDistanceToObstacle,
                /* layerMask:   */  layerMask))
        {
            var vel = rigidbody.velocity;
            vel.y = vel.y > 0 ? 0 : vel.y;
            rigidbody.velocity = vel;
        }

        StoreVehiclePosition();
    }

    [PunRPC]
    public override void Respawn(Vector3 position, Quaternion rotation, bool restoreLife, bool firstTime)
    {
        base.Respawn(position, rotation, restoreLife, firstTime);

        currentTargetId = BattleController.DEFAULT_TARGET_ID;

        threats.Clear();

        IsCrashing = false;
    }

    public override bool PrimaryFire(Quaternion rotation)
    {       
        return true;
    }

    public override void SecondaryFire(GunShellInfo.ShellType shellType, int targetId)
    {      
    }

    /* PRIVATE SECTION */

    public override void UpdateBotAssets(VehicleController nativeController)
    {
    }

    protected override void OnTargetAimed(EventId id, EventInfo ei)
    {
        if (!PhotonView.isMine || IsBot)
            return;

        EventInfo_IIB info = (EventInfo_IIB)ei;

        TargetAimed = info.bool1;
        currentTargetId = info.int2;

        Dispatcher.Send(
            id:     EventId.SACLOSLaunchRequired,
            info:   new EventInfo_B(TargetAimed && weapons[GunShellInfo.ShellType.Missile_SACLOS].IsReady));
    }

    protected override void OnVehicleTakesDamage(EventId id, EventInfo ei)
    {
        EventInfo_U info = (EventInfo_U)ei;

        int damage = (int)info[1];
        
        if (!PhotonView.isMine)
            return;

        if (!IsCrashing)
            lastAttackerId = (int)info[2];

        if (IsAvailable && HPSystem.Armor <= MaxArmor * MORTAL_ARMOR_RATIO && !IsCrashing)
            IsCrashing = true;

        if (IsBot)
        {
            Dispatcher.Send(EventId.TankHealthChanged, new EventInfo_II(data.playerId, HPSystem.Armor));
            return;
        }

        switch ((GunShellInfo.ShellType)(int)info[3])
        {
            case GunShellInfo.ShellType.Missile_SACLOS:
                BattleStatisticsManager.BattleStats["TakenDamage_SACLOS"] += damage;
                BattleStatisticsManager.BattleStats["TakenHits_SACLOS"]++;
                break;

            case GunShellInfo.ShellType.Usual:
                BattleStatisticsManager.BattleStats["TakenDamage"] += damage;
                BattleStatisticsManager.BattleStats["TakenHits"]++;
                break;
        }
    }

    protected override void OnStartBurstFire(EventId eid, EventInfo ei) { }

    protected override void OnStopBurstFire(EventId eid, EventInfo ei) { }


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
                        TargetAimed ? currentTargetId : BattleController.DEFAULT_TARGET_ID),
            target: Dispatcher.EventTargetType.ToAll);
    }

    public override void StopBurst()
    {
        Dispatcher.Send(
            id:     EventId.StopBurstAimedFire,
            info:   new EventInfo_III(
                        data.playerId,
                        (int)primaryShellInfo.type,
                        TargetAimed ? currentTargetId : BattleController.DEFAULT_TARGET_ID),
            target: Dispatcher.EventTargetType.ToAll);
    }

    protected override IEnumerator CheckOutOfMap()
    {
        yield break; // отключено по просьбе Славы
        while (transform)
        {
            yield return new WaitForSeconds(checkOutOfMapDelay);

            worldMapCenterDirection = mapCenterPos - transform.position;

            if (!outOfMapCol.bounds.Contains(transform.position))
                StartCoroutine(RotateToMapCenter());

            if (!outOfMapWarningCol.bounds.Contains(transform.position))
            {
                if (Vector3.Dot(transform.forward, worldMapCenterDirection) < 0 ||
                    transform.position.y > outOfMapCol.bounds.max.y)
                {
                    //TODO: на наш механизм
                    //Notifier.Instance.ShowOutOfMapNotify();
                }
                else
                {
                    //TODO: на наш механизм
                    //Notifier.Instance.StopOutOfMapNotify();
                }
            }
            else
            {
                //TODO: на наш механизм
                //Notifier.Instance.StopOutOfMapNotify();
            }
        }

        yield return null;
    }

    protected override IEnumerator RotateToMapCenter()
    {
        // не используется, когда отключена корутина CheckOutOfMap
        while (!outOfMapCol.bounds.Contains(transform.position))
        {
            if (transform.position.y > outOfMapCol.bounds.max.y)
            {
                rigidbody.AddForce(Vector3.down * backToMapForceValue, ForceMode.Force);
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

                rigidbody.AddForce(forceDir * backToMapForceValue, ForceMode.Force);

                rigidbody.rotation
                    = Quaternion.RotateTowards(
                        from:               rigidbody.rotation,
                        to:                 Quaternion.LookRotation(worldMapCenterDirection, Vector3.up),
                        maxDegreesDelta:    stabilizationSpeed * Time.deltaTime);
            }

            yield return null;
        }
    }

    private void OnSecondaryWeaponUsed(EventId id, EventInfo ei)
    {
        EventInfo_III info = (EventInfo_III)ei;

        GunShellInfo.ShellType shellType = (GunShellInfo.ShellType)info.int2;

        if (info.int1 != PhotonView.ownerId)
            return;

        SecondaryFire(
            shellType:  shellType,
            targetId:   shellType == GunShellInfo.ShellType.IRCM ? BattleController.DEFAULT_TARGET_ID : info.int3);
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

                if (info.int1 != StaticType.BattleController.Instance<IBattleController>().MyPlayerId)
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

        rigidbody.freezeRotation = false;

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

            rigidbody.AddForce(
                force:  Vector3.down * FALL_FORCE * Time.deltaTime * strenghtRatio,
                mode:   ForceMode.VelocityChange);

            rigidbody.transform.Rotate(
                axis:       Vector3.up,
                angle:      CRASHING_Y_ROTATION_QUALIFIER * Time.deltaTime * strenghtRatio,
                relativeTo: Space.Self);

            rigidbody.transform.Rotate(
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
            rigidbody.angularVelocity
                += Vector3.right * UnityEngine.Random.Range(-xShakingVelocity, xShakingVelocity)
                    + Vector3.up * UnityEngine.Random.Range(-shakingVelocity, shakingVelocity)
                    + Vector3.forward * UnityEngine.Random.Range(-shakingVelocity, shakingVelocity);

            yield return new WaitForFixedUpdate();
        }
    }

    private void UseSecondaryWeapon(GunShellInfo.ShellType shellType)
    {
        if (!weapons[shellType].IsReady)
            return;

        Dispatcher.Send(
            id:     EventId.SecondaryWeaponUsed,
            info:   new EventInfo_III(
                        PhotonView.ownerId,
                        (int)shellType,
                        currentTargetId),
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
        rocketGuidanceAudio.volume = global::Settings.SoundVolume;
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
        rocketThreatAudio.volume = global::Settings.SoundVolume;
        rocketThreatAudio.spatialBlend = PhotonView.isMine ? 0 : 1;
        rocketThreatAudio.maxDistance = DEFAULT_SOUND_DISTANCE;
        rocketThreatAudio.dopplerLevel = 0;
    }

    private void SetShootingAudio()
    {
        if (!PhotonView.isMine || IsBot)
            return;

        shootingAudio = shootingAudio ?? gameObject.AddComponent<AudioSource>();
        SetAudioParams(ref shootingAudio, shootingSound, true, AudioRolloffMode.Linear, DEFAULT_SOUND_DISTANCE, 0, global::Settings.SoundVolume, PhotonView.isMine ? 0 : 1);
    }

    private float Accelerate(float oldSpeed, float newSpeed, float step, float inertionRatio)
    {
        step *= (newSpeed < oldSpeed && newSpeed >= 0 && oldSpeed > 0 ||newSpeed > oldSpeed && newSpeed <= 0 && oldSpeed < 0 /*||
            !HelpTools.Approximately(Mathf.Sign(newSpeed), Mathf.Sign(oldSpeed)) && Mathf.Abs(oldSpeed) > 0 && Mathf.Abs(newSpeed) > 0)*/)?
             inertionRatio: inertionRatio * INERTION_MULTIPLIER;
        return Mathf.MoveTowards(oldSpeed, newSpeed, step);
    }

    private void SetDamageEffects()
    {
        float healthRatio = HPSystem.Armor / (float)MaxArmor;

        if (fireEffect)
        {
            foreach (var fire in fireEffect.GetComponentsInChildren<ParticleSystem>())
                fire.startColor
                    = Color.Lerp(
                        a:  FIRE_START_COLOR,
                        b:  FIRE_FINAL_COLOR,
                        t:  1 - healthRatio);

            fireEffect.gameObject.SetActive(
                PhotonView.isMine &&
                !IsExploded &&
                qualityLevel > 1 &&
                healthRatio <= FIRE_START_HEALTH_RATIO_THRESHOLD);
        }

        if (smokeEffect)
        {
            foreach (var smoke in smokeEffect.GetComponentsInChildren<ParticleSystem>())
                smoke.startColor
                    = Color.Lerp(
                        a:  SMOKE_START_COLOR,
                        b:  SMOKE_FINAL_COLOR,
                        t:  1 - healthRatio);

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
        {
            return;
        }

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
            waterTrailEffect.transform.LookAt(worldPosition:  hit.normal, worldUp: Vector3.right);
            waterTrailEffect.SetChildrenAlpha(1 - (hit.distance / WATER_EFFECT_DISTANCE));

            return;
        }

        waterTrailEffect.gameObject.SetActive(false);
    }

    private void AssignEnvironmentEffects()
    {
        if (MapParticles.Instance.WaterTrail == null)
        {
            return;
        }

        waterTrailEffect = Instantiate(MapParticles.Instance.WaterTrail);
        waterTrailEffect.transform.parent = shipTransform;
        waterTrailEffect.transform.localPosition = Vector3.zero;
        waterTrailEffect.transform.localRotation = Quaternion.identity;
        waterTrailEffect.gameObject.SetActive(false);
    }
}
