using CodeStage.AntiCheat.ObscuredTypes;
using UnityEngine;

public class Weapon
{
    private static readonly ObscuredFloat FIRE_RATE_SECONDS = 60.0f;

    private readonly ShellType shellType;
    private readonly VehicleController vehicleController;

    public Weapon(VehicleController vehicleController, ShellType shellType)
    {
        this.vehicleController = vehicleController;
        this.shellType = shellType;
    }

    public bool IsReady
    {
        get { return !(!IsReloaded || IsOverheat); }
    }

    public bool IsOverheat
    {
        get
        {
            bool overheat = (vehicleController.IsMain && HeatingProgress > 1 - vehicleController.GetHeating(shellType));

            if (overheat)
            {
                Dispatcher.Send(EventId.WeaponOverheated, new EventInfo_I((int)shellType));
            }

            return overheat;
        }
    }

    public bool IsReloaded
    {
        get; private set;
    }

    public float ReloadRemainingSeconds
    {
        get { return ReloadingTimeSeconds * (1 - ReloadingProgress); }
    }

    public ObscuredFloat ReloadingProgress
    {
        get; private set;
    }

    public ObscuredFloat HeatingProgress
    {
        get; private set;
    }

    public ObscuredFloat ReloadingTimeSeconds 
    {
        get { return FIRE_RATE_SECONDS / vehicleController.GetROF(shellType); }
    }

    public void UpdateReloadingProgress()
    {
        HeatingProgress = Mathf.Clamp01(HeatingProgress - vehicleController.GetCooling(shellType) * Time.deltaTime);

        if (IsReloaded)
        {
            return;
        }

        ReloadingProgress += Time.deltaTime / ReloadingTimeSeconds;

        if (ReloadingProgress >= 1)
        {
            IsReloaded = true;

            if (!vehicleController.IsMain)
            {
                return;
            }

            Dispatcher.Send(EventId.WeaponReloaded, new EventInfo_I((int)shellType));
        }
    }

    public void InstantReload()
    {
        ReloadingProgress = 1;
        HeatingProgress = 0;
    }

    public void RegisterShot()
    {
        IsReloaded = false;
        ReloadingProgress = 0;

        HeatingProgress += vehicleController.GetHeating(shellType);
    }
}
