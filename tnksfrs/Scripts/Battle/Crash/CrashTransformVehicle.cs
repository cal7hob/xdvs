using UnityEngine;

public class CrashTransformVehicle : MonoBehaviour
{
    [SerializeField]
    private Transform           turret = null;
    private VehicleController   vehicleController = null;

    public Transform Turret
    {
        get
        {
            return turret;
        }

        set
        {
            turret = value;
        }
    }

    private void Awake()
    {
        Dispatcher.Subscribe(EventId.TankLeftTheGame, OnTankLeftTheGame);
    }

    private void OnDestroy()
    {
        Dispatcher.Unsubscribe(EventId.TankLeftTheGame, OnTankLeftTheGame);
    }

    public void Init(VehicleController vehicleController)
    {
        this.vehicleController = vehicleController;
    }

    public void Crash()
    {
        if (Turret == null)
        {
            return;
        }

        Turret.transform.rotation = vehicleController.Turret.rotation;
    }

    private void OnTankLeftTheGame(EventId id, EventInfo ei)
    {
        EventInfo_I info = (EventInfo_I)ei;

        int playerId = info.int1;

        if (playerId == vehicleController.data.playerId)
        {
            Destroy(gameObject);
        }
    }
}
