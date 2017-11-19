using System;
using System.Collections;
using System.Collections.Generic;
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
    [SerializeField] private Camera cam;

    [SerializeField] private int minXAngle = 0;
    [SerializeField] private int maxXAndle = 87;
    [SerializeField] private int zoomSpeedPC = 300;
    [SerializeField] private float minCameraZoomZ = -5;
    [SerializeField] private float maxCameraZoomZ = -9.5f;
    [SerializeField] private float maxCameraZoomZDefault = -8;
    [SerializeField] private float defaultRotationSpeedX;
    [SerializeField] private float defaultRotationSpeedY = 10;

    [Header("Module Shop Cam Approach Params:")]
    [SerializeField] private float maxDegreesDelta = 40;
    [SerializeField] private float maxDistanceDelta = 5;
    [SerializeField] private float minCamApproachTime = 1;
    [SerializeField] private float maxCamApproachTime = 3;
    [SerializeField] private float fromToOrbitApproachTime = 2;
    [SerializeField] private float camMoveToMaxZoomSpeed = 3;
    [SerializeField] private float camMoveToMaxZoomDelay = 0.2f;
    [SerializeField] private bool autoMaxZoom = true;

    [SerializeField] private float equalizationAvarageSpeed = 5f;

    public Dictionary<TankModuleInfos.ModuleType, CamLookPoint> camLookPoints = new Dictionary<TankModuleInfos.ModuleType, CamLookPoint>(5)
    {
        {TankModuleInfos.ModuleType.Cannon, null},
        {TankModuleInfos.ModuleType.Armor, null},
        {TankModuleInfos.ModuleType.Tracks, null},
        {TankModuleInfos.ModuleType.Engine, null},
        {TankModuleInfos.ModuleType.Reloader, null}
    };

    private Renderer cachedBottomGuiPanelRenderer;
    private BoxCollider cachedRightGuiPanelCollider;
    private BoxCollider cachedScoresBoxCollider;

    private Rect touchableArea;
    private bool touchableAreaInited;
    private float rotationSpeedX;
    private float rotationSpeedY;
    private float fingerPositionX;
    private float rotationX;
    private float distanceBetweenFingers;
    private float prevDistanceBetweenFingers;
    private Vector3 prevMousePosition;
    private Vector3 defPosition;
    private float deltaMousePositionX;
    private float deltaMousePositionY;
    private float camZCoord;
    private bool floorCollision;
    private Vector3 camEulerAngles;
    private RaycastHit hitInfo;
    private float cameraHeight = 0;

    //cam approach
    private bool isCamLookPointsFound;
    private CamLookPoint camLookPoint;
    private float timeToApproach;
    private List<IEnumerator> coroutines = new List<IEnumerator>();
    private float wrapperApproachDist;
    private float wrapperApproachAngleDiff;
    private float curDegreesDelta;
    private float camMoveBackAngleFixTime;

    public static HangarCameraController Instance { get; private set; }
    public static bool CanMoveOnTouch { get; set; }

    void Awake()
    {
        Instance = this;

        GUIPager.OnPageChange += OnPageChange;

        Messenger.Subscribe(EventId.TouchableAreaChanged, OnTouchableAreaChanged);
        Messenger.Subscribe(EventId.ResolutionChanged, OnTouchableAreaChanged);
        Messenger.Subscribe(EventId.VipStatusUpdated, SetCameraZoomRange);
        Messenger.Subscribe(EventId.AfterHangarInit, SetCameraZoomRange);
        Messenger.Subscribe(EventId.VehicleSelected, OnVehicleSelected);
        Messenger.Subscribe(EventId.AfterHangarInit, OnVehicleSelected);

        cam.transform.LookAt(transform);
    }

    void Start()
    {
        cachedBottomGuiPanelRenderer = HangarController.Instance.bottomGuiPanel.GetComponent<Renderer>();
        camEulerAngles = transform.eulerAngles;
        defPosition = transform.position;
        camMoveBackAngleFixTime = fromToOrbitApproachTime * 0.5f;
    }

    void OnDestroy()
    {
        Messenger.Unsubscribe(EventId.TouchableAreaChanged, OnTouchableAreaChanged);
        Messenger.Unsubscribe(EventId.ResolutionChanged, OnTouchableAreaChanged);
        Messenger.Unsubscribe(EventId.VipStatusUpdated, SetCameraZoomRange);
        Messenger.Unsubscribe(EventId.AfterHangarInit, SetCameraZoomRange);
        Messenger.Unsubscribe(EventId.VehicleSelected, OnVehicleSelected);
        Messenger.Unsubscribe(EventId.AfterHangarInit, OnVehicleSelected);

        GUIPager.OnPageChange -= OnPageChange;

        Instance = null;
    }

    void Update()
    {
        if (GUIPager.ActivePage != "Armory" || !isCamLookPointsFound)
        {
            OnTouchMovement();
            PinchZoom();
        }
    }

    public void OnVehicleSelected(EventId id, EventInfo info)
    {
        if (HangarVehiclesSwitcher.HangarVehicleCamParams == null)
        {
            if (maxCameraZoomZ != maxCameraZoomZDefault) maxCameraZoomZ = maxCameraZoomZDefault;

            if (cameraHeight != 0)
            {
                if (DeltaCheck0(cameraHeight - transform.position.y))
                {
                    cameraHeight = 0;
                    StartCoroutine(CamUp());
                }
                else
                {
                    cameraHeight = 0;
                }
            }
            return;
        }

        maxCameraZoomZ = HangarVehiclesSwitcher.HangarVehicleCamParams.MinDistanceToVehicle;
        coroutines.Add(CamToMaxZoomRoutine());

        if (cameraHeight != HangarVehiclesSwitcher.HangarVehicleCamParams.Height)
        {
            if (DeltaCheck0(cameraHeight - transform.position.y))
            {
                cameraHeight = HangarVehiclesSwitcher.HangarVehicleCamParams.Height;
                StartCoroutine(CamUp());
            }
            else
            {
                cameraHeight = HangarVehiclesSwitcher.HangarVehicleCamParams.Height;
            }
        }

        StartCamApproachCoroutines();
    }

    private IEnumerator CamUp()
    {
        float startCamY = transform.position.y;
        float radPerSecond = Mathf.PI * 0.5f / Mathf.Abs(startCamY - cameraHeight) * equalizationAvarageSpeed;
        float t = 0f;
        while (!Mathf.Approximately(t, Mathf.PI * 0.5f))
        {
            t = Mathf.Clamp(t + Time.deltaTime * radPerSecond, 0f, Mathf.PI * 0.5f);
            transform.position = new Vector3(transform.position.x, Mathf.Lerp(startCamY, cameraHeight, Mathf.Sin(t)), transform.position.z);
            yield return null;
        }
    }

    private bool DeltaCheck0(float delta) { return delta > 0 ? delta < 0.01f : delta > -0.01f; }

    private IEnumerator CamToMaxZoomRoutine()
    {
        if (!autoMaxZoom)
        {
            yield break;
        }

        yield return new WaitForSeconds(camMoveToMaxZoomDelay);

        float newPos = (minCameraZoomZ + maxCameraZoomZ) * 0.5f;
        while (!Mathf.Approximately(newPos, cam.transform.localPosition.z))
        {
            var locPos = cam.transform.localPosition;
            locPos.z = Mathf.MoveTowards(locPos.z, newPos, camMoveToMaxZoomSpeed * Time.deltaTime);
            cam.transform.localPosition = locPos;
            yield return null;
        }
    }

    private void OnPageChange(string from, string to)
    {
        if (!isCamLookPointsFound)
        {
            return;
        }

        if (from == "Armory")
        {
            StopCamApproachCoroutines();
            coroutines.Add(SetDefaultUpDirection());
            coroutines.Add(MoveBackToOrbit());
        }

        if (to == "Armory")
        {
            camEulerAngles = transform.eulerAngles;
            StopCoroutine("MoveBackToOrbit");
        }

        if (from == "VehicleShopWindow")
        {
            var hangarVehicleCamParams = Shop.CurrentVehicle.HangarVehicle.GetComponent<HangarVehicleCamParams>();

            if (hangarVehicleCamParams != null)
            {
                maxCameraZoomZ = hangarVehicleCamParams.MinDistanceToVehicle;
                coroutines.Add(CamToMaxZoomRoutine());
            }
        }

        StartCamApproachCoroutines();
    }

    public void OnModuleClicked()
    {
        if (ModuleShop.Instance.ModuleInView == null || !isCamLookPointsFound)
            return;

        rotationSpeedX = 0;
        rotationSpeedY = 0;

        GetLookParams();
        GetTimeToApproach();

        StopCamApproachCoroutines();
        coroutines.Add(RotateCamWrapper());
        coroutines.Add(MoveCamWrapper());
        coroutines.Add(MoveCam());
        StartCamApproachCoroutines();
    }

    private void StopCamApproachCoroutines(bool clearCoroutinesList = true)
    {
        if (coroutines.Count != 0)
        {
            foreach (var coroutine in coroutines)
            {
                StopCoroutine(coroutine);
            }
            if (clearCoroutinesList)
                coroutines.Clear();
        }
    }

    private void StartCamApproachCoroutines()
    {
        StopCamApproachCoroutines(false);
        foreach (var coroutine in coroutines)
        {
            StartCoroutine(coroutine);
        }
    }

    private void GetTimeToApproach()
    {
        wrapperApproachAngleDiff = Quaternion.Angle(transform.rotation, camLookPoint.LookPoint.rotation);
        wrapperApproachDist = Vector3.Distance(transform.position, camLookPoint.Pivot.position);

        var wrapperMoveTime = wrapperApproachDist / maxDistanceDelta;
        var wrapperRotateTime = wrapperApproachAngleDiff / maxDegreesDelta;

        timeToApproach = wrapperMoveTime > wrapperRotateTime ? wrapperMoveTime : wrapperRotateTime;
        timeToApproach = Mathf.Clamp(timeToApproach, minCamApproachTime, maxCamApproachTime);
    }

    private IEnumerator RotateCamWrapper()
    {
        var time = Time.realtimeSinceStartup;
        curDegreesDelta = wrapperApproachAngleDiff / timeToApproach;

        while (Time.realtimeSinceStartup - time < timeToApproach)
        {
            transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(camLookPoint.LookPoint.forward, Vector3.up), curDegreesDelta * Time.deltaTime);
            yield return null;
        }

        //Debug.Log(timeToApproach);
        //Debug.Log(Time.realtimeSinceStartup - time + " rotate wrapper");
    }

    private IEnumerator MoveCamWrapper()
    {
        var time = Time.realtimeSinceStartup;
        var maxDistDelta = wrapperApproachDist / timeToApproach;

        while (Time.realtimeSinceStartup - time < timeToApproach)
        {
            transform.position = Vector3.MoveTowards(transform.position, camLookPoint.Pivot.position, maxDistDelta * Time.deltaTime);
            yield return null;
        }
        //Debug.Log(Time.realtimeSinceStartup - time + " move wrapper");
    }

    private IEnumerator MoveCam()
    {
        var time = Time.realtimeSinceStartup;
        var dist = Mathf.Abs(cam.transform.localPosition.z + camLookPoint.Pivot.localPosition.z);
        var maxDistDelta = dist / timeToApproach;

        while (Time.realtimeSinceStartup - time < timeToApproach)
        {

            var camLocPos = cam.transform.localPosition;
            camLocPos.z = Mathf.MoveTowards(camLocPos.z, -camLookPoint.Pivot.localPosition.z, maxDistDelta * Time.deltaTime);
            cam.transform.localPosition = camLocPos;


            yield return null;
        }
        //Debug.Log(Time.realtimeSinceStartup - time + " move cam");
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

    private IEnumerator MoveBackToOrbit()
    {
        var dist = Vector3.Distance(transform.position, defPosition);
        var wrapperMaxDistDelta = dist / fromToOrbitApproachTime;

        var localDist = Math.Abs(maxCameraZoomZ - cam.transform.localPosition.z);
        var camMaxDistDelta = localDist / fromToOrbitApproachTime;

        while (!Mathf.Approximately(maxCameraZoomZ - cam.transform.localPosition.z, 0))
        {
            camEulerAngles = transform.eulerAngles;
            transform.position = Vector3.MoveTowards(transform.position, defPosition, wrapperMaxDistDelta * Time.deltaTime);
            var camLocPos = cam.transform.localPosition;
            camLocPos.z = Mathf.MoveTowards(camLocPos.z, maxCameraZoomZ, camMaxDistDelta * Time.deltaTime);
            cam.transform.localPosition = camLocPos;
            yield return null;
        }
    }

    private IEnumerator SetDefaultUpDirection()
    {
        var camWrapperUpRotation = Quaternion.LookRotation(transform.forward, Vector3.up);
        var angle = Vector3.Angle(Vector3.up, transform.up);
        var delta = angle / camMoveBackAngleFixTime;
        var time = Time.realtimeSinceStartup;

        while (Time.realtimeSinceStartup - time < camMoveBackAngleFixTime)
        {
            transform.rotation = Quaternion.RotateTowards(transform.rotation, camWrapperUpRotation, delta * Time.deltaTime);
            yield return null;
        }
    }

    private void SetCameraZoomRange(EventId id, EventInfo info)
    {
        if (GameData.IsGame(Game.IronTanks) && ProfileInfo.IsPlayerVip) // костыль , чтобы в vip ангаре ит отлетало подальше
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
            if (cachedScoresBoxCollider == null)
                cachedScoresBoxCollider = ScoresController.Instance.scrollableArea.GetComponent<BoxCollider>();

            float scoresBoxMaxX =
                HangarController.Instance.GuiCamera.WorldToScreenPoint(
                    cachedScoresBoxCollider.bounds.max)
                    .x;

            touchableArea.xMin =
                ScoresController.Instance.mainLayout.gameObject.activeInHierarchy ? scoresBoxMaxX : touchableArea.xMin;
        }

        touchableArea.xMax =
            HangarController.Instance.GuiCamera.WorldToScreenPoint(
                HangarController.Instance.Tk2dGuiCamera.ScreenExtents.max)
                .x;

        if (RightPanel.Instance != null)
        {
            if (cachedRightGuiPanelCollider == null)
                cachedRightGuiPanelCollider = RightPanel.Instance.rightPanel.scrollableArea.GetComponent<BoxCollider>();

            float rightGUIPanelMinXBound =
                HangarController.Instance.GuiCamera.WorldToScreenPoint(cachedRightGuiPanelCollider.bounds.min).x;

            touchableArea.xMax =
                RightPanel.Instance.rightPanel.gameObject.activeInHierarchy
                    ? rightGUIPanelMinXBound
                    : touchableArea.xMax;
        }

        touchableArea.yMin =
            HangarController.Instance.GuiCamera.WorldToScreenPoint(
                HangarController.Instance.topGUIPanel.bounds.min)
                .y;

        if (cachedBottomGuiPanelRenderer == null)
            cachedBottomGuiPanelRenderer = HangarController.Instance.bottomGuiPanel.GetComponent<Renderer>();

        touchableArea.yMax =
            HangarController.Instance.GuiCamera.WorldToScreenPoint(
                cachedBottomGuiPanelRenderer.bounds.max)
                .y;
    }

    private Vector3 GetRealDeltaTouchPos(Vector2 deltaPos)
    {
        return cam.ScreenToViewportPoint(deltaPos);
    }

    private void OnTouchMovement()
    {
        if (CanMoveOnTouch && !ScoresPage.IsScrolling)
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

        rotationSpeedX = Mathf.Lerp(rotationSpeedX, defaultRotationSpeedX, Time.deltaTime);
        rotationSpeedY = Mathf.Lerp(rotationSpeedY, defaultRotationSpeedY, Time.deltaTime);

        RotateCamera();
    }

    private void FreezeMotion()
    {
        if (!DeltaCheck0(rotationSpeedX))
        {
            rotationSpeedX -= rotationSpeedX * 0.05f;
        }
        else
        {
            rotationSpeedX = 0;
        }

        if (!DeltaCheck0(rotationSpeedY))
        {
            rotationSpeedY -= rotationSpeedY * 0.05f;
        }
        else
        {
            rotationSpeedY = 0;
        }
    }

    private void RotateCamera()
    {
        camEulerAngles.y += rotationSpeedY * Time.deltaTime;

        if (GameData.IsGame(Game.SpaceJet))
        {
            camEulerAngles.x += rotationSpeedX * Time.deltaTime;
        }
        else
        {
            camEulerAngles.x = HelpTools.ClampAngle(camEulerAngles.x + rotationSpeedX * Time.deltaTime, minXAngle, maxXAndle);
        }

        transform.eulerAngles = camEulerAngles;
    }

    private void GetMouseTransitions()
    {
        deltaMousePositionX = Input.mousePosition.x - prevMousePosition.x;
        deltaMousePositionY = Input.mousePosition.y - prevMousePosition.y;
        prevMousePosition = Input.mousePosition;
    }

    private void SetRotationSpeedY(float deltaPositionX)
    {
        rotationSpeedY = deltaPositionX;

        if (rotationSpeedY < 0)
        {
            defaultRotationSpeedY = -Mathf.Abs(defaultRotationSpeedY);
        }
        else
        {
            defaultRotationSpeedY = Mathf.Abs(defaultRotationSpeedY);
        }
    }

    private void SetRotationSpeedX(float deltaPositionY)
    {
        rotationSpeedX = deltaPositionY * -1;
    }

    private void PinchZoom()
    {
        if (!CanMoveOnTouch)
            return;

        if (Input.touchCount == 2)
        {
            rotationSpeedX = Mathf.SmoothDamp(rotationSpeedX, 0f, ref rotationSpeedX, 0.5f);
            rotationSpeedY = Mathf.SmoothDamp(rotationSpeedY, 0f, ref rotationSpeedX, 0.5f);

            distanceBetweenFingers = Vector2.Distance(Input.GetTouch(0).position, Input.GetTouch(1).position);

            if (Input.GetTouch(0).phase == TouchPhase.Moved || Input.GetTouch(1).phase == TouchPhase.Moved)
            {
                if (distanceBetweenFingers > prevDistanceBetweenFingers)
                {
                    Zoom(-1 * Vector2.Distance(GetRealDeltaTouchPos(Input.GetTouch(0).position), GetRealDeltaTouchPos(Input.GetTouch(1).position)));

                }
                else
                {
                    Zoom(Vector2.Distance(GetRealDeltaTouchPos(Input.GetTouch(0).position), GetRealDeltaTouchPos(Input.GetTouch(1).position)));
                }
            }

            prevDistanceBetweenFingers = distanceBetweenFingers;
        }

#if UNITY_WEBPLAYER || UNITY_STANDALONE || UNITY_EDITOR || UNITY_WEBGL || UNITY_WSA
        if (touchableArea.Contains(Input.mousePosition, true) && !Mathf.Approximately(Input.GetAxisRaw("Mouse ScrollWheel"), 0))
        {
            
            Zoom(Input.GetAxisRaw("Mouse ScrollWheel") * -zoomSpeedPC);
        }
#endif
    }

    private void Zoom(float zoomSpeed)
    {
        if (maxCameraZoomZ <= minCameraZoomZ)
            return;

        StopCamApproachCoroutines();

        camZCoord =
              Mathf.MoveTowards(
                  current: cam.transform.localPosition.z,
                  target: zoomSpeed > 0 ? minCameraZoomZ : maxCameraZoomZ,
                  maxDelta: Math.Abs(zoomSpeed) * Time.deltaTime);

        cam.transform.localPosition = 
            new Vector3(cam.transform.localPosition.x, cam.transform.localPosition.y, camZCoord);
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
