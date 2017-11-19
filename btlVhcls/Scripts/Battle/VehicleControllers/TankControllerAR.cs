using UnityEngine;

using Hashtable = ExitGames.Client.Photon.Hashtable;

public class TankControllerAR : TankController
{
    [Header("Настройки Armada")]

    [Header("Звуки")]
    public AudioClip idleSound;
    public AudioClip trackSound;
    public AudioClip collisionSound;
    public AudioClip rotationSound;
    public AudioClip accelerationSound;
    public AudioClip reloadingSound;
    public AudioClip reverseSound;

    [Header("Управление")]
    public float xAxisAccelerationStep = 4.0f;
    public float yAxisAccelerationStep = 1.5f;
    public float xAxisInertion = 1.2f;
    public float yAxisInertion = 0.45f;
    public float yAxisChangingDirectionInertion = 5.0f;
    public float rotationSpeedInZoom = 20f;

    protected float xAxisAcceleration;
    protected float yAxisAcceleration;
    protected float deltaRotationMagnitude;
    protected Vector3 deltaRotationAxis;
    protected Quaternion lastRotation;
    protected Camera camera2d;
    protected SuspensionController suspensionController;
    protected TransmissionControllerAR transmissionController;
    protected SkidmarksPoint[] skidmarkPoints;

    private bool isChoppedSkidmarks;
    private VehicleStateDispatcher stateDispatcher;
    //private Collider[] checkHits = new Collider[10];
    //private int checkHitsCount;

    public override Vector3 AngularVelocity
    {
        get
        {
            if (PhotonView == null)
                return Vector3.zero;

            if (PhotonView.isMine)
                return rb.angularVelocity;

            return ((deltaRotationAxis * deltaRotationMagnitude) / Time.deltaTime) * Mathf.Deg2Rad;
        }
    }

    public override Transform Turret
    {
        get { return turret = turret ?? transform.Find("Body/Turret"); }
    }

    public override Transform ShotPoint
    {
        get { return shotPoint = shotPoint ?? transform.Find("Body/Turret/ShotPoint"); }
    }

    public override Transform CannonEnd
    {
        get { return cannonEnd = cannonEnd ?? transform.Find("Body/Turret/CannonEnd"); }
    }

    public override float YAxisAcceleration { get { return yAxisAcceleration; } }

    public override float XAxisAcceleration { get { return xAxisAcceleration; } }

    protected override AudioClip CollisionSound
    {
        get { return collisionSound; }
    }

    public override void UpdateBotPrefabs(VehicleController nativeController)
    {
        
    }

    private bool IsBackwardInverted
    {
        get
        {
            return ProfileInfo.isInvert;
        }
    }

    public override void OnPhotonInstantiate(PhotonMessageInfo info)
    {
        suspensionController = GetComponent<SuspensionController>();
        transmissionController = GetComponent<TransmissionControllerAR>();
        skidmarkPoints = GetComponentsInChildren<SkidmarksPoint>();

        camera2d = BattleGUI.Instance.GuiCamera;
        stateDispatcher = new VehicleStateDispatcher();

        stateDispatcher.Init(this);

        base.OnPhotonInstantiate(info);

        Dispatcher.Subscribe(EventId.StartBurstFire, OnStartBurstFire);
        Dispatcher.Subscribe(EventId.ShellHit, OnShellHit);
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();

        Dispatcher.Unsubscribe(EventId.StartBurstFire, OnStartBurstFire);
        Dispatcher.Unsubscribe(EventId.ShellHit, OnShellHit);
    }

    protected override void Update()
    {
        base.Update();

        if (IsMain)
            MoveStaticGunsight();
    }

    protected override void FixedUpdate()
    {
        if (!PhotonView || !IsAvailable)
            return;

        if (PhotonView.isMine)
        {
            MovePlayer();

            if (!IsBot)
                DrawSkidmarks();
        }
        else
        {
            AnimateClone();
            StoreCloneRotation();
        }
    }

    protected override void OnCollisionEnter(Collision collision)
    {
        base.OnCollisionEnter(collision);
        suspensionController.CheckGroundContacts(collision);
    }

    [PunRPC]
    public override void Respawn(Vector3 position, Quaternion rotation, bool restoreLife, bool firstTime)
    {
        base.Respawn(position, rotation, restoreLife, firstTime);

        yAxisAcceleration = 0;
        xAxisAcceleration = 0;
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

        EffectPoolDispatcher.GetFromPool(
            _effect:        shotPrefab,
            _position:      CannonEnd.position,
            _rotation:      CannonEnd.rotation,
            useEffectMover: true,
            moverTarget:    CannonEnd);

        Shell shell
            = ShellPoolManager.GetShell(
                shellName:  primaryShellInfo.shellPrefabName,
                position:   shotPoint.position,
                rotation:   TargetAimed && IsMain
                                ? Quaternion.LookRotation((TargetPosition - shotPoint.position).normalized, shotPoint.up)
                                : shotPoint.rotation);

        shell.OwnerSpeed = Mathf.Abs(curMaxSpeed);

        continuousFire = false;

        shell.Activate(this, data.attack, hitMask);

        AudioDispatcher.PlayClipAtPosition(shotSound, shotPoint.position, SoundControllerBase.SHOT_VOLUME);

        return true;
    }

    public override void StartBurst()
    {
        Dispatcher.Send(
            id:     EventId.StartBurstFire,
            info:   new EventInfo_II(data.playerId, (int)primaryShellInfo.type),
            target: Dispatcher.EventTargetType.ToAll);
    }

    protected override void SetEngineAudio() { }

    protected override void SetTurretAudio() { }

    protected override void SetEngineNoise(float t)  { }

    protected override void PlayExplosionSound()
    {
        AudioDispatcher.PlayClipAtPosition(
            /* clip:     */ explosionSound,
            /* position: */ IsMain ? BattleCamera.Instance.transform.position : transform.position,
            /* volume:   */ SoundControllerBase.EXPLOSION_VOLUME,
            /* parent:   */ IsMain ? BattleCamera.Instance.transform : transform);
    }

    private void OnStartBurstFire(EventId id, EventInfo ei)
    {
        EventInfo_II info = (EventInfo_II)ei;

        int playerId = info.int1;

        if (playerId != data.playerId || PhotonView.isMine)
            return;

        if (IsMain || Settings.GraphicsLevel > GraphicsLevel.lowQuality)
            PrimaryFire();
    }

    protected float Accelerate(
        float   oldSpeed,
        float   newSpeed,
        float   step,
        float   inertionRatio,
        bool    xAxis)
    {
        bool isForwardDecelerating = newSpeed < oldSpeed && newSpeed >= 0 && oldSpeed > 0;
        bool isBackwardDecelerating = newSpeed > oldSpeed && newSpeed <= 0 && oldSpeed < 0;
        bool isChangingDirection = (newSpeed > 0 && oldSpeed < 0) || (newSpeed < 0 && oldSpeed > 0);
        bool isDecelerating = isForwardDecelerating || isBackwardDecelerating;

        if (isDecelerating)
            step *= inertionRatio;

        if (isChangingDirection)
            step *= yAxisChangingDirectionInertion;

        step *= Time.deltaTime;

        if (xAxis)
            stateDispatcher.RegisterRotation(oldSpeed, newSpeed);
        else
            stateDispatcher.RegisterMovement(oldSpeed, newSpeed, step);

        return Mathf.MoveTowards(oldSpeed, newSpeed, step);
    }

    override protected float RotationSpeed {
        get { return MaxSpeed * rotationSpeedQualifier * xAxisAcceleration; }
    }

    protected override float ZoomRotationSpeed {
        get {
            return Mathf.Min (MaxSpeed * rotationSpeedQualifier, rotationSpeedInZoom) * xAxisAcceleration;
        }
    }

    public override void MovePlayer()
    {
        rb.centerOfMass = centerOfMass;
        CalcXAxisAcceleration ();
        CalcYAxisAcceleration ();

        curMaxSpeed = MaxSpeed * yAxisAcceleration;
        curMaxRotationSpeed = BattleCamera.Instance.IsZoomed ? ZoomRotationSpeed : RotationSpeed;

        if (Mathf.Abs(curMaxSpeed) > MOVEMENT_SPEED_THRESHOLD)
            MarkActivity();

        if (Mathf.Abs(curMaxRotationSpeed) > MOVEMENT_SPEED_THRESHOLD)
            MarkActivity();

        if (onGround)
        {
            requiredLocalVelocity = LocalVelocity;

            requiredLocalVelocity.z = Mathf.Abs(curMaxSpeed) > 0.05f ? curMaxSpeed : 0;

            if (Vector3.Angle(transform.forward, rb.velocity) > 5.0f)
                requiredLocalVelocity.x = 0;


            //Vector3 transformDirection = transform.TransformDirection(requiredLocalVelocity);
            //var angle = Vector3.Angle (transformDirection, Vector3.up) * Mathf.Deg2Rad;
            //var verticalKoefficient = Mathf.Clamp01 (1f - Mathf.Cos (angle));
            //transformDirection.y = 0;
            //transformDirection *= verticalKoefficient;
            //transformDirection.y = rb.velocity.y;

            //rb.velocity = transformDirection;

            Vector3 transformDirection = transform.TransformDirection(requiredLocalVelocity);
            var angle = Vector3.Angle (transformDirection, Vector3.up) * Mathf.Deg2Rad;
            var verticalKoefficient = Mathf.Clamp01 (1.5f - Mathf.Cos (angle));
            //transformDirection.y = 0;
            //transformDirection *= verticalKoefficient;
            transformDirection.y *= verticalKoefficient;

            rb.velocity = transformDirection;

            //rb.velocity = transform.TransformDirection(requiredLocalVelocity);

            bool moveBackward = YAxisControl < 0;

            if (Mathf.Abs(curMaxRotationSpeed) > 0.1f)
            {
                requiredLocalAngularVelocity = LocalAngularVelocity;

                requiredLocalAngularVelocity.y = (moveBackward && IsBackwardInverted ? -1 : 1) * curMaxRotationSpeed * 0.03f; // всем костылям костыль

                requiredLocalAngularVelocity = transform.TransformDirection(requiredLocalAngularVelocity);

                rb.angularVelocity = requiredLocalAngularVelocity;
            }
        }
        else
        {
            rb.AddForce((Vector3.down - transform.up) * 45, ForceMode.Acceleration);
        }

        StoreVehiclePosition();

        if (transmissionController != null)
            transmissionController.Receive();
    }

    public override void AnimateClone()
    {
        if (transmissionController != null)
        {
            transmissionController.Spin(Wheel.Mode.All, LocalVelocity.z);
            transmissionController.Turn(LocalAngularVelocity.y);
        }
    }

    public override void StoreCloneRotation()
    {
        Quaternion deltaRotation = transform.rotation * Quaternion.Inverse(lastRotation);

        deltaRotation.ToAngleAxis(out deltaRotationMagnitude, out deltaRotationAxis);

        lastRotation = transform.rotation;
    }

    private void MoveStaticGunsight()
    {
        if (BattleGUI.Instance.iGunSight != null)
            BattleGUI.Instance.iGunSight.ShowStaticGunSight(Turret.position + Turret.forward * 2000f);
        else if (BattleGUI.Instance.StaticGunsight != null)
        {
            Vector3 sightPoint = Camera.main.WorldToViewportPoint(Turret.position + Turret.forward * 2000f);
            sightPoint = camera2d.ViewportToWorldPoint(sightPoint);
            BattleGUI.Instance.StaticGunsight.transform.position = sightPoint;
        }
    }

    private void DrawSkidmarks()
    {
        if (Mathf.Abs(curMaxSpeed) < SKIDMARK_SPEED_THRESHOLD && Mathf.Abs(curMaxRotationSpeed) < SKIDMARK_SPEED_THRESHOLD)
        {
            if (!isChoppedSkidmarks)
            {
                foreach (SkidmarksPoint skidmarksPoint in skidmarkPoints)
                    skidmarksPoint.Chop();

                isChoppedSkidmarks = true;
            }

            return;
        }

        isChoppedSkidmarks = false;

        foreach (SkidmarksPoint skidmarksPoint in skidmarkPoints)
        {
            if (skidmarksPoint.CheckGroundContact())
                skidmarksPoint.Draw();
            else
                skidmarksPoint.Chop();
        }
    }

    private float CalcYAxisAcceleration()
    {
        return yAxisAcceleration
                = Accelerate(
                    oldSpeed:       yAxisAcceleration,
                    newSpeed:       YAxisControl,
                    step:           yAxisAccelerationStep,
                    inertionRatio:  yAxisInertion,
                    xAxis:          false);
    }

    private float CalcXAxisAcceleration()
    {
        return xAxisAcceleration
                = Accelerate(
                    oldSpeed:       xAxisAcceleration,
                    newSpeed:       XAxisControl,
                    step:           xAxisAccelerationStep,
                    inertionRatio:  xAxisInertion,
                    xAxis:          true);
    }

    private void OnShellHit(EventId eid, EventInfo ei)
    {
        EventInfo_U info = (EventInfo_U)ei;

        int victimId = (int)info[0];
        int damage = (int)info[1];
        int ownerId = (int)info[2];
        GunShellInfo.ShellType shellType = (GunShellInfo.ShellType)(int)info[3];
        int hits = (int)info[4];
        Vector3 hitPosition = (Vector3)info[5];

        if (victimId != data.playerId)
            return;

        Vector3 position = transform.TransformPoint(hitPosition);

        AudioDispatcher.PlayClipAtPosition(blowSound, position, SoundControllerBase.TANK_HIT_VOLUME);
    }
}
