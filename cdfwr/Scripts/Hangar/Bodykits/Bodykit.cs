using System;
using CodeStage.AntiCheat.ObscuredTypes;
using System.Collections.Generic;
using UnityEngine;

public class Bodykit : IShopItem, IStatGainer
{
    public ObscuredInt id;
    public ObscuredBool isHidden;
    public ObscuredBool isVip;
    public ObscuredBool isComingSoon;
    public ObscuredInt availabilityLevel;
    public ObscuredDouble lifetime;
    public ObscuredFloat damageGain;
    public ObscuredFloat rocketDamageGain;
    public ObscuredFloat speedGain;
    public ObscuredFloat armorGain;
    public ObscuredFloat rofGain;
    public ObscuredFloat magazineGain;
    public ObscuredFloat reloadGain;
    public ObscuredFloat ircmRofGain;
    public List<ProfileInfo.Price> pricesToGroups;

    private readonly tk2dSprite iconSprite;

    public Bodykit(BodykitInEditor sourceBodykit)
    {
        id = sourceBodykit.id;
        isHidden = sourceBodykit.isHidden;
        isVip = sourceBodykit.isVip;
        availabilityLevel = sourceBodykit.availabilityLevel;
        lifetime = sourceBodykit.lifetime;
        damageGain = sourceBodykit.damageGain;
        rocketDamageGain = sourceBodykit.rocketDamageGain;
        speedGain = sourceBodykit.speedGain;
        armorGain = sourceBodykit.armorGain;
        rofGain = sourceBodykit.rofGain;
        magazineGain = sourceBodykit.magazineGain;
        reloadGain = sourceBodykit.reloadTimeGain;
        ircmRofGain = sourceBodykit.ircmRofGain;
        pricesToGroups = sourceBodykit.pricesToGroups;
        iconSprite = sourceBodykit.gameObject.GetComponent<tk2dSprite>();
    }

    bool IShopItem.LockCondition { get { return ProfileInfo.Level < availabilityLevel; } }

    bool IShopItem.VipCondition { get { return isVip; } }

    bool IShopItem.HideCondition { get { return isHidden; } }

    bool IShopItem.ComingSoonCondition { get { return isComingSoon; } }

    int IShopItem.Id { get { return id; } }

    int IShopItem.AvailabilityLevel { get { return availabilityLevel; } }

    string IShopItem.Description { get { return String.Empty; } }

    public ProfileInfo.Price Price { get { return pricesToGroups[Shop.CurrentVehicle.Info.vehicleGroup - 1]; } }

    public tk2dSprite IconSprite { get { return iconSprite; } }

    float IStatGainer.Damage { get { return damageGain; } }

    float IStatGainer.RocketDamage { get { return rocketDamageGain; } }

    float IStatGainer.Armor { get { return armorGain; } }

    float IStatGainer.Speed { get { return speedGain; } }

    float IStatGainer.ROF { get { return rofGain; } }

    float IStatGainer.Reload { get { return reloadGain; } }

    float IStatGainer.Magazine { get { return magazineGain; } }

    float IStatGainer.IRCMROF { get { return ircmRofGain; } }

    public string IdString { get { return id.ToString("D2"); } }

    public string GetBonusesText()
    {
        string status = "";
        if (damageGain > 0)
        {
            status
                += string.Format(
                    "{0}+{1} {2}",
                    status.Length > 0 ? Environment.NewLine : "",
                    Convert.ToInt32(damageGain),
                    Localizer.GetText("ForDamageGain"));

        }

        if (rocketDamageGain > 0)
        {
            status
                += string.Format(
                    "{0}+{1} {2}",
                    status.Length > 0 ? Environment.NewLine : "",
                    Convert.ToInt32(rocketDamageGain),
                    Localizer.GetText("ForDamageGain"));

        }

        if (armorGain > 0)
        {
            status
                += string.Format(
                    "{0}+{1} {2}",
                    status.Length > 0 ? Environment.NewLine : "",
                    Convert.ToInt32(armorGain),
                    Localizer.GetText("ForArmorGain"));

        }

        if (speedGain > 0)
        {
            status
                += string.Format(
                    "{0}+{1} {2}",
                    status.Length > 0 ? Environment.NewLine : "",
                    Convert.ToInt32(speedGain),
                    Localizer.GetText("ForSpeedGain"));

        }

        if (rofGain > 0)
        {
            status
                += string.Format(
                    "{0}+{1} {2}",
                    status.Length > 0 ? Environment.NewLine : "",
                    Convert.ToInt32(rofGain),
                    Localizer.GetText("ForROFGain"));

        }
        if (magazineGain > 0)
        {

            status
                += string.Format(
                    "{0}+{1} {2}",
                    status.Length > 0 ? Environment.NewLine : "",
                    /*Convert.ToInt32*/(magazineGain),
                    Localizer.GetText("forMagazineGain"));

        }
        if (reloadGain > 0 || reloadGain < 0)
        {
            status
                += string.Format(
                    "{0}+{1} {2}",
                    status.Length > 0 ? Environment.NewLine : "",
                    /*Convert.ToInt32*/(reloadGain),
                    Localizer.GetText("forReloadGain"));

        }

        return status;


    }
}