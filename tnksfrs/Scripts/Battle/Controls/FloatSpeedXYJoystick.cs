using System.Collections.Generic;
using Rewired;
using UnityEngine;

public class FloatSpeedXYJoystick : MonoBehaviour
{
    private enum TypeJoystick { speed, floating, floatingX, floatingY };
    [SerializeField]
    private TypeJoystick joystickUseType;
    [SerializeField]
    private JoystickManager _joystickManager;
    private CustomController touchController;
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
    #endregion

    #region "Property"
    public float AxisX
    {
        get { return (joystickUseType != TypeJoystick.floatingY) ? axisXY.x : 0; }
        private set
        {
            axisXY.x = Mathf.Clamp(value, -1, 1);
        }
    }
    public float AxisY
    {
        get { return (joystickUseType != TypeJoystick.floatingY) ? axisXY.y : 0; }

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
    private List<Rect> joyAreaException = new List<Rect>();
    private int lastScreenHeight;
    private int lastScreenWidth;
    private Vector3 lastCoordAnchorsLeftUp;
    private Vector3 lastCoordAnchorsRightDown;
    #endregion

    #region "FloatJoystick"
    private Vector2 startPos;
    private Vector2 currentPos;
    [SerializeField]
    private int workZoneInPixX = 120;
    [SerializeField]
    private int workZoneInPixY = 120;
    #endregion

    #region "SpeedJoystick"
    private Vector2 lastPos;
    [SerializeField]
    private float speedPixPerSecX = 300;
    [SerializeField]
    private float speedPixPerSecY = 200;
    #endregion

    #region "Sprite&Co"
    /*[SerializeField]
    private tk2dSprite backCircleAllAxis;
    [SerializeField]
    private tk2dSprite frontButtonAllAxis;
    [SerializeField]
    private tk2dSprite backCircleOneAxis;
    [SerializeField]
    private tk2dSprite frontButtonOneAxis;*/
    [SerializeField]
    private float transparencySpeed = 5f;
    [SerializeField]
    private GameObject allAxis;
    [SerializeField]
    //private GameObject oneAxis;
    /*private tk2dSprite backStationaryCircle;
    private tk2dSprite frontMovingButton;*/
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
    private float fId = -1;
    private bool imUseTouch;
    private bool imUseMouse;
    private bool joystickIsAlive;
    private bool IsOn;
    private float startTapTime;
    private float h2;
    private float g2;
    private float g1;
    #endregion

    public void AddAreaExcepts(Rect ex)
    {
        joyAreaException.Add(ex);
    }

    public void ClearAreaExcepts()
    {
        joyAreaException.Clear();
    }

    void Start()
    {
        Input.simulateMouseWithTouches = false;
        //oneAxis.SetActive(false);
        allAxis.SetActive(false);
        _alpha = new Color(1, 1, 1, alphalvl);
        /*backAllSize = backCircleAllAxis.GetBounds().extents;
        backOneSize = backCircleOneAxis.GetBounds().extents;
        backAllScale = new Vector3(backCircleAllAxis.scale.x, backCircleAllAxis.scale.y, 1);
        backOneScale = new Vector3(backCircleOneAxis.scale.x, backCircleOneAxis.scale.y, 1);*/
        lastScreenHeight = Screen.height;
        lastScreenWidth = Screen.width;
       /* bounds = new Rect(
            BattleGUI.Instance.GuiCamera.WorldToScreenPoint(upLeft.transform.position).x,
            BattleGUI.Instance.GuiCamera.WorldToScreenPoint(downRight.transform.position).y,
            BattleGUI.Instance.GuiCamera.WorldToScreenPoint(downRight.transform.position).x - BattleGUI.Instance.GuiCamera.WorldToScreenPoint(upLeft.transform.position).x,
            BattleGUI.Instance.GuiCamera.WorldToScreenPoint(upLeft.transform.position).y - BattleGUI.Instance.GuiCamera.WorldToScreenPoint(downRight.transform.position).y
        );*/
        lastCoordAnchorsLeftUp = upLeft.transform.position;
        lastCoordAnchorsRightDown = downRight.transform.position;
        twoLastTapsTime = new List<float>();
    }

    void Update()
    {
        CheckScreenSize();
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

    private void Began(Vector2 pos)
    {
        startTapTime = Time.time;
        joystickIsAlive = true;
     //   startPos = BattleGUI.Instance.GuiCamera.ScreenToWorldPoint(pos);
        alphalvl = 0f;
        if ((joystickUseType == TypeJoystick.floatingX) || (joystickUseType == TypeJoystick.floatingY))
        {
            allAxis.SetActive(false);
            //oneAxis.SetActive(true);
            //MoveToStartPositionControllers(backCircleOneAxis, frontButtonOneAxis, backOneScale, backOneSize.x, backOneSize.y);
        }
        else
        {
            //oneAxis.SetActive(false);
            allAxis.SetActive(true);
            //MoveToStartPositionControllers(backCircleAllAxis, frontButtonAllAxis, backAllScale, backAllSize.x, backAllSize.y);
        }
    }

    private void MovAndStat(Vector2 cpos)
    {
       /*currentPos = BattleGUI.Instance.GuiCamera.ScreenToWorldPoint(cpos);
        switch (joystickUseType)
        {
            case TypeJoystick.floating:
                AxisX = ((currentPos.x - startPos.x) / workZoneInPixX);
                AxisY = ((currentPos.y - startPos.y) / workZoneInPixY);
                GoMove(currentPos.x, currentPos.y);
                break;

            case TypeJoystick.floatingX:
                AxisX = ((currentPos.x - startPos.x) / workZoneInPixX);
                GoMove(currentPos.x, startPos.y);
                break;

            case TypeJoystick.floatingY:
                AxisY = ((currentPos.y - startPos.y) / workZoneInPixY);
                GoMove(startPos.x, currentPos.y);
                break;

            case TypeJoystick.speed:
                AxisX = ((currentPos.x - lastPos.x) / Time.deltaTime) / speedPixPerSecX;
                AxisY = ((currentPos.y - lastPos.y) / Time.deltaTime) / speedPixPerSecY;
                lastPos = currentPos;
                break;
        }*/
    }

    private void Ended()
    {
        oneOrTwoTouch(false, true);
        alphalvl = 1;
        multipledForColor = -1;
        ResetVar();
    }

    // Этот метод пригоняет в точку прикоснования спрайт подложки и самой кнопки, дает доступ к ним из скрипта и + масштабирует подложку. 
    /*private void MoveToStartPositionControllers(tk2dSprite back, tk2dSprite front, Vector3 backScale, float sizeX, float sizeY)
    {
        /*backStationaryCircle = back;
        frontMovingButton = front;
        backStationaryCircle.scale = new Vector3(((workZoneInPixX / backScale.x) / sizeX), ((workZoneInPixY / backScale.y) / sizeY), 1f);
        backStationaryCircle.transform.position = new Vector3((startPos.x), (startPos.y), backStationaryCircle.transform.position.z);*/
        /*multipledForColor = 1;
    }*/

    // В этот метод передаем информацию для изменения местоположения спрайтов. Он проверяет, не дальше ли это допустимого и двигает туда.
    private void GoMove(float _x, float _y)
    {
        float startMinusWZX = startPos.x - workZoneInPixX;
        float startPlusWZX = startPos.x + workZoneInPixX;
        float startMinusWZY = startPos.y - workZoneInPixY;
        float startPlusWZY = startPos.y + workZoneInPixY;
        //frontMovingButton.transform.position = new Vector3(Mathf.Clamp(_x, startMinusWZX, startPlusWZX), Mathf.Clamp(_y, startMinusWZY, startPlusWZY), frontMovingButton.transform.position.z);
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
        /*frontButtonAllAxis.color = _alpha;
        frontButtonOneAxis.color = _alpha;
        backCircleOneAxis.color = _alpha;
        backCircleAllAxis.color = _alpha;*/
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
     /*   bounds.x = (BattleGUI.Instance.GuiCamera.WorldToScreenPoint(upLeft.transform.position)).x;
        bounds.y = (BattleGUI.Instance.GuiCamera.WorldToScreenPoint(downRight.transform.position)).y;
        bounds.width = (BattleGUI.Instance.GuiCamera.WorldToScreenPoint(downRight.transform.position)).x - bounds.x;
        bounds.height = (BattleGUI.Instance.GuiCamera.WorldToScreenPoint(upLeft.transform.position)).y - bounds.y;*/
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

    void Awake()
    {
        touchController = XDevs.Input.TouchController;
        ReInput.InputSourceUpdateEvent += ReInput_InputSourceUpdateEvent;
        IsOn = true;
    }

    void OnDestroy()
    {
        ReInput.InputSourceUpdateEvent -= ReInput_InputSourceUpdateEvent;
    }

    private void ReInput_InputSourceUpdateEvent()
    {
        if (!IsOn) return;
        touchController.SetAxisValue(horizontalAxisKey, AxisX);
        touchController.SetAxisValue(verticalAxisKey, AxisY);
        touchController.SetButtonValue(singleTouchTap, oneTap);
        touchController.SetButtonValue(doubleTouchTap, doubleTap);
    }

    private void oneOrTwoTouch(bool start, bool end)
    {
        oneTap = false;
        doubleTap = false;
        if (start)
        {
            startTapTime = Time.time;
        }
        if (end)
        {
            start = false;
            end = false;
            if ((startTapTime + timeBetweenStartAndEnd) >= Time.time)
            {
                twoLastTapsTime.Add(Time.time);

                if (twoLastTapsTime.Count > 1)
                {
                    if (twoLastTapsTime[0] + timeForDoubleTapInSec >= twoLastTapsTime[1])
                    {
                        doubleTap = true;
                    }
                    else
                    {
                        oneTap = true;
                    }
                    twoLastTapsTime.RemoveAt(0);
                }
            }
        }
    }

}





