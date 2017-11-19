using UnityEngine;

public class SkidmarksPoint : MonoBehaviour
{
    private VehicleController vehicleController;
    private Skidmarks skidmarks;
    private int lastSkidmarkId = Skidmarks.DEFAULT_SKIDMARK_ID;

    void Awake()
    {
        vehicleController = GetComponentInParent<VehicleController>();
        skidmarks = FindObjectOfType<Skidmarks>();

        Messenger.Subscribe(EventId.VehicleKilled, OnTankKilled);
    }

    void OnDestroy()
    {
        Messenger.Unsubscribe(EventId.VehicleKilled, OnTankKilled);
    }

    private void OnTankKilled(EventId id, EventInfo ei)
    {
        EventInfo_II info = (EventInfo_II)ei;

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
        if (Settings.GraphicsLevel < GraphicsLevel.NormalQuality)
            return;

        lastSkidmarkId
            = skidmarks.AddSkidMark(
                position:   transform.position,
                normal:     transform.up,
                intensity:  1,
                lastIndex:  lastSkidmarkId);
    }
}
