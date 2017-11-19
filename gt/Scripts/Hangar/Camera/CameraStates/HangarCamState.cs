public abstract class HangarCamState
{
    protected HangarCameraController hangarCamWrapper;

    public virtual bool CanChangeState { get { return true; } }

    protected HangarCamState(HangarCameraController hangarCamWrapper)
    {
        this.hangarCamWrapper = hangarCamWrapper;
    }

    public abstract void Move();
    public abstract void OnStateChange();
}
