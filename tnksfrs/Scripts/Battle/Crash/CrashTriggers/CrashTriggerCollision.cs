using UnityEngine;

public class CrashTriggerCollision : CrashTriggerBase
{
    public void OnCollisionEnter(Collision collision) // Если вдруг нет триггера.
    {
        CallCrash(GetComponent<Collider>());
    }

    public void OnTriggerEnter(Collider collider)
    {
        CallCrash(collider);
    }
}
