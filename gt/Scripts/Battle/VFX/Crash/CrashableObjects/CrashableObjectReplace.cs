using UnityEngine;

public class CrashableObjectReplace : CrashableObjectBase
{
    public Transform crashObject;
    public AudioClip[] sounds;

    private Vector3 beforeCrashPosition;
    private Quaternion beforeCrashRotation;
    private Transform beforeCrashParent;

    protected override void CrashItself()
    {
        base.CrashItself();

        beforeCrashPosition = crashObject.position;
        beforeCrashRotation = crashObject.rotation;
        beforeCrashParent = crashObject.parent;

        crashObject.SetParent(transform.parent);

        transform.gameObject.SetActive(false);
        crashObject.gameObject.SetActive(true);

        if (sounds.Length > 0)
            AudioDispatcher.PlayClipAtPosition(
                clip:       sounds.GetRandomItem(),
                position:   transform.position,
                channel:    AudioSourceInsert.Channel.Crash,
                volume:     Settings.SoundVolume * SOUND_VOLUME_RATIO);        
    }
}
