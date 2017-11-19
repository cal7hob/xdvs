using System.Collections.Generic;

using UnityEngine;

public class AudioSourceInsert : MonoBehaviour
{
    public enum Channel
    {
        Master,
        Voice,
        Crash
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

    public static AudioSourceInsert CreateAudioSource(int id, Channel channel, Transform parent, List<AudioSourceInsert> list)
    {
        GameObject newAudio = new GameObject(string.Format("AudioSource_{0} ({1})", id, channel));

        newAudio.transform.parent = parent;

        AudioSourceInsert insert = newAudio.AddComponent<AudioSourceInsert>();
        AudioSource source = newAudio.AddComponent<AudioSource>();

        source.playOnAwake = false;
        source.maxDistance = 65.0f;
        source.dopplerLevel = 0.0f;
        source.spread = 360.0f;

        switch (channel)
        {
            case Channel.Master:
                source.rolloffMode = AudioRolloffMode.Linear;

                if (!XD.StaticContainer.SceneManager.InBattle)
                {
                    source.spatialBlend = 0.0f;
                }
                else
                {
                    source.spatialBlend = 1.0f;

                    if (Settings.GraphicsLevel > GraphicsLevel.mediumQuality)
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

                if (Settings.GraphicsLevel > GraphicsLevel.mediumQuality)
                    source.gameObject.AddComponent<AudioObstacleCutoff>();

                break;
        }

        insert.source = source;
        insert.channel = channel;
        insert.initialParent = newAudio.transform.parent;

        list.Add(insert);

        return insert;
    }

    public void Play()
    {
        source.Play();
        lastPlayed = Time.time;
    }
}
