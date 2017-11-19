using UnityEngine;

public class RegularState : CamState
{
    public RegularState(BattleCamera camera) : base(camera)
    {
    }

    public override void CamMotion()
    {
        camera.CamRegularMotion();
    } 
}
