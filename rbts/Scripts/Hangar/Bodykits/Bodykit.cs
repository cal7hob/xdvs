using System;
using CodeStage.AntiCheat.ObscuredTypes;
using System.Collections.Generic;

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
    public ObscuredFloat magazineGain;
    public ObscuredFloat armorGain;
    public ObscuredFloat rofGain;
    public ObscuredFloat ircmRofGain;
    public List<ProfileInfo.Price> pricesToGroups;

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
        magazineGain = sourceBodykit.magazineGain;
        armorGain = sourceBodykit.armorGain;
        rofGain = sourceBodykit.rofGain;
        ircmRofGain = sourceBodykit.ircmRofGain;
        pricesToGroups = sourceBodykit.pricesToGroups;
    }

    bool IShopItem.LockCondition { get { return ProfileInfo.Level < availabilityLevel; } }

    bool IShopItem.VipCondition { get { return isVip; } }

    bool IShopItem.HideCondition { get { return isHidden; } }

    bool IShopItem.ComingSoonCondition { get { return isComingSoon; } }

    int IShopItem.Id { get { return id; } }

    int IShopItem.AvailabilityLevel { get { return availabilityLevel; } }

    string IShopItem.Description { get { return string.Empty; } }

    public ProfileInfo.Price Price { get { return pricesToGroups[Shop.CurrentVehicle.Info.vehicleGroup - 1]; } }

    public string IdString { get { return id.ToString("D2"); } }

    float IStatGainer.Damage { get { return damageGain; } }

    float IStatGainer.RocketDamage { get { return rocketDamageGain; } }

    float IStatGainer.Armor { get { return armorGain; } }

    float IStatGainer.Speed { get { return speedGain; } }

    float IStatGainer.RoF { get { return rofGain; } }

    float IStatGainer.IRCMROF { get { return ircmRofGain; } }

    public string GetBonusesText()
    {
        string status = "";
        if (damageGain > 0)
        {
            status
                += string.Format(
                    "{0}{1}% {2}",
                    Environment.NewLine,
                    Convert.ToInt32(damageGain * 100).ToString("+0;-#"),
                    Localizer.GetText("ForDamageGain"));

        }

        if (rocketDamageGain > 0)
        {
            status
                += string.Format(
                    "{0}{1}% {2}",
                    Environment.NewLine,
                    Convert.ToInt32(rocketDamageGain * 100).ToString("+0;-#"),
                    Localizer.GetText("ForDamageGain"));

        }

        if (armorGain > 0)
        {
            status
                += string.Format(
                    "{0}{1}% {2}",
                    Environment.NewLine,
                    Convert.ToInt32(armorGain * 100).ToString("+0;-#"),
                    Localizer.GetText("ForArmorGain"));

        }

        if (speedGain > 0)
        {
            status
                += string.Format(
                    "{0}{1}% {2}",
                    Environment.NewLine,
                    Convert.ToInt32(speedGain * 100).ToString("+0;-#"),
                    Localizer.GetText("ForSpeedGain"));

        }

        if (rofGain > 0)
        {
            status
                += string.Format(
                    "{0}{1}% {2}",
                    Environment.NewLine,
                    Convert.ToInt32(rofGain * 100).ToString("+0;-#"),
                    Localizer.GetText("ForRoFGain"));

        }

        return status.Trim();
    }
}