using System;

using CodeStage.AntiCheat.ObscuredTypes;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
#if UNITY_EDITOR
using UnityEditor;
#endif
using Hashtable = ExitGames.Client.Photon.Hashtable;

[System.Serializable]
public class AxleInfo {
    public WheelMF leftWheel;
    public WheelMF rightWheel;
    public bool motor; // is this wheel attached to motor?
    public bool steering; // does this wheel apply steer angle?
    [Range(0f, 1f)]
    public float steerQualifier = 1f;
    public bool limitVisualAngle = false;
    [Range(0f, 90f)]
    public float maxVisualAngle = 90f;
    [Tooltip("Increase for longer suspension, vehicle will ride higher")]
    public float suspensionHeight = .3f;
    //front and side grip for the two wheels of the vehicle 
    public float forwardGrip = 2f;
    public float sideGrip = 1.5f;
}


public class TankControllerMF : TankController
{
    [Header("Настройки Metal Force")]

    [Header("Ссылки")]
    public string shellPrefabName = "Shell_1";

    [Header("Звуки")]
    public AudioClip collisionSound;
    public AudioClip reloadingSound;
    public AudioClip machinegunReloadingSound;
    public AudioClip agsReloadingSound;
    public AudioClip atgwReloadingSound;

    #region Arcade style wheel vehicle phisycs
    [Header("Управление по осям колес")]
    public List<AxleInfo> axleInfos; // the information about each individual axle

    [Space (10)]
    [Header("Vehicle Performance Settings")]
    //[Tooltip("Power of the Vehicle Engine")]
    public ObscuredFloat engineTorque = 600f;
    public ObscuredFloat brakingPower = 300f;
    //[Tooltip("Input sensitivity for the sharpness of the steering")]
    public ObscuredFloat steeringSensitivity = 20f;
    [Tooltip("Raise this value to tighten the suspension, Lower the value to loosen the suspension")]
    public float suspSpring = 35000f;
    [Tooltip("Raise this value to quicken the suspension response")]
    public float suspDamping = 1200f;
    public float suspTarget = .5f;

    public ObscuredFloat downForce = 3f; //the downforce on the vehicle based on it's velocity  
    private ObscuredFloat force; //private variable for applying the downForce. 
    public float stopSpeed = 0.4f;
    public float speed = 0;
    public float speedKmH = 0;

    [Space (10)]
    [Header("Vehicle stabilization")]
    public float stability = 0.3f;
    public float stabilitySpeed = 2.0f;
    public float returnToUpTorque = 400.0f;


    // The velocity at which drag should begin being applied.
    public ObscuredFloat dragStartVelocity { get { return .8f * maxVelocity; } }

    // The velocity at which drag should equal maxDrag. 
    private ObscuredFloat dragMaxVelocity;

    // The maximum allowed velocity. The velocity will be clamped to keep
    // it from exceeding this value. (Note: this value should be greater than
    // or equal to dragMaxVelocity.)
    public ObscuredFloat maxVelocity { get { return MaxSpeed / 3.6f; } }

    [Space (10)]
    [Header ("Max speed limit")]
    // The maximum drag to apply. This is the value that will
    // be applied if the velocity is equal or greater
    // than dragMaxVelocity. Between the start and max velocities,
    // the drag applied will go from 0 to maxDrag, increasing
    // the closer the velocity gets to dragMaxVelocity.
    //[Tooltip("The maximum drag you wish to apply.")]
    public ObscuredFloat maxDrag = 1.0f;

    // The original drag of the object, which we use if the velocity is below dragStartVelocity.
    private ObscuredFloat originalDrag;
    // Cached values used in FixedUpdate
    private ObscuredFloat sqrDragStartVelocity;
    private ObscuredFloat sqrDragVelocityRange;
    private ObscuredFloat sqrMaxVelocity;

    public bool IsRotateOnPlace {
        get { return isRotateOnPlace; }
        set {
            if (value != isRotateOnPlace) {
                isRotateOnPlace = value;
                CalcCachedSpeedLimitValues ();
            }
        }
    }

    private bool isBrake = false;
    private bool isStopped = false;
    private bool isRotateOnPlace = false;

    private bool isYControl = false;
    private bool isXControl = false;

    #endregion

    public override ObscuredFloat MaxSpeed {
        get { return base.MaxSpeed; }
        set {
            base.MaxSpeed = value;
            CalcCachedSpeedLimitValues ();
        }
    }

    public override float CurrentSpeed {
        get {
            return speedKmH;
        }
    }

    protected float deltaRotationMagnitude;
    protected Vector3 deltaRotationAxis;
    protected Quaternion lastRotation;
    protected Camera camera2d;
    protected SkidmarksPoint[] skidmarkPoints;
    private bool useLeftATGWShotPoint;
    private bool isChoppedSkidmarks;
    private float correctSteering;
    private Transform shotPointMachineGun;
    private Transform shotPointAGS;
    private Transform shotPointATGWLeft;
    private Transform shotPointATGWRight;
    private VehicleStateDispatcher stateDispatcher;

    public override Transform BodyMeshTransform { get { return bodyMeshTransform = bodyMeshTransform ?? Body.Find ("Mesh_Body") ?? Body; } }

    public override Vector3 AngularVelocity {
        get {
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

    public override Transform GetShotPoint(BattleItem weapon)
    {
        if (weapon is MachineGun)
            return ShotPointMachineGun;

        if (weapon is GrenadeLauncher)
            return ShotPointAGS;

        if (weapon is ATGWMissile)
            return ShotPointATGW;

        return ShotPoint;
    }

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

    private Transform ShotPointMachineGun
    {
        get { return shotPointMachineGun = shotPointMachineGun ?? transform.Find("Body/Turret/ShotPoint_GUN"); }
    }

    private Transform ShotPointAGS
    {
        get { return shotPointAGS = shotPointAGS ?? transform.Find("Body/Turret/ShotPoint_AGS"); }
    }

    private Transform ShotPointATGW
    {
        get
        {
            shotPointATGWLeft = shotPointATGWLeft ?? transform.Find("Body/Turret/ShotPoint_ptur_L");
            shotPointATGWRight = shotPointATGWRight ?? transform.Find("Body/Turret/ShotPoint_ptur_R");

            useLeftATGWShotPoint = !useLeftATGWShotPoint;

            return useLeftATGWShotPoint ? shotPointATGWLeft : shotPointATGWRight;
        }
    }



    protected override float TurretAxisControl {
        get {
            float control = base.TurretAxisControl;
            if (aimingController != null) {
                if (Mathf.Approximately(control, 0f) && !TurretCentering) {
                    control = aimingController.turretRotationAutoaim;
                }
                else {
                    aimingController.ResetAutoaim();
                }
            }
            return control;
        }
    }


    private static int ms_cnt = 0;
    private static List<int> ms_ids = new List<int>();
    private int m_id = 0;

    public override void OnPhotonInstantiate(PhotonMessageInfo info)
    {
        skidmarkPoints = GetComponentsInChildren<SkidmarksPoint>();

        camera2d = BattleGUI.Instance.GuiCamera;
        BodyMesh = BodyMeshTransform.gameObject.GetComponentInChildren<MeshFilter> (true).sharedMesh;

        stateDispatcher = new VehicleStateDispatcher();
        stateDispatcher.Init(this);

        base.OnPhotonInstantiate(info);

        rb.collisionDetectionMode = PhotonView.isMine ? CollisionDetectionMode.ContinuousDynamic : CollisionDetectionMode.Continuous;
        rb.maxDepenetrationVelocity = 5;

        // Setup phisyc for our vehicle
        //You can adjust from here for certain platforms. 24 is a good balance between PC and Mobile platforms.   
        //Physics.defaultSolverIterations = 24;

        // set up JointSpring for the suspension settings
        JointSpring suspJointSpring = new JointSpring();

        suspJointSpring.damper = suspDamping;
        suspJointSpring.spring = suspSpring;
        suspJointSpring.targetPosition = suspTarget;

        //Make sure to set all WheelColliders to low Mass value such as 1, otherwise the wheel will not slow and accelerate as it should. 
        foreach (AxleInfo axleInfo in axleInfos) {
            WheelFrictionCurve curveForward = new WheelFrictionCurve();
            WheelFrictionCurve curveSideways = new WheelFrictionCurve();

            curveForward.extremumSlip = .4f;
            curveForward.extremumValue = 1f;
            curveForward.asymptoteSlip = .8f;
            curveForward.asymptoteValue = .5f;

            curveSideways.extremumSlip = .2f;
            curveSideways.extremumValue = 1f;
            curveSideways.asymptoteSlip = .5f;
            curveSideways.asymptoteValue = .75f;

            curveForward.stiffness = axleInfo.forwardGrip;
            curveSideways.stiffness = axleInfo.sideGrip;


            //Allow the designer to adjust the suspension ride height for rear axle and front axle of vehicle
            axleInfo.leftWheel.col.suspensionDistance = axleInfo.suspensionHeight;
            axleInfo.rightWheel.col.suspensionDistance = axleInfo.suspensionHeight;

            axleInfo.leftWheel.col.suspensionSpring = suspJointSpring;
            axleInfo.rightWheel.col.suspensionSpring = suspJointSpring;

            axleInfo.leftWheel.col.forwardFriction = curveForward;
            axleInfo.leftWheel.col.sidewaysFriction = curveSideways;

            axleInfo.rightWheel.col.forwardFriction = curveForward;
            axleInfo.rightWheel.col.sidewaysFriction = curveSideways;

            axleInfo.leftWheel.isSteering = axleInfo.steering;
            axleInfo.leftWheel.limitVisualAngle = axleInfo.limitVisualAngle;
            axleInfo.leftWheel.maxVisualAngle = axleInfo.maxVisualAngle;
            axleInfo.rightWheel.isSteering = axleInfo.steering;
            axleInfo.rightWheel.limitVisualAngle = axleInfo.limitVisualAngle;
            axleInfo.rightWheel.maxVisualAngle = axleInfo.maxVisualAngle;

            if (SystemInfo.deviceType != DeviceType.Handheld) {
                axleInfo.leftWheel.col.ConfigureVehicleSubsteps (5f, 10, 10);
                axleInfo.rightWheel.col.ConfigureVehicleSubsteps (5f, 10, 10);
            }
        }

        // Init max speed limit
        originalDrag = rb.drag;

        // Calculate cached values
        CalcCachedSpeedLimitValues ();

        Dispatcher.Subscribe(EventId.StartBurstFire, OnStartBurstFire);
        Dispatcher.Subscribe(EventId.ShellHit, OnShellHit);

        ms_cnt++;
        m_id = data.playerId;
        ms_ids.Add(m_id);
        UberDebug.LogChannel("TankControllerMF", "+++ TankControllerMF for {0}, all objects = {1}", m_id, ms_cnt);
    }

    void CalcCachedSpeedLimitValues () {
        dragMaxVelocity = maxVelocity - 0f; //always make sure drag max is lower than max velocity.

        sqrDragStartVelocity = dragStartVelocity * dragStartVelocity;
        sqrDragVelocityRange = (dragMaxVelocity * dragMaxVelocity) - sqrDragStartVelocity;
        sqrMaxVelocity = maxVelocity * maxVelocity;

        if (isRotateOnPlace) {
            sqrDragStartVelocity *= .1f;
            sqrDragVelocityRange *= .1f;
            sqrMaxVelocity *= .1f;
            dragMaxVelocity *= .1f;
        }
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();

        Dispatcher.Unsubscribe(EventId.StartBurstFire, OnStartBurstFire);
        Dispatcher.Unsubscribe(EventId.ShellHit, OnShellHit);

        ms_cnt--;
        ms_ids.Remove(m_id);
        string ids = string.Join(",", ms_ids.Select(id => id.ToString()).ToArray());
        UberDebug.LogChannel("TankControllerMF", "--- TankControllerMF for {0}, all objects = {1}, left = {2}", m_id, ms_cnt, ids);
    }


    private void LateUpdate () {
        if (IsMain && IsAvailable) {
            MoveStaticGunsight ();
        }
    }

    public float cloneSteer = 0f;
    protected override void Update()
    {
        base.Update();

        if (PhotonView.isMine) {
            speedKmH = speed * 3.6f;

            isYControl = !Mathf.Approximately (YAxisControl, 0f);
            isXControl = !Mathf.Approximately (XAxisControl, 0f);

            stateDispatcher.RegisterMovement (YAxisControl);

            isStopped = HelpTools.Approximately (speed, 0f, stopSpeed);
            isBrake = (isStopped && !isYControl) // Стоим
                   || (speed > stopSpeed && YAxisControl < 0f) || (speed < -stopSpeed && YAxisControl > 0f); // Двигаемся

            if (!isRotateOnPlace && isStopped && isXControl && !isYControl) { // Start rotate on place
                IsRotateOnPlace = true;
            }
            if (isRotateOnPlace && isYControl) { // Stop on place rotation when accelerate
                IsRotateOnPlace = false;
            }
            if (isRotateOnPlace && !isXControl) { // Stop on place rotation
                IsRotateOnPlace = false;
            }

            if (!IsBot) {
                StoreVehiclePosition ();
            }
        }
        else {
            // Speed and steer for clone
            speed = Vector3.Dot (rb.velocity, transform.forward);
            //cloneSteer = Mathf.LerpAngle (cloneSteer, Mathf.Clamp (LocalAngularVelocity.y, -1f, 1f) * steeringSensitivity * Mathf.Sign (speed), 5f * Time.deltaTime);
            StoreCloneRotation ();
        }
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
        }
    }

    protected override void OnCollisionEnter(Collision collision)
    {
        base.OnCollisionEnter(collision);
    }

    [PunRPC]
    public override void Respawn(Vector3 position, Quaternion rotation, bool restoreLife, bool firstTime)
    {
        base.Respawn(position, rotation, restoreLife, firstTime);

        if (IsMain) {
            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.ResetInertiaTensor ();

            foreach (AxleInfo axleInfo in axleInfos) {
                axleInfo.leftWheel.col.motorTorque = 0;
                axleInfo.leftWheel.col.brakeTorque = 0;
                axleInfo.leftWheel.col.steerAngle = 0;

                axleInfo.rightWheel.col.motorTorque = 0;
                axleInfo.rightWheel.col.brakeTorque = 0;
                axleInfo.rightWheel.col.steerAngle = 0;
            }
        }
    }


    protected override void OnTankKilledForMain (EventId eid, EventInfo ei) {
        EventInfo_III info = (EventInfo_III) ei;
        if (info.int1 != data.playerId) {
            return;
        }

        base.OnTankKilledForMain (eid, ei);

        if (PhotonView.isMine) {
            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.ResetInertiaTensor ();

            foreach (AxleInfo axleInfo in axleInfos) {
                axleInfo.leftWheel.col.motorTorque = 0;
                axleInfo.leftWheel.col.brakeTorque = 0;
                axleInfo.leftWheel.col.steerAngle = 0;

                axleInfo.rightWheel.col.motorTorque = 0;
                axleInfo.rightWheel.col.brakeTorque = 0;
                axleInfo.rightWheel.col.steerAngle = 0;
            }
        }
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
                shellName:  shellPrefabName,
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

    override protected float RotationSpeed {
        get { return MaxSpeed * rotationSpeedQualifier * XAxisControl; }
    }

    float motor;
    float steering;
    float vSqr;
    float stopCoeff;
    public override void MovePlayer()
    {
        #region Arcade Wheel Vehicle
        //track the vehicle's speed for reading out the MPH or applying downforce to the vehicle 
        speed = Vector3.Dot (rb.velocity, transform.forward);

        if (!Mathf.Approximately (speed, 0f)) {
            MarkActivity ();
        }

        bool isMoveBackward = speed < 0f;
        motor = engineTorque * YAxisControl * 10f;
        steering = steeringSensitivity * XAxisControl * (isMoveBackward && IsBackwardInverted ? -1 : 1);

        #region Rotation in a place
        if (isRotateOnPlace) {
            //curMaxRotationSpeed = BattleCamera.Instance.IsZoomed ? ZoomRotationSpeed : RotationSpeed;
            motor = engineTorque * rotationSpeedQualifier * Mathf.Abs (XAxisControl);
            steering = Mathf.Min (steeringSensitivity * 2, 70f) * XAxisControl;// * (isMoveBackward && IsBackwardInverted ? -1 : 1);
            isBrake = false;
        }
        #endregion

        #region Max speed limit
        // We use sqrMagnitude instead of magnitude for performance reasons.
        vSqr = rb.velocity.sqrMagnitude;

        if (vSqr > sqrDragStartVelocity) {
            stopCoeff = (vSqr - sqrDragStartVelocity) / sqrDragVelocityRange;
            rb.drag = Mathf.Lerp (originalDrag, maxDrag, Mathf.Clamp01 (stopCoeff));

            // Clamp the motor, if necessary
            motor *= Mathf.Clamp01 (1f - stopCoeff);
        }
        else {
            rb.drag = originalDrag;
        }
        #endregion


        foreach (AxleInfo axleInfo in axleInfos) {
            if (axleInfo.motor) {
                axleInfo.leftWheel.col.motorTorque = motor;
                axleInfo.rightWheel.col.motorTorque = motor;
            }
            if (axleInfo.steering) {
                axleInfo.leftWheel.col.steerAngle = steering * axleInfo.steerQualifier;
                axleInfo.rightWheel.col.steerAngle = steering * axleInfo.steerQualifier;
            }
            else {
                axleInfo.leftWheel.col.steerAngle = 0f;
                axleInfo.rightWheel.col.steerAngle = 0f;
                if (isRotateOnPlace) {
                    axleInfo.leftWheel.col.motorTorque = 0;
                    axleInfo.rightWheel.col.motorTorque = 0;
                }
            }
            if (isBrake) {
                //apply the brake power variable here and slow the wheelCollider revolutions
                axleInfo.leftWheel.col.brakeTorque = brakingPower;
                axleInfo.rightWheel.col.brakeTorque = brakingPower;
            }
            else {
                //if brakes are not being applied 
                axleInfo.leftWheel.col.brakeTorque = 0;
                axleInfo.rightWheel.col.brakeTorque = 0;
            }
        }

        //Call method that will apply a downforce to vehicle based on velocity 
        force = downForce * rb.velocity.sqrMagnitude;
        rb.AddForceAtPosition (force * -transform.up, transform.position);
        #endregion


        #region Stabilization
        Vector3 predictedUp = Quaternion.AngleAxis(
            rb.angularVelocity.magnitude * Mathf.Rad2Deg * stability / stabilitySpeed,
            rb.angularVelocity) * transform.up;

        Vector3 torqueVector = Vector3.Cross(predictedUp, Vector3.up);
        // Uncomment the next line to stabilize on only 1 axis. 
        //torqueVector = Vector3.Project(torqueVector, transform.forward);

        float angle = Vector3.Dot(transform.up, Vector3.up);
        if (angle < -.85f) {
            torqueVector.x = returnToUpTorque * Mathf.Sign(rb.angularVelocity.x);
            //torqueVector.z = returnToUpTorque * Mathf.Sign(torqueVector.z);
        }

        rb.AddTorque (torqueVector * stabilitySpeed * stabilitySpeed);
        #endregion
    }

    public override void MoveClone () {
        if (isExploded) {
            if (Turret)
                Turret.localEulerAngles = Vector3.zero;

            return;
        }
        #region Smooth network move
        double currentTime = PhotonNetwork.time;
        double interpolationTime = currentTime - interpolationBackTime;
        // We have a window of interpolationBackTime where we basically play 
        // By having interpolationBackTime the average ping, you will usually use interpolation.
        // And only if no more data arrives we will use extrapolation

        // Use interpolation
        // Check if latest state exceeds interpolation time, if this is the case then
        // it is too old and extrapolation should be used
        if (m_BufferedState[0].timestamp > interpolationTime) {
            for (int i = 0; i < m_TimestampCount; i++) {
                // Find the state which matches the interpolation time (time+0.1) or use last state
                if (m_BufferedState[i].timestamp <= interpolationTime || i == m_TimestampCount - 1) {
                    // The state one slot newer (<100ms) than the best playback state
                    State rhs = m_BufferedState[Mathf.Max(i-1, 0)];
                    // The best playback state (closest to 100 ms old (default time))
                    //State lhs = m_BufferedState[i];

                    // Use the time between the two slots to determine if interpolation is necessary
                    //double length = rhs.timestamp - lhs.timestamp;
                    //float t = 0.0F;
                    //// As the time difference gets closer to 100 ms t gets closer to 1 in 
                    //// which case rhs is only used
                    //if (length > 0.0001)
                    //    t = (float)((interpolationTime - lhs.timestamp) / length);

                    // if t=0 => lhs is used directly
                    cloneSteer = Mathf.MoveTowardsAngle (cloneSteer, rhs.steering, 180 * Time.deltaTime);
                    if (Turret)
                        Turret.localEulerAngles = new Vector3 (0, Mathf.MoveTowardsAngle (Turret.localEulerAngles.y, rhs.tRot, 180 * Time.deltaTime), 0);
                    return;
                }
            }
        }
        // Use extrapolation. Here we do something really simple and just repeat the last
        // received state. You can do clever stuff with predicting what should happen.
        else {
            State latest = m_BufferedState[0];
            cloneSteer = latest.steering;

            if (Turret)
                Turret.localEulerAngles = new Vector3 (0, latest.tRot, 0);
        }
        #endregion
    }

    public override void AnimateClone()
    {
        //if (transmissionController != null)
        //{
        //    transmissionController.Spin(Wheel.Mode.All, LocalVelocity.z);
        //    transmissionController.Turn(LocalAngularVelocity.y);
        //}
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
    }

    private void DrawSkidmarks()
    {
        if (Mathf.Abs(speed) < SKIDMARK_SPEED_THRESHOLD)
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

    public override void OnPhotonSerializeView (PhotonStream stream, PhotonMessageInfo info) {
        //if (isExploded)
        //    return;

        if (stream.isWriting) {
            stream.SendNext ((short)steering);

            if (Turret)
                stream.SendNext (Turret.localEulerAngles.y);
        }
        else {
            MarkActivity ();

            correctSteering = (short)stream.ReceiveNext ();
            correctTurretAngle = (float)stream.ReceiveNext ();

            #region Smooth network move 
            // Shift buffer contents, oldest data erased, 18 becomes 19, ... , 0 becomes 1
            for (int i = m_BufferedState.Length - 1; i >= 1; i--) {
                m_BufferedState[i] = m_BufferedState[i - 1];
            }

            // Save currect received state as 0 in the buffer, safe to overwrite after shifting
            State state;
            state.timestamp = info.timestamp;
            state.pos = correctPosition;
            state.rot = correctRotation;
            state.vel = correctVelocity;
            state.tRot = correctTurretAngle;
            state.steering = correctSteering;
            m_BufferedState[0] = state;

            // Increment state count but never exceed buffer size
            m_TimestampCount = Mathf.Min (m_TimestampCount + 1, m_BufferedState.Length);

            // Check integrity, lowest numbered state in the buffer is newest and so on
            for (int i = 0; i < m_TimestampCount - 1; i++) {
                if (m_BufferedState[i].timestamp < m_BufferedState[i + 1].timestamp)
                    Debug.Log ("State inconsistent");
            }

            //Debug.Log("stamp: " + info.timestamp + "my time: " + PhotonNetwork.time + "delta: " + (PhotonNetwork.time - info.timestamp));
            #endregion

            if (settingSpawnPosition) {
                if (Turret)
                    Turret.localEulerAngles = new Vector3 (0, correctTurretAngle, 0);

                settingSpawnPosition = false;
            }

            currentCorrection = 0;
        }
    }


#if UNITY_EDITOR
    //override protected void OnDrawGizmos()
    //{
    //    base.OnDrawGizmos();

    //    #region Stabilization
    //    if (rb) {
    //        Vector3 predictedUp = Quaternion.AngleAxis(
    //            rb.angularVelocity.magnitude * Mathf.Rad2Deg * stability / stabilitySpeed,
    //            rb.angularVelocity) * transform.up;

    //        Vector3 torqueVector = Vector3.Cross(predictedUp, Vector3.up);
    //        // Uncomment the next line to stabilize on only 1 axis. 
    //        //torqueVector = Vector3.Project(torqueVector, transform.forward);

    //        float angle = Vector3.Dot(transform.up, Vector3.up);
    //        if (angle < -.85f)
    //        {
    //            //torqueVector.x = 3f * -Mathf.Sign(torqueVector.x);
    //            torqueVector.x = returnToUpTorque;
    //        }

    //        Handles.Label(transform.position, string.Format ("a:{0}\nx:{1}\ny:{2}\nz:{3}", angle, torqueVector.x, torqueVector.y, torqueVector.z));
    //    }
    //    #endregion
    //}
#endif
}
