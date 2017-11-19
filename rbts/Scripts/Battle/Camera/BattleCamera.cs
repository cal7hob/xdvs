using System.Collections;
using UnityEngine;
using UnityStandardAssets.ImageEffects;

public abstract class BattleCamera : MonoBehaviour
{
    [SerializeField] private float turretIndicationZoomSqrDist = 2000;
    [SerializeField] private float vehicleIndicationZoomSqrDist = 2000;
    [SerializeField] private float cameraZoomMoveSpeed = 500;
    [SerializeField] private float cameraZoomFOVChangeSpeed = 250f;
    [SerializeField] private float defaultCamFOV = 60;
    [SerializeField] private float zoomedCamFOV = 10;
    [SerializeField] protected float enemyShowingRotationSpeed = 1;

    protected float camFOV;
    protected bool isShowingKiller;
    protected bool targetAimed;
    protected Camera cam;
    protected Transform camTransform;
    protected Transform lookPoint;
    protected VehicleController myVehicle;
    protected VehicleController killer;
    protected VehicleController vehicleInView;
    public VehicleController VehicleInView { get { return vehicleInView; } }
    protected Vector3 camStartPos;
    protected Quaternion camStartRotation;

    protected CamState regularState;
    protected CamState showKillerState;
    protected CamState idleState;
    protected CamState zoomedState;
    protected CamState currentState;

    public static BattleCamera Instance { get; private set; }
    public static Camera Camera { get { return Instance.cam; } }

    public bool IsZoomed { get { return currentState == zoomedState; } }

    public float CameraZoomFOVChangeSpeed { get { return cameraZoomFOVChangeSpeed; } }

    public float CameraZoomMoveSpeed { get { return cameraZoomMoveSpeed; } }

    public float DefaultCamFOV { get { return defaultCamFOV; } }

    public float ZoomedCamFOV { get { return zoomedCamFOV; } }

    public float TurretIndicationZoomSqrDist { get { return turretIndicationZoomSqrDist; } }

    public float VehicleIndicationZoomSqrDist { get { return vehicleIndicationZoomSqrDist; } }

    protected virtual void Awake()
    {
        Instance = this;
        cam = Camera.main;
        camTransform = cam.transform;

        regularState = new RegularState(this);
        showKillerState = new ShowKillerState(this);
        zoomedState = new ZoomedState(this);
        idleState = new IdleState(this);

        SetCameraPostProcessors();
        Subscribes();
    }

    private void SetCameraPostProcessors()
    {
        if (QualityManager.CurrentQualityLevel < (int) GraphicsLevel.UltraQuality)
        {
            var screenSpaceAmbientOcclusion = GetComponent<ScreenSpaceAmbientOcclusion>();
            var colorCorrectionCurves = GetComponent<ColorCorrectionCurves>();

            if (screenSpaceAmbientOcclusion != null)
                screenSpaceAmbientOcclusion.enabled = false;

            if (colorCorrectionCurves != null)
                colorCorrectionCurves.enabled = false;
        }

        if (QualityManager.CurrentQualityLevel < (int) GraphicsLevel.HighQuality)
        {
            var sunShafts = cam.GetComponent<SunShafts>();

            if (sunShafts != null)
                sunShafts.enabled = false;
        }
    }

    protected void Subscribes()
    {
        Messenger.Subscribe(EventId.ChangeZoomState, ChangeZoomState);
        Messenger.Subscribe(EventId.MyTankRespawned, SwitchToMyVehicle, 4);
        Messenger.Subscribe(EventId.MainTankAppeared, SwitchToMyVehicle, 4);
        Messenger.Subscribe(EventId.TankLeftTheGame, OnVehicleLeft);
        Messenger.Subscribe(EventId.MainTankAppeared, Init, 2);
        Messenger.Subscribe(EventId.VehicleKilled, OnVehicleKilled);
        Messenger.Subscribe(EventId.TargetAimed, OnTargetAimed);
        Messenger.Subscribe(EventId.BattleEnd, OnBattleEnd);
    }

    protected virtual void OnDestroy()
    {
        Instance = null;

        Unsubscribes();
    }

    protected virtual void Unsubscribes()
    {
        Messenger.Unsubscribe(EventId.ChangeZoomState, ChangeZoomState);
        Messenger.Unsubscribe(EventId.MyTankRespawned, SwitchToMyVehicle);
        Messenger.Unsubscribe(EventId.MainTankAppeared, SwitchToMyVehicle);
        Messenger.Unsubscribe(EventId.MainTankAppeared, Init);
        Messenger.Unsubscribe(EventId.VehicleKilled, OnVehicleKilled);
        Messenger.Unsubscribe(EventId.TargetAimed, OnTargetAimed);
        Messenger.Unsubscribe(EventId.TankLeftTheGame, OnVehicleLeft);
        Messenger.Unsubscribe(EventId.BattleEnd, OnBattleEnd);
    }

    protected virtual void Init(EventId id, EventInfo info)
    {
        camStartPos = transform.position;
        camStartRotation = transform.rotation;

        myVehicle = BattleController.MyVehicle;

        ResetCameraParams();

        if (MapParticles.Instance == null || MapParticles.Instance.CameraParticles == null)
            return;

        foreach (GameObject cameraParticlePrefab in MapParticles.Instance.CameraParticles)
        {
            GameObject cameraParticle = Instantiate(cameraParticlePrefab);

            cameraParticle.transform.SetParent(transform);
            cameraParticle.transform.localPosition = cameraParticlePrefab.transform.localPosition;
            cameraParticle.transform.localRotation = cameraParticlePrefab.transform.localRotation;
        }

        if (Map.Instance != null)
        {
            cam.farClipPlane = Map.Instance.CameraClippingDistance;
        }
    }

    protected void OnVehicleLeft(EventId eid, EventInfo ei)
    {
        var info = ei as EventInfo_I;
        var vehicleId = info.int1;

        if (vehicleInView != null && vehicleInView.data.playerId == vehicleId)
        {
            ReturnToStart();
        }
    }

    protected virtual void OnVehicleKilled(EventId id, EventInfo ei)
    {
        var info = ei as EventInfo_II;
        var victimId = info.int1;
        var killerId = info.int2;

        if (victimId != vehicleInView.data.playerId)
            return;

        if (killerId == victimId || !BattleController.allVehicles.TryGetValue(killerId, out killer))
        {
            ReturnToStart();
            return;
        }
        
        ResetCameraParams();
        SwitchToKiller();
    }

    protected virtual void SwitchToMyVehicle(EventId eid = 0, EventInfo ei = null)
    {
        if (IsZoomed)
            Messenger.Send(EventId.ChangeZoomState, new EventInfo_SimpleEvent());

        currentState = regularState;
        killer = null;

        if (myVehicle == null)
        {
            return;
        }

        vehicleInView = myVehicle;
        lookPoint = myVehicle.lookPoint;
    }

    protected virtual void SwitchToKiller()
    {
        Messenger.Send(EventId.ZoomStateChanged, new EventInfo_B(IsZoomed));

        killer.SetMarkedStatus(false);
        currentState = showKillerState;
        lookPoint = killer.lookPoint;
        vehicleInView = killer;
    }

    protected virtual void OnTargetAimed(EventId id, EventInfo ei)
    {
        EventInfo_IIB info = ei as EventInfo_IIB;
        if (info.int1 == BattleController.MyPlayerId)
            targetAimed = info.bool1;
    }

    protected virtual void ChangeZoomState(EventId id, EventInfo ei)
    {
        if(myVehicle != null)
            OnZoomBtn();
    }

    protected void CheckJoystickZoomBtn()
    {
        if (XDevs.Input.GetButtonDown("Zoom") && myVehicle != null && !StatTable.OnScreen)
            Messenger.Send(EventId.ChangeZoomState, new EventInfo_SimpleEvent());
    }

    protected IEnumerator CameraToShotPoint()
    {
        while (Vector3.SqrMagnitude(camTransform.position - myVehicle.AimingPoint.position) > 1)
        {
            camTransform.position = Vector3.MoveTowards(camTransform.position, myVehicle.AimingPoint.position, cameraZoomMoveSpeed * Time.deltaTime);
            yield return null;
        }
    }

    protected virtual void OnBattleEnd(EventId id, EventInfo info)
    {
        StopAllCoroutines();
        ReturnToStart();
    }

    protected virtual void ResetCameraParams()
    {
        cam.fieldOfView = DefaultCamFOV;
        camFOV = DefaultCamFOV;
    }

    public virtual void ReturnToStart()
    {
        currentState = idleState;

        Instance.transform.position = camStartPos;
        Instance.transform.rotation = camStartRotation;

        ResetCameraParams();
    }
     
    protected abstract void OnZoomBtn();
    public abstract void CamRegularMotion();
    public abstract void ShowKillerMotion();
    public abstract void ZoomMotion();
}