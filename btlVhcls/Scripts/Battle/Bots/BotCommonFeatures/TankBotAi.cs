using System.Collections;
using UnityEngine;
using XDevs.LiteralKeys;

public class TankBotAI : BotAI
{
    protected LayerMask walkableMask = 1 << UnityEngine.AI.NavMesh.GetAreaFromName("Walkable");

    public UnityEngine.AI.NavMeshPath Path = new UnityEngine.AI.NavMeshPath();

    public TankBotAI(VehicleController vehicle) : base(vehicle) { }

    protected override IEnumerator FindingPosition()
    {
        while (true)
        {
            FindPositionToMove();
            yield return new WaitForSeconds(CurrentBehaviour.FindingPosDelay);
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

    public override void MyBotUpdate()
    {
        base.MyBotUpdate();
        ThisVehicle.MovePlayer();
    }

    public override void Move(bool forwards = true)
    {
        if (Path.corners.Length == 0)
        {
            return;
        }

        if (CurrentBehaviour == FighterBehaviour && BotTargetAimed && Target == ThisVehicle.Target)
        {
            SetState(StopState, true);
        }

        if (CurrentWaypoint > Path.corners.Length - 1 || CurrentWaypoint < 0)
        {
            CurrentWaypoint = 0;
            FindPositionToMove();
            FindPath();

            return;
        }

        CurrentWaypointPos = Path.corners[CurrentWaypoint];

        if (Vector3.SqrMagnitude(ThisVehicle.transform.position - CurrentWaypointPos) < BotSettings.clearWaypointSqrDistance_s)
        {
            CurrentWaypoint++;
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

        if (!forwards)
        {
            botXAxisControl = -botXAxisControl;
            botYAxisControl = -botYAxisControl;
        }

        dir = botXAxisControl > 0 ? ThisVehicle.transform.right : -ThisVehicle.transform.right;
        var vehCenter = ThisVehicle.transform.TransformPoint(ThisVehicle.BodyMeshBounds.center);
        var rayStart = vehCenter + ThisVehicle.transform.forward * ThisVehicle.BodyMeshBounds.extents.z;

        if (Physics.Raycast(rayStart, dir, ThisVehicle.BodyMeshBounds.extents.x + ThisVehicle.BodyMeshBounds.extents.z, ThisVehicle.OthersLayerMask))
        {
            botXAxisControl = 0;
        }

        rayStart = vehCenter + ThisVehicle.transform.forward * ThisVehicle.BodyMeshBounds.extents.z;
        RaycastHit hit;

        if (Physics.SphereCast(rayStart, ThisVehicle.BodyMeshBounds.extents.x, ThisVehicle.transform.forward, out hit, ThisVehicle.BodyMeshBounds.extents.z, ThisVehicle.OthersLayerMask))
        {
            botYAxisControl = 0;
        }
    }

    public override void OnStateChange() 
    {
        botFireButtonPressed = false;
    }

    public override bool RotateToWaypoint()
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

    public override IEnumerator Fire()
    {
        yield return new WaitForSeconds(CurrentBehaviour.FireDelay);

        botFireButtonPressed = true;
        yield return null;
        yield return null;
        botFireButtonPressed = false;
    }

    public override IEnumerator PathUpdating()
    {
        var waiter = new WaitForSeconds(BotSettings.pathUpdateDelays_s.RandWithinRange);

        while (true)
        {
            if (Physics.CheckSphere(ThisVehicle.transform.position, 10, ThisVehicle.OthersLayerMask) || Path.corners.Length == 0)
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

    public override bool FindPath()
    {
        Path.ClearCorners();
        CurrentWaypoint = 0;

        if(float.IsNaN(PositionToMove.x) || float.IsNaN(PositionToMove.y) || float.IsNaN(PositionToMove.z))
        {
            PositionToMove = FindRandomPointNearPosition(Target == null ? 
                ThisVehicle.transform.position : Target.transform.position);
        }

        UnityEngine.AI.NavMesh.CalculatePath(ThisVehicle.transform.position, PositionToMove, walkableMask, Path);

        if (Path.corners.Length > 0)
        {
            Path.corners[0] = ThisVehicle.transform.position;
        }

        return Path.status == UnityEngine.AI.NavMeshPathStatus.PathComplete;
    }

    public override void FindPositionToMove()
    {
        if (CurrentState != TakingBonusState)
        {
            if (Target != null)
            {
                PositionToMove = FindRandomPointNearPosition(Target.transform.position);
            }
            else if (GameData.IsTeamMode)
            {
                PositionToMove = ThisVehicle.IsMainsFriend
                    ? FindRandomPointNearPosition(BotDispatcher.EnemiesEpicenter, 30)
                    : FindRandomPointNearPosition(BotDispatcher.FriendsEpicenter, 30);
            }
            else
            {
                PositionToMove = FindRandomPointNearPosition(ThisVehicle.transform.position, radius: 40);
            }

            FindPath();
        }
        else if (!FindPath())
        {
            PositionToMove = FindRandomPointNearPosition(ThisVehicle.transform.position, 30);

            if (!inaccessibleBonuses.Contains(ClosestBonus))
            {
                inaccessibleBonuses.Add(ClosestBonus);
            }

            SetState(NormalState);
        }
    }

    public override Vector3 FindRandomPointNearPosition(Vector3 position, int radius = 25)
    {
        var pointFound = false;
        var i = 0;
        var navMeshHit = new UnityEngine.AI.NavMeshHit();

        while (!pointFound && i++ < 10)
        {
            var sourcePosition = position + ThisVehicle.transform.forward * MiscTools.random.Next(0, radius) + ThisVehicle.transform.right * MiscTools.random.Next(-radius, radius); 
            pointFound = UnityEngine.AI.NavMesh.SamplePosition(sourcePosition, out navMeshHit, 60, walkableMask);
        }

        return pointFound ? navMeshHit.position : SpawnPoints.instance.GetRandomPoint(ThisVehicle.data.teamId).position; ;
    }
}
