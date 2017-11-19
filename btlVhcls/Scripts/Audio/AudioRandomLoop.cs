using System.Collections;
using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class AudioRandomLoop : MonoBehaviour
{
	public AudioClip[] clips;

    [Header("Минимальная задержка")]
    public float minDelay = 1.0f;

    [Header("Максимальная задержка")]
    public float maxDelay = 2.0f;
    
    [Space]
    public float minPitch = 0.95f;
    public float maxPitch = 1.05f;

    [Range(0.0f, 1.0f)]
    public float spatialBlend;

    [Header("Луп первого звука")]
    public bool loop;

    private AudioSource audioSource;

    void Awake()
    {
        audioSource = GetComponent<AudioSource>();

        audioSource.volume = Settings.SoundVolume;

        Dispatcher.Subscribe(EventId.SettingsSubmited, OnSettingsSubmited);
        Dispatcher.Subscribe(EventId.BattleSettingsSubmited, OnSettingsSubmited);
        Dispatcher.Subscribe(EventId.SoundVolumeChanged, OnSoundVolumeChanged);
    }

    void OnDestroy()
    {
        Dispatcher.Unsubscribe(EventId.SettingsSubmited, OnSettingsSubmited);
        Dispatcher.Unsubscribe(EventId.BattleSettingsSubmited, OnSettingsSubmited);
        Dispatcher.Unsubscribe(EventId.SoundVolumeChanged, OnSoundVolumeChanged);
    }

    private void Start()
    {
        audioSource.spatialBlend = spatialBlend;
        StartCoroutine(LoopPlaying());
    }

    private void OnSettingsSubmited(EventId id, EventInfo ei)
    {
        audioSource.volume = Settings.SoundVolume;
    }

    private void OnSoundVolumeChanged(EventId id, EventInfo ei)
    {
        audioSource.volume = Settings.SoundVolume;
    }

    /// <summary>
    /// Воспроизведение случайного клипа из списка.
    /// </summary>
    private void Play()
    {
        int randomClip = Random.Range(0, clips.Length);
        float randomPitch = Random.Range(minPitch, maxPitch);

        audioSource.clip = clips[randomClip];
        audioSource.pitch = randomPitch;
        audioSource.loop = loop;

        audioSource.Play();
    }

    /// <summary>
    /// Корутина для зацикленного проигрывания рандомных звуков.
    /// </summary>
    private IEnumerator LoopPlaying()
    {
        while (true)
        {
            yield return null;

            if (audioSource.isPlaying)
                continue;

            yield return new WaitForSeconds(Random.Range(minDelay, maxDelay));

            Play();

            if (loop)
                yield break;
        }
    }
}
