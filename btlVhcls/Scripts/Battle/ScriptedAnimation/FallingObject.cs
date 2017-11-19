using System;
using UnityEngine;

[Obsolete("Используй CrashableObjectTree")]
public class FallingObject : MonoBehaviour // TODO: удалить.
{
    private const int RELATIVE_VELOCITY_THRESHOLD = 5;
    private const float DEFAULT_MASS = 300.0f;

    void Start()
    {
        GetComponent<Rigidbody>().isKinematic = true;
        GetComponent<Rigidbody>().mass = DEFAULT_MASS;
    }

    void OnCollisionEnter(Collision collision)
    {
        if (collision.relativeVelocity.magnitude > RELATIVE_VELOCITY_THRESHOLD)
            GetComponent<Rigidbody>().isKinematic = false;
    }
}
