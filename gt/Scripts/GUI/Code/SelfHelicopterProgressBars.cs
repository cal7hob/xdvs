public class SelfHelicopterProgressBars : SelfTankProgressBars
{
    public ProgressBar ircmReloadProgressBar;

    protected override void Update()
    {
        if (!BattleController.MyVehicle)
            return;

        weaponReloadProgressBar.Percentage = BattleController.MyVehicle.GetWeapon(ShellType.Missile_SACLOS).ReloadingProgress;
        ircmReloadProgressBar.Percentage = BattleController.MyVehicle.GetWeapon(ShellType.IRCM).ReloadingProgress;
    }
}
