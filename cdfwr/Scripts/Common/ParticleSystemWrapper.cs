using UnityEngine;

public class ParticleSystemWrapper : MonoBehaviour
{
    private class ParticleSystemChild
    {
        public ParticleSystemChild(ParticleSystem particleSystem)
        {
            ParticleSystem = particleSystem;
            InitialColor = particleSystem.main.startColor.color;
        }

        public ParticleSystem ParticleSystem { get; private set; }

        public Color InitialColor { get; private set; }
    }

    private ParticleSystemChild[] children;

    void Awake()
    {
        ParticleSystem[] particleSystems = GetComponentsInChildren<ParticleSystem>();

        children = new ParticleSystemChild[particleSystems.Length];

        for (int i = 0; i < particleSystems.Length; i++)
            children[i] = new ParticleSystemChild(particleSystems[i]);
    }

    public void SetChildrenAlpha(float value)
    {
        foreach (ParticleSystemChild child in children)
            child.ParticleSystem.startColor
                = new Color(
                    r: child.ParticleSystem.main.startColor.color.r,
                    g: child.ParticleSystem.main.startColor.color.g,
                    b:  child.ParticleSystem.main.startColor.color.b,
                    a:  child.InitialColor.a * value);
    }

    public void SetChildrenColor(Color color)
    {
        foreach (ParticleSystemChild child in children)
            child.ParticleSystem.startColor = color;
    }
}
