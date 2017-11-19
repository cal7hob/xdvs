using UnityEngine;

[RequireComponent(typeof(LODGroup))]
public class LodGroupManager : MonoBehaviour
{
    [SerializeField] private int zoomForcedLOD = 0;

    private LODGroup lodGroup;

    void Awake()
    {
        lodGroup = GetComponent<LODGroup>();
        if (lodGroup == null)
        {
            Debug.LogErrorFormat(gameObject, "There is no LodGroup for LodGroupManager in gameobject {0}", name);
            return;
        }

        Subscriptions();
    }

    void OnDestroy()
    {
        Unsubscriptions();
    }

    private void Subscriptions()
    {
        Messenger.Subscribe(EventId.ZoomStateChanged, OnZoomStateChanged);
    }

    private void Unsubscriptions()
    {
        Messenger.Unsubscribe(EventId.ZoomStateChanged, OnZoomStateChanged);
    }

    private void OnZoomStateChanged(EventId eid, EventInfo ei)
    {
        EventInfo_B info = ei as EventInfo_B;
        bool zoomed = info.bool1;

        lodGroup.ForceLOD(zoomed ? zoomForcedLOD : -1);
    }
}
