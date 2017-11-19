using UnityEngine;

namespace Bots
{
    public abstract class BotBehaviour
    {
        protected BotAI botAI;

        protected Vector3 inaccuracyOffset;
        protected Vector3 goalGunsightPoint;
        protected PeriodicTask inaccuracyTask;
        protected ParamsRange inaccuracyUpdateRange = new ParamsRange(0.5f, 2.5f);

        public abstract VehicleController FindTarget();

        protected BotBehaviour(BotAI botAI)
        {
            this.botAI = botAI;

            inaccuracyTask = new PeriodicTask(RandomizeAiming, inaccuracyUpdateRange);
            goalGunsightPoint = botAI.transform.forward * 50;
        }

        public virtual void Aiming()
        {
            goalGunsightPoint = botAI.Target != null
                ? botAI.Target.CritCollider.transform.position + inaccuracyOffset
                : botAI.CurrentWaypointPosition + Vector3.up * 1.5f;

            inaccuracyTask.TryExecute(true);

            var currentAimingPoint = Vector3.Lerp(botAI.SlaveController.CamSightPoint, goalGunsightPoint, 3 * Time.deltaTime); // todo: вынести в настройки весь хардкод
            botAI.SlaveController.SetAimingPoint(currentAimingPoint);
        }

        protected void RandomizeAiming()
        {
            inaccuracyOffset = new Vector3(Random.Range(-0.5f, 0.5f), Random.Range(-0.5f, 0.5f)); // сбиваем прицел раз в небольшой промежуток времени
        }

        public virtual void Move()
        {
            botAI.BotMove();
        }

        protected VehicleController GetWeakestEnemyCharacter()
        {
            VehicleController weakestPlayer = null;
            float minHealth = float.MaxValue;

            foreach (var vehicle in BattleController.allVehicles.Values)
            {
                if (StaticContainer.AreFriends(vehicle, botAI.SlaveController) || vehicle == botAI.SlaveController || !vehicle.IsAvailable)
                {
                    continue;
                }

                var currentVehicleArmor = vehicle.Armor;

                if (currentVehicleArmor < minHealth)
                {
                    minHealth = currentVehicleArmor;
                    weakestPlayer = vehicle;
                }
            }

            return weakestPlayer;
        }

        protected VehicleController GetClosestEnemy()
        {
            VehicleController closestEnemyCharacter = null;

            var minDist = float.MaxValue;

            foreach (var currentCharacter in BattleController.allVehicles.Values)
            {
                if (currentCharacter == null || currentCharacter == botAI.SlaveController || !currentCharacter.IsAvailable)
                {
                    continue;
                }

                var distToVehicle = (botAI.SlaveController.transform.position - currentCharacter.transform.position).sqrMagnitude;

                if (minDist > distToVehicle)
                {
                    minDist = distToVehicle;

                    if (!StaticContainer.AreFriends(currentCharacter, botAI.SlaveController))
                    {
                        closestEnemyCharacter = currentCharacter;
                    }
                }
            }

            return closestEnemyCharacter;
        }
    }
}
