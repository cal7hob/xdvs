using UnityEngine;

public class AgressorBehaviour : BotBehaviour
{
    public AgressorBehaviour(BotAI BotAi) : base(BotAi)
    {
        BotSettings =
            ScriptableObject.Instantiate(
                Resources.Load<BotSettings>(string.Format("{0}/ScriptableObjects/BotSettings/AgressorBotSettings", GameManager.CurrentResourcesFolder)));
    }

    public override void FindTarget()
    {
        if (BotAI.Target == null)
        {
            BotAI.Target = BotAI.ClosestEnemyVehicle;
        }
    }
}
