using System.Collections.Generic;
using UnityEngine;

public class AudioSourceInsert
{
    public enum Channel
    {
        Master,
        Voice,
        Crash,
        SpatialBlendOff
    }

    public AudioSource source;
    public Transform initialParent;
    public Channel channel;

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

    public static AudioSourceInsert CreateAudioSource(int id, Channel channel, GameObject targetObject, List<AudioSourceInsert> list)
    {
        AudioSourceInsert result = CreateAudioSource(id, channel, false, null, targetObject, list);
        result.IsAttachedToSpecificGameObject = true;
        return result;
    }

    public static AudioSourceInsert CreateAudioSource(int id, Channel channel, Transform parent, List<AudioSourceInsert> list)
    {
        return CreateAudioSource(id, channel, true, parent, null, list);
    }

    private static AudioSourceInsert CreateAudioSource(
        int                     id,
        Channel                 channel,
        bool                    useParent,
        Transform               parent,
        GameObject              targetObject,
        List<AudioSourceInsert> list)
    {
        targetObject = targetObject ?? new GameObject(string.Format("AudioSource_{0} ({1})", id, channel));

        if (useParent)
        {
            targetObject.transform.parent = parent;
        }

        AudioSourceInsert insert = new AudioSourceInsert();
        AudioSource source = targetObject.AddComponent<AudioSource>();

        source.playOnAwake = false;
        source.maxDistance = AudioDispatcher.MaxDistance;
        source.dopplerLevel = 0.0f;
        source.spread = 179.0f;

        switch (channel)
        {
            case Channel.Master:
                source.rolloffMode = AudioRolloffMode.Linear;

                if (GameData.IsHangarScene)
                {
                    source.spatialBlend = 0.0f;
                }
                else
                {
                    source.spatialBlend = 1.0f;

                    if (Settings.GraphicsLevel > GraphicsLevel.mediumQuality && !insert.IsAttachedToSpecificGameObject)
                        source.gameObject.AddComponent<AudioObstacleCutoff>();
                }

                break;

            case Channel.Voice:
                source.rolloffMode = AudioRolloffMode.Linear;
                source.spatialBlend = 0.0f;
                break;

            case Channel.Crash:
                source.rolloffMode = AudioRolloffMode.Linear;
                source.spatialBlend = 1.0f;

                if (Settings.GraphicsLevel > GraphicsLevel.mediumQuality && !insert.IsAttachedToSpecificGameObject)
                    source.gameObject.AddComponent<AudioObstacleCutoff>();

                break;

            case Channel.SpatialBlendOff:
                source.rolloffMode = AudioRolloffMode.Linear;
                source.spatialBlend = 0.0f;
                break;
        }

        insert.source = source;
        insert.channel = channel;
        insert.initialParent = targetObject.transform.parent;

        list.Add(insert);

        return insert;
    }
}
