namespace Bots
{
    public class FindingTargetBotTask : BotTask
    {
        protected PeriodicTask targetFinder;

        public FindingTargetBotTask(BotAI botAI) : base(botAI)
        {
            targetFinder = new PeriodicTask(botAI.FindTarget, botAI.BotSettings.FindingTargetDelays);
        }

        protected override void SelectTask()
        {
        }

        protected override void Execute()
        {
            targetFinder.TryExecute(true);
        }
    }
}
