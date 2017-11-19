class HangarCamIdleState : HangarCamState
{
    public HangarCamIdleState(HangarCameraController hangarCamWrapper) : base(hangarCamWrapper)
    {
    }

    public override void Move()
    {
        hangarCamWrapper.IdleUpdate();
    }

    public override void OnStateChange()
    {
    }
}
