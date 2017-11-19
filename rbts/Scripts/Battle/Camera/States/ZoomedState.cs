class ZoomedState : CamState
{
    public ZoomedState(BattleCamera camera) : base(camera)
    {
    }

    public override void CamMotion()
    {
        camera.ZoomMotion();
    }
}
