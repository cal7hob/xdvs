using UnityEngine;

namespace Bots
{
    public class MyBotRootTask : BotTask
    {
        protected PatrolBotTask patrolBotTask;
        protected ChaseBotTask chaseBotTask;
        protected PathUpdatingTask pathUpdatingTask;

        public MyBotRootTask(BotAI botAI) : base(botAI)
        {
            patrolBotTask = new PatrolBotTask(botAI);
            chaseBotTask = new ChaseBotTask(botAI);
            pathUpdatingTask = new PathUpdatingTask(botAI);
        }

        protected override void SelectTask()
        {
            if (botAI.Target == null)
            {
                patrolBotTask.Update();
                return;
            }
            
            chaseBotTask.Update();
        }

        protected override void Execute()
        {
            pathUpdatingTask.Update();
        }
    }
}

