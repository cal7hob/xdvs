class AgressorBehaviour : BotBehaviour
{
    public AgressorBehaviour(BotAI BotAi) : base(BotAi)
    {
    }

    public override float FireDelay { get { return MiscTools.random.Next(20, 100) * 0.001f; } }

    public override float FindingTargetDelay { get { return BotSettings.findingTargetDelaysAgressor_s.RandWithinRange; } }

    public override float FindingPosDelay { get { return BotSettings.findingPosDelaysAgressor_s.RandWithinRange; } }

    public override void FindTarget()
    {
        if (BotAI.Target == null)
        {
            BotAI.Target = BotAI.ClosestEnemyVehicle;
            BotAI.CalcPathToTarget();
        }
    }
}
