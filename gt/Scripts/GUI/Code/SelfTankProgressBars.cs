using Disconnect;
using UnityEngine;

public class SelfTankProgressBars : MonoBehaviour
{
    public ProgressBar armorProgressBar;
    public ProgressBar weaponReloadProgressBar;

    void Awake()
    {
		Dispatcher.Subscribe(EventId.MainTankAppeared, OnMainVehicleAppeared);
		Dispatcher.Subscribe(EventId.TankHealthChanged, OnVehicleHealthChange);
        Dispatcher.Subscribe(EventId.TankRespawned, OnVehicleRespawned, 1);
    }

    void OnDestroy()
    {
        Dispatcher.Unsubscribe(EventId.MainTankAppeared, OnMainVehicleAppeared);
        Dispatcher.Unsubscribe(EventId.TankHealthChanged, OnVehicleHealthChange);
        Dispatcher.Unsubscribe(EventId.TankRespawned, OnVehicleRespawned);
    }

    protected virtual void Update()
    {
        if (!BattleController.MyVehicle)
        {
            return;
        }

		weaponReloadProgressBar.Percentage = BattleController.MyVehicle.WeaponReloadingProgress;
    }

	private void Refresh()
	{
        if (!BattleController.MyVehicle)
        {
            return;
        }

        armorProgressBar.Percentage = (float)BattleController.MyVehicle.Armor / BattleController.MyVehicle.data.maxArmor;
	}

	
	private void OnMainVehicleAppeared(EventId id, EventInfo ei)
	{
		Refresh();
	}

	private void OnVehicleHealthChange(EventId id, EventInfo ei)
	{
        EventInfo_II eventInfo = (EventInfo_II)ei;
		int playerId = eventInfo.int1;
        if (playerId == BattleController.MyPlayerId)
        {
            Refresh();
        }
    }

    private void OnVehicleRespawned(EventId id, EventInfo info)
    {
        EventInfo_I eventInfo = (EventInfo_I)info;
        int playerId = eventInfo.int1;

        if (playerId == BattleController.MyPlayerId)
        {
            Refresh();
        }
    }
}
