using UnityEngine;

namespace Bots
{
    public abstract class FindingDestinationBotTask : BotTask
    {
        protected PeriodicTask destinationPointFinder;

        protected FindingDestinationBotTask(BotAI botAI) : base(botAI)
        {
        }

        protected override void SelectTask()
        {
            if (botAI.CheckIfPathEnded())
            {
                FindDestination();
            }
        }

        protected override void Execute()
        {
            destinationPointFinder.TryExecute(true);
        }

        protected abstract void FindDestination();
    }
}
