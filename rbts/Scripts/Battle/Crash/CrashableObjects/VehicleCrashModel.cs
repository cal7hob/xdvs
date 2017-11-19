using Pool;
using UnityEngine;

public class VehicleCrashModel : MonoBehaviour
{
    private int ownerId;

    void Awake()
    {
        gameObject.SetActive(false);
    }

    public void Init(int ownerId)
    {
        this.ownerId = ownerId;
        Messenger.Subscribe(EventId.VehicleRespawned, OnTankRespawned);
        Messenger.Subscribe(EventId.VehicleKilled, OnTankKilled);
        Messenger.Subscribe(EventId.TankLeftTheGame, OnTankLeftTheGame);
    }

    void OnDestroy()
    {
        Messenger.Unsubscribe(EventId.VehicleRespawned, OnTankRespawned);
        Messenger.Unsubscribe(EventId.VehicleKilled, OnTankKilled);
        Messenger.Unsubscribe(EventId.TankLeftTheGame, OnTankLeftTheGame);
    }

    private void OnTankRespawned(EventId eid, EventInfo ei)
    {
        EventInfo_I info = (EventInfo_I) ei;
        if (info.int1 != ownerId)
            return;

        gameObject.SetActive(false);
    }

    private void OnTankKilled(EventId eid, EventInfo ei)
    {
        EventInfo_II info = (EventInfo_II)ei;
        if (info.int1 != ownerId)
            return;

        VehicleController owner = BattleController.allVehicles[ownerId];
        transform.position = owner.transform.position;
        transform.rotation = owner.transform.rotation;
        gameObject.SetActive(true);
    }

    private void OnTankLeftTheGame(EventId eid, EventInfo ei)
    {
        EventInfo_I info = (EventInfo_I)ei;
        if (info.int1 != ownerId)
            return;

        Destroy(gameObject);
    }
}