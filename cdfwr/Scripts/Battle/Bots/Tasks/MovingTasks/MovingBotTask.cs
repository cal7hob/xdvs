using UnityEngine;

namespace Bots
{
    public abstract class MovingBotTask : BotTask
    {
        protected BotTask aimingBotTask;

        protected MovingBotTask(BotAI botAI) : base(botAI)
        {  
            aimingBotTask = new AimingBotTask(botAI);
        }

        protected override void Execute()
        {
            aimingBotTask.Update();
            botAI.BotBehaviour.Move();
        }

        protected bool CheckForBonusNearBy()
        {
            if (botAI.ClosestBonus == null)
            {
                return false;
            }

            return Vector3.SqrMagnitude(botAI.transform.position - botAI.ClosestBonus.transform.position) < 100; // todo: move to settings
        }
    }
}
