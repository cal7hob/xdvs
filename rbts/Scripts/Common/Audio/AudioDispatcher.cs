using System;
using System.Collections;
using System.Collections.Generic;
using Pool;
using UnityEngine;

public class AudioDispatcher : MonoBehaviour
{
	private const int AUDIO_SOURCE_AMOUNT = 10;

	private static AudioDispatcher instance;

    private DPool<AudioPlayer> masterPool;
    private DPool<AudioPlayer> voicePool;
    private DPool<AudioPlayer> importantPool;
    private DPool<AudioPlayer> guiPool;

    void Awake()
	{
		instance = this;

        Messenger.Subscribe(EventId.BeforeReconnecting, OnBeforeReconnecting);

        Init();
	}

    void OnDestroy()
    {
        Messenger.Unsubscribe(EventId.BeforeReconnecting, OnBeforeReconnecting);
    }

	/* PUBLIC SECTION */

    public static AudioPlayer PlayClip(AudioClip clip, bool loop, AudioPlayer.Channel channel = AudioPlayer.Channel.Master)
    {
        if (clip == null)
        {
            Debug.LogError("Trying to play NULL sound");
            return null;
        }

        DPool<AudioPlayer> pool = instance.GetPool(channel);

        Transform sourceTransform = GameData.IsHangarScene ? instance.transform : BattleCamera.Instance.transform;
        AudioPlayer player = pool.GetObject(sourceTransform.position, sourceTransform.rotation, sourceTransform);
        player.transform.position = Camera.main.transform.position;
        player.Play(clip, Settings.SoundVolume, loop);

        return player;
    }

    public static AudioPlayer PlayClipAtPosition(AudioClip clip, float volume, AudioPlayer.Channel channel, Vector3 position, bool loop, Transform parent)
    {
        if (clip == null)
        {
            Debug.LogError("Trying to play NULL sound");
            return null;
        }

        if (Vector3.Distance(Camera.main.transform.position, position) > SoundSettings.MAX_SOUND_DISTANCE &&
            parent == null)
        {
            return null;
        }

        DPool<AudioPlayer> pool = instance.GetPool(channel);
        AudioPlayer audioPlayer = pool.GetObject(position, Quaternion.identity, parent);
        audioPlayer.transform.position = position;
        if (parent != null)
        {
            audioPlayer.transform.SetParent(parent);
        }

        audioPlayer.Play(clip, volume, loop);

        return audioPlayer;
    }

    public static AudioPlayer PlayClipAtPosition(AudioClip clip, Transform parent, bool loop = false)
    {
        return PlayClipAtPosition(clip, Settings.SoundVolume, AudioPlayer.Channel.Master, parent.position, loop, parent);
    }

    public static AudioPlayer PlayClipAtPosition(AudioClip clip, Vector3 position, float volume, Transform parent = null)
    {
        return PlayClipAtPosition(clip, Settings.SoundVolume * volume, AudioPlayer.Channel.Master, position, false, parent);
    }

    public static AudioPlayer PlayClipAtPosition(AudioClip clip, float volume, AudioPlayer.Channel channel, Transform parent, bool loop = false)
    {
        return PlayClipAtPosition(clip, Settings.SoundVolume * volume, channel, parent.position, loop, parent);
    }

    public static AudioPlayer PlayClipAtPosition(AudioClip clip, Vector3 position, Transform parent = null, bool loop = false)
    {
        return PlayClipAtPosition(clip, Settings.SoundVolume, AudioPlayer.Channel.Master, position, loop, parent);
    }

    /* PRIVATE SECTION */

    private void OnBeforeReconnecting(EventId id, EventInfo ei)
    {
        Init();
    }

    private void Init()
	{
        masterPool = new DPool<AudioPlayer>("AudioSystem/AudioPlayer_Master", AUDIO_SOURCE_AMOUNT);
        voicePool = new DPool<AudioPlayer>("AudioSystem/AudioPlayer_Voice", 1);
        guiPool = new DPool<AudioPlayer>("AudioSystem/AudioPlayer_SBOff", 2);
        importantPool = new DPool<AudioPlayer>("AudioSystem/AudioPlayer_Crash");
    }

    private DPool<AudioPlayer> GetPool(AudioPlayer.Channel channel)
    {
        switch (channel)
        {
            case AudioPlayer.Channel.Master:
                return masterPool;
            case AudioPlayer.Channel.Important:
                return importantPool;
            case AudioPlayer.Channel.GUI:
                return guiPool;
            case AudioPlayer.Channel.Voice:
                return voicePool;
            default:
                return null;
        }
    }
}
