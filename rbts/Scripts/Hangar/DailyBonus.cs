using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class DailyBonus : MonoBehaviour, IQueueablePage
{
    public Transform dailyBonusWrapper;
    public BonusFrame bonusFramePrefab;
    public tk2dTextMesh header;
    public Color header_firstStringColor = Color.white;
    public Color header_playEverydayColor = Color.white;
    public Color header_gameNameColor = Color.white;
    public Color header_toGetBonusColor = Color.white;
    public float distanceBetweenItems = 0;

    private List<BonusFrame> bonusFrames = new List<BonusFrame>();

    public static Dictionary<int, ProfileInfo.Price> dailyBonusesDict;
   
    public static void ShowDailyBonusPage()
    {
        if (ProfileInfo.launchesCount < 2 || GUIPager.ActivePage == "DailyBonus" || dailyBonusesDict == null || GUIPager.QueueContainsPage("DailyBonus"))
            return;

        GUIPager.EnqueuePage("DailyBonus", false, true);
    }

    public void BeforeActivation()
    {
        if (header != null)
            header.text = Localizer.GetText("lblBonusText",
                header_firstStringColor.To2DToolKitColorFormatString(),
                header_playEverydayColor.To2DToolKitColorFormatString(),
                header_gameNameColor.To2DToolKitColorFormatString(),
                Application.productName,
                header_toGetBonusColor.To2DToolKitColorFormatString());

        //Инстанируем бонусы
        var lastDailyBonusDataItem = dailyBonusesDict.Last();
        int i = 0;
        foreach (var bonusData in dailyBonusesDict)
        {
            BonusFrame bonus;
            if (bonusFrames.ElementAtOrDefault(i) == null)
            {
                bonus = Instantiate(bonusFramePrefab);
                bonus.gameObject.name = string.Format("day_{0:00}", i);
                bonusFrames.Add(bonus);
                bonus.transform.SetParent(dailyBonusWrapper);
                bonus.transform.localPosition = new Vector3(distanceBetweenItems * i, 0, 0);
            }
            else
            {
                bonus = bonusFrames[i];
            }
            bonus.Setup(bonusData, bonusData.Equals(lastDailyBonusDataItem));
            i++;
        }
    }

    public void Activated()
    {
        GUIPager.ResetBlackAlphaLayerSortingOrder();
    }
}
