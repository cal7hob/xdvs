using UnityEngine;

public class HumanCamera : GroundCamera
{
    public override Vector3 DeltaEulerAngles
    {
        get { return mouseSensivityQualifier * (Vector3.up * CameraXAxis - Vector3.right * CameraYAxis); }
    }

    public override bool MouseLeftBtnDown
    {
        get { return Input.GetMouseButton(0); }
    }
}
