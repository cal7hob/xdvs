using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class DispatchedAudioSource : MonoBehaviour
{
    private AudioSource audioSource;

    void Awake()
    {
        audioSource = GetComponent<AudioSource>();

        Dispatcher.Subscribe(EventId.SettingsSubmited, OnSettingsSubmited);
        Dispatcher.Subscribe(EventId.BattleSettingsSubmited, OnSettingsSubmited);
        Dispatcher.Subscribe(EventId.SoundVolumeChanged, OnSoundVolumeChanged);
    }

    void Start()
    {
        audioSource.volume = Settings.SoundVolume;
    }

    void OnDestroy()
    {
        Dispatcher.Unsubscribe(EventId.SettingsSubmited, OnSettingsSubmited);
        Dispatcher.Unsubscribe(EventId.BattleSettingsSubmited, OnSettingsSubmited);
        Dispatcher.Unsubscribe(EventId.SoundVolumeChanged, OnSoundVolumeChanged);
    }

    private void OnSettingsSubmited(EventId id, EventInfo ei)
    {
        audioSource.volume = Settings.SoundVolume;
    }

    private void OnSoundVolumeChanged(EventId id, EventInfo ei)
    {
        audioSource.volume = Settings.SoundVolume;
    }
}
