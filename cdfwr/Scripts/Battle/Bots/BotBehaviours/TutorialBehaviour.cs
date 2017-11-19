namespace Bots
{
    public class TutorialBehaviour : TargetBehaviour
    {
        public TutorialBehaviour(BotAI botAI) : base(botAI)
        {
            botAI.SlaveController.ShootingController.ShootingStateMachine.SetState(ShootingStates.noShoot);
        }

        public override VehicleController FindTarget()
        {
            return null;
        }

        public override void Aiming()
        {
            
        }
    }
}
