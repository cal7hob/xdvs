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
    public class AwardForPlace
    {
        public GameObject awardContainer;
        public LabelLocalizationAgent awardPlace;
        public tk2dTextMesh awardAmount;
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
