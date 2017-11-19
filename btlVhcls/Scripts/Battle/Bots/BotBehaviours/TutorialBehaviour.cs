using System.Collections;

class TutorialBehaviour : TargetBehaviour
{
    public TutorialBehaviour(BotAI BotAi) : base(BotAi) { }

    public override float FindingPosDelay
    {
        get { return BotSettings.findingPosDelaysTutorial_s.RandWithinRange; }
    }

    public override IEnumerator Shoot()
    {
        yield break;
    }

    public override void FindTarget()
    {
        if (!GameData.IsGame(Game.BattleOfWarplanes))
            BotAI.Target = myVehicle;
    }

    public override void OnDamage(int attackerId)
    {
        if (GameData.IsGame(Game.BattleOfWarplanes))
            return;

        base.OnDamage(attackerId);

        if (MiscTools.random.Next(0, 100) < BotSettings.targetBotRevengeChance_s && BotAI.CurrentState != BotAI.RevengeState)
        {
            BotAI.Target = BattleController.allVehicles[attackerId];
            BotAI.SetState(BotAI.RevengeState);
            BotAI.GetDirectionsToTarget();
        }
    }
}
