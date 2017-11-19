using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class CamLookPoint
{
    private Vector3 wrapperPosition;
    private Vector3 camPosition;
    private Quaternion wrapperRotation;

    public Vector3 WrapperPosition
    {
        get { return wrapperPosition; }
    }

    public Vector3 CamPosition
    {
        get { return camPosition; }
    }

    public Quaternion WrapperRotation
    {
        get { return wrapperRotation; }
    }

    public void SetWrapperPosition(Vector3 wrapperPosition)
    {
        this.wrapperPosition = wrapperPosition;
    }

    public void SetCamPosition(Vector3 camPosition)
    {
        this.camPosition = camPosition;
    }

    public void SetWrapperRotation(Quaternion rotation)
    {
        this.wrapperRotation = rotation;
    }
}

public class CamLookTransform
{
    public const string CAM_LOOK_POINTS_PATH = "CamLookPoints";

    private readonly Transform lookTransform;
    private readonly Transform pivot;

    public CamLookTransform(TankModuleInfos.ModuleType moduleType)
    {
        string path = string.Format("{0}/{1}", CAM_LOOK_POINTS_PATH, moduleType);

        this.lookTransform = Shop.VehicleInView.HangarVehicle.transform.Find(path);

        if (this.lookTransform == null)
            this.lookTransform = Shop.CurrentVehicle.HangarVehicle.transform.Find(path);

        if (this.lookTransform == null)
        {
            Debug.LogError("lookTransform == null");
            return;
        }

        this.pivot = lookTransform.Find("pivot");
    }

    public Transform LookTransform
    {
        get { return lookTransform; }
    }

    public Transform Pivot
    {
        get { return pivot; }
    }
}

public class HangarCameraController : MonoBehaviour
{
#pragma warning disable 649
    [Header("Общее")]
    [SerializeField] private Camera cam;
    [SerializeField] private GameObject topPanel;
    [SerializeField] private bool logEnabled;
    [SerializeField] private int minXAngle = 0;
    [SerializeField] private int maxXAngle = 87;
    [SerializeField] private int zoomSpeedPC = 300;
    [SerializeField] private float minCameraZoomZ = -5;
    [SerializeField] private float maxCameraZoomZ = -8;
    [SerializeField] private float defaultRotationSpeedX;
    [SerializeField] private float defaultRotationSpeedY = 10;
    [SerializeField] private float maxDegreesDelta = 40;
    [SerializeField] private float maxDistanceDelta = 5;
    [SerializeField] private float minCamApproachTime = 1;
    [SerializeField] private float maxCamApproachTime = 3;
    [SerializeField] private float fromToOrbitApproachTime = 2;
    [SerializeField] private float camMoveToMaxZoomSpeed = 3;
    [SerializeField] private float camMoveToMaxZoomDelay = 0.2f;
    [SerializeField] private bool autoMaxZoom = true;
    [SerializeField] private Renderer cachedTopGuiPanelRenderer;
    [SerializeField] private Renderer cachedBottomGuiPanelRenderer;
    [SerializeField] private Renderer cachedRightGuiPanelRenderer;

    [Header("Проектозависимые костыли")]
    [SerializeField] private float maxYPositionBOHPrem = 16.0f;
    [SerializeField] private float dafaultCameraZoomZBOH = -16;
#pragma warning restore 649

    private const float PREPARE_ROTATION_ZOOM = 18.0f;
    private const float PREPARE_ROTATION_DURATION = 1.5f;

    private readonly Dictionary<TankModuleInfos.ModuleType, CamLookTransform> camLookTransforms = new Dictionary<TankModuleInfos.ModuleType, CamLookTransform>(5)
    {
        { TankModuleInfos.ModuleType.Cannon, null },
        { TankModuleInfos.ModuleType.Armor, null },
        { TankModuleInfos.ModuleType.Tracks, null },
        { TankModuleInfos.ModuleType.Engine, null },
        { TankModuleInfos.ModuleType.Reloader, null }
    };

    private readonly List<IEnumerator> coroutines = new List<IEnumerator>();

    private bool isRestoringPosition;
    private bool isCamLookPointsFound;
    private bool preventCamRotation; // Временно запрещаем вращать камеру и приближать зум.
    private bool moduleClickedOncePerArmoryEnter;
    private float rotationSpeedX;
    private float rotationSpeedY;
    private float distanceBetweenFingers;
    private float lastDistanceBetweenFingers;
    private float deltaMousePositionX;
    private float deltaMousePositionY;
    private float camZCoord;
    private float timeToApproach;
    private float wrapperApproachDistance;
    private float wrapperApproachDeltaAngle;
    private float currentDeltaAngle;
    private float camMoveBackAngleFixTime;
    private float lastModuleClickedTime;
    private Rect touchableArea;
    private Vector3 lastMousePosition;
    private Vector3 initialPosition;
    private Vector3 camEulerAngles;

    public static HangarCameraController Instance
    {
        get; private set;
    }

    public CamLookTransform CurrentCamLookTransform
    {
        get
        {
            if (ModuleShop.Instance.ModuleInView == null)
                return null;

            switch (ModuleShop.Instance.ModuleInView.type)
            {
                case TankModuleInfos.ModuleType.Cannon:
                    return camLookTransforms[TankModuleInfos.ModuleType.Cannon];
                case TankModuleInfos.ModuleType.Engine:
                    return camLookTransforms[TankModuleInfos.ModuleType.Engine];
                case TankModuleInfos.ModuleType.Tracks:
                    return camLookTransforms[TankModuleInfos.ModuleType.Tracks];
                case TankModuleInfos.ModuleType.Armor:
                    return camLookTransforms[TankModuleInfos.ModuleType.Armor];
                case TankModuleInfos.ModuleType.Reloader:
                    return camLookTransforms[TankModuleInfos.ModuleType.Reloader];
                default:
                    return null;
            }
        }
    }

    private bool CanControlCam
    {
        get { return GUIPager.ActivePage != null && GUIPager.ActivePage.AllowCamRotationControl && !preventCamRotation; }
    }

    private bool CanRotateCam
    {
        get { return GUIPager.ActivePage != null && GUIPager.ActivePage.AllowCamRotation && !preventCamRotation; }
    }

    private bool IsCamZoomRestored
    {
        get { return Mathf.Approximately(maxCameraZoomZ - cam.transform.localPosition.z, 0); }
    }

    private bool IsCamWrapperPosRestored
    {
        get { return Vector3.Distance(transform.position, initialPosition) < 0.05f; }
    }

    void Awake()
    {
        Instance = this;

        Dispatcher.Subscribe(EventId.TouchableAreaChanged, OnTouchableAreaChanged);
        Dispatcher.Subscribe(EventId.ResolutionChanged, OnTouchableAreaChanged);
        Dispatcher.Subscribe(EventId.VipStatusUpdated, SetCameraZoomRange);
        Dispatcher.Subscribe(EventId.AfterHangarInit, SetCameraZoomRange);
        Dispatcher.Subscribe(EventId.VehicleSelected, OnVehicleSelected);
        Dispatcher.Subscribe(EventId.AfterHangarInit, OnVehicleSelected);

        GUIPager.OnPageChange += OnPageChange;

        cam.transform.LookAt(transform);
    }

    void OnDestroy()
    {
        Dispatcher.Unsubscribe(EventId.TouchableAreaChanged, OnTouchableAreaChanged);
        Dispatcher.Unsubscribe(EventId.ResolutionChanged, OnTouchableAreaChanged);
        Dispatcher.Unsubscribe(EventId.VipStatusUpdated, SetCameraZoomRange);
        Dispatcher.Unsubscribe(EventId.AfterHangarInit, SetCameraZoomRange);
        Dispatcher.Unsubscribe(EventId.VehicleSelected, OnVehicleSelected);
        Dispatcher.Unsubscribe(EventId.AfterHangarInit, OnVehicleSelected);

        GUIPager.OnPageChange -= OnPageChange;

        Instance = null;
    }

    void Start()
    {
        cachedTopGuiPanelRenderer = topPanel.GetComponent<Renderer>();
        cachedRightGuiPanelRenderer = RightPanel.Instance != null ? RightPanel.Instance.rightPanel.scrollableArea.GetComponent<Renderer>() : null;
        cachedBottomGuiPanelRenderer = HangarController.Instance.bottomGuiPanel.GetComponent<Renderer>();
        camEulerAngles = transform.eulerAngles;
        initialPosition = transform.position;
        camMoveBackAngleFixTime = fromToOrbitApproachTime * 0.5f;

        if (GameData.IsGame(Game.BattleOfWarplanes | Game.WingsOfWar))
        {
            Vector3 localPosition = cam.transform.localPosition;
            localPosition.z = dafaultCameraZoomZBOH;
            cam.transform.localPosition = localPosition;
        }
    }

    void Update()
    {
        OnTouchMovement();
        PinchZoom();
        CorrectVerticalPosition();
    }

    /// <summary>
    /// Для отображения области прокрутки танка.
    /// </summary>
    //private void OnGUI()
    //{
    //    GUI.Button(new Rect { xMin = touchableArea.xMin, xMax = touchableArea.xMax, yMin = Screen.height - touchableArea.yMin, yMax = Screen.height - touchableArea.yMax }, "Rotation");
    //}

    public void OnVehicleSelected(EventId id, EventInfo info)
    {
        if (HangarVehiclesSwitcher.HangarVehicleCamParams == null)
            return;

        maxCameraZoomZ = HangarVehiclesSwitcher.HangarVehicleCamParams.MinDistanceToVehicle;

        coroutines.Add(CamToMaxZoomRoutine());

        StartCamApproachCoroutines();
    }

    public void OnModuleClicked()
    {
        if (ModuleShop.Instance.ModuleInView == null || !isCamLookPointsFound)
            return;

        float currentTime = Time.time;

        if (HelpTools.Approximately(currentTime, lastModuleClickedTime))
            return;

        rotationSpeedX = 0;
        rotationSpeedY = 0;

        Approach(CurrentCamLookTransform);

        moduleClickedOncePerArmoryEnter = true;

        lastModuleClickedTime = currentTime;
    }

    public void FindCamLookPoints()
    {
        camLookTransforms[TankModuleInfos.ModuleType.Cannon] = new CamLookTransform(TankModuleInfos.ModuleType.Cannon);
        camLookTransforms[TankModuleInfos.ModuleType.Tracks] = new CamLookTransform(TankModuleInfos.ModuleType.Tracks);
        camLookTransforms[TankModuleInfos.ModuleType.Armor] = new CamLookTransform(TankModuleInfos.ModuleType.Armor);
        camLookTransforms[TankModuleInfos.ModuleType.Engine] = new CamLookTransform(TankModuleInfos.ModuleType.Engine);
        camLookTransforms[TankModuleInfos.ModuleType.Reloader] = new CamLookTransform(TankModuleInfos.ModuleType.Reloader);

        isCamLookPointsFound = camLookTransforms.All(point => point.Value.LookTransform != null);
    }

    private void SetCameraZoomRange(EventId id, EventInfo info)
    {
        if (GameData.IsGame(Game.IronTanks) && ProfileInfo.IsPlayerVip) // Костыль, чтобы в VIP ангаре и отлетало подальше.
        {
            minCameraZoomZ = -10f;
            maxCameraZoomZ = -4.2f;
        }
    }

    private void OnTouchableAreaChanged(EventId id, EventInfo info)
    {
        if (HangarController.Instance == null)
            return;

        touchableArea.xMin = 0;

        if (ScoresController.Instance != null)
        {
            float scoresBoxMaxX = HangarController.Instance.GuiCamera.WorldToScreenPoint(ScoresController.Instance.mainBackground.GetComponent<Renderer>().bounds.max).x;
            touchableArea.xMin = ScoresController.Instance.mainLayout.isActiveAndEnabled ? scoresBoxMaxX : touchableArea.xMin;
        }

        touchableArea.xMax = HangarController.Instance.GuiCamera.WorldToScreenPoint(HangarController.Instance.Tk2dGuiCamera.ScreenExtents.max).x;

        if (RightPanel.Instance != null)
        {
            if (cachedRightGuiPanelRenderer == null)
                cachedRightGuiPanelRenderer = RightPanel.Instance.rightPanel.scrollableArea.GetComponent<Renderer>();

            touchableArea.xMax
                = RightPanel.Instance.rightPanel.gameObject.activeInHierarchy
                    ? HangarController.Instance.GuiCamera.WorldToScreenPoint(cachedRightGuiPanelRenderer.bounds.min).x
                    : touchableArea.xMax;
        }

        if (cachedTopGuiPanelRenderer == null)
            cachedTopGuiPanelRenderer = topPanel.GetComponent<Renderer>();

        touchableArea.yMin = HangarController.Instance.GuiCamera.WorldToScreenPoint(cachedTopGuiPanelRenderer.bounds.min).y;

        if (cachedBottomGuiPanelRenderer == null)
            cachedBottomGuiPanelRenderer = HangarController.Instance.bottomGuiPanel.GetComponent<Renderer>();

        touchableArea.yMax = HangarController.Instance.GuiCamera.WorldToScreenPoint(cachedBottomGuiPanelRenderer.bounds.max).y;
    }

    private void OnTouchMovement()
    {
        if (CanControlCam && !ScoresPage.IsScrolling)
        {
            if (Input.touchCount == 1 && touchableArea.Contains(Input.GetTouch(0).position, true))
            {
                switch (Input.GetTouch(0).phase)
                {
                    case TouchPhase.Moved:
                        SetRotationSpeedX(GetRealDeltaTouchPos(Input.GetTouch(0).deltaPosition).y * 15000);
                        SetRotationSpeedY(GetRealDeltaTouchPos(Input.GetTouch(0).deltaPosition).x * 15000);
                        break;
                    case TouchPhase.Stationary:
                        FreezeMotion();
                        break;
                }
            }
#if UNITY_WEBPLAYER || UNITY_STANDALONE || UNITY_EDITOR || UNITY_WEBGL || UNITY_WSA
            else if (touchableArea.Contains(Input.mousePosition, true))
            {
                GetMouseTransitions();

                if (HelpTools.Approximately(deltaMousePositionX + deltaMousePositionY, 0) && Input.GetMouseButton(0))
                {
                    FreezeMotion();
                    return;
                }
                if (Input.GetMouseButton(0))
                {
                    SetRotationSpeedX(deltaMousePositionY);
                    SetRotationSpeedY(deltaMousePositionX);
                }
            }
#endif
        }

        if (CanRotateCam)
        {
            rotationSpeedX = Mathf.Lerp(rotationSpeedX, defaultRotationSpeedX, Time.deltaTime);
            rotationSpeedY = Mathf.Lerp(rotationSpeedY, defaultRotationSpeedY, Time.deltaTime);
            RotateCamera();
        }
    }

    private void OnPageChange(string from, string to)
    {
        if (!isCamLookPointsFound)
            return;

        if (from == "Armory")
        {
            moduleClickedOncePerArmoryEnter = false;
            StopCamApproachCoroutines();
            coroutines.Add(SetDefaultUpDirection());
            coroutines.Add(MoveBackToOrbit());
            coroutines.Add(PreventRotation());
        }

        if (to == "Armory")
        {
            camEulerAngles = transform.eulerAngles;
            StopCoroutine("MoveBackToOrbit");
        }

        if (from == "VehicleShopWindow")
        {
            HangarVehicleCamParams hangarVehicleCamParams = Shop.CurrentVehicle.HangarVehicle.GetComponent<HangarVehicleCamParams>();

            if (hangarVehicleCamParams != null)
            {
                maxCameraZoomZ = hangarVehicleCamParams.MinDistanceToVehicle;
                StopCamApproachCoroutines();
                coroutines.Add(CamToMaxZoomRoutine());
            }
        }

        StartCamApproachCoroutines();
    }

    private void StartCamApproachCoroutines()
    {
        StopCamApproachCoroutines(false);

        foreach (IEnumerator coroutine in coroutines)
            StartCoroutine(coroutine);
    }

    private void StopCamApproachCoroutines(bool clearCoroutinesList = true)
    {
        if (coroutines.Count != 0)
        {
            foreach (IEnumerator coroutine in coroutines)
                StopCoroutine(coroutine);

            if (clearCoroutinesList)
                coroutines.Clear();
        }
    }

    private void GetTimeToApproach(CamLookTransform lookTransform)
    {
        wrapperApproachDeltaAngle = Quaternion.Angle(transform.rotation, lookTransform.LookTransform.rotation);
        wrapperApproachDistance = Vector3.Distance(transform.position, lookTransform.Pivot.position);

        float wrapperMoveTime = wrapperApproachDistance / maxDistanceDelta;
        float wrapperRotateTime = wrapperApproachDeltaAngle / maxDegreesDelta;

        timeToApproach = wrapperMoveTime > wrapperRotateTime ? wrapperMoveTime : wrapperRotateTime;
        timeToApproach = Mathf.Clamp(timeToApproach, minCamApproachTime, maxCamApproachTime);
    }

    private void Approach(CamLookTransform camLookTransform)
    {
        GetTimeToApproach(camLookTransform);

        StopCamApproachCoroutines();

        float prepareRotationDelay = 0;

        if (!moduleClickedOncePerArmoryEnter)
        {
            prepareRotationDelay = PREPARE_ROTATION_DURATION;
            coroutines.Add(PrepareRotation(camLookTransform.Pivot.forward, prepareRotationDelay));
        }

        coroutines.Add(RotateCamWrapper(camLookTransform.LookTransform.forward, prepareRotationDelay));
        coroutines.Add(MoveCamWrapper(camLookTransform.Pivot.position, prepareRotationDelay));
        coroutines.Add(MoveCam(camLookTransform.Pivot.localPosition.z, prepareRotationDelay));

        StartCamApproachCoroutines();
    }

    private void PinchZoom()
    {
        if (!CanControlCam)
            return;

        if (Input.touchCount == 2)
        {
            distanceBetweenFingers = Vector2.Distance(GetRealDeltaTouchPos(Input.GetTouch(0).position), GetRealDeltaTouchPos(Input.GetTouch(1).position));

            if (Input.GetTouch(0).phase == TouchPhase.Moved || Input.GetTouch(1).phase == TouchPhase.Moved)
            {
                if (distanceBetweenFingers > lastDistanceBetweenFingers)
                    Zoom(-1f * distanceBetweenFingers * 15f);

                else
                    Zoom(distanceBetweenFingers * 15f);

            }

            lastDistanceBetweenFingers = distanceBetweenFingers;
        }

#if UNITY_WEBPLAYER || UNITY_STANDALONE || UNITY_EDITOR || UNITY_WEBGL || UNITY_WSA
        if (touchableArea.Contains(Input.mousePosition, true) && !Mathf.Approximately(Input.GetAxisRaw("Mouse ScrollWheel"), 0))
            Zoom(Input.GetAxisRaw("Mouse ScrollWheel") * -zoomSpeedPC);
#endif
    }

    private void CorrectVerticalPosition()
    {
        if (GameData.IsGame(Game.BattleOfWarplanes | Game.WingsOfWar) && ProfileInfo.IsPlayerVip)
        {
            if (cam.transform.position.y > maxYPositionBOHPrem)
                Zoom(0.05f * -zoomSpeedPC);
        }
    }

    private Vector3 GetRealDeltaTouchPos(Vector2 deltaPos)
    {
        return cam.ScreenToViewportPoint(deltaPos);
    }

    private void FreezeMotion()
    {
        rotationSpeedX = 0;
        rotationSpeedY = 0;
    }

    private void RotateCamera()
    {
        camEulerAngles.y += rotationSpeedY * Time.deltaTime;

        if (GameData.IsGame(Game.SpaceJet))
            camEulerAngles.x += rotationSpeedX * Time.deltaTime;
        else
            camEulerAngles.x = HelpTools.ClampAngle(camEulerAngles.x + rotationSpeedX * Time.deltaTime, minXAngle, maxXAngle);

        transform.eulerAngles = camEulerAngles;
    }

    private void GetMouseTransitions()
    {
        deltaMousePositionX = Input.mousePosition.x - lastMousePosition.x;
        deltaMousePositionY = Input.mousePosition.y - lastMousePosition.y;
        lastMousePosition = Input.mousePosition;
    }

    private void SetRotationSpeedY(float deltaPositionX)
    {
        rotationSpeedY = deltaPositionX;

        if (rotationSpeedY < 0)
            defaultRotationSpeedY = -Mathf.Abs(defaultRotationSpeedY);
        else
            defaultRotationSpeedY = Mathf.Abs(defaultRotationSpeedY);
    }

    private void SetRotationSpeedX(float deltaPositionY)
    {
        rotationSpeedX = deltaPositionY * -1;
    }

    private void Zoom(float zoomSpeed)
    {
        //StopCamApproachCoroutines(); // Пока закомментил. Мешает при выходе из окна модулей.

        if (isRestoringPosition) // Запрещаем зумить, если камера не восстановила своё положение.
            zoomSpeed = 0;

        camZCoord = Mathf.MoveTowards(cam.transform.localPosition.z, zoomSpeed > 0 ? minCameraZoomZ : maxCameraZoomZ, Math.Abs(zoomSpeed) * Time.deltaTime);
        cam.transform.localPosition = new Vector3(cam.transform.localPosition.x, cam.transform.localPosition.y, camZCoord);
    }

    private IEnumerator CamToMaxZoomRoutine()
    {
        if (!autoMaxZoom)
            yield break;

        yield return new WaitForSeconds(camMoveToMaxZoomDelay);

        float newPosition = (minCameraZoomZ + maxCameraZoomZ) * 0.5f;

        while (!Mathf.Approximately(newPosition, cam.transform.localPosition.z) && !isRestoringPosition)
        {
            Vector3 localPosition = cam.transform.localPosition;
            localPosition.z = Mathf.MoveTowards(localPosition.z, newPosition, camMoveToMaxZoomSpeed * Time.deltaTime);
            cam.transform.localPosition = localPosition;
            yield return null;
        }
    }

    private IEnumerator PreventRotation()
    {
        float time = Time.realtimeSinceStartup;

        preventCamRotation = true;

        while (Time.realtimeSinceStartup - time < camMoveBackAngleFixTime)
            yield return null;

        preventCamRotation = false;
    }

    private IEnumerator PrepareRotation(Vector3 direction, float duration)
    {
        Vector3 forwardDirection = Vector3.ProjectOnPlane(direction, Vector3.up).normalized;
        Quaternion camWrapperUpRotation = Quaternion.LookRotation(forwardDirection, Vector3.up);
        float camWrapperAngle = Quaternion.Angle(transform.rotation, camWrapperUpRotation);
        float camWrapperDeltaAngle = camWrapperAngle / duration;

        float maxDeltaCamPosZ = PREPARE_ROTATION_ZOOM / duration;
        float startTime = Time.realtimeSinceStartup;

        while (Time.realtimeSinceStartup - startTime < duration)
        {
            Vector3 camLocPos = cam.transform.localPosition;
            camLocPos.z = Mathf.MoveTowards(camLocPos.z, -PREPARE_ROTATION_ZOOM, maxDeltaCamPosZ * Time.deltaTime);
            cam.transform.localPosition = camLocPos;

            transform.rotation = Quaternion.RotateTowards(transform.rotation, camWrapperUpRotation, camWrapperDeltaAngle * Time.deltaTime);

            yield return null;
        }
    }

    private IEnumerator RotateCamWrapper(Vector3 direction, float delay)
    {
        while (delay > 0)
        {
            delay -= Time.deltaTime;
            yield return null;
        }

        float startTime = Time.realtimeSinceStartup;
        currentDeltaAngle = wrapperApproachDeltaAngle / timeToApproach;

        while (Time.realtimeSinceStartup - startTime < timeToApproach)
        {
            transform.rotation
                = Quaternion.RotateTowards(
                    from:               transform.rotation,
                    to:                 Quaternion.LookRotation(direction, Vector3.up),
                    maxDegreesDelta:    currentDeltaAngle * Time.deltaTime);

            yield return null;
        }
    }

    private IEnumerator MoveCamWrapper(Vector3 targetPosition, float delay)
    {
        while (delay > 0)
        {
            delay -= Time.deltaTime;
            yield return null;
        }

        float startTime = Time.realtimeSinceStartup;
        float maxDistDelta = wrapperApproachDistance / timeToApproach;

        while (Time.realtimeSinceStartup - startTime < timeToApproach)
        {
            transform.position
                = Vector3.MoveTowards(
                    current:            transform.position,
                    target:             targetPosition,
                    maxDistanceDelta:   maxDistDelta * Time.deltaTime);

            yield return null;
        }
    }

    private IEnumerator MoveCam(float distance, float delay)
    {
        while (delay > 0)
        {
            delay -= Time.deltaTime;
            yield return null;
        }

        float startTime = Time.realtimeSinceStartup;
        float dist = Mathf.Abs(cam.transform.localPosition.z + distance);
        float maxDistDelta = dist / timeToApproach;

        while (Time.realtimeSinceStartup - startTime < timeToApproach)
        {
            Vector3 camLocPos = cam.transform.localPosition;
            camLocPos.z = Mathf.MoveTowards(camLocPos.z, -distance, maxDistDelta * Time.deltaTime);
            cam.transform.localPosition = camLocPos;

            yield return null;
        }
    }

    private IEnumerator MoveBackToOrbit()
    {
        isRestoringPosition = true;

        float dist = Vector3.Distance(transform.position, initialPosition);
        float wrapperMaxDistDelta = dist / fromToOrbitApproachTime;

        float localDist = Math.Abs(maxCameraZoomZ - cam.transform.localPosition.z);
        float camMaxDistDelta = localDist / fromToOrbitApproachTime;

        while (!IsCamZoomRestored|| !IsCamWrapperPosRestored)
        {
            camEulerAngles = transform.eulerAngles;

            transform.position = Vector3.MoveTowards(transform.position, initialPosition, wrapperMaxDistDelta * Time.deltaTime);

            Vector3 camLocPos = cam.transform.localPosition;  
                 
            camLocPos.z = Mathf.MoveTowards(camLocPos.z, maxCameraZoomZ, camMaxDistDelta * Time.deltaTime);
            cam.transform.localPosition = camLocPos;

            yield return null;
        }

        isRestoringPosition = false;
    }

    private IEnumerator SetDefaultUpDirection()
    {
        Vector3 defaultForwardDirection = Vector3.ProjectOnPlane(transform.forward, Vector3.up).normalized;
        Quaternion camWrapperUpRotation = Quaternion.LookRotation(defaultForwardDirection, Vector3.up);
        float angle = Vector3.Angle(Vector3.up, transform.up);
        float delta = angle / camMoveBackAngleFixTime;
        float time = Time.realtimeSinceStartup;

        while (Time.realtimeSinceStartup - time < camMoveBackAngleFixTime)
        {
            transform.rotation = Quaternion.RotateTowards(transform.rotation, camWrapperUpRotation, delta * Time.deltaTime);
            yield return null;
        }  
    }
}
