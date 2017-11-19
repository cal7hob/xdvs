using UnityEngine;
using System.Collections;
using XDevs.LiteralKeys;

public class GroundCamera : BattleCamera
{
    [Header("Движение камеры/Облет препятствий")]
    public float camZoomAimingSpeed = 0.1f;
    public float zoomAutoAimMinSqrDist = 125;
    public float rotationSpeed = 3;
    public float camTowardsSpeed = 18;
    public float sphereCastRadius = 0.3f;
    public float correctCamDistance = 1;
    public LayerMask hitMask;
    public float showKillerOffsetQualifZ = 1.5f;
    public float showKillerOffestQualifY = 3;
    public float camToTankDistRiseQualif = 1.75f; // хз как назвать. коэфф подъема камеры по направлению между forCamera и расчетной позицией камеры
    public float vehicleHideSqrDistance = 1;

    private Vector3 camDeltaPos;
    private int hitMaskInt;
    private RaycastHit hitInfo;
    private Vector3 camPos;
    private Vector3 camSmoothVelocity;

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

    protected override void Init(EventId id, EventInfo ei)
    {
        base.Init(id, ei);

        hitMaskInt = hitMask.value;
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
        transform.LookAt(lookPoint);
        SetCamDeltaPosition(vehicleInView.forCam.position);
    }

    protected override void SwitchToKiller()
    {
        base.SwitchToKiller();

        SetCamDeltaPosition(vehicleInView.forCam.position);
        transform.position = camPos;
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
        Vector3 tankGroundForward = Vector3.ProjectOnPlane(vehicleInView.Turret.forward, Vector3.up).normalized;

        if (currentState == showKillerState && !PhotonNetwork.isMasterClient)
        {
            camPos = vehicleInView.CorrectPosition + vehicleInView.Turret.localPosition +
                     tankGroundForward*camDeltaPos.z + Vector3.up*camDeltaPos.y +
                     vehicleInView.Turret.right*camDeltaPos.x;
        }
        else
        {
            camPos = vehicleInView.Turret.position + tankGroundForward * camDeltaPos.z + Vector3.up * camDeltaPos.y + vehicleInView.Turret.right * camDeltaPos.x;
        }

        

        var correctionDir = vehicleInView.forCam.transform.position - camPos;
        camPos += -camToTankDistRiseQualif * (BattleSettings.Instance.CamToTankScrollValue - 1) * correctionDir;

        bool hitsSomething
            = Physics.SphereCast(
                /* origin:      */  vehicleInView.AimingPoint.position,
                /* radius:      */  sphereCastRadius,
                /* direction:   */  (camPos - vehicleInView.AimingPoint.position).normalized,
                /* hitInfo:     */  out hitInfo,
                /* maxDistance: */  Vector3.Distance(vehicleInView.AimingPoint.position, camPos),
                /* layerMask:   */  hitMaskInt);

        if (hitsSomething && hitInfo.collider.tag != Tag.Items[Tag.Key.IgnoreCameraCollision])
            MoveAroundObstacles();
        else
            NormalMove();
    }

    private void MoveAroundObstacles()
    {
        hitInfo.point = Vector3.MoveTowards(hitInfo.point, vehicleInView.AimingPoint.position, correctCamDistance);
        transform.position = Vector3.MoveTowards(transform.position, hitInfo.point, camTowardsSpeed * Time.deltaTime);
        transform.LookAt(lookPoint.position);
    }

    private void NormalMove()
    {
        transform.position = Vector3.SmoothDamp(transform.position, camPos, ref camSmoothVelocity, 1 / vehicleInView.maxSpeed);
        //transform.position = camPos;
        transform.LookAt(lookPoint.position);
    }

    void OnTriggerEnter(Collider col)
    {
        if (col.attachedRigidbody == vehicleInView.Rb)
        {
            vehicleInView.IsVisible = false;
        }
    }

    void OnTriggerExit(Collider col)
    {
        if (!IsZoomed && !col.CompareTag("CritZone") && col.transform.IsChildOf(vehicleInView.transform))
        {
            vehicleInView.IsVisible = true;
        }
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
            transform.forward = Vector3.MoveTowards(transform.forward, myVehicle.AimingPoint.forward, Time.deltaTime * camZoomAimingSpeed);
        }

        var angles = transform.eulerAngles;
        angles.y = myVehicle.AimingPoint.eulerAngles.y;
        transform.eulerAngles = angles;
        transform.position = myVehicle.AimingPoint.position;
    }

    protected override void OnZoomBtn()
    {
        if (IsZoomed)
        {
            camFOV = DefaultCamFOV;
            vehicleInView.IsVisible = true;
            currentState = regularState;
        }
        else
        {
            vehicleInView.IsVisible = false;
            StartCoroutine(CameraToShotPoint());
            transform.eulerAngles = myVehicle.Turret.eulerAngles;
            camFOV = ZoomedCamFOV;
            currentState = zoomedState;
        }

        StartCoroutine(SettingFOV());
        Messenger.Send(EventId.ZoomStateChanged, new EventInfo_B(IsZoomed));
    }
}
