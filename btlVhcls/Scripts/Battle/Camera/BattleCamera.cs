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
    protected Transform lookPointTransform;
    protected VehicleController myVehicle;
    protected VehicleController killer;
    protected VehicleController vehicleInView;
    protected Vector3 camStartPos;
    protected Quaternion camStartRotation;

    protected CamState regularState;
    protected CamState showKillerState;
    protected CamState idleState;
    protected CamState zoomedState;
    protected CamState currentState;

    public static BattleCamera Instance { get; private set; }

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
        Dispatcher.Subscribe (EventId.MainTankAppeared, Init, 2);
    }

    private void SetCameraPostProcessors()
    {
        var screenSpaceAmbientOcclusion = GetComponent<ScreenSpaceAmbientOcclusion>();
        var colorCorrectionCurves = GetComponent<ColorCorrectionCurves>();
        var sunShafts = cam.GetComponent<SunShafts>();

        if (QualityManager.CurrentQualityLevel < (int) GraphicsLevel.ultraQuality)
        {
            if (screenSpaceAmbientOcclusion != null)
                screenSpaceAmbientOcclusion.enabled = false;

            if (colorCorrectionCurves != null)
                colorCorrectionCurves.enabled = false;
        }

        if (QualityManager.CurrentQualityLevel < (int) GraphicsLevel.highQuality)
        {
            if (sunShafts != null)
                sunShafts.enabled = false;
        }

#if !(UNITY_EDITOR || UNITY_STANDALONE || UNITY_WEBGL || UNITY_WSA)
            if (sunShafts != null)
                sunShafts.enabled = false;

            if (screenSpaceAmbientOcclusion != null)
                screenSpaceAmbientOcclusion.enabled = false;

            if (colorCorrectionCurves != null)
                colorCorrectionCurves.enabled = false;

            var otherPostEffects = GetComponents<PostEffectsBase>();

            if (otherPostEffects != null)
            {
                foreach (PostEffectsBase postEffect in otherPostEffects)
                    postEffect.enabled = false;
            }
#endif
    }

    protected void Subscribes()
    {
        Dispatcher.Subscribe(EventId.ChangeZoomState, ChangeZoomState);
        Dispatcher.Subscribe(EventId.MyTankRespawned, SwitchToMyVehicle, 4);
        Dispatcher.Subscribe(EventId.MainTankAppeared, SwitchToMyVehicle, 4);
        Dispatcher.Subscribe(EventId.TankLeftTheGame, OnVehicleLeft);
        Dispatcher.Subscribe(EventId.TankKilled, OnVehicleKilled, 2);
        Dispatcher.Subscribe(EventId.DeathAnimationDone, OnDeathAnimationDone);
        Dispatcher.Subscribe(EventId.TargetAimed, OnTargetAimed);
        Dispatcher.Subscribe(EventId.BattleEnd, OnBattleEnd);
    }

    protected virtual void OnDestroy()
    {
        Instance = null;

        Unsubscribes();
    }

    protected virtual void Unsubscribes()
    {
        Dispatcher.Unsubscribe(EventId.ChangeZoomState, ChangeZoomState);
        Dispatcher.Unsubscribe(EventId.MyTankRespawned, SwitchToMyVehicle);
        Dispatcher.Unsubscribe(EventId.MainTankAppeared, SwitchToMyVehicle);
        Dispatcher.Unsubscribe(EventId.MainTankAppeared, Init);
        Dispatcher.Unsubscribe(EventId.TankKilled, OnVehicleKilled);
        Dispatcher.Unsubscribe(EventId.DeathAnimationDone, OnDeathAnimationDone);
        Dispatcher.Unsubscribe(EventId.TargetAimed, OnTargetAimed);
        Dispatcher.Unsubscribe(EventId.TankLeftTheGame, OnVehicleLeft);
        Dispatcher.Unsubscribe(EventId.BattleEnd, OnBattleEnd);
    }

    protected virtual void Init(EventId id, EventInfo info)
    {
        camStartPos = transform.position;
        camStartRotation = transform.rotation;

        myVehicle = BattleController.MyVehicle;

        ResetCameraParams();
        Subscribes ();

        if (MapParticles.Instance == null || MapParticles.Instance.CameraParticles == null)
            return;

        foreach (GameObject cameraParticlePrefab in MapParticles.Instance.CameraParticles)
        {
            GameObject cameraParticle = Instantiate(cameraParticlePrefab);

            cameraParticle.transform.parent = transform;
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

        if (killer != null && killer.data.playerId == vehicleId)
        {
            ReturnToStart();
        }
    }

    protected virtual void OnVehicleKilled(EventId id, EventInfo ei)
    {
        var info = ei as EventInfo_III;
        var victimId = info.int1;
        var killerId = info.int2;

        if ( (killerId != BattleController.MyPlayerId) && (victimId != BattleController.MyPlayerId)) { // Не наше событие, выходим
            return;
        }

        if (victimId == BattleController.MyPlayerId) {
            killer = BattleController.allVehicles[killerId];
        }
    }

    protected virtual void OnDeathAnimationDone (EventId id, EventInfo ei) 
    {
        if (killer == null) {
            return;
        }
        if (killer.data.playerId == BattleController.MyPlayerId) { // когда читерски убили себя, либо миной
            ReturnToStart ();
            return;
        }
        ResetCameraParams ();
        SwitchToKiller ();
    }

    protected virtual void SwitchToMyVehicle(EventId id, EventInfo ei)
    {
        if (vehicleInView != null && !vehicleInView.IsVisible)
            vehicleInView.IsVisible = true;

        if (IsZoomed)
            Dispatcher.Send(EventId.ChangeZoomState, new EventInfo_SimpleEvent());

        currentState = regularState;

        killer = null;

        if (myVehicle == null)
            return;

        vehicleInView = myVehicle;

        SetLookPoint(myVehicle.lookPoint);
    }

    protected virtual void SwitchToKiller()
    {
        Dispatcher.Send(EventId.ZoomStateChanged, new EventInfo_B(IsZoomed));

        killer.SetMarkedStatus(false);
        currentState = showKillerState;
        SetLookPoint(killer.lookPoint);
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
        if(BattleController.MyVehicle != null)
            OnZoomBtn();
    }

    protected void CheckJoystickZoomBtn()
    {
        if (XDevs.Input.GetButtonDown("Zoom") && BattleController.MyVehicle != null && !StatTable.OnScreen)
            Dispatcher.Send(EventId.ChangeZoomState, new EventInfo_SimpleEvent());
    }

    protected IEnumerator CameraToShotPoint()
    {
        Transform camTransform = Camera.main.transform;
        VehicleController myVehicle = BattleController.MyVehicle;

        while (Vector3.SqrMagnitude(camTransform.position - myVehicle.ShotPoint.position) > 1)
        {
            camTransform.position = Vector3.MoveTowards(camTransform.position, myVehicle.ShotPoint.position, cameraZoomMoveSpeed * Time.deltaTime);
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

    protected virtual void SetLookPoint(Transform point)
    {
        lookPointTransform = point;
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