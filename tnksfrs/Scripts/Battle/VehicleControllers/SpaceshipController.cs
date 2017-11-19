using UnityEngine;
using System.Collections;
using CodeStage.AntiCheat.ObscuredTypes;
using XD;

public class SpaceshipController : FlightController
{
    [Header("Настройки для космолёта")]

    [Header("Управление")]
    public ObscuredFloat maxInclineAngle = 70;
    public ObscuredFloat inclineSmooth = 0.03f;
    public ObscuredFloat torqueAcceleration = 2.0f;
    public ObscuredFloat torqueBrake = 0.1f;

    [Header("Настройки эффекта двигателя")]
    public ParticleSystem[] primaryEffects;
    public ParticleSystem[] trailEffects;

    [Header("Визуальные эффекты")]
    public GameObject speedEffect;
    public LensFlare spawnLens;
    public GameObject spawnEffect;

    private const float FIXED_ENGINE_ALPHA = 50f / 255;
    private const float SPAWN_EFFECT_ALPHA = 0.7f;
    private const float SPEED_EFFECT_ALPHA = 27f / 255;
    private const float SPAWN_LENS_BRIGHTNESS = 0.5f;
    private const float SPAWN_EFFECT_FADE_OUT_TIME = 0.2f;
    private const float SPAWN_EFFECT_TIME = 1.2f;
    private const float MIN_OBSTACLE_DISTANCE = 60.0f;
    private const float MAX_OBSTACLE_DISTANCE = 185.0f;
    private const float OBSTACLE_STABILIZATION_QUALIFIER = 1.55f;
    private const float MAX_OBSTACLE_STABILIZATION_DURATION = 1.0f;
    private const float MAX_OBSTACLE_STABILIZATION_DELAY = 2.0f;
    private const float COLLISION_RESOLVE_TIME = 0.33f;
    private const float COLLISION_RESOLVE_ACCELERATION = 0.2f;

    private static readonly ObscuredFloat SPEED_RATIO = 2;
    private static readonly ObscuredFloat ODOMETER_RATIO = 0.04f;
	private static readonly ObscuredFloat MAX_SHOOT_ANGLE = 20;
	private static readonly ObscuredFloat CORRECTION_TIME = 0.5f;

    private bool isStabilizing;
    private bool isGettingAroundObstacle;
    private bool isCollidedRecently;
    private int qualityLevel;
    private float length;
    private float lastObstacleStabilizationTime;
    private float lastCollisionTime;
    private Transform cannonEnd;
    private ParticleSystem[] speedEffects;
    private ParticleSystem[] spawnEffects;
    private ParticleSystem[] engines;
    private ParticleSystem[] glows;
    private ParticleSystem[] trails;
    private IEnumerator spawnEffectRoutine;
    private IEnumerator gettingAroundObstacleRoutine;
    
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
	
	protected override float CorrectionTime
	{
		get 
        { 
            return CORRECTION_TIME; 
        }
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

    /* UNITY SECTION */

    protected override void OnPhotonInstantiate(PhotonMessageInfo info)
	{
		base.OnPhotonInstantiate(info);

        length = shipTransform.GetComponent<Renderer>().bounds.extents.z;

        if (PhotonView.isMine)
            qualityLevel = QualitySettings.GetQualityLevel();
        else
            qualityLevel = 1; // Жесточайший хардкод. Убираем тяжелые эффекты для клонов.
        
        CollectEffects();

        DrawEngineJets();
        CheckSpeedEffect(1);
	}

    protected override void NormalUpdate()
    {
        base.NormalUpdate();

        if (!IsAvailable)
            return;

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
    }

    protected override void PhysicsUpdate()
    {
        MovePlayer();
    }

    void OnCollisionStay(Collision collision)
    {
        lastCollisionTime = Time.time;
    }

    /* PUBLIC SECTION */

    public override void MovePlayer()
    {
        if (!PhotonView.isMine || !IsAvailable)
        {
            return;
        }

        var horizontalAccel = XAxisControl;
        var verticalAccel = YAxisControl;

        if (!Mathf.Approximately(verticalAccel, 0) || !Mathf.Approximately(horizontalAccel, 0))
        {
            MarkActivity();
        }

        GetAroundObstacle();

        rigidbody.velocity = shipTransform.forward * currentSpeed;
        rigidbody.AddForce(transform.forward * acceleration, ForceMode.Acceleration);

        isCollidedRecently = (Time.time - lastCollisionTime) < COLLISION_RESOLVE_TIME;

        if (isCollidedRecently && !HelpTools.Approximately(verticalAccel, 0))
        {
            rigidbody.velocity += -shipTransform.up * currentSpeed * COLLISION_RESOLVE_ACCELERATION * Mathf.Sign(verticalAccel);
        }
        if (Vector3.Dot(rigidbody.velocity, shipTransform.forward) < 0)
        {
            rigidbody.velocity = Vector3.zero;
        }

        Vector3 localAV = transform.InverseTransformDirection(rigidbody.angularVelocity);

        if (!HelpTools.Approximately(horizontalAccel, 0))
        {
            rigidbody.AddRelativeTorque(0, horizontalAccel * torqueAcceleration, 0, ForceMode.Acceleration);
        }
        else
        {
            rigidbody.AddRelativeTorque(0, -localAV.y / torqueBrake, 0, ForceMode.Acceleration);
        }

        if (!HelpTools.Approximately(verticalAccel, 0))
        {
            rigidbody.AddRelativeTorque(verticalAccel * torqueAcceleration, 0, 0, ForceMode.Acceleration);
        }
        else
        {
            rigidbody.AddRelativeTorque(-localAV.x / torqueBrake, 0, 0, ForceMode.Acceleration);
        }

        Vector3 localEuler = shipTransform.localEulerAngles;

        localEuler.z = Mathf.LerpAngle(localEuler.z, -horizontalAccel * maxInclineAngle, inclineSmooth);

        shipTransform.localEulerAngles = localEuler;

        SetEngineNoise(Mathf.Abs(currentSpeed / Settings[Setting.MovingSpeed].Max));

        StoreVehiclePosition();
    }

    [PunRPC]
	public override void Respawn(Vector3 position, Quaternion rotation, bool restoreLife, bool firstTime)
	{
		base.Respawn(position, rotation, restoreLife, firstTime);
	}

    public override void UpdateBotAssets(VehicleController nativeController)
    {
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

        if (spawnEffectRoutine != null)
        {
            StopCoroutine(spawnEffectRoutine);
        }

        spawnEffectRoutine = SpawnEffect();
        StartCoroutine(spawnEffectRoutine);
    }

    private void GetAroundObstacle()
    {
        bool isStabilizedRecently = (Time.time - lastObstacleStabilizationTime) < MAX_OBSTACLE_STABILIZATION_DELAY;

        if (isGettingAroundObstacle || isStabilizedRecently)
        {
            return;
        }

        RaycastHit hit;

        if (Physics.Raycast(
            /* origin:      */  shipTransform.position,
            /* direction:   */  shipTransform.forward,
            /* hitInfo:     */  out hit,
            /* maxDistance: */  length + MIN_OBSTACLE_DISTANCE,
            /* layerMask:   */  HitMask))
        {
            if (gettingAroundObstacleRoutine != null)
                StopCoroutine(gettingAroundObstacleRoutine);

            gettingAroundObstacleRoutine = GettingAroundObstacle(hit);

            StartCoroutine(gettingAroundObstacleRoutine);
        }
    }

    private void CheckSpeedEffect(float accelerationValue)
    {
        if (!HelpTools.Approximately(accelerationValue, 0))
        {
            speedEffects.SetEffectsAlpha(Mathf.Lerp(0, SPEED_EFFECT_ALPHA, (currentSpeed - minSpeed) / (Settings[Setting.MovingSpeed].Max - minSpeed)));
        }
    }

    private void DrawEngineJets()
    {
        if (qualityLevel == 0)
        {
            engines.SetEffectsAlpha(FIXED_ENGINE_ALPHA);
            return;
        }
        
        drawEngineEff.DrawJet(
            particleSystems:    engines,
            minSpeed:           0,
            maxSpeed:           Settings[Setting.MovingSpeed].Max,
            minAlpha:           0,
            maxAlpha:           0.3f,
            disableForLowSpeed: false,
            speed:              currentSpeed);

        if (qualityLevel > 1)
        {
            drawEngineEff.DrawJet(
                particleSystems:    glows,
                minSpeed:           0,
                maxSpeed:           Settings[Setting.MovingSpeed].Max,
                minAlpha:           0,
                maxAlpha:           0.3f,
                disableForLowSpeed: false,
                speed:              currentSpeed);

            if (qualityLevel > 2)
                drawEngineEff.DrawJet(
                    particleSystems:    trailEffects,
                    minSpeed:           0.3f * Settings[Setting.MovingSpeed].Max,
                    maxSpeed:           Settings[Setting.MovingSpeed].Max,
                    minAlpha:           0.1f,
                    maxAlpha:           0.24f,
                    disableForLowSpeed: true,
                    speed:              currentSpeed);
        }
    }


    private IEnumerator GettingAroundObstacle(RaycastHit hit)
    {
        isGettingAroundObstacle = true;
        float duration = 0;

        RaycastHit ht;

        while (Physics.Raycast(
            /* origin:      */  shipTransform.position,
            /* direction:   */  shipTransform.forward,
            /* hitInfo:     */  out ht,
            /* maxDistance: */  length + MAX_OBSTACLE_DISTANCE,
            /* layerMask:   */  HitMask))
        {
            transform.rotation
                = Quaternion.RotateTowards(
                    from:               transform.rotation,
                    to:                 Quaternion.LookRotation(hit.normal),
                    maxDegreesDelta:    stabilizationSpeed * OBSTACLE_STABILIZATION_QUALIFIER * Time.deltaTime);

            duration += Time.deltaTime;

            if (duration > MAX_OBSTACLE_STABILIZATION_DURATION)
            {
                isGettingAroundObstacle = false;
                lastObstacleStabilizationTime = Time.time;
                break;
            }

            yield return null;
        }

        isGettingAroundObstacle = false;
        lastObstacleStabilizationTime = Time.time;
    }

    private IEnumerator SpawnEffect()
    {
        spawnLens.gameObject.SetActive(true);

        if (PhotonView.isMine)
            spawnEffect.SetActive(true);

        spawnEffects.SetEffectsAlpha(SPAWN_EFFECT_ALPHA);

        spawnLens.brightness = SPAWN_LENS_BRIGHTNESS;
       
        yield return new WaitForSeconds(SPAWN_EFFECT_TIME);

        float t = 0;

        while (t < 1)
        {
            t += Time.deltaTime / SPAWN_EFFECT_FADE_OUT_TIME;

            spawnEffects.SetEffectsAlpha(Mathf.Lerp(SPAWN_EFFECT_ALPHA, 0, t));

            spawnLens.brightness = Mathf.Lerp(SPAWN_LENS_BRIGHTNESS, 0, t);

            yield return null;
        }

        spawnEffect.SetActive(false);
        spawnLens.gameObject.SetActive(false);
    }

    public IEnumerator Stabilization()
    {
        if (isStabilizing)
            yield break;

        isStabilizing = true;

        Quaternion stopRotation;

        do
        {
            stopRotation = Quaternion.LookRotation(transform.forward, Vector3.up);

            rigidbody.rotation = Quaternion.RotateTowards(rigidbody.rotation, stopRotation, stabilizationSpeed * Time.deltaTime);

            yield return null;
        }
        while (rigidbody.rotation != stopRotation);

        isStabilizing = false;
    }

    private void CollectEffects()
    {
        this.ChangePlayEngineEffBehaviour(new XD.DrawEngineJet(gameObject));
        if (spawnEffect)
        {
            speedEffects = speedEffect.GetComponentsInChildren<ParticleSystem>(includeInactive: true);
            spawnEffects = spawnEffect.GetComponentsInChildren<ParticleSystem>(includeInactive: true);

            spawnEffect.gameObject.SetActive(false);
            speedEffect.gameObject.SetActive(IsMine);

            spawnLens.gameObject.SetActive(false);
        }

        Transform effectRoot = transform.Find("Mesh/Effects");

        engines = drawEngineEff.FindEffects(effectRoot, "Engines");
        glows = drawEngineEff.FindEffects(effectRoot, "Glows");
        trails = drawEngineEff.FindEffects(effectRoot, "Trails");

        MiscTools.SetObjectsActivityByComponents(engines, true);
        MiscTools.SetObjectsActivityByComponents(glows, qualityLevel > 1);
        MiscTools.SetObjectsActivityByComponents(trails, qualityLevel > 2);
    }
}
