#if (UNITY_IPHONE || UNITY_ANDROID || UNITY_WP8) && !UNITY_EDITOR
    #define TOUCH_SCREEN
#endif

using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using XDevs.LiteralKeys;

using Hashtable = ExitGames.Client.Photon.Hashtable;

public class CarController : VehicleController
{
    public const float ACTIVITY_THRESHOLD = 0.01f;

    [Header("Настройки для автомобилей")]

    [Header("Ссылки")]
    public Transform cannonEnd;
    public Transform indicatorPoint;
    public List<Transform> rocketLaunchPoints = null;

    [Tooltip("Не используется.")]
    public Transform bumper;

    public Transform critZones;
    public Collider bodyCollider;
    public Collider turretCollider;
    public EngineController engineController;
    public TransmissionController transmissionController;
    public SuspensionController suspensionController;
    public DustController dustController;

    [Header("Физика")]
    [Tooltip("Коэффициент скорости.")]
    public float speedRatio = 10.0f;

    [Tooltip("Коэффициент влияния террейна на скорость передвижения машины.")]
    public float climbingResistanceQualifier = 0.009f;

    [Tooltip("Центр тяжести (применяется сразу).")]
    public Vector3 newCenterOfMass;

    [Header("Звуки")]
    public float maxEnginePitch = 1.7f;
    public float maxEngineVolume = 1.8f;
    public AudioClip stuckSound;
    public AudioClip skidSound;
    public AudioClip[] crashSounds;
    public AudioClip[] jumpSounds;

    [Header("Ускорение")]
    [Tooltip(
        "Вторичный коэффициент разгона, с учётом трения (чем меньше, тем плавнее). " +
        "Первичный, разгон самого движка: EngineController.accelerationRatio.")]
    public float accelerationRatio = 0.5f;

    [Tooltip("Коэффициент скорости при заднем ходе.")]
    public float backwardsRatio = 0.65f;

    [Header("Торможение")]
    [Tooltip("Коэффициент торможения (чем меньше, тем плавнее).")]
    public float brakingRatio = 0.045f;

    [Header("Поворот")]
    [Tooltip("Коэффициент скорости поворота.")]
    public float rotationRatio = 0.15f;

    [Tooltip("Коэффициент скорости поворота при заднем ходе.")]
    public float backwardsRotationRatio = 2.15f;

    [Tooltip("Коэффициент скорости поворота при торможении.")]
    public float brakeRotationRatio = 0.5f;

    [Tooltip("Коэффициент набора скорости поворота.")]
    public float rotationAccelerationRatio = 0.1f;

    [Tooltip("Коэффициент набора скорости поворота при торможении.")]
    public float brakeRotationAccelerationRatio = 0.2f;

    [Header("Занос")]
    [Tooltip("Коэффициент заноса (эффект заметен только при значении от ~0.94).")]
    public float driftRatio = 0.955f;

    [Tooltip("Коэффициент заноса при торможении (эффект заметен только при значении от ~0.94).")]
    public float brakeDriftRatio = 0.980025f;

    [Header("Танковый поворот")]
    [Tooltip("Коэффициент скорости поворота при танкоподобном развороте.")]
    public float tankTurningRotationRatio = 1.5f;

    [Tooltip("Коэффициент набора скорости поворота при танкоподобном развороте.")]
    public float tankTurningRotationAccelerationRatio = 0.8f;

    [Header("Полицейский разворот")]
    [Tooltip(
        "Нужно только для упрощённого полицейского разворота (без газа). "
            + "Значение прогресса разгона, при котором машина начнёт разворачиваться на ручнике "
            + "(0 – машина ещё стоит, 1 – машина разогналась до максимальной скорости).")]
    public float arcadePoliceTurningAccelerationThreshold = 0.8f;

    [Header("Стрельба")]
    public float gunHeatPerShot = 0.045f;
    public float coolingSpeed = 0.25f;

    private const int MIN_GROUND_CONTACTS = 2;
    private const float CORRECTION_TIME = 0.8f;
    private const float MAX_SHOOT_ANGLE = 45.0f;
    private const float MIN_SPEED = 0.05f;
    private const float MIN_ROTATION_SPEED = 0.1f;
    private const float MAX_FORCED_LANDING_QUALIFIER = 60.0f;
    private const float MIN_FORCED_LANDING_QUALIFIER = 12.0f;
    private const float VERTICAL_AUTO_AIM = 20.0f;
    private const float HORIZONTAL_AUTO_AIM = 3.25f;
    private const float TURRET_SOUND_DISTANCE = 25.0f;
    private const float PRIMARY_HORIZONTAL_COLLISION_DETECTION_THRESHOLD = 0.38f;
    private const float SECONDARY_HORIZONTAL_COLLISION_DETECTION_THRESHOLD = 0.185f;
    private const float VERTICAL_COLLISION_DETECTION_THRESHOLD = 0.075f;
    private const float MAX_AUTOFIRE_DISTANCE_SQR = 60.0f * 60.0f;
    private const float TANK_TURNING_SPEED_RATIO = 0.0f;
    private const GunShellInfo.ShellType SECONDARY_WEAPON_SHELL = GunShellInfo.ShellType.Missile_Med;  // TODO: Снаряд вторичного оружия. Временная заглушка.


    #if TOUCH_SCREEN
    private const float INPUT_THRESHOLD = 0.5f;
    #endif

    private int groundContacts;
    private int currentShotPointIndex;
    private int currentRocketLaunchPointIndex;
    private int currentTargetId;
    private bool isFallen;
    private bool isMovingBackwards;
    private bool isBrakeButtonPressed;
    private bool isBraking;
    private bool isHandBraking;
    private bool isTankTurning;
    private bool isArcadePoliceTurning;
    private float joystickXAxis;
    private float joystickYAxis;
    private float currentRotationSpeed;
    private float climbingAngle;
    private float climbingResistance;
    private float affectedByTerrainMaxSpeed;
    private float affectedByTerrainAvailableSpeed;
    private float affectedByControlsAvailableSpeed;
    private float affectedByControlsRotationSpeed;
    private float verticalTransformVelocity;
    private float storedHorizontalTransformVelocity;
    private float storedVerticalTransformVelocity;
    private float deltaRotationMagnitude;
    private float deltaHorizontalTansformVelocity;
    private float deltaVericalTansformVelocity;
    private Vector3 requiredLocalVelocity;
    private Vector3 requiredLocalAngularVelocity;
    private Vector3 deltaRotationAxis;
    private Quaternion lastRotation;
    private AudioSource skidAudio;

    public override float WeaponReloadingProgress
    {
        get { return weapons[DefaultShellType].HeatingProgress; }
    }

    public override float GetHeating(GunShellInfo.ShellType shellType)
    {
        switch (shellType)
        {
            case GunShellInfo.ShellType.Usual:
                return gunHeatPerShot;

            default:
                return 0.0f;
        }
    }

    public override float GetCooling(GunShellInfo.ShellType shellType)
    {
        switch (shellType)
        {
            case GunShellInfo.ShellType.Usual:
                return coolingSpeed;

            default:
                return 0.0f;
        }
    }

    public override Transform Turret
    {
        get { return turret; }
    }

    public override Transform ShotPoint
    {
        get { return shotPoint; }
    }

    public override Transform CannonEnd
    {
        get { return cannonEnd; }
    }
    
    public override Vector3 AngularVelocity
    {
        get
        {
            if (PhotonView.isMine)
                return rb.angularVelocity;

            return ((deltaRotationAxis * deltaRotationMagnitude) / Time.deltaTime) * Mathf.Deg2Rad;
        }
    }

    public bool IsBraking
    {
        get { return isBraking; }
    }

    public bool IsHandBraking
    {
        get { return isHandBraking; }
    }

    public bool IsTankTurning
    {
        get { return isTankTurning; }
    }

    public bool IsOnGround
    {
        get; private set;
    }

    public float JoystickXAxis
    {
        get { return joystickXAxis; }
    }

    public float HorizontalTransformVelocity
    {
        get; private set;
    }

    float currentSpeed;
    override public float CurrentSpeed
    {
        get { return currentSpeed; }
    }

    public float AccelerationProgress
    {
        get { return Mathf.Abs(CurrentSpeed) / affectedByTerrainMaxSpeed; }
    }

    protected override bool FireButtonPressed
    {
        get
        {
            return continuousFire
                ? Input.GetButton("Fire1") || BattleGUI.FireButtons[DefaultShellType].fireButton.IsPressed // TODO: поменять ShellType.
                : Input.GetButtonDown("Fire1") || BattleGUI.FireButtons[DefaultShellType].fireButton.IsPressed;
        }
    }

    public override bool IsRequirePrimaryFire
    {
        get
        {
            return TargetAimed &&
                   !BattleGUI.IsWindowOnScreen &&
                   (transform.position - aimPointInfo.point).sqrMagnitude < MAX_AUTOFIRE_DISTANCE_SQR;
        }
    }

    public override bool IsRequireSecondaryFire
    {
        get
        {
            return !secondaryShellInfo.isPrimary &&
                   FireButtonPressed &&
                   !BattleGUI.IsWindowOnScreen;
        }
    }

    protected override bool NeedCorrectAimY
    {
        get { return true; }
    }

    protected override float OdometerRatio
    {
        get { return 1; }
    }

    protected override float SpeedRatio
    {
        get { return GameData.CurrentGame == Game.BlowOut ? speedRatio : 1; }
    }

    public override float MaxShootAngle
    {
        get { return MAX_SHOOT_ANGLE; }
    }

    protected override float CorrectionTime
    {
        get { return CORRECTION_TIME; }
    }

    protected override float VertAimCapture
    {
        get { return VERTICAL_AUTO_AIM; }
    }

    protected override float HorizAimCapture
    {
        get { return HORIZONTAL_AUTO_AIM; }
    }

    protected override Transform IndicatorPoint
    {
        get { return indicatorPoint; }
    }

    protected override Transform CritZones
    {
        get { return critZones; }
    }

    protected override Transform Bumper
    {
        get { return bumper; }
    }

    public override void OnPhotonInstantiate(PhotonMessageInfo info)
    {
        base.OnPhotonInstantiate(info);

        rb.interpolation = RigidbodyInterpolation.Interpolate;

        if (terrainLayer == 0)
            terrainLayer = LayerMask.NameToLayer(Layer.Items[Layer.Key.Terrain]);

        SetSkidAudio();

        Dispatcher.Subscribe(EventId.SecondaryWeaponUsed, OnSecondaryWeaponUsed);

        if (!PhotonView.isMine)
        {
            Dispatcher.Subscribe(EventId.StartBurstFire, OnStartBurstFire);
            Dispatcher.Subscribe(EventId.StopBurstFire, OnStopBurstFire);

            return;
        }

        CameraToHome();
    }

    protected override void Update()
    {
        base.Update();

        if (PhotonView.isMine && IsRequireSecondaryFire)
            UseSecondaryWeapon();

        if (burst)
            PrimaryFire();
    }

    protected override void FixedUpdate()
    {
        if (!PhotonView || !IsAvailable)
            return;

        if (PhotonView.isMine)
            MovePlayer();
        else
        {
            AnimateClone();
            StoreCloneRotation();
        }
    }

    void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.layer != terrainLayer)
            return;

        groundContacts++;

        IsOnGround = suspensionController.CheckGroundContacts(collision) || groundContacts > MIN_GROUND_CONTACTS;
    }

    void OnCollisionExit(Collision collision)
    {
        if (collision.gameObject.layer != terrainLayer)
            return;

        groundContacts--;

        IsOnGround = groundContacts != 0;
    }

    #if UNITY_EDITOR
    override protected void OnDrawGizmos()
    {
        base.OnDrawGizmos ();

        Gizmos.color = Color.magenta;
        Gizmos.DrawSphere(center: transform.TransformPoint(newCenterOfMass), radius: 0.1f);
    }
    #endif

    [PunRPC]
    public override void Respawn(Vector3 position, Quaternion rotation, bool restoreLife, bool firstTime)
    {
        base.Respawn(position, rotation, restoreLife, firstTime);
        if (PhotonView.isMine)
            CameraToHome();
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
        
        Quaternion rotation
            = !TargetAimed
                ? currentShotEffectPoint.rotation
                : Quaternion.LookRotation((aimPointInfo.point - currentShotEffectPoint.position).normalized, currentShotEffectPoint.up);

        EffectPoolDispatcher.GetFromPool(
            _effect:        shotPrefab,
            _position:      currentShotEffectPoint.position,
            _rotation:      currentShotEffectPoint.rotation,
            useEffectMover: true,
            moverTarget:    currentShotEffectPoint);

        Shell shell = ShellPoolManager.GetShell(primaryShellInfo.shellPrefabName, currentShotEffectPoint.position, rotation);
        continuousFire = primaryShellInfo.continuousFire;

        shell.Activate(this, data.attack, hitMask);

        AudioDispatcher.PlayClipAtPosition(shell.ShotSound, currentShotEffectPoint.position);

        return true;
    }

    public override void SecondaryFire(GunShellInfo.ShellType shellType, int targetId, Vector3 aimPointLocalToTarget)
    {
        MarkActivity();

        if (PhotonView.isMine)
            BattleGUI.FireButtons[shellType].SimulateReloading();

        weapons[shellType].RegisterShot();

        if (shootAnimation)
            shootAnimation.Play();

        Transform currentRocketLaunchPoint = rocketLaunchPoints[currentRocketLaunchPointIndex];

        currentRocketLaunchPointIndex = (int)Mathf.Repeat(++currentRocketLaunchPointIndex, shootEffectPoints.Count);

        Quaternion rotation
            = !TargetAimed
                ? currentRocketLaunchPoint.rotation
                : Quaternion.LookRotation((aimPointInfo.point - currentRocketLaunchPoint.position).normalized, currentRocketLaunchPoint.up);

        EffectPoolDispatcher.GetFromPool(
            _effect:        shotPrefab,
            _position:      currentRocketLaunchPoint.position,
            _rotation:      currentRocketLaunchPoint.rotation,
            useEffectMover: true,
            moverTarget:    currentRocketLaunchPoint);

        GunShellInfo shellInfo = GunShellInfo.GetShellInfoForType(shellType);

        Shell shell = ShellPoolManager.GetShell(shellInfo.shellPrefabName, currentRocketLaunchPoint.position, rotation);

        continuousFire = shellInfo.continuousFire;

        shell.Activate(owner: this, damage: data.rocketAttack, hitMask: hitMask, victimId: targetId, shellType: shellType); // TODO: нужно прописать поведение ракеты, а не активировать пулю с фейковым хитом.

        AudioDispatcher.PlayClipAtPosition(shell.ShotSound, currentRocketLaunchPoint.position);
    }

    public override void UpdateBotPrefabs(VehicleController nativeController)
    {
    }

    protected override void OnTargetAimed(EventId id, EventInfo ei)
    {
        EventInfo_IIB info = (EventInfo_IIB)ei;
        currentTargetId = info.int2;
    }

    protected override void SetEngineNoise(float t)
    {
        engineAudio.pitch = Mathf.Lerp(1.0f, maxEnginePitch, t);
        engineAudio.volume = Settings.SoundVolume * Mathf.Lerp(1.0f, maxEngineVolume, t);
    }

    protected override void SetEngineAudio()
    {
        engineAudio = gameObject.AddComponent<AudioSource>();

        engineAudio.clip = engineSound;
        engineAudio.loop = true;
        engineAudio.rolloffMode = AudioRolloffMode.Linear;
        engineAudio.volume = Settings.SoundVolume;
        engineAudio.spatialBlend = PhotonView.isMine ? 0 : 1;
        engineAudio.maxDistance = DEFAULT_SOUND_DISTANCE;
        engineAudio.dopplerLevel = 0;
        
        engineAudio.Play();
    }

    protected override void SetTurretAudio()
    {
        turretAudio = gameObject.AddComponent<AudioSource>();

        turretAudio.clip = turretRotationSound;
        turretAudio.loop = true;
        turretAudio.rolloffMode = AudioRolloffMode.Linear;
        turretAudio.volume = Settings.SoundVolume;
        turretAudio.spatialBlend = PhotonView.isMine ? 0 : 1;
        turretAudio.maxDistance = TURRET_SOUND_DISTANCE;
        turretAudio.dopplerLevel = 0;
    }

    protected override void OnDestroy()
    {
        Dispatcher.Unsubscribe(EventId.StartBurstFire, OnStartBurstFire);
        Dispatcher.Unsubscribe(EventId.StopBurstFire, OnStopBurstFire);
        Dispatcher.Unsubscribe(EventId.SecondaryWeaponUsed, OnSecondaryWeaponUsed);

        base.OnDestroy();
    }

    public override void StartBurst()
    {
        Dispatcher.Send(EventId.StartBurstFire, new EventInfo_II(data.playerId, /*(int)currentShellInfo.type*/(int)DefaultShellType), Dispatcher.EventTargetType.ToAll);
    }

    public override void StopBurst()
    {
        Dispatcher.Send(EventId.StopBurstFire, new EventInfo_II(data.playerId, (int)DefaultShellType), Dispatcher.EventTargetType.ToAll);
    }

    private void OnStartBurstFire(EventId eid, EventInfo ei)
    {
        EventInfo_II info = (EventInfo_II)ei;

        if (info.int1 != PhotonView.ownerId)
            return;

        burst = true;
        
        if ((int)primaryShellInfo.type != info.int2)
            primaryShellInfo = GunShellInfo.GetShellInfoForType((GunShellInfo.ShellType)info.int2);
    }

    private void OnStopBurstFire(EventId eid, EventInfo ei)
    {
        EventInfo_II info = (EventInfo_II)ei;

        if (info.int1 != PhotonView.ownerId)
            return;

        burst = false;
    }

    private void UseSecondaryWeapon()
    {
        if (secondaryShellInfo.isPrimary || !UseItem(secondaryShellInfo.itemName))
            return;

        Dispatcher.Send(
            id:     EventId.SecondaryWeaponUsed,
            info:   new EventInfo_IIIV(
                        PhotonView.ownerId,
                        (int)SECONDARY_WEAPON_SHELL,
                        currentTargetId,
                        AimPointLocalToTarget),
            target: Dispatcher.EventTargetType.ToAll);
    }
    
    private void OnSecondaryWeaponUsed(EventId id, EventInfo ei)
    {
        EventInfo_IIIV info = (EventInfo_IIIV)ei;

        int playerId = info.int1;
        int targetId = info.int3;
        GunShellInfo.ShellType shellType = (GunShellInfo.ShellType)info.int2;
        Vector3 aimPointLocalToTarget = info.vector;

        if (playerId == PhotonView.ownerId)
            SecondaryFire(shellType, targetId, aimPointLocalToTarget);
    }
    
    private void SetSkidAudio()
    {
        if (!PhotonView.isMine)
            return;

        skidAudio = gameObject.AddComponent<AudioSource>();

        skidAudio.clip = skidSound;
        skidAudio.loop = true;
        skidAudio.rolloffMode = AudioRolloffMode.Linear;
        skidAudio.volume = Settings.SoundVolume;
        skidAudio.spatialBlend = PhotonView.isMine ? 0 : 1;
        skidAudio.maxDistance = DEFAULT_SOUND_DISTANCE;
        skidAudio.dopplerLevel = 0;
    }

    public override void MovePlayer()
    {
        joystickXAxis = Mathf.Clamp(XAxisControl, -1, 1);
        joystickYAxis = YAxisControl;

        #if TOUCH_SCREEN

        joystickYAxis
            = Mathf.Abs(joystickXAxis) > INPUT_THRESHOLD
                ? 1 * Mathf.Sign(joystickYAxis)
                : joystickYAxis;

        #endif

        isBrakeButtonPressed = Input.GetButton("Brake") || HandBrakeButton.IsPressed;

        isArcadePoliceTurning
            = !HelpTools.Approximately(joystickXAxis, 0) &&
              HelpTools.Approximately(joystickYAxis, 0) &&
              isBrakeButtonPressed &&
              !isHandBraking;

        isTankTurning
            = !HelpTools.Approximately(joystickXAxis, 0) &&
              HelpTools.Approximately(joystickYAxis, 0) &&
              !isArcadePoliceTurning;

        if (isTankTurning || isArcadePoliceTurning)
            joystickYAxis = Mathf.Abs(joystickXAxis);

        if (joystickXAxis > ACTIVITY_THRESHOLD || joystickYAxis > ACTIVITY_THRESHOLD)
            MarkActivity();

        engineController.Acceleration = MaxSpeed * joystickYAxis;

        isHandBraking
            = (isBrakeButtonPressed && !isArcadePoliceTurning) ||
              (isArcadePoliceTurning && AccelerationProgress > arcadePoliceTurningAccelerationThreshold);

        isBraking = engineController.IsChangingDirection && !isHandBraking;

        climbingAngle
            = HelpTools.SignedAngle(
                from:   transform.forward.GetHorizontalIdentity(),
                to:     transform.forward,
                axis:   transform.right);

        climbingAngle *= Mathf.Sign(engineController.Torque);

        climbingResistance = climbingAngle * climbingResistanceQualifier * MaxSpeed;

        climbingResistance *= climbingResistance * Mathf.Sign(climbingResistance);

        affectedByTerrainMaxSpeed = MaxSpeed + climbingResistance;

        affectedByTerrainMaxSpeed
            = Mathf.Clamp(
                value:  affectedByTerrainMaxSpeed,
                min:    0,
                max:    MaxSpeed);

        affectedByTerrainAvailableSpeed = isTankTurning ? engineController.RequiredTorque : engineController.Torque;

        if (!isTankTurning)
            affectedByTerrainAvailableSpeed += climbingResistance * Mathf.Sign(engineController.Torque);

        affectedByTerrainAvailableSpeed
            = affectedByTerrainAvailableSpeed > 0
                ? Mathf.Clamp(affectedByTerrainAvailableSpeed, 0, affectedByTerrainMaxSpeed)
                : Mathf.Clamp(affectedByTerrainAvailableSpeed, -affectedByTerrainMaxSpeed, 0);

        affectedByTerrainAvailableSpeed *= isMovingBackwards ? backwardsRatio : 1;

        affectedByControlsAvailableSpeed
            = affectedByTerrainAvailableSpeed
                * (isTankTurning
                    ? TANK_TURNING_SPEED_RATIO
                    : isBraking || isHandBraking
                        ? 0
                        : 1);

        currentSpeed
            = Mathf.Lerp(
                a:  CurrentSpeed,
                b:  affectedByControlsAvailableSpeed,
                t:  isBraking || isHandBraking ? brakingRatio : accelerationRatio);

        if (HorizontalTransformVelocity < ACTIVITY_THRESHOLD && !engineController.IsAccelerating)
        {
            if(!skidAudio.isPlaying)
                AudioDispatcher.PlayClipAtPosition(stuckSound, transform.position);

            engineController.Stop();
            currentSpeed = 0;
        }

        if (deltaHorizontalTansformVelocity > PRIMARY_HORIZONTAL_COLLISION_DETECTION_THRESHOLD)
        {
            engineController.Stop();
            currentSpeed = 0;
        }

        transmissionController.Receive();

        if (deltaHorizontalTansformVelocity > SECONDARY_HORIZONTAL_COLLISION_DETECTION_THRESHOLD && !skidAudio.isPlaying && !isFallen)
            AudioDispatcher.PlayClipAtPosition(crashSounds.GetRandomItem(), transform.position);

        SetEngineNoise(engineController.AccelerationProgress);

        if ((isBraking || isHandBraking) && !skidAudio.isPlaying && IsOnGround)
            skidAudio.Play();
        else if (!(isBraking || isHandBraking) && skidAudio.isPlaying)
            skidAudio.Stop();

        if (IsOnGround)
        {
            affectedByControlsRotationSpeed
                = joystickXAxis
                    * (isHandBraking ? CurrentSpeed : affectedByTerrainAvailableSpeed)
                    * (IsBraking || isHandBraking ? brakeRotationRatio : rotationRatio)
                    * (isTankTurning ? tankTurningRotationRatio : 1)
                    * (isMovingBackwards ? backwardsRotationRatio : 1);

            currentRotationSpeed
                = Mathf.Lerp(
                    a:  currentRotationSpeed,
                    b:  affectedByControlsRotationSpeed,
                    t:  isBraking || isHandBraking
                            ? brakeRotationAccelerationRatio
                            : isTankTurning
                                ? tankTurningRotationAccelerationRatio
                                : rotationAccelerationRatio);

            requiredLocalVelocity = LocalVelocity;

            requiredLocalVelocity.z = CurrentSpeed;

            requiredLocalVelocity.z = Mathf.Abs(requiredLocalVelocity.z) > MIN_SPEED ? requiredLocalVelocity.z : 0;

            requiredLocalVelocity.x
                = Mathf.Abs(currentRotationSpeed) > 0 || isHandBraking || isBraking
                    ? requiredLocalVelocity.x
                        * (isHandBraking || isBraking
                            ? brakeDriftRatio
                            : (driftRatio * engineController.AccelerationProgress))
                    : 0;

            rb.velocity = transform.TransformDirection(requiredLocalVelocity);

            isMovingBackwards = joystickYAxis < 0;

            if (Mathf.Abs(currentRotationSpeed) > MIN_ROTATION_SPEED)
            {
                requiredLocalAngularVelocity = LocalAngularVelocity;
                requiredLocalAngularVelocity.y = currentRotationSpeed * (isMovingBackwards && ProfileInfo.isInvert ? -1 : 1);

                rb.angularVelocity = transform.TransformDirection(requiredLocalAngularVelocity);
            }
        }

        if (!IsOnGround)
            rb.AddForce(
                force:  Vector3.down
                            * Mathf.MoveTowards(
                                current:    MAX_FORCED_LANDING_QUALIFIER,
                                target:     MIN_FORCED_LANDING_QUALIFIER,
                                maxDelta:   AccelerationProgress * MAX_FORCED_LANDING_QUALIFIER),
                mode:   ForceMode.Acceleration);

        HorizontalTransformVelocity = Vector3.Distance(storedVehiclePosition.GetHorizontalIdentity(), transform.position.GetHorizontalIdentity());

        verticalTransformVelocity = Vector3.Distance(storedVehiclePosition.GetVerticalIdentity(), transform.position.GetVerticalIdentity());

        odometer += HorizontalTransformVelocity;

        deltaHorizontalTansformVelocity = Mathf.Abs(storedHorizontalTransformVelocity - HorizontalTransformVelocity);

        deltaVericalTansformVelocity = Mathf.Abs(storedVerticalTransformVelocity - verticalTransformVelocity);

        isFallen = deltaVericalTansformVelocity > VERTICAL_COLLISION_DETECTION_THRESHOLD;

        if (isFallen)
            AudioDispatcher.PlayClipAtPosition(jumpSounds.GetRandomItem(), transform.position);

        storedVehiclePosition = transform.position;
        storedHorizontalTransformVelocity = HorizontalTransformVelocity;
        storedVerticalTransformVelocity = verticalTransformVelocity;

        rb.centerOfMass = newCenterOfMass;
    }

    override public void AnimateClone()
    {
        transmissionController.Spin(Wheel.Mode.All, LocalVelocity.z);
        transmissionController.Turn(LocalAngularVelocity.y);

        SetEngineNoise(LocalVelocity.z / MaxSpeed);
    }

    override public void StoreCloneRotation()
    {
        Quaternion deltaRotation = transform.rotation * Quaternion.Inverse(lastRotation);

        deltaRotation.ToAngleAxis(out deltaRotationMagnitude, out deltaRotationAxis);

        lastRotation = transform.rotation;
    }

    private void CameraToHome()
    {
        Camera.main.transform.position = forCam.position;
        Camera.main.transform.rotation = forCam.rotation;
    }
}
