using System.Collections;
using UnityEngine;

public class AircraftBotAI : BotAI
{
    protected float botThrottleLevelInputAxis;
    protected AircraftBotController aircraftBotController;
    protected AircraftController thisAircraft;
    protected bool posToMoveIsInFront;
    protected Vector3 dirToTargetNormalized;

    private const float TARGET_POSITION_OFFSET_MAX = 300.0f;

    private Vector3 targetPositionOffset;

    public float ThrottleLevelInputLevelInputAxis { get { return botThrottleLevelInputAxis; } }

    public AircraftBotAI(VehicleController vehicleController) : base(vehicleController)
    {
        // Тут вываливается InvalidCastException от конструктора SpaceshipBotAI.
        // Пока что закомментил – всё равно нигде не используется.
        //helicopterBotController = (AircraftBotController) vehicleController;

        // Тоже временная мера. Используем это с проверкой на null.
        thisAircraft = ThisVehicle as AircraftController;
    }

    protected bool TargetIsInFront
    {
        get { return Target != null && Vector3.Dot(dirToTargetNormalized, ThisVehicle.transform.forward) > 0; }
    }

    protected virtual Vector3 TargetPosition 
    {
        get { return Map.OutOfMapWarningCol.ClosestPoint(Target.transform.position + targetPositionOffset); }
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
        var result = position + new Vector3(MiscTools.random.Next(-radius, radius), MiscTools.random.Next(-radius, radius), MiscTools.random.Next(-radius, radius));
        result = Map.OutOfMapWarningCol.ClosestPoint(result);
        return result;
    }

    private Vector3 FindRandomPointInsideCollider(Collider collider, float accuracy)
    {
        var xRange = (int)(collider.bounds.size.x * 0.5f * accuracy);
        var yRange = (int)(collider.bounds.size.y * 0.5f * accuracy);
        var zRange = (int)(collider.bounds.size.z * 0.5f * accuracy);

        Vector3 boundsPoint
            = collider.bounds.center
                + new Vector3(
                    x:  MiscTools.random.Next(-xRange, xRange),
                    y:  MiscTools.random.Next(-yRange, yRange),
                    z:  MiscTools.random.Next(-zRange, zRange));

        return collider.ClosestPoint(boundsPoint);
    }

    public override void Move(bool forwards = true)
    {
        if (ProfileInfo.IsBattleTutorial && BattleTutorial.Instance.CurrentBattleLesson == BattleTutorial.BattleLessons.fire)
        {
            FireLessonMove(); // Костыль.
            return;
        }

        if ((ThisVehicle.transform.position - PositionToMove).sqrMagnitude < BotSettings.clearWaypointSqrDistance_s)
        {
            FindPositionToMove();
        }

        if (Target != null && (CurrentState != TakingBonusState || CurrentState == RevengeState))
        {
            PositionToMove = TargetPosition;
            dirToTargetNormalized = (TargetPosition - ThisVehicle.transform.position).normalized;
        }

        var dirToMovePos = PositionToMove - ThisVehicle.transform.position;
        var dirToMovePosNormalized = dirToMovePos.normalized;

        var dotProdX = Vector3.Dot(dirToMovePosNormalized, ThisVehicle.transform.right);
        var dotProdY = Vector3.Dot(dirToMovePosNormalized, ThisVehicle.transform.up);

        botXAxisControl = dotProdX * 0.5f;
        botYAxisControl = -dotProdY * 0.5f;

        botThrottleLevelInputAxis = Mathf.Clamp01(dirToMovePos.magnitude / 1000.0f);
    }

    public override void Move(float speed, float rotSpeed) {}

    public override void OnStateChange() {}

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
        if (thisAircraft == null)
            yield break;

        while (true)
        {
            if (Target != null)
            {
                Vector3 targetDirection = Target.transform.position - ThisVehicle.transform.position;

                botFireButtonPressed
                    = TargetIsInFront &&
                      Target.IsAvailable &&
                      Vector3.Angle(ThisVehicle.transform.forward, targetDirection.normalized) < ThisVehicle.MaxShootAngle &&
                      targetDirection.sqrMagnitude < ThisVehicle.MaxAimDistance * ThisVehicle.MaxAimDistance;
            }
            else
            {
                botFireButtonPressed = false;
            }

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
        return true;
    }

    public override void FindPositionToMove()
    {
        if (ProfileInfo.IsBattleTutorial && BattleTutorial.Instance.CurrentBattleLesson == BattleTutorial.BattleLessons.fire) // Костыль.
            return;

        if (CurrentState == TakingBonusState)
            return;

        if (GameData.IsTeamMode)
        {
            PositionToMove = ThisVehicle.IsMainsFriend
                ? FindRandomPointNearPosition(BotDispatcher.EnemiesEpicenter, 150)
                : FindRandomPointNearPosition(BotDispatcher.FriendsEpicenter, 150);
        }
        else
        {
            PositionToMove = FindRandomPointInsideCollider(Map.OutOfMapWarningCol, 0.66f);
        }
    }

    protected override void CheckIfFireNeed()
    {
        base.CheckIfFireNeed();

        if (thisAircraft == null)
            return;

        if (thisAircraft.IsRequireSecondaryFire && thisAircraft.Target != null)
            thisAircraft.UseSecondaryWeapon(GunShellInfo.ShellType.Missile_SACLOS);
    }

    protected override void OnTargetChanged(VehicleController target)
    {
        if (target == null)
            return;

        targetPositionOffset = Random.onUnitSphere * Random.Range(1, 2) * TARGET_POSITION_OFFSET_MAX;
        targetPositionOffset.z = -Mathf.Abs(targetPositionOffset.z);
    }

    private void FireLessonMove()
    {
        botXAxisControl = 0;
        botYAxisControl = 0;
        botThrottleLevelInputAxis = 0;
    }
}
