using UnityEngine;

namespace Bots
{
    public class ChaseBotTask : MovingBotTask
    {
        protected FindingDestinationToTargetBotTask findingDestinationToTargetBotTask;
        protected BotTask findingBonusBotTask;
        protected BotTask findingDestinationToBonusBotTask;

        public ChaseBotTask(BotAI botAI) : base(botAI)
        {
            findingDestinationToTargetBotTask = new FindingDestinationToTargetBotTask(botAI);
            findingBonusBotTask = new FindingBonusBotTask(botAI);
            findingDestinationToBonusBotTask = new FindingDestinationToBonus(botAI);
        }

        protected override void SelectTask()
        {
            if (CheckForBonusNearBy())
            {
                findingDestinationToBonusBotTask.Update();
                return;
            }

            findingDestinationToTargetBotTask.Update();
        }

        protected override void Execute()
        {
            base.Execute();

            findingBonusBotTask.Update();
        }
    }
}
