namespace Bots
{
    public class TargetBehaviour : BotBehaviour
    {
        public TargetBehaviour(BotAI botAI) : base(botAI)
        {
        }

        public override VehicleController FindTarget()
        {
            if (!GameData.IsTeamMode)
            {
                //return null;
            }

            return GetWeakestEnemyCharacter() ?? GetClosestEnemy();
        }
    }
}
