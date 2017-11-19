using UnityEngine;

public class VehicleCameraCollision : MonoBehaviour
{
    private VehicleController myVehicle;

    void Awake()
    {
        Messenger.Subscribe(EventId.MainTankAppeared, delegate(EventId id, EventInfo info)
        {
            myVehicle = BattleController.MyVehicle;
        });
    }

    void OnTriggerEnter(Collider col)
    {
        if (col.transform.parent == myVehicle.transform || col.transform.parent == myVehicle.Turret)
            myVehicle.IsVisible = false;
    }

    void OnTriggerExit(Collider col)
    {
        if (col.transform.parent == myVehicle.transform || col.transform.parent == myVehicle.Turret)
            myVehicle.IsVisible = true;
    }
}
