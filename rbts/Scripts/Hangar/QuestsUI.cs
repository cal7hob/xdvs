using UnityEngine;

public class QuestsUI : MonoBehaviour
{
    public static QuestsUI Instance { get; private set; }

    public tk2dTextMesh[] questInfo;
    public tk2dTextMesh[] questReward;
    public tk2dBaseSprite[] checkBoxes;

    public tk2dTextMesh timer;

    public tk2dTextMesh goldQuestInfo;
    public tk2dTextMesh goldReward;

    public Transform currentQuestArrow;

    public tk2dSprite[] questSilverIcons = new tk2dSprite[3];
    public tk2dSprite[] questGoldIcons = new tk2dSprite[3];

    public HorizontalLayout[] arrayToAlign;
    public Transform[] questsParents;

    public Color activeQuestColor = new Color(1f, 0.5137f, 0.1764f);
    public Color inactiveQuestColor = new Color(1, 1, 1, 0.4f);

    private void Awake()
    {
        Instance = this;
    }

    void OnDestroy()
    {
        Instance = null;
    }

    /// <summary>
    /// change opacity of quests, moving pointer
    /// </summary>
    public void MarkerizeQuestList()
    {
        if (arrayToAlign != null)
            for (int j = 0; j < arrayToAlign.Length; j++)
                if (arrayToAlign[j] != null)
                    arrayToAlign[j].Align();
        int questsCompleted = QuestsInfo.CurrentQuestIndex;

        int i = 0;
        foreach (Quest quest in QuestsInfo.Quests)  
        {
            questInfo[i].color = inactiveQuestColor;//текст квеста
            questReward[i].color = inactiveQuestColor;//сколько денег
            questSilverIcons[i].color = new Color(questSilverIcons[i].color.r, questSilverIcons[i].color.g, questSilverIcons[i].color.b, inactiveQuestColor.a);//валюта денег
            questGoldIcons[i].color = new Color(questGoldIcons[i].color.r, questGoldIcons[i].color.g, questGoldIcons[i].color.b, inactiveQuestColor.a);//валюта денег
            checkBoxes[i].color = new Color(checkBoxes[i].color.r, checkBoxes[i].color.g, checkBoxes[i].color.b, inactiveQuestColor.a);

            Transform plus = checkBoxes[i].transform.Find("plus");
            if (plus != null)
                plus.gameObject.SetActive(false);

            if (i < questsCompleted)
            {
                if(plus != null)
                {
                    plus.gameObject.SetActive(true);
                    tk2dBaseSprite checkBox_check = plus.GetComponent<tk2dBaseSprite>();
                    checkBox_check.color = new Color(checkBox_check.color.r, checkBox_check.color.g, checkBox_check.color.b, inactiveQuestColor.a);
                }
            }
            else
            {
                if (i == questsCompleted)
                {
                    questInfo[i].color = activeQuestColor;
                    questReward[i].color = activeQuestColor;
                    questSilverIcons[i].color = Color.white;
                    questGoldIcons[i].color = Color.white;
                    checkBoxes[i].color = Color.white;
                }
            }

            //localize quest description and price
            questInfo[i].text = quest.LocalizedDescription;

            questReward[i].text = ProfileInfo.IsPlayerVip
                ? quest.LocalizedVipReward
                : quest.LocalizedReward;

            quest.CurReward.SetMoneySpecificColorIfCan(questReward[i]);

            i++;
        }

        //changing quests pointer position
        currentQuestArrow.SetParent(questsParents[Mathf.Clamp(questsCompleted, 0, questsParents.Length - 1)]);
        currentQuestArrow.localPosition = Vector3.zero;

        goldReward.text = QuestsInfo.CurRewardForThreeQuests.LocalizedValue;
        QuestsInfo.CurRewardForThreeQuests.SetMoneySpecificColorIfCan(goldReward);
    }

    public static void SetQuestsTimer(double time)
    {
        Instance.timer.text = Clock.GetTimerString ((int)(ProfileInfo.nextDayServerTime - GameData.CorrectedCurrentTimeStamp));
    }
}
