using System.Collections;
using UnityEngine; 

public class DummyTankBotAI : TankBotAI
{
    public DummyTankBotAI(TankController vehicle, BotDispatcher.BotBehaviours botBehaviour) : base(vehicle, botBehaviour)
    {
    }

    public override Vector3 FindRandomPointNearPosition(Vector3 pos, float radius)
    {
        return Vector3.zero;
    }

    public override void Move()
    {
    }

    public override void Move(float speed, float rotSpeed)
    {
    }

    public override void OnStateChange()
    {
    }

    protected override bool RotateToWaypoint()
    {
        return false;
    }

    public override IEnumerator PathUpdating()
    {
        yield break;
    }
}
