using System.Collections;
using UnityEngine;
using UnityEngine.AI;

namespace Bots
{
    public class SoldierBotAI : BotAI
    {
        protected Transform soldierIK;
        protected LayerMask walkableMask;
        protected int currentWaypointIndex;
        protected SoldierBotController soldierBotController;
        protected Vector3 currentWaypointPosition;

        public NavMeshPath Path { get; protected set; }

        public override Vector3 CurrentWaypointPosition { get { return currentWaypointPosition; } }

        protected override void Initialize()
        {
            base.Initialize();

            walkableMask = 1 << NavMesh.GetAreaFromName("Walkable");
            soldierBotController = (SoldierBotController) slaveController;
            soldierIK = soldierBotController.IkController.transform;
            Path = new NavMeshPath();
        }

        protected override Vector3 FindRandomPointNearPosition(Vector3 pos, int radius = 25)
        {
            var pointFound = false;
            var i = 0;
            var navMeshHit = new NavMeshHit();

            while (!pointFound && i++ < 10)
            {
                var sourcePosition = pos + new Vector3(MiscTools.random.Next(-radius, radius), MiscTools.random.Next(-radius, radius));
                pointFound = NavMesh.SamplePosition(sourcePosition, out navMeshHit, 60, walkableMask);
            }

            return pointFound ? navMeshHit.position : SpawnPoints.instance.GetRandomPoint(slaveController.data.teamId).position;
        }

        protected override void OnTakesDamage(int attackerId)
        {
            target = BattleController.allVehicles[attackerId];
        }

        protected override bool CheckBonusAcessibility()
        {
            NavMeshPath pathToBonus = new NavMeshPath();

            if (NavMesh.CalculatePath(transform.position, goalPosition, walkableMask, pathToBonus))
            {
                Path = pathToBonus;
                return true;
            }

            return false;
        }

        protected override void OnGetMasterStatus(EventId id, EventInfo ei)
        {
            slaveController.Rb.isKinematic = false;
            soldierBotController.animator.applyRootMotion = true;
            slaveController.Rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            botRootTask = myBotRootTask;
            ReanimateBot();
        }

        public override void FindBotDestinationCloseToTarget()
        {
            goalPosition = FindRandomPointNearPosition(target.transform.position, 10);
            UpdatePath();
        }

        public override void FindBotDestinationTeam()
        {
            goalPosition = slaveController.IsMainsFriend
                    ? FindRandomPointNearPosition(BotDispatcher.EnemiesEpicenter, 30)
                    : FindRandomPointNearPosition(BotDispatcher.FriendsEpicenter, 30);
            UpdatePath();
        }

        public override void FindRandomBotDestination()
        {
            goalPosition = FindRandomPointNearPosition(slaveController.transform.position, radius: 40);
            UpdatePath();
        }

        public override void OnChangeAvailability(bool isAvailable)
        {
        }

        public override bool CheckIfPathEnded()
        {
            return Path.corners.Length == 0 || currentWaypointIndex > Path.corners.Length - 1 || currentWaypointIndex < 0;
        }

        public override void BotMove()
        {
            if (CheckIfPathEnded())
            {
                Stop();
                return;
            }

            currentWaypointPosition = Path.corners[currentWaypointIndex];
            var dirToWaypoint = currentWaypointPosition - soldierIK.position;

            if (Vector3.SqrMagnitude(dirToWaypoint) < BotSettings.ClearWaypointSqrDistance)
            {
                currentWaypointIndex++;
            }


            dirToWaypoint = dirToWaypoint.normalized;
            var dotProd = Vector3.Dot(dirToWaypoint, soldierIK.right);
            var waypointIsInFront = Vector3.Dot(dirToWaypoint, soldierIK.forward) > 0 ? 1 : -1;

            botXAxisControl = Mathf.Clamp(dotProd * 2, -1, 1);
            botYAxisControl = Mathf.Clamp01(1 - Mathf.Abs(dotProd)) * waypointIsInFront;
        }

        public override void UpdatePath()
        {
            Path.ClearCorners();
            currentWaypointIndex = 0;

            NavMesh.CalculatePath(slaveController.transform.position, goalPosition, walkableMask, Path);

            if (Path.corners.Length > 0)
            {
                Path.corners[0] = slaveController.transform.position;
            }
        }

#if UNITY_EDITOR
        void OnDrawGizmos()
        {
            if (!BotDispatcher.Instance || !BotDispatcher.DrawBotPaths || Path == null || Path.corners.Length <= 0 
                || currentWaypointIndex > Path.corners.Length - 1)
            {
                return;
            }

            for (int i = 0; i < Path.corners.Length - 1; i++)
            {
                Debug.DrawLine(Path.corners[i], Path.corners[i + 1], Color.red);
            }

            Gizmos.color = Color.red;
            Gizmos.DrawSphere(Path.corners[currentWaypointIndex], 1);

            Gizmos.color = Color.yellow;
            Gizmos.DrawSphere(goalPosition, 1);
        }
#endif
    }
}
