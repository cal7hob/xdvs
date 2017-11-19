using UnityEngine;

public class CrashableObjectReplaceRandom : CrashableObjectBase
{
    public Transform[] crashObjects;
    public float soundVolume = 1.0f;
    public AudioClip[] sounds;
    public GameObject[] particlePrefabs;
    public Transform effectPoint;

    [Range(0, 360)]
    public float maxRandomRotationAngle;

    private Vector3 beforeCrashPosition;
    private Quaternion beforeCrashRotation;
    private Transform beforeCrashParent;
    private Transform crashObject;

    protected override void CrashItself(Collider collider)
    {
        crashObject = crashObjects.GetRandomItem();

        beforeCrashPosition = crashObject.position;
        beforeCrashRotation = crashObject.rotation;
        beforeCrashParent = crashObject.parent;

        crashObject.SetParent(transform.parent);

        transform.gameObject.SetActive(false);
        crashObject.gameObject.SetActive(true);

		crashObject.localRotation = Quaternion.Euler(0, Random.Range(0, maxRandomRotationAngle), 0);

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
