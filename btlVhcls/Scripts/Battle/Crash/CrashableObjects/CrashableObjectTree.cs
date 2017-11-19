using System.Collections;
using UnityEngine;
using Random = UnityEngine.Random;

public class CrashableObjectTree : CrashableObjectBase
{
    public bool leaveStump;
    public Transform stump;
    public float soundVolume = 1.0f;
    public AudioClip[] sounds;
    public GameObject[] particlePrefabs;
    public Transform effectPoint;
    public GameObject[] objectsToHide;
    public MonoBehaviour[] componentsToActivate;

    private const float TRUNK_HIDING_DURATION = 5.0f;
    private const float ADDITIONAL_FORCE_AMOUNT_MIN = 75.0f;
    private const float ADDITIONAL_FORCE_AMOUNT_MAX = 200.0f;

    private Vector3 beforeCrashPosition;
    private Quaternion beforeCrashRotation;
    private Transform beforeCrashParent;
    private new Rigidbody rigidbody;
    private new Renderer renderer;
    private IEnumerator trunkHidingRoutine;

    void Awake()
    {
        renderer = transform.GetComponentInChildren<Renderer>();
    }

    protected override void CrashItself(Collider collider)
    {
        if (stump != null)
            ShowStump();

        HideTrunk();

        rigidbody = rigidbody ?? gameObject.AddComponent<Rigidbody>();

        rigidbody.isKinematic = false;
        rigidbody.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

        AddForce(collider != null ? collider.transform.position : Random.onUnitSphere);

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

        foreach (GameObject objectToHide in objectsToHide)
            objectToHide.SetActive(false);

        foreach (MonoBehaviour componentToActivate in componentsToActivate)
            componentToActivate.enabled = true;
    }

    protected override void RestoreItself()
    {
        if (stump != null)
            HideStump();

        ShowTrunk();

        rigidbody.isKinematic = false;
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

    private void AddForce(Vector3 colliderPosition)
    {
        Vector3 position = renderer.bounds.max;
        Vector3 direction = (transform.position - colliderPosition).normalized;

        direction = direction.GetHorizontalIdentity();
        direction *= Random.Range(ADDITIONAL_FORCE_AMOUNT_MIN, ADDITIONAL_FORCE_AMOUNT_MAX);

        rigidbody.AddForceAtPosition(direction, position);
    }

    private IEnumerator TrunkHiding()
    {
        yield return new WaitForSeconds(TRUNK_HIDING_DURATION);
        gameObject.SetActive(false);
    }
}
