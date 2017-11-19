using System.Collections;
using UnityEngine;
using XD;

public class TankBotAI : BotAI
{
    protected LayerMask                 walkableMask = 1 << UnityEngine.AI.NavMesh.GetAreaFromName("Walkable");
    public UnityEngine.AI.NavMeshPath   path = new UnityEngine.AI.NavMeshPath();

    public TankBotAI(VehicleController vehicle) : base(vehicle)
    {
    }

    public override IEnumerator NormalStateUpdating()
    {
        while (true)
        {
            yield return null;

            CurrentBehaviour.Moving();
            //ThisVehicle.TurretRotation();
        }
    }

    public override IEnumerator CloseToSomebodyStateUpdating()
    {
        var elapsedTime = 0f;
        float goalTime = MiscTools.random.Next(100, 300) * 0.01f;

        while (true)
        {
            elapsedTime += Time.deltaTime;

            Move();

            if (ClosestEnemyVehicle != null)
            {
                var sqrDistToClosestVehicle = Vector3.SqrMagnitude(ThisVehicle.transform.position - ClosestEnemyVehicle.transform.position);

                SetSpeed(Mathf.Clamp01((sqrDistToClosestVehicle - ThisVehicle.SqrBodyZSize) / ThisVehicle.SqrBodyZSize));
                SetRotation(botXAxisControl * botYAxisControl);

                if (sqrDistToClosestVehicle > StopDistanceToOtherVehicles && elapsedTime > goalTime)
                {
                    SetState(NormalState);
                    yield break;
                }
            }
            else
            {
                SetState(NormalState);
                yield break;
            }

            yield return null;
        }
    }

    public override IEnumerator OneShotStateUpdating()
    {
        if (!ThisVehicle.IsAvailable)
        {
            yield break;
        }

        while (CurrentBehaviour.Target != null)
        {
            CurrentBehaviour.Moving();

            yield return null;
        }

        SetState(NormalState);
    }

    public override IEnumerator RevengeStateUpdating()
    {
        var revengeDelay = Random.Range(BotSettings.minRevengeDelay_s, BotSettings.maxRevengeDelay_s);

        yield return new WaitForSeconds(revengeDelay);

        while (CurrentBehaviour.Target != null)
        {
            CurrentBehaviour.Moving();

            yield return null;
        }

        SetState(NormalState);
    }

    public override IEnumerator RollbackStateUpdating()
    {
        var moveBackTime = Random.Range(1, 5);
        var time = 0f;
        var speed = -Mathf.Sign(YAxisControl) * Random.Range(0.3f, 1);
        var rotSpeed = Random.Range(-1, 1);

        while (time < moveBackTime)
        {
            time += Time.deltaTime;
            Move(speed, rotSpeed);

            yield return new WaitForEndOfFrame();
        }

        StopVehicle();
        yield return new WaitForSeconds(Random.Range(3, 5));

        SetState(NormalState);
    }

    public override IEnumerator StopStateUpdating()
    {
        while (true)
        {
            if (!TargetAimed || CurrentBehaviour.Target != AimPoint.Target)
            {
                SetState(NormalState);
                yield break;
            }

            yield return null;
        }
    }

    public override IEnumerator StuckStateUpdating()
    {
        var moveBackTime = Random.Range(0.5f, 0.75f);
        var time = 0f;

        var speed = Mathf.Sign(YAxisControl) * Random.Range(0.4f, 1);
        var rotSpeed = 1 * Mathf.Sign(ThisVehicle.transform.InverseTransformPoint(CurrentWaypointPos).x);

        while (time < moveBackTime)
        {
            time += Time.deltaTime;
            Move(-speed, rotSpeed);
            yield return null;
        }

        StopVehicle();

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
            CurrentBehaviour.Moving();
            yield return null;
        }
    }

    public override void Move(bool forwards = true)
    {
        if (path.corners.Length == 0)
        {
            return;
        }

        if (CurrentWaypoint > path.corners.Length - 1 || CurrentWaypoint < 0)
        {
            CurrentBehaviour.StartFindingPosition();
            CurrentBehaviour.StartPathUpdating();
            CurrentWaypoint = 0;
            botYAxisControl = 0;
            return;
        }

        CurrentWaypointPos = path.corners[CurrentWaypoint];
        Vector3 position = ThisVehicle.Transform.position;
        Vector3 dir = (CurrentWaypointPos - position).normalized;
        var dotProd = Vector3.Dot(dir, ThisVehicle.transform.right);
        var waypointIsInFront = Vector3.Dot(dir, ThisVehicle.transform.forward) > 0;

        if (waypointIsInFront)
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

        Vector3 right = ThisVehicle.transform.right;
        Vector3 forward = ThisVehicle.transform.forward;
        Vector3 rayDir = botXAxisControl > 0 ? right : -right;
        Vector3 rayStart = ThisVehicle.transform.TransformPoint(ThisVehicle.BodyMeshBounds.center) + forward * ThisVehicle.BodyMeshBounds.extents.z;

        if (Physics.Raycast(rayStart, rayDir, ThisVehicle.BodyMeshBounds.extents.x + ThisVehicle.BodyMeshBounds.extents.z, ThisVehicle.OthersLayerMask))
        {
            botXAxisControl = 0;
        }

        if (Vector3.SqrMagnitude(position - CurrentWaypointPos) < BotSettings.clearWaypointDistance_s)
        {
            CurrentWaypoint++;
        }
    }

    public override void Draw()
    {
        if (ThisVehicle == null || !ThisVehicle.IsAvailable)
        {
            return;
        }

        Gizmos.color = Color.yellow;
        Gizmos.DrawSphere(CurrentWaypointPos, 2f);
#if UNITY_EDITOR
        UnityEditor.Handles.Label(CurrentWaypointPos, "Current Way Point".FormatString(Color.yellow), GUIStyle.none);
#endif
        Debug.DrawLine(ThisVehicle.Turret.position, CurrentWaypointPos, Color.yellow);
        

        if (CurrentBehaviour.Target != null)
        {
            Vector3 additional = new Vector3();
            if (ThisVehicle.Team == StaticContainer.GameManager.Team)
            {
                additional = Vector3.up * 2;
            }

            Debug.DrawLine(ThisVehicle.ShotPoint.position + additional, CurrentBehaviour.Target.Position + additional, ThisVehicle.Team == StaticContainer.GameManager.Team ? Color.cyan : Color.blue);
            Gizmos.color = ThisVehicle.Team == StaticContainer.GameManager.Team ? Color.cyan : Color.blue;
            Gizmos.DrawSphere(CurrentBehaviour.Target.Position, 2f);
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
        botFireButtonPressed = false;
    }

    public override void EffectItself(VehicleEffect effect, bool positive)
    {
    }

    public override IEnumerator PathUpdating()
    {
        while (ThisVehicle != null && ThisVehicle.SelfObstacle != null && ThisVehicle.IsAvailable)
        {
            if (Physics.CheckSphere(ThisVehicle.transform.position, 10, ThisVehicle.OthersLayerMask) || path.corners.Length == 0)
            {
                ThisVehicle.SelfObstacle.enabled = false;
                yield return null;
                yield return null;
                FindPath();

                yield return null;
                yield return null;
                ThisVehicle.SelfObstacle.enabled = true;
            }

            yield return new WaitForSeconds(0.3f);
        }
    }

    public override void FindPath()
    {
        if (!ThisVehicle.IsAvailable)
        {
            return;
        }

        path.ClearCorners();
        CurrentWaypoint = 0;

        if (float.IsNaN(CurrentBehaviour.PositionToMove.x) || float.IsNaN(CurrentBehaviour.PositionToMove.y) || float.IsNaN(CurrentBehaviour.PositionToMove.z))
        {
            CurrentBehaviour.SetPositionToGo(FindRandomPointNearPosition(ThisVehicle.Transform.position), findPath: false);
        }

        UnityEngine.AI.NavMesh.CalculatePath(ThisVehicle.Transform.position + ThisVehicle.Transform.forward * 5f, CurrentBehaviour.PositionToMove, walkableMask, path);
    }

    public override Vector3 FindRandomPointNearPosition(Vector3 vehiclePos, int radius = 10)
    {
        bool pointFound = false;
        int i = 0;

        UnityEngine.AI.NavMeshHit navMeshHit = new UnityEngine.AI.NavMeshHit();

        while (!pointFound && i++ < 10)
        {
            Vector3 sourcePosition = vehiclePos + ThisVehicle.transform.forward * MiscTools.random.Next(0, radius) + ThisVehicle.transform.right * MiscTools.random.Next(-radius, radius) + ThisVehicle.transform.up * MiscTools.random.Next(0, radius);
            pointFound = UnityEngine.AI.NavMesh.SamplePosition(sourcePosition, out navMeshHit, 20, walkableMask);
        }

        return pointFound ? navMeshHit.position : vehiclePos;
    }
}