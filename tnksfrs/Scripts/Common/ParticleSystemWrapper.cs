using UnityEngine;
using XD;

public class ParticleSystemWrapper : MonoBehaviour
{
    private ParticleSystemChild[] children;
    private class ParticleSystemChild
    {
        public ParticleSystem ParticleSystem { get; private set; }
        public Color InitialColor { get; private set; }

        public ParticleSystemChild(ParticleSystem particleSystem)
        {
            ParticleSystem = particleSystem;
            InitialColor = particleSystem.startColor;
        }
    }

    private void Awake()
    {
        ParticleSystem[] particleSystems = GetComponentsInChildren<ParticleSystem>();

        children = new ParticleSystemChild[particleSystems.Length];

        for (int i = 0; i < particleSystems.Length; i++)
        {
            children[i] = new ParticleSystemChild(particleSystems[i]);
        }
    }

    public void SetChildrenAlpha(float value)
    {
        foreach (ParticleSystemChild child in children)
        {
            child.ParticleSystem.SetEffectAlpha(child.InitialColor.a*value);
        }
    }

    public void SetChildrenColor(Color color)
    {
        foreach (ParticleSystemChild child in children)
        {
            child.ParticleSystem.startColor = color;
        }
    }
}
