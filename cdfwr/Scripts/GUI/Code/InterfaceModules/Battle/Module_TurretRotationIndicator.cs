using UnityEngine;
using Rewired;

public class Module_TurretRotationIndicator : InterfaceModuleBase
{
    enum AlphaState
    {
        Idle,
        Wait,
        FadeIn,
        FadeOut,
    }

    [SerializeField] private tk2dBaseSprite sprTurret;
    [SerializeField] private tk2dUIItem uiitem;
    [SerializeField] private InterfaceExtensions.ConditionHelper conditionHelper;

    [Header("Параметры анимации альфой")]
    [SerializeField] private InterfaceElementBase[] objectsToChangeAlpha;
    [SerializeField] private float speedOfRisingAlfa = 3;
    [SerializeField] private float speedOfFallingAlfa = 1;
    [SerializeField] private float waitingTime = 2;
    [SerializeField] private float idleAlpha = 0;
    /// <summary>
    /// Допустимое отклонение
    /// Если угол поворота башни по отношению к базе меньше этого значения, башня считается отцентрованной и индикатор не появится
    /// </summary>
    [SerializeField] private float permissibleDeviation = 4;

    private bool needToUpdateAlpha = false;
    private float curAlpha = 0;
    private float counter = 0;
    private AlphaState state = AlphaState.Idle;
    private float angle = 0;
    private bool isAngleMoreThenPermissibleDeviation;

    private AlphaState State
    {
        get { return state; }
        set
        {
            //Debug.LogErrorFormat("Change state from {0} to {1}", state, value);
            if (state != value)
                counter = 0;
            state = value;
        }
    }

    protected CustomController touchController;


    protected override void Awake()
    {
        if (!ProfileInfo.IsBattleTutorialCompleted)
        {
            SetActive(false);
            return;
        }

        Dispatcher.Subscribe(EventId.MainTankAppeared, OnMainTankAppeared);
        Dispatcher.Subscribe(EventId.OnStatTableChangeVisibility, OnStatTableChangeVisibility);
        wrapper.StateChanged += OnWrapperStateChanged;
        if (conditionHelper)
            conditionHelper.State = System.Convert.ToInt32(BattleGUI.IsTargetPlatformForShowingJoysticks);

        touchController = XDevs.Input.TouchController;

        if (setInitialStateOnAwake)
            SetActive(initialState);
        State = AlphaState.Idle;//всем присваиваем альфу 0
        HelpTools.SetAlphaToAllInterfaceElementsInCollection(objectsToChangeAlpha, idleAlpha);
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
        Dispatcher.Unsubscribe(EventId.MainTankAppeared, OnMainTankAppeared);
        Dispatcher.Unsubscribe(EventId.OnStatTableChangeVisibility, OnStatTableChangeVisibility);
    }

    private void OnMainTankAppeared(EventId id, EventInfo info)
    {
        SetActive(true);
    }

    private void OnStatTableChangeVisibility(EventId id, EventInfo info)
    {
        SetActive(!((EventInfo_B)info).bool1);
    }

    private void Update()
    {
        if (BattleController.MyVehicle == null)
            return;

        sprTurret.transform.localRotation = Quaternion.Euler(0, 0, BattleController.MyVehicle.Turret.localRotation.eulerAngles.y);
        angle = Vector3.Angle(BattleController.MyVehicle.Turret.forward, BattleController.MyVehicle.Body.forward);//angle = Quaternion.Angle(BattleController.MyVehicle.Turret.localRotation, BattleController.MyVehicle.Body.localRotation);
        isAngleMoreThenPermissibleDeviation = angle > permissibleDeviation;
        //if (isAngleMoreThenPermissibleDeviation)
        //    Debug.LogError("angle = " + angle);

        //Условия смены состояний
        switch (state)
        {
            case AlphaState.Idle:
                if (isAngleMoreThenPermissibleDeviation)
                    State = AlphaState.FadeIn;
                break;
            case AlphaState.FadeIn:
                if (curAlpha >= 1)
                {
                    curAlpha = 1;
                    State = AlphaState.Wait;
                }
                break;
            case AlphaState.Wait:
                if (counter >= waitingTime)
                    State = AlphaState.FadeOut;
                break;
            case AlphaState.FadeOut:
                if (isAngleMoreThenPermissibleDeviation)
                    State = AlphaState.FadeIn;
                else if (curAlpha <= 0)
                {
                    curAlpha = 0;
                    State = AlphaState.Idle;
                }
                break;
        }

        //Анимация состояния
        switch (state)
        {
            case AlphaState.Idle:
            case AlphaState.Wait:
                needToUpdateAlpha = false;
                if (isAngleMoreThenPermissibleDeviation)
                    counter = 0;
                else
                    counter += Time.deltaTime;
                break;
            case AlphaState.FadeIn:
                curAlpha += Time.deltaTime * speedOfRisingAlfa;
                needToUpdateAlpha = true;
                break;
            case AlphaState.FadeOut:
                curAlpha -= Time.deltaTime * speedOfFallingAlfa;
                needToUpdateAlpha = true;
                break;
        }

        if (needToUpdateAlpha)
            HelpTools.SetAlphaToAllInterfaceElementsInCollection(objectsToChangeAlpha, curAlpha);
    }

    #region Центровка по нажатию
    protected override void OnWrapperStateChanged(StateEventSender sender, bool en)
    {
        int buttonIsActive = PlayerPrefs.GetInt("TurretButtonActive", 1);
        if (buttonIsActive != 0)
        {
            if (en)
                ReInput.InputSourceUpdateEvent += ReInput_InputSourceUpdateEvent;
            else
                ReInput.InputSourceUpdateEvent -= ReInput_InputSourceUpdateEvent;
        }

    }

    private void ReInput_InputSourceUpdateEvent()
    {
        if (State == AlphaState.Idle)//Не показываем нажатия если не видно индикатора
            touchController.SetButtonValue("Center Turret", false);
        touchController.SetButtonValue("Center Turret", uiitem.IsPressed);
    }
    #endregion
    public override Rect Coord()
    {
        var joyWorldTopRight = sprTurret.transform.TransformPoint(sprTurret.GetBounds().max);
        var joyScreenTopRight = BattleGUI.Instance.GuiCamera.WorldToScreenPoint(joyWorldTopRight);
        var joyWorldBottomLeft = sprTurret.transform.TransformPoint(sprTurret.GetBounds().min);
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

