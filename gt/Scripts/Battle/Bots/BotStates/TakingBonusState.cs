using System.Collections;
using UnityEngine;

class TakingBonusState : BotState
{
    public TakingBonusState(BotAI botAI) : base(botAI)
    {
    }

    public override void OnStart()
    {
        base.OnStart();

        var closestBonus = botAI.ClosestBonus;

        if (!closestBonus)
        {
            botAI.SetState(botAI.NormalState);
            return;
        }

        botAI.GettingClosestBonusController.Pause();
        botAI.GettingClosestVehicleController.Resume();
        botAI.FindingOneShotEnemyController.Resume();
        botAI.CheckingIfStuckController.Resume();
        botAI.FindTargetController.Pause();
        botAI.FindingPositionController.Restart();
        botAI.PathUpdatingController.Resume();
        botAI.AimingController.Resume();
        botAI.ShootingController.Resume();

        if (botAI.CurrentState != botAI.RevengeState)
        {
            botAI.FindTargetController.Resume(); 
        }

        botAI.ResetUpdatingCoroutine(botAI.TakingBonusStateUpdating());
    }

    public override void FindPositionToMove()
    {
        botAI.MoveToBonus();
    }
}
