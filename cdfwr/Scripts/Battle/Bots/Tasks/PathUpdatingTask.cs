namespace Bots
{
    public class PathUpdatingTask : BotTask
    {
        protected PeriodicTask pathUpdater;

        public PathUpdatingTask(BotAI botAI) : base(botAI)
        {
            pathUpdater = new PeriodicTask(botAI.UpdatePath, botAI.BotSettings.PathUpdateDelays);
        }

        protected override void SelectTask()
        {
        }

        protected override void Execute()
        {
            pathUpdater.TryExecute(true);
        }
    }
}
