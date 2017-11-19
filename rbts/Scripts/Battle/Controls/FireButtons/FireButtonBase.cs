using UnityEngine;
using Rewired;
using System;

public abstract class FireButtonBase : MonoBehaviour
{
    [Header("Общие настройки")]

    [Header("Ссылки")]
    public tk2dUIItem fireButton;
    public tk2dSprite sprFireButton;
    public tk2dUILayout layout;

    [Header("Остальное")]
    public GunShellInfo.ShellType shellType = GunShellInfo.ShellType.Usual;
    public float areaScalingQualifier = 0.07f;
    public bool continueWhenLeavingCollider;

    protected bool IsReloading { get; private set; }
    public bool IsFiring { get; private set; }

    protected CustomController touchController;

    /* UNITY SECTION */

    protected virtual void Awake()
    {
        touchController = XDevs.Input.TouchController;

        fireButton.OnDown += StartFiring;
        fireButton.OnDown += FireButton_Down;

        fireButton.OnUp += StopFiringOnUp;
        fireButton.OnUp += FireButton_Up;
        
        fireButton.OnRelease += StopFiringOnRelease;

        Messenger.Subscribe(EventId.MainVehWeaponReloaded, OnReloaded);
    }

    protected virtual void OnDestroy()
    {
        fireButton.OnDown -= StartFiring;
        fireButton.OnDown -= FireButton_Down;

        fireButton.OnUp -= StopFiringOnUp;
        fireButton.OnUp -= FireButton_Up;

        fireButton.OnRelease -= StopFiringOnRelease;

        Messenger.Unsubscribe(EventId.MainVehWeaponReloaded, OnReloaded);
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
        touchController.SetButtonValue((int)shellType, IsFiring);
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

    private void StartFiring()
    {
        IsFiring = true;
    }

    private void StopFiringOnUp()
    {
        if (!continueWhenLeavingCollider)
        {
            IsFiring = false;
        }
    }

    private void StopFiringOnRelease()
    {
        if (continueWhenLeavingCollider)
        {
            IsFiring = false;
        }
    }

    protected abstract void FireButton_Down();

    protected abstract void FireButton_Up();

    protected virtual void OnReloaded(EventId id, EventInfo ei)
    {
        IsReloading = false;
    }

    public virtual Rect RectForJoystick()
    {
        return layout.GetRect();
    }
}
 
