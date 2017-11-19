public class HangarCamModuleApproachingState : HangarCamState
{
    public HangarCamModuleApproachingState(HangarCameraController hangarCamWrapper) : base(hangarCamWrapper)
    {
    }

    public override void Move()
    {
        hangarCamWrapper.ModuleApproachingUpdate();
    }

    public override void OnStateChange()
    {
        hangarCamWrapper.OnSwitchToModuleApproachingState();
    }
}
