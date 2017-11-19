using UnityEngine;

public class CrashableObjectDisappear : CrashableObjectBase
{
    public bool particlesFromPrefab;
    public GameObject[] particlePrefabs;
    public ParticleSystem[] particles;
    public Transform effectPoint;

    protected override void CrashItself(Collider collider)
    {
        transform.gameObject.SetActive(false);

        PlaySounds();

        if (particlesFromPrefab)
        {
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
        else
        {
            foreach (ParticleSystem particle in particles)
            {
                if (particle == null)
                {
                    Debug.LogWarning("Missing effect instance reference!");
                    continue;
                }

                particle.Play();
            }
        }
    }

    protected override void RestoreItself()
    {
        transform.gameObject.SetActive(true);
    }
}
