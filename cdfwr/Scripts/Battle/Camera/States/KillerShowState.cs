public class KillerShowState : CameraState
{
    public KillerShowState(BattleCamera camera, InputControllerType InputController) : base(camera, InputController)
    {
    }

    public override void CameraMotion()
    {
        camera.CommonMotion();
        camera.FollowKillerView();
    }

    public override void OnStateChanged()
    {
    }
}
