public abstract class CameraState
{
    public BattleCamera camera;

    protected CameraState(BattleCamera camera)
    {
        this.camera = camera;
    }

    public abstract void CameraMotion();
    public abstract void OnStateChanged();
}
