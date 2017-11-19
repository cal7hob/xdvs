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

public class TopPanelValues : AbstractClassForButtons, IFlag
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

    public JoystickManager JManager;
    private GameObject BtnProlongGame { get { return btnProlongGame == null ? sprProlongGame.gameObject : btnProlongGame; } }
    private tk2dSlicedSprite prolongBtnSlicedSprite;
    private bool isElementsMovedByBtnStatsAppearing = false;

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

        Dispatcher.Subscribe(EventId.MainTankAppeared, OnMainTankAppeared, 4);
        Dispatcher.Subscribe(EventId.ExperienceAcquired, SetEarnedExperience);
        Dispatcher.Subscribe(EventId.ProfileMoneyChange, OnProfileMoneyChange);
        Dispatcher.Subscribe(EventId.FlagSettingsChanged, ApplyAvatarOption);
        Dispatcher.Subscribe(EventId.BtnBackInBattleChangeVisibility, OnBtnBackInBattleChangeVisibility);

        prolongBtnSlicedSprite = AlternativeSpriteforDeadZone ?? (tk2dSlicedSprite) sprProlongGame;        
    }

    private void StartTimer()
    {
        if (timer != null && ProfileInfo.IsBattleTutorial) //Отключать показ таймера в туторе
            timer.gameObject.SetActive(false);
        else
            StartCoroutine(SettingTimer());
    }

    void OnDestroy()
    {
        Instance = null;
        Dispatcher.Unsubscribe(EventId.MainTankAppeared, OnMainTankAppeared);
        Dispatcher.Unsubscribe(EventId.ExperienceAcquired, SetEarnedExperience);
        Dispatcher.Unsubscribe(EventId.ProfileMoneyChange, OnProfileMoneyChange);
        Dispatcher.Unsubscribe(EventId.FlagSettingsChanged, ApplyAvatarOption);
        Dispatcher.Unsubscribe(EventId.BtnBackInBattleChangeVisibility, OnBtnBackInBattleChangeVisibility);
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

    private IEnumerator SettingTimer()
    {
        while (true)
        {
            if (BattleController.TimeRemaining != prevTimeRemaining)
            {
                Timer = CalcTimeRemaining();
            }

            prevTimeRemaining = BattleController.TimeRemaining;

            yield return null;
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
        StartTimer();
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
        timer.color = new Color(0.96f, 0.424f, 0.216f);
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
        if (JManager != null)
        {
            JManager.ReplaceButtons();
        }
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
            if (!GameData.IsGame(Game.WWT2))
            {
                color = lblProlongGamePrice.color;
                color.a = clampedAlpha;
                lblProlongGamePrice.color = color;
            }

            yield return null;
        }
        BtnProlongGame.SetActive(false);
        if (JManager != null)
        {
            JManager.ReplaceButtons();
        }
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

    private Vector3 ActualDimensions(tk2dSlicedSprite sprite, string minOrMax)
    {
        var dimensionX = sprite.dimensions.x;
        var dimensionY = sprite.dimensions.y;
        var centerX = sprite.GetBounds().center.x;
        var centerY = sprite.GetBounds().center.y;
        var halfDimensionX = dimensionX / 2;
        var halfDimensionY = dimensionY / 2;
        var xMax = centerX + halfDimensionX;
        var yMax = centerY + halfDimensionY;
        var xMin = centerX - halfDimensionX;
        var yMin = centerY - halfDimensionY;
        var max = new Vector3(xMax * sprite.scale.x, yMax * sprite.scale.y, 0);
        var min = new Vector3(xMin * sprite.scale.x, yMin * sprite.scale.y, 0);
        if (minOrMax.Equals("min"))
        {
            return min;
        }
        if (minOrMax.Equals("max"))
        {
            return max;
        }
        else
        {
            return Vector3.zero;
        }
    }
    public override Rect Coord()
    {
        var joyWorldTopRight = prolongBtnSlicedSprite.transform.TransformPoint(ActualDimensions(prolongBtnSlicedSprite, "max"));
        var joyScreenTopRight = BattleGUI.Instance.GuiCamera.WorldToScreenPoint(joyWorldTopRight);
        var joyWorldBottomLeft = prolongBtnSlicedSprite.transform.TransformPoint(ActualDimensions(prolongBtnSlicedSprite, "min"));
        var joyScreenBottomLeft = BattleGUI.Instance.GuiCamera.WorldToScreenPoint(joyWorldBottomLeft);
        var Area = new Rect
        {
            xMin = joyScreenBottomLeft.x,
            yMin = joyScreenBottomLeft.y,
            xMax = joyScreenTopRight.x,
            yMax = joyScreenTopRight.y,
        };
        if (sprProlongGame.gameObject.activeInHierarchy)
        {
            return Area;

        }
        else
        {
            return new Rect(0, 0, 0, 0);

        }

    }
}
