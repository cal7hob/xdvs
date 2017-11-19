using UnityEngine;
using Rewired;

public class Module_TurretRotationIndicator : InterfaceModuleBase
{
    enum AlphaState
    {
        Idle,
        Wait,
        Rise,
        Fall,
    }

    [SerializeField]
    private GameObject turretTank;
    [SerializeField]
    private GameObject turretRobot;
    [SerializeField]
    private tk2dBaseSprite sprTurretBaseTank;
    [SerializeField]
    private tk2dBaseSprite sprTurretBaseRobot;

    [SerializeField]
    private tk2dUIItem uiItemTank;
    [SerializeField]
    private tk2dUIItem uiItemRobot;

    [SerializeField]
    private InterfaceExtensions.ConditionHelper conditionHelper;

    [Header("Параметры анимации альфой")]
    [SerializeField]
    private InterfaceElementBase[] objectsToChangeAlpha;
    [SerializeField]
    private float speedOfRisingAlfa = 3;
    [SerializeField]
    private float speedOfFallingAlfa = 1;
    [SerializeField]
    private float waitingTime = 2;
    [SerializeField]
    private float idleAlpha = 0;
    /// <summary>
    /// Допустимое отклонение
    /// Если угол поворота башни по отношению к базе меньше этого значения, башня считается отцентрованной и индикатор не появится
    /// </summary>
    [SerializeField]
    private float permissibleDeviation = 4;

    private bool needToUpdateAlpha = false;
    private float curAlpha = 0;
    private float counter = 0;
    private AlphaState state = AlphaState.Idle;
    private float angle = 0;
    private bool isAngleMoreThenPermissibleDeviation;

    private tk2dBaseSprite sprTurretBase;
    private tk2dUIItem uiItem;
    
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
        if (ProfileInfo.IsBattleTutorial)
        {
            gameObject.SetActive(false);
            return;
        }

        base.Awake();

        Messenger.Subscribe(EventId.MainTankAppeared, OnMainTankAppeared);
        Messenger.Subscribe(EventId.StatTableVisibilityChange, OnStatTableChangeVisibility);

        if (conditionHelper)
            conditionHelper.State = System.Convert.ToInt32(BattleGUI.IsTargetPlatformForShowingJoysticks);

        touchController = XDevs.Input.TouchController;

        State = AlphaState.Idle;//всем присваиваем альфу 0
        HelpTools.SetAlphaToAllInterfaceElementsInCollection(objectsToChangeAlpha, idleAlpha);
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
        Messenger.Unsubscribe(EventId.MainTankAppeared, OnMainTankAppeared);
        Messenger.Unsubscribe(EventId.StatTableVisibilityChange, OnStatTableChangeVisibility);
    }

    private void OnMainTankAppeared(EventId id, EventInfo info)
    {
        if (BattleController.MyVehicle == null)
            return;

        sprTurretBaseRobot.gameObject.SetActive(BattleController.MyVehicle.VehicleType == VehicleInfo.VehicleType.Robot);
        sprTurretBaseTank.gameObject.SetActive(BattleController.MyVehicle.VehicleType == VehicleInfo.VehicleType.Tank);
        turretTank.SetActive(BattleController.MyVehicle.VehicleType == VehicleInfo.VehicleType.Tank);
        turretRobot.SetActive(BattleController.MyVehicle.VehicleType == VehicleInfo.VehicleType.Robot);

        switch (BattleController.MyVehicle.VehicleType)
        {
            case VehicleInfo.VehicleType.Tank:
                sprTurretBase = sprTurretBaseTank;
                uiItem = uiItemTank;
                break;
            case VehicleInfo.VehicleType.Robot:
                sprTurretBase = sprTurretBaseRobot;
                uiItem = uiItemRobot;
                break;
        }
    }

    private void OnStatTableChangeVisibility(EventId id, EventInfo info)
    {
        SetActive(!((EventInfo_B)info).bool1);
    }

    private void Update()
    {
        if (BattleController.MyVehicle == null)
            return;

        sprTurretBase.transform.localRotation = Quaternion.Euler(0, 0, BattleController.MyVehicle.Turret.localRotation.eulerAngles.y);

        angle = Vector3.Angle(BattleController.MyVehicle.Turret.forward, BattleController.MyVehicle.Body.forward);//angle = Quaternion.Angle(BattleController.MyVehicle.Turret.localRotation, BattleController.MyVehicle.Body.localRotation);
        isAngleMoreThenPermissibleDeviation = angle > permissibleDeviation;
        //if (isAngleMoreThenPermissibleDeviation)
        //    Debug.LogError("angle = " + angle);

        //Условия смены состояний
        switch (state)
        {
            case AlphaState.Idle:
                if (isAngleMoreThenPermissibleDeviation)
                {
                    State = AlphaState.Rise;
                    SetActive(true);
                }
                break;
            case AlphaState.Rise:
                if (curAlpha >= 1)
                {
                    curAlpha = 1;
                    State = AlphaState.Wait;
                }
                break;
            case AlphaState.Wait:
                if (counter >= waitingTime)
                    State = AlphaState.Fall;
                break;
            case AlphaState.Fall:
                if (isAngleMoreThenPermissibleDeviation)
                    State = AlphaState.Rise;
                else if (curAlpha <= 0)
                {
                    curAlpha = 0;
                    State = AlphaState.Idle;
                    SetActive(false);
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
            case AlphaState.Rise:
                curAlpha += Time.deltaTime * speedOfRisingAlfa;
                needToUpdateAlpha = true;
                break;
            case AlphaState.Fall:
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
        int buttonIsActive = PlayerPrefs.GetInt("TurretButtonActive", Settings.DEFAULT_TURRET_ROTATION_INDICATOR_ACTIVITY);

        if (buttonIsActive != 0)
        {
            if (en)
                ReInput.InputSourceUpdateEvent += ReInput_InputSourceUpdateEvent;
            else
                ReInput.InputSourceUpdateEvent -= ReInput_InputSourceUpdateEvent;
        }

        JoystickManager.UpdateDeadZones();
    }

    private void ReInput_InputSourceUpdateEvent()
    {
        if (State == AlphaState.Idle) // Не показываем нажатия если не видно индикатора
            touchController.SetButtonValue("Center Turret", false);

        touchController.SetButtonValue("Center Turret", uiItem.IsPressed);
    }

    #endregion
}
