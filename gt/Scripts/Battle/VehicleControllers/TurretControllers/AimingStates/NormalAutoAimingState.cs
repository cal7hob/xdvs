namespace AimingStates
{
    public class NormalAutoAimingState : AutoAimingState
    {
        public NormalAutoAimingState(TurretController turretController) : base(turretController)
        {
        }

        public override void AutoAim()
        {
            turretController.DefaultAutoAim();
        }
    }
}
