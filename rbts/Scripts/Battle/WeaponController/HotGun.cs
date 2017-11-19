using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HotGun : WeaponController
{
    private float lastShotTime;
    private float heating;

    public override float Progress
    {
        get
        {
            return heating;
        }
    }

    public HotGun(VehicleController owner) : base(owner)
    {}

    public override void InstantReload()
    {
        heating = 0f;
    }

    public override void UpdateWeapon()
    {
        float cooling = (owner.RoF * GameSettings.Instance.Heating - 1) / 60f;
        heating = Mathf.Clamp01(heating - cooling * Time.deltaTime);

        /*if (owner.IsMain)
            Messenger.Send(EventId.MainVehWeaponReloaded, new EventInfo_SimpleEvent());*/
    }

    public override void RegisterShot()
    {
        if (1 - heating < GameSettings.Instance.Heating)
            return;

        lastShotTime = Time.time;
        heating = Mathf.Clamp01(heating + GameSettings.Instance.Heating);
    }

    public override bool IsReady
    {
        get
        {
            return Time.time - lastShotTime >= 60 / (GameSettings.Instance.ColdFireRatio * owner.RoF) && (1 - heating >= GameSettings.Instance.Heating);
        }
    }

    public override int Counter
    {
        get { return 0; }
    }
}