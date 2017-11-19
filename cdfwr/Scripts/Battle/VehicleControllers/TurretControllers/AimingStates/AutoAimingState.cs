namespace AimingStates
{
    public abstract class AutoAimingState
    {
        protected TurretController turretController;

        protected AutoAimingState(TurretController turretController)
        {
            this.turretController = turretController;
        }

        public abstract void AutoAim();
    }
}
