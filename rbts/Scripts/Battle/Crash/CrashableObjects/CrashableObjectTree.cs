using System.Collections;
using Pool;
using UnityEngine;

public class CrashableObjectTree : CrashableObject
{
    public bool leaveStump;
    public Transform stump;

    private const float TRUNK_HIDING_DURATION = 3.0f;

    private Vector3 beforeCrashPosition;
    private Quaternion beforeCrashRotation;
    private Transform beforeCrashParent;
    private new Rigidbody rigidbody;
    private IEnumerator trunkHidingRoutine;

    protected override void CrashItself()
    {
        if (stump != null)
            ShowStump();

        HideTrunk();

        rigidbody = rigidbody ?? gameObject.AddComponent<Rigidbody>();

        rigidbody.isKinematic = false;

        if (sounds.Length > 0)
            AudioDispatcher.PlayClipAtPosition(
                clip:       sounds.GetRandomItem(),
                position:   transform.position,
                channel:    AudioPlayer.Channel.Important,
                loop:       false,
                parent:     null,
                volume:     Settings.SoundVolume * SOUND_VOLUME_RATIO);

        Vector3 effectPosition = transform.position;
        Quaternion effectRotation = Quaternion.identity;

        if (effectPoint != null)
        {
            effectPosition = effectPoint.position;
            effectRotation = effectPoint.rotation;
        }

        if (crashEffect != null)
        {
            PoolManager.GetObject<PoolEffect>(
                crashEffect.GetResourcePath(true),
                effectPosition,
                effectRotation
                );
        }
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

    private IEnumerator TrunkHiding()
    {
        yield return new WaitForSeconds(TRUNK_HIDING_DURATION);
        gameObject.SetActive(false);
    }
}
