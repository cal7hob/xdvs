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
    private bool isPlaying;


    public bool IsPlaying
    {
        get { return isPlaying; }
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
        isPlaying = true;
        currentSound = variantSounds.GetRandomItem();

        if (GameData.IsHangarScene)
        {
            if (currentSound != null)
            {
                AudioDispatcher.PlayClip(currentSound, false, AudioPlayer.Channel.Voice);
            }
        }
        else
        {
            if (BattleCamera.Instance == null)
                return;

            AudioPlayer player = AudioDispatcher.PlayClipAtPosition(
                /* clip:    */  currentSound,
                /* volume:  */  Settings.SoundVolume * DEFAULT_VOICE_VOLUME_RATIO * volume,
                /* channel: */  AudioPlayer.Channel.Voice,
                /* parent:  */  BattleCamera.Instance.transform);

            player.OnStopPlaying += OnPlayerStopsToPlay;
        }

        lastPlayedTime = Time.time;
    }

    private void OnPlayerStopsToPlay(AudioPlayer player)
    {
        isPlaying = false;
    }
}
