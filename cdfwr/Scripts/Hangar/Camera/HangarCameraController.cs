using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using UnityEngine;


[Serializable]
public class CamLookPoint
{
    [SerializeField] private Transform lookPoint;
    [SerializeField] private Transform pivot;

    public CamLookPoint(Transform lookPoint, Transform pivot)
    {
        this.lookPoint = lookPoint;
        this.pivot = pivot;
    }

    public Transform LookPoint { get { return lookPoint; } }
    public Transform Pivot { get { return pivot; } }
}

public class HangarCameraController : MonoBehaviour
{
    public List<GameObject> nonCamRotationWindows;

    [SerializeField] private Camera cam;
    [SerializeField] private GameObject topPanel;
    [SerializeField] private int minXAngle = 0;
    [SerializeField] private int maxXAndle = 87;
    [SerializeField] private int zoomSpeed = 1;
    [SerializeField] private int swipeSpeedQualifier = 3;
    [SerializeField] private float approximationTolerance = 0.01f;
    [SerializeField] private float minCameraZoomZ = -5;
    [SerializeField] private float maxCameraZoomZ = -8;
    [SerializeField] private Vector2 defaultRotationSpeed = Vector2.zero;
    [SerializeField] private float camOffset = 1f;
    [SerializeField] private Transform weaponsPoint;
    [SerializeField] private Vector3 weaponShopCamPos = new Vector3(0,0, 1.2f);

    [Header("Module Shop Cam Approach Params:")]
    [SerializeField] private float maxDegreesDelta = 40;
    [SerializeField] private float maxDistanceDelta = 5;
    [SerializeField] private float minCamApproachTime = 1;
    [SerializeField] private float maxCamApproachTime = 3;
    [SerializeField] private float backToOrbitApproachTime = 2;
    [SerializeField] private float camMoveToMaxZoomSpeed = 3;
    [SerializeField] private float camMoveToMaxZoomDelay = 0.2f;

    public Dictionary<TankModuleInfos.ModuleType, CamLookPoint> camLookPoints = new Dictionary<TankModuleInfos.ModuleType, CamLookPoint>(5)
    {
        {TankModuleInfos.ModuleType.Cannon, null},
        {TankModuleInfos.ModuleType.Armor, null},
        {TankModuleInfos.ModuleType.Tracks, null},
        {TankModuleInfos.ModuleType.Engine, null},
        {TankModuleInfos.ModuleType.Reloader, null}
    };

    [SerializeField] private Renderer cachedTopGuiPanelRenderer;
    [SerializeField] private Renderer cachedBottomGuiPanelRenderer;
    [SerializeField] private Renderer cachedRightGuiPanelRenderer;

    private Vector3 savedSoldierPosition;
    private Vector3 savedWrapperEulerAngles;
    private Vector3 normalCamWrapperPos;
    private Vector3 normalCamPos;
    private Rect touchableArea;
    private bool touchableAreaInited;
    private float fingerPositionX;
    private float rotationX;
    private Vector2 prevMousePosition;
    private Vector2 deltaMousePosition;
    private Vector2 idleRotationSpeed;
    private Vector2 rotationSpeed;
    private float camBeforeZoomZPos;
    private float initialZoomFingersDist;
    private float camZ;
    private float camInitialZ;
    private bool floorCollision;
    private RaycastHit hitInfo;
    private Vector3 defCamWrapperPosition;

    private HangarCamIdleState idleCamState;
    private HangarCamIdleWithoutControlState idleWithoutControlCamState;
    private HangarCamMovingBackState movingBackCamState;
    private HangarCamShowingModuleState showingModuleState;
    private HangarCamModuleApproachingState moduleApproachingState;   
    private HangarCamState currentCamState;

    //cam approach
    private bool isCamLookPointsFound;
    private bool isOrbitChanging;
    private CamLookPoint camLookPoint;
    private float timeToApproach;
    private float wrapperApproachDist;
    private float wrapperApproachAngleDiff;
    private float curDegreesDelta;
    private float camMaxDistDeltaZ;
    private float wrapperMaxDistDelta;
    private float camToDefaultUpDeltaAngles;
    private float camWrapperMaxDistanceDelta;
    private float camMaxDistanceDelta;
    private Quaternion wrapperTargetRotation;

    public static HangarCameraController Instance { get; private set; }

    public bool CanMoveOnTouch
    {
        get { return nonCamRotationWindows.All(window => window.name != GUIPager.ActivePage); }
    }

    public static float CamOffset { get { return Instance.camOffset; } }

    void Awake()
    {
        Instance = this;

        GUIPager.OnPageChange += OnPageChange;

        Dispatcher.Subscribe(EventId.TouchableAreaChanged, OnTouchableAreaChanged);
        Dispatcher.Subscribe(EventId.ResolutionChanged, OnTouchableAreaChanged);
        Dispatcher.Subscribe(EventId.AfterHangarInit, Initialize);

        cam.transform.LookAt(transform);

        idleCamState = new HangarCamIdleState(this);
        idleWithoutControlCamState = new HangarCamIdleWithoutControlState(this);
        movingBackCamState = new HangarCamMovingBackState(this);
        showingModuleState = new HangarCamShowingModuleState(this);
        moduleApproachingState = new HangarCamModuleApproachingState(this);

        currentCamState = idleCamState;
    }

    void OnDestroy()
    {
        Dispatcher.Unsubscribe(EventId.TouchableAreaChanged, OnTouchableAreaChanged);
        Dispatcher.Unsubscribe(EventId.ResolutionChanged, OnTouchableAreaChanged);
        Dispatcher.Unsubscribe(EventId.AfterHangarInit, Initialize);

        GUIPager.OnPageChange -= OnPageChange;

        Instance = null;
    }

    void Update()
    {
        currentCamState.Move();
    }

    private void ManualCameraControl()
    {
        ApproximateToDefaultRotationSpeed();

        if (ScoresPage.IsScrolling)
        {
            return;
        }

        switch (Input.touchCount)
        {
            case 1:
                CheckForSwipeRotation();
                break;
            case 2:
                CheckForPinchZoom();
                break;
        }

        GetMouseControl();
    }

    private void GetMouseControl()
    {
        if (!touchableArea.Contains(Input.mousePosition, true))
        {
            return;
        }

        //if (!Mathf.Approximately(Input.GetAxisRaw("Mouse ScrollWheel"), 0))
        //{
        //    ScrollZoomCamTransition(Input.GetAxisRaw("Mouse ScrollWheel") * zoomSpeed);
        //}

#if UNITY_WEBPLAYER || UNITY_STANDALONE || UNITY_EDITOR || UNITY_WEBGL || UNITY_WSA

        GetMouseTransitions();

        if (Input.GetMouseButton(0))
        {
            SetRotationSpeed(HelpTools.GetScreenRelPos(deltaMousePosition));
        }
        else
        {
            ApproximateToDefaultRotationSpeed();
        }

#endif
    }

    private void CheckForSwipeRotation()
    {
        var touch = Input.GetTouch(0);

        if (touchableArea.Contains(touch.position, true))
        {
            switch (Input.GetTouch(0).phase)
            {
                case TouchPhase.Moved:
                    SetRotationSpeed(HelpTools.GetScreenRelPos(touch.deltaPosition) * swipeSpeedQualifier);
                    break;
                case TouchPhase.Stationary:
                    SetRotationSpeed(Vector2.zero);
                    break;
            }
        }
    }

    private void CheckForPinchZoom()
    {
        return;

        var touch0 = Input.GetTouch(0);
        var touch1 = Input.GetTouch(1);

        SetRotationSpeed(Vector2.zero);

        if ((touch0.phase == TouchPhase.Moved && touch1.phase == TouchPhase.Moved) ||
            (touch0.phase == TouchPhase.Stationary && touch1.phase == TouchPhase.Moved) ||
            (touch0.phase == TouchPhase.Moved && touch1.phase == TouchPhase.Stationary))
        {
            PinchZoomCamTransition(GetScreenRelFingersDist(touch0.position, touch1.position));
        }
        else if (touch0.phase == TouchPhase.Began || touch1.phase == TouchPhase.Began)
        {
            camBeforeZoomZPos = cam.transform.localPosition.z;
            initialZoomFingersDist = GetScreenRelFingersDist(touch0.position, touch1.position);
        }
    }

    private void Initialize(EventId id, EventInfo info)
    {
        cachedTopGuiPanelRenderer = topPanel.GetComponent<Renderer>();
        cachedRightGuiPanelRenderer = RightPanel.Instance != null ? RightPanel.Instance.rightPanel.scrollableArea.GetComponent<Renderer>() : null;
        cachedBottomGuiPanelRenderer = MenuController.Instance.bottomGuiPanel.GetComponent<Renderer>();
        defCamWrapperPosition = transform.position;
        idleRotationSpeed = defaultRotationSpeed;
        Cursor.lockState = CursorLockMode.None;
    }

    public void SetState(HangarCamState state)
    {
        if (currentCamState.CanChangeState)
        {
            currentCamState = state;
            currentCamState.OnStateChange();
        }
    }

    private void OnPageChange(string from, string to)
    {
        if (CanMoveOnTouch)
        {
            SetState(idleCamState);
        }
        else
        {
            SetState(idleWithoutControlCamState);
        }

        if (from == "DecalShop")
        {
            OnWeaponShopClosed();
        }
    }

    public void OnModuleClicked()
    {
        if (ModuleShop.Instance.ModuleInView == null || !isCamLookPointsFound)
            return;

        SetState(moduleApproachingState);
    }

    private void GetTimeToApproach()
    {
        wrapperApproachAngleDiff = Quaternion.Angle(transform.rotation, camLookPoint.LookPoint.rotation);
        wrapperApproachDist = Vector3.Distance(transform.position, camLookPoint.Pivot.position);

        var wrapperMoveTime = wrapperApproachDist/maxDistanceDelta;
        var wrapperRotateTime = wrapperApproachAngleDiff/maxDegreesDelta;

        timeToApproach = wrapperMoveTime > wrapperRotateTime ? wrapperMoveTime : wrapperRotateTime;
        timeToApproach = Mathf.Clamp(timeToApproach, minCamApproachTime, maxCamApproachTime); 
    }

    private bool RotateCamWrapper()
    {
        if (HelpTools.Approximately(Quaternion.Angle(transform.rotation, wrapperTargetRotation), 0, approximationTolerance))
        {
            return true;
        }

        transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(camLookPoint.LookPoint.forward, Vector3.up), curDegreesDelta * Time.deltaTime);

        return false;
    }

    private bool MoveCamWrapper()
    {
        if (HelpTools.Approximately((transform.position - camLookPoint.Pivot.position).sqrMagnitude, 0, approximationTolerance))
        {
            return true;
        }

        transform.position = Vector3.MoveTowards(transform.position, camLookPoint.Pivot.position, wrapperMaxDistDelta * Time.deltaTime);

        return false;
    }

    private bool MoveCam()
    {
        if (HelpTools.Approximately(cam.transform.localPosition.z, -camLookPoint.Pivot.localPosition.z, approximationTolerance))
        {
            return true;
        }

        var camLocPos = cam.transform.localPosition;
        camLocPos.z = Mathf.MoveTowards(camLocPos.z, -camLookPoint.Pivot.localPosition.z, camMaxDistDeltaZ * Time.deltaTime);
        cam.transform.localPosition = camLocPos;

        return false;
    }

    private void GetLookParams()
    {
        if (ModuleShop.Instance.ModuleInView != null)
        {
            switch (ModuleShop.Instance.ModuleInView.type)
            {
                case TankModuleInfos.ModuleType.Cannon:
                    camLookPoint = camLookPoints[TankModuleInfos.ModuleType.Cannon];
                    break;
                case TankModuleInfos.ModuleType.Engine:
                    camLookPoint = camLookPoints[TankModuleInfos.ModuleType.Engine];
                    break;
                case TankModuleInfos.ModuleType.Tracks:
                    camLookPoint = camLookPoints[TankModuleInfos.ModuleType.Tracks];
                    break;
                case TankModuleInfos.ModuleType.Armor:
                    camLookPoint = camLookPoints[TankModuleInfos.ModuleType.Armor];
                    break;
                case TankModuleInfos.ModuleType.Reloader:
                    camLookPoint = camLookPoints[TankModuleInfos.ModuleType.Reloader];
                    break;
            }
        }
    }

    public void OnSwitchToMoveBackState()
    {
        var dist = Vector3.Distance(transform.position, defCamWrapperPosition);
        camWrapperMaxDistanceDelta = dist / backToOrbitApproachTime;

        var localDist = Math.Abs(camInitialZ - cam.transform.localPosition.z);
        camMaxDistanceDelta = localDist / backToOrbitApproachTime;

        var angle = Vector3.Angle(Vector3.up, transform.up);
        camToDefaultUpDeltaAngles = angle / backToOrbitApproachTime;
    }

    public void OnSwitchToModuleApproachingState()
    {
        GetLookParams();
        GetTimeToApproach();

        var dist = Mathf.Abs(cam.transform.localPosition.z + camLookPoint.Pivot.localPosition.z);
        camMaxDistDeltaZ = dist / timeToApproach;

        wrapperTargetRotation = Quaternion.LookRotation(camLookPoint.LookPoint.forward, Vector3.up);
        wrapperMaxDistDelta = wrapperApproachDist / timeToApproach;
        curDegreesDelta = wrapperApproachAngleDiff / timeToApproach;
    }

    public void IdleUpdate()
    {
        ManualCameraControl();
        RotateCamera();
    }

    public void ModuleApproachingUpdate()
    {
        var isWrapperRotated = RotateCamWrapper();
        var isCamWrapperMoved = MoveCamWrapper();
        var isCamMoved = MoveCam();

        if (isWrapperRotated && isCamWrapperMoved && isCamMoved)
        {
            SetState(showingModuleState);
        }
    }

    public void MovingBackUpdate()
    {
        if (movingBackCamState.CanChangeState)
        {
            SetState(CanMoveOnTouch ? (HangarCamState) idleCamState : idleWithoutControlCamState);
        }
    }

    public bool MoveBackToOrbit()
    {
        if (HelpTools.Approximately(cam.transform.localPosition.z, camInitialZ, approximationTolerance) &&
            HelpTools.Approximately((transform.position - defCamWrapperPosition).sqrMagnitude, 0, approximationTolerance))
        {
            return true;
        }

        var camLocPos = cam.transform.localPosition;
        camLocPos.z = Mathf.MoveTowards(camLocPos.z, camInitialZ, camMaxDistanceDelta * Time.deltaTime);
        cam.transform.localPosition = camLocPos;

        transform.position = Vector3.MoveTowards(transform.position, defCamWrapperPosition, camWrapperMaxDistanceDelta * Time.deltaTime);

        return false;
    }

    public bool SettingDefaultUpDirection()
    {
        if (HelpTools.Approximately(transform.eulerAngles.z, 0, approximationTolerance))
        {
            return true;
        }

        var camEulerAngles = transform.eulerAngles;
        camEulerAngles.z = Mathf.MoveTowardsAngle(camEulerAngles.z, 0, camToDefaultUpDeltaAngles * Time.deltaTime);
        transform.eulerAngles = camEulerAngles;

        return false;
    }

    public void OnWeaponShopOpen()
    {
        HangarWeaponsHolder.Instance[Shop.VehicleInView.Upgrades.DecalId].transform.SetParent(HangarWeaponsHolder.Instance.transform, false);
        savedSoldierPosition = Shop.VehicleInView.HangarVehicle.transform.position;
        Shop.VehicleInView.HangarVehicle.transform.position -= Vector3.right * 10000;
        normalCamWrapperPos = transform.position;
        savedWrapperEulerAngles = transform.localEulerAngles;
        transform.localEulerAngles = Vector3.up*90;
        rotationSpeed = Vector2.zero;
        transform.position = weaponsPoint.transform.position;
        normalCamPos = cam.transform.localPosition;
        cam.transform.localPosition = weaponShopCamPos;
    }

    private void OnWeaponShopClosed()
    {
        Shop.VehicleInView.HangarVehicle.transform.position = savedSoldierPosition;
        transform.position = normalCamWrapperPos;
        transform.eulerAngles = savedWrapperEulerAngles;
        cam.transform.localPosition = normalCamPos;
        rotationSpeed = Vector2.zero;
        HangarWeaponsHolder.Instance.GiveCurrentWeaponToSoldier();
    }

    private void OnTouchableAreaChanged(EventId id, EventInfo info)
    {
        if (HangarController.Instance == null)
            return;

        touchableArea.xMin = 0;

        if (ScoresController.Instance != null)
        {
            float scoresBoxMaxX =
                HangarController.Instance.GuiCamera.WorldToScreenPoint(
                    ScoresController.Instance.mainBackground.GetComponent<Renderer>().bounds.max).x;

            touchableArea.xMin = ScoresController.Instance.mainLayout.isActiveAndEnabled ? scoresBoxMaxX : touchableArea.xMin;
        }

        touchableArea.xMax =
            HangarController.Instance.GuiCamera.WorldToScreenPoint(
                HangarController.Instance.Tk2dGuiCamera.ScreenExtents.max).x;

        if (RightPanel.Instance != null)
        {
            if (cachedRightGuiPanelRenderer == null)
                cachedRightGuiPanelRenderer = RightPanel.Instance.rightPanel.scrollableArea.GetComponent<Renderer>();

            touchableArea.xMax =
                RightPanel.Instance.rightPanel.gameObject.activeInHierarchy
                    ? HangarController.Instance.GuiCamera.WorldToScreenPoint(cachedRightGuiPanelRenderer.bounds.min).x
                    : touchableArea.xMax;
        }

        if (cachedTopGuiPanelRenderer == null)
            cachedTopGuiPanelRenderer = topPanel.GetComponent<Renderer>();
        
        touchableArea.yMin = HangarController.Instance.GuiCamera.WorldToScreenPoint(cachedTopGuiPanelRenderer.bounds.min).y;

        if (cachedBottomGuiPanelRenderer == null)
            cachedBottomGuiPanelRenderer = MenuController.Instance.bottomGuiPanel.GetComponent<Renderer>();

        touchableArea.yMax = HangarController.Instance.GuiCamera.WorldToScreenPoint(cachedBottomGuiPanelRenderer.bounds.max).y;
    }

    public void ApproximateToDefaultRotationSpeed()
    {
        rotationSpeed = Vector2.Lerp(rotationSpeed, idleRotationSpeed, Time.deltaTime);
    }

    public void RotateCamera()
    {
        var camEulerAngles = transform.eulerAngles;
        camEulerAngles.y += rotationSpeed.y * Time.deltaTime;
        camEulerAngles.x = HelpTools.ClampAngle(camEulerAngles.x + rotationSpeed.x * Time.deltaTime, minXAngle, maxXAndle);
        transform.eulerAngles = camEulerAngles;
    }

    private void GetMouseTransitions()
    {
        deltaMousePosition = (Vector2) Input.mousePosition - prevMousePosition;
        prevMousePosition = Input.mousePosition;
    }

    private float GetScreenRelFingersDist(Vector2 touch0, Vector2 touch1)
    {
        return Vector2.Distance(HelpTools.GetScreenRelPos(touch0), HelpTools.GetScreenRelPos(touch1));
    }

    private void SetRotationSpeed(Vector2 deltaPosition)
    {
        rotationSpeed.y = deltaPosition.x;
        //rotationSpeed.x = -deltaPosition.y;

        idleRotationSpeed.y = Mathf.Sign(rotationSpeed.y)*defaultRotationSpeed.y;
    }

    private void PinchZoomCamTransition(float relDist)
    {
        return;

        var locPos = cam.transform.localPosition;
        locPos.z = Mathf.Clamp(-maxCameraZoomZ * (relDist - initialZoomFingersDist) * 2/ HelpTools.NativeWidth + camBeforeZoomZPos, minCameraZoomZ, maxCameraZoomZ);
        cam.transform.localPosition = locPos;
    }

    private void ScrollZoomCamTransition(float deltaZ)
    {
        return;

        var locPos = cam.transform.localPosition;
        locPos.z = Mathf.Clamp(locPos.z + deltaZ, minCameraZoomZ, maxCameraZoomZ);
        cam.transform.localPosition = locPos;
    }

    /// <summary>
    ///Для отображения области прокрутки танка
    /// </summary>
    //private void OnGUI()
    //{
    //    GUI.Button(new Rect { xMin = touchableArea.xMin, xMax = touchableArea.xMax, yMin = Screen.height - touchableArea.yMin, yMax = Screen.height - touchableArea.yMax }, "Rotation");
    //}

    public void FindCamLookPoints()
    {
        CameraLookPoints lookPoints = null;

        if (HangarVehiclesSwitcher.HangarVehicleCamParams != null)
            lookPoints = HangarVehiclesSwitcher.HangarVehicleCamParams.CameraLookPoints;

        camLookPoints[TankModuleInfos.ModuleType.Cannon] = lookPoints ? lookPoints.Cannon : new CamLookPoint(GetLookPoint("CamLookPoints/Cannon"), GetLookPoint("CamLookPoints/Cannon/pivot"));
        camLookPoints[TankModuleInfos.ModuleType.Tracks] = lookPoints ? lookPoints.Tracks : new CamLookPoint(GetLookPoint("CamLookPoints/Tracks"), GetLookPoint("CamLookPoints/Tracks/pivot"));
        camLookPoints[TankModuleInfos.ModuleType.Armor] = lookPoints ? lookPoints.Armor : new CamLookPoint(GetLookPoint("CamLookPoints/Armor"), GetLookPoint("CamLookPoints/Armor/pivot"));
        camLookPoints[TankModuleInfos.ModuleType.Engine] = lookPoints ? lookPoints.Engine : new CamLookPoint(GetLookPoint("CamLookPoints/Engine"), GetLookPoint("CamLookPoints/Engine/pivot"));
        camLookPoints[TankModuleInfos.ModuleType.Reloader] = lookPoints ? lookPoints.Reloader : new CamLookPoint(GetLookPoint("CamLookPoints/Reloader"), GetLookPoint("CamLookPoints/Reloader/pivot"));

        isCamLookPointsFound = camLookPoints.All(point => point.Value.LookPoint != null);
    }

    private static Transform GetLookPoint(string path)
    {
        return Shop.VehicleInView.HangarVehicle.transform.Find(path);
    }
}
