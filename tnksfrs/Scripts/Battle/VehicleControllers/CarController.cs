#if (UNITY_IPHONE || UNITY_ANDROID || UNITY_WP8) && !UNITY_EDITOR
    #define TOUCH_SCREEN
#endif

using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using XD;
using XDevs.LiteralKeys;

using Hashtable = ExitGames.Client.Photon.Hashtable;

public class CarController : VehicleController
{
    public const float ACTIVITY_THRESHOLD = 0.01f;

    [Header("Настройки для автомобилей")]

    [Header("Ссылки")]    
    public List<Transform> rocketLaunchPoints = null;

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
        get 
        { 
            return weapons[DefaultShellType].HeatingProgress; 
        }
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
    
    public override Vector3 AngularVelocity
    {
        get
        {
            if (PhotonView.isMine)
            {
                return rigidbody.angularVelocity;
            }

            return ((deltaRotationAxis * deltaRotationMagnitude) / Time.deltaTime) * Mathf.Deg2Rad;
        }
    }

    public bool IsBraking
    {
        get 
        { 
            return isBraking; 
        }
    }

    public bool IsHandBraking
    {
        get 
        { 
            return isHandBraking; 
        }
    }

    public bool IsTankTurning
    {
        get 
        { 
            return isTankTurning; 
        }
    }

    public bool IsOnGround
    {
        get; private set;
    }

    public float JoystickXAxis
    {
        get 
        { 
            return joystickXAxis; 
        }
    }

    public float HorizontalTransformVelocity
    {
        get; private set;
    }

    public float CurrentSpeed
    {
        get; private set;
    }

    public float AccelerationProgress
    {
        get 
        { 
            return Mathf.Abs(CurrentSpeed) / affectedByTerrainMaxSpeed; 
        }
    }

    protected override bool FireButtonPressed
    {
        get
        {
            return continuousFire
                ? Input.GetButton("Fire1")// TODO: поменять ShellType.
                : Input.GetButtonDown("Fire1");
        }
    }

    public override bool IsRequirePrimaryFire
    {
        get
        {
            return TargetAimed &&
                   !StaticContainer.UI.IsWindowOnScreen &&
                   (transform.position - AimPoint.point).sqrMagnitude < MAX_AUTOFIRE_DISTANCE_SQR;
        }
    }

    protected override bool IsRequireSecondaryFire
    {
        get
        {
            return !secondaryShellInfo.isPrimary &&
                   FireButtonPressed &&
                   !StaticContainer.UI.IsWindowOnScreen;
        }
    }
    protected override float OdometerRatio
    {
        get { return 1; }
    }

    protected override float SpeedRatio
    {
        get { return GameData.CurrentGame == Game.ApocalypticCars ? speedRatio : 1; }
    }

    protected override float CorrectionTime
    {
        get { return CORRECTION_TIME; }
    }

    protected override void OnPhotonInstantiate(PhotonMessageInfo info)
    {
        base.OnPhotonInstantiate(info);

        rigidbody.interpolation = RigidbodyInterpolation.Interpolate;
        
        SetSkidAudio();

        Dispatcher.Subscribe(EventId.ShellHit, OnShellHit);
        Dispatcher.Subscribe(EventId.SecondaryWeaponUsed, OnSecondaryWeaponUsed);

        if (!PhotonView.isMine)
        {
            Dispatcher.Subscribe(EventId.StartBurstFire, OnStartBurstFire);
            Dispatcher.Subscribe(EventId.StopBurstFire, OnStopBurstFire);

            return;
        }
        
        CameraToHome();
    }

    protected override void NormalUpdate()
    {
        base.NormalUpdate();

        if (PhotonView.isMine && IsRequireSecondaryFire)
            UseSecondaryWeapon();

        if (burst)
            PrimaryFire(shotPoint.rotation);
    }

    protected override void PhysicsUpdate()
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
    void OnDrawGizmos()
    {
        Gizmos.color = Color.magenta;
        Gizmos.DrawSphere(center: transform.TransformPoint(newCenterOfMass), radius: 0.1f);
    }
    #endif

    [PunRPC]
    public override void Respawn(Vector3 position, Quaternion rotation, bool restoreLife, bool firstTime)
    {
        base.Respawn(position, rotation, restoreLife, firstTime);
        if (PhotonView.isMine)
        {
            CameraToHome();
        }
    }

    public override bool PrimaryFire(Quaternion rotation)
    {
        return true;
    }

    public override void UpdateBotAssets(VehicleController nativeController)
    {
    }

    protected override void OnTargetAimed(EventId id, EventInfo ei)
    {
        EventInfo_IIB info = (EventInfo_IIB)ei;

        TargetAimed = info.bool1;
        currentTargetId = info.int2;
    }

    protected override void SetEngineNoise(float t)
    {
        engineAudio.pitch = Mathf.Lerp(1.0f, maxEnginePitch, t);
        engineAudio.volume = global::Settings.SoundVolume * Mathf.Lerp(1.0f, maxEngineVolume, t);
    }

    protected override void SetEngineAudio()
    {
    }

    protected override void SetTurretAudio()
    {
        turretAudio = gameObject.AddComponent<AudioSource>();

        turretAudio.clip = turretRotationSound;
        turretAudio.loop = true;
        turretAudio.rolloffMode = AudioRolloffMode.Linear;
        turretAudio.volume = global::Settings.SoundVolume;
        turretAudio.spatialBlend = PhotonView.isMine ? 0 : 1;
        turretAudio.maxDistance = TURRET_SOUND_DISTANCE;
        turretAudio.dopplerLevel = 0;
    }

    protected override void OnDestroy()
    {
        Dispatcher.Unsubscribe(EventId.StartBurstFire, OnStartBurstFire);
        Dispatcher.Unsubscribe(EventId.StopBurstFire, OnStopBurstFire);
        Dispatcher.Unsubscribe(EventId.ShellHit, OnShellHit);
        Dispatcher.Unsubscribe(EventId.SecondaryWeaponUsed, OnSecondaryWeaponUsed);

        base.OnDestroy();
    }

    public override void StartBurst()
    {
        Dispatcher.Send(EventId.StartBurstFire, new EventInfo_IIV(data.playerId, (int)DefaultShellType, ShotPoint.forward), Dispatcher.EventTargetType.ToAll);
    }

    public override void StopBurst()
    {
        Dispatcher.Send(EventId.StopBurstFire, new EventInfo_II(data.playerId, (int)DefaultShellType), Dispatcher.EventTargetType.ToAll);
    }

    private void OnStartBurstFire(EventId eid, EventInfo ei)
    {
        EventInfo_IIV info = (EventInfo_IIV)ei;

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
    }
    
    private void OnSecondaryWeaponUsed(EventId id, EventInfo ei)
    {
        EventInfo_III info = (EventInfo_III)ei;

        if (info.int1 != PhotonView.ownerId)
            return;

        SecondaryFire(shellType: (GunShellInfo.ShellType)info.int2, targetId: info.int3);
    }
    
    private void OnShellHit(EventId eid, EventInfo ei)
    {
        EventInfo_IIIIV info = (EventInfo_IIIIV)ei;

        if (info.int1 != Player.ID)
            return;

        Vector3 position = transform.TransformPoint(info.vector);
        Shell shell
            = ShellPoolManager.GetShell(
                shellName:  GunShellInfo.GetShellInfoForType((GunShellInfo.ShellType)info.int4).shellPrefabName,
                position:   Vector3.zero,
                rotation:   Quaternion.identity);

        shell.Explosion(position: position, hitsVehicle: true);

        int damage = info.int2;
        HPSystem.ChangeHitPoints(damage, info.int3, false);

        Dispatcher.Send(EventId.TankTakesDamage, new EventInfo_U(info.int1, damage, info.int3, info.int4, info.vector));
        Hashtable props = new Hashtable { { "hl", HPSystem.Armor } };

        Player.SetCustomProperties(props);
    }

    private void SetSkidAudio()
    {
        if (!PhotonView.isMine)
            return;

        skidAudio = gameObject.AddComponent<AudioSource>();

        skidAudio.clip = skidSound;
        skidAudio.loop = true;
        skidAudio.rolloffMode = AudioRolloffMode.Linear;
        skidAudio.volume = global::Settings.SoundVolume;
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

        isBrakeButtonPressed = Input.GetButton("Brake") || false;

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
        {
            MarkActivity();
        }

        engineController.Acceleration = Settings[Setting.MovingSpeed].Max * joystickYAxis;

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

        climbingResistance = climbingAngle * climbingResistanceQualifier * Settings[Setting.MovingSpeed].Max;

        climbingResistance *= climbingResistance * Mathf.Sign(climbingResistance);

        affectedByTerrainMaxSpeed = Settings[Setting.MovingSpeed].Max + climbingResistance;

        affectedByTerrainMaxSpeed
            = Mathf.Clamp(
                value:  affectedByTerrainMaxSpeed,
                min:    0,
                max:    Settings[Setting.MovingSpeed].Max);

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

        CurrentSpeed
            = Mathf.Lerp(
                a:  CurrentSpeed,
                b:  affectedByControlsAvailableSpeed,
                t:  isBraking || isHandBraking ? brakingRatio : accelerationRatio);

        if (HorizontalTransformVelocity < ACTIVITY_THRESHOLD && !engineController.IsAccelerating)
        {
            if(!skidAudio.isPlaying)
                AudioDispatcher.PlayClipAtPosition(stuckSound, transform.position);

            engineController.Stop();
            CurrentSpeed = 0;
        }

        if (deltaHorizontalTansformVelocity > PRIMARY_HORIZONTAL_COLLISION_DETECTION_THRESHOLD)
        {
            engineController.Stop();
            CurrentSpeed = 0;
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

            rigidbody.velocity = transform.TransformDirection(requiredLocalVelocity);

            isMovingBackwards = joystickYAxis < 0;

            if (Mathf.Abs(currentRotationSpeed) > MIN_ROTATION_SPEED)
            {
                requiredLocalAngularVelocity = LocalAngularVelocity;
                requiredLocalAngularVelocity.y = currentRotationSpeed * (isMovingBackwards && ProfileInfo.isInvert ? -1 : 1);

                rigidbody.angularVelocity = transform.TransformDirection(requiredLocalAngularVelocity);
            }
        }

        if (!IsOnGround)
            rigidbody.AddForce(
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

        rigidbody.centerOfMass = newCenterOfMass;
    }

    private void AnimateClone()
    {
        transmissionController.Spin(Wheel.Mode.All, LocalVelocity.z);
        transmissionController.Turn(LocalAngularVelocity.y);

        SetEngineNoise(LocalVelocity.z / Settings[Setting.MovingSpeed].Max);
    }

    private void StoreCloneRotation()
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
