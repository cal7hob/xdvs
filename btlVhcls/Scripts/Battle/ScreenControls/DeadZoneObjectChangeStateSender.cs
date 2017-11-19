using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DeadZoneObjectChangeStateSender : MonoBehaviour, IDeadZone
{
    [SerializeField] private Renderer objRenderer;

	void Awake ()
    {
        if (!objRenderer)
            objRenderer = GetComponent<Renderer>();
	}

    public Rect GetDeadZone()
    {
        if (!objRenderer)
        {
            Debug.LogErrorFormat("Dead zone Renderer is undefined for object {0}", MiscTools.GetFullTransformName(transform));
            return Rect.zero;
        }
            
        return MiscTools.GetScreenRectOfRenderer(objRenderer);
    }

    private void OnEnable()
    {
        Dispatcher.Send(EventId.DeadZoneObjectStateChanged, new EventInfo_U(this, true));
    }

    private void OnDisable()
    {
        Dispatcher.Send(EventId.DeadZoneObjectStateChanged, new EventInfo_U(this, false));
    }
}
