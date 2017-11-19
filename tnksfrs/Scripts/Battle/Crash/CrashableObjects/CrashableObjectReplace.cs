using UnityEngine;
using XD;

public class CrashableObjectReplace : CrashableObjectBase
{
    [Space]
    [SerializeField]
    private Transform           effectPoint = null;
    [SerializeField]
    private bool                particlesFromPrefab;
    [SerializeField]
    private Transform           crashObject = null;
    [SerializeField]
    private GameObject[]        particlePrefabs = null;
    [SerializeField]
    private ParticleSystem[]    particles = null;    

    private Vector3             beforeCrashPosition = new Vector3();
    private Quaternion          beforeCrashRotation = new Quaternion();
    private Transform           beforeCrashParent = null;
    

    protected override void CrashItself(Collider collider)
    {        
        beforeCrashPosition = crashObject.position;
        beforeCrashRotation = crashObject.rotation;
        beforeCrashParent = crashObject.parent;

        crashObject.SetParent(transform.parent);

        transform.gameObject.SetActive(false);
        crashObject.gameObject.SetActive(true);

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
        base.CrashItself(collider);
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
