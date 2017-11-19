using System;
using System.Collections;
using System.Collections.Generic;
using Tanks.Models;
using UnityEngine;
using JSONObject = System.Collections.Generic.Dictionary<string, object>;

public class WeeklyAwards : MonoBehaviour, IQueueablePage
{
    [SerializeField] private tk2dTextMesh awardLabel;
    [SerializeField] private tk2dTextMesh regionLabel;
    [SerializeField] private tk2dUILayout layoutToBeResized;
    [SerializeField] private AwardForPlace awardForPlace;
    [SerializeField] private UniAlignerBase[] aligners;

    private WeeklyAwardsArea awardArea;

    private Queue<WeeklyAward> weeklyAwards = new Queue<WeeklyAward>();

    private Vector3 defaultLayoutMinBounds, defaultLayoutMaxBounds, defaultWindowLocalposition;

    public static WeeklyAwards Instance { get; private set; }

    void Awake()
    {
        Instance = this;
        Dispatcher.Subscribe(EventId.AfterHangarInit, EnqueueWeeklyAwards);
        Dispatcher.Subscribe(EventId.WeeklyAwardsChanged, WeeklyAwardsChanged);
    }

    void Start()
    {
        if (layoutToBeResized == null)
            return;

        defaultLayoutMinBounds = layoutToBeResized.GetMinBounds();
        defaultLayoutMaxBounds = layoutToBeResized.GetMaxBounds();
        defaultWindowLocalposition = layoutToBeResized.transform.localPosition;
    }

    void OnDestroy()
    {
        Dispatcher.Unsubscribe(EventId.AfterHangarInit, EnqueueWeeklyAwards);
        Dispatcher.Unsubscribe(EventId.WeeklyAwardsChanged, WeeklyAwardsChanged);
        Instance = null;
    }

    public void BeforeActivation()
    {
        if (weeklyAwards.Count > 0)
        {
            var award = weeklyAwards.Dequeue();
            //Debug.LogWarning("Dequeing " + award.Area);

            ShowWeeklyAwardPage(award);
        }
    }

    public void Activated() { }

    public void WeeklyAwardsChanged(EventId id, EventInfo info)
    {
        //Debug.LogWarning("WeeklyAwardsChanged");

        if (weeklyAwards.Count > 0)
        {
            if(GUIPager.ActivePage != "WeeklyAwards")
                GUIPager.EnqueuePage("WeeklyAwards");
            else
                BeforeActivation();
        }
        else
        {
            if (GUIPager.ActivePage == "WeeklyAwards")
                GUIPager.ToMainMenu();
        }
    }

    public void EnqueueWeeklyAwards(EventId id, EventInfo info)
    {
        var weeklyAwardsJsonPrefs = new JsonPrefs(ProfileInfo.weeklyAwardsDict);
        ProfileInfo.weeklyAwardsDict = null;
        
        //Debug.LogWarning("WeeklyAwards: got data: " + weeklyAwardsJsonPrefs.ToString());

        foreach (WeeklyAwardsArea awardArea in Enum.GetValues(typeof(WeeklyAwardsArea)))
        {
            // Get enum's key as string
            var areaName = awardArea.ToString().ToLower();
            
            if (!weeklyAwardsJsonPrefs.Contains(areaName))
                continue;

            // areaName — массив JSON, чтобы можно было получать несколько наград за турнир.
            // Например, за 1 место по миру — 100 золота и 500000 серебра.
            foreach (JSONObject award in weeklyAwardsJsonPrefs.ValueObjectList(areaName, new List<object>()))
            {
                weeklyAwards.Enqueue(new WeeklyAward(awardArea, award));
            }
        }

        Dispatcher.Send(EventId.WeeklyAwardsChanged, null);
    }
   
    public void ShowWeeklyAwardPage(WeeklyAward award)
    {
        //Debug.LogWarning("Area: " + award.Area + ", Award: " + award.Award.ToString());
        awardArea = award.Area;

        // Изменение высоты и позиционирование окна с наградами
        if (layoutToBeResized != null)
        {
            float bodyYpositionDelta = 0;

            switch (award.Area)
            {
                case WeeklyAwardsArea.World:
                case WeeklyAwardsArea.Clans:
                    bodyYpositionDelta = 85;
                    break;
            }

            layoutToBeResized.SetBounds(defaultLayoutMinBounds,
                new Vector3(defaultLayoutMaxBounds.x,
                    defaultLayoutMaxBounds.y - bodyYpositionDelta,
                    defaultLayoutMaxBounds.z));

            layoutToBeResized.transform.localPosition =
                new Vector2(defaultWindowLocalposition.x,
                    defaultWindowLocalposition.y - bodyYpositionDelta / 2);
        }

        var areaName = award.Area.ToString().ToLower();
        var area = Localizer.GetText(
            string.Format("WeeklyAwardArea{0}{1}", char.ToUpper(areaName[0]), areaName.Substring(1)));

        awardLabel.text = Localizer.GetText("lblWeeklyAwardArea", area);

        if(regionLabel != null)//Если задан собственный текст меш для региона - присваиваем регион туда, иначе в awardLabel
        {
            regionLabel.text = (award.Area != WeeklyAwardsArea.World && award.Area != WeeklyAwardsArea.Clans) ? award.AreaName : "";
        }
        else
        {
            if (award.Area != WeeklyAwardsArea.World && award.Area != WeeklyAwardsArea.Clans)
                awardLabel.text += "\n" + award.AreaName;
        }

        awardForPlace.awardPlace.Parameter = Convert.ToString(award.Place);
        awardForPlace.awardAmount.text = award.Award.value.ToString("N0", GameData.instance.cultureInfo.NumberFormat);
        awardForPlace.moneyIcon.SetCurrency(award.Award.currency);
        awardForPlace.awardAmount.Commit();

        if (aligners != null)
            for (int i = 0; i < aligners.Length; i++)
                if (aligners[i] != null)
                    aligners[i].Align();
    }

    #region Обработка кнопок окна

    public void ShareWeeklyAwardsBtn()
    {
        StartCoroutine(Share());
    }

    private IEnumerator Share()
    {
        yield return new WaitForEndOfFrame();
        var area = awardArea.ToString().ToLower();
        var text = Localizer.GetText(
            string.Format("textTournament{0}{1}Post", char.ToUpper(area[0]), area.Substring(1)));

        SocialSettings.GetSocialService().Post(text, MiscTools.GetScreenshot());
    }

    public void TakeWeeklyAwardsBtn()
    {
        Dispatcher.Send(EventId.WeeklyAwardsChanged, null);
    }

    #endregion
}
