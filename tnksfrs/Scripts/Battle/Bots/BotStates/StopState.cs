using System.Collections;
using UnityEngine;

class StopState : BotState
{
    public StopState(BotAI BotAi) : base(BotAi)
    {
    }

    public override IEnumerator Updating()
    {
        yield return botAI.StopStateUpdating();
    }

    public override void OnStart()
    {
        botAI.StopVehicle();

        botAI.StartGettingClosestVehicle();
        botAI.CurrentBehaviour.StartUpdating();
        botAI.CurrentBehaviour.StartFindingTarget();
        botAI.CurrentBehaviour.StartShooting();
    }
}
