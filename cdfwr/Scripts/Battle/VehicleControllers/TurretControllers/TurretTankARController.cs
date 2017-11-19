using UnityEngine;

public class TurretTankARController : TurretTankController
{
    public TurretTankARController(VehicleController vehicle, Animation shootAnimation)
        : base(vehicle, shootAnimation)
    {
        tank = vehicle as TankController;
    }

    protected virtual Quaternion Rotation
    {
        get { return AdditionShellRotation(vehicle.ShotPoint); }
    }

    protected override Quaternion AdditionShellRotation(Transform shotPoint)
    { 
        return (TargetAimed && vehicle.IsMain)? //берем точку чуть выше трансформа танка, т.к. он выставлен на земле и иногда поэтому мажем
            Quaternion.LookRotation((vehicle.ViewPoint - shotPoint.position).normalized, shotPoint.up): 
            shotPoint.rotation;
    }

    public override bool Fire()
    {
        if (!BaseFire()) 
        {
            return false;
        }
        
        if (IsMine)
        {
            BattleGUI.FireButtons[DefaultShellType].SimulateReloading();
        }

        Quaternion rotation = Camera.main.transform.rotation; //todo: переделать, когда будет к чему привязывать направление выстрела
        Shell shell = GetShell(vehicle.ShotPoint, rotation, CannonEnd, vehicle.PrimaryShellInfo, Mathf.Abs(tank.curMaxSpeed));
        vehicle.SetContiniousFire(false);
        shell.Activate(vehicle, vehicle.data.attack, vehicle.HitMask);

        AudioDispatcher.PlayClipAtPosition(vehicle.shotSound, vehicle.ShotPoint.position, SoundControllerBase.SHOT_VOLUME);

        return true;
    }
}