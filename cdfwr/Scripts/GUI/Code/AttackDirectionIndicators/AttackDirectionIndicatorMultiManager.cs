using UnityEngine;

public class AttackDirectionIndicatorMultiManager : MonoBehaviour
{
    public AttackDirectionIndicatorMulti[] indicators;

    void Awake()
    {
        Dispatcher.Subscribe(EventId.TankTakesDamage, DetermineAtackDirectrion);
    }

    void OnDestroy()
    {
        Dispatcher.Unsubscribe(EventId.TankTakesDamage, DetermineAtackDirectrion);
    }

    private void DetermineAtackDirectrion(EventId id, EventInfo ei)
    {
        if (BattleController.MyVehicle == null)
            return;

        EventInfo_U info = (EventInfo_U)ei;

        int victimId = (int)info[0];

        if (victimId != BattleController.MyVehicle.data.playerId)
            return;

        int damage = (int)info[1];
        int attackerId = (int)info[2];

        VehicleController attacker;

        if (!BattleController.allVehicles.TryGetValue(attackerId, out attacker))
            return;

        foreach (AttackDirectionIndicatorMulti indicator in indicators)
        {
            if (!indicator.CheckAvailability(attacker))
                continue;

            indicator.Indicate(attacker, damage);

            break;
        }
    }
}
