namespace Bots
{
    public class AgressorBehaviour : BotBehaviour
    {
        public AgressorBehaviour(BotAI botAI) : base(botAI)
        {
        }

        public override VehicleController FindTarget()
        {
            return GetClosestEnemy();
        }
    }
}
