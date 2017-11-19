public abstract class CameraState
{
    protected BattleCamera camera;
    protected InputControllerType InputController;

    protected CameraState(BattleCamera camera, InputControllerType InputController)
    {
        this.camera = camera;
        this.InputController = InputController;
    }

    public abstract void CameraMotion();
    public abstract void OnStateChanged();
}
