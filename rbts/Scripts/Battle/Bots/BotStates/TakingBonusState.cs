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

        if (!botAI.ClosestBonus)
        {
            botAI.SetState(botAI.NormalState);
            return;
        }

        botAI.PositionToMove = botAI.ClosestBonus.transform.position;

        botAI.GettingClosestBonusController.Pause();
        botAI.GettingClosestVehicleController.Resume();
        botAI.FindingOneShotEnemyController.Resume();
        botAI.CheckingIfStuckController.Resume();
        botAI.FindTargetController.Pause();
        botAI.FindingPositionController.Pause();
        botAI.PathUpdatingController.Resume();
        botAI.ShootingController.Resume();

        if (botAI.CurrentState != botAI.RevengeState)
        {
            botAI.FindTargetController.Resume(); 
        }

        botAI.ResetUpdatingCoroutine(botAI.TakingBonusStateUpdating());
    }
}
