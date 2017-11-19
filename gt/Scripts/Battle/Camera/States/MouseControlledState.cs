using UnityEngine;

public class MouseControlledState : CameraState
{
    public MouseControlledState(BattleCamera camera) : base(camera)
    {
    }

    public override void CameraMotion()
    {
        camera.CommonMotion();
        camera.MouseControlledMotion();
        camera.CheckJoystickZoomBtn();
    }

    public override void OnStateChanged()
    {
        Cursor.lockState = CursorLockMode.Locked;
        camera.OnChangeToMouseControlledState();
    }
}
