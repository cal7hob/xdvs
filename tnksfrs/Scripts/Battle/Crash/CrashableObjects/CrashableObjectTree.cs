using System.Collections;
using UnityEngine;

public class CrashableObjectTree : CrashableObjectBase
{
    [SerializeField]
    private bool                particlesFromPrefab = false;
    [SerializeField]
    private Transform           stump = null;
    [SerializeField]
    private GameObject[]        particlePrefabs = null;
    [SerializeField]
    private ParticleSystem[]    particles = null;
    [SerializeField]
    private Transform           effectPoint = null;

    [SerializeField]
    private float               fallStartTime = 5f;
    [SerializeField]
    private float               angleMax = 90f;
    [SerializeField]
    private float               accelerate = 2f;
    
    private const float         TRUNK_HIDING_DURATION = 5.0f;

    private Vector3             beforeCrashPosition = new Vector3();
    private Quaternion          beforeCrashRotation = new Quaternion();
    private Transform           beforeCrashParent = null;
    private IEnumerator         trunkHidingRoutine = null;

    private Collider[] colliders = null;

    private Collider[] Colliders
    {
        get
        {
            if (colliders == null)
            {
                colliders = GetComponentsInChildren<Collider>();
            }

            return colliders;
        }
    }

    private IEnumerator Fall(Vector3 point)
    {
        Vector3 direction = transform.position - point;
        if (direction == Vector3.zero)
        {
            direction = Random.insideUnitSphere;
        }

        for (int i = 0; i < Colliders.Length; i++)
        {
            Colliders[i].isTrigger = true;
        }

        direction.Normalize();
        direction = Quaternion.Euler(0, 90, 0) * direction;
        yield return StartCoroutine(Rotate(angleMax, direction, fallStartTime, accelerate));

        float upRandom = Random.Range(7f, 13f);
        yield return StartCoroutine(Rotate(upRandom, -direction, 0.1f, -accelerate));
        yield return StartCoroutine(Rotate(upRandom, direction, fallStartTime / 3, accelerate));
    }

    private IEnumerator Rotate(float angle, Vector3 direction, float time, float accelerate)
    {
        Quaternion rotation = transform.rotation;

        while (Quaternion.Angle(transform.rotation, rotation) < angle)
        {
            time -= Time.deltaTime * accelerate;
            transform.Rotate(direction, Time.deltaTime * angle / time);
            yield return null;
        }
    }

    protected override void CrashItself(Collider collider)
    {
        if (stump != null)
        {
            ShowStump();
        }

        HideTrunk();

        StartCoroutine(Fall(collider == null ? transform.position : collider.transform.position));

        //rigidbody = rigidbody ?? gameObject.AddComponent<Rigidbody>();
        //
        //rigidbody.isKinematic = false;

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
        if (stump != null)
        {
            HideStump();
        }

        ShowTrunk();

        for (int i = 0; i < Colliders.Length; i++)
        {
            Colliders[i].isTrigger = false;
        }
        //rigidbody.isKinematic = false;
    }

    private void ShowStump()
    {
        beforeCrashPosition = stump.position;
        beforeCrashRotation = stump.rotation;
        beforeCrashParent = stump.parent;

        stump.SetParent(transform.parent);

        stump.gameObject.SetActive(true);
    }

    private void HideStump()
    {
        stump.gameObject.SetActive(false);

        stump.SetParent(beforeCrashParent);

        stump.localPosition = beforeCrashPosition;
        stump.localRotation = beforeCrashRotation;
    }

    private void ShowTrunk()
    {
        gameObject.SetActive(true);
    }

    private void HideTrunk()
    {
        if (trunkHidingRoutine != null)
            StopCoroutine(trunkHidingRoutine);

        trunkHidingRoutine = TrunkHiding();

        StartCoroutine(trunkHidingRoutine);
    }

    private IEnumerator TrunkHiding()
    {
        yield return new WaitForSeconds(TRUNK_HIDING_DURATION);
        gameObject.SetActive(false);
    }
}
