using System.Collections;
using UnityEngine;

public class FlightCameraController : BattleCamera
{
    [Header("FOV")]
    public float accelerationThreshold = 0.35f;
    public float acceleratedCamFOV = 72;
    public float accelerationFOVChangeSpeed = 250f;
    public float respawnedCamFOV = 180;
    public float respawnFOVChangeSpeed = 230f;
    public float aimedCamFOV = 40.0f;

    [Header("Тряска BOH")]
    public float shakingStartSpeed = 10;
    public float accelerationRatio = 1;
    public float vibrationFadeRatio = 0.99f;

    [Header("Дамаг тряска BOW")]
    public float damageShakingAmplitude = 0.3f;
    public float damageShakingSpeedMin = 2.0f;
    public float damageShakingSpeedMax = 4.0f;
    public AnimationCurve damageShakingCurve;

    [Header("Ускорение тряска BOW")]
    public float accelerationShakingAmplitudePosition = 0.05f;
    public float accelerationShakingAmplitudeLook = 1.0f;
    public float accelerationAccumulationMultiplier = 7.0f;
    public float accelerationAccumulationDecreaseSpeed = 0.8f;
    public AnimationCurve accelerationShakingCurveX;
    public AnimationCurve accelerationShakingCurveY;

    [Header("Разное")]
    public float rotationSmooth = 0.1f;
    public float childRotationSmooth = 0.25f;
    public float noiseDuration = 0.6f;

    protected bool IsDamageShaking;
    protected GameObject noiseEffect;
    protected Vector3 initialCamPosition;
    protected Quaternion initialCamRototation;
    protected FlightController myFlight;
    protected FlightController flightInView;
    
    private bool noiseIsOnScreen;
    private float previousAccelerationProgress;
    private float accumulatedAcceleration;
    private IEnumerator damageShakeRoutine;
    private IEnumerator screenNoiseRoutine;

    public static FlightCameraController FlightCamInstance { get; protected set; }

    protected override void Awake()
    {
        base.Awake();

        FlightCamInstance = this;

        Dispatcher.Subscribe(EventId.TankTakesDamage, OnTankTakesDamage);
        Dispatcher.Subscribe(EventId.ShellHit, OnShellHit);
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();

        FlightCamInstance = null;

        Dispatcher.Unsubscribe(EventId.ShellHit, OnShellHit);
        Dispatcher.Unsubscribe(EventId.TankTakesDamage, OnTankTakesDamage);
    }

    protected void FixedUpdate()
    {
        if (vehicleInView == null)
            return;

        currentState.CamMotion();
    }

    public override void CamRegularMotion()
    {
        transform.position = vehicleInView.transform.position;

        transform.rotation
            = Quaternion.Lerp(
                a:  transform.rotation,
                b:  Quaternion.LookRotation(
                        forward:    (lookPointTransform.position - transform.position).normalized,
                        upwards:    vehicleInView.transform.up),
                t:  rotationSmooth);

        if (IsDamageShaking)
            return;

        float deltaAccelerationProgress = flightInView.PureAccelerationProgress - previousAccelerationProgress;

        if (deltaAccelerationProgress > 0)
            accumulatedAcceleration += deltaAccelerationProgress * accelerationAccumulationMultiplier;

        accumulatedAcceleration = Mathf.MoveTowards(accumulatedAcceleration, 0, accelerationAccumulationDecreaseSpeed * Time.fixedDeltaTime);

        float accelerationShakeInput = Mathf.Repeat(accumulatedAcceleration, 1);

        Vector3 offsetPosition
            = (Vector3.right * accelerationShakingCurveX.Evaluate(accelerationShakeInput) * Random.Range(0.25f, 1.0f) * accelerationShakingAmplitudePosition)
                + (Vector3.up * accelerationShakingCurveY.Evaluate(accelerationShakeInput) * Random.Range(0.25f, 1.0f) * accelerationShakingAmplitudePosition);

        Vector3 offsetLook
            = (Vector3.right * accelerationShakingCurveX.Evaluate(accelerationShakeInput) * Random.Range(0.25f, 1.0f) * accelerationShakingAmplitudeLook)
                + (Vector3.up * accelerationShakingCurveY.Evaluate(accelerationShakeInput) * Random.Range(0.25f, 1.0f) * accelerationShakingAmplitudeLook);

        camTransform.localPosition = flightInView.cameraPosition + offsetPosition;

        camTransform.rotation
            = Quaternion.Lerp(
                a:  camTransform.rotation,
                b:  Quaternion.LookRotation(
                        forward:    ((lookPointTransform.position + offsetLook) - camTransform.position).normalized,
                        upwards:    transform.up),
                t:  childRotationSmooth);

        SetAccelerationFOV();

        if (flightInView.PureAccelerationProgress >= 0)
            previousAccelerationProgress = flightInView.PureAccelerationProgress;
    }

    public override void ShowKillerMotion()
    {
        CamRegularMotion(); // Можно и как-то иначе показывать убийцу... не так же, как себя.
    }

    public override void ZoomMotion() { }

    public void DamageShake(Vector3 shakingVector)
    {
        if (IsDamageShaking)
            return;

        damageShakeRoutine = DamageShaking(shakingVector);

        StartCoroutine(damageShakeRoutine);
    }

    protected override void Init(EventId id, EventInfo ei)
    {
        base.Init(id, ei);

        myFlight = (FlightController)BattleController.MyVehicle;

        camTransform = camTransform ?? cam.transform;

        camTransform.localPosition = myFlight.cameraPosition;

        initialCamPosition = camTransform.localPosition;
        initialCamRototation = camTransform.localRotation;

        cam.gameObject.AddComponent<VehicleCameraCollision>();
    }

    protected virtual void OnShellHit(EventId eid, EventInfo ei)
    {
        EventInfo_U info = (EventInfo_U)ei;

        int victimId = (int)info[0];
        int damage = (int)info[1];
        int ownerId = (int)info[2];
        GunShellInfo.ShellType shellType = (GunShellInfo.ShellType)(int)info[3];
        int hits = (int)info[4];
        Vector3 hitPosition = (Vector3)info[5];

        if (victimId != BattleController.MyPlayerId || !BattleController.MyVehicle.IsAvailable)
            return;

        VehicleController attacker;

        if (!BattleController.allVehicles.TryGetValue(ownerId, out attacker))
            return;

        Vector3 hitPoint = BattleController.MyVehicle.transform.TransformPoint(hitPosition);

        DamageShake((attacker.transform.position - hitPoint).normalized);
    }

    protected override void OnVehicleKilled(EventId id, EventInfo ei)
    {
        var info = (EventInfo_III)ei;

        var victimId = info.int1;
        var killerId = info.int2;

        if ((killerId != BattleController.MyPlayerId) && (victimId != BattleController.MyPlayerId)) // Не наше событие, выходим.
            return;

        base.OnVehicleKilled(id, ei);

        if (screenNoiseRoutine != null)
            StopCoroutine(screenNoiseRoutine);

        SwitchNoise(false);

        if (damageShakeRoutine != null)
            StopCoroutine(damageShakeRoutine);

        SwitchOffShaking();
    }

    protected override void OnTargetAimed(EventId id, EventInfo ei) { }

    protected override void SwitchToMyVehicle(EventId id, EventInfo ei)
    {
        base.SwitchToMyVehicle(id, ei);

        flightInView = myFlight;

        camTransform.localPosition = initialCamPosition;
        camTransform.localRotation = initialCamRototation;
    }

    protected override void OnBattleEnd(EventId id, EventInfo ei)
    {
        StopAllCoroutines();
        SwitchNoise(false);
    }

    protected override void SwitchToKiller()
    {
        base.SwitchToKiller();
        flightInView = killer as FlightController;
    }

    protected override void OnZoomBtn() { }

    protected void SetAccelerationFOV()
    {
        float currentSpeed = myFlight.IsAvailable ? flightInView.CurrentSpeed : 0;

        Camera.main.fieldOfView
            = Mathf.MoveTowards(
                current:    Camera.main.fieldOfView,
                target:     ((currentSpeed - myFlight.minSpeed) / (flightInView.MaxSpeed - flightInView.minSpeed)) > accelerationThreshold ? acceleratedCamFOV : DefaultCamFOV,
                maxDelta:   accelerationFOVChangeSpeed * Time.deltaTime);
    }

    protected virtual void SwitchOffShaking()
    {
        camTransform.localPosition = initialCamPosition;
        IsDamageShaking = false;
    }

    protected virtual IEnumerator DamageShaking(Vector3 shakingDirection)
    {
        IsDamageShaking = true;

        float speed = Random.Range(damageShakingSpeedMin, damageShakingSpeedMax);
        Vector3 initialLocalPosition = camTransform.localPosition;
        Vector3 localShakingDirection = camTransform.InverseTransformDirection(shakingDirection);

        float progress = 0;

        while (progress < 1)
        {
            camTransform.localPosition = initialLocalPosition + (localShakingDirection * damageShakingCurve.Evaluate(progress) * damageShakingAmplitude);
            progress += Time.deltaTime * speed;
            yield return null;
        }

        SwitchOffShaking();
    }

    private void OnTankTakesDamage(EventId eid, EventInfo ei)
    {
        EventInfo_U info = (EventInfo_U)ei;

        if ((int)info[0] != BattleController.MyPlayerId)
            return;

        if (!noiseIsOnScreen)
        {
            screenNoiseRoutine = ScreenNoiseAppearing();
            StartCoroutine(screenNoiseRoutine);
        }
    }

    private void SwitchNoise(bool active)
    {
        if (noiseEffect == null)
            return;

        noiseIsOnScreen = active;

        noiseEffect.SetActive(active);
    }

    private IEnumerator ScreenNoiseAppearing()
    {
        SwitchNoise(true);
        yield return new WaitForSeconds(noiseDuration);
        SwitchNoise(false);
    }
}
