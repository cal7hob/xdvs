public class HangarCamMovingBackState : HangarCamState
{
    private bool isMovedBackToOrbit;
    private bool isDefaultUpDirectionSet;

    public HangarCamMovingBackState(HangarCameraController hangarCamWrapper) : base(hangarCamWrapper)
    {
    }

    public override bool CanChangeState
    {
        get { return isMovedBackToOrbit && isDefaultUpDirectionSet; }
    }

    public override void Move()
    {
        isMovedBackToOrbit = hangarCamWrapper.MoveBackToOrbit();
        isDefaultUpDirectionSet = hangarCamWrapper.SettingDefaultUpDirection();

        hangarCamWrapper.MovingBackUpdate();
    }

    public override void OnStateChange()
    {
        hangarCamWrapper.OnSwitchToMoveBackState();
    }
}
