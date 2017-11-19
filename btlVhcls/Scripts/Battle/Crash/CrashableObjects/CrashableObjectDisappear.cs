using UnityEngine;

public class CrashableObjectDisappear : CrashableObjectBase
{
    public float soundVolume = 1.0f;
    public AudioClip[] sounds;
    public GameObject[] particlePrefabs;
    public Transform effectPoint;

    protected override void CrashItself(Collider collider)
    {
        transform.gameObject.SetActive(false);

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
    }
}
