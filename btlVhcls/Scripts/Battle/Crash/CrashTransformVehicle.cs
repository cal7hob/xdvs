using UnityEngine;

public class CrashTransformVehicle : MonoBehaviour
{
    public Transform turret;

    private int playerId;

    void Awake()
    {
        Dispatcher.Subscribe(EventId.TankLeftTheGame, OnTankLeftTheGame);
    }

    void OnDestroy()
    {
        Dispatcher.Unsubscribe(EventId.TankLeftTheGame, OnTankLeftTheGame);
    }

    public void Init(VehicleController vehicleController)
    {
        playerId = vehicleController.data.playerId;
    }

    private void OnTankLeftTheGame(EventId id, EventInfo ei)
    {
        EventInfo_I info = (EventInfo_I)ei;

        int playerId = info.int1;

        if (playerId == this.playerId)
            Destroy(gameObject);
    }
}
