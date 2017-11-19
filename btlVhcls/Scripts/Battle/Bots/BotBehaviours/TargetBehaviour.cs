class TargetBehaviour : BotBehaviour
{
    public TargetBehaviour(BotAI BotAi) : base(BotAi)
    {
    }

    public override float FireDelay { get { return MiscTools.random.Next(75, 100) * 0.001f; } }

    public override float FindingPosDelay { get { return BotSettings.findingPosDelaysTarget_s.RandWithinRange; } }

    public override float FindingTargetDelay { get { return BotSettings.findingTargetDelaysTarget_s.RandWithinRange; } }

    public override void FindTarget() { }

    public override void OnDamage(int attackerId)
    {
        base.OnDamage(attackerId);

        if (MiscTools.random.Next(0, 100) < BotSettings.targetBotRevengeChance_s && BotAI.CurrentState != BotAI.RevengeState)
        {
            BotAI.Target = BattleController.allVehicles[attackerId];
            BotAI.SetState(BotAI.RevengeState);
            BotAI.GetDirectionsToTarget();
        }
    }
}
