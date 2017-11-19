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
            CurrentState.FindPositionToMove();
            yield return new WaitForSeconds(CurrentState.FindingPosDelay);
        }
    } 

    public override IEnumerator CheckingIfStuck()
    {
        yield return null;

        while (true)
        {
            var savedPos = ThisVehicle.transform.position;
            var savedRotation = ThisVehicle.transform.rotation;

            yield return new WaitForSeconds(CurrentBehaviour.BotSettings.TimesToStuck.RandWithinRange);

            if (!ThisVehicle.IsAvailable)
                yield return null;

            if ((ThisVehicle.transform.position - savedPos).sqrMagnitude < CurrentBehaviour.BotSettings.StuckVelocitySqrMagintude &&
                Quaternion.Angle(savedRotation, ThisVehicle.transform.rotation) < CurrentBehaviour.BotSettings.StuckAngle && !collisionAlert)
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
            ThisVehicle.turretController.TurretRotation();

            yield return null;
        }
    }

    public override IEnumerator OneShotStateUpdating()
    {
        while (true)
        {
            Move();
            ThisVehicle.turretController.TurretRotation();

            yield return null;
        }
    }

    public override IEnumerator RevengeStateUpdating()
    {
        var duration = RevengeState.Duration;
        var timeToRevenge = RevengeState.TimeToRevenge;

        if (duration >= timeToRevenge || (Mathf.Approximately(RevengeState.Duration, 0) && Mathf.Approximately(RevengeState.TimeToRevenge, 0)))
        {
            RevengeState.Init();
            yield return new WaitForSeconds(RevengeState.Delay);
        }       

        while (duration < timeToRevenge)
        {
            if (CurrentState != RevengeState)
            {
                yield break;
            }

            duration += Time.deltaTime;
            Move();
            ThisVehicle.turretController.TurretRotation();

            yield return null; 
        }

        Target = null;
        SetState(NormalState);
    }

    public override IEnumerator StopStateUpdating()
    {
        var stayingTime = 0f;
        var timeToStay = MiscTools.random.Next(600, 800) * 0.01f;

        while (true)
        {
            stayingTime += Time.deltaTime;
            ThisVehicle.turretController.TurretRotation();

            if (!ThisVehicle.TargetAimed && stayingTime > timeToStay)
            {
                SetState(NormalState);
                yield break;
            }

            yield return null;
        }
    }

    public override IEnumerator StuckStateUpdating()
    {
        var moveBackTime = CurrentBehaviour.BotSettings.TimesToMoveBack.RandWithinRange;
        var time = 0f;

        var speed = Mathf.Sign(YAxisControl) * CurrentBehaviour.BotSettings.MoveBackSpeeds.RandWithinRange;
        var rotSpeed = 1 * Mathf.Sign(ThisVehicle.transform.InverseTransformPoint(CurrentWaypointPos).x);

        while (time < moveBackTime)
        {
            time += Time.deltaTime;
            Move(-speed, rotSpeed);
            ThisVehicle.turretController.TurretRotation();

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
            ThisVehicle.turretController.TurretRotation();

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
            botXAxisControl = 0;
            botYAxisControl = 0;
            return;
        }

        if (CurrentWaypoint > Path.corners.Length - 1 || CurrentWaypoint < 0)
        {
            CurrentWaypoint = 0;
            FindingPositionController.Restart();

            return;
        }

        CurrentWaypointPos = Path.corners[CurrentWaypoint];

        if (Vector3.SqrMagnitude(ThisVehicle.transform.position - CurrentWaypointPos) < CurrentBehaviour.BotSettings.ClearWaypointSqrDistance)
        {
            CurrentWaypoint++;
        }

        var dirToWaypoint = (CurrentWaypointPos - ThisVehicle.transform.position).normalized;
        var dotProd = Vector3.Dot(dirToWaypoint, ThisVehicle.transform.right);
        var waypointIsInFront = Vector3.Dot(dirToWaypoint, ThisVehicle.transform.forward) > 0;

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

        if (ClosestVehicle != null)
        {
            var dirToClosestVeh = ClosestVehicle.transform.position - ThisVehicle.transform.position;

            var curDistToClosestVeh = Vector3.SqrMagnitude(dirToClosestVeh);

            if (curDistToClosestVeh < StopSqrDistanceToOtherVehicles)
            {
                var qual = Mathf.Clamp01(1 - Vector3.Dot(ThisVehicle.transform.forward, dirToClosestVeh.normalized));
                botXAxisControl = Mathf.Clamp(botXAxisControl * qual * 15, -1, 1); // чтобы все-таки хоть как-то поворачивался
                botYAxisControl *= qual;
                collisionAlert = true;
            }
            else
            {
                collisionAlert = false;
            }
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
        botFireButtonPressed = true;
        yield return null;
        yield return null;
        botFireButtonPressed = false;
    }

    public override IEnumerator PathUpdating()
    {
        var waiter = new WaitForSeconds(CurrentBehaviour.BotSettings.PathUpdateDelays.RandWithinRange);

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

    public override void MoveToBonus()
    {
        if (ClosestBonus == null)
        {
            SetState(NormalState);
            return;
        }

        PositionToMove = ClosestBonus.transform.position;

        if (!FindPath())
        {
            PositionToMove = FindRandomPointNearPosition(ThisVehicle.transform.position, 30);

            if (!inaccessibleBonuses.Contains(ClosestBonus))
            {
                inaccessibleBonuses.Add(ClosestBonus);
            }

            SetState(NormalState);
        }
    }

    public override Vector3 FindRandomPointNearPosition(Vector3 pos, int radius = 25)
    {
        var pointFound = false;
        var i = 0;
        var navMeshHit = new UnityEngine.AI.NavMeshHit();

        while (!pointFound && i++ < 10)
        {
            var t = ThisVehicle.transform;
            var sourcePosition = pos + t.forward * MiscTools.random.Next(0, radius) + t.right * MiscTools.random.Next(-radius, radius); 
            pointFound = UnityEngine.AI.NavMesh.SamplePosition(sourcePosition, out navMeshHit, 60, walkableMask);
        }

        return pointFound ? navMeshHit.position : SpawnPoints.instance.GetRandomPoint(ThisVehicle.data.teamId).position; ;
    }
}
