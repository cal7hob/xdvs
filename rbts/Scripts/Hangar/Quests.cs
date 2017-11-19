using System;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

public class Quest
{
    public enum Type
    {
        Collect, // Собрать N бонусов.
        Revenge, // Отомсти обидчику.
        Mileage, // Проехать расстояние.
        MaxKillsInARowPerBattle, // Совершить убийств подряд за битву.
    }

    public int id;
    public Type type;
    public ProfileInfo.Price Reward;
    public ProfileInfo.Price VipReward;

    public int progress = 0;
    public bool isComplete = false;

    public Quest(int id, Type type, string battleStatsDictionaryKey, string localizationKey, int completeCount, ProfileInfo.Price reward, ProfileInfo.Price vipReward = null)
    {
        this.id = id;
        this.type = type;
        BattleStatsDictionaryKey = battleStatsDictionaryKey;
        LocalizationKey = localizationKey;
        CompleteCount = completeCount;
        Reward = reward;
        VipReward = vipReward;
    }

    public int CompleteCount { get; set; }
    public string BattleStatsDictionaryKey { get; set; }
    public string LocalizationKey { get; set; }

    public string LocalizedReward
    {
        get
        {
            return Reward == null
                ? "0"
                : Reward.value.ToString("N0", GameData.instance.cultureInfo.NumberFormat);
        }
    }

    public string LocalizedVipReward
    {
        get
        {
            return VipReward == null
                ? "0"
                : VipReward.value.ToString("N0", GameData.instance.cultureInfo.NumberFormat);
        }
    }
    public string LocalizedDescription
    {
        get
        {
            return type == Quest.Type.Revenge
                ? Localizer.GetText(LocalizationKey)
                : Localizer.GetText(LocalizationKey, CompleteCount);
        }
    }

    public ProfileInfo.Price CurReward { get { return ProfileInfo.IsPlayerVip ? VipReward : Reward; } }

    public override string ToString()
    {
        return string.Format("{0} {1}", BattleStatsDictionaryKey, CompleteCount);
    }

    public bool CheckQuestComplete()
    {
        switch (this.type)
        {
            case Type.MaxKillsInARowPerBattle:
            case Type.Mileage:
            case Type.Collect:
                if(!BattleStatisticsManager.BattleStats.ContainsKey(BattleStatsDictionaryKey))
                {
                    DT.LogError("CheckQuestComplete(). BattleStatisticsManager.BattleStats not contains {0}", BattleStatsDictionaryKey);
                    return false;
                }
                return BattleStatisticsManager.BattleStats[BattleStatsDictionaryKey] >= CompleteCount;
            case Type.Revenge:
                return BattleStatisticsManager.isRevengeDone;
            default: return false;
        }
    }

    public ProfileInfo.Price GetQuestReward(bool isVipQuest)
    {
        return isVipQuest ? VipReward : Reward;
    }

    public static Quest CreateFromDictionary(object dict)
    {
        var p = new JsonPrefs(dict);
        Quest q = new Quest(
            p.ValueInt("id"),
            (Type)Enum.Parse(typeof(Type), p.ValueString("quest/type"), true),
            p.ValueString("quest/name"),
            "qst" + p.ValueString("quest/name"),
            p.ValueInt("quest/amount"),
            ProfileInfo.Price.FromDictionary(p.ValueObjectDict("quest/reward")),
            ProfileInfo.Price.FromDictionary(p.ValueObjectDict("quest/rewardVip"))
        );
        q.progress = p.ValueInt("progress");
        q.isComplete = p.ValueBool("isComplete");
        return q;
    }
}

public static class QuestsInfo
{
    public static ProfileInfo.Price RewardForAllQuests = new ProfileInfo.Price(3, ProfileInfo.PriceCurrency.Gold); // Награда за выполнение всех квестов за день.
    public static ProfileInfo.Price VipRewardForAllQuests = new ProfileInfo.Price(10, ProfileInfo.PriceCurrency.Gold); // То же самое для випа.

    private static List<Quest> quests = new List<Quest>();
    private static int currentQuest = 0;

    public static bool IsAllQuestsCompleted
    {
        get { return currentQuest >= quests.Count; }
    }

    public static List<Quest> Quests { get { return quests; } }

    public static Quest CurrentQuest
    {
        get { 
            if((currentQuest < 0) || (currentQuest >= quests.Count)) {
                Debug.LogErrorFormat ("currentQuest id = {0} doesn't contains in quests!", currentQuest);
                return null;
            }

            return quests[currentQuest];
        }
    }
    public static int CurrentQuestIndex { get { return currentQuest; } }


    public static ProfileInfo.Price GetAllQuestsReward(bool isVip)
    {
        return isVip ? VipRewardForAllQuests : RewardForAllQuests;
    }

    public static ProfileInfo.Price CurRewardForThreeQuests
    {
        get { return ProfileInfo.IsPlayerVip ? VipRewardForAllQuests : RewardForAllQuests; }
    }

    public static void FromDictionary(List<object> list)
    {
        currentQuest = 0;
        quests.Clear();
        foreach (var i in list) {
            Quest q = Quest.CreateFromDictionary(i);
            quests.Add(q);
            if (q.isComplete) {
                currentQuest++;
            }
        }
        UpdateQuests(true);
    }

    public static void UpdateQuests(bool forceRegenerating = false)
    {
        if (forceRegenerating)
        {
            if (QuestsUI.Instance != null)
                QuestsUI.Instance.MarkerizeQuestList();//заменяем локазизации на случай если окно квестов открыто
        }

    }

}