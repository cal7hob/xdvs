using UnityEngine;

public abstract class CrashTriggerBase : MonoBehaviour
{
    public CrashableObjectBase[] crashableObjects;

    public void CallCrash(Collider collider)
    {
        foreach (CrashableObjectBase crashableObject in crashableObjects)
        {
            if (crashableObject == null)
            {
                Debug.LogWarning("Missed reference! CrashableObjectBase not found.", gameObject);
                continue;
            }

            crashableObject.Crash(collider);
        }
    }
}
