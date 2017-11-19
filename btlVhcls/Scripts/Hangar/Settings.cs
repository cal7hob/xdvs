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
    public tk2dUIItem SettingsPageBtn;
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
    joystick    = 0,
    gyroscope   = 1
}

public enum AutoAimingType
{
    WithoutAutoAiming = 0,
    DefaultAutoAiming = 1,
    //FullAutoAiming,
}

public enum GraphicsLevel
{
    lowQuality,
    mediumQuality,
    normalQuality,
    highQuality,
    ultraQuality
}

public enum MaterialQualityLevel
{
    mobile_default,
    mobile_max,
    pc_max
}

public class Settings : HangarPage
{
    public enum Tab
    {
        Main,
        Configuration,
        Notifications,
        Graphics,
        Hyroscope,
    }

    public const float DEFAULT_MUSIC_VOLUME = 0.2f;
    public const float DEFAULT_SOUND_VOLUME = 0.5f;
    public const float DEFAULT_TURRET_ROTATION_SENSITIVITY = 0.5f;
    public const int   DEFAULT_TURRET_ROTATION_INDICATOR_ACTIVITY = 1;
    public const float DEFAULT_VEHICLE_CAMERA_DISTANCE = 0.5f;
    public const float DEFAULT_CAM_TO_TANK_DISTANCE = 1f;
    public const int   DEFAULT_SWAP_CONTROLS_VALUE = 0;

    public const FloatSpeedXYJoystick.TypeJoystick DEFAULT_TURRET_CONTROL_TYPE = FloatSpeedXYJoystick.TypeJoystick.floatingX;
    public const int MAX_NAME_LENGTH = 14;

    [SerializeField] private tk2dUIToggleButtonGroup tabs;
    [SerializeField] private GameObject[] pages;//порядок как в енаме Tab
    [SerializeField] private GameObject btnOpenLinkAccountPage;
    [SerializeField] private GameObject[] objectsOnlyForHandheldDevices;//for hiding hiro tab button

    [Header("Main:")]
    public tk2dTextMesh lblLanguage;
    public GameObject lblLanguageNext;
    public GameObject lblLanguagePrev;
    public tk2dTextMesh lblPlayerId;
    public tk2dUIScrollbar musicVolumeScrollbar;
    public tk2dUIScrollbar soundVolumeScrollbar;
    public tk2dUITextInput nickTk2dUiTextInput;
    public PriceRenderer nickChangePrice;
    public tk2dUIToggleButtonGroup avatarAndFlagBtnGroup;
    public GameObject btnAccountManagement;
    
    [Header("Configuration:")]
    public tk2dUIToggleControl invert;
    public tk2dUIToggleControl sliderControl;
    public tk2dUIToggleControl fireOnDoubleTap;
    public tk2dUIToggleControl hideMyFlag;
    public tk2dUIToggleControl voiceDisabled;
    public tk2dUIToggleControl reverseControls; //Fire button to the left. Управление для левшей.
    public tk2dUIToggleControl activatedTurretCenterButton;
    public tk2dUIToggleButtonGroup controlOptionBtnGroup;
    public tk2dUIItem calibrateBtn;

    [Header("Notifications:")]
    public tk2dUIToggleControl pushForDailyBonus;
    public tk2dUIToggleControl pushForUpgrade;
    public tk2dUIToggleControl pushForFuel;

    [Header("Graphics:")]
    public tk2dUIToggleButtonGroup graphicsOptionsBtnGroup;
    public VerticalLayout verticalLayout;


    private const float SOUND_VOLUME_SCROLLER_MULTIPLIER = 1.0f;

    private static float soundVolume;
    private static int graphicsLevel;
    private static List<Localizer.LocalizationLanguage> languagesList;
    private static int languageIndex;
    private static GameObject levelObject;
    private string levelObjectName;
    private static bool isCalibrating;
    private static IEnumerator changingObjectMaterialRoutine;

    public static Settings Instance { get; private set; }
    public static string PlayerName { get; set; }
    public static bool TransferableSettingsChanged { get; set; }
    public static Vector3 InitialAcceleration { get; private set; }
    public static GraphicsLevel GraphicsLevel { get { return (GraphicsLevel)graphicsLevel; } }

    protected override void Create()
    {
        base.Create();
        Instance = this;

        Dispatcher.Subscribe(EventId.SettingsSubmited, OnSettingsSubmited);
        Dispatcher.Subscribe(EventId.OnLanguageChange, OnLanguageChange);

        Transform btnChooseServerReset = transform.FindInHierarchy("btnChooseServer");
        if (btnChooseServerReset != null)
        {
            btnChooseServerReset.gameObject.SetActive(Http.Manager.Instance().GetAvailableServers().Count > 1);
        }

        nickTk2dUiTextInput.OnTextChange += OnNickChanged;
        musicVolumeScrollbar.OnScroll += SetMusicVolume;
        soundVolumeScrollbar.OnScroll += SetSoundVolume;

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
        if (sliderControl)
            sliderControl.gameObject.SetActive(false);
#endif

#if UNITY_WEBPLAYER || UNITY_WEBGL
        btnOpenLinkAccountPage.SetActive(false);
#endif
    }

    protected override void Destroy()
    {
        Dispatcher.Unsubscribe(EventId.SettingsSubmited, OnSettingsSubmited);
        Dispatcher.Unsubscribe(EventId.OnLanguageChange, OnLanguageChange);
        musicVolumeScrollbar.OnScroll -= SetMusicVolume;
        soundVolumeScrollbar.OnScroll -= SetSoundVolume;
        Instance = null;
        base.Destroy();
    }

    protected override void Init()
    {
        base.Init();

        MiscTools.SetObjectsActivity(GameData.DeviceHasHyroscope, objectsOnlyForHandheldDevices);

        if (GameData.DeviceHasHyroscope && controlOptionBtnGroup)//Если есть гироспком и текущая игра его использует
        {
            controlOptionBtnGroup.OnChange += SetCalibrateBtnActivity;
            controlOptionBtnGroup.SelectedIndex = ProfileInfo.controlOption;
            SetCalibrateBtnActivity(controlOptionBtnGroup);
        }
        else
        {
            ProfileInfo.controlOption = (int)ControlOption.joystick;//setup joystick, not hyro
            if (controlOptionBtnGroup)
                controlOptionBtnGroup.SelectedIndex = (int)ControlOption.joystick;
        }
        
        if (isUltraQualityExists ())
        {
#if !UNITY_EDITOR
            graphicsOptionsBtnGroup.ToggleBtns[(int)GraphicsLevel.lowQuality].gameObject.SetActive (!isDeviceForUltraQuality ()); // так просил Слава
#endif
            graphicsOptionsBtnGroup.ToggleBtns[(int)GraphicsLevel.ultraQuality].gameObject.SetActive (isDeviceForUltraQuality ());
            if (verticalLayout) verticalLayout.Align ();
        }

        PlayerPrefs.SetInt("SwapControls", Convert.ToInt32(false));//Выключаем принудительно управление для левшей, пока не реализовано корректное перемещение всех элементов (или навсегда)

        if (PlayerPrefs.HasKey("GraphicsLevel")) {
            graphicsLevel = PlayerPrefs.GetInt("GraphicsLevel");
        }
        else {
            switch (SystemInfo.deviceType) {
                case DeviceType.Desktop:
                case DeviceType.Console:
                    if (isUltraQualityExists () && isDeviceForUltraQuality ()) {
                        graphicsLevel = (int)GraphicsLevel.ultraQuality;
                    }
                    else {
                        graphicsLevel = (int)GraphicsLevel.highQuality;
                    }
                    break;

                case DeviceType.Handheld:

                default:
                    graphicsLevel = SystemInfo.systemMemorySize < 700 ? (int)GraphicsLevel.lowQuality :
                                    SystemInfo.systemMemorySize > 2100 ? (int)GraphicsLevel.highQuality : (int)GraphicsLevel.normalQuality;
                    break;
            }
#if UNITY_WEBGL
            graphicsLevel = (int) GraphicsLevel.mediumQuality;
#endif
        }

        StartCoroutine(SetQuality());

        if (!GameData.countryFlagsIsOn)
        {
            hideMyFlag.gameObject.SetActive(false);
            avatarAndFlagBtnGroup.gameObject.SetActive(false);

            ProfileInfo.avatarOption = (int)AvatarOption.showOnlyAvatars;
        }

        ProfileChanged();
        UpdateScreenRotationAvailability();
    }

    protected override void Show()
    {
        base.Show();

        if (GUIPager.ActivePage.WindowData != null && GUIPager.ActivePage.WindowData.ContainsKey("Tab"))
            SetTab((Tab)GUIPager.ActivePage.WindowData.GetValue("Tab"));
    }

    protected override void ProfileChanged()
    {
        base.ProfileChanged();

        SetLanguage();

        MusicVolume = PlayerPrefs.GetFloat("MusicVolume", DEFAULT_MUSIC_VOLUME);
        SoundVolume = PlayerPrefs.GetFloat("SoundVolume", DEFAULT_SOUND_VOLUME);

        musicVolumeScrollbar.Value = MusicVolume * SOUND_VOLUME_SCROLLER_MULTIPLIER;
        soundVolumeScrollbar.Value = SoundVolume * SOUND_VOLUME_SCROLLER_MULTIPLIER;

        if(activatedTurretCenterButton)
            activatedTurretCenterButton.IsOn = Convert.ToBoolean(PlayerPrefs.GetInt("TurretButtonActive", DEFAULT_TURRET_ROTATION_INDICATOR_ACTIVITY));

        //if (reverseControls)
        //    reverseControls.IsOn = Convert.ToBoolean(PlayerPrefs.GetInt("SwapControls", DEFAULT_SWAP_CONTROLS_VALUE));

        invert.IsOn = ProfileInfo.isInvert;
        fireOnDoubleTap.IsOn = ProfileInfo.isFireOnDoubleTap;
        hideMyFlag.IsOn = ProfileInfo.isHideMyFlag;

#if TOUCH_SCREEN
        if (sliderControl)
            sliderControl.IsOn = ProfileInfo.isSliderControl;
#endif

        pushForDailyBonus.IsOn = ProfileInfo.isPushForDailyBonus;
        pushForUpgrade.IsOn = ProfileInfo.isPushForUpgrade;
        pushForFuel.IsOn = ProfileInfo.isPushForFuel;
        voiceDisabled.IsOn = ProfileInfo.isVoiceDisabled;
        avatarAndFlagBtnGroup.SelectedIndex = ProfileInfo.avatarOption;
        InitialAcceleration = ProfileInfo.initialAcceleration;

        nickChangePrice.gameObject.SetActive(ProfileInfo.nickEntered && GameData.changeNickPrice != null);
        if (nickChangePrice.gameObject.activeSelf)
            nickChangePrice.Price = GameData.changeNickPrice;

        CheckVoiceDisabling();
        nickTk2dUiTextInput.Text = ProfileInfo.PlayerName;
        SetPlayerIdLbl();

        if (btnAccountManagement != null)
        {
            btnAccountManagement.SetActive(ProfileInfo.Level >= GameData.accountManagementMinLevel);
        }

    }

    private void SetMusicVolume(tk2dUIScrollbar scrollbar)
    {
        MusicVolume = musicVolumeScrollbar.Value / SOUND_VOLUME_SCROLLER_MULTIPLIER;
        MusicBox.Volume = MusicVolume;
    }
    private void SetSoundVolume(tk2dUIScrollbar scrollbar)
    {
        SoundVolume = soundVolumeScrollbar.Value / SOUND_VOLUME_SCROLLER_MULTIPLIER;
    }

    private void OnNickChanged(tk2dUITextInput textInput)
    {
        if (nickTk2dUiTextInput.Text.Length > MAX_NAME_LENGTH)
            nickTk2dUiTextInput.Text = nickTk2dUiTextInput.Text.Substring(0, MAX_NAME_LENGTH);
    }

    public void SetLanguage()
    {
        languagesList = new List<Localizer.LocalizationLanguage>(20);
        languageIndex = ProfileInfo.languageIndex;
        foreach (Localizer.LocalizationLanguage i in Enum.GetValues(typeof(Localizer.LocalizationLanguage)))
        {
            languagesList.Add(i);
        }

        if (languageIndex > (languagesList.Count - 1))
        {
            DT.LogError("Wrong languageIndex <{0}> was received from server! Set Language To English!", languageIndex);
            lblLanguage.text = Localizer.LocalizationLanguage.English.ToString();
            Localizer.Language = Localizer.LocalizationLanguage.English;
        }
        else
        {
            lblLanguage.text = languagesList[languageIndex].ToString();
            Localizer.Language = languagesList[languageIndex];
        }

    }

    public static IEnumerator SetQuality()
    {
        if (changingObjectMaterialRoutine != null)
            QualityManager.Instance.StopCoroutine(changingObjectMaterialRoutine);

        string levelObjectName;

        if (GameData.IsHangarScene)
        {
            levelObjectName = string.Format("Hangar_{0}_{1}", GameData.ClearGameFlags(GameData.CurrentGame), ProfileInfo.IsPlayerVip ? "Premium" : "Standart");
            Instance.graphicsOptionsBtnGroup.SelectedIndex = graphicsLevel;
        }
        else
        {
            levelObjectName = string.Format("Level_{0}", GameManager.CurrentMap.ToString().Replace("scnb_", ""));
        }

        levelObject = GameObject.Find(levelObjectName);

        changingObjectMaterialRoutine = QualityManager.Instance.ObjectMaterialsChanging(levelObject);

        yield return QualityManager.Instance.StartCoroutine(changingObjectMaterialRoutine);

        QualityManager.SetQualityLevel(graphicsLevel);

        Dispatcher.Send(EventId.QualitySettingsChanged, new EventInfo_SimpleEvent());
    }

    private void SetCalibrateBtnActivity(tk2dUIToggleButtonGroup toggle)
    {
        if (calibrateBtn)
            calibrateBtn.gameObject.SetActive(toggle.SelectedIndex == (int)ControlOption.gyroscope);
    }

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

    private void SetPlayerIdLbl()
    {
        if (lblPlayerId == null)
            return;

        var playerId = ProfileInfo.profileId > 0 ? "#" + ProfileInfo.profileId + " " : string.Empty;
        var version = GameData.instance.GetBundleVersion();
        var region = Http.Manager.Instance().Region;

        lblPlayerId.text = string.Format("{0}{1} v{2}", playerId, region, version);
    }

    public static float MusicVolume
    {
        get; set;
    }

    public static float SoundVolume
    {
        get
        {
            if (Settings.Instance != null)
                return soundVolume;
            else if (BattleSettings.Instance != null)
                return BattleSettings.Instance.SoundVolume;

            return 0;
        }
        set
        {
            if (!HelpTools.Approximately(value, soundVolume))
                Dispatcher.Send(EventId.SoundVolumeChanged, new EventInfo_SimpleEvent());

            soundVolume = value;
        }
    }

    public void NextLanguage()
    {
        if (++languageIndex >= languagesList.Count)
        {
            languageIndex = 1;
        }

        lblLanguage.text = languagesList[languageIndex].ToString();
        Localizer.Language = languagesList[languageIndex];
    }

    public void PreviousLanguage()
    {
        if (--languageIndex <= 0)
        {
            languageIndex = languagesList.Count - 1;
        }

        lblLanguage.text = languagesList[languageIndex].ToString();
        Localizer.Language = languagesList[languageIndex];
    }
    public void SubmitForAdvancedInput(tk2dUIItem uiItem)
    {
        SaveParams();
        ApplySettings();
    }
    public void Submit(tk2dUIItem uiItem)
    {
        UpdateScreenRotationAvailability();

        if (isCalibrating)
        {
            ProfileInfo.initialAcceleration = Input.acceleration;
            InitialAcceleration = ProfileInfo.initialAcceleration;
            isCalibrating = false;
            SetTab(Tab.Configuration);
            return;
        }

        Localizer.Language = languagesList[languageIndex];
        PlayerName = nickTk2dUiTextInput.Text;
        SaveParams();

        if (ProfileInfo.PlayerName != PlayerName)
        {
            SetNickName(PlayerName, ApplySettings);
        }
        else
        {
            ApplySettings();
        }
    }

    public void SetNickName(string newNickname, Action successCallback)
    {
        Http.Manager.ChangeNickName(newNickname,
            // Success callback
            (result) =>
            {
                if (null != successCallback)
                {
                    successCallback();
                    TransferableSettingsChanged = true;
                }
            },
            // Fail callback
            (result) =>
            {
                Debug.Log("Change nick error: " + result.ServerError.ToString());
                nickTk2dUiTextInput.Text = ProfileInfo.PlayerName;//При ошибке возвращаем предыдущий ник
                switch (result.ServerError)
                {
                    case Http.Error.InternalInvalidNickname:
                        MessageBox.Show(MessageBox.Type.Info, Localizer.GetText("lblBadNickName"));
                        if (GUIPager.ActivePageName == "Settings")
                            SetTab(Tab.Main);
                        break;
                    case Http.Error.ShopNotEnoughMoney:
                        MessageBox.Show(
                            MessageBox.Type.Info, Localizer.GetText("lblNotEnoughGoldForNick"),
                            answer => HangarController.Instance.GoToBank(Bank.Tab.Gold, voiceRequired: true));

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

    public static string SerializedTransferableSettings
    {
        get { return ProfileInfo.PlayerName + ", " + ProfileInfo.isHideMyFlag; }
    }

    private void SaveParams()
    {
        MusicVolume = musicVolumeScrollbar.Value / SOUND_VOLUME_SCROLLER_MULTIPLIER;
        SoundVolume = soundVolumeScrollbar.Value / SOUND_VOLUME_SCROLLER_MULTIPLIER;
        graphicsLevel = graphicsOptionsBtnGroup.SelectedIndex;

        if (activatedTurretCenterButton)
            PlayerPrefs.SetInt("TurretButtonActive", Convert.ToInt32(activatedTurretCenterButton.IsOn));

        //if (reverseControls)
        //    PlayerPrefs.SetInt("SwapControls", Convert.ToInt32(reverseControls.IsOn));

        ProfileInfo.isInvert = invert.IsOn;
        ProfileInfo.isFireOnDoubleTap = fireOnDoubleTap.IsOn;
        ProfileInfo.isPushForDailyBonus = pushForDailyBonus.IsOn;
        ProfileInfo.isPushForUpgrade = pushForUpgrade.IsOn;
        ProfileInfo.isPushForFuel = pushForFuel.IsOn;
        ProfileInfo.languageIndex = languageIndex;
        ProfileInfo.initialAcceleration = InitialAcceleration;
        ProfileInfo.isVoiceDisabled = voiceDisabled.IsOn;

#if TOUCH_SCREEN
        if (sliderControl)
            ProfileInfo.isSliderControl = sliderControl.IsOn;
#endif

        if (GameData.DeviceHasHyroscope && controlOptionBtnGroup)//Сохраняем только если можем поставить гироскоп, иначе будет по дефолту джойстик
            ProfileInfo.controlOption = controlOptionBtnGroup.SelectedIndex;

        if (ProfileInfo.isHideMyFlag != hideMyFlag.IsOn)
        {
            TransferableSettingsChanged = true;
            ProfileInfo.isHideMyFlag = hideMyFlag.IsOn;
        }

        if (ProfileInfo.avatarOption != avatarAndFlagBtnGroup.SelectedIndex)
        {
            ProfileInfo.avatarOption = avatarAndFlagBtnGroup.SelectedIndex;

            if (BattleController.Instance != null)
            {
                //Dispatcher.Send(EventId.AvatarSettingsChanged, null);// Теперь настроек в бою нет и это событие не используется
            }

            TransferableSettingsChanged = true;
        }

        PlayerPrefs.SetFloat("MusicVolume", MusicVolume);
        PlayerPrefs.SetFloat("SoundVolume", SoundVolume);
        PlayerPrefs.SetInt("GraphicsLevel", graphicsLevel);
        PlayerPrefs.Save();
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

    private static void OnSettingsSubmited(EventId id, EventInfo info)
    {
        GUIPager.SetActivePage("MainMenu");
        Shop.VehicleInView.BodykitController.SetShadowPlane();
        VehicleShop.ForcedShowVehicle(Shop.CurrentVehicle.Info.id);
    }

    private void OnPlayerIdClicked()
    {
#if UNITY_EDITOR
        Debug.Log("Player ID label clicked");
        Http.Manager.Instance().OpenPlayerProfile();
#endif
    }

    private void OnChooseServerClicked()
    {
        XDevs.Loading.ServerChooser.ClearChoose();
        MessageBox.Show(MessageBox.Type.Info, Localizer.GetText("msgServerChooseReseted"));
    }

    private void OnLanguageChange(EventId id, EventInfo ei)
    {
        CheckVoiceDisabling();
    }

    private void CheckVoiceDisabling()
    {
        voiceDisabled.gameObject.SetActive(ProfileInfo.IsVoiceDisablingAvailable);
    }

    public void ResetSettings()
    {
        Init();
    }

    private bool isUltraQualityExists () {
        return graphicsOptionsBtnGroup.gameObject != null && (int)GraphicsLevel.ultraQuality < graphicsOptionsBtnGroup.ToggleBtns.Length;
    }

    private bool isDeviceForUltraQuality () {
        return (SystemInfo.deviceType == DeviceType.Desktop) ||
               (SystemInfo.deviceType == DeviceType.Console);
    }

    /// <summary>
    /// Я так понимаю, это для того чтобы запретить с гироскопом менять ориентацию экрана.
    /// </summary>
    private void UpdateScreenRotationAvailability()
    {
        MiscTools.SetScreenAutoOrientation(ProfileInfo.ControlOption != ControlOption.gyroscope);
    }

    /// <summary>
    /// Чтоб не делать метод под обработку клика каждой кнопки
    /// </summary>
    private void OnClick(tk2dUIItem btn)
    {
        switch(btn.name)
        {
            case "btnOpenAdvancedInputSettings": GUIPager.SetActivePage("AdvancedInputSettings"); break;
            case "btnOpenLinkAccountPage": LinkAccountPage.OpenPageStatic(); break;
            case "btnAccountManagement": ClansManager.OpenAccountManagementWebPageStatic(); break;
        }
    }

    public void OnTabChanged(tk2dUIToggleButtonGroup buttonGroup)
    {
        for (int i = 0; i < pages.Length; i++)
            if(pages[i])
                pages[i].SetActive(tabs.SelectedIndex == i);

        isCalibrating = CurrentTab == Tab.Hyroscope;
    }

    private void SetTab(Tab _tab)
    {
        if (((int)_tab) != tabs.SelectedIndex)
            tabs.SelectedIndex = (int)_tab;
    }

    public Tab CurrentTab { get { return (Tab)tabs.SelectedIndex; } }
}
