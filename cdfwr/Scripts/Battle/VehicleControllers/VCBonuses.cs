using System.Collections;
using System.Collections.Generic;
using UnityEngine;
//[RequireComponent(typeof(VehicleController))]

public interface IBonuseUseAbility
{
    void TakeExperienceBonus(int amount);
    void TakeGoldBonus(int amount);
    void TakeSilverBonus(int amount);
    void TakeHealthBonus();
    void TakeFuelBonus();
    void TakeBonus(BonusItem.BonusType bonusType, int amount);
}

public class CantUseBonus:IBonuseUseAbility
{
    public void TakeExperienceBonus(int amount){}
    public void TakeGoldBonus(int amount){}
    public void TakeSilverBonus(int amount){}
    public void TakeHealthBonus(){}
    public void TakeFuelBonus(){}
    public void TakeBonus(BonusItem.BonusType bonusType, int amount) { }
}

public class FullBonusUse:IBonuseUseAbility
{
    private VehicleController vehicle;
    private bool isMain;
    public FullBonusUse(VehicleController vehicle) 
    {
        this.vehicle = vehicle;
        this.isMain = vehicle.IsMain;
    }

    public virtual void TakeExperienceBonus(int amount)
    {
        if (vehicle.IsMine)
        {
            ScoreCounter.ScoreInto(vehicle, amount);
            if (vehicle.IsMain && !vehicle.IsBot)
            {
                Dispatcher.Send(EventId.ExperienceAcquired, new EventInfo_I(amount));
            }
        }
    }

    public virtual void TakeGoldBonus(int amount)
    {
        if (isMain)
        {
            Dispatcher.Send(EventId.GoldAcquired, new EventInfo_I(amount));
        }
    }

    public virtual void TakeSilverBonus(int amount)
    {
        if (isMain)
        {
            Dispatcher.Send(EventId.SilverAcquired, new EventInfo_I(amount));
        }
    }

    public virtual void TakeHealthBonus()
    {
        if (!vehicle.IsMine)
        {
            return;
        }
        vehicle.Armor = vehicle.MaxArmor;
        vehicle.SetCustomProperties(XDevs.LiteralKeys.StatisticKey.Health, vehicle.Armor);
    }

    public void TakeFuelBonus()
    {
        Dispatcher.Send(EventId.FuelAcquired, new EventInfo_I(1));
    }

    public void TakeBonus(BonusItem.BonusType bonusType, int amount)
    {
        if (!vehicle.IsMine)
        {
            return;
        }

        switch (bonusType)
        {
            case BonusItem.BonusType.Experience:
                TakeExperienceBonus(amount);
                break;
            case BonusItem.BonusType.Gold:
                TakeGoldBonus(amount);
                break;
            case BonusItem.BonusType.Silver:
                TakeSilverBonus(amount);
                break;
            case BonusItem.BonusType.Health:
                TakeHealthBonus();
                break;
            case BonusItem.BonusType.Fuel:
                TakeFuelBonus();
                break;
           /* case BonusItem.BonusType.GoldRush:
                amount = GoldRush.TotalStake;
                TakeGoldRushBonus();
                break;*/
        }

        if (vehicle.IsBot)
        {
            return;
        }

        #region Google Analytics: picking up booster

        GoogleAnalyticsWrapper.LogEvent(
            new CustomEventHitBuilder()
                .SetParameter(GAEvent.Category.PickedUpBonus)
                .SetParameter<GAEvent.Action>()
                .SetSubject(GAEvent.Subject.MapName, GameManager.CurrentMap)
                .SetParameter<GAEvent.Label>()
                .SetSubject(GAEvent.Subject.BonusType, bonusType)
                .SetValue(ProfileInfo.Level));

        GoogleAnalyticsWrapper.LogEvent(
            new CustomEventHitBuilder()
                .SetParameter(GAEvent.Category.PickedUpBonus)
                .SetParameter<GAEvent.Action>()
                .SetSubject(GAEvent.Subject.MapName, GameManager.CurrentMap)
                .SetParameter<GAEvent.Label>()
                .SetSubject(GAEvent.Subject.VehicleID, ProfileInfo.currentVehicle)
                .SetValue(ProfileInfo.Level));

        #endregion

        Notifier.ShowBonus(bonusType, amount);
    }
    
}


