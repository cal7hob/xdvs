using UnityEngine;

public class MouseInputController : InputControllerType
{
    private const float mouseSensitivityQualifier = 1;

    public override float XAxis
    {
        get { return Input.GetAxis("Mouse X"); }
    }

    public override float YAxis
    {
        get { return Input.GetAxis("Mouse Y"); }
    }

    public override float SensitivityQualifier
    {
        get { return mouseSensitivityQualifier; }
    }

    public MouseInputController(BattleCamera camera) : base(camera)
    {
    }

    public override void RegularSpecificCameraMotion()
    {
        camera.MouseSpecificMotion();
    }

    public override void ZoomSpecificCameraMotion()
    {
        camera.MouseSpecificZoomMotion();
    }
}
