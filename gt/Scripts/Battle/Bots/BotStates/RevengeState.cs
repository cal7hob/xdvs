public class RevengeState : BotState
{
    public float Delay { get; private set; }
    public float TimeToRevenge { get; private set; }
    public float Duration { get; private set; }

    public override bool CanSwitchToThisState { get { return botAI.CurrentState != botAI.DeadState; } }

    public override float FindingPosDelay { get { return botAI.CurrentBehaviour.BotSettings.RevengeFindingPosDelays.RandWithinRange; } }

    public RevengeState(BotAI botAI) : base(botAI)
    {
        Init();
    }

    public void Init()
    {
        Delay = botAI.CurrentBehaviour.BotSettings.RevengeDelays.RandWithinRange;
        TimeToRevenge = botAI.CurrentBehaviour.BotSettings.RevengeTimes.RandWithinRange;
        Duration = 0;
    }

    public override void OnStart()
    {
        base.OnStart();

        botAI.GettingClosestBonusController.Pause();
        botAI.GettingClosestVehicleController.Pause();
        botAI.FindingOneShotEnemyController.Pause();
        botAI.CheckingIfStuckController.Resume();
        botAI.FindTargetController.Pause();
        botAI.FindingPositionController.Restart();
        botAI.PathUpdatingController.Resume();
        botAI.AimingController.Resume();
        botAI.ShootingController.Resume();

        botAI.ResetUpdatingCoroutine(botAI.RevengeStateUpdating());
    }
}
