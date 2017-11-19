/*
using System.Collections.Generic;
using UnityEngine;

public class AudioSourceInsert
{
    public AudioSource source;
    public Transform initialParent;
    public AudioPlayer.Channel channel;

    private const float RECENTLY_DURATION = 20.0f;

    private float lastPlayed;

    public bool IsPlayedRecently
    {
        get { return Time.time < (lastPlayed + RECENTLY_DURATION); }
    }

    public bool IsAttachedToSpecificGameObject
    {
        get; private set;
    }

    public void Play()
    {
        source.Play();
        lastPlayed = Time.time;
    }

    public static AudioSourceInsert CreateAudioSource(int id, AudioPlayer.Channel channel, GameObject targetObject, List<AudioSourceInsert> list)
    {
        AudioSourceInsert result = CreateAudioSource(id, channel, false, null, targetObject, list);
        result.IsAttachedToSpecificGameObject = true;
        return result;
    }

    public static AudioSourceInsert CreateAudioSource(int id, AudioPlayer.Channel channel, Transform parent, List<AudioSourceInsert> list)
    {
        return CreateAudioSource(id, channel, true, parent, null, list);
    }
}
*/
