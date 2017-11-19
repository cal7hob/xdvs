#if !(UNITY_STANDALONE_OSX || UNITY_WEBPLAYER || UNITY_WEBGL)
    #define TOUCH_SCREEN
#endif

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Assets.Scripts.GUI.Layouts;
using CodeStage.AntiCheat.ObscuredTypes;
using UnityEngine;
using Hashtable = ExitGames.Client.Photon.Hashtable;

[System.Serializable]
public class SettingsPages
{
	public GameObject SettingsPage;
}

public enum AvatarOption
{
	showEverything,
	showOnlyFlags,
    showNothing,
    showOnlyAvatars
}

public enum ControlOption
{
    joystick,
    gyroscope
}

public enum GraphicsLevel
{
    lowQuality,
    mediumQuality,
    normalQuality,
    highQuality,
    ultraQuality
}

public class Settings : MonoBehaviour
{
	public const float DEFAULT_MUSIC_VOLUME = 0.2f;
	public const float DEFAULT_SOUND_VOLUME = 0.5f;
    public const float DEFAULT_TURRET_ROTATION_SENSITIVITY = 0.5f;
    public const float DEFAULT_CAM_TO_TANK_DISTANCE = 1f;
    public const int MAX_NAME_LENGTH = 14;

    public GameObject settingsBtnBattle;
    public GameObject wrapper;

    [Header("Main:")]
    public GameObject lblLanguageNext;
    public GameObject lblLanguagePrev;
    public GameObject btnAccountManagement;
    public UniAlignerBase bottomButtonsAligner;

    [Header("Graphics:")]
    public VerticalLayout verticalLayout;

	public List<SettingsPages> settingsPages = new List<SettingsPages>();

    private const float SOUND_VOLUME_SCROLLER_MULTIPLIER = 1.0f;

    private static float musicVolume;
	private static float soundVolume;
    private static int graphicsLevel;
    private static int languageIndex;
    private static MessageBox.Answer userAnswer;
    private static GameObject levelObject;
    private string levelObjectName;
    private static bool isCalibrating;
    private static IEnumerator changingObjectMaterialRoutine;

    public static Settings Instance { get; private set; }
	public static string PlayerName { get; set; }
    public static bool TransferableSettingsChanged { get; set; }
    public static Vector3 InitialAcceleration { get; private set; }
    public static GraphicsLevel GraphicsLevel { get { return (GraphicsLevel) graphicsLevel; } }

    void Awake()
	{
		Instance = this;

        Dispatcher.Subscribe(EventId.AfterHangarInit, Init, 4);
        Dispatcher.Subscribe(EventId.MainTankAppeared, Init);
	    Dispatcher.Subscribe(EventId.SettingsSubmited, OnSettingsSubmited);
	    Dispatcher.Subscribe(EventId.SettingsSubmited, CheckIfGyroControl);
        Dispatcher.Subscribe(EventId.MainTankAppeared, CheckIfGyroControl);
        Dispatcher.Subscribe(EventId.OnLanguageChange, OnLanguageChange);
	}

    void OnDestroy()
    {
        Dispatcher.Unsubscribe(EventId.AfterHangarInit, Init);
        Dispatcher.Unsubscribe(EventId.MainTankAppeared, Init);
        Dispatcher.Unsubscribe(EventId.SettingsSubmited, OnSettingsSubmited);
        Dispatcher.Unsubscribe(EventId.SettingsSubmited, CheckIfGyroControl);
        Dispatcher.Unsubscribe(EventId.MainTankAppeared, CheckIfGyroControl);
        Dispatcher.Unsubscribe(EventId.OnLanguageChange, OnLanguageChange);

        Instance = null;
    }

	void Start()
	{
        #if LANG_CHINESE_ONLY
        if (lblLanguageNext)
        {
            lblLanguageNext.SetActive(false);
        }
        if (lblLanguagePrev)
        {
            lblLanguagePrev.SetActive(false);
        }
        #endif

        #if !TOUCH_SCREEN

        #endif
	}

	public void Init(EventId id, EventInfo info)
	{
        musicVolume = PlayerPrefs.HasKey ("MusicVolume") ? PlayerPrefs.GetFloat ("MusicVolume") : DEFAULT_MUSIC_VOLUME;
        soundVolume = PlayerPrefs.HasKey ("SoundVolume") ? PlayerPrefs.GetFloat ("SoundVolume") : DEFAULT_SOUND_VOLUME;

        //Принудилово на джойстик-управление, т.к. в билде "случайно" были включены радиобаттоны для переключения джойстик / гироскоп
        if (GameData.IsGame(Game.Armada2))
            ProfileInfo.controlOption = (int)ControlOption.joystick;

	    if (PlayerPrefs.HasKey("GraphicsLevel"))
	    {
            graphicsLevel = PlayerPrefs.GetInt("GraphicsLevel");
	    }
	    else
	    {
#if UNITY_STANDALONE || UNITY_EDITOR || (UNITY_WSA && UNITY_WSA_8_1)
            graphicsLevel = (int) GraphicsLevel.highQuality;
#elif UNITY_WEBGL
            graphicsLevel = (int) GraphicsLevel.mediumQuality;
#else
            graphicsLevel = SystemInfo.systemMemorySize < 700 ? (int) GraphicsLevel.lowQuality : (int) GraphicsLevel.normalQuality;
#endif
        }

        Debug.LogError("Graphics: " + graphicsLevel);
	    StartCoroutine(SetQuality());

        //UpdateChangeNickPrice();

        CheckVoiceDisabling();

        if (!GameData.countryFlagsIsOn)
	    {

            ProfileInfo.avatarOption = (int) AvatarOption.showOnlyAvatars;
	    }

	    if (ProfileInfo.Level < GameData.accountManagementMinLevel && btnAccountManagement != null)
	    {
	        btnAccountManagement.SetActive(false);
	    }

#if UNITY_STANDALONE_OSX || UNITY_WEBGL || UNITY_WEBPLAYER

#endif
    }

    public static IEnumerator SetQuality(int gLevel = -1)
    {
        if (gLevel != -1)
        {
            graphicsLevel = gLevel;
        }

        Debug.Log("SetQuality - Start: " + graphicsLevel);
        if (changingObjectMaterialRoutine != null)
        {
            QualityManager.Instance.StopCoroutine(changingObjectMaterialRoutine);
        }

        string levelObjectName;

        if (!XD.StaticContainer.SceneManager.InBattle)
        {
            levelObjectName = string.Format("Hangar_{0}_{1}", GameData.CurrentGame,
                ProfileInfo.IsPlayerVip ? "Premium" : "Standart");
            
            //changingObjectMaterialRoutine = QualityManager.Instance.ChangeObjectMaterials(Shop.VehicleInView.HangarVehicle.gameObject);
            //yield return QualityManager.Instance.StartCoroutine(changingObjectMaterialRoutine); закомменчено, чтобы ангарная техника оставалась со своим матом
        }
        else
        {
            levelObjectName = string.Format("Level_{0}", XD.StaticContainer.GameManager.CurrentMap.ToString().Replace("scnb_", ""));            
        }

        levelObject = GameObject.Find(levelObjectName);

        //changingObjectMaterialRoutine = QualityManager.Instance.ChangeObjectMaterials(levelObject);

        yield return QualityManager.Instance.StartCoroutine(changingObjectMaterialRoutine);
        Debug.Log("SetQuality - End: " + graphicsLevel);
        QualityManager.SetQualityLevel(graphicsLevel);
        Debug.Log("SetQuality - SetQualityLevel!");

        Dispatcher.Send(EventId.QualitySettingsChanged, new EventInfo_SimpleEvent());
        Debug.Log("SetQuality - Dispatcher.Send!");

    }

    /*public void UpdateChangeNickPrice()
    {
        if (nickChangePrice)
            nickChangePrice.gameObject.SetActive(false);
        if (nickChangeCurrencySprite)
            nickChangeCurrencySprite.gameObject.SetActive(false);
        if (ProfileInfo.nickEntered && !XD.StaticContainer.SceneManager.InBattle && GameData.changeNickPrice != null)
        {
            if (nickChangePrice)
            {
                nickChangePrice.gameObject.SetActive(true);
                nickChangePrice.SetText(GameData.changeNickPrice.LocalizedValue);
                GameData.changeNickPrice.SetMoneySpecificColorIfCan(nickChangePrice.TextMesh);
            }
            if (nickChangeCurrencySprite)
            {
                nickChangeCurrencySprite.SetSprite(GameData.changeNickPrice.currency == ProfileInfo.PriceCurrency.Gold ? "gold" : "silver");
                nickChangeCurrencySprite.gameObject.SetActive(true);
            }
                
        }
    }*/

    public static void RefreshBodykit(GameObject obj)
    {
        if (obj == null)
        {
            return;
        }

        var bodykitController = obj.GetComponent<BodykitController>();

        if (bodykitController == null)
            return;

        bodykitController.RefreshDefaultMaterials();
        bodykitController.RefreshCurrentMaterials();
    }

	public static float MusicVolume
	{
		get { return musicVolume; }
	}

	public static float SoundVolume
	{
		get
        {
            if (Settings.Instance != null)
                return soundVolume;
            else if(BattleSettings.Instance != null)
                return BattleSettings.Instance.SoundVolume;
            return 0.5f;
        }
	}


    /*public void Submit(tk2dUIItem uiItem)
    {
        if (isCalibrating)
        {
            ProfileInfo.initialAcceleration = Input.acceleration;
            InitialAcceleration = ProfileInfo.initialAcceleration;
            isCalibrating = false;
            //CheckIfInitialAccelerationIsSet();
            return;
        }

        SaveParams ();

        if (ProfileInfo.PlayerName != PlayerName) {
            SetNickName (ApplySettings);
        }
        else {
            ApplySettings ();
        }
    }*/

    public void SetNickName (Action successCallback)
    {
        Http.Manager.ChangeNickName (PlayerName,
            // Success callback
            (result) => {
                if (null != successCallback) {
                    successCallback ();
                    TransferableSettingsChanged = true;
                }
            },
            // Fail callback
            (result) => {
                Debug.Log ("Change nick error: " + result.ServerError.ToString ());
                switch (result.ServerError) {

                    case Http.Error.InternalInvalidNickname:
                        foreach (var settingsPage in settingsPages) {
                           
                        }
                        break;

                    case Http.Error.ShopNotEnoughMoney:
					

#region Google Analytics: nickname changing failure "not enough money"

                        GoogleAnalyticsWrapper.LogEvent(
                            new CustomEventHitBuilder()
                                .SetParameter(GAEvent.Category.NicknameChanging)
                                .SetParameter(GAEvent.Action.NotEnoughMoney)
                                .SetParameter<GAEvent.Label>()
                                .SetSubject(GAEvent.Subject.PlayerLevel, ProfileInfo.Level)
                                .SetValue(ProfileInfo.Gold));

#endregion

                        break;
                }
            }
        );
    }
    #region Disabled btnSettings in Battle
    //public static void ShowSettingsBtn()
    //{
    //    Instance.settingsBtnBattle.SetActive(true);
    //}

    //public static void HideSettingsBtn()
    //{
    //    Instance.settingsBtnBattle.SetActive(false);
    //}
    #endregion

    /*public void PageSelectionClick(tk2dUIItem item)
	{
		foreach (var page in settingsPages)
		{
			var currentPage = page.SettingsPage;
            var showHideScript = currentPage.GetComponent<ShowHideGUIPage>();

            if (page.SettingsPageBtn == item)
			{
                if (showHideScript) showHideScript.MoveToDefaultPositionAndShow();
                else currentPage.SetActive(true);

                if (currentToggleBtn != null)
                {
                    currentToggleBtn.IsOn = true;
                }

            }
			else
			{
                if (showHideScript) showHideScript.Hide();
                else currentPage.SetActive(false);
            }
		}

        if (bottomButtonsAligner)
            bottomButtonsAligner.Align();
    }*/

    public static void ChangePlayerSettings(string settings, int id)
    {
        if (String.IsNullOrEmpty(settings)) return;

        var ei = settings.Split(',');
        var playerName = ei[0];
        var hideHisFlag = ei[1].Trim() == "True";

        XD.StaticContainer.BattleController.Units[id].data.hideMyFlag = hideHisFlag;
        XD.StaticContainer.BattleController.Units[id].data.playerName = playerName;
        XD.StaticContainer.BattleController.Units[id].Statistics.Nick = playerName;
        /*var indicator = TankIndicators.GetIndicator(id);

        if (indicator == null) return;
        indicator.playerName.text = playerName;*/
        Dispatcher.Send(EventId.FlagSettingsChanged, null);
    }

    public static string SerializedTransferableSettings
    {
        get { return ProfileInfo.PlayerName + ", " + ProfileInfo.isHideMyFlag; }
    }

    public static void SetFirstSettingsTab()
    {
    }

    void Update()
    {
        /*if (!!XD.StaticContainer.SceneManager.InBattle && XDevs.Input.GetButtonDown("Back") && wrapper.activeSelf)
            OnBtnSettingsInBattleClick(null);*/
    }

    private void SaveParams()
    {
        Debug.LogError("SaveParams: " + graphicsLevel);
        ProfileInfo.languageIndex = languageIndex;
        ProfileInfo.initialAcceleration = InitialAcceleration;

        PlayerPrefs.SetFloat ("MusicVolume", musicVolume);
        PlayerPrefs.SetFloat ("SoundVolume", soundVolume);
        PlayerPrefs.SetInt ("GraphicsLevel", graphicsLevel);
        PlayerPrefs.Save ();

        if(HangarController.Instance != null)
            HangarController.Instance.ShowUserInfo ();
    }

    private void ApplySettings()
    {
#region Google Analytics: nickname changing success or window closing

        GoogleAnalyticsWrapper.LogEvent(
            new CustomEventHitBuilder()
                .SetParameter(GAEvent.Category.NicknameChanging)
                .SetParameter(
                    ProfileInfo.PlayerName != PlayerName
                    ? GAEvent.Action.Succeed
                    : GAEvent.Action.ClosedWindow)
                .SetParameter<GAEvent.Label>()
                .SetSubject(GAEvent.Subject.PlayerLevel, ProfileInfo.Level)
                .SetValue(ProfileInfo.Gold));

#endregion

        StartCoroutine(SetQuality());
        ProfileInfo.SaveToServer();
        Dispatcher.Send(EventId.SettingsSubmited, null);
    }

    /*public void OnBtnSettingsInBattleClick(tk2dUIItem uiItem)
    {
        var isSettingsOnScreen = wrapper.activeSelf;
        wrapper.SetActive(!isSettingsOnScreen);
        //StatTable.instance.gameObject.SetActive(!StatTable.instance.gameObject.activeSelf);
        SetFirstSettingsTab();
    }*/

    private static void OnSettingsSubmited(EventId id, EventInfo info)
    {
        if (!XD.StaticContainer.SceneManager.InBattle)
        {
        }
        else
        {
            Instance.wrapper.SetActive(false);

            //StatTable.instance.gameObject.SetActive(StatTable.State != XD.WindowShowCause.UnitSelection);

            if (XD.StaticContainer.BattleController.CurrentUnit != null)
            {
                foreach (var vehicleController in XD.StaticContainer.BattleController.Units)
                    vehicleController.Value.BodykitController.SetShadowPlane();
            }

            //TopPanelValues.NickName = ProfileInfo.PlayerName;
            Dispatcher.Send(EventId.FlagSettingsChanged, null);
           // TopPanelValues.SetEarnedGold(ProfileInfo.Gold);
           // TopPanelValues.SetEarnedSilver(ProfileInfo.Silver);

            if(!TransferableSettingsChanged) return;

            var properties = new Hashtable(){ {"st", SerializedTransferableSettings} };
            PhotonNetwork.player.SetCustomProperties(properties);

            TransferableSettingsChanged = false;

            CheckIfGyroControl();
        }
    }

    private void OnPlayerIdClicked ()
    {
#if UNITY_EDITOR
        Debug.Log ("Player ID label clicked");
        Http.Manager.Instance ().OpenPlayerProfile ();
#endif
    }

    private static void CheckIfGyroControl(EventId id = 0, EventInfo info = null)
    {
        MiscTools.SetScreenAutoOrientation(ProfileInfo.ControlOption != ControlOption.gyroscope);
    }

    private void OnLanguageChange(EventId id, EventInfo ei)
    {
        CheckVoiceDisabling();
    }

    private void CheckVoiceDisabling()
    {
    }

    //private void CheckIfInitialAccelerationIsSet()
    //{
    //    if (InitialAcceleration == Vector3.zero)
    //    {
    //        StartCoroutine(calibrateBtnFadingRoutine);
    //    }
    //    else
    //    {
    //        StopCoroutine(calibrateBtnFadingRoutine);
    //        var color = calibrateBtnSpr.color;
    //        color.a = 1;
    //        calibrateBtnSpr.color = color;
    //    }
    //}
}
