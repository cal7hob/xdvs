using UnityEngine;

namespace Bots
{
    public class PatrolBotTask : MovingBotTask
    {
        protected BotTask findingTargetBotTask;
        protected BotTask findingBonusBotTask;
        protected BotTask findingRandomDestinationBotTask;
        protected BotTask findingDestinationToBonusBotTask;

        public PatrolBotTask(BotAI botAI) : base(botAI)
        {
            findingTargetBotTask = new FindingTargetBotTask(botAI);
            findingBonusBotTask = new FindingBonusBotTask(botAI);
            findingRandomDestinationBotTask = new FindingRandomDestination(botAI); 
            findingDestinationToBonusBotTask = new FindingDestinationToBonus(botAI);
        }

        protected override void SelectTask()
        {
            if (CheckForBonusNearBy())
            {
                findingDestinationToBonusBotTask.Update();
                return;
            }

            findingRandomDestinationBotTask.Update();
        }

        protected override void Execute()
        {
            base.Execute();

            findingTargetBotTask.Update();
            findingBonusBotTask.Update();
        }
    }
}
