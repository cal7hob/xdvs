/*using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CartridgeWeapon : WeaponController
{
    //private static readonly float burstInterval;

    private int shotCounter;
    private float reloadingStartTime;
    private float lastShotTime;
    private float timeInReload;
    private bool reloading;

    public override float Progress
    {
        get
        {
            return reloading ?
                timeInReload / owner.RoF
                : (float)shotCounter / owner.Magazine;
        }
    }

    static CartridgeWeapon()
    {
        //burstInterval = GameSettings.Instance.BurstInterval;
    }

    public CartridgeWeapon(VehicleController owner) : base(owner)
    {
        shotCounter = owner.Attack;
    }

    public override void InstantReload()
    {
        reloading = false;
        shotCounter = owner.Magazine;
    }

    public override void UpdateWeapon()
    {
        if (!reloading)
            return;

        timeInReload = Time.time - reloadingStartTime;
        shotCounter = (int)(timeInReload / owner.RoF * owner.Magazine + 0.01f);
        if (shotCounter != owner.Magazine)
            return;

        reloading = false;
        if (owner.IsMain)
            Messenger.Send(EventId.MainVehWeaponReloaded, new EventInfo_SimpleEvent());
    }
    
    public override void RegisterShot()
    {
        if (reloading)
            return;

        lastShotTime = Time.time;
        if (--shotCounter > 0 || !owner.PhotonView.isMine)
            return;

        reloading = true;
        reloadingStartTime = Time.time;
    }

    public override float NextShotDelay
    {
        get { return reloading ? owner.RoF : GameSettings.Instance.BurstInterval; }
    }

    public override bool IsReady
    {
        get
        {
            return Time.time - lastShotTime >= GameSettings.Instance.BurstInterval && (!owner.PhotonView.isMine || !reloading); // Клоны не перезаряжают сами
        }
    }

    public override int Counter
    {
        get { return shotCounter; }
    }
}*/