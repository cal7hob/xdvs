using UnityEngine;
using Rewired;
using System;

public abstract class FireButtonBase : AbstractClassForButtons
{
    [Header("Общие настройки")]

    [Header("Ссылки")]
    public tk2dUIItem fireButton;
    public tk2dSprite sprFireButton;

    [Header("Остальное")]
    public ShellType shellType = ShellType.Usual;
    public float areaScalingQualifier = 0.07f;

    protected bool isPressed;

    public static Rect Area { get; private set; }

    protected bool IsReloading { get; private set; }

    protected CustomController touchController;

    /* UNITY SECTION */

    protected virtual void Awake()
    {
        touchController = XDevs.Input.TouchController;

        Dispatcher.Subscribe(EventId.WeaponReloaded, OnReloaded);
        Dispatcher.Subscribe(EventId.MainTankAppeared, OnMainVehicleAppeared);
    }

    protected virtual void OnDestroy()
    {
        Dispatcher.Unsubscribe(EventId.WeaponReloaded, OnReloaded);
        Dispatcher.Unsubscribe(EventId.MainTankAppeared, OnMainVehicleAppeared);
    }

    protected virtual void OnEnable ()
    {
        ReInput.InputSourceUpdateEvent += ReInput_InputSourceUpdateEvent;
    }
    protected virtual void OnDisable()
    {
        ReInput.InputSourceUpdateEvent -= ReInput_InputSourceUpdateEvent;
    }

    private void ReInput_InputSourceUpdateEvent()
    {
        touchController.SetButtonValue((int)shellType, isPressed);
    }

    ///// <summary>
    ///// Для отображения области кнопки. Не удалять.
    ///// </summary>
    //void OnGUI()
    //{
    //    GUI.Button(
    //        position: new Rect
    //                    {
    //                        xMin = Area.xMin,
    //                        xMax = Area.xMax,
    //                        yMin = Screen.height - Area.yMin,
    //                        yMax = Screen.height - Area.yMax
    //                    },
    //        text: "Fire");
    //}

    /* PUBLIC SECTION */

    public virtual void SimulateReloading()
    {
        IsReloading = true;
    }

    public void SimulateClick()
    {
        FireButton_Down();
    }

    /* PRIVATE SECTION */

    protected virtual bool FireButton_Down()
    {
        if (!JoystickController.CanAct)
        {
            return false;
        }

        isPressed = true;

        //Dispatcher.Send(EventId.BattleBtnPressed, new EventInfo_SimpleEvent());
        return true;
    }

    protected virtual void FireButton_Up()
    {
        isPressed = false;
    }

    protected virtual void OnReloaded(EventId id, EventInfo ei)
    {
        var eii = (EventInfo_I)ei;
        if ((ShellType)(eii.int1) == shellType)
        {
            IsReloading = false;
        }
    }

    protected virtual void OnMainVehicleAppeared(EventId id, EventInfo ei)
    {
        Bounds bounds = sprFireButton.GetBounds();

        float additionalLength = bounds.max.y * areaScalingQualifier;

        Vector3 worldTopLeftPosition
            = sprFireButton.transform.TransformPoint(
                new Vector3(
                    x:  bounds.min.x,
                    y:  bounds.max.y,
                    z:  sprFireButton.transform.localPosition.z));

        Vector3 worldBottomRightPosition
            = sprFireButton.transform.TransformPoint(
                new Vector3(
                    x:  bounds.max.x,
                    y:  bounds.min.y,
                    z:  sprFireButton.transform.localPosition.z));

        Vector3 sreenTopLeftPosition = BattleGUI.Instance.GuiCamera.WorldToScreenPoint(worldTopLeftPosition);
        Vector3 sreenBottomRightPosition = BattleGUI.Instance.GuiCamera.WorldToScreenPoint(worldBottomRightPosition);

        Area = new Rect
        {
            xMin = sreenTopLeftPosition.x - additionalLength,
            yMin = sreenBottomRightPosition.y - additionalLength,
            xMax = sreenBottomRightPosition.x + additionalLength,
            yMax = sreenTopLeftPosition.y + additionalLength
        };
    }
    public override Rect Coord()
    {
        return new Rect(0,0,0,0);
    }
}
 