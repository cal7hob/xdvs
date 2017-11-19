using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class NormalState : BotState {

    public NormalState(BotAI BotAi) : base(BotAi)
    {
    }

    public override IEnumerator Updating()
    {
        yield return botAI.NormalStateUpdating();
    }

    public override void OnStart()
    {
        base.OnStart();

        botAI.StartGettingClosestVehicle();
        botAI.StartFindingOneShotPlayer();
        botAI.StartCheckingIfStuck();
        botAI.CurrentBehaviour.StartFindingTarget();
        botAI.CurrentBehaviour.StartFindingPosition();
        botAI.CurrentBehaviour.StartPathUpdating();        
        botAI.CurrentBehaviour.StartShooting();
        botAI.CurrentBehaviour.StartUpdating();
    }
}
