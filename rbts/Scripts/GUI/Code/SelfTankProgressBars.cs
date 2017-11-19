using Disconnect;
using UnityEngine;

public class SelfTankProgressBars : MonoBehaviour
{
    public ProgressBar armorProgressBar;
    public ProgressBar weaponReloadProgressBar;

    [SerializeField]
    private GameObject sprArmorBarIconTank;
    [SerializeField]
    private GameObject sprArmorBarIconRobot;

    private void Awake()
    {
        Messenger.Subscribe(EventId.MainTankAppeared, OnMainVehicleAppeared);
        Messenger.Subscribe(EventId.TankHealthChanged, OnVehicleHealthChange);
        Messenger.Subscribe(EventId.VehicleRespawned, OnVehicleRespawned, 1);

        VehicleInfo.VehicleType vehicletype =
            ProfileInfo.IsBattleTutorial
                ? Shop.GetVehicle(GameData.CurrentTutorialVehicleId).Info.vehicleType
                : Shop.CurrentVehicle.Info.vehicleType;

        sprArmorBarIconTank.SetActive(vehicletype == VehicleInfo.VehicleType.Tank);
        sprArmorBarIconRobot.SetActive(vehicletype == VehicleInfo.VehicleType.Robot);
    }

    private void OnDestroy()
    {
        Messenger.Unsubscribe(EventId.MainTankAppeared, OnMainVehicleAppeared);
        Messenger.Unsubscribe(EventId.TankHealthChanged, OnVehicleHealthChange);
        Messenger.Unsubscribe(EventId.VehicleRespawned, OnVehicleRespawned);
    }

    protected virtual void Update()
    {
		if (!BattleController.MyVehicle)
			return;

		weaponReloadProgressBar.Percentage = BattleController.MyVehicle.WeaponReloadingProgress;
    }

	private void Refresh()
	{
        if (!BattleController.MyVehicle)
            return;

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
			Refresh();
    }

    private void OnVehicleRespawned(EventId id, EventInfo info)
    {
        EventInfo_I eventInfo = (EventInfo_I)info;

        int playerId = eventInfo.int1;

        if (playerId == BattleController.MyPlayerId)
            Refresh();
    }
}
