public class ShowKillerState : CamState
{
    public ShowKillerState(BattleCamera camera) : base(camera)
    {
    }

    public override void CamMotion()
    {
        camera.ShowKillerMotion(); 
    }  
}
