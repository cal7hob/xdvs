namespace Bots
{
    public class FighterBehaviour : BotBehaviour
    {
        public FighterBehaviour(BotAI botAI) : base(botAI)
        {
        }

        public override VehicleController FindTarget()
        {
            return GetWeakestEnemyCharacter();
        }

        public override void Move()  // как того требовало поведение Fighter . по-мне здесь это лишнее
        {
            if (botAI.SlaveController.TargetAimed)
            {
                botAI.Stop();
                return;
            }

            botAI.BotMove();
        }
    }
}
