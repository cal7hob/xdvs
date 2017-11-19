using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class AudioFXVolume : MonoBehaviour
{
    private AudioSource audioSource;
    private float startVolume;

    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        startVolume = audioSource.volume;

        Messenger.Subscribe(EventId.ProfileInfoLoadedFromServer, OnSettingsMayChange);
        Messenger.Subscribe(EventId.SoundVolumeChanged, OnSettingsMayChange);
    }

    private void OnDestroy()
    {
        Messenger.Unsubscribe(EventId.ProfileInfoLoadedFromServer, OnSettingsMayChange);
        Messenger.Unsubscribe(EventId.SoundVolumeChanged, OnSettingsMayChange);
    }

    private void SetVolume()
    {
        audioSource.volume = Settings.SoundVolume * startVolume;
    }

    private void OnSettingsMayChange(EventId eid, EventInfo ei)
    {
        SetVolume();
    }
}
