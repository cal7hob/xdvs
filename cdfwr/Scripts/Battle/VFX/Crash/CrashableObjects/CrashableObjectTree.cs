using System.Collections;
using UnityEngine;

public class CrashableObjectTree : CrashableObjectBase
{
    public bool leaveStump;
    public Transform stump;
    public AudioClip[] sounds;

    private const float TRUNK_HIDING_DURATION = 3.0f;

   // private Vector3 beforeCrashPosition;
  //  private Quaternion beforeCrashRotation;
 //   private Transform beforeCrashParent;
    private new Rigidbody rigidbody;
    private IEnumerator trunkDestroyRoutine;

    protected override void CrashItself()
    {
        if (stump != null)
        {
            ShowStump();
        }

        DestroyTrunk();

        rigidbody = rigidbody ?? gameObject.AddComponent<Rigidbody>();
        rigidbody.isKinematic = false;

        if (sounds.Length > 0)
        {
            AudioDispatcher.PlayClipAtPosition(
                clip:       sounds.GetRandomItem(),
                position:   transform.position,
                channel:    AudioSourceInsert.Channel.Crash,
                volume:     Settings.SoundVolume * SOUND_VOLUME_RATIO);
		}
    }

    private void ShowStump()
    {
     //   beforeCrashPosition = stump.position;
     //   beforeCrashRotation = stump.rotation;
     //   beforeCrashParent = stump.parent;

        stump.SetParent(transform.parent);
        stump.gameObject.SetActive(true);
    }

    private void DestroyTrunk()
    {
        if (trunkDestroyRoutine != null)
        {
            StopCoroutine(trunkDestroyRoutine);
        }

        trunkDestroyRoutine = TrunkDestroy();
        StartCoroutine(trunkDestroyRoutine);
    }

    private IEnumerator TrunkDestroy()
    {
        yield return new WaitForSeconds(Time.deltaTime*5);
        Collider[] colls = GetComponentsInChildren<Collider>();
        foreach (Collider col in colls)
        {
            col.gameObject.layer = LayerMask.NameToLayer("Trees");
        }
        yield return new WaitForSeconds(TRUNK_HIDING_DURATION);
        gameObject.SetActive(false);
    }
}
