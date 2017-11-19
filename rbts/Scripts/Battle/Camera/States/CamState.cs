public abstract class CamState
{
    public BattleCamera camera;

    protected CamState(BattleCamera camera)
    {
        this.camera = camera;
    }

    public abstract void CamMotion();
}
