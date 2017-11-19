using UnityEngine;
using System.Collections;
using XDevs.LiteralKeys;

public class GroundCamera : BattleCamera
{
    [Header("Движение камеры/Облет препятствий")]
    public float camZoomAimingSpeed = 0.1f;
    public float zoomAutoAimMinSqrDist = 125;
    public float camTowardsSpeed = 18;
    public float sphereCastRadius = 0.3f;
    public float correctCamDistance = 1;
    public float camToTankDistRiseQualif = 1.75f; // хз как назвать. коэфф подъема камеры по направлению между forCamera и расчетной позицией камеры
    public LayerMask hitMask;

    protected Vector3 camPos;
    protected Vector3 camSmoothVelocity;
    protected RaycastHit hitInfo;

    private int cameraCollisionMask;
    private Vector3 camDeltaPos;

    public static GroundCamera GroundCamInstance { get; private set; }

    protected override void Awake()
    {
        base.Awake();

        GroundCamInstance = this;
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();

        GroundCamInstance = null;
    }

    void OnTriggerStay(Collider collider)
    {
        if (collider.tag != "CritZone" && collider.transform.IsChildOf(vehicleInView.transform))
            vehicleInView.IsVisible = false;
    }

    void OnTriggerExit(Collider collider)
    {
        if (!IsZoomed && collider.tag != "CritZone" && collider.transform.IsChildOf(vehicleInView.transform))
            vehicleInView.IsVisible = true;
    }

    public void SetCamDeltaPosition(Vector3 worldCamPos)
    {
        if (vehicleInView == null)
        {
            vehicleInView = currentState == showKillerState ? killer : myVehicle;
        }

        camDeltaPos = vehicleInView.Turret.InverseTransformPoint(worldCamPos);
    }

    protected void LateUpdate()
    {
        if (vehicleInView == null)
            return;

        currentState.CamMotion();
        CheckJoystickZoomBtn();
    }

    protected override void SwitchToMyVehicle(EventId id = 0, EventInfo info = null)
    {
        base.SwitchToMyVehicle(id, info);

        if (myVehicle == null)
        {
            return;
        }

        transform.position = myVehicle.forCam.position;
        transform.LookAt(lookPointTransform);
        SetCamDeltaPosition(vehicleInView.forCam.position);
        cameraCollisionMask = vehicleInView.OthersLayerMask | MiscTools.GetLayerMask(Layer.Key.Terrain) | MiscTools.GetLayerMask(Layer.Key.Default);
    }

    protected override void SwitchToKiller()
    {
        base.SwitchToKiller();

        SetCamDeltaPosition(vehicleInView.forCam.position);
        transform.position = camPos;
        cameraCollisionMask = vehicleInView.OthersLayerMask | MiscTools.GetLayerMask(Layer.Key.Terrain) | MiscTools.GetLayerMask(Layer.Key.Default);
    }

    private IEnumerator SettingFOV()
    {
        var waiter = new WaitForEndOfFrame();

        while (!Mathf.Approximately(camFOV, cam.fieldOfView))
        {
            cam.fieldOfView = Mathf.MoveTowards(cam.fieldOfView, camFOV, CameraZoomFOVChangeSpeed * Time.deltaTime);
            yield return waiter;
        }
    }

    public override void CamRegularMotion()
    {
        float speedK = .4f * (Mathf.Clamp (vehicleInView.CurrentSpeed / vehicleInView.MaxSpeed, .3f, .8f) - .3f);

        camDeltaPos = Vector3.LerpUnclamped(vehicleInView.cameraEndPoint.localPosition, vehicleInView.forCam.localPosition, BattleSettings.Instance.CamToTankScrollValue+speedK);

        Vector3 tankGroundForward = Vector3.ProjectOnPlane(vehicleInView.Turret.forward, Vector3.up).normalized;
        camPos = vehicleInView.Turret.position + tankGroundForward * camDeltaPos.z + Vector3.up * camDeltaPos.y + vehicleInView.Turret.right * camDeltaPos.x;

        //var correctionDir = vehicleInView.forCam.transform.position - camPos;
        //camPos += -camToTankDistRiseQualif * (BattleSettings.Instance.CamToTankScrollValue - 1) * correctionDir;

        bool hitsSomething
            = Physics.SphereCast(
                /* origin:      */  vehicleInView.ShotPoint.position,
                /* radius:      */  sphereCastRadius,
                /* direction:   */  (camPos - vehicleInView.ShotPoint.position).normalized,
                /* hitInfo:     */  out hitInfo,
                /* maxDistance: */  Vector3.Distance(vehicleInView.ShotPoint.position, camPos),
                /* layerMask:   */  cameraCollisionMask);

        if (hitsSomething && hitInfo.collider.tag != Tag.Items[Tag.Key.IgnoreCameraCollision])
            MoveAroundObstacles();
        else
            NormalMove();
    }

    protected virtual void MoveAroundObstacles()
    {
        hitInfo.point = Vector3.MoveTowards(hitInfo.point, vehicleInView.ShotPoint.position, correctCamDistance);
        transform.position = Vector3.MoveTowards(transform.position, hitInfo.point, camTowardsSpeed * Time.deltaTime);
        transform.LookAt(lookPointTransform.position);
    }

    protected virtual void NormalMove()
    {
        //transform.position = Vector3.SmoothDamp(transform.position, camPos, ref camSmoothVelocity, 1.0f / vehicleInView.MaxSpeed);
        transform.position = camPos;
        transform.LookAt(lookPointTransform.position);
    }

    public override void ShowKillerMotion()
    {
        CamRegularMotion();
    }

    public override void ZoomMotion()
    {
        if (targetAimed)
        {
            var targetDir = BattleController.MyVehicle.TargetPosition - transform.position;

            if (targetDir.sqrMagnitude > zoomAutoAimMinSqrDist)
            {
                var aimDir = Vector3.ProjectOnPlane(targetDir, transform.right);

                transform.forward
                = Vector3.MoveTowards(
                    current: transform.forward,
                    target: aimDir,
                    maxDistanceDelta: Time.deltaTime);
            }
        }
        else
        {
            transform.forward = Vector3.MoveTowards(transform.forward, myVehicle.ShotPoint.forward, Time.deltaTime * camZoomAimingSpeed);
        }

        var angles = transform.eulerAngles;
        angles.y = myVehicle.ShotPoint.eulerAngles.y;
        transform.eulerAngles = angles;
        transform.position = myVehicle.ShotPoint.position;
    }

    protected override void OnZoomBtn()
    {
        if (IsZoomed)
        {
            camFOV = DefaultCamFOV;
            currentState = regularState;
            //vehicleInView.IsVisible = true;
        }
        else
        {
            vehicleInView.IsVisible = false;
            StartCoroutine(CameraToShotPoint());
            transform.eulerAngles = myVehicle.CannonEnd.eulerAngles;
            camFOV = ZoomedCamFOV;
            currentState = zoomedState;
        }

        StartCoroutine(SettingFOV());
        Dispatcher.Send(EventId.ZoomStateChanged, new EventInfo_B(IsZoomed));
    }
}
