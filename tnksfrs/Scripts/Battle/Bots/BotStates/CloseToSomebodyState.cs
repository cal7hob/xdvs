using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CloseToSomebodyState : BotState
{
    public CloseToSomebodyState(BotAI botAI) : base(botAI) {}

    public override IEnumerator Updating()
    {
        yield return botAI.CloseToSomebodyStateUpdating();
    }

    public override void OnStart()
    {
        base.OnStart();

        botAI.CurrentBehaviour.Target = botAI.ClosestEnemyVehicle;
        botAI.StopVehicle();
        botAI.CurrentBehaviour.StartPathUpdating();
        botAI.StartFindingOneShotPlayer();
        botAI.CurrentBehaviour.StartFindingTarget();
        botAI.CurrentBehaviour.StartShooting();     
        botAI.StartCheckingIfStuck();
        botAI.CurrentBehaviour.StartUpdating();
    }
}
