using System.Collections;
using UnityEngine;

public class CarCamera : BattleCamera
{
    public LayerMask hitMask;
    public float speed = 3;
    public float moveTowardsSpeed = 18;
    public float sphereCastRadius = 0.3f;
    public float correctCamDistance = 1;

    private int hitMaskInt;
    private Vector3 camDeltaPos;
    private Vector3 lookDeltaPos;
    private Vector3 startPosition;
    private Vector3 lookPos, camPos;
    private Quaternion startRotation;
    private Transform mainCarTransform;
    private RaycastHit hitInfo;
    private CarController myCar;

    public new static CarCamera Instance { get; private set; }

    protected override void Awake()
    {
        base.Awake();

        Instance = this;

        startPosition = transform.position;
        startRotation = transform.rotation;

        Dispatcher.Subscribe(EventId.MainTankAppeared, AfterBattleInit);
        Dispatcher.Subscribe(EventId.TargetAimed, OnTargetAimed);

        hitMaskInt = hitMask.value;
        Instance = this;
        enabled = false;
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
        Instance = null;

        Dispatcher.Unsubscribe(EventId.MainTankAppeared, AfterBattleInit);
        Dispatcher.Unsubscribe(EventId.TargetAimed, OnTargetAimed);
    }

    void OnTriggerEnter(Collider col)
    {
        if (col == myCar.bodyCollider || col == myCar.turretCollider)
            myCar.IsVisible = false;
    }

    void OnTriggerExit(Collider col)
    {
        if (col == myCar.bodyCollider || col == myCar.turretCollider)
            myCar.IsVisible = true;
    }

    public override void CamRegularMotion()
    {
        //NormalMove();
        Vector3 carGroundForward = Vector3.ProjectOnPlane(myCar.Turret.forward, Vector3.up).normalized;

        camPos = mainCarTransform.position + carGroundForward * camDeltaPos.z + Vector3.up * camDeltaPos.y;
        lookPos = mainCarTransform.position + Vector3.up * lookDeltaPos.y;

        if (Physics.SphereCast(
                /*origin:*/         lookPos,
                /*radius:*/         sphereCastRadius,
                /*direction:*/      (camPos - lookPos).normalized,
                /*hitInfo:*/        out hitInfo, // Юнитевский компилятор не хочет парсить out после именованных аргументов.
                                                 /*maxDistance:*/    Vector3.Distance(lookPos, camPos),
                /*layerMask:*/      hitMaskInt))
        {
            MoveAroundBarriers();
        }
        else
        {
            NormalMove();
        }
    }

    protected override void OnZoomBtn()
    {
        if (myCar)
        {
            if (IsZoomed)
            {
                camFOV = DefaultCamFOV;
            }
            else
            {
                StartCoroutine(CameraToShotPoint());
                transform.eulerAngles = myCar.CannonEnd.eulerAngles;
                camFOV = ZoomedCamFOV;
            }
        }

        Dispatcher.Send(EventId.ZoomStateChanged, new EventInfo_B(IsZoomed));
    }

    private void SetFOV()
    {
        cam.fieldOfView = Mathf.MoveTowards(cam.fieldOfView, camFOV, CameraZoomFOVChangeSpeed * Time.deltaTime);
    }

    public override void ReturnToStart()
    {
        Instance.transform.position = startPosition;
        Instance.transform.rotation = startRotation;

        Instance.enabled = false;

        cam.fieldOfView = DefaultCamFOV;
    }

    private void AfterBattleInit(EventId id, EventInfo info)
    {
        myCar = BattleController.MyVehicle as CarController;

        if (myCar == null)
        {
            Debug.LogWarning("CarController works only with CarController and will be disabled.");
            Destroy(this);
            return;
        }

        mainCarTransform = myCar.transform;

        camDeltaPos = mainCarTransform.InverseTransformPoint(myCar.forCam.position);
        lookDeltaPos = mainCarTransform.InverseTransformPoint(lookPointTransform.position);
    }

    public override void ShowKillerMotion()
    {
        throw new System.NotImplementedException();
    }

    public override void ZoomMotion()
    {
        throw new System.NotImplementedException();
    }

    private void MoveAroundBarriers()
    {
        if (IsZoomed)
        {
            CamLookAtTarget();
        }
        else
        {
            hitInfo.point = Vector3.MoveTowards(hitInfo.point, lookPos, correctCamDistance);

            transform.position
                = Vector3.MoveTowards(
                    current:            transform.position,
                    target:             hitInfo.point,
                    maxDistanceDelta:   moveTowardsSpeed * Time.deltaTime);

            transform.LookAt(lookPos);
        }

    }

    private void NormalMove()
    {
        if (IsZoomed)
        {
            CamLookAtTarget();
        }
        else
        {
            //transform.position = Vector3.Lerp(transform.position, place.position, Time.deltaTime * speed);
            //transform.LookAt(lookPoint);
            //NewCameraCorrection();

            transform.position = Vector3.Lerp(transform.position, camPos, Time.deltaTime * speed);  
            transform.LookAt(lookPos);
        }
    }

    private void CamLookAtTarget()
    {
        transform.position = myCar.ShotPoint.position;

        if (targetAimed && Vector3.Distance(BattleController.MyVehicle.TargetPosition, transform.position) > 15)
            transform.forward
                = Vector3.MoveTowards(
                    current:            transform.forward,
                    target:             (BattleController.MyVehicle.TargetPosition - transform.position).normalized,
                    maxDistanceDelta:   Time.deltaTime / 2);

        transform.eulerAngles = new Vector3(transform.eulerAngles.x, myCar.ShotPoint.eulerAngles.y, transform.eulerAngles.z);
    } 
}
