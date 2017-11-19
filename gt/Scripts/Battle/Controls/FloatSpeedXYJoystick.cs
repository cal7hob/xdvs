using System.Collections.Generic;
using Rewired;
using UnityEngine;


/// <summary>
/// Азбука быдлокода
/// </summary>
public class FloatSpeedXYJoystick : MonoBehaviour
{
    public enum TypeJoystick
    {
        //Индексы не менять, поменяются настройки на клиентах
        floatingX = 0,
        speed = 1,
        floating = 2,
        floatingY = 3
    };

    public TypeJoystick UsedTurretJoystickType { get { return BattleSettings.Instance == null ? Settings.DEFAULT_TURRET_CONTROL_TYPE : BattleSettings.Instance.TurretControlType; } }
    [SerializeField]
    private JoystickManager _joystickManager;
    private CustomController touchController;
    public GameObject statTable;
    public GameObject btlSettings;
    public GameObject commandChat;
    #region DoubleAndOneTap
    [SerializeField]
    private float timeForDoubleTapInSec = 0.5f;
    [SerializeField]
    private float timeBetweenStartAndEnd = 0.4f;
    private List<float> twoLastTapsTime;
    #endregion

    #region Работа с Rewired Custom Controller
    [Header("Оси для Rewired Custom Controller")]
    public string horizontalAxisKey = "Screen Axis X";
    public string verticalAxisKey = "Screen Axis Y";
    [Header("Тапы для Rewired Custom Controller")]
    public string doubleTouchTap = "Double Tap";
    public string singleTouchTap = "Single Tap";
    // Чтобы работала автонаводка Лены и не сбивалась при прикосновении к экрану
    private const float Dead_Zone_X = 0.07f;
    #endregion

    #region "Property"
    public float AxisX
    {
        get { return (UsedTurretJoystickType != TypeJoystick.floatingY) ? axisXY.x : 0; }
        private set
        {
            axisXY.x = Mathf.Clamp(value, -1, 1);
        }
    }
    public float AxisY
    {
        get { return (UsedTurretJoystickType != TypeJoystick.floatingY) ? axisXY.y : 0; }

        private set
        {
            axisXY.y = Mathf.Clamp(value, -1, 1);
        }
    }
    public bool doubleTap { get; private set; }
    public bool oneTap { get; private set; }
    #endregion

    #region "InputRegion"
    private Vector2 axisXY;
    private Rect bounds;
    [SerializeField]
    private GameObject upLeft;
    [SerializeField]
    private GameObject downRight;
    public List<Rect> joyAreaException = new List<Rect>();
    private Vector3 lastCoordAnchorsLeftUp;
    private Vector3 lastCoordAnchorsRightDown;
    #endregion

    #region AllJoysticks
    [SerializeField]
    private float workZoneInPixX = 120;
    private float workZoneInPixXMod;
    private float workZoneInPixYMod;
    #endregion

    #region "FloatJoystick"
    private Vector2 startPos;
    private Vector2 currentPos;
    #endregion

    #region "SpeedJoystick"
    private Vector2 lastPos;
    private IList<float> smoothX;
    private IList<float> smoothXTime;
    #endregion

    #region "Sprite&Co"
    [SerializeField]
    private tk2dSprite backCircleAllAxis;
    [SerializeField]
    private tk2dSprite frontButtonAllAxis;
    [SerializeField]
    private tk2dSprite backCircleOneAxis;
    [SerializeField]
    private tk2dSprite frontButtonOneAxis;
    [SerializeField]
    private float transparencySpeed = 5f;
    [SerializeField]
    private GameObject allAxis;
    [SerializeField]
    private GameObject oneAxis;
    private tk2dSprite backStationaryCircle;
    private tk2dSprite frontMovingButton;
    private Vector2 backOneSize;
    private Vector2 backAllSize;
    private Vector3 backAllScale;
    private Vector3 backOneScale;
    private Vector3 UpLeftTransformPositionToCamera;
    private Vector3 DownRightTransformPositionToCamera;
    private Vector3 curPosCam;
    private Vector3 startPosCam;
    private int multipledForColor;
    private float alphalvl;
    private Color _alpha;
    #endregion

    #region "Flags"
    private int fId = -1;
    private bool imUseTouch;
    private bool imUseMouse;
    private bool joystickIsAlive;
    private bool IsOn;
    private float startTapTime;

    #endregion

    public void AddAreaExcepts(Rect ex)
    {   // Если спрайт перевернут, то его rect width или height могут быть меньше нуля. Такой Rect не работает как мертвая зона. Приводим к нормальному виду. 
        // понимаю, выглядит так себе. Если кто придумает что получше - смело убивайте это. 
        if (ex.width < 0)
        {
            ex.width = -ex.width;
            ex.x = ex.x - ex.width;
        }
        if (ex.height < 0)
        {
            ex.height = -ex.height;
            ex.y = ex.y - ex.height;
        }
        joyAreaException.Add(ex);
    }

    public void ClearAreaExcepts()
    {
        joyAreaException.Clear();
    }

    void Start()
    {
        if (BattleCamera.Instance.IsMouseControlled)
        {
            Destroy(gameObject);
        }

        smoothX = new List<float>();
        smoothXTime = new List<float>();
        Input.simulateMouseWithTouches = false;
        oneAxis.SetActive(false);
        allAxis.SetActive(false);
        _alpha = new Color(1, 1, 1, alphalvl);
        backAllSize = backCircleAllAxis.GetBounds().extents;
        backOneSize = backCircleOneAxis.GetBounds().extents;
        backAllScale = new Vector3(backCircleAllAxis.scale.x, backCircleAllAxis.scale.y, 1);
        backOneScale = new Vector3(backCircleOneAxis.scale.x, backCircleOneAxis.scale.y, 1);
        bounds = new Rect(
            BattleGUI.Instance.GuiCamera.WorldToScreenPoint(upLeft.transform.position).x,
            BattleGUI.Instance.GuiCamera.WorldToScreenPoint(downRight.transform.position).y,
            BattleGUI.Instance.GuiCamera.WorldToScreenPoint(downRight.transform.position).x - BattleGUI.Instance.GuiCamera.WorldToScreenPoint(upLeft.transform.position).x,
            BattleGUI.Instance.GuiCamera.WorldToScreenPoint(upLeft.transform.position).y - BattleGUI.Instance.GuiCamera.WorldToScreenPoint(downRight.transform.position).y
        );
        lastCoordAnchorsLeftUp = upLeft.transform.position;
        lastCoordAnchorsRightDown = downRight.transform.position;
        twoLastTapsTime = new List<float>();
    }

    void Update()
    {
        CheckScreenSize();

        if (btlSettings.activeInHierarchy || statTable.activeInHierarchy) return;
        if (commandChat != null && commandChat.activeInHierarchy) return;
        if (multipledForColor == 1 || multipledForColor == -1)
        {
            ChangeColor(multipledForColor);
        }
        if ((fId == -1) && (imUseTouch == false))
        {
            MouseMethod();
        }
        if (imUseMouse == false)
        {
            TouchMethod();
        }
    }

    private void MouseMethod()
    {
        if (Input.GetMouseButtonDown(0) && bounds.Contains(Input.mousePosition) && !ExceptContains(Input.mousePosition))
        {
            if (CheckIfClickedOnEnemy(Input.mousePosition))
            {
                return;
            }

            imUseMouse = true;
            if (!joystickIsAlive)
            {
                Began(Input.mousePosition);
            }
        }
        if (Input.GetMouseButton(0) && imUseMouse)
        {
            MovAndStat(Input.mousePosition);
        }
        if (Input.GetMouseButtonUp(0) && imUseMouse)
        {
            Ended();
        }
    }

    private void TouchMethod()
    {
        foreach (var th in Input.touches)
        {
            if (th.phase == TouchPhase.Began && bounds.Contains(th.position) && !ExceptContains(th.position))
            {
                if (CheckIfClickedOnEnemy(th.position))
                {
                    return;
                }

                imUseTouch = true;
                if (!joystickIsAlive)
                {
                    oneOrTwoTouch(true, false);
                    fId = th.fingerId;
                    Began(th.position);
                }
            }
            if (th.fingerId == fId && ((th.phase == TouchPhase.Moved) || (th.phase == TouchPhase.Stationary)))
            {
                MovAndStat(th.position);
            }
            if (th.fingerId == fId && (th.phase == TouchPhase.Ended || th.phase == TouchPhase.Canceled))
            {
                Ended();
            }
        }
    }

    private static bool CheckIfClickedOnEnemy(Vector2 pos)
    {
        if (SystemInfo.deviceType != DeviceType.Handheld || BattleCamera.Instance.IsZoomed)
        {
            return false;
        }

#if !UNITY_WEBGL || UNITY_EDITOR //todo: когда-нибудь вынести это из этого класса
        var ray = Camera.main.ScreenPointToRay(pos);
        RaycastHit ht;

        if (Physics.Raycast(ray, out ht, 300))
        {
            var targetVehicle = ht.transform.GetComponentInParent<VehicleController>();

            if (!ReferenceEquals(targetVehicle, null) && !targetVehicle.IsMainsFriend && !ReferenceEquals(targetVehicle, BattleController.MyVehicle))
            {
                BattleController.MyVehicle.turretController.SetAutoAimingTarget(targetVehicle);
                return true;
            }
        }
#endif

        return false;
    }

    private void Began(Vector2 pos)
    {
        startTapTime = Time.time;
        joystickIsAlive = true;
        startPos = BattleGUI.Instance.GuiCamera.ScreenToWorldPoint(pos);
        alphalvl = 0f;
        workZoneInPixXMod = (workZoneInPixX * 4 * (Mathf.Clamp((BattleSettings.Instance.TurretRotationSensitivity), 0.25f, 1)));
        workZoneInPixYMod = (workZoneInPixXMod * 0.7f);
        if ((UsedTurretJoystickType == TypeJoystick.floatingX) || (UsedTurretJoystickType == TypeJoystick.floatingY))
        {
            allAxis.SetActive(false);
            oneAxis.SetActive(true);
            MoveToStartPositionControllers(backCircleOneAxis, frontButtonOneAxis, backOneScale, backOneSize.x, backOneSize.y);
        }
        else
        {
            oneAxis.SetActive(false);
        }
    }

    private void MovAndStat(Vector2 cpos)
    {
        currentPos = BattleGUI.Instance.GuiCamera.ScreenToWorldPoint(cpos);
        switch (UsedTurretJoystickType)
        {
            case TypeJoystick.floating:
                AxisX = ((currentPos.x - startPos.x) / workZoneInPixXMod);
                AxisY = ((currentPos.y - startPos.y) / workZoneInPixYMod);
                GoMove(currentPos.x, currentPos.y);
                break;

            case TypeJoystick.floatingX:
                AxisX = ((currentPos.x - startPos.x) / workZoneInPixXMod);
                GoMove(currentPos.x, startPos.y);
                break;

            case TypeJoystick.floatingY:
                AxisY = ((currentPos.y - startPos.y) / workZoneInPixYMod);
                GoMove(startPos.x, currentPos.y);
                break;

            case TypeJoystick.speed:
                AxisX = SmoothingXMedian(((currentPos.x - lastPos.x) / Time.deltaTime) / workZoneInPixXMod);
                // AxisY = ((currentPos.y - lastPos.y) / Time.deltaTime) / speedPixPerSecY;
                lastPos = currentPos;
                break;
        }
    }

    private void Ended()
    {
        oneOrTwoTouch(false, true);
        alphalvl = 1;
        multipledForColor = -1;
        ResetVar();
    }

    // Этот метод пригоняет в точку прикоснования спрайт подложки и самой кнопки, дает доступ к ним из скрипта и + масштабирует подложку. 
    private void MoveToStartPositionControllers(tk2dSprite back, tk2dSprite front, Vector3 backScale, float sizeX, float sizeY)
    {
        backStationaryCircle = back;
        frontMovingButton = front;
        backStationaryCircle.scale = new Vector3(((workZoneInPixXMod / backScale.x) / sizeX), ((workZoneInPixYMod / backScale.y) / sizeY), 1f);
        backStationaryCircle.transform.position = new Vector3((startPos.x), (startPos.y), backStationaryCircle.transform.position.z);
        frontMovingButton.transform.position = backStationaryCircle.transform.position;
        multipledForColor = 1;
    }

    // В этот метод передаем информацию для изменения местоположения спрайтов. Он проверяет, не дальше ли это допустимого и двигает туда.
    private void GoMove(float _x, float _y)
    {
        float startMinusWZX = startPos.x - workZoneInPixXMod;
        float startPlusWZX = startPos.x + workZoneInPixXMod;
        float startMinusWZY = startPos.y - workZoneInPixYMod;
        float startPlusWZY = startPos.y + workZoneInPixYMod;

        if (frontMovingButton != null)
        {
            frontMovingButton.transform.position = new Vector3(Mathf.Clamp(_x, startMinusWZX, startPlusWZX), Mathf.Clamp(_y, startMinusWZY, startPlusWZY), frontMovingButton.transform.position.z);
        }
    }

    private void ResetVar()
    {
        joystickIsAlive = false;
        AxisX = 0;
        AxisY = 0;
        fId = -1;
        imUseMouse = false;
        imUseTouch = false;
        currentPos = Vector2.zero;
        startPos = Vector2.zero;
    }

    private void ChangeColor(float val)
    {
        alphalvl += Time.deltaTime * transparencySpeed * val;
        alphalvl = Mathf.Clamp(alphalvl, 0, 1);
        _alpha.a = alphalvl;
        frontButtonAllAxis.color = _alpha;
        frontButtonOneAxis.color = _alpha;
        backCircleOneAxis.color = _alpha;
        backCircleAllAxis.color = _alpha;
        if (alphalvl >= 1 || alphalvl <= 0)
        {
            multipledForColor = 0;
        }
    }

    private void OnDisable()
    {
        ResetVar();
    }

    private void BoundsOverride()
    {
        bounds.x = (BattleGUI.Instance.GuiCamera.WorldToScreenPoint(upLeft.transform.position)).x;
        bounds.y = (BattleGUI.Instance.GuiCamera.WorldToScreenPoint(downRight.transform.position)).y;
        bounds.width = (BattleGUI.Instance.GuiCamera.WorldToScreenPoint(downRight.transform.position)).x - bounds.x;
        bounds.height = (BattleGUI.Instance.GuiCamera.WorldToScreenPoint(upLeft.transform.position)).y - bounds.y;
    }

    private void CheckScreenSize()
    {
        if ((lastCoordAnchorsLeftUp != upLeft.transform.position) || (lastCoordAnchorsRightDown != downRight.transform.position))
        {
            lastCoordAnchorsLeftUp = upLeft.transform.position;
            lastCoordAnchorsRightDown = downRight.transform.position;
            BoundsOverride();
            _joystickManager.ReplaceButtons();
        }
    }

    private bool ExceptContains(Vector3 point)
    {
        foreach (var item in joyAreaException)
        {
            if (item.Contains(point)) return true;
        }
        return false;
    }

    private void ResetDoubleTap(EventId _id, EventInfo _info)
    {
        doubleTap = false;
    }

    void Awake()
    {
        Dispatcher.Subscribe(EventId.WeaponReloaded, ResetDoubleTap);
        touchController = XDevs.Input.TouchController;
        ReInput.InputSourceUpdateEvent += ReInput_InputSourceUpdateEvent;
        IsOn = true;
    }

    void OnEnable()
    {
        alphalvl = 0;
        allAxis.SetActive(false);
        oneAxis.SetActive(false);
    }

    void OnDestroy()
    {
        ReInput.InputSourceUpdateEvent -= ReInput_InputSourceUpdateEvent;
        Dispatcher.Unsubscribe(EventId.WeaponReloaded, ResetDoubleTap);
    }

    private void ReInput_InputSourceUpdateEvent()
    {
        if (!IsOn)
        {
            return;
        }

        touchController.SetAxisValue(horizontalAxisKey, Mathf.Abs(AxisX) > Dead_Zone_X ? AxisX : 0);
        touchController.SetAxisValue(verticalAxisKey, AxisY);
        touchController.SetButtonValue(singleTouchTap, oneTap);
        touchController.SetButtonValue(doubleTouchTap, doubleTap);
    }

    private void oneOrTwoTouch(bool start, bool end)
    {
        if (start)
        {
            startTapTime = Time.time;
        }
        if (end)
        {
            start = false;
            end = false;  // Если дабл тап не глючит,можно удалять. 
            if ((startTapTime + timeBetweenStartAndEnd) >= Time.time)
            {
                twoLastTapsTime.Add(Time.time);

                if (twoLastTapsTime.Count > 1)
                {
                    if (twoLastTapsTime[0] + timeForDoubleTapInSec >= twoLastTapsTime[1])
                    {
                        oneTap = false;
                        doubleTap = true;
                    }
                    else
                    {
                        doubleTap = false;
                        oneTap = true;
                    }
                    twoLastTapsTime.RemoveAt(0);
                }
            }
        }
    }

    private float SmoothingXMedian(float rawX)
    {
        smoothX.Add(rawX);
        smoothXTime.Add(rawX);
        if (smoothX.Count > 4)
        {
            for (int i = 0; i < smoothX.Count; i++)
            {
                for (int j = 0; j < smoothX.Count - i - 1; j++)
                {
                    if (smoothX[j] > smoothX[j + 1])
                    {
                        float temp = smoothX[j];
                        smoothX[j] = smoothX[j + 1];
                        smoothX[j + 1] = temp;
                        smoothX.Remove(smoothXTime[0]);
                        smoothXTime.RemoveAt(0);
                    }
                }
            }
        }
        return smoothX[(int)smoothX.Count / 2];
    }
}





