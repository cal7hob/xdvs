using System;
using System.Collections;
using System.Collections.Generic;
using Tanks.Models;
using UnityEngine;
using JSONObject = System.Collections.Generic.Dictionary<string, object>;

public class WeeklyAwardsInfo : MonoBehaviour
{
    public tk2dTextMesh lblTimer;

    [SerializeField] private tk2dTextMesh selectedArea;
    [SerializeField] private tk2dTextMesh regionLabel;
    [SerializeField] private LabelLocalizationAgent playerPlace;
    
    [SerializeField] private tk2dUILayout layoutToBeResized;
    [SerializeField] private UniAlignerBase[] aligners;

    [SerializeField] private AwardForPlace[] awardsForPlaces = new AwardForPlace[4];

    private WeeklyAwardsArea? weeklyAwardsAreaKey;

    private Vector3 defaultLayoutMinBounds, defaultLayoutMaxBounds, defaultWindowLocalposition;

    private const float HEADER_ROW_HEIGHT = 85;

    private static Dictionary<WeeklyAwardsArea?, List<WeeklyAward>> weeklyAwards;
    public static Dictionary<WeeklyAwardsArea?, List<WeeklyAward>> WeeklyAwards
    {
        get
        {
            return weeklyAwards
              ?? (weeklyAwards = AwardsFromJSONPrefs(GameData.tournamentAwardsJSONPrefs));
        }
    }

    public static WeeklyAwardsInfo Instance { get; private set; }

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        if (layoutToBeResized == null)
            return;

        defaultLayoutMinBounds = layoutToBeResized.GetMinBounds();
        defaultLayoutMaxBounds = layoutToBeResized.GetMaxBounds();
        defaultWindowLocalposition = layoutToBeResized.transform.localPosition;
    }

    private void OnDestroy() { Instance = null; }

    private static Dictionary<WeeklyAwardsArea?, List<WeeklyAward>> AwardsFromJSONPrefs(JsonPrefs weeklyAwardsJsonPrefs)
    {
        //Debug.LogWarning("WeeklyAwardsInfo.AwardsFromJSONPrefs() weeklyAwardsJsonPrefs: " + weeklyAwardsJsonPrefs);

        var weeklyAwards =
            new Dictionary<WeeklyAwardsArea?, List<WeeklyAward>>();

        foreach (WeeklyAwardsArea awardArea in Enum.GetValues(typeof(WeeklyAwardsArea)))
        {
            // Get enum's key as string
            var areaName = awardArea.ToString().ToLower();

            if (!weeklyAwardsJsonPrefs.Contains(areaName))
                continue;

            var weeklyAwardsList = new List<WeeklyAward>();

            foreach (JSONObject award in weeklyAwardsJsonPrefs.ValueObjectList(areaName, new List<object>()))
            {
                //Debug.LogWarning("Area: " + awardArea + ", Award: " + award.ToStringFull());

                weeklyAwardsList.Add(new WeeklyAward(awardArea, award));
            }

            if (weeklyAwardsList.Count > 0)
                weeklyAwards[awardArea] = weeklyAwardsList;
        }

        return weeklyAwards;
    }

    public static WeeklyAwardsArea? WeeklyAwardAreaFromKey(string key)
    {
        try
        {
            return (WeeklyAwardsArea)Enum.Parse(typeof(WeeklyAwardsArea), key, true);
        }
#pragma warning disable 0168
        catch (Exception ex)
#pragma warning restore 0168
        {
            // Don't do it at home, baby
            // http://blogs.msdn.com/b/dotnet/archive/2009/02/19/why-catch-exception-empty-catch-is-bad.aspx

            //Debug.LogError("Enum conversion failed: " + ex.Message);
            return null;
        }
    }

    public void ShowWeeklyAwardsInfoPage(int? place, string area, string areaName)
    {
        //Debug.LogWarning("WeeklyAwardsInfo.ShowWeeklyAwardsInfoPage: " + "Place: " + place + ", Area: " + area);

        weeklyAwardsAreaKey = WeeklyAwardAreaFromKey(area);
        if (weeklyAwardsAreaKey == null || !WeeklyAwards.ContainsKey(weeklyAwardsAreaKey))
            return;

        // Изменение высоты и позиционирование окна с наградами
        if (layoutToBeResized != null)
        {
            float bodyYpositionDelta = 0;

            switch (weeklyAwardsAreaKey)
            {
                case WeeklyAwardsArea.World:
                case WeeklyAwardsArea.Clans:
                    bodyYpositionDelta = HEADER_ROW_HEIGHT;

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

        var selectedAreaParameter = Localizer.GetText(
            string.Format("WeeklyAwardArea{0}{1}", char.ToUpper(area[0]), area.Substring(1)));

        selectedArea.text = Localizer.GetText("lblWeeklyAwardArea", selectedAreaParameter);

        if (regionLabel != null) // Если задан собственный текст меш для региона - присваиваем регион туда, иначе в awardLabel
        {
            regionLabel.text = weeklyAwardsAreaKey != WeeklyAwardsArea.World && weeklyAwardsAreaKey != WeeklyAwardsArea.Clans ? areaName : "";
        }
        else
        {
            if (weeklyAwardsAreaKey != WeeklyAwardsArea.World && weeklyAwardsAreaKey != WeeklyAwardsArea.Clans)
                selectedArea.text += "\n" + areaName;
        }

        if (place != null)
        {
            playerPlace.Parameter = Convert.ToString(place);
            playerPlace.gameObject.SetActive(true);
        }
        else
        {
            playerPlace.gameObject.SetActive(false);
        }

        for (var i = 0; i < awardsForPlaces.Length; i++)
        {
            if (i < WeeklyAwards[weeklyAwardsAreaKey].Count)
            {
                var award = WeeklyAwards[weeklyAwardsAreaKey][i].Award;
                awardsForPlaces[i].awardPlace.Parameter = i == 3 ? "4 - 10" : Convert.ToString(i + 1);
                awardsForPlaces[i].awardAmount.text = award.value.ToString("N0", GameData.instance.cultureInfo.NumberFormat);
                awardsForPlaces[i].moneyIcon.SetCurrency(award.currency);
                awardsForPlaces[i].awardContainer.SetActive(true);
            }
            else
            {
                awardsForPlaces[i].awardContainer.SetActive(false);
            }
        }

        lblTimer.text = "";

        GUIPager.SetActivePage("WeeklyAwardsInfo", false, true);
        if (aligners != null)
            for (int i = 0; i < aligners.Length; i++)
                if (aligners[i] != null)
                    aligners[i].Align();
    }

    public void OnShareButtonPress()
    {
        StartCoroutine(Share());
    }

    private IEnumerator Share()
    {
        yield return new WaitForEndOfFrame();
        var area = weeklyAwardsAreaKey.ToString().ToLower();
        var text = Localizer.GetText(
            string.Format("textTournament{0}{1}Post", char.ToUpper(area[0]), area.Substring(1)));

        SocialSettings.GetSocialService().Post(text, MiscTools.GetScreenshot());
    }

    public void OnOKButtonPress()
    {
        GUIPager.Back();
    }
}
