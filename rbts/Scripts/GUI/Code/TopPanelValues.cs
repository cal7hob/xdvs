using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[Serializable]
public class GameObjectParams
{
    public GameObject gameObject;
    public Vector3 position;

    public void SetLocalPosition()
    {
        if (gameObject)
            gameObject.transform.localPosition = position;
    }
}

public class TopPanelValues : MonoBehaviour, IFlag
{
    public tk2dTextMesh earnedExperience;
    public tk2dTextMesh earnedGold;
    public tk2dTextMesh earnedSilver;
    public tk2dTextMesh timer;
    public GameObject goldRushSection;
    public tk2dTextMesh goldRushAward;
    public tk2dTextMesh goldRushTimer;
    public tk2dUIItem btnOpenStatisticks;//Only for iOS 
    public GameObject btnProlongGame;
    public tk2dBaseSprite sprProlongGame;
    public tk2dSlicedSprite AlternativeSpriteforDeadZone;
    public float prolongGameAnimation_minAlpha = 0.3f;
    public float prolongGameAnimation_maxAlpha = 0.7f;
    public tk2dTextMesh lblProlongGamePrompt;
    public tk2dTextMesh lblProlongGamePrice;
    public float prolongButtonBlinkTime;
    public tk2dSlicedSprite countryFlag;
    public tk2dTextMesh nickName;
    public bool nickPosFrozen;
    [SerializeField]
    List<GameObjectParams> objectsToMoveWhenBtnStatAppears;//на iOS кнопка btnOpenStatistics отображается всегда, поэтому нужно двигать опыт и никнейм чтобы были справа от этой кнопки.

    private GameObject BtnProlongGame { get { return btnProlongGame == null ? sprProlongGame.gameObject : btnProlongGame; } }
    private tk2dSlicedSprite prolongBtnSlicedSprite;
#if UNITY_IOS
    private bool isElementsMovedByBtnStatsAppearing = false;
#endif

    public static TopPanelValues Instance { get; set; }
    public static bool CriticalTime
    {
        get { return Instance.criticalTime; }
    }

    private static Vector3 defNickLocPosition;

    private static int prevTimeRemaining;

    private static int minutes;
    private static int seconds;
    private static Color normTimerColor;

    private int earnedExperienceInt;
    private bool criticalTime;

    private bool isLoading = false;

    void Awake()
    {
        nickName.maxChars = Settings.MAX_NAME_LENGTH;
        BtnProlongGame.SetActive(false);
        Instance = this;
        normTimerColor = timer.color;
        defNickLocPosition = nickName.transform.localPosition;
        if (timer != null && ProfileInfo.IsBattleTutorial)  //Отключать показ таймера в туторе
            timer.gameObject.SetActive(false);

        Messenger.Subscribe(EventId.MainTankAppeared, OnMainTankAppeared, 4);
        Messenger.Subscribe(EventId.ExperienceAcquired, SetEarnedExperience);
        Messenger.Subscribe(EventId.ProfileMoneyChange, OnProfileMoneyChange);
        Messenger.Subscribe(EventId.FlagSettingsChanged, ApplyAvatarOption);
        Messenger.Subscribe(EventId.BtnBackInBattleChangeVisibility, OnBtnBackInBattleChangeVisibility);

        prolongBtnSlicedSprite = AlternativeSpriteforDeadZone != null ? AlternativeSpriteforDeadZone : (tk2dSlicedSprite) sprProlongGame;
             
    }

    void OnDestroy()
    {
        Instance = null;
        Messenger.Unsubscribe(EventId.MainTankAppeared, OnMainTankAppeared);
        Messenger.Unsubscribe(EventId.ExperienceAcquired, SetEarnedExperience);
        Messenger.Unsubscribe(EventId.ProfileMoneyChange, OnProfileMoneyChange);
        Messenger.Unsubscribe(EventId.FlagSettingsChanged, ApplyAvatarOption);
        Messenger.Unsubscribe(EventId.BtnBackInBattleChangeVisibility, OnBtnBackInBattleChangeVisibility);
        criticalTime = false;
    }

    public void SetEarnedExperience(EventId id, EventInfo info)
    {
        earnedExperienceInt += ((EventInfo_I)info).int1;
        earnedExperience.text = earnedExperienceInt.ToString("N0", GameData.instance.cultureInfo.NumberFormat);
    }

    public void OnBtnBackInBattleChangeVisibility(EventId id, EventInfo info)
    {
#if UNITY_IOS
        if(((EventInfo_B)info).bool1 && !isElementsMovedByBtnStatsAppearing)
        {
            if (objectsToMoveWhenBtnStatAppears != null)
                for (int i = 0; i < objectsToMoveWhenBtnStatAppears.Count; i++)
                    objectsToMoveWhenBtnStatAppears[i].SetLocalPosition();
            isElementsMovedByBtnStatsAppearing = true;
        }
#endif
    }

    public static void ShowCriticalTime(bool showCritTime, int displayedPrice = 0)
    {
        Instance.lblProlongGamePrice.text = displayedPrice.ToString();
        Instance.lblProlongGamePrompt.text = Localizer.GetText("lblProlongGamePrompt", (int)(GameData.ProlongTimeAddition / 60), Localizer.GetText("TimeMinutes"));
        if (showCritTime)
        {
            if (Instance.criticalTime)
                return;

            Instance.criticalTime = true;
            Instance.StartCoroutine(Instance.ShowingCriticalTime());
            Instance.StartCoroutine(Instance.ShowingCriticalTimeButton());
        }
        else
        {
            Instance.timer.GetComponent<Renderer>().enabled = true;
            Instance.timer.color = normTimerColor;
            Instance.criticalTime = false;
        }
    }

    public static void SetGoldRushAward(int award)
    {
        if (Instance && Instance.goldRushAward)
            Instance.goldRushAward.text = award.ToString();
    }

    public static void SwitchGoldRush(bool enable)
    {
        if (Instance && Instance.goldRushSection)
            Instance.goldRushSection.SetActive(enable);
    }

    public static void SetEarnedGold(int gold)
    {
        Instance.earnedGold.text = gold.ToString("N0", GameData.instance.cultureInfo.NumberFormat);
    }

    public static void SetEarnedSilver(int silver)
    {
        Instance.earnedSilver.text = silver.ToString("N0", GameData.instance.cultureInfo.NumberFormat);
    }

    public static string NickName
    {
        get { return Instance.nickName.text; }
        set { Instance.nickName.text = value; }
    }

    public static string Timer
    {
        get { return Instance.timer.text; }
        set
        {
            Instance.timer.text = value;
            if (Instance.goldRushTimer)
                Instance.goldRushTimer.text = value;
        }
    }

    void Update()
    {
        if (!ProfileInfo.IsBattleTutorial)
        {
            if (BattleController.TimeRemaining != prevTimeRemaining)
            {
                Timer = CalcTimeRemaining();
            }

            prevTimeRemaining = BattleController.TimeRemaining;
        }
    }

    public void ApplyAvatarOption(EventId id = 0, EventInfo info = null)
    {
        if (ProfileInfo.isHideMyFlag || ProfileInfo.AvatarOption == AvatarOption.showOnlyAvatars)
        {
            if (!nickPosFrozen)
            {
                var nickPos = nickName.transform.position;
                nickPos.x = countryFlag.GetComponent<Renderer>().bounds.min.x;
                nickName.transform.position = nickPos;
            }

            countryFlag.gameObject.SetActive(false);
        }
        else
        {
            if (!nickPosFrozen)
            {
                nickName.transform.localPosition = defNickLocPosition;
            }

            countryFlag.gameObject.SetActive(true);
            countryFlag.SetSprite(ProfileInfo.FlagSprite);
        }
    }

    private void OnMainTankAppeared(EventId id, EventInfo info)
    {
        ApplyAvatarOption();
        SetGoldRushAward(GoldRush.TotalStake);
        SwitchGoldRush(GoldRush.Leader == BattleController.MyPlayerId);
    }

    private static string CalcTimeRemaining()
    {
        if (BattleController.TimeRemaining >= 0)
        {
            minutes = BattleController.TimeRemaining / 60;
            seconds = BattleController.TimeRemaining - minutes * 60;
        }

        return string.Format("{0:00}:{1:00}", minutes, seconds);
    }

    private IEnumerator ShowingCriticalTime()
    {
        timer.color = GameData.IsGame(Game.IronTanks) ? new Color(0.776f, 0.004f, 0.004f) : new Color(0.96f, 0.424f, 0.216f);
        while (Instance.criticalTime)
        {
            timer.GetComponent<Renderer>().enabled = !timer.GetComponent<Renderer>().enabled;
            yield return new WaitForSeconds(0.4f);
        }
    }

    private IEnumerator ShowingCriticalTimeButton()
    {
        BtnProlongGame.SetActive(true);
        sprProlongGame.gameObject.SetActive(true);
        timer.color = Color.red;
        float alpha = prolongGameAnimation_minAlpha;
        float clampedAlpha;
        Color color;
        while (Instance.criticalTime)
        {
            alpha += Time.deltaTime / prolongButtonBlinkTime;
            clampedAlpha = prolongGameAnimation_minAlpha + Mathf.PingPong(alpha, prolongGameAnimation_maxAlpha);
            color = sprProlongGame.color;
            color.a = clampedAlpha;
            sprProlongGame.color = color;
            if (!GameData.IsGame(Game.BattleOfHelicopters | Game.Armada))
            {
                color = lblProlongGamePrice.color;
                color.a = clampedAlpha;
                lblProlongGamePrice.color = color;
            }

            yield return null;
        }
        BtnProlongGame.SetActive(false);
    }

    private void OnProlongGameClick(tk2dUIItem item)
    {
        if (isLoading)
        {
            return;
        }

        isLoading = true;
        Http.Manager.BattleServer.BuyGameProlongation(BattleController.Instance.CurrentProlongPrice,
            delegate (Http.Response result)
            {
                isLoading = false;

                BattleController.Instance.ProlongGameForMoney();

#region Google Analytics: game prolongation bought

                GoogleAnalyticsWrapper.LogEvent(
                    new CustomEventHitBuilder()
                        .SetParameter(GAEvent.Category.GameProlongationBuying)
                        .SetParameter<GAEvent.Action>()
                        .SetSubject(GAEvent.Subject.MapName, GameManager.CurrentMap)
                        .SetParameter<GAEvent.Label>()
                        .SetSubject(GAEvent.Subject.VehicleID, ProfileInfo.currentVehicle)
                        .SetValue(ProfileInfo.Level));

#endregion
            },
            delegate (Http.Response result)
            {
                isLoading = false;
            }
        );
    }

    private void OnProfileMoneyChange(EventId id, EventInfo ei)
    {
        EventInfo_II info = (EventInfo_II)ei;
        SetEarnedGold(info.int1);
        SetEarnedSilver(info.int2);
    }
}
