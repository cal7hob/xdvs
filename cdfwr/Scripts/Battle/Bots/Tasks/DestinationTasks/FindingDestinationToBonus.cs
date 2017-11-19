namespace Bots
{
    public class FindingDestinationToBonus : FindingDestinationBotTask
    {
        public FindingDestinationToBonus(BotAI botAI) : base(botAI)
        {
            destinationPointFinder = new PeriodicTask(FindDestination, new ParamsRange(5, 10));
        }

        protected override void FindDestination()
        {
            botAI.GetBonusPosition();
        }
    }
}
