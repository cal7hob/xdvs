public class HangarCamIdleWithoutControlState : HangarCamState
{
    public HangarCamIdleWithoutControlState(HangarCameraController hangarCamWrapper) : base(hangarCamWrapper)
    {
    }

    public override void Move()
    {
        hangarCamWrapper.RotateCamera();
        hangarCamWrapper.ApproximateToDefaultRotationSpeed();
    }

    public override void OnStateChange()
    {
    }
}
