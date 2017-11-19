namespace Bots
{
    public class FindingRandomDestination : FindingDestinationBotTask
    {
        public FindingRandomDestination(BotAI botAI) : base(botAI)
        {
            destinationPointFinder = new PeriodicTask(FindDestination, botAI.BotSettings.DestinationFindingDelays);
        }

        protected override void FindDestination()
        {
            if (GameData.IsTeamMode)
            {
                botAI.FindBotDestinationTeam();
                return;
            }

            botAI.FindRandomBotDestination();
        }
    }
}
