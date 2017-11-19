using UnityEngine;

public class TankAnimationController : MonoBehaviour
{
    public AnimationClip shotClip;
    public AnimationClip startClip;
    public AnimationClip stopClip;

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

        Dispatcher.Subscribe(EventId.EngineStateChanged, OnEngineStateChanged);
        Dispatcher.Subscribe(EventId.StartBurstFire, OnStartBurstFire);
    }

    void OnDestroy()
    {
        Dispatcher.Unsubscribe(EventId.EngineStateChanged, OnEngineStateChanged);
        Dispatcher.Unsubscribe(EventId.StartBurstFire, OnStartBurstFire);
    }

    private void OnEngineStateChanged(EventId id, EventInfo ei)
    {
        if (!tankController.IsMine)
            return;

        EventInfo_II info = (EventInfo_II)ei;

        int playerId = info.int1;
        EngineState engineState = (EngineState)info.int2;

        if (playerId == tankController.data.playerId)
            PlayClip(ChooseAnimationClip(engineState));
    }

    private void OnStartBurstFire(EventId id, EventInfo ei)
    {
        EventInfo_IIV info = (EventInfo_IIV)ei;

        int playerId = info.int1;

        if (playerId != tankController.data.playerId)
            return;

        if (tankController.IsMine || Settings.GraphicsLevel > GraphicsLevel.lowQuality)
            PlayClip(shotClip);
    }

    private AnimationClip ChooseAnimationClip(EngineState engineState)
    {
        switch (engineState)
        {
            case EngineState.ForwardAcceleration:
                return startClip;
            case EngineState.ForwardBrake:
                return stopClip;
            case EngineState.BackwardAcceleration:
                return stopClip;
            case EngineState.BackwardBrake:
                return startClip;
            default:
                return null;
        }
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
