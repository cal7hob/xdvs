using System;
using UnityEngine;

public class ScoresItemCollision : MonoBehaviour
{
    public Action<Collider> OnTriggerEnterEvent;

    private void OnTriggerEnter(Collider other)
    {
        //Debug.LogError("OnTriggerEnter: " + other.transform.parent.name);

        if (OnTriggerEnterEvent != null && other != null)
            OnTriggerEnterEvent(other);
    }
}
