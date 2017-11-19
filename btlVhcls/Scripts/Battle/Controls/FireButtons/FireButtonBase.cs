using UnityEngine;
using Rewired;
using System;

public abstract class FireButtonBase : MonoBehaviour
{
    [Header("Общие настройки")]

    [Header("Ссылки")]
    public tk2dUIItem fireButton;
    public tk2dSprite sprFireButton;

    [Header("Остальное")]
    public GunShellInfo.ShellType shellType = GunShellInfo.ShellType.Usual;
    public float areaScalingQualifier = 0.07f;

    protected bool IsReloading { get; private set; }

    protected CustomController touchController;

    /* UNITY SECTION */

    protected virtual void Awake()
    {
        touchController = XDevs.Input.TouchController;

        fireButton.OnUp += FireButton_Up;
        fireButton.OnDown += FireButton_Down;

        Dispatcher.Subscribe(EventId.WeaponReloaded, OnReloaded);
    }

    protected virtual void OnDestroy()
    {
        Dispatcher.Unsubscribe(EventId.WeaponReloaded, OnReloaded);
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
        touchController.SetButtonValue((int)shellType, fireButton.IsPressed);
    }

    public virtual void SimulateReloading()
    {
        IsReloading = true;
    }

    public void SimulateClick()
    {
        FireButton_Down();
    }

    /* PRIVATE SECTION */

    protected abstract void FireButton_Down();

    protected abstract void FireButton_Up();

    protected virtual void OnReloaded(EventId id, EventInfo ei)
    {
        var eii = (EventInfo_I)ei;
        if ((GunShellInfo.ShellType)(eii.int1) == shellType)
        {
            IsReloading = false;
        }
    }
}
 