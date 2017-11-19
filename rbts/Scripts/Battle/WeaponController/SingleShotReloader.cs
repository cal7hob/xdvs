using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SingleShotReloader : WeaponController
{
    //private static readonly float burstInterval;

    private float reloadingStartTime;
    private float timeInReload;
    private bool reloading;

    public override float Progress
    {
        get
        {
            return Mathf.Clamp01(timeInReload / (60 / owner.RoF));
        }
    }

    public SingleShotReloader(VehicleController owner) : base(owner)
    {
        reloadingStartTime = 0f;
    }

    public override void InstantReload()
    {
        reloading = false;
        timeInReload = 100f;

        if (owner.IsMain)
            Messenger.Send(EventId.MainVehWeaponReloaded, new EventInfo_SimpleEvent());
    }

    public override void UpdateWeapon()
    {
        if (!reloading)
            return;

        timeInReload = Time.time - reloadingStartTime;
        if (timeInReload < 60 / owner.RoF)
            return;

        reloading = false;
        if (owner.IsMain)
            Messenger.Send(EventId.MainVehWeaponReloaded, new EventInfo_SimpleEvent());
    }
    
    public override void RegisterShot()
    {
        if (reloading)
            return;

        reloading = true;
        reloadingStartTime = Time.time;
    }

    public override bool IsReady
    {
        get
        {
            return !owner.PhotonView.isMine || !reloading; // Клоны не перезаряжают сами
        }
    }

    public override int Counter
    {
        get { return 0; }
    }
}