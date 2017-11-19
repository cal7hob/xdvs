using System;
using System.Collections.Generic;
using UnityEngine;
using System.Collections;


public class AfterBattleIncomeFrame : MonoBehaviour
{
    public enum RowType
    {
        Experience,
        Gold,
        Silver,
        Fuel,
        Quest,
        AllQuests
    }
    [Serializable]
    public class RowPrefab
    {
        public RowType type;
        public IncomeFrameRow prefab;
    }

    public class Data
    {
        public int val;
        public string spriteName;
        public string explanation;

        public Data(int _val, string _spriteName = null, string _explanation = null)
        {
            val = _val;
            spriteName = _spriteName;
            explanation = _explanation;
        }
    }

    public IncomeFrameRow incomeFrameRow;
    public bool useCustomRowPrefabs = false;
    public List<RowPrefab> customRowPrefabs;
    public GameObject lblEarnedInBattle;
    public GameObject vipBaskground;
    public GameObject dimmerBackground;
    public Transform columnWrapper;
    public float interval = 60;

    public bool NotEmpty { get { return data != null && data.Count > 0; } }

    private Vector3 nextRowLocalPos = Vector3.zero;
    private Dictionary<RowType, Data> data = new Dictionary<RowType, Data>();

    public void Init(bool isVipFrame)
    {
        // get multipiers for vip/common user
        var br = Http.Manager.BattleServer.result;
        int exp = isVipFrame ? br.experienceVIP : br.experienceBase;
        int earnedSilver = isVipFrame ? br.silverVIP : br.silverBase;

        //Проверяем на 0, чтобы по наполненности словаря data можно было судить о пустой/заполненной панели чтобы ее спрятать / показать
        if (exp > 0)
            data.Add(RowType.Experience, new Data(exp, useCustomRowPrefabs ? null : "FreeExp"));
        if (br.gold > 0)
            data.Add(RowType.Gold, new Data(br.gold, useCustomRowPrefabs ? null : "goldSmall"));
        if (earnedSilver > 0)
            data.Add(RowType.Silver, new Data(earnedSilver, useCustomRowPrefabs ? null : "silverSmall"));
        if (br.fuel > 0)
            data.Add(RowType.Fuel, new Data(br.fuel, useCustomRowPrefabs ? null : "fuel3d"));

        if (!br.isAllQuestsCompleted)
        {
            if ((br.quest != null) && br.quest.isComplete)
            {
                var currencySpriteName = string.Empty;

                switch (br.quest.GetQuestReward(isVipFrame).currency)
                {
                    case ProfileInfo.PriceCurrency.Gold:
                        currencySpriteName = "goldSmall";
                        break;
                    case ProfileInfo.PriceCurrency.Silver:
                        currencySpriteName = "silverSmall";
                        break;
                }

                data.Add(RowType.Quest, new Data(
                    br.quest.GetQuestReward(isVipFrame).value,
                    useCustomRowPrefabs ? null : currencySpriteName,
                    "lblFromQuest"));

                // Выполнены все квесты
                if (br.questAll.value > 0)
                {
                    switch (QuestsInfo.GetAllQuestsReward(isVipFrame).currency)
                    {
                        case ProfileInfo.PriceCurrency.Gold:
                            currencySpriteName = "goldSmall";
                            break;
                        case ProfileInfo.PriceCurrency.Silver:
                            currencySpriteName = "silverSmall";
                            break;
                    }

                    data.Add(RowType.AllQuests, new Data(
                        QuestsInfo.GetAllQuestsReward(isVipFrame).value,
                        useCustomRowPrefabs ? null : currencySpriteName,
                        "lblFrom3Quest"));
                }
            }
        }

        //Instantiate frames
        nextRowLocalPos = Vector3.zero;
        foreach (RowType rowType in Enum.GetValues(typeof(RowType)))
            if (data.ContainsKey(rowType))
            {
                if(data[rowType].val <= 0)
                    continue;
                var row = Instantiate(useCustomRowPrefabs ? GetCustomRowPrefabByType(rowType) : incomeFrameRow);
                row.transform.SetParent(columnWrapper);
                row.transform.localPosition = nextRowLocalPos;
                nextRowLocalPos -= new Vector3(0, interval, 0);
                row.Init(data[rowType]);
            }

        if (vipBaskground)
            vipBaskground.SetActive(isVipFrame);

        dimmerBackground.SetActive(isVipFrame != br.isBattleAsVip);

        lblEarnedInBattle.SetActive(NotEmpty);
        gameObject.SetActive(NotEmpty);

        #region Google Analytics: battle stats
        // Отправляем данные группы соответствующей текущему вип статусу игрока.
        if (isVipFrame == ProfileInfo.IsPlayerVip)
        {
            // Немного говнокода: 
            int silverAmount = !data.ContainsKey(RowType.Silver) ? 0 : data[RowType.Silver].val;
            int goldAmount = !data.ContainsKey(RowType.Gold) ? 0 : data[RowType.Gold].val;
            int experienceAmount = !data.ContainsKey(RowType.Experience) ? 0 : data[RowType.Experience].val;

            string silverAmountSubjectValue = GAEvent.Converter.ToQuantitySubject(silverAmount, ProfileInfo.PriceCurrency.Silver).ToFriendlyString();
            string goldAmountSubjectValue = GAEvent.Converter.ToQuantitySubject(goldAmount, ProfileInfo.PriceCurrency.Gold).ToFriendlyString();
            string experienceAmountSubjectValue = GAEvent.Converter.ToQuantitySubject(experienceAmount, ProfileInfo.PriceCurrency.Silver).ToFriendlyString();

            // Логгирование заработанного серебра:
            if (silverAmount > 0)
            {
                GoogleAnalyticsWrapper.LogEvent(
                new CustomEventHitBuilder()
                    .SetParameter(GAEvent.Category.SilverEarnedInBattle)
                    .SetParameter<GAEvent.Action>()
                    .SetSubject(GAEvent.Subject.MapName, Loading.PreviousScene)
                    .SetParameter<GAEvent.Label>()
                    .SetSubject(GAEvent.Subject.SilverAmount, silverAmountSubjectValue)
                    .SetValue(ProfileInfo.Level));

                GoogleAnalyticsWrapper.LogEvent(
                    new CustomEventHitBuilder()
                        .SetParameter(GAEvent.Category.SilverEarnedInBattle)
                        .SetParameter<GAEvent.Action>()
                        .SetSubject(GAEvent.Subject.VehicleID, ProfileInfo.CurrentVehicle)
                        .SetParameter<GAEvent.Label>()
                        .SetSubject(GAEvent.Subject.SilverAmount, silverAmountSubjectValue)
                        .SetValue(ProfileInfo.Level));
            }

            // Логгирование заработанного золота:
            if (goldAmount > 0)
            {
                GoogleAnalyticsWrapper.LogEvent(
                new CustomEventHitBuilder()
                    .SetParameter(GAEvent.Category.GoldEarnedInBattle)
                    .SetParameter<GAEvent.Action>()
                    .SetSubject(GAEvent.Subject.MapName, Loading.PreviousScene)
                    .SetParameter<GAEvent.Label>()
                    .SetSubject(GAEvent.Subject.GoldAmount, goldAmountSubjectValue)
                    .SetValue(ProfileInfo.Level));

                GoogleAnalyticsWrapper.LogEvent(
                    new CustomEventHitBuilder()
                        .SetParameter(GAEvent.Category.GoldEarnedInBattle)
                        .SetParameter<GAEvent.Action>()
                        .SetSubject(GAEvent.Subject.VehicleID, ProfileInfo.CurrentVehicle)
                        .SetParameter<GAEvent.Label>()
                        .SetSubject(GAEvent.Subject.GoldAmount, goldAmountSubjectValue)
                        .SetValue(ProfileInfo.Level));
            }

            // Логгирование заработанного опыта:
            if (experienceAmount > 0)
            {
                GoogleAnalyticsWrapper.LogEvent(
                    new CustomEventHitBuilder()
                        .SetParameter(GAEvent.Category.ExperienceEarnedInBattle)
                        .SetParameter<GAEvent.Action>()
                        .SetSubject(GAEvent.Subject.MapName, Loading.PreviousScene)
                        .SetParameter<GAEvent.Label>()
                        .SetSubject(GAEvent.Subject.ExperienceAmount, experienceAmountSubjectValue)
                        .SetValue(ProfileInfo.Level));

                GoogleAnalyticsWrapper.LogEvent(
                    new CustomEventHitBuilder()
                        .SetParameter(GAEvent.Category.ExperienceEarnedInBattle)
                        .SetParameter<GAEvent.Action>()
                        .SetSubject(GAEvent.Subject.VehicleID, ProfileInfo.CurrentVehicle)
                        .SetParameter<GAEvent.Label>()
                        .SetSubject(GAEvent.Subject.ExperienceAmount, experienceAmountSubjectValue)
                        .SetValue(ProfileInfo.Level));
            }
        }
        #endregion
    }

    private IncomeFrameRow GetCustomRowPrefabByType(RowType rType)
    {
        for (int i = 0; i < customRowPrefabs.Count; i++)
            if (customRowPrefabs[i].type == rType)
                return customRowPrefabs[i].prefab;

        return null;
    }
}
