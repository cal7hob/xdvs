using UnityEngine;
using System;

public class StateEventSender : MonoBehaviour
{
    public event Action<StateEventSender, bool> StateChanged = delegate (StateEventSender btn, bool state) { };

    void OnEnable()
    {
        StateChanged(this, true);
    }

    void OnDisable()
    {
        StateChanged(this, false);
    }
}
