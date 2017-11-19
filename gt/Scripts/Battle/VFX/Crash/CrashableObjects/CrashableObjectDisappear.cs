using UnityEngine;

public class CrashableObjectDisappear : CrashableObjectBase
{
    public AudioClip[] sounds;

    protected override void CrashItself()
    {
        base.CrashItself();

        transform.gameObject.SetActive(false);

        if (sounds.Length > 0)
            AudioDispatcher.PlayClipAtPosition(
                clip:       sounds.GetRandomItem(),
                position:   transform.position,
                channel:    AudioSourceInsert.Channel.Crash,
                volume:     Settings.SoundVolume * SOUND_VOLUME_RATIO);
    }
}
