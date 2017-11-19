namespace Bots
{
    public abstract class BotTask
    {
        protected BotAI botAI;
        protected VehicleController slaveController;
        protected SoldierBotController soldierBotController;

        protected BotTask(BotAI botAI)
        {
            this.botAI = botAI;

            slaveController = botAI.SlaveController;
            soldierBotController = slaveController as SoldierBotController; // не надо так, но приходится 
        } 

        protected abstract void SelectTask();
        protected abstract void Execute();

        public virtual void Update()
        {
            Execute();
            SelectTask();
        }
    }
}
