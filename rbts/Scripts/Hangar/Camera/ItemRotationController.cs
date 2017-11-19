using UnityEngine;

public class ItemRotationController : MonoBehaviour
{
    public GameObject topPanel;
    public GameObject bottomPanel;
    public Transform target;
    public float rotationSpeedMultiplier;
    public float defaultRotationSpeed;

    private const int AFTER_TOUCH_COUNT_MAX = 40;
    private const int TOUCH_DELTA_MAGNITUDE_MIN = 2;

    private Rect touchableArea;
    private int afterTouchCounter;
    private bool touchableAreaIsInited;
    private float rotationSpeed;
    private float deltaMousePositionX;
    private Vector3 previousMousePosition;

    public static ItemRotationController Instance { get; private set; }

    public static bool EnableMovement { get; set; }

    void Awake()
    {
        Instance = this;

        Messenger.Subscribe(EventId.PageChanged, SetTouchableArea);
        Messenger.Subscribe(EventId.ScoresBoxActivated, SetTouchableArea);
        Messenger.Subscribe(EventId.WindowModeChanged, SetTouchableArea);
    }

    void OnDestroy()
    {
        Messenger.Unsubscribe(EventId.PageChanged, SetTouchableArea);
        Messenger.Unsubscribe(EventId.ScoresBoxActivated, SetTouchableArea);
        Messenger.Unsubscribe(EventId.WindowModeChanged, SetTouchableArea);

        Instance = null;
    }

    void Update() { Movement(); }

    public void SetTarget(Transform transform)
    {
        target = transform;
    }

    public void SetTouchableArea(EventId id, EventInfo info)
    {
        if (!ScoresController.Instance)
            return;

        float scoresBoxMaxX
            = HangarController.Instance.GuiCamera
                .WorldToScreenPoint(
                    ScoresController.Instance
                        .mainBackground
                        .GetComponent<Renderer>().bounds.max)
                .x;

        if (!touchableAreaIsInited)
        {
            touchableArea.xMin = scoresBoxMaxX;
            touchableArea.xMax = HangarController.Instance.GuiCamera.WorldToScreenPoint(topPanel.GetComponent<Renderer>().bounds.max).x;
            touchableArea.yMin = HangarController.Instance.GuiCamera.WorldToScreenPoint(topPanel.GetComponent<Renderer>().bounds.min).y;
            touchableArea.yMax = HangarController.Instance.GuiCamera.WorldToScreenPoint(bottomPanel.GetComponent<Renderer>().bounds.max).y;

            touchableAreaIsInited = true;
        }
        else
        {
            touchableArea.xMin = ScoresController.Instance.mainLayout.isActiveAndEnabled ? scoresBoxMaxX : 0;
        }
    }

    private void Movement()
    {
        if(target == null)
            return;

        if (EnableMovement && !ScoresPage.IsScrolling)
        {
            if (Input.touchCount == 1 && touchableArea.Contains(Input.GetTouch(0).position, true))
            {
                switch (Input.GetTouch(0).phase)
                {
                    case TouchPhase.Moved:
                        SetRotationSpeed(Input.GetTouch(0).deltaPosition.x);
                        break;
                    case TouchPhase.Stationary:
                        Freeze();
                        break;
                }
            }

            #if UNITY_WEBPLAYER || UNITY_STANDALONE || UNITY_EDITOR || UNITY_WEBGL || UNITY_WSA

            else if (touchableArea.Contains(Input.mousePosition, true))
            {
                CheckMousePosition();

                if (Input.GetMouseButton(0))
                    SetRotationSpeed(deltaMousePositionX);
            }

            #endif
        }

        if (afterTouchCounter++ >= AFTER_TOUCH_COUNT_MAX)
        {
            rotationSpeed
                = Mathf.Lerp(
                    rotationSpeed,
                    defaultRotationSpeed,
                    Time.deltaTime);
        }

        target.Rotate(
            xAngle: 0,
            yAngle: rotationSpeed * Time.deltaTime,
            zAngle: 0,
            relativeTo: Space.Self);
    }

    private void Freeze()
    {
        if (Vector3.Magnitude(Input.GetTouch(0).deltaPosition) > TOUCH_DELTA_MAGNITUDE_MIN)
            return;

        afterTouchCounter = 0;
        rotationSpeed = 0;
    }

    private void CheckMousePosition()
    {
        deltaMousePositionX = Input.mousePosition.x - previousMousePosition.x;
        previousMousePosition = Input.mousePosition;
    }

    private void SetRotationSpeed(float deltaMousePositionX)
    {
        rotationSpeed = deltaMousePositionX * rotationSpeedMultiplier * -1;
        defaultRotationSpeed *= (rotationSpeed < 0 ? -1 : 1);
    }
}
