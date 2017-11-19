using System;
using System.Collections;
using System.Collections.Generic;
using Tanks.Models;
using UnityEngine;
using JSONObject = System.Collections.Generic.Dictionary<string, object>;
using ScoresItemByArea = System.Collections.Generic.Dictionary<string, ScoresItem>;
using ScoresObject = System.Collections.Generic.Dictionary<string, System.Collections.Generic.Dictionary<string, ScoresItem>>;

public class ScoresController : MonoBehaviour
{
    public tk2dUILayout mainLayout;
    // Область, в которой будет создаваться и скроллироваться контент статистики
    public tk2dUIScrollableArea scrollableArea;
    public tk2dUIItem btnToggleArrow;

    public PanelSwitch topSwitcher;
    public PanelSwitch leftSwitcher;
    public FriendsScoresPage friendsScores;
    public GameObject mainBackground;

    public ScoresItemPlayer playerItemPrefab;
    public ScoresItemClan clanItemPrefab;
    public ScoresItemCreateClan createClanItemPrefab;

    [SerializeField]
    private ScoresMenuBehaviourPlayer playerMenuBehaviour;
    public ScoresMenuBehaviourPlayer PlayerMenuBehaviour { get { return playerMenuBehaviour; } }
    [SerializeField]
    private ScoresMenuBehaviourClan clanMenuBehaviour;
    public bool IsScoresReceived { get; private set; }

    public float overrideBottomPanelHeight; // used in TW
    public int minPlaceForNextPanelSwitch = 5;
    public float spaceBetweenItems = 8f;

    public LabelLocalizationAgent timer;

    private int m_lastCamHeight;
    private Dictionary<string, ScoresPage> m_pages = new Dictionary<string, ScoresPage>();
    private Dictionary<string, PageOptions> m_options = new Dictionary<string, PageOptions>();
    private string m_currentKey;
    private ScoresPage m_currentPage;
    private ScoresObject highlightedItemByAreaByTime;
    private HashSet<System.Object> panelHiders = new HashSet<System.Object>();

    public ScoresObject HighlightedItemByAreaByTime { get { return highlightedItemByAreaByTime; } }

    private int? MyPlace
    {
        get
        {
            if (highlightedItemByAreaByTime.ContainsKey(leftSwitcher.CurrentPanel.key) &&
                highlightedItemByAreaByTime[leftSwitcher.CurrentPanel.key] != null
                && highlightedItemByAreaByTime[leftSwitcher.CurrentPanel.key].ContainsKey(topSwitcher.CurrentPanel.key)
                && highlightedItemByAreaByTime[leftSwitcher.CurrentPanel.key][topSwitcher.CurrentPanel.key] != null)
                return highlightedItemByAreaByTime[leftSwitcher.CurrentPanel.key][topSwitcher.CurrentPanel.key].Place;

            return null;
        }
    }

    public static JSONObject ScoresData { get; private set; }

    public static ScoresController Instance { get; private set; }

    private void Awake()
    {
        Instance = this;

        m_options["friends"] = new PageOptions();

        Dispatcher.Subscribe(EventId.AfterHangarInit, AfterHangarInitHandler);
        Dispatcher.Subscribe(EventId.MessageBoxChangeVisibility, OnMessageBoxChangeVisibility);
        Dispatcher.Subscribe(EventId.ChangeElementStateRequest, OnChangeElementStateRequest);
    }

    private void OnDestroy()
    {
        leftSwitcher.OnPanelChanged -= PanelChanged;
        topSwitcher.OnPanelChanged -= PanelChanged;
        HangarController.OnTimerTick -= ScoresWeeklyBonus;

        Dispatcher.Unsubscribe(EventId.AfterHangarInit, AfterHangarInitHandler);
        Dispatcher.Unsubscribe(EventId.MessageBoxChangeVisibility, OnMessageBoxChangeVisibility);
        Dispatcher.Unsubscribe(EventId.ChangeElementStateRequest, OnChangeElementStateRequest);

        Instance = null;
    }

    protected class PageOptions
    {
        public float position = 0f;
        public bool isFirstOpen = true;
    }

    public void UpdatePlayer(Player player)
    {
        foreach (var page in m_pages)
            page.Value.UpdatePlayer(player);

        friendsScores.UpdatePlayer(player);
    }

    public IEnumerator ScoresReceived(JSONObject data)
    {
        ScoresData = data;

        bool thereIsWorld = false;

        highlightedItemByAreaByTime = new ScoresObject();

        foreach (var timeDict in data)
        {
            var areaDict = timeDict.Value as JSONObject;

            if (areaDict == null)
                continue;

            highlightedItemByAreaByTime[timeDict.Key] = new ScoresItemByArea();

            foreach (var area in areaDict)
            {
                var key = timeDict.Key + "_" + area.Key;

                yield return
                    UpdatePage(key, timeDict.Key, area.Key, area.Value as JSONObject,
                       highlightedItem => highlightedItemByAreaByTime[timeDict.Key][area.Key] = highlightedItem);

                yield return null;
            }

            if (areaDict.ContainsKey("world"))
                thereIsWorld = true;
        }

        if (!thereIsWorld)
            topSwitcher.hide("world");

        foreach (var p in topSwitcher.panels)
        {
            if ((p.secondLabel == "UNKNOWN") && (p.key == "city" || p.key == "region"))
            {
                topSwitcher.hide(p.key);
            }
        }

        #region Выключаем пустые страницы

        foreach (var panel in topSwitcher.panels)
        {
            if (panel.isHided || panel.key == "friends") // За friends отвечает FriendsManager
                continue;

            var found = false;

            foreach (var leftPanel in leftSwitcher.panels)
            {
                var pageKey = leftPanel.key + "_" + panel.key;

                if (!m_pages.ContainsKey(pageKey))
                    continue;

                found = true;
                break;
            }

            if (found)
                continue;

            topSwitcher.hide(panel.key);
        }

        #endregion

        Dispatcher.Send(EventId.ScoresHighlightedItemsReady, null);

        #region Турниры
        // Включаем только те турниры, информация о которых пришла с сервера
        foreach (var panel in topSwitcher.panels)
        {
            if (!panel.isHided)
            {
                WeeklyAwardsArea? awardAreaFromPanelKey = WeeklyAwardsInfo.WeeklyAwardAreaFromKey(panel.key);

                if (awardAreaFromPanelKey.HasValue
                    && WeeklyAwardsInfo.WeeklyAwards.ContainsKey(awardAreaFromPanelKey)
                    && !panel.hideOnOpen.Contains(topSwitcher.advancedPanel.panel.gameObject))
                    continue;

                panel.hideOnOpen.Add(topSwitcher.advancedPanel.panel.gameObject);
            }
        }

        // Пользователь убирается из статистики и перестаёт участвовать в турнирах, кроме кланового,
        // когда он в клане. Выключаем турниры.
        /*if (ClansManager.Instance.playersClan != null)
        {
            foreach (var panel in topSwitcher.panels)
            {
                if (!panel.isHided && panel.key != "clans" && !panel.hideOnOpen.Contains(topSwitcher.advancedPanel.panel.gameObject))
                    panel.hideOnOpen.Add(topSwitcher.advancedPanel.panel.gameObject);
            }
        }*/
        #endregion

        PanelChanged();

        string keyTime;
        string keyPlace;

        GetPreferredScoresPage(out keyTime, out keyPlace);

        if (!string.IsNullOrEmpty(keyPlace))
            topSwitcher.switchTo(keyPlace);

        if (!string.IsNullOrEmpty(keyTime))
            leftSwitcher.switchTo(keyTime);

        ScoresWeeklyBonus(default(double));

        IsScoresReceived = true;
        UpdateVisibility();
        Dispatcher.Send(EventId.ScoresBoxActivated, null);
    }

    public void GetPreferredScoresPage(out string keyTime, out string keyPlace)
    {
        keyTime = null;
        keyPlace = null;

        // По умолчанию показывать таблицу с рейтингом кланов если игрок состоит в клане
        if (ProfileInfo.Clan != null)
        {
            keyPlace = "clans";
        }
        else if (friendsScores.HighlightedItem != null && friendsScores.HighlightedItem.Place > minPlaceForNextPanelSwitch)
        {
            keyPlace = "friends";
        }
        else
        {
            // Ищем лидерборду, в которой еще стоит подняться выше в рейтинге
            // Если в предыдущей мы и так высоко находимся
            for (int iTime = 0; iTime < leftSwitcher.panels.Length; iTime++)
            {
                var tKeyTime = leftSwitcher.panels[iTime].key;

                if (!highlightedItemByAreaByTime.ContainsKey(tKeyTime))
                    continue;

                keyTime = tKeyTime;

                bool found = false;

                for (int iPlace = 0; iPlace < topSwitcher.panels.Length; iPlace++)
                {
                    var tKeyPlace = topSwitcher.panels[iPlace].key;
                    if (!highlightedItemByAreaByTime[tKeyTime].ContainsKey(tKeyPlace))
                        continue;

                    keyPlace = tKeyPlace;

                    //Debug.LogErrorFormat("highlightedItemByAreaByTime[{0}][{1}].Place == {2}", tKeyTime, tKeyPlace, highlightedItemByAreaByTime[tKeyTime][tKeyPlace] == null ? "null" : highlightedItemByAreaByTime[tKeyTime][tKeyPlace].Place.ToString());

                    if (highlightedItemByAreaByTime[tKeyTime][tKeyPlace] == null ||
                        highlightedItemByAreaByTime[tKeyTime][tKeyPlace].Place > minPlaceForNextPanelSwitch)
                    {
                        found = true;
                        break;
                    }
                }

                if (found)
                    break;
            }
        }
    }

    private IEnumerator UpdatePage(string key, string timeKey, string placeKey, JSONObject areaDict,
        Action<ScoresItem> highlightedItem)
    {
        if (areaDict == null)
        {
            throw new Exception("Data is null");
        }
        var prefs = new JsonPrefs(areaDict);

        string placeName = prefs.ValueString("name", "Unknown");
        topSwitcher.SetSecondLabelForKey(placeKey, placeName);

        ScoresPage page;
        if (!m_pages.ContainsKey(key))
        {
            switch (placeKey)
            {
                case "clans":
                    page = ScoresPage.Create<ScoresPageClans>(key, scrollableArea, clanItemPrefab, clanMenuBehaviour);
                    break;
                default:
                    page = ScoresPage.Create<ScoresPagePlayers>(key, scrollableArea, playerItemPrefab, playerMenuBehaviour);
                    break;
            }

            page.PlaceKey = placeKey;
            m_pages[key] = page;
            page.gameObject.SetActive(false);
            m_options[key] = new PageOptions();
        }
        else
        {
            page = m_pages[key];
            page.Clear();
        }

        var leaderboard = prefs.ValueObjectList("leaderBoard", null);
        if (leaderboard == null)
        {
            Debug.LogError("Can't get leaderboard for " + timeKey + " - " + placeKey);
            highlightedItem(null);
            yield break;
        }

        foreach (var item in leaderboard)
        {
            page.AddItem(item as JSONObject);
            yield return null;
        }

        highlightedItem(page.Reposition());
    }

    void PanelChanged(int index = 0, string key = "")
    {
        // Сохранаяем настройки открытой страницы
        if (m_currentPage != null)
        {
            m_options[m_currentKey].position = scrollableArea.Value;
            m_currentPage.gameObject.SetActive(false);
            m_currentPage = null;
        }
        else if (m_currentKey == "friends")
        {
            m_options[m_currentKey].position = scrollableArea.Value;
        }

        // Открываем новую страницу
        string k = topSwitcher.CurrentPanel.key == "friends" ?
            topSwitcher.CurrentPanel.key :
            leftSwitcher.CurrentPanel.key + "_" + topSwitcher.CurrentPanel.key;
        m_currentKey = k;

        friendsScores.gameObject.SetActive(m_currentKey == "friends");

        if (m_pages.ContainsKey(k))
        {
            m_currentPage = m_pages[k];
            m_currentPage.gameObject.SetActive(true);
            scrollableArea.contentContainer = m_currentPage.gameObject;
            scrollableArea.ContentLength = m_currentPage.ContentLength;
            scrollableArea.Value = m_options[m_currentKey].position;
            if (m_options[m_currentKey].isFirstOpen)
            {
                m_options[m_currentKey].isFirstOpen = false;
                m_currentPage.scrollToHighlightedItem = true;
            }
            //scrollableArea.MeasureContentLength ();
        }
        else if (m_currentKey == "friends")
        {
            scrollableArea.contentContainer = friendsScores.gameObject;

            if (m_options[m_currentKey].isFirstOpen)
            {
                m_options[m_currentKey].isFirstOpen = false;

                friendsScores.Reposition();
                friendsScores.scrollToHighlightedItem = true;
            }

            scrollableArea.ContentLength = friendsScores.ContentLength;
            scrollableArea.Value = m_options[m_currentKey].position;
        }
    }

    public void ShowWeeklyAwardsInfoPage()
    {
        //if(MyPlace == null)
        //{
        //    Debug.LogError("Can't show Weekly Awards Page! MyPlace == NULL.");
        //    return;
        //}
        MenuController.NextSound();
        WeeklyAwardsInfo.Instance.ShowWeeklyAwardsInfoPage(MyPlace, topSwitcher.CurrentPanel.key, topSwitcher.CurrentPanel.secondLabel);
        ScoresWeeklyBonus(default(double)); // Аргумент не используется в методе
    }

    private void ScoresWeeklyBonus(double timeStamp)
    {
        long secondsTillWeeklyTournamentDeadline = GameData.weeklyTournamentEndTime - (long)GameData.CorrectedCurrentTimeStamp;

        if (secondsTillWeeklyTournamentDeadline <= 0)
        {
            secondsTillWeeklyTournamentDeadline = 0;

            var request = Http.Manager.Instance().CreateRequest("/tournament/info");
            Http.Manager.StartAsyncRequest(request,
                successfullResult =>
                {
                    var prefs = new JsonPrefs(successfullResult.Data);
                    GameData.weeklyTournamentEndTime = prefs.ValueLong("endTime");
                    secondsTillWeeklyTournamentDeadline =
                        GameData.weeklyTournamentEndTime - (long)GameData.CorrectedCurrentTimeStamp;
                },
                failedResult => Debug.LogError("Failed to get weeklyTournamentEndTime"));
        }

        var timerString = Clock.GetTimerString(secondsTillWeeklyTournamentDeadline) ?? "";
        if (timer != null && !string.IsNullOrEmpty(timerString))
        {
            timer.Parameter = timerString;
        }

        if (GUIPager.ActivePage == "WeeklyAwardsInfo")
        {
            if (WeeklyAwardsInfo.Instance.lblTimer != null)
            {
                WeeklyAwardsInfo.Instance.lblTimer.text = timerString;
            }
        }
    }
    
    void Start()
    {
        VerticalAlign();
        leftSwitcher.OnPanelChanged += PanelChanged;
        topSwitcher.OnPanelChanged += PanelChanged;
        PanelChanged();
        SetActive(true);
    }

    void VerticalAlign()
    {
        float layoutBottomPosition;

        if (overrideBottomPanelHeight != default(float))
        {
            layoutBottomPosition = overrideBottomPanelHeight;
        }
        else
        {
            layoutBottomPosition = MenuController.Instance.bottomGuiPanel.GetComponent<Renderer>().bounds.size.y;
        }
        var delta = (layoutBottomPosition + HangarController.Instance.Tk2dGuiCamera.ScreenExtents.yMin) - mainLayout.GetMinBounds().y;

        // Для того, чтобы при Reshape() изменялся tk2dUIScrollableArea.VisibleAreaLength,
        // нужно накинуть родительский tk2dUILayout на tk2dUIScrollableArea.BackgroundLayoutItem.
        mainLayout.Reshape(new Vector3(0, delta, 0), Vector3.zero, true);
    }

    void LateUpdate()
    {
        if ((int)HangarController.Instance.Tk2dGuiCamera.ScreenExtents.yMin != m_lastCamHeight)
        {
            m_lastCamHeight = (int)HangarController.Instance.Tk2dGuiCamera.ScreenExtents.yMin;
            VerticalAlign();
        }
    }

    public void SetActive(bool en)
    {
        mainLayout.gameObject.SetActive(en);

        Dispatcher.Send(EventId.TouchableAreaChanged, null);
    }

    private void AfterHangarInitHandler(EventId id, EventInfo info)
    {
        HangarController.OnTimerTick += ScoresWeeklyBonus;
    }

    private void OnMessageBoxChangeVisibility(EventId id, EventInfo info)
    {
        UpdateVisibility();
    }

    private void OnChangeElementStateRequest(EventId id, EventInfo info)
    {
        EventInfo_U eventInfoU = info as EventInfo_U;
        ChangeElementStateRequestInfo stateInfo = (ChangeElementStateRequestInfo)eventInfoU[0];
        if (!stateInfo.ForMe(GetType()))
        {
            return;
        }
        if (!panelHiders.Contains(stateInfo.sender) && !stateInfo.state)//если просят выключить панель и заявки от него еще нету
        {
            panelHiders.Add(stateInfo.sender);
            UpdateVisibility();
        }
        else if (stateInfo.state && panelHiders.Contains(stateInfo.sender))
        {
            panelHiders.Remove(stateInfo.sender);
            UpdateVisibility();
        }
    }

    private void UpdateVisibility()
    {
        gameObject.SetActive(IsScoresReceived && !(MessageBox.IsShown || panelHiders.Count > 0));
    }

    public void OnArrowClick(tk2dUIItem arrowItem) 
    {
        MenuController.SmallArrowSound();
    }
    private void ShowHideArrow(tk2dUIItem arrowItem) 
    {
        //Перенесено в AnimatedWindow
       // MenuController.checkBoxSound);
    }
}
