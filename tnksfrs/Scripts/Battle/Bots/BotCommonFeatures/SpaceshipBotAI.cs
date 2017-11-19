using System.Collections;
using UnityEngine;

public class SpaceshipBotAI : BotAI
{
    protected float botThrottleLevelInputAxis;

    public float ThrottleLevelInputLevelInputAxis { get { return botThrottleLevelInputAxis; } }

    public SpaceshipBotAI(VehicleController vehicleController) : base(vehicleController)
    {
    }

    public override Vector3 FindRandomPointNearPosition(Vector3 vehiclePos, int radius)
    {
        return Vector3.zero;
    }

    public override void EffectItself(VehicleEffect effect, bool positive)
    {

    }

    public override void Move(bool forwards = true)
    {
        var waypoint = XD.StaticContainer.BattleController.CurrentUnit.transform.position;

        var dir = (waypoint - ThisVehicle.transform.position).normalized;

        var dotProdX = Vector3.Dot(dir, ThisVehicle.transform.right);
        var dotProdY = Vector3.Dot(dir, ThisVehicle.transform.up);

        var waypointIsInFront = Vector3.Dot(dir, ThisVehicle.transform.forward) > 0;

        if (waypointIsInFront)
        {
            botXAxisControl = Mathf.Clamp(dotProdX, -1, 1);
            //botYAxisControl = Mathf.Clamp(dotProdY, -1, 1);
        }
        else
        {
            botXAxisControl = dotProdX > 0 ? 1 : -1;
            //botYAxisControl = dotProdY > 0 ? 1 : -1;
        }
    }

    public override void Move(float speed, float rotSpeed)
    {

    }

    public override void OnStateChange()
    {

    }

    public override IEnumerator NormalStateUpdating()
    {
        yield break;
    }

    public override IEnumerator CloseToSomebodyStateUpdating()
    {
        yield break;
    }

    public override IEnumerator OneShotStateUpdating()
    {
        yield break;
    }

    public override IEnumerator RevengeStateUpdating()
    {
        yield break;
    }

    public override IEnumerator RollbackStateUpdating()
    {
        yield break;
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
        yield break;
    }

    public override bool RotateToWaypoint()
    {
        return false;
    }

    public override IEnumerator Fire()
    {
        yield break;
    }

    public override IEnumerator PathUpdating()
    {
        yield break;
    }

    public override void FindPath()
    {
    }
}
