using System.Collections;
using UnityEngine;

public class FighterBehaviour : BotBehaviour
{
    public FighterBehaviour(BotAI BotAi) : base(BotAi)
    {
        BotSettings =
            ScriptableObject.Instantiate(
                Resources.Load<BotSettings>(string.Format("{0}/ScriptableObjects/BotSettings/FighterBotSettings", GameManager.CurrentResourcesFolder)));
    }

    public override void FindTarget()
    {
        BotAI.Target = BotAI.FindWeakestEnemyVehicle() ?? BotAI.GetClosestEnemyVehicle();
    }

    public IEnumerator CheckingIfTargetAimed()
    {
        var waiter = new WaitForSeconds(0.5f);

        while (true)
        {
            if (BotAI.BotTargetAimed && BotAI.Target == BotAI.ThisVehicle.Target && BotAI.CurrentState != BotAI.StopState)
            {
                BotAI.SetState(BotAI.StopState, true);
            }

            yield return waiter;
        }
    }
}
