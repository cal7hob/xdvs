using System.Collections;
using System.Collections.Generic;
using UnityEngine;
// ReSharper disable PossibleNullReferenceException

public class VisibilityChecker : MonoBehaviour
{
    private Transform cameraTransform;
    private Renderer checkRenderer;
    private bool zoomStatus;

    void Start()
    {
        cameraTransform = Camera.main.transform;
        checkRenderer = GetComponentInChildren<Renderer>();

        Messenger.Subscribe(EventId.ZoomStateChanged, OnZoomStateChanged);
    }

    void OnDestroy()
    {
        Messenger.Unsubscribe(EventId.ZoomStateChanged, OnZoomStateChanged);
    }

    public bool IsWellVisible(float maxDistance, float maxZoomDistance)
    {
        return checkRenderer.isVisible &&
            Vector3.SqrMagnitude(transform.position - cameraTransform.position) <
            (zoomStatus ?
            maxZoomDistance * maxZoomDistance :
            maxDistance * maxDistance);
    }

    private void OnZoomStateChanged(EventId eid, EventInfo ei)
    {
        zoomStatus = (ei as EventInfo_B).bool1;
    }
}
