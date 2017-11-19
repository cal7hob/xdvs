using UnityEngine;
using XD;
using System.Collections.Generic;
using UnityEngine.Profiling;
using Hashtable = ExitGames.Client.Photon.Hashtable;

public enum EngineState
{
    Idle,
    ForwardAcceleration,
    BackwardAcceleration,
    Movement,
    ReverseMovement,
    ForwardBrake,
    BackwardBrake
}

public class TankControllerAR : TankController
{
    [Header("Настройки Armada")]
    [SerializeField]
    private WheelCollider[]             wheelColliders = null;
    [SerializeField]
    protected SuspensionController      suspensionController = null;

    [Header("Звуки")]
    public AudioClip                    collisionSound = null;
    public AudioClip                    reloadingSound = null;

    [Header("Управление")]
    public float                        xAxisAccelerationStep = 4.0f;
    public float                        yAxisAccelerationStep = 1.5f;
    public float                        xAxisInertion = 1.2f;
    public float                        yAxisInertion = 0.45f;
    public float                        yAxisChangingDirectionInertion = 5.0f;

    [Header("Debug")]
    [SerializeField]
    private float                       gravity = 0;
    [SerializeField]
    private float                       angle = 0;
    [SerializeField]
    private float                       mTorque = 125;

    [SerializeField]
    private bool                        realWheels = true;

    [SerializeField]
    private float                       windowWidth = 192;
    [SerializeField]
    private bool                        fullDebug = false;

    private float                       oldYAxis = 0;
    private float                       oldXAxis = 0;

    protected bool                      isRotatedFrameBefore;

    protected float                     oldMovement = 0;
    protected float                     oldTurn = 0;
    protected float                     xAxisAcceleration;
    protected float                     yAxisAcceleration;
    protected float                     deltaRotationMagnitude;

    protected EngineState               lastEngineState;
    protected Vector3                   deltaRotationAxis;
    protected Quaternion                lastRotation;
    
    protected ISkidmarksPoint[]         skidmarksPoints = null;
    protected TransmissionControllerAR  transmissionController = null;
    
    private float                       verticalKoefficient = 1;
    private float                       brakeKoefficient = 50;
    private float                       oldMotor = 0;
    private float                       oldBrake = 0;
    private IMaster                     master = null;

    private Dictionary<WheelCollider, ISkidmarksPoint> skidmarksDictionary = null;

    private Dictionary<WheelCollider, ISkidmarksPoint> SkidmarksDictionary
    {
        get
        {
            if (skidmarksDictionary == null)
            {
                skidmarksDictionary = new Dictionary<WheelCollider, ISkidmarksPoint>();

                for (int i = 0; i < SkidmarksPoints.Length; i++)
                {
                    if (SkidmarksPoints[i].WheelCollider == null)
                    {
                        continue;
                    }

                    skidmarksDictionary[SkidmarksPoints[i].WheelCollider] = SkidmarksPoints[i];
                }
            }

            return skidmarksDictionary;
        }
    }

    public ISkidmarksPoint[] SkidmarksPoints
    {
        get
        {
            return skidmarksPoints;
        }
    }

    public override float Gravity
    {
        get
        {
            return gravity;
        }

        set
        {
            gravity = value;
        }
    }

    public override Vector3 AngularVelocity
    {
        get
        {
            if (PhotonView == null)
            {
                return Vector3.zero;
            }

            if (PhotonView.isMine)
            {
                return rigidbody.angularVelocity;
            }

            return ((deltaRotationAxis * deltaRotationMagnitude) / Time.deltaTime) * Mathf.Deg2Rad;
        }
    }

    public override float YAxisAcceleration
    {
        get
        {
            return yAxisAcceleration
                = Accelerate(
                    oldSpeed: yAxisAcceleration,
                    newSpeed: YAxisControl,
                    step: yAxisAccelerationStep,
                    inertionRatio: yAxisInertion,
                    xAxis: false);
        }
    }

    public override float XAxisAcceleration
    {
        get
        {
            return xAxisAcceleration
                = Accelerate(
                    oldSpeed: xAxisAcceleration,
                    newSpeed: XAxisControl,
                    step: xAxisAccelerationStep,
                    inertionRatio: xAxisInertion,
                    xAxis: true);
        }
    }

    protected override AudioClip CollisionSound
    {
        get
        {
            return collisionSound;
        }
    }

    public override void UpdateBotAssets(VehicleController nativeController)
    {

    }

    private bool IsBackwardInverted
    {
        get
        {
            return backInverse || transmissionController != null;
        }
    }

    public override void InitComponents()
    {
        base.InitComponents();

        if (master == null)
        {
            master = StaticType.Master.Instance<IMaster>();
        }

        if (suspensionController == null)
        {
            suspensionController = GetComponent<SuspensionController>();           
        }

        if (suspensionController != null)
        {
            suspensionController.InitComponents(IsMine);
        }

        if (transmissionController == null)
        {
            transmissionController = GetComponent<TransmissionControllerAR>();  
        }

        if (transmissionController != null)
        {
            transmissionController.InitComponents(IsMine);
        }

        if (skidmarksPoints == null)
        {
            skidmarksPoints = GetComponentsInChildren<ISkidmarksPoint>();
        }

        if (wheelColliders == null || wheelColliders.Length == 0)
        {
            wheelColliders = GetComponentsInChildren<WheelCollider>();
        }

        if (PhotonView != null)
        {
            bool isMine = true;
            if (StaticType.Input.Instance<IInput>().IsMobile && StaticType.Options.Instance<IOptions>().GraphicsQuality == 0)
            {
                isMine = IsMine;
            }
            else
            {
                isMine = PhotonView.isMine;
            }

            if (!isMine)
            {
                if (lowQualityCollider != null)
                {
                    lowQualityCollider.gameObject.SetActive(true);
                }
            }

            InitWheels(isMine);
        }
    }

    protected override void OnPhotonInstantiate(PhotonMessageInfo info)
    {
        base.OnPhotonInstantiate(info);        
       
        rigidbody.centerOfMass = CenterOfMass;

        Dispatcher.Subscribe(EventId.ShellHit, OnShellHit);
        Dispatcher.Subscribe(EventId.StartBurstFire, OnStartBurstFire); 
    }

    protected override void OnIamMaster(EventId id, EventInfo ei)
    {
        base.OnIamMaster(id, ei);
        InitWheels(true);
    }

    private void InitWheels(bool realWheels)
    {
        this.realWheels = realWheels;

        for (int i = 0; i < wheelColliders.Length; i++)
        {
            WheelFrictionCurve forwardFriction = wheelColliders[i].forwardFriction;
            forwardFriction.stiffness = realWheels ? 1f : 0.3f;
            wheelColliders[i].forwardFriction = forwardFriction;

            WheelFrictionCurve sidewaysFriction = wheelColliders[i].sidewaysFriction;
            sidewaysFriction.stiffness = realWheels ? 0.6f : 1f;
            wheelColliders[i].sidewaysFriction = sidewaysFriction;
        }

        if (realWheels)
        {
            rigidbody.drag = 0;
        }
        else
        {
            rigidbody.drag = 2;
        }
    }

    protected override void Unsubscribes()
    {
        base.Unsubscribes();

        Dispatcher.Unsubscribe(EventId.ShellHit, OnShellHit);
        Dispatcher.Unsubscribe(EventId.StartBurstFire, OnStartBurstFire);
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();

        Dispatcher.Unsubscribe(EventId.ShellHit, OnShellHit);
        Dispatcher.Unsubscribe(EventId.StartBurstFire, OnStartBurstFire);
    }
    
    protected override void NormalUpdate()
    {
        base.NormalUpdate();

        if (IsMine)
        {
            MoveStaticGunsight();
        }

        DrawSkidmarks();

        suspensionController.NormalUpdate();
    }

    private bool effectRequestSended = false;

    protected override void PhysicsUpdate()
    {
        if (PhotonView == null || !IsAvailable)
        {
            return;
        }

        if (PhotonView.isMine)
        {
            if (!master.UnFreezed)
            {
                return;
            }

            base.PhysicsUpdate();
            MovePlayer();
            velocity = rigidbody.velocity;
        }
        else
        {
            AnimateClone();
            StoreCloneRotation();
            velocity = CorrectVelocity;
        }
    }

    //protected override void OnCollisionEnter(Collision collision)
    //{
    //    base.OnCollisionEnter(collision);
    //
    //    //if (suspensionController != null)
    //    //{
    //    //    suspensionController.CheckGroundContacts(collision);
    //    //}
    //}

    [PunRPC]
    public override void Respawn(Vector3 position, Quaternion rotation, bool restoreLife, bool firstTime)
    {
        base.Respawn(position, rotation, restoreLife, firstTime);

        yAxisAcceleration = 0;
        xAxisAcceleration = 0;
    }

    public override bool PrimaryFire(Quaternion rotation)
    {
#if UNITY_STANDALONE 
        if (IsMine && Cursor.visible)
        {
            return false;
        }
#endif

        if (StaticType.GameController.Instance<IGameController>().BattleEnded)
        {
            return false;
        }

        MarkActivity();

        //Weapon weapon = weapons[DefaultShellType];

        if (!mainWeapon.IsReady)
        {
            return false;
        }

        //if (PhotonView.isMine)
        //{
        //    BattleGUI.FireButtons[DefaultShellType].SimulateReloading();
        //}

        mainWeapon.RegisterShot();

        Shell shell
            = ShellPoolManager.GetShell(
                shellName: primaryShellInfo.shellPrefabName,
                position: shotPoint.position,
                rotation: rotation);

        shell.OwnerSpeed = Mathf.Abs(currentAcceleration);
        continuousFire = false;

        float damage = Damage;
        bool crit = false;

        if (mainWeapon.consumable != null)
        {
            if (Random.Range(0, 1f) < mainWeapon.consumable.Settings[Setting.CritProbability])
            {
                //Debug.LogError("CRIT: " + damage + " * " + mainWeapon.consumable.Settings[Setting.CritFactor].current + " = " + damage * mainWeapon.consumable.Settings[Setting.CritFactor].current + " shellID: " + shell.specialID);
                damage *= mainWeapon.consumable.Settings[Setting.CritFactor].Current;
                crit = true;
            }
            //Debug.LogError("mainWeapon.consumable == NULL, " + name, gameObject);
        }

        shell.Activate(this, (int)damage, hitMask, -1, DefaultShellType, mainWeapon.consumable, crit);
        return true;
    }

    public override void StartBurst()
    {        
        Dispatcher.Send(EventId.StartBurstFire, new EventInfo_IIV(data.playerId, (int)primaryShellInfo.type, shotPoint.forward), Dispatcher.EventTargetType.ToAll);
    }

    protected override void SetEngineAudio()
    {
    }

    protected override void SetTurretAudio()
    {
    }

    protected override void SetEngineNoise(float t)
    {
    }

    private void OnShellHit(EventId eid, EventInfo ei)
    {
        EventInfo_IIIIV info = (EventInfo_IIIIV)ei;

        if (info.int1 != data.playerId)
        {
            return;
        }

        Vector3 position = transform.TransformPoint(info.vector);

        var shellType = (GunShellInfo.ShellType)info.int4;

        if (shellType == GunShellInfo.ShellType.Usual) //ДЛЯ ЧЕГО ЭТА ХЕРЬ???
        {
            Shell shell
                = ShellPoolManager.GetShell(
                    shellName: GunShellInfo.GetShellInfoForType(shellType).shellPrefabName,
                    position: Vector3.zero,
                    rotation: Quaternion.identity);

            shell.Explosion(position: position, hitsVehicle: true);
        }

        int damage = info.int2;

        //if (!IsMine)
        //    Debug.LogError("ChangeHealth hp:" + Armor + " -= " + damage);

        HPSystem.ChangeHitPoints(damage, info.int3, false);

        if (shellType != GunShellInfo.ShellType.Buff)
        {
        }

        Dispatcher.Send(EventId.TankTakesDamage, new EventInfo_U(info.int1, damage, info.int3, info.int4, position));

        if (!IsBot)
        {
            Player.SetCustomProperties(new Hashtable { { "hl", HPSystem.Armor } });
        }
        else
        {
            Hashtable properties = new Hashtable { { KeyForBotHealth, HPSystem.Armor } };
            PhotonNetwork.room.SetCustomProperties(properties);
        }
    }

    private void OnStartBurstFire(EventId id, EventInfo ei)
    {
        EventInfo_IIV info = (EventInfo_IIV)ei;

        int playerId = info.int1;

        if (playerId != data.playerId || PhotonView.isMine)
        {
            return;
        }

        PrimaryFire(Quaternion.LookRotation(info.vector));        
    }

    private void CheckPhysEffects(bool xAxis, float newSpeed, float oldSpeed)
    {
        if (!xAxis)
        {
            int directionStart = 0;
            bool onStop = false;
            if (Mathf.Abs(newSpeed) > 0.05f)
            {
                if ((Mathf.Sign(oldYAxis) != Mathf.Sign(newSpeed) || Mathf.Abs(oldYAxis) < 0.05f))
                {
                    directionStart = (int)Mathf.Sign(newSpeed);
                }
            }
            else
            {
                if (Mathf.Abs(oldYAxis) > 0.05f)
                {
                    directionStart = -(int)Mathf.Sign(oldSpeed);
                    onStop = true;
                }
            }
            oldYAxis = newSpeed;

            if (directionStart != 0 && Settings[Setting.Accelerate].Max > 0)
            {
                Event(Message.MoveStateChange, directionStart, onStop);
            }
        }
        else
        {
            int directionStart = 0;
            bool needSend = false;
            if (Mathf.Abs(newSpeed) > 0.05f)
            {
                if ((Mathf.Sign(oldXAxis) != Mathf.Sign(newSpeed) || Mathf.Abs(oldXAxis) < 0.05f))
                {
                    directionStart = (int)Mathf.Sign(newSpeed);
                    needSend = true;
                }
            }
            else
            {
                if (Mathf.Abs(oldXAxis) > 0.05f)
                {
                    needSend = true;
                }
            }
            oldXAxis = newSpeed;

            if (needSend && Settings[Setting.Accelerate].Max > 0)
            {
                Event(Message.SteerStateChange, directionStart, Settings[Setting.MovingSpeed].Percent);
            }
        }
    }

    protected float Accelerate(float oldSpeed, float newSpeed, float step, float inertionRatio, bool xAxis)
    {
        bool isForwardDecelerating = newSpeed < oldSpeed && newSpeed >= 0 && oldSpeed > 0;
        bool isBackwardDecelerating = newSpeed > oldSpeed && newSpeed <= 0 && oldSpeed < 0;
        bool isForwardAccelerating = newSpeed > oldSpeed && newSpeed > 0 && oldSpeed >= 0;
        bool isBackwardAccelerating = newSpeed < oldSpeed && newSpeed < 0 && oldSpeed <= 0;
        bool isChangingDirection = (newSpeed > 0 && oldSpeed < 0) || (newSpeed < 0 && oldSpeed > 0);
        bool isIdling = HelpTools.Approximately(oldSpeed, 0) && HelpTools.Approximately(newSpeed, 0);
        bool isDecelerating = isForwardDecelerating || isBackwardDecelerating;

        CheckPhysEffects(xAxis, newSpeed, oldSpeed);        

        if (isDecelerating)
        {
            step *= inertionRatio;
        }

        if (isChangingDirection)
        {
            step *= yAxisChangingDirectionInertion;
        }

        step *= Time.deltaTime;

        bool isSavingSpeed = HelpTools.Approximately(newSpeed, oldSpeed, step);

        if (xAxis)
        {
            if (!isIdling && !isRotatedFrameBefore)
            {
                SetEngineState(EngineState.ForwardAcceleration);
            }

            isRotatedFrameBefore = !isIdling;
        }
        else
        {
            if (isIdling)
            {
                SetEngineState(EngineState.Idle);
            }

            if (!isSavingSpeed)
            {
                if (isForwardAccelerating)
                {
                    SetEngineState(EngineState.ForwardAcceleration);
                }

                if (isBackwardAccelerating)
                {
                    SetEngineState(EngineState.BackwardAcceleration);
                }

                if (isForwardDecelerating)
                {
                    SetEngineState(EngineState.ForwardBrake);
                }

                if (isBackwardDecelerating)
                {
                    SetEngineState(EngineState.BackwardBrake);
                }
            }
            else if (!isIdling)
            {
                if (isForwardAccelerating)
                {
                    SetEngineState(EngineState.Movement);
                }

                if (isBackwardAccelerating)
                {
                    SetEngineState(EngineState.ReverseMovement);
                }
            }
        }

        return Mathf.MoveTowards(oldSpeed, newSpeed, step);
    }

    private float ZoomRotationSpeed
    {
        get
        {
            return Settings[Setting.TurnSpeed].Max * rotationSpeedQualifier * XAxisAcceleration * 0.5f;
        }
    }

    //private GameObject[]            chaines = null;
    //private void SetEffects()
    //{
    //    if (chaines != null)
    //    {
    //        return;
    //    }
    //
    //    chaines = new GameObject[2];
    //    chaines[0] = PutEffectInto(1);
    //    chaines[1] = PutEffectInto(-1);
    //}

    private GameObject PutEffectInto(int dir)
    {
        GameObject go = GameObject.Instantiate<GameObject>(Resources.Load<GameObject>("VFX/armada_tank_chaine_sand"), transform, false);
        go.transform.localPosition = Vector3.right * dir * 1.15f - Vector3.forward * 1.7f;
        return go;
    }

    protected virtual void CheckOnGround()
    {
        if (!realWheels)
        {
            return;
        }

        if (wheelColliders == null || wheelColliders.Length == 0)
        {
            return;
        }

        ISkidmarksPoint point = null;
        onGround = false;
        for (int i = 0; i < wheelColliders.Length; i++)
        {
            WheelHit hit;
            bool groundHit = wheelColliders[i].GetGroundHit(out hit);
            if (SkidmarksDictionary.TryGetValue(wheelColliders[i], out point))
            {
                point.SetGround(groundHit);
            }

            if (groundHit)
            {
                onGround = true;
            }
        }
    }

    protected override void Awake()
    {
        base.Awake();
    }

    private void ApplyAngularVelocity()
    {
        bool moveBackward = Settings[Setting.MovingSpeed].Current < -1;

        if (Mathf.Abs(curMaxRotationSpeed) > 0.1f)
        {
            requiredLocalAngularVelocity = LocalAngularVelocity;

            requiredLocalAngularVelocity.y = (moveBackward && IsBackwardInverted ? -1 : 1) * curMaxRotationSpeed * 0.03f; // всем костылям костыль

            requiredLocalAngularVelocity = transform.TransformDirection(requiredLocalAngularVelocity);

            rigidbody.angularVelocity = requiredLocalAngularVelocity * verticalKoefficient;
        }
    }

#if UNITY_EDITOR && !UNITY_ANDROID
    private void OnGUI()
    {
        if (!StaticType.DevelopmentData.Instance<IDevelopmentData>().DebugOn)
        {
            return;
        }

        if (!IsMine)
        {
            return;
        }

        if (!IsAvailable)
        {
            return;
        }

        float width = fullDebug ? 24 + (windowWidth * (2 + wheelColliders.Length / 4)) : 24 + (windowWidth);
        GUI.Window(777, new Rect(24, Screen.height - 24 * 5, width, 24 * 5), Window, name);
    }
#endif

    private void Window(int id)
    {
        if (id != 777)
        {
            return;
        }

        GUI.Label(new Rect(24, 24, windowWidth, 24), string.Format("SMAX: {0} KM/H", (Settings[Setting.MovingSpeed].Max * 3.6f).Round(1)).RichString("color:red;size:12"));
        GUI.Label(new Rect(24, 48, windowWidth, 24), string.Format("SCUR: {0} KM/H", (Settings[Setting.MovingSpeed].Current * 3.6f).Round(1)).RichString("color:yellow;size:12"));

        GUI.Label(new Rect(24, 72, windowWidth, 24), string.Format("ACUR: {0} N", (currentAcceleration * mTorque).Round(1)).RichString("color:yellow;size:12"));
        GUI.Label(new Rect(24, 96, windowWidth, 24), string.Format("AMAX: {0} N", (Settings[Setting.Accelerate].Max * mTorque).Round(1)).RichString("color:yellow;size:12"));

        if (fullDebug)
        {
            for (int i = 0; i < wheelColliders.Length; i++)
            {
                GUI.Label(new Rect(24 + windowWidth + windowWidth * (i % 4), 24 + 24 * (i / 4), windowWidth, 24), string.Format("W[{0}] RPM:{1}; M:{2}; BR:{3}", wheelColliders[i].name.Substring(wheelColliders[i].name.Length - 4, 4), (wheelColliders[i].rpm).Round(1), (wheelColliders[i].motorTorque).Round(1), (wheelColliders[i].brakeTorque).Round(1)).RichString("color:cyan;size:12"));
            }
        }
    }

    private Clamper Acceleration()
    {
        if (!Settings.Contains(Setting.Accelerate))
        {
            return new Clamper();
        }

        Clamper accelerate = Settings[Setting.Accelerate];

        if (Mathf.Abs(YAxisAcceleration) > 0.05f)
        {
            accelerate.Current += Time.fixedDeltaTime * 0.05f / Mathf.Clamp(verticalKoefficient, 0.1f, 1);

            if (accelerate.Peaked)
            {
                accelerate.Refresh();
            }
        }
        else
        {
            accelerate.Current -= Time.fixedDeltaTime;

            if (accelerate.Minimum)
            {
                accelerate.Reset();
            }
        }
        return accelerate;
    }

    protected virtual void MoveWheels()
    {
        Clamper movingSpeed = Settings[Setting.MovingSpeed];
        movingSpeed.Current = LocalVelocity.z;

        float currentSpeed = movingSpeed.Current;

        float motorTorque = 0;
        float brakeTorque = 0;

        bool speedClamped = Mathf.Abs(movingSpeed.Current) < movingSpeed.Max;
        bool rpmClamped = Mathf.Abs(wheelColliders[0].rpm) < 350;
        bool accelerationApproximated = Mathf.Abs(currentAcceleration) > 0.1f;
        bool accelerationClamped = Settings[Setting.Accelerate].Max > 0;
        bool reverse = accelerationApproximated && Mathf.Sign(currentAcceleration) != Mathf.Sign(movingSpeed.Current);

        if (speedClamped && rpmClamped && accelerationApproximated && accelerationClamped)
        {
            motorTorque = currentAcceleration * mTorque / Mathf.Clamp(verticalKoefficient / 2, 0.1f, 1);

            if (Mathf.Sign(currentAcceleration) != Mathf.Sign(movingSpeed.Current))
            {
                motorTorque *= 10;
            }
        }
        else
        {
            if (Mathf.Abs(wheelColliders[0].rpm) > 150 || !accelerationApproximated || !accelerationClamped || accelerationApproximated)
            {
                brakeTorque = mTorque * brakeKoefficient;
            }
        }

        //if (oldMotor == motorTorque && oldBrake == brakeTorque)
        //{
        //    return;
        //}

        for (int i = 0; i < wheelColliders.Length; i++)
        {
            //if (oldBrake != brakeTorque)
            {
                wheelColliders[i].brakeTorque = brakeTorque;
                oldBrake = brakeTorque;
            }

            //if (oldMotor != motorTorque)
            {
                wheelColliders[i].motorTorque = motorTorque;
                oldMotor = motorTorque;
            }
        }
    }

    
    public override void MovePlayer()
    {        
        Profiler.BeginSample("MovePlayer/CheckOnGround");
        CheckOnGround();
        Profiler.EndSample();

        Profiler.BeginSample("MovePlayer/Acceleration");
        Clamper acceleration = Acceleration();
        Profiler.EndSample();

        Profiler.BeginSample("MovePlayer/currentAcceleration");
        float yAxisAcceleration = YAxisAcceleration;
        currentAcceleration = acceleration.Current * yAxisAcceleration;
        curMaxRotationSpeed = Settings[Setting.TurnSpeed].Max * rotationSpeedQualifier * XAxisAcceleration;
        Profiler.EndSample();

        Profiler.BeginSample("MovePlayer/MarkActivity0");
        if (Mathf.Abs(currentAcceleration) > MOVEMENT_SPEED_THRESHOLD)
        {
            MarkActivity();
        }
        Profiler.EndSample();

        Profiler.BeginSample("MovePlayer/MarkActivity1");
        if (Mathf.Abs(curMaxRotationSpeed) > MOVEMENT_SPEED_THRESHOLD)
        {
            MarkActivity();
        }
        Profiler.EndSample();

        Profiler.BeginSample("MovePlayer/OnGround");
        OnGround();
        Profiler.EndSample();

        Profiler.BeginSample("MovePlayer/transmissionController.Receive");
        if (transmissionController != null)
        {
            transmissionController.Receive();
        }
        Profiler.EndSample();

        Profiler.BeginSample("MovePlayer/gravity");
        if (gravity > 0)
        {
            rigidbody.AddForce((Vector3.down) * gravity * rigidbody.drag, ForceMode.Acceleration);
        }
        Profiler.EndSample();

        Profiler.BeginSample("MovePlayer/StoreVehiclePosition");
        StoreVehiclePosition();
        Profiler.EndSample();

        Profiler.BeginSample("MovePlayer/SoundRequest/Movement");
        if (yAxisAcceleration != oldMovement)
        {
            SendMessageToFX(Message.SoundRequest, SoundRequestType.Continuous, SoundContinuousType.Movement, yAxisAcceleration);
            oldMovement = yAxisAcceleration;
        }
        Profiler.EndSample();

        Profiler.BeginSample("MovePlayer/SoundRequest/Turn");
        float turnSound = Mathf.Abs(YAxisAcceleration) > 0 ? 0 : XAxisAcceleration;
        if (turnSound != oldTurn)
        {
            SendMessageToFX(Message.SoundRequest, SoundRequestType.Continuous, SoundContinuousType.Turn, turnSound);
            oldTurn = turnSound;
        }
        Profiler.EndSample();
    }

    private void OnGround()
    {
        if (!onGround)
        {
            return;
        }

        if (Mathf.Abs(curMaxRotationSpeed) > 0.1f && Mathf.Abs(currentAcceleration) < 0.05f)
        {
            currentAcceleration = 0.1f;
        }

        requiredLocalVelocity = LocalVelocity;
        requiredLocalVelocity.z = Mathf.Abs(currentAcceleration) > 0.05f ? currentAcceleration : 0;
        Vector3 transformDirection = transform.TransformDirection(requiredLocalVelocity);
        angle = Vector3.Angle(transformDirection, Vector3.up) * Mathf.Deg2Rad;

        float startKoef = IsBot ? 1 : 0.75f;
        verticalKoefficient = Mathf.Clamp01(startKoef - Mathf.Cos(angle));

        if (Vector3.Angle(transform.forward, rigidbody.velocity) > 5.0f)
        {
            requiredLocalVelocity.x = 0;
        }

        if (realWheels && wheelColliders.Length > 0)
        {
            MoveWheels();
        }
        else
        {
            transformDirection.y = 0;
            transformDirection *= verticalKoefficient;
            transformDirection.y = rigidbody.velocity.y;
            rigidbody.velocity = transformDirection;
        }

        ApplyAngularVelocity();

        bool needPlay = Mathf.Abs(currentAcceleration) > 0.05f;
        if (effectRequestSended != needPlay)
        {
            SendMessageToFX(Message.EffectRequest, EffectTarget.Movement, needPlay);
            SendMessageToFX(Message.EffectRequest, EffectTarget.Engine, needPlay);
            effectRequestSended = needPlay;
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

    private void SetEngineState(EngineState engineState)
    {
        if (engineState != lastEngineState)
        {
            Dispatcher.Send(EventId.EngineStateChanged, new EventInfo_II(data.playerId, (int)engineState));
        }

        lastEngineState = engineState;
    }

    private void MoveStaticGunsight()
    {
        if (turret == null)
        {
            return;
        }

        Vector3 sightPoint = StaticContainer.MainCamera.Camera.WorldToViewportPoint(turret.position + turret.forward * 2000f);

        //sightPoint = camera2d.ViewportToWorldPoint(sightPoint);

        //  BattleGUI.Instance.StaticGunsight.transform.position = sightPoint;
    }

    private void DrawSkidmarks()
    {
        if (velocity.sqrMagnitude < MOVEMENT_SPEED_THRESHOLD && AngularVelocity.sqrMagnitude < MOVEMENT_SPEED_THRESHOLD)
        {
            return;
        }

        foreach (SkidmarksPoint skidmarksPoint in SkidmarksPoints)
        {
            if (onGround)
            {
                skidmarksPoint.Draw();
            }
            else
            {
                skidmarksPoint.Chop();
            }
        }
    }

    private void ChopSkidMarks()
    {
        foreach (SkidmarksPoint skidmarksPoint in SkidmarksPoints)
        {
            skidmarksPoint.Chop();
        }
    }

    public override void Death(int attackerId = -1)
    {
        base.Death(attackerId);

        ChopSkidMarks();
    }

    protected override void OnCollisionExit(Collision collision)
    {
        if (wheelColliders.Length == 0)
        {
            base.OnCollisionExit(collision);
        }
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
}