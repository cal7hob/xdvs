using System.Collections;
using UnityEngine;

class RollBackState : BotState
{
    public RollBackState(BotAI botAI) : base(botAI)
    {
    }

    public override IEnumerator Updating()
    {
        yield return botAI.RollbackStateUpdating();
    }

    public override void OnStart()
    {
        base.OnStart();

        botAI.StartCheckingIfStuck();
        botAI.StartGettingClosestVehicle();
        botAI.CurrentBehaviour.StartFindingTarget();
        botAI.CurrentBehaviour.StartFindingPosition();
        botAI.CurrentBehaviour.StartPathUpdating();
        botAI.CurrentBehaviour.StartShooting();
        botAI.CurrentBehaviour.StartUpdating();
    }
}
