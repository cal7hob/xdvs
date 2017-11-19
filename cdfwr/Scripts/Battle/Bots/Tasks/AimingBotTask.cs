using UnityEngine;

namespace Bots
{
    public class AimingBotTask : BotTask // возможно класс стал лишним
    {
        public AimingBotTask(BotAI botAI) : base(botAI)
        {
        }

        protected override void SelectTask()
        {
        }

        protected override void Execute()
        {
            botAI.BotBehaviour.Aiming();
        }
    }
}
