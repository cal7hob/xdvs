using System.Collections;
using UnityEngine;

class RevengeState : BotState
{
    public RevengeState(BotAI botAI) : base(botAI)
    {
    }

    public override IEnumerator Updating()
    {
        yield return botAI.RevengeStateUpdating();
    }

    public override void OnStart()
    {
        base.OnStart();

        botAI.StartGettingClosestVehicle();
        botAI.StartCheckingIfStuck();
        botAI.CurrentBehaviour.StartFindingPosition();
        botAI.CurrentBehaviour.StartPathUpdating();
        botAI.CurrentBehaviour.StartShooting();
        botAI.CurrentBehaviour.StartUpdating();
    }
}
