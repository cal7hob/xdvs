using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CrashableObjectWithRigidbody : CrashableObjectBase 
{
    public AudioClip[] sounds;

    private const float OBJECT_HIDING_DURATION = 3.0f;

    private Vector3 beforeCrashPosition;
    private Quaternion beforeCrashRotation;
    private Transform beforeCrashParent;
    private new Rigidbody rigidbody;
    private IEnumerator objectDestroyRoutine;

    protected override void CrashItself()
    {
        base.CrashItself();

        DestroyObj();

        rigidbody = rigidbody ?? gameObject.AddComponent<Rigidbody>();
        rigidbody.isKinematic = false;

        if (sounds.Length > 0)
        {
            AudioDispatcher.PlayClipAtPosition(
                clip: sounds.GetRandomItem(),
                position: transform.position,
                channel: AudioSourceInsert.Channel.Crash,
                volume: Settings.SoundVolume * SOUND_VOLUME_RATIO);
        }
    }

    private void DestroyObj()
    {
        if (objectDestroyRoutine != null)
        {
            StopCoroutine(objectDestroyRoutine);
        }

        objectDestroyRoutine = ObjectDestroy();
        StartCoroutine(objectDestroyRoutine);
    }

    private IEnumerator ObjectDestroy()
    {
        yield return new WaitForSeconds(OBJECT_HIDING_DURATION);
        Destroy(gameObject);
    }
}
