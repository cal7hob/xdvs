using UnityEngine;

public class TankControllerAR : TankController
{
    [Header("Настройки WWT2")]

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
    public float rotationSpeedInZoom = 0.7f;

    protected float xAxisAcceleration;
    protected float yAxisAcceleration;
    protected float deltaRotationMagnitude;
    protected Vector3 deltaRotationAxis;
    protected Quaternion lastRotation;
    protected Camera camera2d;
    protected SuspensionController suspensionController;
    protected TransmissionControllerAR transmissionController;
    protected SkidmarksPoint[] skidmarkPoints;

    private VehicleStateDispatcher stateDispatcher;
    //private Collider[] checkHits = new Collider[10];
    //private int checkHitsCount;

    public override Vector3 AngularVelocity
    {
        get
        {
            if (PhotonView == null)
            {
                return Vector3.zero;
            }

            if (IsMine)
            {
                return rb.angularVelocity;
            }

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
            return ProfileInfo.isInvert || transmissionController != null;
        }
    }

    protected override float ZoomRotationSpeed
    {
        get
        {
            return Mathf.Min(maxSpeed * rotationSpeedQualifier, rotationSpeedInZoom) * xAxisAcceleration;
        }
    }

    protected override float RotationSpeed
    {
        get { return maxSpeed * rotationSpeedQualifier * xAxisAcceleration; }
    }

    protected override void OnPhotonInstantiate(PhotonMessageInfo info)
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

    protected override TurretController GetTurret(VehicleController vehicle, Animation shootAnimation)
    {
        return new TurretTankARController(vehicle, shootAnimation);
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
        {
            MoveStaticGunsight();
            DrawSkidmarks();
        } 
    }

    protected override void FixedUpdate()
    {
        if (!PhotonView || !IsAvailable)
        {
            return;
        }

        if (IsMine)
        {
            MovePlayer();
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

    public override void StartBurst()
    {
        Dispatcher.Send(
            id:     EventId.StartBurstFire,
            info:   new EventInfo_II(data.playerId, (int)primaryShellInfo.type),
            target: Dispatcher.EventTargetType.ToAll);
    }

    protected override void SetEngineAudio() { }

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

        if (playerId != data.playerId || IsMine)
        {
            return;
        }

        if (IsMain || Settings.GraphicsLevel > GraphicsLevel.lowQuality)
        {
            turretController.PrimaryFire();
        }
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
        {
            step *= inertionRatio;
        }

        if (isChangingDirection)
        {
            step *= yAxisChangingDirectionInertion;
        }

        step *= Time.deltaTime;

        if (xAxis)
        {
            stateDispatcher.RegisterRotation(oldSpeed, newSpeed);
        }
        else
        {
            stateDispatcher.RegisterMovement(oldSpeed, newSpeed, step);
        }

        return Mathf.MoveTowards(oldSpeed, newSpeed, step);
    }

    public override void MovePlayer()
    {
        rb.centerOfMass = centerOfMass;

        CalcYAxisAcceleration();
        CalcXAxisAcceleration();

        curMaxSpeed = maxSpeed * yAxisAcceleration;
        curMaxRotationSpeed = BattleCamera.Instance.IsZoomed ? ZoomRotationSpeed : RotationSpeed;

        if (Mathf.Abs(curMaxSpeed) > MOVEMENT_SPEED_THRESHOLD)
        {
            MarkActivity();
        }

        if (Mathf.Abs(curMaxRotationSpeed) > MOVEMENT_SPEED_THRESHOLD)
        {
            MarkActivity();
        }

        if (onGround)
        {
            requiredLocalVelocity = LocalVelocity;

            requiredLocalVelocity.z = Mathf.Abs(curMaxSpeed) > 0.05f ? curMaxSpeed : 0;
            // Исключение заноса.
            if (Vector3.Angle(transform.forward, rb.velocity) > 5.0f)
            {
                requiredLocalVelocity.x = 0;
            }
            
            rb.velocity = transform.TransformDirection(requiredLocalVelocity);

            bool moveBackward = requiredLocalVelocity.z < 0;

            //if (Mathf.Abs(curMaxRotationSpeed) > 0.1f)
            {
                requiredLocalAngularVelocity = LocalAngularVelocity;

                requiredLocalAngularVelocity.y = (moveBackward && IsBackwardInverted ? -1 : 1) * curMaxRotationSpeed; 

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
        {
            transmissionController.Receive();
        }
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

   /* public bool isChooseTarget() 
    { 
        Vector3 sightPoint = camera2d.ViewportToWorldPoint(sightPoint);
    }*/

    private void MoveStaticGunsight()
    {
        if (BattleGUI.Instance.StaticGunsight == null)
        {
            return;
        }

        Vector3 sightPoint = Camera.main.WorldToViewportPoint(Turret.position + Turret.forward * 2000f);
        sightPoint = camera2d.ViewportToWorldPoint(sightPoint);
        BattleGUI.Instance.StaticGunsight.transform.position = sightPoint;
    }

    protected void DrawSkidmarks()
    {
        if (Mathf.Abs(curMaxSpeed) < MOVEMENT_SPEED_THRESHOLD && Mathf.Abs(curMaxRotationSpeed) < MOVEMENT_SPEED_THRESHOLD)
        {
            return;
        }

        foreach (SkidmarksPoint skidmarksPoint in skidmarkPoints)
        {
            if (skidmarksPoint.CheckGroundContact())
            {
                skidmarksPoint.Draw();
            }
            else
            {
                skidmarksPoint.Chop();
            }
        }
    }

    private float CalcYAxisAcceleration()
    {
        return yAxisAcceleration = Accelerate(
                    oldSpeed:       yAxisAcceleration,
                    newSpeed:       YAxisControl,
                    step:           yAxisAccelerationStep,
                    inertionRatio:  yAxisInertion,
                    xAxis:          false);
    }

    private float CalcXAxisAcceleration()
    {
        return xAxisAcceleration = Accelerate(
                    oldSpeed:       xAxisAcceleration,
                    newSpeed:       XAxisControl,
                    step:           xAxisAccelerationStep,
                    inertionRatio:  xAxisInertion,
                    xAxis:          true);
    }

    //private void MoveStaticGunsight()
    //{
    //    if (BattleGUI.Instance.StaticGunsight == null)
    //        return;

    //    Vector3 sightPoint;

    //    if (BattleCamera.Instance.IsZoomed)
    //    {
    //        sightPoint = Camera.main.WorldToViewportPoint(turret.position + GroundCamera.AimDir * 2000f);
    //    }
    //    else
    //    {
    //        sightPoint = Camera.main.WorldToViewportPoint(Turret.position + shotPoint.forward * 2000f);
    //    }



    //    sightPoint = camera2d.ViewportToWorldPoint(sightPoint);
    //    sightPoint.z = 1;

    //    BattleGUI.Instance.StaticGunsight.transform.position = sightPoint;
    //}

    private void OnShellHit(EventId eid, EventInfo ei)
    {
        EventInfo_IIIIV info = (EventInfo_IIIIV)ei;

        if (info.int1 != data.playerId)
        {
            return;
        }

        Vector3 position = transform.TransformPoint(info.vector);
        AudioDispatcher.PlayClipAtPosition(blowSound, position, SoundControllerBase.TANK_HIT_VOLUME);
    }
    private bool FromScreenToWorldCoord(Vector2 flatPos, ref Vector3 pos) 
    {
        Ray ray = Camera.main.ScreenPointToRay(flatPos);
        Plane plane = new Plane(Vector3.up, transform.position);
        float distance = 0; // this will return the distance from the camera
        if (plane.Raycast(ray, out distance))
        {
            pos = ray.GetPoint(distance); // get the point
            return true;
        }
        return false;
    }
}
