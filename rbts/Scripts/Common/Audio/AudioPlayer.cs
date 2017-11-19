using System.Collections;
using System.Collections.Generic;
using Pool;
using UnityEngine;

public class AudioPlayer : PoolObject
{
    public enum Channel
    {
        Master,
        /// <summary>
        /// Голос, максимум 1 одновременно
        /// </summary>
        Voice,
        /// <summary>
        /// Обязательно звучащие звуки с неотбираемыми плейерами
        /// </summary>
        Important,
        /// <summary>
        /// Звуки без 3D-эффекта
        /// </summary>
        GUI
    }

    [SerializeField] private Channel channel = Channel.Master;
    private AudioSource audioSource;
    private float lastSettingsVolume;
    public event System.Action<AudioPlayer> OnStopPlaying;
    

    private void Awake()
    {
        audioSource = GetComponent<AudioSource>() ?? gameObject.AddComponent<AudioSource>();
        ConfigureAudioSource(audioSource, channel);
        Messenger.Subscribe(EventId.SoundVolumeChanged, OnSoundVolumeChanged);
    }

    private void Update()
    {
        if (audioSource.isPlaying)
            return;

        ReturnObject();
    }

    private void OnDestroy()
    {
        Messenger.Unsubscribe(EventId.SoundVolumeChanged, OnSoundVolumeChanged);
    }

    public override void OnGetFromPool()
    {
        gameObject.SetActive(true);
    }

    public void Play(AudioClip clip, float volume, bool loop)
    {
        audioSource.clip = clip;
        audioSource.loop = loop;
        audioSource.volume = volume;
        lastSettingsVolume = Settings.SoundVolume;

        audioSource.Play();
    }

    public void Stop()
    {
        ReturnObject();
    }

    public AudioClip Clip
    {
        get { return audioSource.clip; }
    }

    public override float StealPriority
    {
        get { return audioSource.time / audioSource.clip.length; }
    }

    public override void ReturnObject()
    {
        audioSource.Stop();
        audioSource.clip = null;
        if (OnStopPlaying != null)
        {
            OnStopPlaying.Invoke(this);
            OnStopPlaying = null;
        }

        base.ReturnObject();
    }

    private void OnSoundVolumeChanged(EventId eid, EventInfo ei)
    {
        audioSource.volume *= Settings.SoundVolume / lastSettingsVolume;
        lastSettingsVolume = Settings.SoundVolume;
    }

    private static void ConfigureAudioSource(AudioSource audioSource, Channel channel)
    {
        audioSource.playOnAwake = false;
        audioSource.maxDistance = SoundSettings.MAX_SOUND_DISTANCE;
        audioSource.dopplerLevel = 0.0f;
        audioSource.spread = 179.0f;
        audioSource.volume = Settings.SoundVolume;

        switch (channel)
        {
            case Channel.Master:
                audioSource.spatialBlend = GameData.IsHangarScene ? 0.0f : 1.0f;
                break;

            case Channel.Voice:
                audioSource.spatialBlend = 0.0f;
                break;

            case Channel.Important:
                audioSource.spatialBlend = 1.0f;
                break;

            case Channel.GUI:
                audioSource.spatialBlend = 0.0f;
                break;
        }
    }
}
