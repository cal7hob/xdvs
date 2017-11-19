using System;
using UnityEngine;

public class HumanCamera : GroundCamera
{
    protected float sensitivity;

    [SerializeField]
    private float vertMaxAngle = 40f;

    private SoldierController humanInView;

    private float realSens;
    private Vector3 eulerAngles;
    private Vector3 localEulerAngles;


    protected override void Awake()
    {
        base.Awake();

        Dispatcher.Subscribe(EventId.BattleSettingsSubmited, OnBattleSettingsApplied);
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();

        Dispatcher.Unsubscribe(EventId.BattleSettingsSubmited, OnBattleSettingsApplied);
    }

    protected override void OnVehicleSwitch()
    {
        humanInView = vehicleInView as SoldierController;
        transform.position = vehicleInView.CameraEndPoint.position;
        transform.forward = CameraGroundForward;
        crane.forward = (transform.position - vehicleInView.CameraPoint.position).normalized; ;
        Cam.transform.localPosition = crane.InverseTransformPoint(vehicleInView.CameraPoint.position);
        wrapperMovingSmoothTime = 1 / vehicleInView.maxSpeed;
    }

    protected void OnBattleSettingsApplied(EventId id, EventInfo info)
    {
        sensitivity = BattleSettings.TurretRotationSensitivity * 3f + 0.1f; // сенс меняется в диапазоне от 0.1 до 1
    }

    protected override void RewiredInputUpdateHandler()
    {
        rewiredController.SetButtonValue(mouseLeftBtn, false);

        if (MouseLeftBtnPressed && IsMouseControlled && !CursorManager.IsGUIOnScreen)
        {
            if (Cursor.lockState == CursorLockMode.Locked)
            {
                rewiredController.SetButtonValue(mouseLeftBtn, true);
            }
        }
    }

    public override void FollowKillerView()
    {
        crane.forward = humanInView.IkController.transform.forward;
    }

    private void RotateCamera(float zoomQualifier = 1)
    {
        if (!JoystickController.CanAct || CursorManager.UnlockButtonIsDown)
        {
            return;
        }

        realSens = CurrentInputController.SensitivityQualifier * zoomQualifier * sensitivity;

        eulerAngles = transform.eulerAngles + realSens * Vector3.up * CameraXAxis;
        localEulerAngles = crane.transform.localEulerAngles + realSens * Vector3.left * CameraYAxis;

        if (localEulerAngles.x < vertMaxAngle || localEulerAngles.x > 360 - vertMaxAngle)
        {
            transform.eulerAngles = eulerAngles;//hor
            crane.transform.localEulerAngles = localEulerAngles;//vert
        }
    }

    public override void MouseSpecificMotion()
    {
        RotateCamera();
    }

    public override void MouseSpecificZoomMotion()
    {
        CommonZoomMotion();
    }

    public override void TouchSpecificMotion()
    {
        RotateCamera();
    }

    public override void TouchSpecificZoomMotion()
    {
        CommonZoomMotion();
    }

    private void CommonZoomMotion() // все свелось к тому, что различия остались в самих контроллерах(тач и мышь), а здесь все одинакого
    {
        transform.position = vehicleInView.CameraEndPoint.position;
        MovingCameraToShotPoint();
        RotateCamera(zoomSensitivityQualifier);
        SetFOV();
    }
}
