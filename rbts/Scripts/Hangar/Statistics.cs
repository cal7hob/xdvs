using UnityEngine;

public class Statistics : MonoBehaviour
{
    public tk2dTextMesh battlesCount;
    public tk2dTextMesh givenDamage;
    public tk2dTextMesh shoots;
    public tk2dTextMesh hits;
    public tk2dTextMesh accuracy;
    public tk2dTextMesh frags;
    public tk2dTextMesh mileage;
    public tk2dTextMesh deaths;

    public GameObject btnAchievements; // Need for Future Tanks On iOS, may be for IronTanks too.
    public GameObject btnRating;
    public GameObject btnOk;//Only in IT
    public GameObject btnClans;
    public GameObject gameCenterButtonsWraper;
    public LabelLocalizationAgent[] btnClansAgents;
    [Header("btnStatisticks for IronTanks only")]
    public GameObject btnStatistics;

    public tk2dBaseSprite[] socialIcons;//Icons g+ and GameCenter on buttons
    public static Statistics Instance;

    private void Awake()
    {
        Instance = this;
        Messenger.Subscribe(EventId.ClanChanged, OnClanChanged);
        Messenger.Subscribe(EventId.GameCenterDisabled, GameCenterDisabled);
    }

    private void OnDestroy()
    {
        Messenger.Unsubscribe(EventId.ClanChanged, OnClanChanged);
        Messenger.Unsubscribe(EventId.GameCenterDisabled, GameCenterDisabled);
        Instance = null;
    }

    //TODO: Возможно, CloseStatistics больше не используется в префабах, и её можно удалить
    private void CloseStatistics(tk2dUIItem item) { GUIPager.Back(); }

    private void OpenClansWebPageFromStatistics(tk2dUIItem item)
    {
        GUIPager.Back();
        ClansManager.Instance.OpenClansWebPage();
    }

    public void ShowStatistics()
    {
        GUIPager.SetActivePage("Statistics", true, GameData.IsGame(Game.IronTanks | Game.SpaceJet | Game.BattleOfHelicopters));//Choose project to show BlackAlphaLayer in it

        bool countRocketShots = GameData.IsGame(Game.BattleOfWarplanes | Game.BattleOfHelicopters | Game.ApocalypticCars);

        battlesCount.text = BattleStatisticsManager.GetOverallBattleStatStringSafely("BattlesCount");
        givenDamage.text = BattleStatisticsManager.GetOverallBattleStatStringSafely("TotalGivenDemage");
        shoots.text = BattleStatisticsManager.GetOverallBattleStatStringSafely(countRocketShots ? "TotalShoots_SACLOS" : "TotalShoots");
        hits.text = BattleStatisticsManager.GetOverallBattleStatStringSafely(countRocketShots ? "TotalHits_SACLOS" : "TotalHits");
        accuracy.text = BattleStatisticsManager.GetOverallBattleStatStringSafely(countRocketShots ? "OverralAccuracy_SACLOS" : "OverralAccuracy");
        frags.text = BattleStatisticsManager.GetOverallBattleStatStringSafely("TotalFrags");
        mileage.text = BattleStatisticsManager.GetOverallBattleStatStringSafely("TotalMileage");
        deaths.text = BattleStatisticsManager.GetOverallBattleStatStringSafely("TotalDeaths");

        SetupButtons();
    }

    public void SetupButtons()
    {
        // Enable / disable buttons.
        if (btnAchievements == null || btnRating == null)
            return;

#if UNITY_WEBPLAYER || UNITY_WEBGL
        if (!GameData.IsGame(Game.IronTanks))// В IT окно с кнопками пропускается.
        {
            btnAchievements.SetActive(false);
            btnRating.SetActive(false);
        } 
#elif UNITY_IOS || UNITY_STANDALONE_OSX
        btnAchievements.SetActive(true);
        btnRating.SetActive(true);

        //Replace g+ Icon by GameCenter Icon
        if(socialIcons != null)
            for(int i = 0; i < socialIcons.Length; i++)
                socialIcons[i].SetSprite("GCIcon");
#else //Android & WindowsPhone
        btnAchievements.SetActive(true);
        btnRating.SetActive(true);
#endif

        //Включаем / выключаем кнопки кланы и ОК
        if (!GameData.IsGame(Game.IronTanks))
        {
            btnClans.SetActive(true);
        }
        else//IronTanks
        {
            if (GUIPager.ActivePage != "CommonStat" && GameData.IsGame(Game.IronTanks))
            {
                WorkaroundForIronTanksClanButton();
#if UNITY_WEBPLAYER || UNITY_WEBGL
                btnOk.SetActive(false);
                btnClans.SetActive(true);
#else
                btnOk.SetActive(true);
                btnClans.SetActive(false);
#endif
            }
            else
            {
                btnOk.SetActive(true);
                btnClans.SetActive(true);
            }
        }

        if (ProfileInfo.Level < GameData.accountManagementMinLevel)
        {
            btnClans.SetActive(false);
        }

        UpdateL10NAgents();
    }

    private void WorkaroundForIronTanksClanButton()
    {
        Transform[] children = transform.GetComponentsInChildren<Transform>(true);

        foreach (Transform child in children)
        {
            if (child.gameObject.name == "btnOk")
            {
                btnOk = child.gameObject;
            }
            else if (child.gameObject.name == "btnClans")
            {
                btnClans = child.gameObject;
            }
        }
    }

    private void OnClanChanged(EventId id, EventInfo info)
    {
        UpdateL10NAgents();
    }

    private void GameCenterDisabled(EventId id, EventInfo info)
    {
        gameCenterButtonsWraper.SetActive(false);

        if (GameData.IsGame(Game.IronTanks))
        {
            btnStatistics.transform.localPosition = new Vector3(btnStatistics.transform.localPosition.x, 74, btnStatistics.transform.localPosition.z);
            btnClans.transform.localPosition = new Vector3(btnStatistics.transform.localPosition.x, -36, btnStatistics.transform.localPosition.z);
        }
    }

    private void UpdateL10NAgents()
    {
        if (btnClans == null)
            return;

        //Для совместимости с предыдущими проектами, в которых ссылки в btnClansAgents не накинуты... ()
        LabelLocalizationAgent[] agents = btnClansAgents != null ? btnClansAgents : new LabelLocalizationAgent[] { btnClans.transform.Find("lblClans").GetComponent<LabelLocalizationAgent>() };

        if(agents != null)
            for(int i = 0; i < agents.Length; i++)
            {
                if (ProfileInfo.Clan != null)
                    agents[i].key = "lblClan";
                else
                    agents[i].key = "lblClanJoin";

                agents[i].LocalizeLabel();
            }
    }
}
