namespace AimingStates
{
    public class WithoutAutoAimingState : AutoAimingState
    {
        public WithoutAutoAimingState(TurretController turretController) : base(turretController)
        {
        }

        public override void AutoAim()
        {
        }
    }
}
