namespace AimingStates
{
    public class FullAutoAimingState : AutoAimingState
    {
        public FullAutoAimingState(TurretController turretController) : base(turretController)
        {
        }

        public override void AutoAim()
        {
            turretController.FullAutoAim();
        }
    }
}
