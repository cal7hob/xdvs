using UnityEngine;
using System.Collections;

public class StuckState : BotState {

    public StuckState(BotAI BotAi) : base(BotAi)
    {
    }

    public override IEnumerator Updating()
    {
        yield return botAI.StuckStateUpdating();
    }

    public override void OnStart()
    {
        base.OnStart();

        botAI.StopVehicle();

        botAI.StartGettingClosestVehicle();
        botAI.StartFindingOneShotPlayer();
        botAI.StartCheckingIfStuck();
        botAI.CurrentBehaviour.StartFindingTarget();
        botAI.CurrentBehaviour.StartShooting();
        botAI.CurrentBehaviour.StartUpdating();
    }
}
