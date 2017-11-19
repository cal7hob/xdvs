using System.Collections;

class OneShotKillState : BotState
{
    public OneShotKillState(BotAI botAI) : base(botAI)
    {
    }

    public override void OnStart()
    {
        base.OnStart();

        botAI.GettingClosestBonusController.Pause();
        botAI.GettingClosestVehicleController.Pause();
        botAI.FindingOneShotEnemyController.Pause();
        botAI.CheckingIfStuckController.Resume();
        botAI.FindTargetController.Pause();
        botAI.FindingPositionController.Resume();
        botAI.PathUpdatingController.Resume();
        botAI.AimingController.Resume();
        botAI.ShootingController.Resume();

        botAI.ResetUpdatingCoroutine(botAI.OneShotStateUpdating());
    }
}
