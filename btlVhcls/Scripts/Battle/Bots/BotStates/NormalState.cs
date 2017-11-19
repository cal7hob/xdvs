public class NormalState : BotState {

    public NormalState(BotAI BotAi) : base(BotAi)
    {
    }

    public override void OnStart()
    {
        base.OnStart();
       
        botAI.GettingClosestBonusController.Resume();
        botAI.GettingClosestVehicleController.Resume();
        botAI.FindingOneShotEnemyController.Resume();
        botAI.CheckingIfStuckController.Resume();
        botAI.FindTargetController.Resume();
        botAI.FindingPositionController.Resume();
        botAI.PathUpdatingController.Resume();
        botAI.AimingController.Resume();
        botAI.ShootingController.Resume();

        botAI.ResetUpdatingCoroutine(botAI.NormalStateUpdating());
    }
}
