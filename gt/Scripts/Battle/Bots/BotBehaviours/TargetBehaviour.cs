using UnityEngine;

public class TargetBehaviour : BotBehaviour
{
    public TargetBehaviour(BotAI BotAi) : base(BotAi)
    {
        BotSettings =
            ScriptableObject.Instantiate(
                Resources.Load<BotSettings>(string.Format("{0}/ScriptableObjects/BotSettings/TargetBotSettings", GameManager.CurrentResourcesFolder)));
    }

    public override void FindTarget()
    {
        if (!GameData.IsTeamMode)
            return;

        BotAI.Target = BotAI.FindWeakestEnemyVehicle() ?? BotAI.GetClosestEnemyVehicle();
    }

    public override void OnDamage(int attackerId)
    {
        base.OnDamage(attackerId);

        if (MiscTools.random.Next(0, 100) < BotSettings.TargetBotRevengeChance && BotAI.CurrentState != BotAI.RevengeState)
        {
            BotAI.Target = BattleController.allVehicles[attackerId];
            BotAI.SetState(BotAI.RevengeState);
            BotAI.GetDirectionsToTarget();
        }
    }
}
