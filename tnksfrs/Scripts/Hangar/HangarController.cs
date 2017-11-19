using System;
using System.Linq;
using CodeStage.AntiCheat.ObscuredTypes;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Disconnect;
using UnityEngine.SceneManagement;
using UnityEngine.Serialization;
using XD;

public class HangarController : MonoBehaviour
{
    public enum ActionBoxType
    {
        None,
        Buy,
        Install,
        Installed,
        Deliver,
        Inaccessible,
        Rent,
        Renting,
        Rented,
		Sale,
        RentSale,
		FreeTank,
        ComingSoon,
    }

    public static event Action<double> OnTimerTick;

    public bool isWaitingForSaving;

    [FormerlySerializedAs("tankPanelBackground")]
    public MeshFilter bottomGuiPanel;

    public float lblUserLevelEuropianFontScale = 1;//в китайском языке цифры большие, приходится вручную скейлить (пока только в SpaceJet)
    public float lblUserLevelChineseFontScale = 1;
    public GameObject recieveFreeTankBox;
    public GameObject installBox;
    public GameObject installedBox;
    public GameObject inaccessibleBox;
    public GameObject comingSoonBox;
    public bool demoVersion;
    public GameObject btnBank;
    public GameObject hasDoubleExpTanksIndicator;
    public GameObject doubleExpWrapper;
    public GameObject btnVipShop;
    public GameObject btnBuyVip;
    public GameObject vipAccountShop;

    public AudioClip backSound;
    public AudioClip tankSelectSound;
    public AudioClip patternSelectSound;
    public AudioClip decalSelectSound;
    public AudioClip buyingSound;
    public AudioClip moduleInstallSound;
    public AudioClip toBattleSoundIronTanks;
    public AudioClip toBattleSoundTeamMode;
    public AudioClip tankInstallSound;

    public Color quitGameQuestionTextColor = Color.white;
    public Color quitGameQuestionGameColor = Color.white;

    public bool forceShadowPlaneShow;

    private static bool firstHangarEnter = true;

    private float lastTick;

    public static HangarController Instance { get; private set; }

    public static bool FirstEnter { get { return firstHangarEnter; } }

    public static bool DemoVersion { get { return Instance.demoVersion; } }

    public static bool ExitingGame { get; private set; }

    public bool IsInitialized { get; private set; }

    public Dictionary<int, float> ArmorMax { get; private set; }

    public Dictionary<int, float> DamageMax { get; private set; }

    public Dictionary<int, float> RocketDamageMax { get; private set; }

    public Dictionary<int, float> SpeedMax { get; private set; }

    public Dictionary<int, float> ROFMax { get; private set; }

    public Dictionary<int, float> IRCMROFMax { get; private set; }

    public Camera GuiCamera { get; private set; }

    /*    UNITY SECTION    */

    public GameObject prefabGUIConsumable;
    public void GUIConsumable()
    {
        GameObject.Instantiate(prefabGUIConsumable);
    }  

    void ProfileLoadedFromServer(EventId id, EventInfo info)
    {
        //RefuelByTime(GameData.CurrentTimeStamp - ProfileInfo.lastVisit);
        Debug.Log("profile loaded from server");

        ProfileInfo.HinduBugFix();

        ShowUserInfo();

        Debug.Log("Loaded from server!");
    }
    
    private void ScheduleLocalNotifications()
    {
#if !(UNITY_WP8 || UNITY_WP8_1 || UNITY_WSA || UNITY_WSA_8_0 || UNITY_WSA_8_1 || UNITY_WSA_10_0) && !UNITY_EDITOR
            if(IsInitialized && !ThirdPartyAdsManager.IsShowing)
            {
                Instance.StartCoroutine(PushNotifications.Instance.SettingLocalNotifications());
            }
#endif
    }

    void OnApplicationFocus(bool focused)
	{
#if UNITY_EDITOR
        if (Time.realtimeSinceStartup < 1)
        {
            //Debug.LogError("Ignoring OnApplicationPause " + focused + "Time.realtimeSinceStartup: " + Time.realtimeSinceStartup);
            return;
        }
#endif

	    if (focused)
	    {
            //Debug.LogError("App is focused.");
	    }
	    else
	    {
            //Debug.LogError("App is not in focus.");

            ScheduleLocalNotifications();
        }
    }

    void OnApplicationPause(bool paused)
    {
        if (!paused)
            return;

        ScheduleLocalNotifications();

        //Debug.LogError("App is paused.");
    }

    void OnDestroy()
    {
#if UNITY_EDITOR
		HelpTools.CleanSceneFromTextures();
#endif
        if (GameData.timeGettingError)
            return;

        GUIPager.OnPageChange -= OnPageChange;

        OnTimerTick = null;

        ProfileInfo.lastVisit = GameData.CurrentTimeStamp;

        firstHangarEnter = false;

		Dispatcher.Unsubscribe(EventId.ProfileInfoLoadedFromServer, ProfileLoadedFromServer);
        Dispatcher.Unsubscribe(EventId.MessageBoxChangeVisibility, OnMessageBoxChangeVisibility);
        Dispatcher.Unsubscribe(EventId.OnLanguageChange, OnLanguageChange);

        XD.StaticContainer.Connector.ClearPhotonMessageTargets();
        Instance = null;
        ObscuredPrefs.onAlterationDetected -= OnObscuredPrefsAlteration;
    }

    public static void HangarReenter()
    {
        firstHangarEnter = true;
    }

    /*    PUBLIC SECTION    */
    public static void CheckForNewDay(double timeStamp)
    {
      
	}

    //public static bool CheckPlaySecondDayInARaw()
    //{
    //    var timeDiff = MiscTools.TimestampToDate(GameData.CurrentTimeStamp) - MiscTools.TimestampToDate(ProfileInfo.nextDayServerTime);
    //    return timeDiff.TotalDays >= 0 && timeDiff.TotalDays <= 1;
    //}

    public void ShowUserInfo()
    {
           
    }

    public static DateTime UnixTimeStampToDateTime(double unixTimeStamp)
    {
	    // Unix timestamp is seconds past epoch.
	    DateTime dtDateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
	    dtDateTime = dtDateTime.AddSeconds( unixTimeStamp );

	    return dtDateTime;
	}

    public static double DateTimeToUnixTimeStamp(DateTime dateTime)
    {
        DateTime defaultTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
        TimeSpan difference = dateTime.ToUniversalTime() - defaultTime;

        return Math.Floor(difference.TotalSeconds);
    }

	public void SetActiveDoubleExpText(int currentVehicleId)
	{
    	doubleExpWrapper.SetActive(ProfileInfo.doubleExpVehicles.Contains(currentVehicleId));
	}

    public void RecalcMaxStats(ShopItemType itemType = ShopItemType.Vehicle | ShopItemType.Module)
    {
       
    }

    public void PlaySound(AudioClip clip)
    {
    }

    public void OnVehicleShopEnter(string buttonName)
    {
        GUIPager.SetActivePage("VehicleShopWindow");
        Dispatcher.Send(EventId.VoiceRequired, new EventInfo_I((int) VoiceEventKey.VehicleShopEnter));
    }

    private void OnArmoryEnter(string buttonName)
    {
        //HangarCameraController.Instance.FindCamLookPoints();
        GUIPager.SetActivePage("Armory");
        Dispatcher.Send(EventId.VoiceRequired, new EventInfo_I((int) VoiceEventKey.ModuleShopEnter));
    }

	public void OnBackClick(string buttonName)
    {
        if (GUIPager.ActivePage == "MainMenu" && !StaticType.SocialSettings.Instance<ISocialSettings>().IsWebPlatform && !isWaitingForSaving)
        {
			GUIPager.SetActivePage("QuitGame", false);

            MessageBox.Show(MessageBox.Type.Question,
                Localizer.GetText("QuitGameConfirm",
                    quitGameQuestionTextColor.To2DToolKitColorFormatString(),
                    quitGameQuestionGameColor.To2DToolKitColorFormatString(),
                    Application.productName,
                    quitGameQuestionTextColor.To2DToolKitColorFormatString()),
                QuitGame);

            PlaySound(backSound);

			return;
        }

        GUIPager.Back();

        PlaySound(backSound);
    }

    public void NavigateToVipShop(object desiredItem)
    {
        if (desiredItem == null || desiredItem is string)
        {
            PassEnterToVipShop();
            return;
        }

        string itemTypeAvailability = String.Empty;

        if (desiredItem is IUnitHangar)
            itemTypeAvailability = Localizer.GetText(
                GameData.IsGame(Game.SpaceJet) ? 
                "thisSpaceshipAvailable":
                GameData.IsGame(Game.BattleOfWarplanes) ?
                "thisPlaneAvailable" :
                "thisTankAvailable");

        if (desiredItem is Pattern)
            itemTypeAvailability = Localizer.GetText("thisPatternAvailable");

        if (desiredItem is Decal)
            itemTypeAvailability = Localizer.GetText("thisDecalAvailable");

        MessageBox.Show(
            _type:      MessageBox.Type.Question,
            _text:      string.Format("{0} {1}", itemTypeAvailability, Localizer.GetText("onlyForVip")),
            _callBack:  answer =>
                        {
                            if (answer == MessageBox.Answer.Yes)
                                PassEnterToVipShop();
                        });
    }

    public static void QuitGame(MessageBox.Answer answer = MessageBox.Answer.Yes)
    {
        if (answer == MessageBox.Answer.Yes)
        {
            XdevsSplashScreen.SetActiveWaitingIndicator(true);

            Action passingApplicationQuit = () =>
            {
                XdevsSplashScreen.SetActiveWaitingIndicator(false);
                if(PushNotifications.Pusher != null)
                {
                    PushNotifications.OnPushesScheduled += GameData.QuitGame;
                }
                else
                {
                    GameData.QuitGame();
                }

                Instance.StartCoroutine(PushNotifications.Instance.SettingLocalNotifications());
            };

            ThirdPartyAdsManager.Show(
                targetPlace:        AdsShowingMode.OnQuit,
                closingCallback:    passingApplicationQuit);
        }
        else
        {
            Application.CancelQuit();
            GUIPager.Back();
        }
    }

    private static void OnSettingsClick(string buttonName)
    {
        GUIPager.SetActivePage("Settings", true, true);
		Settings.SetFirstSettingsTab();
    }

    private void PassEnterToVipShop()
    {
        //GUIPager.ClearHistory();//Илья. 09.09.2016. Закомментил по задаче, чтоб после страницы випа возвращало в банк
        //GUIPager.SetActivePage("MainMenu");
        GUIPager.SetActivePage("VipAccountShop");

        // Set back bank page to gold (because vip tab is empty).
    }
    
    /*** BANK ***/
    /*public void GoToBank(Bank.Tab tab, bool addToHistory = true, bool voiceRequired = false)
    {
        if (!Bank.Instance.IsInitialized)
        {
#if !(UNITY_WEBPLAYER || UNITY_WEBGL)
            //
#endif
            Debug.LogError("!Bank.Instance.IsInitialized! return;");
            return;
        }

        GUIPager.SetActivePage("Bank", addToHistory, true, voiceRequired ? (int)VoiceEventKey.NotEnoughMoney : -1);
        
        bankCurrencyToggle.SelectedIndex = (int)tab;
    }*/

    private void OnBankClick(string buttonName)
    {
         //GoToBank(Bank.Tab.Gold);
    }

    //private void OnGameModeSwitch(tk2dUIToggleButtonGroup buttonGroup)
    //{
    //    int gameMode = 1;
    //    gameMode += buttonGroup.SelectedIndex;
    //    GameData.Mode = (GameData.GameMode)gameMode;
    //}

    private static void OnPatternShopClick(string buttonName)
    {
        GUIPager.SetActivePage("PatternShop");
        Dispatcher.Send(EventId.VoiceRequired, new EventInfo_I((int) VoiceEventKey.PatternShopEnter));
    }

    private static void OnDecalsShopClick(string buttonName)
    {
        GUIPager.SetActivePage("DecalShop");
        Dispatcher.Send(EventId.VoiceRequired, new EventInfo_I((int)VoiceEventKey.DecalShopEnter));
    }

    private static void OnBattleClick(string buttonName)
    {
        GUIPager.SetActivePage("MapSelection", true, true);
        Dispatcher.Send(EventId.VoiceRequired, new EventInfo_I((int)VoiceEventKey.MapSelection));
    }

    private static void OnBattleQuestsClick(string buttonName)
    {
        if (!ObscuredPrefs.GetBool("DidFirstBattlePlayed") && !Debug.isDebugBuild)
        {
            //var mapSelector = MapFramesCreator.MapSelectionFrames.FirstOrDefault();
            return;
        }

        if (QuestsInfo.IsAllQuestsCompleted)
        {
            GUIPager.SetActivePage("MapSelection", true, true);
            Dispatcher.Send(EventId.VoiceRequired, new EventInfo_I((int)VoiceEventKey.MapSelection));
        }
        else
        {
            GUIPager.SetActivePage("Quests", true,!GameData.IsGame(Game.Armada2));
            //QuestsUI.Instance.MarkerizeQuestList();
            Dispatcher.Send(EventId.VoiceRequired, new EventInfo_I((int)VoiceEventKey.CurrentQuests));
        }
    }

    private void HangarTimerTick()
    {
        double tickDeltaTime = GameData.instance.GetCorrectedTime () - lastTick;

        RefuelByTime(tickDeltaTime);

        lastTick = GameData.instance.GetCorrectedTime ();

        Dispatcher.Send(EventId.HangarTimerTick, new EventInfo_I((int)lastTick));

        if (OnTimerTick != null)
            OnTimerTick(GameData.CurrentTimeStamp);
    }

    private void OnObscuredPrefsAlteration()
    {
		MessageBox.Show(MessageBox.Type.Info, "Data modification detected");

        GameData.QuitGame ();

        ShowUserInfo();       
    }

    private void OnPageChange(string from, string to)
    {
		if (to == "MainMenu")
        {
            GUIPager.PagesHistory.Clear();
            SetActiveDoubleExpText(ProfileInfo.currentVehicle);
        }
            
    }

    public void RefuelByTime(double timeInterval)
    {
    }

    private void OnMessageBoxChangeVisibility(EventId id, EventInfo info)
    {
        if (GUIPager.ActivePage == "Chat" && ((EventInfo_B)info).bool1)//Чтобы часть месседжбокса не перекрывалась маской для обрезки сообщений чата
            GUIPager.SetActivePage("MainMenu"); 
    }

    private void OnLanguageChange(EventId id, EventInfo info)
    {
        float scale = Localizer.Language == Localizer.LocalizationLanguage.Chinese ? lblUserLevelChineseFontScale : lblUserLevelEuropianFontScale;//захардкодил китайский скейл, потому что китайский шрифт один на все проекты
    }
}
