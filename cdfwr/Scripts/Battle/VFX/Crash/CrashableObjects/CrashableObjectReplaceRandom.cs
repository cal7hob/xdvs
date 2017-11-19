using UnityEngine;

public class CrashableObjectReplaceRandom : CrashableObjectBase
{
    public Transform[] crashObjects;
    public AudioClip[] sounds;

    [Range(0, 360)]
    public float maxRandomRotationAngle;

    private Vector3 beforeCrashPosition;
    private Quaternion beforeCrashRotation;
    private Transform beforeCrashParent;
    private Transform crashObject;

    protected override void CrashItself()
    {
        base.CrashItself();

        crashObject = crashObjects.GetRandomItem();

        beforeCrashPosition = crashObject.position;
        beforeCrashRotation = crashObject.rotation;
        beforeCrashParent = crashObject.parent;

        crashObject.SetParent(transform.parent);

        transform.gameObject.SetActive(false);
        crashObject.gameObject.SetActive(true);

		crashObject.localRotation = Quaternion.Euler(0, Random.Range(0, maxRandomRotationAngle), 0);

        if (sounds.Length > 0)
        {
            AudioDispatcher.PlayClipAtPosition(
                clip: sounds.GetRandomItem(),
                position: transform.position,
                channel: AudioSourceInsert.Channel.Crash,
                volume: Settings.SoundVolume * SOUND_VOLUME_RATIO);
        }
    }
}
