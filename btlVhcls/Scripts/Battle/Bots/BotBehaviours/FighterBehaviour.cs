class FighterBehaviour : BotBehaviour
{
    public FighterBehaviour(BotAI BotAi) : base(BotAi)
    {
    }

    public override float FireDelay { get { return MiscTools.random.Next(50, 100) * 0.001f; } }

    public override float FindingPosDelay { get { return BotSettings.findingPosDelaysFighter_s.RandWithinRange; } }

    public override float FindingTargetDelay { get { return BotSettings.findingTargetDelaysFighter_s.RandWithinRange; } }

    public override void FindTarget()
    {
        BotAI.Target = BotAI.FindWeakestEnemyVehicle() ?? BotAI.GetClosestEnemyVehicle();
    } 
}
