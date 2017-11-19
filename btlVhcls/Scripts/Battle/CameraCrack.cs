using System.Collections;
using UnityEngine;

public class CameraCrack : MonoBehaviour
{
    private const float HEALTH_RATIO_THRESHOLD = 0.4f;
    private const float DISAPPEAING_SPEED = 1.00f;
    private const string DISTORTION_SHADER_NAME = "Additive_Distort";
    private const string COLOR_PROPERTY_KEY = "_TintColor";
    private const string DISTORTION_PROPERTY_KEY = "_BumpAmt";

    private new ParticleSystem particleSystem;
    private Renderer[] particleRenderers;
    private bool isPlaying;
    private bool isDisappearingSmoothly;
    private float initialDistortion;
    private Color initialColor;
    private IEnumerator disappearingRoutine;

    void Awake()
    {
        particleSystem = GetComponent<ParticleSystem>();
        particleRenderers = GetComponentsInChildren<Renderer>();

        GetInitialValues(ref initialDistortion, ref initialColor);

        Dispatcher.Subscribe(EventId.TankDamageApplied, OnTankDamageApplied);
        Dispatcher.Subscribe(EventId.TankRespawned, OnTankRespawned);
        Dispatcher.Subscribe(EventId.TankHealthChanged, OnTankHealthChanged);
    }

    void OnDestroy()
    {
        Dispatcher.Unsubscribe(EventId.TankDamageApplied, OnTankDamageApplied);
        Dispatcher.Unsubscribe(EventId.TankRespawned, OnTankRespawned);
        Dispatcher.Unsubscribe(EventId.TankHealthChanged, OnTankHealthChanged);
    }

    private void OnTankDamageApplied(EventId id, EventInfo ei)
    {
        EventInfo_U info = (EventInfo_U)ei;

        int playerId = (int)info[0];

        if (playerId != BattleController.MyPlayerId)
            return;

        float healthRatio = BattleController.MyVehicle.Armor / (float)BattleController.MyVehicle.MaxArmor;

        if (healthRatio < HEALTH_RATIO_THRESHOLD && !isPlaying)
            Play();
    }

    private void OnTankRespawned(EventId id, EventInfo ei)
    {
        EventInfo_I info = (EventInfo_I)ei;

        int playerId = info.int1;

        if (playerId == BattleController.MyPlayerId)
            Disappear(ref disappearingRoutine);
    }

    private void OnTankHealthChanged(EventId id, EventInfo ei)
    {
        EventInfo_II info = (EventInfo_II)ei;

        int playerId = info.int1;

        if (playerId != BattleController.MyPlayerId)
            return;

        float healthRatio = BattleController.MyVehicle.Armor / (float)BattleController.MyVehicle.MaxArmor;

        if (healthRatio > HEALTH_RATIO_THRESHOLD && isPlaying)
            DisappearSmoothly(ref disappearingRoutine);
    }

    private void Play()
    {
        StopDisappearing(disappearingRoutine);
        isPlaying = true;
        particleSystem.Play(true);
    }

    private void Disappear(ref IEnumerator disappearingRoutine)
    {
        if (disappearingRoutine != null)
            StopDisappearing(disappearingRoutine);

        particleSystem.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
    }

    private void DisappearSmoothly(ref IEnumerator disappearingRoutine)
    {
        if (isDisappearingSmoothly)
            return;

        if (disappearingRoutine != null)
            StopSmoothDisappearing(disappearingRoutine);

        disappearingRoutine = Disappearing();

        StartCoroutine(disappearingRoutine);
    }

    private void StopDisappearing(IEnumerator disappearingRoutine)
    {
        if (disappearingRoutine != null)
            StopCoroutine(disappearingRoutine);

        particleSystem.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        SetOpacity(1);
        isPlaying = false;
    }

    private void StopSmoothDisappearing(IEnumerator disappearingRoutine)
    {
        StopCoroutine(disappearingRoutine);
        SetOpacity(1);
        isDisappearingSmoothly = false;
    }

    private void SetOpacity(float value)
    {
        foreach (Renderer particleRenderer in particleRenderers)
        {
            if (!particleRenderer.material.shader.name.Contains(DISTORTION_SHADER_NAME))
                continue;

            particleRenderer.material.SetColor(COLOR_PROPERTY_KEY, Color.Lerp(Color.black, initialColor, value));
            particleRenderer.material.SetFloat(DISTORTION_PROPERTY_KEY, Mathf.Lerp(0, initialDistortion, value));
        }
    }

    private void GetInitialValues(ref float initialDistortion, ref Color initialColor)
    {
        foreach (Renderer particleRenderer in particleRenderers)
        {
            if (particleRenderer.material.shader.name.Contains(DISTORTION_SHADER_NAME))
            {
                initialDistortion = particleRenderer.material.GetFloat(DISTORTION_PROPERTY_KEY);
                initialColor = particleRenderer.material.GetColor(COLOR_PROPERTY_KEY);
                return;
            }
        }

        Debug.LogError("Initial values not found!");
    }

    private IEnumerator Disappearing()
    {
        isDisappearingSmoothly = true;
        float progress = 1;

        while (progress > 0)
        {
            SetOpacity(progress);
            progress -= DISAPPEAING_SPEED * Time.deltaTime;
            yield return null;
        }

        particleSystem.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        isPlaying = false;
        isDisappearingSmoothly = false;
    }
}
