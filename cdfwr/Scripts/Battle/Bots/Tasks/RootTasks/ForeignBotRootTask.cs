using UnityEngine;

namespace Bots
{
    public class ForeignBotRootTask : BotTask
    {
        public ForeignBotRootTask(BotAI botAI) : base(botAI)
        {
        }

        protected override void SelectTask()
        {
            Debug.Log("not mine bot");
        }

        protected override void Execute()
        {
        }
    }
}
