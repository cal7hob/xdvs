using UnityEngine;
using System.Collections;

public class StuckState : BotState {

    public StuckState(BotAI BotAi) : base(BotAi)
    {
    }

    public override void OnStart()
    {
        base.OnStart();

        botAI.GettingClosestBonusController.Pause();
        botAI.GettingClosestVehicleController.Resume();
        botAI.FindingOneShotEnemyController.Resume();
        botAI.CheckingIfStuckController.Resume();
        botAI.FindTargetController.Resume();
        botAI.FindingPositionController.Pause();
        botAI.PathUpdatingController.Pause();
        botAI.ShootingController.Resume();

        botAI.ResetUpdatingCoroutine(botAI.StuckStateUpdating());
    }
}
