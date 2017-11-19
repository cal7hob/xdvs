using UnityEngine;
using XDevs.LiteralKeys;

public class GroundCamera : BattleCamera
{
    [SerializeField] protected float zoomAutoAimMinSqrDist = 125;
    [SerializeField] protected float treesCullDistance = 50f;

    protected Vector3 CameraGroundForward
    {
        get
        {
            return Vector3.ProjectOnPlane(vehicleInView.Turret.forward, Vector3.up).normalized;
        }
    }

    public override Vector3 DeltaEulerAngles
    {
        get { return mouseSensivityQualifier * Vector3.up * CameraXAxis; }
    }

    protected override void Awake()
    {
        base.Awake();

        SetTreesCullDistance();
    }

    protected void SetTreesCullDistance()
    {
        var distances = Cam.layerCullDistances;
        distances[LayerMask.NameToLayer("Trees")] = treesCullDistance;
        Cam.layerCullDistances = distances;
    }

    protected override void SwitchToMyVehicle(EventId id = 0, EventInfo info = null)
    {
        base.SwitchToMyVehicle(id, info);

        if (myVehicle == null)
        {
            return;
        }

        OnVehicleSwitch();
        cameraCollisionMask = vehicleInView.OthersLayerMask | MiscTools.GetLayerMask(Layer.Key.Terrain) | MiscTools.GetLayerMask(Layer.Key.Default);
    }

    protected override void SwitchToKiller()
    {
        base.SwitchToKiller();

        OnVehicleSwitch();
        cameraCollisionMask = vehicleInView.OthersLayerMask | MiscTools.GetLayerMask(Layer.Key.Terrain) | MiscTools.GetLayerMask(Layer.Key.Default);
    }

    protected override void OnVehicleSwitch()
    {
        transform.position = vehicleInView.CameraEndPoint.position;
        transform.forward = CameraGroundForward;
        craneDirection = (transform.position - vehicleInView.forCam.position).normalized;
        crane.forward = craneDirection;
        Cam.transform.localPosition = crane.InverseTransformPoint(vehicleInView.forCam.position);
        //defaultCameraLocalPos = Cam.transform.localPosition;
        lookPoint.transform.position = vehicleInView.lookPoint.position;
        lookPoint.transform.forward = vehicleInView.lookPoint.forward;
        Cam.transform.LookAt(lookPoint);
        wrapperMovingSmoothTime = 1 / vehicleInView.maxSpeed;
    }

    public override void OnChangeToMouseControlledState()
    {
        transform.forward = CameraGroundForward;
    }

    public override void CommonMotion()
    {
        Cam.transform.LookAt(lookPoint);
        transform.position = Vector3.SmoothDamp(transform.position, vehicleInView.CameraEndPoint.position, ref camSmoothVelocity,
            wrapperMovingSmoothTime);

        SettingFOV();
        CheckObstacles();

        Cam.transform.localPosition = Vector3.SmoothDamp(Cam.transform.localPosition, camLocalPos, ref camLocalPosSmoothVelocity,
            obstaclesAvoidanceSmoothTime);       
    }

    //void OnDrawGizmos()
    //{
    //    Gizmos.color = Color.red;
    //    Gizmos.DrawSphere(transform.position + (Cam.transform.position - transform.position).normalized * Vector3.Magnitude(defaultCameraLocalPos), 0.5f);

    //    Gizmos.color = Color.yellow;
    //    Gizmos.DrawSphere(Cam.transform.position, 0.3f);

    //    Gizmos.color = Color.blue;
    //    Gizmos.DrawSphere(transform.InverseTransformPoint(camLocalPos), 0.4f);
    //}

    public override void CamRegularMotion()
    {
        transform.forward = Vector3.SmoothDamp(transform.forward, CameraGroundForward, ref camToGroundForwardSmoothVelocity,
            1 / vehicleInView.maxSpeed);
    }

    public override void MouseControlledMotion()
    {
        if (isGUIOnScreen || CursorManager.UnlockButtonIsDown)
        {
            return;
        }

        transform.localEulerAngles += DeltaEulerAngles;

        var leftOrRightDot = Vector3.Dot(transform.forward, VehicleInView.Turret.right);

        xAxisCameraValue = Mathf.Abs(leftOrRightDot) > camToTurretRotationThresholdCos ?
            (leftOrRightDot > 0 ? 1 : -1) :
            0;
    }

    private void MovingCameraToShotPoint()
    {
        if (!HelpTools.Approximately(Vector3.SqrMagnitude(Cam.transform.localPosition), 0, 0.5f))
        {
            Cam.transform.localPosition = Vector3.SmoothDamp(Cam.transform.localPosition, Vector3.zero, ref camToShootPointSmoothVelocity, cameraZoomInOutSmoothTime);
        }
    }

    public override void ZoomMotion()
    {
        transform.position = vehicleInView.ShotPoint.position;
        transform.forward = vehicleInView.ShotPoint.forward;
        var camForward = transform.forward;

        MovingCameraToShotPoint();
        SettingFOV();

        if (targetAimed)
        {
            var targetDir = vehicleInView.Target.Turret.transform.position - Cam.transform.position;

            if (targetDir.sqrMagnitude > zoomAutoAimMinSqrDist)
            {
                camForward = Vector3.ProjectOnPlane(targetDir.normalized, transform.right);
            }
        }

        Cam.transform.forward = Vector3.SmoothDamp(Cam.transform.forward, camForward, ref camZoomForwardSmoothVelocity, zoomVerticalAimSmoothTime);

        var angles = transform.eulerAngles;
        angles.z = vehicleInView.ShotPoint.eulerAngles.z;
        transform.eulerAngles = angles;

        xAxisCameraValue = Mathf.Clamp(CameraXAxis, -1, 1);
    }
}
