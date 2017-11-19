using UnityEngine;

public class CrashTriggerCollision : CrashTriggerBase
{
    public void OnTriggerEnter(Collider collider)
    {
        CallCrash(collider);
    }
}
