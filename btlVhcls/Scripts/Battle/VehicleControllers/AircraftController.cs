using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using CodeStage.AntiCheat.ObscuredTypes;

public class AircraftController : FlightController
{
    [Header("Настройки для самолёта")]

    [Header("Ссылки")]
    public Transform indicatorPoint;
    public List<Transform> rocketLaunchPoints;
    public AircraftAnimationController animationController;

    [Header("Управление")]
    public ObscuredFloat maxInclineAngle = 70.0f;
    public ObscuredFloat inclineSmooth = 0.03f;
    public ObscuredFloat verticalTorqueAcceleration = 2.0f;
    public ObscuredFloat horizontalTorqueAcceleration = 2.5f;
    public ObscuredFloat torqueBrake = 0.1f;
    public ObscuredFloat stabilizationDelay = 0.2f;
    public ObscuredFloat minUnstabilizedAngle = 45.0f;
    public ObscuredFloat shakingRatio = 0.6f;

    [Header("Звуки")]
    public AudioClip rocketGuidanceSound;

    protected VehicleEffectsControllerBOW effectsController;

    private const float STABILIZATION_INPUT_THRESHOLD = 0.01f;

    private static readonly ObscuredFloat SPEED_RATIO = 2.0f;
    private static readonly ObscuredFloat ODOMETER_RATIO = 0.04f;
    private static readonly ObscuredFloat CORRECTION_TIME = 0.5f;
    private static readonly ObscuredFloat MAX_SHOOT_ANGLE = 20.0f;
    private static readonly ObscuredFloat AIM_DISTANCE_FACTOR = 1.0f;

    private bool stabilization;
    private int currentRocketLaunchPointIndex;
    private float angleToHorizon;
    private Transform cannonEnd;
    private AudioSource rocketGuidanceAudio;
    private IEnumerator stabilizationRoutine;
    private IEnumerator missileGuidanceRoutine;
    private float sqrMaxAimDistance;
    private float minAimAngleCos;

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

    public bool IsAimedSACLOSMissile
    {
        get { return HelpTools.Approximately(MissileAimProgress, 1); }
    }

    public float MissileAimProgress
    {
        get; private set;
    }

    protected override bool FireButtonPressed
    {
        get
        {
            return continuousFire
                ? XDevs.Input.GetButton("Fire Primary") || XDevs.Input.GetButton("Launch IRCM")
                : XDevs.Input.GetButtonDown("Fire Primary") || XDevs.Input.GetButtonDown("Launch IRCM");
        }
    }

    public override bool IsRequirePrimaryFire
    {
        get
        {
            return TargetAimed &&
                   !BattleGUI.IsWindowOnScreen &&
                   (transform.position - aimPointInfo.point).sqrMagnitude < MaxAimDistance * MaxAimDistance;
        }
    }

    public override bool IsRequireSecondaryFire
    {
        get { return IsAimedSACLOSMissile && FireButtonPressed && !BattleGUI.IsWindowOnScreen; }
    }

    public override float MaxShootAngle
    {
        get { return MAX_SHOOT_ANGLE; }
    }

    public int QualityLevel
    {
        get { return PhotonView.isMine ? QualitySettings.GetQualityLevel() : 1; }
    }

    protected override float OdometerRatio
    {
        get { return ODOMETER_RATIO; }
    }

    protected override float SpeedRatio
    {
        get { return SPEED_RATIO; }
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
        get { return RadarController.MaxVisibleDistance/AIM_DISTANCE_FACTOR; }
    }

    protected override Vector3 IndicatorDeltaOffset
    {
        get { return Vector3.up * 5.0f; }
    }

    protected override Transform IndicatorPoint
    {
        get { return indicatorPoint = indicatorPoint ?? transform.Find("IndicatorPoint"); }
    }
    
    protected virtual float MissileAimingDuration
    {
        get { return GameManager.MissileAimingDuration; }
    }

    protected virtual float StabilizationInputThreshold
    {
        get { return STABILIZATION_INPUT_THRESHOLD; }
    }

    /* UNITY SECTION */
    public override void OnPhotonInstantiate(PhotonMessageInfo info)
    {
        Dispatcher.Subscribe(EventId.SecondaryWeaponUsed, OnSecondaryWeaponUsed);

        base.OnPhotonInstantiate(info);

        SetRocketGuidanceAudio();

        sqrMaxAimDistance = MaxAimDistance * MaxAimDistance;
        minAimAngleCos = Mathf.Cos(Mathf.Deg2Rad * MaxShootAngle);

        effectsController = GetComponent<VehicleEffectsControllerBOW>();
    }

    protected override void OnDestroy()
    {
        Dispatcher.Unsubscribe(EventId.SecondaryWeaponUsed, OnSecondaryWeaponUsed);
        base.OnDestroy();
    }

    protected override void Update()
    {
        base.Update();

        if (!IsAvailable)
            return;

        if (PhotonView.isMine)
        {
            MoveStaticGunsight();

            requiredSpeed = Mathf.Lerp(minSpeed, MaxSpeed, ThrottleLevel.Value);

            accelerationDirection
                = HelpTools.Approximately(currentSpeed, requiredSpeed)
                    ? 0
                    : Mathf.Sign(requiredSpeed - currentSpeed);

            currentSpeed = Mathf.MoveTowards(currentSpeed, requiredSpeed, acceleration * Time.deltaTime);
        }
        else
        {
            float correctSpeed = correctVelocity.magnitude;

            if (!Mathf.Approximately(currentSpeed, correctSpeed))
            {
                currentSpeed = Mathf.MoveTowards(currentSpeed, correctVelocity.magnitude, acceleration * Time.deltaTime);
                accelerationDirection = 1f;
            }
            else
                accelerationDirection = 0f;

            if (burst)
                PrimaryFire();
        }

        if (!HelpTools.Approximately(accelerationDirection, 0))
            MarkActivity();

        if (!IsAvailable)
            return;

        if (IsMain)
            effectsController.UpdateEffects(accelerationDirection);

        if (PhotonView.isMine && IsRequireSecondaryFire)
            UseSecondaryWeapon(GunShellInfo.ShellType.Missile_SACLOS);

        if (burst)
            PrimaryFire();
    }

    protected override void FixedUpdate()
    {
        MovePlayer();
    }

    void OnCollisionEnter(Collision collision)
    {
        CollisionReaction(collision);
    }

    void OnCollisionStay(Collision collision)
    {
        CollisionReaction(collision);
    }

#if UNITY_EDITOR
    protected override void OnDrawGizmos()
    {
        base.OnDrawGizmos();

        if (BotDispatcher.Instance == null || !BotDispatcher.DrawBotPaths || BotAI == null)
            return;

        Gizmos.color = IsMainsFriend ? Color.green : Color.red;
        Gizmos.DrawSphere(transform.position, 7.0f);

        Gizmos.color = Color.cyan;
        Gizmos.DrawSphere(BotAI.PositionToMove, 12.5f);

        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(transform.position, BotAI.PositionToMove);
    }
#endif

    /* PUBLIC SECTION */

    public override void MovePlayer()
    {
        animationController.Receive();

        if (!PhotonView.isMine || !IsAvailable)
            return;

        var horizontalAccel = XAxisControl;
        var verticalAccel = YAxisControl;

        if (!Mathf.Approximately(YAxisControl, 0) || !Mathf.Approximately(XAxisControl, 0))
            MarkActivity();

        rb.velocity = currentSpeed * shipTransform.forward;

        rb.AddForce(transform.forward * acceleration, ForceMode.Acceleration);

        if (Vector3.Dot(rb.velocity, shipTransform.forward) < 0)
            rb.velocity = Vector3.zero;

        Vector3 localAV = transform.InverseTransformDirection(rb.angularVelocity);

        var horTorque = horizontalAccel * horizontalTorqueAcceleration;

        if (!HelpTools.Approximately(horTorque, 0))
            rb.AddRelativeTorque(0, horizontalAccel, 0, ForceMode.Acceleration);
        else if (!HelpTools.Approximately(-localAV.y, 0))
            rb.AddRelativeTorque(0, -localAV.y / torqueBrake, 0, ForceMode.Acceleration);

        if (!HelpTools.Approximately(verticalAccel, 0))
            rb.AddRelativeTorque(verticalAccel * verticalTorqueAcceleration, 0, 0, ForceMode.Acceleration);
        else
            rb.AddRelativeTorque(-localAV.x / torqueBrake, 0, 0, ForceMode.Acceleration);

        Vector3 localEuler = shipTransform.localEulerAngles;

        localEuler.z = Mathf.LerpAngle(localEuler.z, -horizontalAccel * maxInclineAngle, inclineSmooth);

        shipTransform.localEulerAngles = localEuler;

        SetEngineNoise(Mathf.Abs(currentSpeed / MaxSpeed));

        angleToHorizon
            = Vector3.Angle(
                from:   transform.forward.GetHorizontalIdentity(),
                to:     transform.forward);

        if (horizontalAccel < StabilizationInputThreshold &&
            verticalAccel < StabilizationInputThreshold &&
            angleToHorizon < Mathf.Abs(minUnstabilizedAngle))
        {
            if (!stabilization)
            {
                stabilization = true;

                stabilizationRoutine = Stabilization();

                StartCoroutine(stabilizationRoutine);
            }
        }
        else
        {
            if (stabilization)
            {
                stabilization = false;
                StopCoroutine(stabilizationRoutine);
            }
        }

        StoreVehiclePosition();
    }

    [PunRPC]
    public override void Respawn(Vector3 position, Quaternion rotation, bool restoreLife, bool firstTime)
    {
        base.Respawn(position, rotation, restoreLife, firstTime);
    }

    public override bool PrimaryFire()
    {
        MarkActivity();

        if (!weapons[DefaultShellType].IsReady)
            return false;

        Shell shell = ShellPoolManager.GetShell(primaryShellInfo.shellPrefabName);

        weapons[DefaultShellType].RegisterShot();

        if (shootAnimation)
            shootAnimation.Play();

        Transform currentShotEffectPoint = shootEffectPoints[currentShotPointIndex];

        currentShotPointIndex = (int)Mathf.Repeat(++currentShotPointIndex, shootEffectPoints.Count);

        Vector3 shotDirection
            = PhotonView.isMine
                ? GetLocalShotDirection(currentShotEffectPoint, shell)
                : GetCloneShotDirection(currentShotEffectPoint, shell);

        Quaternion shotRotation = Quaternion.LookRotation(shotDirection);

        EffectPoolDispatcher.GetFromPool(
            _effect:        shotPrefab,
            _position:      currentShotEffectPoint.position,
            _rotation:      currentShotEffectPoint.rotation,
            useEffectMover: true,
            moverTarget:    currentShotEffectPoint);

        continuousFire = primaryShellInfo.continuousFire;

        shell.OwnerSpeed = Mathf.Abs(currentSpeed);
        shell.SetPositionAndRotation(currentShotEffectPoint.position, shotRotation);
        shell.Activate(this, data.attack, hitMask);

        AudioDispatcher.PlayClipAtPosition(shell.ShotSound, currentShotEffectPoint.position);

        return true;
    }

    public override void SecondaryFire(GunShellInfo.ShellType shellType, int targetId, Vector3 aimPointLocalToTarget)
    {
        MarkActivity();

        if (IsMain)
            BattleGUI.FireButtons[shellType].SimulateReloading();

        weapons[shellType].RegisterShot();

        if (shootAnimation)
            shootAnimation.Play();

        Transform currentRocketLaunchPoint = rocketLaunchPoints[currentRocketLaunchPointIndex];

        currentRocketLaunchPointIndex = (int)Mathf.Repeat(++currentRocketLaunchPointIndex, rocketLaunchPoints.Count);

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

        Shell shell
            = ShellPoolManager.GetShell(
                shellName:  shellInfo.shellPrefabName,
                position:   currentRocketLaunchPoint.position,
                rotation:   rotation);

        continuousFire = shellInfo.continuousFire;

        shell.OwnerSpeed = Mathf.Abs(currentSpeed);
        shell.Activate(this, data.rocketAttack, hitMask, targetId, shellType);
        shell.SetAimPoint(aimPointLocalToTarget);

        AudioDispatcher.PlayClipAtPosition(shell.ShotSound, currentRocketLaunchPoint.position);
    }

    public override void Aiming()
    {
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

    public override void UpdateBotPrefabs(VehicleController nativeController) { }

    public void UseSecondaryWeapon(GunShellInfo.ShellType shellType)
    {
        Dispatcher.Send(
            id:     EventId.SecondaryWeaponUsed,
            info:   new EventInfo_IIIV(
                        data.playerId,
                        (int)shellType,
                        Target.data.playerId,
                        AimPointLocalToTarget),
            target: Dispatcher.EventTargetType.ToAll);
    }

    protected override void OnTargetAimed(EventId id, EventInfo ei)
    {
        if (!PhotonView.isMine)
            return;

        EventInfo_IIB info = (EventInfo_IIB)ei;

        if (info.int1 != data.playerId)
            return;
        
        RestartRocketGuidance();
    }

    protected override void ApplyAvailability()
    {
        base.ApplyAvailability();

        if (!isAvailable)
            RestartRocketGuidance();
    }

    protected override IEnumerator RotateToMapCenter()
    {
        while (Vector3.Angle(transform.forward, worldMapCenterDirection) > 25)
        {
            worldMapCenterDirection = Map.MapCenterPos - transform.position;

            rb.rotation
                = Quaternion.RotateTowards(
                    from:               rb.rotation,
                    to:                 Quaternion.LookRotation(worldMapCenterDirection, Vector3.up),
                    maxDegreesDelta:    stabilizationSpeed * Time.deltaTime);

            yield return null;
        }

        outOfMapRotated = true;
    }

    private void OnSecondaryWeaponUsed(EventId id, EventInfo ei)
    {
        EventInfo_IIIV info = (EventInfo_IIIV)ei;

        int playerId = info.int1;
        int targetId = info.int3;
        GunShellInfo.ShellType shellType = (GunShellInfo.ShellType)info.int2;
        Vector3 aimPointLocalToTarget = info.vector;

        if (playerId != data.playerId)
            return;

        RestartRocketGuidance();

        SecondaryFire(shellType, targetId, aimPointLocalToTarget);
    }

    private IEnumerator Stabilization()
    {
        yield return new WaitForSeconds(stabilizationDelay);

        while (stabilization)
        {
            rb.rotation
                = Quaternion.RotateTowards(
                    from:               rb.rotation,
                    to:                 Quaternion.LookRotation(transform.forward, Vector3.up),
                    maxDegreesDelta:    stabilizationSpeed * Time.deltaTime);

            yield return null;
        }
    }

    private IEnumerator MissileGuidance()
    {
        while (MissileAimProgress < 1)
        {
            MissileAimProgress += Time.deltaTime / MissileAimingDuration;

            MissileAimProgress = Mathf.Clamp01(MissileAimProgress);

            if (!IsBot)
                BattleGUI.Instance.iGunSight.TargetLockedProgressBar.Percentage = MissileAimProgress;

            yield return null;
        }

        Dispatcher.Send(EventId.SACLOSAimed, new EventInfo_IB(data.playerId, true));

        if (IsMain && !IsBot)
            rocketGuidanceAudio.Play();
    }

    private void RestartRocketGuidance()
    {
        if (!PhotonView.isMine || BattleGUI.Instance == null)
            return;

        MissileAimProgress = 0;

        if (!IsBot)
        {
            BattleGUI.Instance.iGunSight.TargetLockedProgressBar.Percentage = 0;
        }

        if (missileGuidanceRoutine != null)
        {
            StopCoroutine(missileGuidanceRoutine);
            missileGuidanceRoutine = null;
        }

        if (rocketGuidanceAudio != null && rocketGuidanceAudio.isPlaying)
            rocketGuidanceAudio.Stop();

        Dispatcher.Send(EventId.SACLOSAimed, new EventInfo_IB(data.playerId, false));

        if (!isAvailable || !TargetAimed)
            return;

        missileGuidanceRoutine = MissileGuidance();
        StartCoroutine(missileGuidanceRoutine);
    }

    private void SetRocketGuidanceAudio()
    {
        if (!IsMain)
            return;

        rocketGuidanceAudio = AudioDispatcher.CreateAudioSource(this.gameObject);

        rocketGuidanceAudio.clip = rocketGuidanceSound;
        rocketGuidanceAudio.loop = true;
        rocketGuidanceAudio.rolloffMode = AudioRolloffMode.Linear;
        rocketGuidanceAudio.volume = Settings.SoundVolume;
        rocketGuidanceAudio.spatialBlend = PhotonView.isMine ? 0 : 1;
        rocketGuidanceAudio.maxDistance = DEFAULT_SOUND_DISTANCE;
        rocketGuidanceAudio.dopplerLevel = 0;
    }

    private void CollisionReaction(Collision collision)
    {
        Vector3 colNormal = collision.contacts[0].normal;
        rb.AddForceAtPosition(colNormal * 200, collision.contacts[0].point, ForceMode.VelocityChange);
    }

    private bool TargetCanBeCaptured(VehicleController vehicle, ref float checkedSqrTargetDistance)
    {
        if (vehicle == null || vehicle == this || !vehicle.IsAvailable || AreFriends(this, vehicle))
            return false;

        Vector3 vehDeltaPos = vehicle.transform.position - transform.position;
        float sqrTargetDistance = Vector3.SqrMagnitude(vehDeltaPos);
        float targetDistance = Mathf.Sqrt(sqrTargetDistance);

        if (targetDistance > MaxAimDistance ||
            sqrTargetDistance > checkedSqrTargetDistance ||
            Vector3.Dot(ShotPoint.forward, vehDeltaPos.normalized) < minAimAngleCos)
        {
            return false;
        }

        Debug.DrawRay(transform.position, vehDeltaPos.normalized * targetDistance, Color.yellow);

        RaycastHit hit;

        if (Physics.Raycast(new Ray(transform.position, vehDeltaPos.normalized), out hit, targetDistance, HitMask))
        {
            VehicleController vehicleObstacle;

            if (!TryGetHitTarget(hit, out vehicleObstacle) || vehicleObstacle != vehicle)
            {
                Debug.DrawRay(transform.position, vehDeltaPos.normalized * targetDistance, Color.red);
                return false;
            }
        }

        checkedSqrTargetDistance = sqrTargetDistance;

        return true;
    }

    private Vector3 GetLocalShotDirection(Transform shotPoint, Shell shell)
    {
        if (TargetAimed)
            return CalculateShotDirection(TargetPosition, Target.Velocity, shotPoint.position, shell.speed);

        return shotPoint.forward;
    }

    private Vector3 GetCloneShotDirection(Transform shotPoint, Shell shell)
    {
        VehicleController possibleTarget = GetPossibleTarget();

        if (possibleTarget == null)
            return shotPoint.forward;

        return CalculateShotDirection(possibleTarget.transform.position, possibleTarget.Velocity, shotPoint.position, shell.speed);
    }

    private VehicleController GetPossibleTarget()
    {
        VehicleController result = null;

        float sqrTargetDistance = sqrMaxAimDistance;

        foreach (var vehicle in BattleController.allVehicles.Values)
        {
            if (TargetCanBeCaptured(vehicle, ref sqrTargetDistance))
                result = vehicle;
        }

        return result;
    }

    /// <summary>
    /// http://answers.unity3d.com/answers/299922/view.html
    /// </summary>
    private Vector3 CalculateShotDirection(Vector3 targetPosition, Vector3 targetVelocity, Vector3 shotPosition, float shellSpeed)
    {
        const float threshold = 0.0001f;
        const float minD = 0.1f;

        Vector3 targetDirection = targetPosition - shotPosition;
        float shellSpeedSqr = shellSpeed * shellSpeed;
        float targetSpeedSqr = targetVelocity.sqrMagnitude;
        float dot = Vector3.Dot(targetDirection, targetVelocity);
        float targetDistanceSqr = targetDirection.sqrMagnitude;
        float d = (dot * dot) - targetDistanceSqr * (targetSpeedSqr - shellSpeedSqr);

        if (d < minD)
        {
            Debug.LogError("Shell isn't fast enough!");
            return Vector3.zero;
        }

        float sqrt = Mathf.Sqrt(d);

        float sFirst = (-dot - sqrt) / targetDistanceSqr;
        float sSecond = (-dot + sqrt) / targetDistanceSqr;

        if (sFirst < threshold)
        {
            if (sSecond < threshold)
            {
                Debug.LogError("Shell can't reach the target!");
                return Vector3.zero;
            }

            return (sSecond) * targetDirection + targetVelocity;
        }

        if (sSecond < threshold)
            return (sFirst) * targetDirection + targetVelocity;

        if (sFirst < sSecond)
            return (sSecond) * targetDirection + targetVelocity;

        return (sFirst) * targetDirection + targetVelocity;
    }
}
