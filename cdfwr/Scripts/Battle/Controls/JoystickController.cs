using UnityEngine;
using Rewired;

public class JoystickController : AbstractClassForButtons
{
    [Header("Ссылки")]
    public tk2dSprite boundingSprite;
    public tk2dSprite sprJoystickLevel;

    [Header("Оси для Rewired Custom Controller")]
    public string horizontalAxisKey;
    public string verticalAxisKey;

    [Header("Остальное")]
    public float trimmedBorder;
    [Range(0f, 1f)]
    public float deadZoneHorizontal = 0.07f;
    [Range(0f, 1f)]
    public float deadZoneVertical = 0.07f;
    [Range(0f, 1f)]
    public float sensitivityHorizontal = 0.65f;
    [Range(0f, 1f)]
    public float sensitivityVertical = 0.65f;

    [Tooltip("For Increasing Area in that cannon cant rotate by swapping by screen. In percents of Joy Texture")] // WAT?
    public float joystickAreaIncreaseKoef = 0.15f;

    [SerializeField]
    private CruiseControl cruiseControl;
    private int m_lastCamHeight;
    public Vector2 relativeDelta;
    private Vector2 joystickExtents;
    private tk2dUIItem joystickUIItem;
    private CustomController touchController;
    public bool JoystickPressed
    {
        get; private set;
    }

    public bool Visible
    {
        get
        {
            return boundingSprite.gameObject.activeSelf && sprJoystickLevel.gameObject.activeSelf;
        }
        set
        {
            boundingSprite.gameObject.SetActive(value);
            sprJoystickLevel.gameObject.SetActive(value);
        }
    }

    public Rect Area
    {
        get; private set;
    }

    public bool IsOn { get; set; }

    public static bool CanAct
    {
        get
        {
            return !CursorManager.IsGUIOnScreen || BattleChatCommands.OnScreen;
        }
    }

    void Start()
    {
        deadZoneHorizontal = PlayerPrefs.HasKey("MoveJDeadZoneX") ? PlayerPrefs.GetFloat("MoveJDeadZoneX") : 0.07f;
        deadZoneVertical = PlayerPrefs.HasKey("MoveJDeadZoneY") ? PlayerPrefs.GetFloat("MoveJDeadZoneY") : 0.07f;
        sensitivityHorizontal = PlayerPrefs.HasKey("MoveJXSensibility") ? PlayerPrefs.GetFloat("MoveJXSensibility") : 0.65f;
        sensitivityVertical = PlayerPrefs.HasKey("MoveJYSensibility") ? PlayerPrefs.GetFloat("MoveJYSensibility") : 0.65f;
    }
    void Awake()
    {
        Dispatcher.Subscribe(EventId.MainTankAppeared, SetControllerAreas);
        touchController = XDevs.Input.TouchController;
        ReInput.InputSourceUpdateEvent += ReInput_InputSourceUpdateEvent;

        joystickUIItem = boundingSprite.GetComponent<tk2dUIItem>();

        if (!joystickUIItem || !boundingSprite)
        {
            Debug.LogError("No UIItem or no sprite specified in joystick. Disabled.", gameObject);
            gameObject.SetActive(false);
            return;
        }

        joystickExtents = boundingSprite.GetBounds().extents - new Vector3(trimmedBorder, trimmedBorder, 0);

        joystickUIItem.OnDown += OnJoystickDown;
        joystickUIItem.OnRelease += OnJoystickRelease;

        IsOn = true;
    }

    void OnDestroy()
    {
        Dispatcher.Unsubscribe(EventId.MainTankAppeared, SetControllerAreas);
        ReInput.InputSourceUpdateEvent -= ReInput_InputSourceUpdateEvent;
    }

    private void ReInput_InputSourceUpdateEvent()
    {
        if (!IsOn) return;
        touchController.SetAxisValue(horizontalAxisKey, GetXAxis());
        touchController.SetAxisValue(verticalAxisKey, GetYAxis());
    }

    void Update()
    {
        CalcJoystickPosition();
    }

    void LateUpdate()
    {
        tk2dCamera cam = BattleGUI.Instance.Tk2dGuiCamera;

        if ((int)cam.ScreenExtents.yMin != m_lastCamHeight)
        {
            m_lastCamHeight = (int)cam.ScreenExtents.yMin;
            SetControllerAreas(EventId.MainTankAppeared, null);
        }
    }

    /// <summary>
    /// Для отображения области джойстика движения. Не удалять.
    /// </summary>
    //void OnGUI()
    //{
    //    GUI.Button(
    //        position: new Rect
    //        {
    //            xMin = Area.xMin,
    //            xMax = Area.xMax,
    //            yMin = Screen.height - Area.yMin,
    //            yMax = Screen.height - Area.yMax
    //        },
    //        text: "Joy");
    //}

    public float GetXAxis()
    {
        return relativeDelta.x;
    }

    public float GetYAxis()
    {
        return relativeDelta.y;
    }

    private void SetControllerAreas(EventId id, EventInfo info)
    {
        var joyWorldTopRight = boundingSprite.transform.TransformPoint(boundingSprite.GetBounds().max);
        var joyScreenTopRight = BattleGUI.Instance.GuiCamera.WorldToScreenPoint(joyWorldTopRight);
        var joyWorldBottomLeft = boundingSprite.transform.TransformPoint(boundingSprite.GetBounds().min);
        var joyScreenBottomLeft = BattleGUI.Instance.GuiCamera.WorldToScreenPoint(joyWorldBottomLeft);
        
        float joyAdd = joystickAreaIncreaseKoef * boundingSprite.GetBounds().size.y;

        Area = new Rect
        {
            xMin = joyScreenBottomLeft.x - joyAdd,
            yMin = joyScreenBottomLeft.y - joyAdd,
            xMax = joyScreenTopRight.x + joyAdd,
            yMax = joyScreenTopRight.y + joyAdd
        };
    }

    private void CalcJoystickPosition()
    {
        if (JoystickPressed)
        {
            Vector3 touchPosition = BattleGUI.Instance.GuiCamera.ScreenToWorldPoint(joystickUIItem.Touch.position);

            Vector2 delta = touchPosition - boundingSprite.transform.position;

            relativeDelta = Vector2.ClampMagnitude(new Vector2(delta.x / joystickExtents.x, delta.y / joystickExtents.y), 1);

            relativeDelta.x =
                HelpTools.ApplySensitivity(Mathf.Abs(relativeDelta.x) > deadZoneHorizontal ? relativeDelta.x : 0,
                    sensitivityHorizontal);
            relativeDelta.y =
                HelpTools.ApplySensitivity(Mathf.Abs(relativeDelta.y) > deadZoneVertical ? relativeDelta.y : 0,
                    sensitivityVertical);
            sprJoystickLevel.transform.localPosition
                = new Vector3(
                    x: relativeDelta.x * joystickExtents.x,
                    y: relativeDelta.y * joystickExtents.y,
                    z: 1);
        }
        else
        {
            if (relativeDelta != Vector2.zero)
            {
                relativeDelta = Vector2.zero;
            }
        }
       
        //else
        //{
        //    relativeDelta = new Vector2(Input.GetAxis(horizontalAxisKey), Input.GetAxis(verticalAxisKey));
        //}
    }

    private void OnJoystickDown()
    {
        if (!CanAct)
        {
            return;
        }

        JoystickPressed = true;
        Dispatcher.Send(EventId.BattleBtnPressed, new EventInfo_SimpleEvent()); 
    }

    private void OnJoystickRelease()
    {
        JoystickPressed = false;

        relativeDelta = Vector2.zero;

        sprJoystickLevel.transform.localPosition = new Vector3(0, 0, 1);
    }
    public override Rect Coord()
    {
        var joyWorldTopRight = boundingSprite.transform.TransformPoint(boundingSprite.GetBounds().max);
        var joyScreenTopRight = BattleGUI.Instance.GuiCamera.WorldToScreenPoint(joyWorldTopRight);
        var joyWorldBottomLeft = boundingSprite.transform.TransformPoint(boundingSprite.GetBounds().min);
        var joyScreenBottomLeft = BattleGUI.Instance.GuiCamera.WorldToScreenPoint(joyWorldBottomLeft);

        var Area = new Rect
        {
            xMin = joyScreenBottomLeft.x,
            yMin = joyScreenBottomLeft.y,
            xMax = joyScreenTopRight.x,
            yMax = joyScreenTopRight.y,
        };
        return Area;
    }
}
