using UnityEngine;

public class VehicleEffectsControllerBOW : MonoBehaviour
{
    [Header("Разгон")]
    public GameObject speedEffect;
    public ParticleSystem[] trailEffects;

    [Header("Повреждения")]
    public GameObject fireEffect;
    public GameObject smokeEffect;

    [Header("Звуковой барьер")]
    public GameObject soundBarrierEffectPrefab;
    public Transform soundBarrierEffectPoint;

    private const float SPEED_EFFECT_ALPHA = 27.0f / 255.0f;
    private const float FIRE_START_HEALTH_RATIO_THRESHOLD = 0.5f;
    private const float SMOKE_START_HEALTH_RATIO_THRESHOLD = 0.4f;
    private const float SOUND_BARRIER_ACCELERATION_PROGRESS_THRESHOLD = 0.75f;
    private const float SOUND_BARRIER_INTERVAL = 10.0f;

    private static readonly Color FIRE_START_COLOR = Color.black;
    private static readonly Color FIRE_FINAL_COLOR = new Color(1.0f, 0.47f, 0);
    private static readonly Color SMOKE_START_COLOR = new Color(0.35f, 0.35f, 0.35f);
    private static readonly Color SMOKE_FINAL_COLOR = Color.black;

    private AircraftController aircraftController;
    private ParticleSystem[] speedEffects;
    private float previousAccelerationProgress;
    private float lastSoundBarrierTime;

    void Awake()
    {
        aircraftController = GetComponent<AircraftController>();

        Dispatcher.Subscribe(EventId.TankJoinedBattle, OnAircraftJoinedBattle);
        Dispatcher.Subscribe(EventId.TankHealthChanged, OnAircraftHealthChanged);
        Dispatcher.Subscribe(EventId.TankKilled, OnAircraftKilled);
        Dispatcher.Subscribe(EventId.TankRespawned, OnAircraftRespawned);
    }

    void OnDestroy()
    {
        Dispatcher.Unsubscribe(EventId.TankJoinedBattle, OnAircraftJoinedBattle);
        Dispatcher.Unsubscribe(EventId.TankHealthChanged, OnAircraftHealthChanged);
        Dispatcher.Unsubscribe(EventId.TankKilled, OnAircraftKilled);
        Dispatcher.Unsubscribe(EventId.TankRespawned, OnAircraftRespawned);
    }

    public void UpdateEffects(float accelerationValue)
    {
        UpdateSpeedEffect(accelerationValue);
        UpdateSoundBarrierEffect();
    }

    private void OnAircraftJoinedBattle(EventId id, EventInfo ei)
    {
        EventInfo_I info = (EventInfo_I)ei;

        int playerId = info.int1;

        if (playerId != aircraftController.data.playerId)
            return;

        CollectEffects();
        SetDamageEffects();
        UpdateEffects(1);
    }

    private void OnAircraftHealthChanged(EventId id, EventInfo ei)
    {
        EventInfo_II info = (EventInfo_II)ei;

        int playerId = info.int1;

        if (playerId == aircraftController.data.playerId)
            SetDamageEffects();
    }

    private void OnAircraftKilled(EventId id, EventInfo ei)
    {
        EventInfo_III info = (EventInfo_III)ei;

        int playerId = info.int1;

        if (playerId == aircraftController.data.playerId)
            SetDeathEffects();
    }

    private void OnAircraftRespawned(EventId id, EventInfo ei)
    {
        EventInfo_I info = (EventInfo_I)ei;

        if (info.int1 != aircraftController.data.playerId)
            return;

        UpdateEffects(1);
    }

    private void CollectEffects()
    {
        if (speedEffect != null)
        {
            speedEffects = speedEffect.GetComponentsInChildren<ParticleSystem>(true);
            speedEffect.gameObject.SetActive(aircraftController.IsMain);
        }

        if (trailEffects != null)
            MiscTools.SetObjectsActivityByComponents(trailEffects, aircraftController.QualityLevel > 1);
    }

    private void SetDamageEffects()
    {
        float healthRatio = aircraftController.Armor / (float)aircraftController.MaxArmor;

        if (fireEffect != null)
        {
            ParticleSystem.MainModule ma = fireEffect.GetComponent<ParticleSystem>().main;

            ma.startColor
                = Color.Lerp(
                    a:  FIRE_START_COLOR,
                    b:  FIRE_FINAL_COLOR,
                    t:  1 - healthRatio);

            fireEffect.gameObject.SetActive(
                aircraftController.IsMain &&
                !aircraftController.IsExploded &&
                aircraftController.QualityLevel > 1 &&
                healthRatio <= FIRE_START_HEALTH_RATIO_THRESHOLD);
        }

        if (smokeEffect != null)
        {
            ParticleSystem.MainModule ma = smokeEffect.GetComponent<ParticleSystem>().main;

            ma.startColor
                = Color.Lerp(
                    a:  SMOKE_START_COLOR,
                    b:  SMOKE_FINAL_COLOR,
                    t:  1 - healthRatio);

            smokeEffect.gameObject.SetActive(
                aircraftController.IsMain &&
                !aircraftController.IsExploded &&
                aircraftController.QualityLevel > 1 &&
                healthRatio <= SMOKE_START_HEALTH_RATIO_THRESHOLD);
        }
    }

    private void SetDeathEffects()
    {
        if (fireEffect != null)
        {
            ParticleSystem.MainModule ma = fireEffect.GetComponent<ParticleSystem>().main;
            ma.startColor = FIRE_FINAL_COLOR;
            fireEffect.gameObject.SetActive(aircraftController.IsMain);
        }

        if (smokeEffect != null)
        {
            ParticleSystem.MainModule ma = smokeEffect.GetComponent<ParticleSystem>().main;
            ma.startColor = SMOKE_FINAL_COLOR;
            smokeEffect.gameObject.SetActive(aircraftController.IsMain && aircraftController.QualityLevel > 1);
        }
    }

    private void UpdateSpeedEffect(float accelerationValue)
    {
        if (!HelpTools.Approximately(accelerationValue, 0) && speedEffects != null)
        {
            SetEffectsAlpha(
                effects:    speedEffects,
                alpha:      Mathf.Lerp(
                                a:  0,
                                b:  SPEED_EFFECT_ALPHA,
                                t:  aircraftController.PureAccelerationProgress));
        }
    }

    private void UpdateSoundBarrierEffect()
    {
        if (!aircraftController.IsMain || soundBarrierEffectPoint == null)
            return;

        float currentAccelerationProgress = aircraftController.PureAccelerationProgress;

        if (currentAccelerationProgress > previousAccelerationProgress &&
            currentAccelerationProgress > SOUND_BARRIER_ACCELERATION_PROGRESS_THRESHOLD &&
            Time.time - lastSoundBarrierTime > SOUND_BARRIER_INTERVAL)
        {
            EffectPoolDispatcher.GetFromPool(
                _effect:        soundBarrierEffectPrefab,
                _position:      soundBarrierEffectPoint.position,
                _rotation:      soundBarrierEffectPoint.rotation,
                useEffectMover: true,
                moverTarget:    aircraftController.Body);

            lastSoundBarrierTime = Time.time;
        }

        if (aircraftController.PureAccelerationProgress >= 0)
            previousAccelerationProgress = currentAccelerationProgress;
    }

    private void SetEffectsAlpha(ParticleSystem[] effects, float alpha)
    {
        for (int i = 0; i < effects.Length; i++)
        {
            ParticleSystem.MainModule main = effects[i].main;

            Color color = main.startColor.color;
            color.a = alpha;

            main.startColor = color;
        }
    }
}
