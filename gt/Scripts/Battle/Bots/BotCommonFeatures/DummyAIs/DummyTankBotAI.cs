using System.Collections;
using UnityEngine; 

public class DummyTankBotAI : TankBotAI
{
    public DummyTankBotAI(TankController vehicle) : base(vehicle)
    {
    }

    public override Vector3 FindRandomPointNearPosition(Vector3 pos, int radius = 10)
    {
        return Vector3.zero;
    }

    public override void Move(bool forwards = true)
    {
    }

    public override void Move(float speed, float rotSpeed)
    {
    }

    public override void OnStateChange()
    {
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
}
