using UnityEngine;
using XDevs.LiteralKeys;

public class SkidmarksPoint : MonoBehaviour
{
    private const float DISTANCE_THRESHOLD = 0.45f;

    private VehicleController vehicleController;
    private Skidmarks skidmarks;
    private int lastSkidmarkId = Skidmarks.DEFAULT_SKIDMARK_ID;
    private RaycastHit hit;

    void Awake()
    {
        vehicleController = GetComponentInParent<VehicleController>();
        skidmarks = FindObjectOfType<Skidmarks>();

        Dispatcher.Subscribe(EventId.TankKilled, OnTankKilled);
        Dispatcher.Subscribe(EventId.TankRespawned, OnTankRespawned);
    }

    void OnDestroy()
    {
        Dispatcher.Unsubscribe(EventId.TankKilled, OnTankKilled);
        Dispatcher.Unsubscribe(EventId.TankRespawned, OnTankRespawned);
    }

    private void OnTankKilled(EventId id, EventInfo ei)
    {
        EventInfo_III info = (EventInfo_III)ei;

        int playerId = info.int1;

        if (playerId == vehicleController.data.playerId)
            Chop();
    }

    private void OnTankRespawned(EventId id, EventInfo ei)
    {
        EventInfo_I info = (EventInfo_I)ei;

        int playerId = info.int1;

        if (playerId == vehicleController.data.playerId)
            Chop();
    }

    public void Chop()
    {
        lastSkidmarkId = Skidmarks.DEFAULT_SKIDMARK_ID;
    }

    public void Draw()
    {
        if (Settings.GraphicsLevel < GraphicsLevel.normalQuality)
            return;

        lastSkidmarkId
            = skidmarks.AddSkidMark(
                position:   hit.point + (hit.normal * 0.01f),
                normal:     hit.normal,
                intensity:  1,
                lastIndex:  lastSkidmarkId);
    }

    public bool CheckGroundContact()
    {
        if (!Physics.Raycast(
            /* ray:         */  new Ray(transform.position + (Vector3.up * 0.1f), -transform.up),
            /* hit          */  out hit,
            /* maxDistance: */  DISTANCE_THRESHOLD,
            /* layerMask:   */  MiscTools.GetLayerMask(Layer.Key.Terrain)))
        {
            return false;
        }

        return true;
    }
}
