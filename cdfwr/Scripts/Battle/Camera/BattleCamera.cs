using Rewired;
using UnityEngine;
using UnityStandardAssets.ImageEffects;
using XDevs.LiteralKeys;

public abstract class BattleCamera : MonoBehaviour
{
    protected const string turretHorizontalAxis = "Screen Axis X";
    protected const string turretVerticalAxis = "Screen Axis Y";
    protected const string mouseLeftBtn = "BattleMouseLeftBtn";

    [SerializeField]
    protected float sphereCastRadius = 0.3f;
    [SerializeField]
    protected float turretIndicationZoomSqrDist = 2000;
    [SerializeField]
    protected float defaultCamFOV = 60;
    [SerializeField]
    protected float zoomedCamFOV = 10;
    [SerializeField, Tooltip("percent of normal")]
    protected float zoomSensitivityQualifier = 0.2f;

    protected float mouseSensitivityQualifier = 1;


    [SerializeField, Tooltip("��� ������� ����������, ������ ���������� �� ��� ���������� � �������� �����, ������������ ����� ����� �����")]
    protected float correctCamDistance = 1;

    [SerializeField, Tooltip("���. ���� ����� right ����� � forward ������� ������, �                                                                �������� �������� �������������� ����� �� �������")]
    protected float cameraAngleToTurretRotationThreshold = 1;

    [SerializeField]
    protected Transform lookPoint;
    [SerializeField]
    protected Transform crane;

    [SerializeField]
    protected float obstaclesAvoidanceSmoothTime = 0.1f;
    [SerializeField]
    protected float zoomVerticalAimSmoothTime = 1;
    [SerializeField]
    protected float cameraZoomInOutSmoothTime = 0.1f;

    protected float camFOV;
    protected Transform camTransform;
    protected VehicleController myVehicle;
    protected VehicleController killer;
    protected VehicleController vehicleInView;
    protected Vector3 camLocalPos;
    protected CustomController rewiredController;
    protected float camToTurretRotationThresholdCos;
    protected float xAxisCameraValue;

    protected CameraState killerShowState;
    protected CameraState regularState;
    protected CameraState zoomedState;
    protected CameraState offState;
    protected CameraState currentState;
    protected CameraState previousState;
    protected CameraState startState;

    protected InputControllerType TouchInputControllerState;
    protected InputControllerType MouseInputControllerState;
    protected InputControllerType CurrentInputController;

    protected Vector3 defaultCameraLocalPos;
    protected float wrapperMovingSmoothTime;
    protected int cameraCollisionMask;

    protected Vector3 camSmoothVelocity;
    protected Vector3 camLocalPosSmoothVelocity;
    protected Vector3 camToGroundForwardSmoothVelocity;
    protected Vector3 camZoomForwardSmoothVelocity;
    protected Vector3 camToShootPointSmoothVelocity;
    protected float camFOVsmoothVelocity;

    public static BattleCamera Instance { get; private set; }

    public bool IsMouseControlled { get; protected set; }

    public VehicleController VehicleInView { get { return vehicleInView; } }

    public bool IsZoomed { get { return currentState == zoomedState; } }

    public float DefaultCamFOV { get { return defaultCamFOV; } }

    public float ZoomedCamFOV { get { return zoomedCamFOV; } }

    public float TurretIndicationZoomSqrDist { get { return turretIndicationZoomSqrDist; } }

    public Camera Cam { get; protected set; }

    public float CameraXAxis { get { return CurrentInputController.XAxis; } }

    public float CameraYAxis { get { return CurrentInputController.YAxis; } }

    public bool ZoomBtn { get { return XDevs.Input.GetButtonDown("Zoom"); } }

    public virtual bool MouseLeftBtnPressed { get { return Input.GetMouseButton(0); } }

    public virtual bool MouseRightBtnClicked { get { return Input.GetMouseButtonDown(1); } }

    public abstract Vector3 DeltaEulerAngles { get; }

    public Transform Crane { get { return crane; } }

    protected virtual void Awake()
    {
        Instance = this;
        Cam = Camera.main;
        camTransform = Cam.transform;

        camToTurretRotationThresholdCos = Mathf.Cos(Mathf.Deg2Rad * (90 - cameraAngleToTurretRotationThreshold));
        rewiredController = XDevs.Input.TouchController;

        SetControllerStates();
        SetCameraStates();

        SetCameraPostProcessors();
        Subscribes();
    }

    void Start()
    {
        SetState(startState);
        transform.position = Map.CameraInitialPlace.position;
        transform.rotation = Map.CameraInitialPlace.rotation;
        Cam.transform.localPosition = Vector3.zero;
        Cam.transform.localRotation = Quaternion.identity;
    }

    protected virtual void OnDestroy()
    {
        Instance = null;
        Unsubscribes();
    }

    protected void LateUpdate()
    {
        currentState.CameraMotion();
    }

    private void SetCameraStates()
    {
        if (CurrentInputController == null)
        {
            SetControllerStates();
        }

        killerShowState = new KillerShowState(this, CurrentInputController);
        zoomedState = new ZoomedState(this, CurrentInputController);
        regularState = new RegularState(this, CurrentInputController);
        offState = new OffState(this, CurrentInputController);
        startState = new StartState(this, CurrentInputController);
    }

    private void SetControllerStates()
    {
        if (SystemInfo.deviceType == DeviceType.Handheld)
        {
            IsMouseControlled = (PlayerPrefs.GetInt("MouseControl", 0) == 1);
        }
        else
        {
            IsMouseControlled = (PlayerPrefs.GetInt("MouseControl", 1) == 1); //todo: �������������
        }
        MouseInputControllerState = new MouseInputController(this);
        TouchInputControllerState = new TouchInputController(this);
        CurrentInputController = IsMouseControlled ? MouseInputControllerState : TouchInputControllerState;
    }

    private void SetCameraPostProcessors()
    {
        if (QualityManager.CurrentQualityLevel < (int)GraphicsLevel.ultraQuality)
        {
            var screenSpaceAmbientOcclusion = GetComponent<ScreenSpaceAmbientOcclusion>();
            var colorCorrectionCurves = GetComponent<ColorCorrectionCurves>();

            if (screenSpaceAmbientOcclusion != null)
                screenSpaceAmbientOcclusion.enabled = false;

            if (colorCorrectionCurves != null)
                colorCorrectionCurves.enabled = false;
        }

        if (QualityManager.CurrentQualityLevel < (int)GraphicsLevel.highQuality)
        {
            var sunShafts = Cam.GetComponent<SunShafts>();

            if (sunShafts != null)
                sunShafts.enabled = false;
        }
    }

    protected void Subscribes()
    {
        Dispatcher.Subscribe(EventId.MyTankRespawned, SwitchToMyVehicle, 4);
        Dispatcher.Subscribe(EventId.MainTankAppeared, SwitchToMyVehicle, 4);
        Dispatcher.Subscribe(EventId.TankLeftTheGame, OnVehicleLeft);
        Dispatcher.Subscribe(EventId.MainTankAppeared, Initialize, 2);
        Dispatcher.Subscribe(EventId.TankKilled, OnVehicleKilled);
        Dispatcher.Subscribe(EventId.BattleEnd, OnBattleEnd);
        Dispatcher.Subscribe(EventId.OnBattleSettingsChangeVisibility, OnGUIToggle);
        Dispatcher.Subscribe(EventId.OnStatTableChangeVisibility, OnGUIToggle);
        Dispatcher.Subscribe(EventId.OnBattleChatCommandsChangeVisibility, OnGUIToggle);
    }

    protected virtual void Unsubscribes()
    {
        Dispatcher.Unsubscribe(EventId.MyTankRespawned, SwitchToMyVehicle);
        Dispatcher.Unsubscribe(EventId.MainTankAppeared, SwitchToMyVehicle);
        Dispatcher.Unsubscribe(EventId.MainTankAppeared, Initialize);
        Dispatcher.Unsubscribe(EventId.TankKilled, OnVehicleKilled);
        Dispatcher.Unsubscribe(EventId.TankLeftTheGame, OnVehicleLeft);
        Dispatcher.Unsubscribe(EventId.BattleEnd, OnBattleEnd);
        Dispatcher.Unsubscribe(EventId.OnBattleSettingsChangeVisibility, OnGUIToggle);
        Dispatcher.Unsubscribe(EventId.OnStatTableChangeVisibility, OnGUIToggle);
        Dispatcher.Unsubscribe(EventId.OnBattleChatCommandsChangeVisibility, OnGUIToggle);

        ReInput.InputSourceUpdateEvent -= RewiredInputUpdateHandler;
    }

    protected virtual void Initialize(EventId id, EventInfo info)
    {
        myVehicle = BattleController.MyVehicle;
        vehicleInView = myVehicle;

        if (IsMouseControlled)
        {
            ReInput.InputSourceUpdateEvent += RewiredInputUpdateHandler;
        }

        ResetFOV();

        if (MapParticles.Instance == null || MapParticles.Instance.CameraParticles == null)
        {
            return;
        }

        foreach (GameObject cameraParticlePrefab in MapParticles.Instance.CameraParticles)
        {
            GameObject cameraParticle = Instantiate(cameraParticlePrefab);

            cameraParticle.transform.parent = transform;
            cameraParticle.transform.localPosition = cameraParticlePrefab.transform.localPosition;
            cameraParticle.transform.localRotation = cameraParticlePrefab.transform.localRotation;
        }

        if (Map.Instance != null)
        {
            Cam.farClipPlane = Map.Instance.CameraClippingDistance;
        }
    }

    protected void OnVehicleLeft(EventId eid, EventInfo ei)
    {
        var info = ei as EventInfo_I;
        var vehicleId = info.int1;

        if (vehicleInView.data.playerId == vehicleId)
        {
            ReturnToStart();
            SetState(startState);
        }
    }

    protected virtual void OnVehicleKilled(EventId id, EventInfo ei)
    {
        var info = ei as EventInfo_II;
        var victimId = info.int1;
        var killerId = info.int2;

        if (killerId == victimId && victimId == VehicleInView.data.playerId) // ����� �������� ����� ����, ���� �����
        {
            ReturnToStart();
            return;
        }

        if (victimId == VehicleInView.data.playerId)
        {
            killer = BattleController.allVehicles[killerId];

            ZoomOut();
            ResetFOV();
            SwitchToKiller();
            SetState(killerShowState);
        }
    }

    protected virtual void SwitchToMyVehicle(EventId eid = 0, EventInfo ei = null)
    {
        if (ReferenceEquals(myVehicle, null))
        {
            return;
        }

        killer = null;
        vehicleInView = myVehicle;
        vehicleInView.IsVisible = true;

        ZoomOut();
        SetState(regularState);
    }

    protected virtual void SwitchToKiller()
    {
        killer.SetMarkedStatus(false);
        if (BattleController.Instance.BattleMode == GameData.GameMode.Team)
        {
            foreach (var aliveVeh in BattleController.allVehicles.Values)
            {
                if (aliveVeh.data.armor > 0 && aliveVeh.TeamId == myVehicle.TeamId)
                {
                    vehicleInView = aliveVeh;
                    break;
                }
            }
        }
        else
        {
            vehicleInView = killer;
        }
    }

    protected void CheckObstacles()
    {
        RaycastHit hitInfo;
        bool hitsSomething
            = Physics.SphereCast(
                /* origin:      */  transform.position,
                /* radius:      */  sphereCastRadius,
                /* direction:   */  (Cam.transform.position - transform.position).normalized,
                /* hitInfo:     */  out hitInfo,
                /* maxDistance: */  Vector3.Magnitude(defaultCameraLocalPos),
                /* layerMask:   */  cameraCollisionMask);

        if (hitsSomething && hitInfo.collider.tag != Tag.Items[Tag.Key.IgnoreCameraCollision])
        {
            hitInfo.point = Vector3.MoveTowards(hitInfo.point, transform.position, correctCamDistance);
            camLocalPos.z = crane.transform.InverseTransformPoint(hitInfo.point).z;
        }
        else
        {
            if (((SoldierController)BattleController.MyVehicle).isMove)
            {
                camLocalPos = defaultCameraLocalPos;
            }

        }
    }

    public void CheckJoystickZoomBtn()
    {
        if (BattleSettings.OnScreen || StatTable.OnScreen)
        {
            return;
        }

        if (ZoomBtn || MouseRightBtnClicked)
        {
            if (IsZoomed)
            {
                ZoomOut();
            }
            else
            {
                ZoomIn();
            }
        }
    }

    protected virtual void OnBattleEnd(EventId id, EventInfo info)
    {
        ReInput.InputSourceUpdateEvent -= RewiredInputUpdateHandler;
        SetState(offState);
        ReturnToStart();
        StopAllCoroutines();
        ZoomOut();
    }

    protected virtual void ResetFOV()
    {
        Cam.fieldOfView = DefaultCamFOV;
        camFOV = DefaultCamFOV;
    }

    protected virtual void OnGUIToggle(EventId id, EventInfo info)
    {
        if (BattleController.IsBattleFinished)
        {
            return;
        }

        if (CursorManager.IsGUIOnScreen && id != EventId.OnBattleChatCommandsChangeVisibility)
        {
            xAxisCameraValue = 0;
        }
    }

    protected void SetState(CameraState state)
    {
        if (ReferenceEquals(currentState, offState))
        {
            return;
        }

        if (ReferenceEquals(state, currentState))
        {
            return;
        }

        if (!ReferenceEquals(currentState, null))
        {
            previousState = currentState;
        }

        currentState = state ?? regularState;
        currentState.OnStateChanged();
    }

    protected virtual void RewiredInputUpdateHandler()
    {
        rewiredController.SetButtonValue(mouseLeftBtn, false);

        if (MouseLeftBtnPressed && IsMouseControlled && !CursorManager.IsGUIOnScreen)
        {
            if (Cursor.lockState == CursorLockMode.Locked)
            {
                rewiredController.SetButtonValue(mouseLeftBtn, true);
            }
        }

        rewiredController.SetAxisValue(turretHorizontalAxis, CursorManager.UnlockButtonIsDown ? 0 : xAxisCameraValue);
    }

    public virtual void ReturnToStart()
    {
        transform.position = Map.CameraInitialPlace.position;
        transform.rotation = Map.CameraInitialPlace.rotation;
        Cam.transform.localPosition = Vector3.zero;
        Cam.transform.localRotation = Quaternion.identity;

        ResetFOV();
    }

    public virtual void ZoomIn()
    {
        if (currentState == zoomedState)
        {
            return;
        }

        vehicleInView.IsVisible = false;
        camFOV = ZoomedCamFOV;
        SetState(zoomedState);

        Dispatcher.Send(EventId.ZoomStateChanged, new EventInfo_B(true));
    }

    public virtual void ZoomOut()
    {
        camFOV = DefaultCamFOV;
        vehicleInView.IsVisible = true;
        SetState(regularState);

        Dispatcher.Send(EventId.ZoomStateChanged, new EventInfo_B(false));
    }

    protected void SetFOV()
    {
        if (!HelpTools.Approximately(Cam.fieldOfView, camFOV, 0.1f))
        {
            Cam.fieldOfView = Mathf.SmoothDampAngle(Cam.fieldOfView, camFOV, ref camFOVsmoothVelocity, cameraZoomInOutSmoothTime);
        }
    }

    public void SetCamDefaultPosition(Vector3 camLocaPos)
    {
        defaultCameraLocalPos = camLocaPos;
        Cam.transform.localPosition = defaultCameraLocalPos;
    }

    protected abstract void OnVehicleSwitch();
    public abstract void OnChangeToMouseControlledState();
    public abstract void CommonMotion();
    public abstract void TouchSpecificMotion();
    public abstract void TouchSpecificZoomMotion();
    public abstract void MouseSpecificMotion();
    public abstract void MouseSpecificZoomMotion();
    public abstract void FollowKillerView();

}