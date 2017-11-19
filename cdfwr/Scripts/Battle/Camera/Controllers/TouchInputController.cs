public class TouchInputController : InputControllerType
{
    private const float touchSensitivityQualifier = 0.1f;

    public override float XAxis
    {
        get { return TouchReceiver.DeltaTouchPosition.x; }
    }

    public override float YAxis
    {
        get { return TouchReceiver.DeltaTouchPosition.y; }
    }

    public override float SensitivityQualifier
    {
        get { return touchSensitivityQualifier; }
    }

    public TouchInputController(BattleCamera camera) : base(camera)
    {
    }

    public override void RegularSpecificCameraMotion()
    {
        camera.TouchSpecificMotion();
    }

    public override void ZoomSpecificCameraMotion()
    {
        camera.TouchSpecificZoomMotion();
    }
}
