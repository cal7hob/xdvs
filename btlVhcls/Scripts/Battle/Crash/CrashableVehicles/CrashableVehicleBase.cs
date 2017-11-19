using UnityEngine;

public class CrashableVehicleBase : MonoBehaviour
{
    protected VehicleController vehicleController;

    virtual protected void Awake()
    {
        Dispatcher.Subscribe(EventId.TankKilled, OnTankKilled);
        vehicleController = GetComponent<VehicleController>();
    }

    virtual protected void OnDestroy()
    {
        Dispatcher.Unsubscribe(EventId.TankKilled, OnTankKilled);
    }

    virtual protected void OnTankKilled(EventId id, EventInfo ei)
    {
        EventInfo_III info = (EventInfo_III)ei;

        int victimId = info.int1;

        if (vehicleController.data.playerId == victimId && victimId == BattleController.MyPlayerId)
            Dispatcher.Send(EventId.DeathAnimationDone, new EventInfo_SimpleEvent());
    }
}
