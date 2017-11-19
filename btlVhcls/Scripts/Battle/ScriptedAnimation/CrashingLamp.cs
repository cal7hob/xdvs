using UnityEngine;

public class CrashingLamp : MonoBehaviour
{
    [Tooltip("Весить только на ParticleSystem, где есть материал с проперти '" + COLOR_PROPERTY_KEY  + "'! Активируется на OnEnable().")]
    public AnimationCurve alphaCurve;

    private const string COLOR_PROPERTY_KEY = "_TintColor";

    private new ParticleSystem particleSystem;
    private new Renderer renderer;
    private Color sourceColor;
    private float startTime;

    private float TimeElapsed
    {
        get { return Time.time - startTime; }
    }

    void OnEnable()
    {
        particleSystem = GetComponent<ParticleSystem>();
        renderer = particleSystem.GetComponent<Renderer>();
        sourceColor = renderer.material.GetColor(COLOR_PROPERTY_KEY);
        startTime = Time.time;
    }

    void Update()
    {
        renderer.material.SetColor(
            name:   COLOR_PROPERTY_KEY,
            value:  new Color(
                        r:  sourceColor.r,
                        g:  sourceColor.g,
                        b:  sourceColor.b,
                        a:  sourceColor.a * alphaCurve.Evaluate(TimeElapsed)));
    }
}
