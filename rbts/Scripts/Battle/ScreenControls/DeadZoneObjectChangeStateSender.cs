using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DeadZoneObjectChangeStateSender : MonoBehaviour, IDeadZone
{
    [SerializeField] private tk2dUILayout deadZone;

    private bool state;
    public bool State
    {
        get
        {
            return state;
        }

        set
        {
            if (state != value)
                state = value;

            Messenger.Send(EventId.DeadZoneObjectStateChanged, new EventInfo_U(this, state));
        }
    }

	void Awake ()
    {
        if (!deadZone)
            deadZone = GetComponent<tk2dUILayout>();
	}

    public Rect GetDeadZone()
    {
        if (!deadZone)
        {
            Debug.LogErrorFormat("Dead zone tk2dUILayout is undefined for object {0}",
                MiscTools.GetFullTransformName(transform));

            return Rect.zero;
        }

        return deadZone.GetRect();
    }

    private void OnEnable()
    {
        State = true;
    }

    private void OnDisable()
    {
        State = false;
    }
}
