// хз возможно лишний класс

using System.Collections;
using UnityEngine;

public class SpaceshipBotAI : AircraftBotAI 
{
    public SpaceshipBotAI(VehicleController vehicleController) : base(vehicleController)
    {
    }

    protected override IEnumerator Shooting()
    {
        if (ProfileInfo.IsBattleTutorial)
            yield break;

        while (true)
        {
            botFireButtonPressed = TargetIsInFront && Vector3.Angle(ThisVehicle.transform.forward, dirToTargetNormalized) < 4 && Target.IsAvailable;
            yield return null;
        }
    }
}
