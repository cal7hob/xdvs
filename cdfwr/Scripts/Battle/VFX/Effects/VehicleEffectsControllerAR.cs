using System.Collections.Generic;
using UnityEngine;

public class VehicleEffectsControllerAR : MonoBehaviour
{
    public ParticleSystemWrapper fireEffect;
    public ParticleSystemWrapper smokeEffect;
    public ParticleSystem startEffect;
    public Transform[] dustEffectPoints;

    private const float FIRE_START_HEALTH_RATIO_THRESHOLD = 0.25f;
    private const float SMOKE_START_HEALTH_RATIO_THRESHOLD = 0.5f;

    private static readonly Color FIRE_START_COLOR = Color.black;
    private static readonly Color FIRE_FINAL_COLOR = new Color(1.0f, 0.47f, 0);
    private static readonly Color SMOKE_START_COLOR = new Color(0.35f, 0.35f, 0.35f);
    private static readonly Color SMOKE_FINAL_COLOR = Color.black;

    private VehicleController vehicleController;
    private List<ParticleSystem> dustEffects;

    void Start()
    {
        vehicleController = GetComponent<VehicleController>();

        SetDamageEffects();

        Dispatcher.Subscribe(EventId.TankHealthChanged, OnVehicleHealthChanged);
        Dispatcher.Subscribe(EventId.EngineStateChanged, OnEngineStateChanged);
    }

    void OnDestroy()
    {
        Dispatcher.Unsubscribe(EventId.TankHealthChanged, OnVehicleHealthChanged);
        Dispatcher.Unsubscribe(EventId.EngineStateChanged, OnEngineStateChanged);
    }

    private void OnVehicleHealthChanged(EventId id, EventInfo ei)
    {
      //  EventInfo_II info = (EventInfo_II)ei;

       // int playerId = info.int1;
        if (((EventInfo_II)ei).int1 == vehicleController.data.playerId)
        {
            SetDamageEffects();
        }
    }

    private void OnEngineStateChanged(EventId id, EventInfo ei)
    {
        EventInfo_II info = (EventInfo_II)ei;

        int playerId = info.int1;
        EngineState engineState = (EngineState)info.int2;

        if (playerId == vehicleController.data.playerId && vehicleController.IsMain)
        {
            SetStartEffect(engineState);
            SetDustEffect(engineState);
        }
    }

    private void SetDamageEffects()
    {
        float healthRatio = vehicleController.Armor / (float)vehicleController.MaxArmor;

        if (fireEffect != null)
        {
            fireEffect.SetChildrenColor(
                Color.Lerp(
                    a:  FIRE_FINAL_COLOR,
                    b:  FIRE_START_COLOR,
                    t:  healthRatio));

            fireEffect.gameObject.SetActive(
                vehicleController.IsAvailable &&
                healthRatio <= FIRE_START_HEALTH_RATIO_THRESHOLD &&
                Settings.GraphicsLevel > GraphicsLevel.mediumQuality);
        }

        if (smokeEffect != null)
        {
            smokeEffect.SetChildrenColor(
                Color.Lerp(
                    a:  SMOKE_FINAL_COLOR,
                    b:  SMOKE_START_COLOR,
                    t:  healthRatio));

            smokeEffect.gameObject.SetActive(
                vehicleController.IsAvailable &&
                healthRatio <= SMOKE_START_HEALTH_RATIO_THRESHOLD &&
                Settings.GraphicsLevel > GraphicsLevel.mediumQuality);
        }
    }

    private void SetStartEffect(EngineState engineState)
    {
        if (Settings.GraphicsLevel < GraphicsLevel.normalQuality)
        {
            return;
        }

        if ((engineState == EngineState.ForwardAcceleration || engineState == EngineState.BackwardAcceleration) && !startEffect.isPlaying)
        {
            startEffect.Play();
        }
    }

    private void SetDustEffect(EngineState engineState)
    {
        
        if (Settings.GraphicsLevel < GraphicsLevel.normalQuality)
        {
            return;
        }

        if (dustEffects == null)
        {
            dustEffects = InstantiateDustEffects();
        }

        if (dustEffects == null)
        {
            return;
        }

        if ((engineState == EngineState.ForwardAcceleration || engineState == EngineState.BackwardAcceleration) && !dustEffects[0].isPlaying)
        {
            
            foreach (ParticleSystem dustEffect in dustEffects)
            {
                dustEffect.Play();
            }
        }

        if (engineState == EngineState.Idle && dustEffects[0].isPlaying)
        {
            foreach (ParticleSystem dustEffect in dustEffects)
            {
                dustEffect.Stop();
            }
        }
    }

    private List<ParticleSystem> InstantiateDustEffects()
    {
        ParticleSystem dustPrefab = MapParticles.Instance.GroundDustAR;

        if (dustPrefab == null)
        {
            Debug.LogWarning("Не навешен эффект MapParticles.groundDustAR!");
            return null;
        }

        dustEffects = new List<ParticleSystem>();

        foreach (Transform point in dustEffectPoints)
        {
            ParticleSystem dustEffect = Instantiate(dustPrefab);

            dustEffect.transform.parent = point;
            dustEffect.transform.localPosition = Vector3.zero;
            dustEffect.transform.localRotation = Quaternion.identity;
            
            dustEffects.Add(dustEffect);
        }

        return dustEffects;
    }
}
