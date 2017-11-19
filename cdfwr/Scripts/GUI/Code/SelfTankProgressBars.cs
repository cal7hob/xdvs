using Disconnect;
using UnityEngine;
using System.Collections;

public class SelfTankProgressBars : MonoBehaviour
{
    public ProgressBar armorProgressBar;
    public ProgressBar weaponReloadProgressBar;
    public ProgressBar ForceReloadProgressBar;
    public tk2dTextMesh magazine;

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

    public void UpdateBar(float percentage, int counter)
    {
        if (ForceReloadProgressBar != null && ForceReloadProgressBar.gameObject.activeInHierarchy)
        {
            ForceReloadProgressBar.Percentage = percentage;
        }
        weaponReloadProgressBar.Percentage = percentage;
        if (magazine != null)
        {
            magazine.text = counter.ToString();
        }
    }

    private void OnTankShot(EventId id, EventInfo info)
    {

        Dispatcher.Unsubscribe(EventId.MyTankShots, OnTankShot);
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
        weaponReloadProgressBar.Percentage = 1;
        magazine.text = BattleController.MyVehicle.data.magazine.ToString();
        BattleController.MyVehicle.turretController.SetProgressBar(this);
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
