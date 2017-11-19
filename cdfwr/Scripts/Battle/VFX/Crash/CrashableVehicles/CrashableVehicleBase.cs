using UnityEngine;

public class CrashableVehicleBase : MonoBehaviour
{

    protected virtual void Awake()
    {
        Dispatcher.Subscribe(EventId.TankKilled, OnTankKilled);
    }

    protected virtual void OnDestroy()
    {
        Dispatcher.Unsubscribe(EventId.TankKilled, OnTankKilled);
    }

    protected virtual void OnTankKilled(EventId id, EventInfo ei)
    {
        EventInfo_II eventInfo = (EventInfo_II)ei;
        var victimId = eventInfo.int1;

        if (victimId == BattleController.MyPlayerId)
        {
            Dispatcher.Send(EventId.DeathAnimationDone, new EventInfo_SimpleEvent());
        }
    }
}
