namespace Bots
{
    public class FindingBonusBotTask : BotTask
    {
        protected PeriodicTask bonusFinder;

        public FindingBonusBotTask(BotAI botAI) : base(botAI)
        {
            bonusFinder = new PeriodicTask(botAI.FindClosestBonus, botAI.BotSettings.GettingClosestBonusDelays);
        }

        protected override void SelectTask()
        {
        }

        protected override void Execute()
        {
            bonusFinder.TryExecute(true);
        }
    }
}
