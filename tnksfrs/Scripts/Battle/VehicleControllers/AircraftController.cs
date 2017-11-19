using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using CodeStage.AntiCheat.ObscuredTypes;
using XD;

public class AircraftController : FlightController
{
    [Header("Настройки для самолёта")]

    [Header("Ссылки")]
    public List<Transform> rocketLaunchPoints = null;
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

    [Header("Настройки эффекта двигателя")]
    public ParticleSystem[] primaryEffects = null;
    public ParticleSystem[] trailEffects = null;

    [Header("Визуальные эффекты")]
    public GameObject speedEffect;
    public GameObject cloudEffect;

    [Header("Повреждения эффекты")]
    public GameObject fireEffect;
    public GameObject smokeEffect;

    [Header("Звуки")]
    public AudioClip rocketGuidanceSound;

    private const float SPEED_EFFECT_ALPHA = 27.0f / 255.0f;
    private const float FIRE_START_HEALTH_RATIO_THRESHOLD = 0.5f;
    private const float SMOKE_START_HEALTH_RATIO_THRESHOLD = 0.4f;

    private static readonly ObscuredFloat SPEED_RATIO = 2.0f;
    private static readonly ObscuredFloat ODOMETER_RATIO = 0.04f;
    private static readonly ObscuredFloat CORRECTION_TIME = 0.5f;
    private static readonly ObscuredFloat MAX_SHOOT_ANGLE = 20.0f;
    private static readonly ObscuredFloat AIM_DISTANCE_FACTOR = 1.0f;
    private static readonly Color FIRE_START_COLOR = Color.black;
    private static readonly Color FIRE_FINAL_COLOR = new Color(1.0f, 0.47f, 0);
    private static readonly Color SMOKE_START_COLOR = new Color(0.35f, 0.35f, 0.35f);
    private static readonly Color SMOKE_FINAL_COLOR = Color.black;

    private readonly IEnumerator shakingDirections
        = new Vector3[]
            { Vector3.left, Vector3.right, Vector3.up, Vector3.down }
                .GetEnumerator();

    private bool stabilization;
    private int qualityLevel;
    private int currentRocketLaunchPointIndex;
    private int currentTargetId;
    private float angleToHorizon;
    private new Transform shotPoint;
    private AudioSource rocketGuidanceAudio;
    private ParticleSystem[] speedEffects;
    private ParticleSystem[] glows;
    private ParticleSystem[] trails;
    private IEnumerator stabilizationRoutine;
    private IEnumerator missileGuidanceRoutine;
    public bool IsAimedSACLOSMissile
    {
        get 
        { 
            return HelpTools.Approximately(MissileAimProgress, 1); 
        }
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
                : XDevs.Input.GetButtonDown("Fire Primary") || XDevs.Input.GetButtonDown("Launch IRCM")
                ;
        }
    }

    public override bool IsRequirePrimaryFire
    {
        get
        {
            return TargetAimed && !StaticContainer.UI.IsWindowOnScreen &&
                   (transform.position - AimPoint.point).sqrMagnitude < 100; //MaxAimDistance * MaxAimDistance;
        }
    }

    protected override bool IsRequireSecondaryFire
    {
        get
        {
            return IsAimedSACLOSMissile && FireButtonPressed && !StaticContainer.UI.IsWindowOnScreen;
        }
    }

    protected override float OdometerRatio
    {
        get 
        { 
            return ODOMETER_RATIO; 
        }
    }

    protected override float SpeedRatio
    {
        get 
        { 
            return SPEED_RATIO; 
        }
    }
    

    protected override Vector3 IndicatorDeltaOffset
    {
        get 
        { 
            return Vector3.up * 5.0f; 
        }
    }

    /* UNITY SECTION */
    protected override void OnPhotonInstantiate(PhotonMessageInfo info)
    {
        Dispatcher.Subscribe(EventId.SecondaryWeaponUsed, OnSecondaryWeaponUsed);
        Dispatcher.Subscribe(EventId.TankHealthChanged, OnAircraftHealthChanged);

        base.OnPhotonInstantiate(info);

        if (PhotonView.isMine)
        {
            qualityLevel = QualitySettings.GetQualityLevel();
        }
        else
        {
            qualityLevel = 1; // Жесточайший хардкод. Убираем тяжелые эффекты для клонов. 
        }

        SetRocketGuidanceAudio();

        CollectEffects();
        SetDamageEffects();

        DrawEngineJets();
        CheckSpeedEffect(1);
    }

    protected override void OnDestroy()
    {
        Dispatcher.Unsubscribe(EventId.SecondaryWeaponUsed, OnSecondaryWeaponUsed);
        Dispatcher.Unsubscribe(EventId.TankHealthChanged, OnAircraftHealthChanged);

        base.OnDestroy();
    }

    protected override void NormalUpdate()
    {
        base.NormalUpdate();

        if (!IsAvailable)
        {
            return;
        }

        if (PhotonView.isMine)
        {
            MoveStaticGunsight();

            requiredSpeed = Mathf.Lerp(minSpeed, Settings[Setting.MovingSpeed].Max, ThrottleLevel.Value);

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
            {
                accelerationDirection = 0f;
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
        {
            return;
        }

        if (PhotonView.isMine)
        {
            CheckSpeedEffect(accelerationDirection);
        }

        if (!HelpTools.Approximately(accelerationDirection, 0))
        {
            DrawEngineJets();
        }

        if (PhotonView.isMine && IsRequireSecondaryFire)
        {
            UseSecondaryWeapon(GunShellInfo.ShellType.Missile_SACLOS);
        }

        if (burst)
        {
            PrimaryFire(shotPoint.rotation);
        }
    }

    protected override void PhysicsUpdate()
    {
        MovePlayer();
    }

    protected override void OnCollisionEnter(Collision collision)
    {
        CollisionReaction(collision);
    }

    void OnCollisionStay(Collision collision)
    {
        CollisionReaction(collision);
    }

    /* PUBLIC SECTION */

    public override void MovePlayer()
    {
        animationController.Receive();

        if (!PhotonView.isMine)
            return;

        var horizontalAccel = XAxisControl;
        var verticalAccel = YAxisControl;

        if (!Mathf.Approximately(YAxisControl, 0) || !Mathf.Approximately(XAxisControl, 0))
        {
            MarkActivity();
        }

        rigidbody.velocity = currentSpeed * shipTransform.forward;

        rigidbody.AddForce(transform.forward * acceleration, ForceMode.Acceleration);

        if (!shakingDirections.MoveNext())
        {
            shakingDirections.Reset();
            shakingDirections.MoveNext();
        }

        if (shakingDirections.Current != null)
        {
            rigidbody.AddForce(
                force: (Vector3)shakingDirections.Current * shakingRatio * currentSpeed * shakingRatio,
                mode: ForceMode.Acceleration);
        }

        if (Vector3.Dot(rigidbody.velocity, shipTransform.forward) < 0)
        {
            rigidbody.velocity = Vector3.zero;
        }

        Vector3 localAV = transform.InverseTransformDirection(rigidbody.angularVelocity);

        var horTorque = horizontalAccel * horizontalTorqueAcceleration;

        if (!HelpTools.Approximately(horTorque, 0))
        {
            rigidbody.AddRelativeTorque(0, horizontalAccel, 0, ForceMode.Acceleration);
        }
        else if (!HelpTools.Approximately(-localAV.y, 0))
        {
            rigidbody.AddRelativeTorque(0, -localAV.y / torqueBrake, 0, ForceMode.Acceleration);
        }

        if (!HelpTools.Approximately(verticalAccel, 0))
        {
            rigidbody.AddRelativeTorque(verticalAccel * verticalTorqueAcceleration, 0, 0, ForceMode.Acceleration);
        }
        else
        {
            rigidbody.AddRelativeTorque(-localAV.x / torqueBrake, 0, 0, ForceMode.Acceleration);
        }

        Vector3 localEuler = shipTransform.localEulerAngles;

        localEuler.z = Mathf.LerpAngle(localEuler.z, -horizontalAccel * maxInclineAngle, inclineSmooth);

        shipTransform.localEulerAngles = localEuler;

        SetEngineNoise(Mathf.Abs(currentSpeed / Settings[Setting.MovingSpeed].Max));

        angleToHorizon
            = Vector3.Angle(
                from: transform.forward.GetHorizontalIdentity(),
                to: transform.forward);

        if (HelpTools.Approximately(horizontalAccel, 0) &&
            HelpTools.Approximately(verticalAccel, 0) &&
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
        currentTargetId = BattleController.DEFAULT_TARGET_ID;
    }

    public override bool PrimaryFire(Quaternion rotation)
    {
        return true;
    }

    /* PRIVATE SECTION */
    protected override void OnTankRespawned(EventId id, EventInfo ei)
    {
        base.OnTankRespawned(id, ei);

        EventInfo_I info = (EventInfo_I)ei;

        if (info.int1 != PhotonView.ownerId)
            return;

        CheckSpeedEffect(1);
        DrawEngineJets();
    }

    protected override void ApplyAvailability()
    {
        base.ApplyAvailability();

        if (!isAvailable)
        {
            RestartRocketGuidance();
        }
    }

    public override void UpdateBotAssets(VehicleController nativeController)
    {
    }

    protected override void OnTargetAimed(EventId id, EventInfo ei)
    {
        EventInfo_IIB info = (EventInfo_IIB)ei;

        TargetAimed = info.bool1;
        currentTargetId = info.int2;

        if (!PhotonView.isMine)
        {
            return;
        }

        RestartRocketGuidance();
    }

    protected override IEnumerator RotateToMapCenter()
    {
        while (Vector3.Angle(transform.forward, worldMapCenterDirection) > 25)
        {
            worldMapCenterDirection = mapCenterPos - transform.position;

            rigidbody.rotation
                = Quaternion.RotateTowards(
                    from:               rigidbody.rotation,
                    to:                 Quaternion.LookRotation(worldMapCenterDirection, Vector3.up),
                    maxDegreesDelta:    stabilizationSpeed * Time.deltaTime);

            yield return null;
        }

        outOfMapRotated = true;
    }

    private void OnSecondaryWeaponUsed(EventId id, EventInfo ei)
    {
        EventInfo_III info = (EventInfo_III)ei;

        if (info.int1 != PhotonView.ownerId)
        {
            return;
        }

        RestartRocketGuidance();
        SecondaryFire(shellType: (GunShellInfo.ShellType)info.int2, targetId: info.int3);
    }

    private void OnAircraftHealthChanged(EventId id, EventInfo ei)
    {
        SetDamageEffects();
    }

    private IEnumerator Stabilization()
    {
        yield return new WaitForSeconds(stabilizationDelay);

        while (stabilization)
        {
            rigidbody.rotation
                = Quaternion.RotateTowards(
                    from:               rigidbody.rotation,
                    to:                 Quaternion.LookRotation(transform.forward, Vector3.up),
                    maxDegreesDelta:    stabilizationSpeed * Time.deltaTime);

            yield return null;
        }
    }

    private IEnumerator MissileGuidance()
    {
        while (MissileAimProgress < 1)
        {
            MissileAimProgress += Time.deltaTime / StaticContainer.GameManager.MissileAimingDuration;

            MissileAimProgress = Mathf.Clamp01(MissileAimProgress);

           // BattleGUI.Instance.TargetLockedBar.Percentage = MissileAimProgress;

            yield return null;
        }

        Dispatcher.Send(EventId.SACLOSAimed, new EventInfo_B(true));

        rocketGuidanceAudio.Play();
    }

    private void RestartRocketGuidance()
    {
        if (!PhotonView.isMine)
        {
            return;
        }

      //  BattleGUI.Instance.TargetLockedBar.Percentage = MissileAimProgress = 0;

        if (missileGuidanceRoutine != null)
        {
            StopCoroutine(missileGuidanceRoutine);
        }

        if (rocketGuidanceAudio != null && rocketGuidanceAudio.isPlaying)
        {
            rocketGuidanceAudio.Stop();
        }

        Dispatcher.Send(EventId.SACLOSAimed, new EventInfo_B(false));

        if (!isAvailable || !TargetAimed)
        {
            return;
        }

        missileGuidanceRoutine = MissileGuidance();
        StartCoroutine(missileGuidanceRoutine);
    }

    private void UseSecondaryWeapon(GunShellInfo.ShellType shellType)
    {
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
        {
            return;
        }

        rocketGuidanceAudio = gameObject.AddComponent<AudioSource>();

        rocketGuidanceAudio.clip = rocketGuidanceSound;
        rocketGuidanceAudio.loop = true;
        rocketGuidanceAudio.rolloffMode = AudioRolloffMode.Linear;
        rocketGuidanceAudio.volume = global::Settings.SoundVolume;
        rocketGuidanceAudio.spatialBlend = PhotonView.isMine ? 0 : 1;
        rocketGuidanceAudio.maxDistance = DEFAULT_SOUND_DISTANCE;
        rocketGuidanceAudio.dopplerLevel = 0;
    }

    private void CheckSpeedEffect(float accelerationValue)
    {
        if (!HelpTools.Approximately(accelerationValue, 0))
        {
            speedEffects.SetEffectsAlpha(alpha: Mathf.Lerp(0, SPEED_EFFECT_ALPHA, (currentSpeed - minSpeed) / (Settings[Setting.MovingSpeed].Max - minSpeed)));
        }
    }

    private void DrawEngineJets()
    {
        if (qualityLevel > 1)
        {
            drawEngineEff.DrawJet(
                particleSystems:    glows,
                minSpeed:           0,
                maxSpeed:           Settings[Setting.MovingSpeed].Max,
                minAlpha:           0,
                maxAlpha:           0.4f,
                disableForLowSpeed: false,
                speed:              currentSpeed);

            if (qualityLevel > 2)
            {
                drawEngineEff.DrawJet( particleSystems:    trailEffects,
                    minSpeed:           0.2f * Settings[Setting.MovingSpeed].Max,
                    maxSpeed:           Settings[Setting.MovingSpeed].Max,
                    minAlpha:           0.03f,
                    maxAlpha:           0.95f,
                    disableForLowSpeed: true,
                    speed:              currentSpeed);
                /*DrawEngineJet*/
            }
        }
    }

    private void CollisionReaction(Collision collision)
    {
        Vector3 colNormal = collision.contacts[0].normal;
        rigidbody.AddForceAtPosition(colNormal * 200, collision.contacts[0].point, ForceMode.VelocityChange);
    }

    private void CollectEffects()
    {
        this.ChangePlayEngineEffBehaviour(new XD.DrawEngineJet(gameObject));
        //drawEngineEff.DrawEngineEffect();
        if (speedEffect)
        {
            speedEffects = speedEffect.GetComponentsInChildren<ParticleSystem>(true);
            speedEffect.gameObject.SetActive(PhotonView.isMine);
        }

        if (cloudEffect)
        {
            cloudEffect.GetComponent<ParticleSystem>().startColor = MapParticles.Instance.CloudsColor;
            cloudEffect.gameObject.SetActive(PhotonView.isMine && qualityLevel > 1);
        }

        Transform effectRoot = transform.Find("Mesh/Effects");

        glows = drawEngineEff.FindEffects(effectRoot, "Glows");
        trails = drawEngineEff.FindEffects(effectRoot, "Trails");

        if (glows != null)
        {
            MiscTools.SetObjectsActivityByComponents(glows, qualityLevel > 1);
        }

        if (trails != null)
        {
            MiscTools.SetObjectsActivityByComponents(trails, qualityLevel > 2);
        }
    }

    private void SetDamageEffects()
    {
        float healthRatio = HPSystem.Armor / (float)MaxArmor;

        if (fireEffect)
        {
            fireEffect.GetComponent<ParticleSystem>().startColor
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
            smokeEffect.GetComponent<ParticleSystem>().startColor
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
}
