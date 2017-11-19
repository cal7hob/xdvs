using System.Collections;

class OneShotKillState : BotState
{
    public OneShotKillState(BotAI botAI) : base(botAI)
    {
    }

    public override IEnumerator Updating()
    {
        yield return botAI.OneShotStateUpdating();
    }

    public override void OnStart()
    {
        if (botAI.CurrentBehaviour.Target == null)
        {
            botAI.SetState(botAI.NormalState);
            return;
        }

        botAI.CurrentBehaviour.SetPositionToGo(botAI.CurrentBehaviour.Target.transform.position);

        base.OnStart();

        botAI.StartGettingClosestVehicle();
        botAI.StartCheckingIfStuck();
        botAI.CurrentBehaviour.StartPathUpdating();        
        botAI.CurrentBehaviour.StartShooting();
        botAI.CurrentBehaviour.StartUpdating();
    }
}
