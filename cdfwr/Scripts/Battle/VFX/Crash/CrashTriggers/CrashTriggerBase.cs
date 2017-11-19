using UnityEngine;

public abstract class CrashTriggerBase : MonoBehaviour
{
    public CrashableObjectBase[] crashableObjects;

    public void CallCrash(Collider coll)
    {
        foreach (CrashableObjectBase crashableObject in crashableObjects)
        {
            if (crashableObject == null)
            {
                Debug.LogWarning("Missed reference! CrashableObjectBase not found.", gameObject);
                continue;
            }
            crashableObject.Crash(coll);
        }
    }
    public void CallCrash(Collision coll)
    {
        CallCrash(coll.collider);
    }

    public void CallCrash()
    {
        foreach (CrashableObjectBase crashableObject in crashableObjects)
        {
            if (crashableObject == null)
            {
                Debug.LogWarning("Missed reference! CrashableObjectBase not found.", gameObject);
                continue;
            }
            crashableObject.Crash(null);
        }
    }
}
