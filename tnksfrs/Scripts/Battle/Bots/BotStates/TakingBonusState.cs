using System.Collections;
using System.Linq;
using UnityEngine;

class TakingBonusState : BotState
{
    public TakingBonusState(BotAI botAI) : base(botAI)
    {
    }

    public override IEnumerator Updating()
    {
        yield return botAI.TakingBonusStateUpdating();
    }

    public override void OnStart()
    {
        base.OnStart();

        if (botAI.ClosestBonus == null)
        {
            botAI.SetState(botAI.NormalState);
            return;
        }


        botAI.CurrentBehaviour.SetPositionToGo(botAI.ClosestBonus.transform.position);

        botAI.StartGettingClosestVehicle();
        botAI.StartFindingOneShotPlayer();
        botAI.StartCheckingIfStuck();
        botAI.CurrentBehaviour.StartPathUpdating();
        botAI.CurrentBehaviour.StartShooting();
        botAI.CurrentBehaviour.StartUpdating();

        if (botAI.CurrentState != botAI.RevengeState)
        {
            botAI.CurrentBehaviour.StartFindingTarget();
        }
    }
}
