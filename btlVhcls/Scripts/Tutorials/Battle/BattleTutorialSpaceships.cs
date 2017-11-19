using UnityEngine;

public class BattleTutorialSpaceships : BattleTutorialFlights
{
    protected override void SetUpArrow()
    {
        arrow.parent = FlightCameraController.FlightCamInstance.gameObject.GetComponentInChildren<Camera>().transform;

        arrow.localPosition = ArrowPosition;
        arrow.localRotation = Quaternion.LookRotation(Vector3.forward);
    }
}
