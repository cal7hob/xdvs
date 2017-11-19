public class DeadState : BotState
{
    public DeadState(BotAI botAI) : base(botAI)
    {
    }

    public override void OnStart()
    {
        base.OnStart();

        botAI.GettingClosestBonusController.Pause();
        botAI.GettingClosestVehicleController.Pause();
        botAI.FindingOneShotEnemyController.Pause();
        botAI.CheckingIfStuckController.Pause();
        botAI.FindTargetController.Pause();
        botAI.FindingPositionController.Pause();
        botAI.PathUpdatingController.Pause();
        botAI.AimingController.Pause();
        botAI.ShootingController.Pause();

        botAI.StopVehicle();
        botAI.ThisVehicle.StopCoroutine(botAI.UpdatingRoutine);
    }
}
