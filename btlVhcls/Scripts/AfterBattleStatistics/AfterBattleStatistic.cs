using System.Collections;
using UnityEngine;

public class AfterBattleStatistic : MonoBehaviour
{
    [SerializeField] private AfterBattleStatisticFrame battleStatisticFrame;
    [SerializeField] private AfterBattleIncomeFrame commonIncomeFrame;
    [SerializeField] private AfterBattleIncomeFrame vipIncomeFrame;
    [SerializeField] private tk2dTextMesh lblBattleResult;
    [SerializeField] private GameObject winnerReward;
    [SerializeField] private tk2dTextMesh lblWinnerReward;
    [SerializeField] private tk2dBaseSprite lblWinnerRewardCurrency;
    [SerializeField] private GameObject[] objectsActivatedIfVip;
    [SerializeField] private GameObject[] objectsDisactivatedIfVip;
    [SerializeField] private UniAlignerBase[] aligners;
    [SerializeField] private GameObject[] winObjects;
    [SerializeField] private GameObject[] defeatObjects;
    [SerializeField] private Color lblBattleResult_DeathmatchColor = Color.white;
    [SerializeField] private Color lblBattleResult_TeamMode_WinColor = Color.white;
    [SerializeField] private Color lblBattleResult_TeamMode_DefeatColor = Color.white;

    public GameObject btnGetDoubleRewardForAdsViewing;

    public static AfterBattleStatistic Instance { get; private set; }

    private bool playMoneySound;

    private void Awake()
    {
        Instance = this;
    }

    private void OnDestroy()
    {
        Instance = null;
    }

    public void Show()
    {
        MiscTools.CheckBtnVipState(objectsActivatedIfVip, objectsDisactivatedIfVip);

        var br = Http.Manager.BattleServer.result;

        winnerReward.SetActive(false);
        lblBattleResult.gameObject.SetActive(true);
        MiscTools.SetObjectsActivity(winObjects, false);
        MiscTools.SetObjectsActivity(defeatObjects, false);
        lblBattleResult.color = lblBattleResult_DeathmatchColor;

        if (!br.isProperBattleEnd)
            lblBattleResult.text = Localizer.GetText("lblNotEndBattle");
        else if (!br.isEnoughPlayers)
            lblBattleResult.text = Localizer.GetText("lblNotEnoughPlayers");
        else if (GameData.IsMode(GameData.GameMode.Deathmatch))
        {
            if(br.award.value >= 1)
            {
                lblBattleResult.text = br.place > GameData.maxPlayers ? string.Empty : Localizer.GetText("lblAwardAfterBattle", br.place);
                winnerReward.SetActive(true);
                lblWinnerReward.text = br.award.LocalizedValue;
                if (lblWinnerRewardCurrency)
                    lblWinnerRewardCurrency.SetSprite(br.award.SpriteName);
                playMoneySound = true;// set sound flag
            }
            else
                lblBattleResult.gameObject.SetActive(false);

            Dispatcher.Send(EventId.VoiceRequired, new EventInfo_I((int)VoiceEventKey.AfterBattleStatistic));
        }
        else if (GameData.IsMode(GameData.GameMode.Team))
        {
            if (ScoreCounter.FriendTeamScore > ScoreCounter.EnemyTeamScore)
            {
                lblBattleResult.text = Localizer.GetText("lblTeamWin");
                MiscTools.SetObjectsActivity(winObjects, true);
                lblBattleResult.color = lblBattleResult_TeamMode_WinColor;
                if(br.award.value >= 1)
                {
                    winnerReward.SetActive(true);
                    lblWinnerReward.text = br.award.LocalizedValue;
                    lblWinnerReward.SetMoneySpecificColorIfCan(br.award);
                    if (lblWinnerRewardCurrency)
                        lblWinnerRewardCurrency.SetSprite(br.award.SpriteName);
                    playMoneySound = true;
                }

                Dispatcher.Send(EventId.VoiceRequired, new EventInfo_I((int)VoiceEventKey.AfterBattleStatistic));
            }
            else
            {
                MiscTools.SetObjectsActivity(defeatObjects, true);
                lblBattleResult.text = Localizer.GetText("lblTeamLoose");
                lblBattleResult.color = lblBattleResult_TeamMode_DefeatColor;
            }
        }

        battleStatisticFrame.FillStatisticFrame();
        battleStatisticFrame.CheckIfFrameIsEmpty();

        commonIncomeFrame.Init(isVipFrame: false);//Надо вынести признак вип фрейма в паблик переменную
        vipIncomeFrame.Init(isVipFrame: true);

        if (!br.isAllQuestsCompleted && (br.quest != null))
        {
            Quest currentQuest = br.quest;
            bool currentQuestCompleted = currentQuest.isComplete;

            #region Google Analytics: quests

            GoogleAnalyticsWrapper.LogEvent(
                new CustomEventHitBuilder()
                    .SetParameter(GAEvent.Category.Quest)
                    .SetParameter(
                        currentQuestCompleted
                            ? GAEvent.Action.Completed
                            : GAEvent.Action.Failed)
                    .SetParameter<GAEvent.Label>()
                    .SetSubject(GAEvent.Subject.Quest, currentQuest)
                    .SetValue(ProfileInfo.Level));

            GoogleAnalyticsWrapper.LogEvent(
                new CustomEventHitBuilder()
                    .SetParameter(GAEvent.Category.Quest)
                    .SetParameter(
                        currentQuestCompleted
                            ? GAEvent.Action.Completed
                            : GAEvent.Action.Failed)
                    .SetSubject(GAEvent.Subject.Quest, currentQuest)
                    .SetParameter<GAEvent.Label>()
                    .SetSubject(GAEvent.Subject.MapName, Loading.PreviousScene)
                    .SetValue(ProfileInfo.Level));

            #endregion

        }

        //Выравниваем все че только можно
        if (aligners != null)
            for (int i = 0; i < aligners.Length; i++)
                if (aligners[i])
                    aligners[i].Align();

        btnGetDoubleRewardForAdsViewing.gameObject.SetActive(ProfileInfo.IsNeededToShowBtnGetDoubleRewardForAdsViewing);

        GUIPager.SetActivePage("AfterBattleScreen", false);
    }

    private void OnOkClick(tk2dUIItem btn)
    {
        if (playMoneySound)
            HangarController.Instance.PlaySound(HangarController.Instance.buyingSound);

        if (!SocialSettings.IsWebPlatform && !SocialSettings.IsLoggedIn && BattleStatisticsManager.OverallBattleStats["BattlesCount"] == GameData.LINK_SUGGESTION_BATTLES_COUNT)
            GUIPager.SetActivePage("LinkAccountPage");
        else
            GUIPager.SetActivePage("MainMenu");
    }

    public void BtnGetDoubleRewardForAdsViewingOnClick(tk2dUIItem btn)
    {
        RewardedVideoController.Instance.BtnAfterBattleScreenOnClick();
    }

    public void OnShareButtonPress()
    {
        StartCoroutine(Share());
    }

    private IEnumerator Share()
    {
        yield return new WaitForEndOfFrame();
        SocialSettings.GetSocialService().Post(Localizer.GetText("textBattleStatisticPost"), MiscTools.GetScreenshot());
    }
}