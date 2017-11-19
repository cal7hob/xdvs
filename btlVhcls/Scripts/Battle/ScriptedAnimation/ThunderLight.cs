using UnityEngine;

public class ThunderLight : MonoBehaviour
{
    [Header("Крутилки")]
    public float maxIntensity = 3.5f;
    public float interval = 14.97f;
    public float duration = 0.1f;

    [Header("Реагирует на запуск частиц (опционально)")]
    public new ParticleSystem particleSystem;

    private new Light light;
    private bool isLightningSet;
    private bool isLaunched;
    private float initialIntensity;
    private float nextLightningTime;

    private bool IsLightningRequired
    {
        get { return Time.time > nextLightningTime && Time.time < (nextLightningTime + duration) && isLaunched; }
    }

    void Awake()
    {
        light = GetComponent<Light>();
        ParticleSystemEvents.Started += OnParticleSystemStarted;
    }

    void OnDestroy()
    {
        ParticleSystemEvents.Started -= OnParticleSystemStarted;
    }

    void Start()
    {
        if (particleSystem == null)
            Launch();
    }

    void Update()
    {
        if (IsLightningRequired)
            Lightning();
        else
            RestoreLight();
    }

    private void OnParticleSystemStarted(ParticleSystem particleSystem)
    {
        if (particleSystem == this.particleSystem)
            Launch();
    }

    private void Launch()
    {
        initialIntensity = light.intensity;
        SetLightningTime();
        isLaunched = true;
    }

    private void SetLightningTime()
    {
        nextLightningTime = Time.time + interval;
        isLightningSet = true;
    }

    private void Lightning()
    {
        light.intensity = maxIntensity;
        isLightningSet = false;
    }

    private void RestoreLight()
    {
        if (isLightningSet || !isLaunched)
            return;

        light.intensity = initialIntensity;

        SetLightningTime();
    }
}
