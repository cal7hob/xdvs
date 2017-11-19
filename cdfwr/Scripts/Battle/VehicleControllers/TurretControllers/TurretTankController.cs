using UnityEngine;

public class TurretTankController : TurretController
{
    protected TankController tank;

    public TurretTankController(VehicleController vehicle, Animation shootAnimation) : base(vehicle, shootAnimation)
    {
        tank = vehicle as TankController;
    }

    public override bool Fire()
    {
        if (!BaseFire())
        {
            return false;
        }

        BattleGUI.FireButtons[StaticContainer.DefaultShellType].SimulateReloading();
        FireWithoutShell(false);
        if (vehicle.IsMain)
        {
            Dispatcher.Send(EventId.MyTankShots, new EventInfo_I((int)StaticContainer.DefaultShellType));
        }
        return true;
    }

    public override void SetTurretAudio() { }
}