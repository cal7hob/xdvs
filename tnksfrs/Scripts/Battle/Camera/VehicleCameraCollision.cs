using UnityEngine;

public class VehicleCameraCollision : MonoBehaviour
{
    private VehicleController myVehicle;

    void Awake()
    {
        Dispatcher.Subscribe(EventId.MainTankAppeared, delegate(EventId id, EventInfo info)
        {
            myVehicle = XD.StaticContainer.BattleController.CurrentUnit;
        });
    }
}
