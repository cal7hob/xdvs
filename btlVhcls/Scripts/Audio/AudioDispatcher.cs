using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AudioDispatcher : MonoBehaviour
{
    private const int AUDIO_SOURCE_AMOUNT = 5;
    private const float MAX_DISTANCE = 65.0f;
    private const float MAX_DISTANCE_SJ = 200.0f;
    private const float SOURCES_CLEANING_INTERVAL = 10.0f;

    private static AudioDispatcher instance;

    private int lastInsertId;
    private List<AudioSourceInsert> audioSourceInserts;

    public static float MaxDistance
    {
        get
        {
            if (GameData.IsGame(Game.SpaceJet))
                return MAX_DISTANCE_SJ;

            return MAX_DISTANCE;
        }
    }

    void Awake()
    {
        instance = this;

        Dispatcher.Subscribe(EventId.SettingsSubmited, OnSettingsSubmitted);
        Dispatcher.Subscribe(EventId.BattleSettingsSubmited, OnSettingsSubmitted);
        Dispatcher.Subscribe(EventId.BeforeReconnecting, OnBeforeReconnecting);
        Dispatcher.Subscribe(EventId.SoundVolumeChanged, OnSoundVolumeChanged);

        Init();
    }

    void OnDestroy()
    {
        Dispatcher.Unsubscribe(EventId.SettingsSubmited, OnSettingsSubmitted);
        Dispatcher.Unsubscribe(EventId.BattleSettingsSubmited, OnSettingsSubmitted);
        Dispatcher.Unsubscribe(EventId.BeforeReconnecting, OnBeforeReconnecting);
        Dispatcher.Unsubscribe(EventId.SoundVolumeChanged, OnSoundVolumeChanged);
    }

    void Start()
    {
        StartCoroutine(SourcesCleaning());
    }

    /* PUBLIC SECTION */

    public static void PlayClip(AudioClip clip)
    {
        try // Костыль.
        {
            AudioSourceInsert insert = instance.FindFreeInsert(AudioSourceInsert.Channel.Master);

            insert.source.volume = Settings.SoundVolume;
            insert.source.transform.position = instance.transform.position;
            insert.source.clip = clip;
            insert.Play();
        }
        catch (Exception) { }
    }

    public static void PlayClipAtPosition(AudioClip clip, float volume, AudioSourceInsert.Channel channel, Vector3 position, Transform parent = null)
    {
        if (instance == null || Vector3.Distance(Camera.main.transform.position, position) > MaxDistance)
            return;

        AudioSourceInsert insert = instance.FindFreeInsert(channel);

        insert.source.transform.position = position;
        insert.source.clip = clip;
        insert.source.volume = volume;

        if (!insert.IsAttachedToSpecificGameObject)
        {
            if (parent != null)
                insert.source.transform.SetParent(parent);
            else if (insert.source.transform.parent)
                insert.source.transform.SetParent(insert.initialParent);
        }

        insert.Play();
    }

    public static void PlayClipAtPosition(AudioClip clip, Transform parent)
    {
        PlayClipAtPosition(clip, parent.position, parent);
    }

    public static void PlayClipAtPosition(AudioClip clip, Vector3 position, float volume, Transform parent = null)
    {
        PlayClipAtPosition(clip, Settings.SoundVolume * volume, AudioSourceInsert.Channel.Master, position, parent);
    }

    public static void PlayClipAtPosition(AudioClip clip, float volume, Transform parent)
    {
        PlayClipAtPosition(clip, Settings.SoundVolume * volume, AudioSourceInsert.Channel.Master, parent.position, parent);
    }

    public static void PlayClipAtPosition(AudioClip clip, float volume, AudioSourceInsert.Channel channel, Transform parent)
    {
        PlayClipAtPosition(clip, Settings.SoundVolume * volume, channel, parent.position, parent);
    }

    public static void PlayClipAtPosition(AudioClip clip, Vector3 position, Transform parent = null)
    {
        PlayClipAtPosition(clip, Settings.SoundVolume, AudioSourceInsert.Channel.Master, position, parent);
    }

    public static void PlayClipAtPosition(AudioClip clip, AudioSourceInsert.Channel channel, Vector3 position, Transform parent = null)
    {
        PlayClipAtPosition(clip, Settings.SoundVolume, channel, position, parent);
    }

    public static void Stop(AudioClip clip)
    {
        foreach (AudioSourceInsert insert in instance.audioSourceInserts)
            if (insert.source != null && insert.source.clip == clip && insert.source.isPlaying)
                insert.source.Stop();
    }

    public static bool IsPlaying(AudioClip clip)
    {
        foreach (AudioSourceInsert insert in instance.audioSourceInserts)
        {
            if (insert.source == null || insert.source.clip != clip)
                continue;

            if (insert.source.isPlaying)
                return true;
        }

        return false;
    }

    public static AudioSource CreateAudioSource(GameObject targetObject, AudioSourceInsert.Channel channel = AudioSourceInsert.Channel.Master)
    {
        AudioSourceInsert insert
            = AudioSourceInsert.CreateAudioSource(
                id:             instance.lastInsertId++,
                channel:        channel,
                targetObject:   targetObject,
                list:           instance.audioSourceInserts);

        return insert.source;
    }

    /* PRIVATE SECTION */

    private void OnSettingsSubmitted(EventId id, EventInfo ei)
    {
        SetAudioSourcesVolume();
    }

    private void OnBeforeReconnecting(EventId id, EventInfo ei)
    {
        Init();
    }

    private void OnSoundVolumeChanged(EventId id, EventInfo ei)
    {
        SetAudioSourcesVolume();
    }

    private void Init()
    {
        audioSourceInserts = new List<AudioSourceInsert>(AUDIO_SOURCE_AMOUNT);

        for (int i = 0; i < AUDIO_SOURCE_AMOUNT; i++)
            AudioSourceInsert.CreateAudioSource(
                id:         lastInsertId++,
                channel:    AudioSourceInsert.Channel.Master,
                parent:     transform,
                list:       audioSourceInserts);

        SetAudioSourcesVolume();
    }

    private AudioSourceInsert FindFreeInsert(AudioSourceInsert.Channel channel)
    {
        foreach (AudioSourceInsert insert in audioSourceInserts)
        {
            if (insert.channel != channel)
                continue;

            if (insert.source == null)
                continue;

            if (insert.source.isPlaying)
                continue;

            if (insert.IsAttachedToSpecificGameObject)
                continue;

            return insert;
        }

        return AudioSourceInsert.CreateAudioSource(
            id:         lastInsertId++,
            channel:    channel,
            parent:     transform,
            list:       audioSourceInserts);
    }

    private void SetAudioSourcesVolume()
    {
        foreach (var insert in audioSourceInserts)
        {
            if (insert.source != null)
                insert.source.volume = Settings.SoundVolume;
        }
    }

    private IEnumerator SourcesCleaning()
    {
        while (true)
        {
            for (int i = audioSourceInserts.Count - 1; i >= 0; i--)
            {
                if (audioSourceInserts.Count <= AUDIO_SOURCE_AMOUNT)
                    break;

                AudioSourceInsert insert = audioSourceInserts[i];

                if ((!insert.IsPlayedRecently || insert.source == null) && !insert.IsAttachedToSpecificGameObject)
                {
                    if (insert.source != null)
                        Destroy(insert.source.gameObject);

                    audioSourceInserts.RemoveAt(i);
                }
            }

            yield return new WaitForSeconds(SOURCES_CLEANING_INTERVAL);
        }
    } 
}
