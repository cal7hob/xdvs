public class RegularControlledState : CameraState
{
    public RegularControlledState(BattleCamera camera) : base(camera)
    {
    }

    public override void CameraMotion()
    {
        camera.CommonMotion();
        camera.CamRegularMotion();
        camera.CheckJoystickZoomBtn();
    }

    public override void OnStateChanged()
    {
        camera.ZoomOut();
    }
}
