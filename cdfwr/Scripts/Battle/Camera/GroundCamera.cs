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
        get { return mouseSensitivityQualifier * Vector3.up * CameraXAxis; }
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
        crane.forward = (transform.position - vehicleInView.CameraPoint.position).normalized; ;
        Cam.transform.localPosition = crane.InverseTransformPoint(vehicleInView.CameraPoint.position);
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

        SetFOV();
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

    public override void TouchSpecificMotion()
    {
        
    }

    public override void TouchSpecificZoomMotion()
    {
        
    }

    public override void MouseSpecificMotion()
    {

    }

    public override void MouseSpecificZoomMotion()
    {
        
    }

    public override void FollowKillerView()
    {
    }

    protected void MovingCameraToShotPoint()
    {
        if (!HelpTools.Approximately(Vector3.SqrMagnitude(Cam.transform.localPosition), 0, 0.05f))
        {
            Cam.transform.localPosition = Vector3.SmoothDamp(Cam.transform.localPosition, Vector3.zero, ref camToShootPointSmoothVelocity, cameraZoomInOutSmoothTime);
        }
    }
}
