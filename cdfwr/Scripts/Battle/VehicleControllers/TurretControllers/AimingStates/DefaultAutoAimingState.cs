namespace AimingStates
{
    public class DefaultAutoAimingState : AutoAimingState
    {
        public DefaultAutoAimingState(TurretController turretController) : base(turretController)
        {
        }

        public override void AutoAim()
        {
            turretController.DefaultAutoAim();
        }
    }
}
