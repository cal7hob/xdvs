using UnityEngine;

public class ThrottleLevel : MonoBehaviour
{
    public GameObject thumb;
    public GameObject filler;

    [Range(1.0f, 3.0f)]
    public float throttleMultiplier = 2;

    protected const float THUMB_SLIDING_SPEED = 200.0f;

    protected static float targetFillerDimensions;
    protected static Vector3 thumbPosition;
    protected static Vector2 fillerSize;
    protected static Collider fillerCollider;

    private FlightController flightController;
    private bool isBarPressed;
    private bool isThumbPressed;
    private bool isThumbPressedHigher;
    private bool isThumbPressedLower;

    public static float Value
    {
        get; protected set;
    }

    protected virtual float MinFillerSize
    {
        get { return 0; }
    }

    protected virtual float MaxFillerSize
    {
        get { return 0; }
    }

    protected float MinThumbPosition
    {
        get { return 0; }
    }

    protected float MaxThumbPosition
    {
        get { return 0; }
    }

    protected float AccelerationProgress
    {
        get { return flightController.CurrentSpeed / flightController.TargetSpeed; }
    }

    public bool IsOn { get; set; }

    /* UNITY SECTION */

    void Awake()
    {
        IsOn = true;

        Dispatcher.Subscribe(EventId.MainTankAppeared, Init);
        Dispatcher.Subscribe(EventId.MyTankRespawned, SetDefaultThumbPosition);
    }

    void OnDestroy()
    {
        Dispatcher.Unsubscribe(EventId.MainTankAppeared, Init);
        Dispatcher.Unsubscribe(EventId.MyTankRespawned, SetDefaultThumbPosition);
    }

    void Start()
    {
        SetDefaultThumbPosition();

        SetupFiller();
        UpdateValue();
    }

    void Update()
    {
        if (flightController == null || !IsOn)
            return;

        if (isThumbPressed)
            CalcThumbPressed();
        else if (isThumbPressedHigher)
            CalcThumbPressedHigher();
        else if (isThumbPressedLower)
            CalcThumbPressedLower();

        if (isBarPressed)
        {

           /* thumbPosition.y
                = Mathf.Clamp(
                    value:  sprBackground.transform.InverseTransformPoint(BattleGUI.Instance.GuiCamera.ScreenToWorldPoint(fillerUIItem.Touch.position)).y,
                    min:    MinThumbPosition,
                    max:    MaxThumbPosition);*/
        }


        UpdateValue();
        UpdateFiller();

        if (!isBarPressed && !isThumbPressed)
            CalcThumbAndBarReleased();
    }

    /* BUTTON SECTION */

    public void OnBtnHigherPressed()
    {
        isThumbPressedHigher = true;
    }

    public void OnBtnLowerPressed()
    {
        isThumbPressedLower = true;
    }

    public void OnBtnRelease()
    {
        isThumbPressedLower = false;
        isThumbPressedHigher = false;
    }

    public void OnLevelBarPressed()
    {
        isBarPressed = true;
    }

    public void OnLevelBarReleased()
    {
        isBarPressed = false;
    }

    public void OnLevelPressed()
    {
        isThumbPressed = true;
    }

    public void OnLevelReleased()
    {
        isThumbPressed = false;
    }

    /* HANDLER SECTION */

    protected virtual void SetDefaultThumbPosition(EventId id = 0, EventInfo info = null)
    {

    }

    protected virtual void SetupFiller()
    {
        
    }

    protected virtual void UpdateFiller()
    {
        targetFillerDimensions = fillerCollider.bounds.size.y * Value; // Размеры, к которым должен стремиться filler.
        targetFillerDimensions = (HelpTools.Approximately(targetFillerDimensions, MinFillerSize) ? 5 : targetFillerDimensions); // Костылик, чтобы filler резко не падал в 0, когда резко сбавляем тягу на 0.

        fillerSize.y = Mathf.Clamp(targetFillerDimensions * AccelerationProgress, MinFillerSize, MaxFillerSize);
    }

    protected virtual void UpdateValue()
    {
    }

    protected virtual void CalcThumbPressed()
    {
    }

    protected virtual void CalcThumbPressedHigher()
    {
        thumbPosition.y
                = Mathf.MoveTowards(
                    current:    thumbPosition.y,
                    target:     MaxThumbPosition,
                    maxDelta:   Time.fixedDeltaTime * THUMB_SLIDING_SPEED);
    }

    protected virtual void CalcThumbPressedLower()
    {
        thumbPosition.y
                = Mathf.MoveTowards(
                    current:    thumbPosition.y,
                    target:     MinThumbPosition,
                    maxDelta:   Time.fixedDeltaTime * THUMB_SLIDING_SPEED);
    }

    protected virtual void CalcThumbAndBarReleased()
    {
        thumbPosition += throttleMultiplier * Vector3.up * flightController.ThrottleLevelInputAxis;

        Vector3 newThumbPosition = thumbPosition;

        newThumbPosition.y = Mathf.Clamp(newThumbPosition.y, MinThumbPosition, MaxThumbPosition);

        thumbPosition = newThumbPosition;
    }

    private void Init(EventId id, EventInfo info)
    {
        flightController = (FlightController)XD.StaticContainer.BattleController.CurrentUnit;
    }
}
