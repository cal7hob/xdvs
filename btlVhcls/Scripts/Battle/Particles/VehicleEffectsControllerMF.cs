using UnityEngine;

public class VehicleEffectsControllerMF : VehicleEffectsControllerAR
{
    public ParticleSystem headlampVolumeLeft;
    public ParticleSystem headlampVolumeRight;
    public ParticleSystem tailLampsLeft;
    public ParticleSystem tailLampsRight;

    private const float MIN_TAIL_ALPHA_MILTIPLIER = 0.2f;
    private const float MAX_TAIL_ALPHA_MILTIPLIER = 1.0f;
    private const float MIN_HEAD_ALPHA_MILTIPLIER = 1.0f;
    private const float MAX_HEAD_ALPHA_MILTIPLIER = 2.5f;
    private const float TAIL_ALPHA_SMOOTHING = 2.0f;
    private const string COLOR_PROPERTY_KEY = "_TintColor";

    private Renderer tailLampsRendererLeft;
    private Renderer tailLampsRendererRight;
    private float currentAlphaMultiplier;
    private float lastAlphaKeyTimeLeft;
    private float lastAlphaKeyTimeRight;
    private Color tailLampSourceColor;
    private Color headLampStartSourceColor;
    private ParticleSystem.ColorOverLifetimeModule colorOverLifetimeHeadLeft;
    private ParticleSystem.ColorOverLifetimeModule colorOverLifetimeHeadRight;
    private ParticleSystem.MainModule mainModuleHeadLeft;
    private ParticleSystem.MainModule mainModuleHeadRight;

    protected override void Start()
    {
        base.Start();

        tailLampsRendererLeft = tailLampsLeft.GetComponent<Renderer>();
        tailLampsRendererRight = tailLampsRight.GetComponent<Renderer>();

        colorOverLifetimeHeadLeft = headlampVolumeLeft.colorOverLifetime;
        colorOverLifetimeHeadRight = headlampVolumeRight.colorOverLifetime;

        mainModuleHeadLeft = headlampVolumeLeft.main;
        mainModuleHeadRight = headlampVolumeRight.main;

        headLampStartSourceColor = mainModuleHeadLeft.startColor.color;

        tailLampSourceColor = tailLampsRendererLeft.material.GetColor(COLOR_PROPERTY_KEY);

        SetHeadLamp();
    }

    void Update()
    {
        SetTailLamps();
        UpdateHeadLamps();
    }

    private void SetHeadLamp()
    {
        bool headLampEnabled = Map.IsDark && Settings.GraphicsLevel > GraphicsLevel.mediumQuality;
        headlampVolumeLeft.gameObject.SetActive(headLampEnabled);
        headlampVolumeRight.gameObject.SetActive(headLampEnabled);
    }

    private void UpdateHeadLamps()
    {
        UpdateHeadLamp(headlampVolumeLeft, colorOverLifetimeHeadLeft, mainModuleHeadLeft, ref lastAlphaKeyTimeLeft);
        UpdateHeadLamp(headlampVolumeRight, colorOverLifetimeHeadRight, mainModuleHeadRight, ref lastAlphaKeyTimeRight);
    }

    private void UpdateHeadLamp(
        ParticleSystem                          headlampVolume,
        ParticleSystem.ColorOverLifetimeModule  colorOverLifetime,
        ParticleSystem.MainModule               mainModule,
        ref float                               lastAlphaKeyTime)
    {
        float gradientAlphaKeyTime = 1.0f;
        float rayDistance = 4.5f;

        Debug.DrawRay(headlampVolume.transform.position, headlampVolume.transform.forward * rayDistance, Color.cyan);

        RaycastHit hit;

        bool hitSomething
            = Physics.Raycast(
                /* ray:         */  new Ray(headlampVolume.transform.position, headlampVolume.transform.forward),
                /* hitInfo:     */  out hit,
                /* maxDistance: */  rayDistance);

        if (hitSomething)
            gradientAlphaKeyTime = Mathf.Clamp01(hit.distance / rayDistance);

        if (!HelpTools.Approximately(gradientAlphaKeyTime, lastAlphaKeyTime))
        {
            GradientColorKey[] gradientColorKeys = colorOverLifetime.color.gradient.colorKeys;
            GradientAlphaKey[] gradientAlphaKeys = colorOverLifetime.color.gradient.alphaKeys;

            GradientAlphaKey[] newGradientAlphaKeys = new GradientAlphaKey[gradientAlphaKeys.Length];

            for (int i = 0; i < gradientAlphaKeys.Length; i++)
            {
                GradientAlphaKey gradientAlphaKey = gradientAlphaKeys[i];

                if (HelpTools.Approximately(gradientAlphaKey.alpha, 0))
                {
                    gradientAlphaKey.time = gradientAlphaKeyTime;
                    lastAlphaKeyTime = gradientAlphaKey.time;
                }

                newGradientAlphaKeys[i] = gradientAlphaKey;
            }

            Gradient gradient = new Gradient();

            gradient.SetKeys(gradientColorKeys, newGradientAlphaKeys);

            colorOverLifetime.color = gradient;

            mainModule.startColor
                = new Color(
                    r: headLampStartSourceColor.r,
                    g: headLampStartSourceColor.g,
                    b: headLampStartSourceColor.b,
                    a: headLampStartSourceColor.a * Mathf.Lerp(MAX_HEAD_ALPHA_MILTIPLIER, MIN_HEAD_ALPHA_MILTIPLIER, gradientAlphaKeyTime));
        }
    }

    private void SetTailLamps()
    {
        float targetAlphaMultiplier;

        if (vehicleController.PhotonView.isMine)
            targetAlphaMultiplier = vehicleController.YAxisControl;
        else
            targetAlphaMultiplier = vehicleController.Speed / vehicleController.MaxSpeed;

        targetAlphaMultiplier = -targetAlphaMultiplier;
        targetAlphaMultiplier = Mathf.Clamp01(targetAlphaMultiplier);
        targetAlphaMultiplier = Mathf.Lerp(MIN_TAIL_ALPHA_MILTIPLIER, MAX_TAIL_ALPHA_MILTIPLIER, targetAlphaMultiplier);

        if (HelpTools.Approximately(currentAlphaMultiplier, targetAlphaMultiplier))
            return;

        currentAlphaMultiplier = Mathf.MoveTowards(currentAlphaMultiplier, targetAlphaMultiplier, TAIL_ALPHA_SMOOTHING * Time.deltaTime);

        tailLampsRendererLeft.material.SetColor(
            name:   COLOR_PROPERTY_KEY,
            value:  new Color(
                        r:  tailLampSourceColor.r,
                        g:  tailLampSourceColor.g,
                        b:  tailLampSourceColor.b,
                        a:  tailLampSourceColor.a * currentAlphaMultiplier));

        tailLampsRendererRight.material.SetColor(
            name:   COLOR_PROPERTY_KEY,
            value:  new Color(
                        r:  tailLampSourceColor.r,
                        g:  tailLampSourceColor.g,
                        b:  tailLampSourceColor.b,
                        a:  tailLampSourceColor.a * currentAlphaMultiplier));
    }
}
