public class RegularState : CameraState
{
    public RegularState(BattleCamera camera, InputControllerType InputController) : base(camera, InputController)
    {
    }

    public override void CameraMotion()
    {
        camera.CommonMotion();
        InputController.RegularSpecificCameraMotion();
        camera.CheckJoystickZoomBtn();
    }

    public override void OnStateChanged()
    {
        camera.ZoomOut();
    }
}
