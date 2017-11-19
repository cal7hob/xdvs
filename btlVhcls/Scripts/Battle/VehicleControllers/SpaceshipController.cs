using UnityEngine;
using System.Collections;
using CodeStage.AntiCheat.ObscuredTypes;

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
    public GameObject sparksEffectPrefab;

    [Header("Звуки")]
    public AudioClip[] defaultCollisionEnterSounds;
    public AudioClip[] defaultCollisionExitSounds;
    public AudioClip[] shipCollisionSounds;
    public AudioClip[] frictionSounds;
    public float frictionFadeInSpeed = 1.75f;
    public float frictionFadeOutSpeed = 1.75f;

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

    public override float MaxAimDistance
    {
        get { return 1000.0f; }
    }

    protected override float OdometerRatio
    {
        get { return ODOMETER_RATIO; }
    }
    
    protected override float SpeedRatio
    {
        get { return SPEED_RATIO; }
    }

    public override float MaxShootAngle
    {
        get { return MAX_SHOOT_ANGLE; }
    }

    protected override bool NeedCorrectAimY
    {
        get { return false; }
    }
    
    protected override float VertAimCapture
    {
        get { return 17.0f; }
    }

    protected override float HorizAimCapture
    {
        get { return 25.5f; }
    }
    
    protected override float CorrectionTime
    {
        get { return CORRECTION_TIME; }
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

    protected Transform landminePoint;

    public override void OnPhotonInstantiate(PhotonMessageInfo info)
    {
        base.OnPhotonInstantiate(info);

        length = BodyMeshTransform.gameObject.GetComponentInChildren<Renderer>(true).bounds.extents.z;

        if (PhotonView.isMine)
            qualityLevel = QualitySettings.GetQualityLevel();
        else
            qualityLevel = 1; // Жесточайший хардкод. Убираем тяжелые эффекты для клонов.
        
        CollectEffects();

        DrawEngineJets();
        CheckSpeedEffect(1);

        landminePoint = transform.FindInHierarchy ("Mine_point");
    }

    protected override void Update()
    {
        base.Update();

        if (!IsAvailable)
            return;

        if (PhotonView.isMine)
        {
            MoveStaticGunsight();
            SetMyShipSpeed(ThrottleLevel.Value);
        }
        else
        {
            SetOthersShipsSpeed();
            
            if (burst)
                PrimaryFire();
        }

        if (!HelpTools.Approximately(accelerationDirection, 0))
            MarkActivity();

        if (!IsAvailable)
            return;

        if (PhotonView.isMine)
            CheckSpeedEffect(accelerationDirection);

        if (!HelpTools.Approximately(accelerationDirection, 0))
            DrawEngineJets();
    }

    protected void SetMyShipSpeed(float throttleVal)
    {
        requiredSpeed = Mathf.Lerp(minSpeed, MaxSpeed, throttleVal);

        accelerationDirection
            = HelpTools.Approximately(currentSpeed, requiredSpeed)
                ? 0
                : Mathf.Sign(requiredSpeed - currentSpeed);

        currentSpeed = Mathf.MoveTowards(currentSpeed, requiredSpeed, acceleration * Time.deltaTime);
    }

    protected void SetOthersShipsSpeed()
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
    }

    protected override void FixedUpdate()
    {
        MovePlayer();
    }

    void OnCollisionEnter(Collision collision)
    {
        EffectPoolDispatcher.GetFromPool(
            _effect:    sparksEffectPrefab,
            _position:  collision.contacts[0].point,
            _rotation:  Quaternion.identity);
    }

    void OnCollisionStay(Collision collision)
    {
        lastCollisionTime = Time.time;
    }

    /* PUBLIC SECTION */

    public override void MovePlayer()
    {
        if (!PhotonView.isMine || !IsAvailable)
            return;

        var horizontalAccel = XAxisControl;
        var verticalAccel = YAxisControl;

        if (!Mathf.Approximately(verticalAccel, 0) || !Mathf.Approximately(horizontalAccel, 0))
            MarkActivity();

        //GetAroundObstacle();

        rb.velocity = shipTransform.forward * currentSpeed;

        rb.AddForce(transform.forward * acceleration, ForceMode.Acceleration);

        isCollidedRecently = (Time.time - lastCollisionTime) < COLLISION_RESOLVE_TIME;

        if (isCollidedRecently && !HelpTools.Approximately(verticalAccel, 0))
            rb.velocity += -shipTransform.up * currentSpeed * COLLISION_RESOLVE_ACCELERATION * Mathf.Sign(verticalAccel);

        if (Vector3.Dot(rb.velocity, shipTransform.forward) < 0)
            rb.velocity = Vector3.zero;

        Vector3 localAV = transform.InverseTransformDirection(rb.angularVelocity);

        if (!HelpTools.Approximately(horizontalAccel, 0))
            rb.AddRelativeTorque(0, horizontalAccel * torqueAcceleration, 0, ForceMode.Acceleration);
        else
            rb.AddRelativeTorque(0, -localAV.y / torqueBrake, 0, ForceMode.Acceleration);

        if (!HelpTools.Approximately(verticalAccel, 0))
            rb.AddRelativeTorque(verticalAccel * torqueAcceleration, 0, 0, ForceMode.Acceleration);
        else
            rb.AddRelativeTorque(-localAV.x / torqueBrake, 0, 0, ForceMode.Acceleration);

        Vector3 localEuler = shipTransform.localEulerAngles;

        localEuler.z = Mathf.LerpAngle(localEuler.z, -horizontalAccel * maxInclineAngle, inclineSmooth);

        shipTransform.localEulerAngles = localEuler;

        StoreVehiclePosition();
    }

    [PunRPC]
    public override void Respawn(Vector3 position, Quaternion rotation, bool restoreLife, bool firstTime)
    {
        base.Respawn(position, rotation, restoreLife, firstTime);
    }

    public override void UpdateBotPrefabs(VehicleController nativeController)
    {
    }

    /* PRIVATE SECTION */

    protected override void OnTankRespawned(EventId id, EventInfo ei)
    {
        base.OnTankRespawned(id, ei);

        EventInfo_I info = (EventInfo_I)ei;

        if (info.int1 != data.playerId)
            return;

        CheckSpeedEffect(1);
        DrawEngineJets();

        if (spawnEffectRoutine != null)
            StopCoroutine(spawnEffectRoutine);

        spawnEffectRoutine = SpawnEffect();

        StartCoroutine(spawnEffectRoutine);
    }

    protected override void SetEngineNoise(float t) { }

    private void GetAroundObstacle()
    {
        bool isStabilizedRecently = (Time.time - lastObstacleStabilizationTime) < MAX_OBSTACLE_STABILIZATION_DELAY;

        if (isGettingAroundObstacle || isStabilizedRecently)
            return;

        RaycastHit hit;

        if (Physics.Raycast(
            /* origin:      */  shipTransform.position,
            /* direction:   */  shipTransform.forward,
            /* hitInfo:     */  out hit,
            /* maxDistance: */  length + MIN_OBSTACLE_DISTANCE,
            /* layerMask:   */  hitMask))
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
            SetEffectsAlpha(speedEffects, Mathf.Lerp(0, SPEED_EFFECT_ALPHA, (currentSpeed - minSpeed) / (MaxSpeed - minSpeed)));
    }

    protected void DrawEngineJets()
    {
        if (qualityLevel == 0)
        {
            SetEffectsAlpha(engines, FIXED_ENGINE_ALPHA);
            return;
        }
        
        DrawEngineJet(
            particleSystems:    engines,
            minSpeed:           0,
            maxSpeed:           MaxSpeed,
            minAlpha:           0,
            maxAlpha:           0.3f,
            disableForLowSpeed: false);

        if (qualityLevel > 1)
        {
            DrawEngineJet(
                particleSystems:    glows,
                minSpeed:           0,
                maxSpeed:           MaxSpeed,
                minAlpha:           0,
                maxAlpha:           0.3f,
                disableForLowSpeed: false);

            if (qualityLevel > 2)
                DrawEngineJet(
                    particleSystems:    trailEffects,
                    minSpeed:           0.3f * MaxSpeed,
                    maxSpeed:           MaxSpeed,
                    minAlpha:           0.1f,
                    maxAlpha:           0.24f,
                    disableForLowSpeed: true);
        }
    }
    
    private void DrawEngineJet(
        ParticleSystem[]    particleSystems,
        float               minSpeed,
        float               maxSpeed,
        float               minAlpha,
        float               maxAlpha,
        bool                disableForLowSpeed)
    {
        if (particleSystems == null)
            return;

        float t = Mathf.InverseLerp(minSpeed, maxSpeed, currentSpeed);
        bool disable = (currentSpeed < minSpeed) && disableForLowSpeed;
        float alpha = Mathf.Lerp(minAlpha, maxAlpha, t);

        foreach (ParticleSystem effect in particleSystems)
        {
            if (!effect)
                continue;

            if (disableForLowSpeed)
            {
                if (effect.gameObject.activeSelf == disable)
                    effect.gameObject.SetActive(!disable);

                continue;
            }

            var ma = effect.main;
            Color color = ma.startColor.color;

            color.a = alpha;

            ma.startColor = color;
        }
    }

    private void SetEffectsAlpha(ParticleSystem[] effects, float alpha)
    {
        for (int i = 0; i < effects.Length; i++)
        {
            var ma = effects[i].main;
            Color color = ma.startColor.color;

            color.a = alpha;

            ma.startColor = color;
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
            /* layerMask:   */  hitMask))
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
        
        SetEffectsAlpha(spawnEffects, SPAWN_EFFECT_ALPHA);

        spawnLens.brightness = SPAWN_LENS_BRIGHTNESS;
       
        yield return new WaitForSeconds(SPAWN_EFFECT_TIME);

        float t = 0;

        while (t < 1)
        {
            t += Time.deltaTime / SPAWN_EFFECT_FADE_OUT_TIME;

            SetEffectsAlpha(spawnEffects, Mathf.Lerp(SPAWN_EFFECT_ALPHA, 0, t));

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

            rb.rotation = Quaternion.RotateTowards(rb.rotation, stopRotation, stabilizationSpeed * Time.deltaTime);

            yield return null;
        }
        while (rb.rotation != stopRotation);

        isStabilizing = false;
    }

    private void CollectEffects()
    {
        if (spawnEffect)
        {
            speedEffects = speedEffect.GetComponentsInChildren<ParticleSystem>(includeInactive: true);
            spawnEffects = spawnEffect.GetComponentsInChildren<ParticleSystem>(includeInactive: true);

            spawnEffect.gameObject.SetActive(false);
            speedEffect.gameObject.SetActive(IsMain);

            spawnLens.gameObject.SetActive(false);
        }

        Transform effectRoot = shipTransform.Find("Effects");

        engines = FindEffects(effectRoot, "Engines");
        glows = FindEffects(effectRoot, "Glows");
        trails = FindEffects(effectRoot, "Trails");

        MiscTools.SetObjectsActivityByComponents(engines, true);
        MiscTools.SetObjectsActivityByComponents(glows, qualityLevel > 1);
        MiscTools.SetObjectsActivityByComponents(trails, qualityLevel > 2);
    }

    private ParticleSystem[] FindEffects(Transform baseRoot, string parentName)
    {
        Transform root = baseRoot.Find(parentName);

        ParticleSystem[] particleSystems = root.GetComponentsInChildren<ParticleSystem>();

        if (!root || null == particleSystems)
        {
            DT.LogError("Cannot collect '{0}' objects in {1}", parentName, name);
            return null;
        }

        return particleSystems;
    }

    public override Vector3 GetConsumableInstantiatePosition (string prefabName) {
        if (prefabName == "Landmine") {
            return landminePoint.position;
        }
        return base.GetConsumableInstantiatePosition (prefabName);
    }
}
