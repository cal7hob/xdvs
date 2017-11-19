using System;
using System.Collections.Generic;
using UnityEngine;

namespace Tanks.Models
{
    public enum WeeklyAwardsArea
    {
        World = 0,
        Country = 1,
        Region = 2,
        Clans = 3,
    }

    [Serializable]
    public class MoneyIcon
    {
        [SerializeField]
        private ProfileInfo.PriceCurrency currency;
        public ProfileInfo.PriceCurrency Currency { get { return currency; } }

        public void SetCurrency(ProfileInfo.PriceCurrency currency)
        {
            this.currency = currency;

            switch (currency)
            {
                case ProfileInfo.PriceCurrency.Gold:
                    break;

                case ProfileInfo.PriceCurrency.Silver:
                    break;
            }
        }
    }

    [Serializable]
    public class AwardForPlace
    {
        public GameObject awardContainer;
        public MoneyIcon moneyIcon;
    }

    [Serializable]
    public class WeeklyAward
    {
        public WeeklyAwardsArea Area;
        public string AreaName;
        public ProfileInfo.Price Award;
        public int Place;

        public WeeklyAward(WeeklyAwardsArea area, Dictionary<string, object> awardDict)
        {
            var award = new JsonPrefs(awardDict);

            Area = area;
            Place = award.ValueInt("place");
            AreaName = award.ValueString("name");
            Award = award.ValuePrice("price");

            if (Place <= 0)
                throw new ArgumentException("Cannot be zero or negative!", "place");

            if (Award == null)
                throw new ArgumentNullException("price", "Cannot be null!");
        }
    }
}
