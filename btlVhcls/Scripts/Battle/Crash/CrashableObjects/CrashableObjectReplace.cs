using UnityEngine;

public class CrashableObjectReplace : CrashableObjectBase
{
    public Transform crashObject;
    public float soundVolume = 1.0f;
    public AudioClip[] sounds;
    public GameObject[] particlePrefabs;
    public Transform effectPoint;

    private Vector3 beforeCrashPosition;
    private Quaternion beforeCrashRotation;
    private Transform beforeCrashParent;

    protected override void CrashItself(Collider collider)
    {
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
                volume:     Settings.SoundVolume * SOUND_VOLUME_RATIO * soundVolume);

        Vector3 effectPosition = transform.position;
        Quaternion effectRotation = Quaternion.identity;

        if (effectPoint != null)
        {
            effectPosition = effectPoint.position;
            effectRotation = effectPoint.rotation;
        }

        foreach (GameObject particlePrefab in particlePrefabs)
        {
            if (particlePrefab == null)
            {
                Debug.LogWarning("Missing effect prefab reference!");
                continue;
            }

            EffectPoolDispatcher.GetFromPool(
                _effect:    particlePrefab,
                _position:  effectPosition,
                _rotation:  effectRotation);
        }
    }

    protected override void RestoreItself()
    {
        transform.gameObject.SetActive(true);
        crashObject.gameObject.SetActive(false);

        crashObject.SetParent(beforeCrashParent);

        crashObject.localPosition = beforeCrashPosition;
        crashObject.localRotation = beforeCrashRotation;
    }
}
