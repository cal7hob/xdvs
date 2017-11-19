using System.Collections;
using UnityEngine;

class RevengeState : BotState
{
    public RevengeState(BotAI botAI) : base(botAI)
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
        botAI.ShootingController.Resume();

        botAI.ResetUpdatingCoroutine(botAI.RevengeStateUpdating());
    }
}
