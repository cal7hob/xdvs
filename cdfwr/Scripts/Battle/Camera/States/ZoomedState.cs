public class ZoomedState : CameraState
{
    public ZoomedState(BattleCamera camera, InputControllerType InputController) : base(camera, InputController)
    {
    }

    public override void CameraMotion()
    {
        InputController.ZoomSpecificCameraMotion();
        camera.CheckJoystickZoomBtn();
    }

    public override void OnStateChanged()
    {
    } 
}
