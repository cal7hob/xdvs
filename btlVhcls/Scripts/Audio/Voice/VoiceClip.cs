using System;
using UnityEngine;

[Serializable]
public class VoiceClip
{
    public VoiceEventKey eventKey;
    public AudioClip[] variantSounds;
    public float intervalLimitSeconds;
    public int triesLimit;

    [Range(0, 1)]
    public float priority;

    private const float DEFAULT_VOICE_VOLUME_RATIO = 2.0f; // Умножается на Settings.SoundVolume. Но, в любом случае, будет обрезано до 1.

    private float lastPlayedTime;
    private int tries;
    private AudioClip currentSound;

    public bool IsPlaying
    {
        get { return AudioDispatcher.IsPlaying(currentSound); }
    }

    public bool CheckPlaybackAvailability()
    {
        if (Time.time < lastPlayedTime + intervalLimitSeconds)
            return false;

        if (++tries < triesLimit && tries != 1)
            return false;

        if (tries >= triesLimit)
            tries = 0;

        return true;
    }

    public void Play(float volume)
    {
        currentSound = variantSounds.GetRandomItem();

        if (GameData.IsHangarScene)
        {
            AudioDispatcher.PlayClip(currentSound);
        }
        else
        {
            if (BattleCamera.Instance == null)
                return;

            AudioDispatcher.PlayClipAtPosition(
                /* clip:    */  currentSound,
                /* volume:  */  DEFAULT_VOICE_VOLUME_RATIO * volume,
                /* channel: */  AudioSourceInsert.Channel.Voice,
                /* parent:  */  BattleCamera.Instance.transform);
        }

        lastPlayedTime = Time.time;
    }

    public void Stop()
    {
        AudioDispatcher.Stop(currentSound);
    }
}
