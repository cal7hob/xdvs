using UnityEngine;

public class TankAnimationController : MonoBehaviour
{
    public AnimationClip shotClip;

    private const float FADE_LENGTH = 0.1f;

    private TankController tankController;
    private new Animation animation;
    private float lastClipEnds;

    public bool IsPlaying
    {
        get { return Time.time < lastClipEnds; }
    }

    void Awake()
    {
        tankController = GetComponent<TankController>();
        animation = GetComponent<Animation>();

        Dispatcher.Subscribe(EventId.StartBurstFire, OnStartBurstFire);
    }

    void OnDestroy()
    {
        Dispatcher.Unsubscribe(EventId.StartBurstFire, OnStartBurstFire);
    }

    private void OnStartBurstFire(EventId id, EventInfo ei)
    {
        //EventInfo_II info = (EventInfo_II)ei;

        //int playerId = info.int1;

        //if (playerId != tankController.data.playerId)
        //    return;

        //if (tankController.IsMain || Settings.GraphicsLevel > GraphicsLevel.lowQuality)
        //    PlayClip(shotClip);
    }

    private void PlayClip(AnimationClip animationClip)
    {
        if (animationClip == null)
            return;

        string clipName = animationClip.name;

        if (animation.GetClip(clipName) == null)
            animation.AddClip(animationClip, clipName);

        if (!animation.IsPlaying(clipName))
        {
            animation.CrossFade(clipName, FADE_LENGTH);
            lastClipEnds = Time.time + animationClip.length;
        }
    }
}
