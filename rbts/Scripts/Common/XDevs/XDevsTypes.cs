using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace XDevs
{
    public enum EntityTypes
    {
        Money,
        Vip,
        Tank,
        Consumable,
        Camouflage,
        Decal,

        ConsumableKit,
    }

    public enum BgType
    {
        None,
        Silver,
        Gold,
        Green,
        Red,
    }

    public class DiscountInfo
    {
        public double endTime = 0;
        public int val = 0;//%

        public long DiscountTimeRemain { get { return (long)(endTime - GameData.CorrectedCurrentTimeStamp); } }
        public bool IsActive { get { return DiscountTimeRemain > 0 && val > 0; } }
        private bool lastCheckDiscountState = false;

        public DiscountInfo(JsonPrefs prefs)
        {
            endTime = prefs.ValueDouble("endTime", -1);
            val = prefs.ValueInt("discount", -1);

            lastCheckDiscountState = IsActive;
        }

        public ProfileInfo.Price GetDiscountPrice(ProfileInfo.Price price)
        {
            ProfileInfo.Price discountPrice = new ProfileInfo.Price(price);
            discountPrice.value = (int)(price.value * (1f - (val / 100f)) );
            return discountPrice;
        }

        public bool IsStateChanged
        {
            get
            {
                if (lastCheckDiscountState != IsActive)
                {
                    lastCheckDiscountState = IsActive;
                    return true;
                }
                return false;
            }
        }
    }
}
