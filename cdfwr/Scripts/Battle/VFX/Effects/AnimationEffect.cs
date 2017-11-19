using UnityEngine;
using VFX;

public class AnimationEffect : Effect
{
    private Animation anim;

    public override bool IsPlaying
    {
        get { return anim.isPlaying; }
    }

    void Awake()
    {
        anim = GetComponent<Animation>();
        anim.wrapMode = WrapMode.Once;

        if (anim == null)
        {
            Debug.LogError("there is no Animation component attached");
        }
    }

    public override void Play()
    {
        anim.Play();
    }

    public override void Stop()
    {
        anim.Stop();
    }
}
