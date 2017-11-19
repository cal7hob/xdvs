using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using DemetriTools.Optimizations;

public class TankBotAI : BotAI
{
    protected LayerMask walkableMask = 1 << NavMesh.GetAreaFromName("Walkable");

    private readonly Vector3[] pathCornersArray = new Vector3[64];
    private readonly List<Vector3> pathCorners = new List<Vector3>(32);
    public List<Vector3> PathCorners { get { return pathCorners; } }

    private readonly NavMeshPath path = new NavMeshPath();
    private readonly RepeatingOptimizer sidewaysLookOptimizer = new RepeatingOptimizer(GameConstants.SIDEWAYS_LOOK_INTERVAL, 0);
    private readonly RepeatingOptimizer forwardLookOptimizer = new RepeatingOptimizer(GameConstants.FORWARD_LOOK_INTERVAL, 0);

    public TankBotAI(VehicleController vehicle, BotDispatcher.BotBehaviours botBehaviour) : base(vehicle, botBehaviour)
    {}

    protected override IEnumerator FindingPosition()
    {
        WaitForSeconds waiter = new WaitForSeconds(CurrentBehaviour.FindingPosDelay);
        while (true)
        {
            FindPositionToMove();
            yield return FindPath() ? waiter : null; //Если путь не найден, поменять целевую позицию в следующем кадре
        }
    }

    public override IEnumerator CheckingIfStuck()
    {
        yield return null;

        while (true)
        {
            var savedPos = ThisVehicle.transform.position;
            var savedRotation = ThisVehicle.transform.rotation;

            yield return new WaitForSeconds(stuckTime);
            if ((ThisVehicle.transform.position - savedPos).sqrMagnitude < BotSettings.stuckSqrMagnitude_s &&
                Quaternion.Angle(savedRotation, ThisVehicle.transform.rotation) < BotSettings.stuckAngle_s)
            {
                SetState(StuckState, CurrentState != StuckState);
            }
        }
    }

    public override IEnumerator NormalStateUpdating()
    {
        while (true)
        {
            Move();
            ThisVehicle.TurretRotation();
            yield return null;
        }
    }

    public override IEnumerator OneShotStateUpdating()
    {
        while (true)
        {
            Move();
            ThisVehicle.TurretRotation();

            yield return null;
        }
    }

    public override IEnumerator RevengeStateUpdating()
    {
        var revengeDelay = BotSettings.revengeDelays_s.RandWithinRange;
        var timeToRevenge = BotSettings.revengeTimes_s.RandWithinRange;
        var duration = 0f;

        yield return new WaitForSeconds(revengeDelay);

        while (duration < timeToRevenge)
        {
            duration += Time.deltaTime;
            Move();
            ThisVehicle.TurretRotation();

            yield return null; 
        }

        Target = null;
        SetState(NormalState);
    }

    public override IEnumerator StopStateUpdating()
    {
        var time = 0f;
        var stopStayTime = MiscTools.random.Next(5, 10) * 0.1f;

        while (true)
        {
            time += Time.deltaTime;
            ThisVehicle.TurretRotation();

            if (time < stopStayTime)
            {
                yield return null;
            }

            if (!ThisVehicle.TargetAimed)
            {
                SetState(NormalState);
                yield break;
            }

            yield return null;
        }
    }

    public override IEnumerator StuckStateUpdating()
    {
        var moveBackTime = BotSettings.timesToMoveBack_s.RandWithinRange;
        var time = 0f;

        var speed = Mathf.Sign(YAxisControl) * BotSettings.moveBackSpeeds_s.RandWithinRange;
        var rotSpeed = 1 * Mathf.Sign(ThisVehicle.transform.InverseTransformPoint(CurrentWaypointPos).x);

        while (time < moveBackTime)
        {
            time += Time.deltaTime;
            Move(-speed, rotSpeed);
            ThisVehicle.TurretRotation();

            yield return null;
        }

        while (!RotateToWaypoint())
        {
            yield return null;
        }

        SetState(NormalState);
    }

    public override IEnumerator TakingBonusStateUpdating()
    {
        while (true)
        {
            Move();
            ThisVehicle.TurretRotation();

            yield return null;
        }
    }

    public override void Move()
    {
        if (pathCorners.Count == 0)
        {
            return;
        }

        if (CurrentBehaviour == FighterBehaviour && BotTargetAimed && Target == ThisVehicle.Target)
        {
            SetState(StopState, true);
            return;
        }

        if (CurrentWaypointInd < 0 || Vector3.SqrMagnitude(ThisVehicle.transform.position - CurrentWaypointPos) < BotSettings.clearWaypointSqrDistance_s)
        {
            CurrentWaypointInd++;
            if (CurrentWaypointInd >= pathCorners.Count)
            {
                FindPositionToMove();
                FindPath();
                return;
            }

            CurrentWaypointPos = PathCorners[CurrentWaypointInd];
        }

        var dir = (CurrentWaypointPos - ThisVehicle.transform.position).normalized;
        var dotProd = Vector3.Dot(dir, ThisVehicle.transform.right);
        var waypointIsInFront = Vector3.Dot(dir, ThisVehicle.transform.forward) > 0;

        if(waypointIsInFront)
        {
            botXAxisControl = Mathf.Clamp(dotProd * 2, -1, 1);
            botYAxisControl = Mathf.Clamp01(1 - Mathf.Abs(dotProd));
        }
        else
        {
            botXAxisControl = dotProd > 0 ? 1 : -1;
            botYAxisControl = 0;
        }
        
        dir = botXAxisControl > 0 ? ThisVehicle.transform.right : -ThisVehicle.transform.right;
        var vehCenter = ThisVehicle.EntireBounds.center;
        var rayStart = vehCenter + ThisVehicle.transform.forward * bodyMeshBounds.extents.z;

        if (sidewaysLookOptimizer.AskPermission())
        {
            sidewaysLookOptimizer.BoolValue = Physics.Raycast(rayStart, dir,
                bodyMeshBounds.extents.x + bodyMeshBounds.extents.z, ThisVehicle.OthersLayerMask);
        }

        if (sidewaysLookOptimizer.BoolValue)
        {
            botXAxisControl = 0;
        }

        rayStart = vehCenter + ThisVehicle.transform.forward * bodyMeshBounds.extents.z;
        RaycastHit hit;

        if (forwardLookOptimizer.AskPermission())
        {
            forwardLookOptimizer.BoolValue = Physics.SphereCast(rayStart, bodyMeshBounds.extents.x,
                ThisVehicle.transform.forward, out hit, bodyMeshBounds.extents.z, ThisVehicle.OthersLayerMask);
        }

        if (forwardLookOptimizer.BoolValue)
        {
            botYAxisControl = 0;
        }
    }

    public override void OnStateChange() 
    {
        botFireButtonPressed = false;
    }

    protected override bool RotateToWaypoint()
    {
        Vector3 dir = (CurrentWaypointPos - ThisVehicle.transform.position).normalized;
        var dotProd = Vector3.Dot(dir, ThisVehicle.transform.right);
        botXAxisControl = Mathf.Sign(dotProd) * 1;
        botYAxisControl = 0;

        return Mathf.Abs(dotProd) < 0.1f;
    }

    public override void Move(float speed, float rotSpeed)
    {
        botYAxisControl = speed;
        botXAxisControl = rotSpeed;
    }

    public override IEnumerator PathUpdating()
    {
        var waiter = new WaitForSeconds(BotSettings.pathUpdateDelays_s.RandWithinRange);

        while (true)
        {
            if (Physics.CheckSphere(ThisVehicle.transform.position, 10, ThisVehicle.OthersLayerMask) || PathCorners.Count == 0)
            {
                ThisVehicle.SelfObstacle.enabled = false;
                yield return null;
                yield return null;
                FindPath();

                yield return null;
                yield return null;
                ThisVehicle.SelfObstacle.enabled = true;
            }
            
            yield return waiter;
        }
    }

    protected override bool FindPath()
    {
        path.ClearCorners();
        
        if(float.IsNaN(PositionToMove.x) || float.IsNaN(PositionToMove.y) || float.IsNaN(PositionToMove.z))
        {
            PositionToMove =
                Target == null
                ? FindRandomPointNearPosition(ThisVehicle.transform.position, 25f)
                : FindRandomPointNearPosition(Target.transform.position, NAVMESH_AGENT_RADIUS);
        }

        pathCorners.Clear();
        CurrentWaypointInd = -1;

        NavMesh.CalculatePath(ThisVehicle.transform.position, PositionToMove, walkableMask, path);
        if (path.status != NavMeshPathStatus.PathComplete)
            return false;

        int pathCornersCount = path.GetCornersNonAlloc(pathCornersArray);
        for (int i = 0; i < pathCornersCount; i++)
        {
            pathCorners.Add(pathCornersArray[i]);
        }

        return true;
    }

    public override void FindPositionToMove()
    {
        if (CurrentState != TakingBonusState)
        {
            if (Target != null)
            {
                PositionToMove = FindRandomPointNearPosition(Target.transform.position, 1f);
            }
            else if (GameData.IsTeamMode)
            {
                PositionToMove = ThisVehicle.IsMainsFriend
                    ? FindRandomPointNearPosition(BotDispatcher.EnemiesEpicenter, 15f)
                    : FindRandomPointNearPosition(BotDispatcher.FriendsEpicenter, 15f);
            }
            else
            {
                PositionToMove = FindRandomPointNearPosition(ThisVehicle.transform.position, 40f);
            }
        }
        else
        {
            PositionToMove = FindRandomPointNearPosition(ThisVehicle.transform.position, 30f);
            inaccessibleBonuses.Add(ClosestBonus);

            SetState(NormalState);
        }
    }

    public override Vector3 FindRandomPointNearPosition(Vector3 pos, float radius = 0f)
    {
        bool pointFound = false;
        int i = 0;
        NavMeshHit navMeshHit = new NavMeshHit();
        while (!pointFound && i++ < 10)
        {
            float zOffset = 1 - 2 * (float)MiscTools.random.NextDouble();
            float maxXOffset = Mathf.Sqrt(1 - zOffset * zOffset);
            float xOffset = 1 - 2 * (float)MiscTools.random.NextDouble() * maxXOffset;

            Vector3 sourcePosition = pos + Vector3.forward * radius * zOffset + Vector3.right * radius * xOffset; 
            pointFound = NavMesh.SamplePosition(sourcePosition, out navMeshHit, 15f, walkableMask);
        }

        return pointFound ? navMeshHit.position : SpawnPoints.instance.GetRandomPoint(ThisVehicle.data.teamId).position;
    }
}
