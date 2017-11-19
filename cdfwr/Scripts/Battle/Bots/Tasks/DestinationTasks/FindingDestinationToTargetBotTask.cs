namespace Bots
{
    public class FindingDestinationToTargetBotTask : FindingDestinationBotTask
    {
        public FindingDestinationToTargetBotTask(BotAI botAI) : base(botAI)
        {
            destinationPointFinder = new PeriodicTask(FindDestination, new ParamsRange(4, 6));
        }

        protected override void FindDestination()
        {
            botAI.FindBotDestinationCloseToTarget();
        }
    }
}
