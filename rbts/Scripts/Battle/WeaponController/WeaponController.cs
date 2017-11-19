using UnityEngine;

public abstract class WeaponController
{
    protected VehicleController owner;

    protected WeaponController(VehicleController owner)
    {
        this.owner = owner;
    }

    public abstract void UpdateWeapon();
    public abstract void RegisterShot();
    public abstract float Progress { get; }
    public abstract bool IsReady { get; }
    public abstract void InstantReload();
    public virtual int Counter { get { return 0; } }
}
