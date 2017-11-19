using System.Collections;
using UnityEngine;

public class HelicopterBotAI : BotAI
{
    public UnityEngine.AI.NavMeshPath Path = new UnityEngine.AI.NavMeshPath();

    protected HelicopterBotController helicopterBotController;
    protected HelicopterController thisHelicopter;
    protected bool posToMoveIsInFront;
    protected Vector3 dirToTargetNormalized;
    protected LayerMask walkableMask = 1 << UnityEngine.AI.NavMesh.GetAreaFromName("Walkable");

    private Vector3 targetPositionCorrection;

    public HelicopterBotAI(VehicleController vehicleController) : base(vehicleController)
    {
        thisHelicopter = ThisVehicle as HelicopterController;
    }

    protected bool TargetIsInFront
    {
        get { return Target != null && Vector3.Dot(dirToTargetNormalized, ThisVehicle.transform.forward) > 0; }
    }

    protected virtual Vector3 TargetPosition
    {
        get
        {
            return Target.transform.position + Target.transform.TransformVector(targetPositionCorrection);
        }
    }

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
        yield break;
    }

    public override Vector3 FindRandomPointNearPosition(Vector3 position, int radius = 10)
    {
        return position + new Vector3(MiscTools.random.Next(-radius, radius), MiscTools.random.Next(-radius, radius), MiscTools.random.Next(-radius, radius));
    }

    public override void Move(bool forwards = true)
    {
        if (Path.corners.Length == 0)
            return;

        if (CurrentBehaviour == FighterBehaviour && BotTargetAimed && Target == ThisVehicle.Target)
            SetState(StopState, true);

        if (CurrentWaypoint > Path.corners.Length - 1 || CurrentWaypoint < 0)
        {
            CurrentWaypoint = 0;

            FindPositionToMove();
            FindPath();

            return;
        }

        CurrentWaypointPos = Path.corners[CurrentWaypoint];

        if (Vector3.SqrMagnitude(ThisVehicle.transform.position - CurrentWaypointPos) < BotSettings.clearWaypointSqrDistance_s)
            CurrentWaypoint++;

        var dirToMovePos = CurrentWaypointPos - ThisVehicle.transform.position;
        var dirToMovePosNormalized = dirToMovePos.normalized;

        var dotProdX = Vector3.Dot(dirToMovePosNormalized, ThisVehicle.transform.right);
        var dotProdY = Vector3.Dot(dirToMovePosNormalized, ThisVehicle.transform.up);

        posToMoveIsInFront = Vector3.Dot(dirToMovePosNormalized, ThisVehicle.transform.forward) > 0;

        if (posToMoveIsInFront)
        {
            botXAxisControl = dotProdX;
            botYAxisControl = -dotProdY;
        }
        else
        {
            botXAxisControl = dotProdX > 0 ? 1 : -1;
            botYAxisControl = dotProdY > 0 ? -1 : 1;
        }
    }

    public override void Move(float speed, float rotSpeed) { }

    public override void OnStateChange() { }

    public override IEnumerator NormalStateUpdating()
    {
        while (true)
        {
            Move();
            yield return null;
        }
    }

    public override IEnumerator OneShotStateUpdating()
    {
        while (true)
        {
            Move();
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
            yield return null;
        }

        Target = null;

        SetState(NormalState);
    }

    public override IEnumerator StopStateUpdating()
    {
        yield break;
    }

    public override IEnumerator StuckStateUpdating()
    {
        yield break;
    }

    public override IEnumerator TakingBonusStateUpdating()
    {
        while (true)
        {
            Move();
            yield return null;
        }
    }

    public override bool RotateToWaypoint()
    {
        return false;
    }

    public override IEnumerator Fire()
    {
        yield break;
    }

    protected override IEnumerator Shooting()
    {
        if (thisHelicopter == null)
            yield break;

        while (true)
        {
            botFireButtonPressed
                = TargetIsInFront &&
                  Vector3.Angle(ThisVehicle.transform.forward, dirToTargetNormalized) < 4 &&
                  Target.IsAvailable;

            yield return new WaitForSeconds(CurrentBehaviour.FireDelay);
        }
    }

    public override IEnumerator PathUpdating()
    {
        while (true)
        {
            FindPositionToMove();
            yield return new WaitForSeconds(CurrentBehaviour.FindingPosDelay);
        }
    }

    public override bool FindPath()
    {
        Path.ClearCorners();

        CurrentWaypoint = 0;

        if (float.IsNaN(PositionToMove.x) || float.IsNaN(PositionToMove.y) || float.IsNaN(PositionToMove.z))
            PositionToMove = FindRandomPointNearPosition(Target == null ? ThisVehicle.transform.position : Target.transform.position);

        UnityEngine.AI.NavMesh.CalculatePath(ThisVehicle.transform.position, PositionToMove, walkableMask, Path);

        if (Path.corners.Length > 0)
            Path.corners[0] = ThisVehicle.transform.position;

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
                inaccessibleBonuses.Add(ClosestBonus);

            SetState(NormalState);
        }
    }

    protected override void CheckIfFireNeed()
    {
        base.CheckIfFireNeed();

        if (thisHelicopter == null)
            return;

        if (thisHelicopter.IsRequireSecondaryFire)
            thisHelicopter.UseSecondaryWeapon(GunShellInfo.ShellType.Missile_SACLOS);
    }

    protected override void OnTargetChanged(VehicleController target)
    {
        if (target == null)
            return;

        targetPositionCorrection = Random.onUnitSphere * Random.Range(1, 2) * 250.0f;
        targetPositionCorrection.z = -Mathf.Abs(targetPositionCorrection.z);
    }
}
