using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Alert : MonoBehaviour
{
    public float TimeToDestroy = 6f;
    public float UpDistanceAfterAnotherAlertDestroy = 50f;
    void Awake()
    {
        Dispatcher.Subscribe(EventId.OnKillAlertDestroy, GoUp);
    }

    void OnDestroy()
    {
        Dispatcher.Unsubscribe(EventId.OnKillAlertDestroy, GoUp);
    }

    void Start()
    {
        Invoke("KillMe", TimeToDestroy);
    }

    void KillMe()
    {
        Dispatcher.Send(EventId.OnKillAlertDestroy, null);
        Destroy(gameObject);
    }

    void GoUp(EventId _id, EventInfo info)
    {
        transform.position = new Vector3(transform.position.x, transform.position.y + UpDistanceAfterAnotherAlertDestroy, transform.position.z);
    }
}