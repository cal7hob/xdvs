public class ZoomedState : CameraState
{
    public ZoomedState(BattleCamera camera) : base(camera)
    {
    }

    public override void CameraMotion()
    {
        camera.ZoomMotion();
        camera.CheckJoystickZoomBtn();
    }

    public override void OnStateChanged()
    {
    }
}
