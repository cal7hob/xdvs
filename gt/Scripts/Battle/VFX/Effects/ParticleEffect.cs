using UnityEngine;
using VFX;

public class ParticleEffect : Effect
{
    private ParticleSystem particleEffect;

    public override bool IsPlaying
    {
        get { return particleEffect.isPlaying; }
    }

    void Awake()
    {
        particleEffect = GetComponent<ParticleSystem>();

        if (particleEffect == null)
        {
            Debug.LogError("there is no ParticleSystem component attached");
        }
    }


    public override void Play()
    {
        particleEffect.Play();
    }

    public override void Stop()
    {
        particleEffect.Stop();
    }
}
