#if !(UNITY_STANDALONE_OSX || UNITY_WEBPLAYER || UNITY_WEBGL)
#define TOUCH_SCREEN
#endif

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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
    joystick,
    gyroscope
}

public enum GraphicsLevel
{
    LowQuality,
    MediumQuality,
    NormalQuality,
    HighQuality,
    UltraQuality,

    GraphicsLevelCount
}

public enum MaterialQualityLevel
{
    mobile_default,
    mobile_max,
    pc_max
}

public class Settings : HangarPage
{
    public const float DEFAULT_MUSIC_VOLUME = 0.2f;
    public const float DEFAULT_SOUND_VOLUME = 0.5f;
    public const float DEFAULT_TURRET_ROTATION_SENSITIVITY = 0.5f;
    public const int   DEFAULT_TURRET_ROTATION_INDICATOR_ACTIVITY = 1;
    public const float DEFAULT_VEHICLE_CAMERA_DISTANCE = 0.5f;
    public const float DEFAULT_CAM_TO_TANK_DISTANCE = 1f;
    public const int   DEFAULT_SWAP_CONTROLS_VALUE = 0;
    public const int DEFAULT_FIX_TURRET_VALUE = 0;

    public const FloatSpeedXYJoystick.TypeJoystick DEFAULT_TURRET_CONTROL_TYPE = FloatSpeedXYJoystick.TypeJoystick.floatingX;
    public const int MAX_NAME_LENGTH = 14;

    public GameObject wrapper;

    [Header("Main:")]
    public tk2dTextMesh lblLanguage;
    public GameObject lblLanguageNext;
    public GameObject lblLanguagePrev;
    public tk2dTextMesh lblPlayerId;
    public tk2dUIScrollbar musicVolumeScrollbar;
    public tk2dUIScrollbar soundVolumeScrollbar;
    public tk2dUITextInput nickTk2dUiTextInput;
    public TextTie nickChangePrice;
    public tk2dSprite nickChangeCurrencySprite;
    public tk2dUIToggleButtonGroup avatarAndFlagBtnGroup;
    public GameObject btnAccountManagement;
    public UniAlignerBase bottomButtonsAligner;

    [Header("Configuration:")]
    public tk2dUIToggleControl invert;
    public tk2dUIToggleControl sliderControl;
    public tk2dUIToggleControl fireOnDoubleTap;
    public tk2dUIToggleControl hideMyFlag;
    public tk2dUIToggleControl voiceDisabled;
    public tk2dUIToggleControl reverseControls;
    public tk2dUIToggleControl activatedTurretCenterButton;
    public tk2dUIToggleButtonGroup controlOptionBtnGroup;
    public tk2dUIToggleControl fixTurret;
    public tk2dUIItem calibrateBtn;
    private tk2dBaseSprite calibrateBtnSpr;

    [Header("Notifications:")]
    public tk2dUIToggleControl pushForDailyBonus;
    public tk2dUIToggleControl pushForUpgrade;
    public tk2dUIToggleControl pushForFuel;

    [Header("Graphics:")]
    public tk2dUIToggleButtonGroup graphicsOptionsBtnGroup;
    public VerticalLayout verticalLayout;

    public List<SettingsPages> settingsPages = new List<SettingsPages>();

    public const float SOUND_VOLUME_SCROLLER_MULTIPLIER = 1.0f;

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

        Messenger.Subscribe(EventId.SettingsSubmited, OnSettingsSubmited);
        Messenger.Subscribe(EventId.SettingsSubmited, CheckIfGyroControl);
        Messenger.Subscribe(EventId.OnLanguageChange, OnLanguageChange);

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
    }

    protected override void Destroy()
    {
        Messenger.Unsubscribe(EventId.SettingsSubmited, OnSettingsSubmited);
        Messenger.Unsubscribe(EventId.SettingsSubmited, CheckIfGyroControl);
        Messenger.Unsubscribe(EventId.OnLanguageChange, OnLanguageChange);
        musicVolumeScrollbar.OnScroll -= SetMusicVolume;
        soundVolumeScrollbar.OnScroll -= SetSoundVolume;
        Instance = null;
        base.Destroy();
    }

    protected override void Init()
    {
        base.Init();
        if (isUltraQualityExists ()) {
            if (controlOptionBtnGroup != null) {
                controlOptionBtnGroup.SelectedIndex = ProfileInfo.controlOption;
                SetCalibrateBtn (controlOptionBtnGroup);
                controlOptionBtnGroup.OnChange += SetCalibrateBtn;
            }

            graphicsOptionsBtnGroup.ToggleBtns[(int)GraphicsLevel.LowQuality].gameObject.SetActive (!isDeviceForUltraQuality ()); // так просил Слава
            graphicsOptionsBtnGroup.ToggleBtns[(int)GraphicsLevel.UltraQuality].gameObject.SetActive (isDeviceForUltraQuality ());
            if (verticalLayout) verticalLayout.Align ();
        }

        if (PlayerPrefs.HasKey("GraphicsLevel")) {
            graphicsLevel = PlayerPrefs.GetInt("GraphicsLevel");
        }
        else {
            switch (SystemInfo.deviceType) {
                case DeviceType.Desktop:
                case DeviceType.Console:
                    if (isUltraQualityExists () && isDeviceForUltraQuality ()) {
                        graphicsLevel = (int)GraphicsLevel.UltraQuality;
                    }
                    else {
                        graphicsLevel = (int)GraphicsLevel.HighQuality;
                    }
                    break;

                case DeviceType.Handheld:

                default:
                    graphicsLevel = SystemInfo.systemMemorySize < 700 ? (int)GraphicsLevel.LowQuality :
                                    SystemInfo.systemMemorySize > 2100 ? (int)GraphicsLevel.HighQuality : (int)GraphicsLevel.NormalQuality;
                    break;
            }
#if UNITY_WEBGL
            graphicsLevel = (int) GraphicsLevel.MediumQuality;
#endif
        }

        StartCoroutine(SetQuality());

        if (!GameData.countryFlagsIsOn)
        {
            hideMyFlag.gameObject.SetActive(false);
            avatarAndFlagBtnGroup.gameObject.SetActive(false);

            ProfileInfo.avatarOption = (int)AvatarOption.showOnlyAvatars;
        }

#if UNITY_STANDALONE_OSX || UNITY_WEBGL || UNITY_WEBPLAYER
        foreach (var settingsPage in settingsPages)
        {
            if (settingsPage.SettingsPageBtn.name.Equals("SocialsBtn"))
            {
                settingsPage.SettingsPageBtn.gameObject.SetActive(false);
            }
        }
#endif

        ProfileChanged();
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

        if (reverseControls)
            reverseControls.IsOn = Convert.ToBoolean(PlayerPrefs.GetInt("SwapControls", DEFAULT_SWAP_CONTROLS_VALUE));

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
        //Принудилово на джойстик-управление, т.к. в билде "случайно" были включены радиобаттоны для переключения джойстик / гироскоп
        if (GameData.IsGame(Game.Armada))
            ProfileInfo.controlOption = (int)ControlOption.joystick;

        fixTurret.IsOn = Convert.ToBoolean(PlayerPrefs.GetInt("FixateTurret", DEFAULT_FIX_TURRET_VALUE));

        UpdateChangeNickPrice();

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
        {
            QualityManager.Instance.StopCoroutine(changingObjectMaterialRoutine);
        }

        string levelObjectName;

        if (GameData.IsHangarScene)
        {
            levelObjectName = string.Format("Hangar_{0}_{1}", GameData.ClearGameFlags(GameData.CurrentGame),
                ProfileInfo.IsPlayerVip ? "Premium" : "Standart");

            Instance.graphicsOptionsBtnGroup.SelectedIndex = graphicsLevel;
            changingObjectMaterialRoutine = QualityManager.Instance.ChangeObjectMaterials(Shop.VehicleInView.HangarVehicle.gameObject);
            //yield return QualityManager.Instance.StartCoroutine(changingObjectMaterialRoutine); закомменчено, чтобы ангарная техника оставалась со своим матом
        }
        else
        {
            levelObjectName = string.Format("Level_{0}", GameManager.CurrentMap.ToString().Replace("scnb_", ""));
        }

        levelObject = GameObject.Find(levelObjectName);

        if (levelObject != null)
        {
            changingObjectMaterialRoutine = QualityManager.Instance.ChangeObjectMaterials(levelObject);

            yield return QualityManager.Instance.StartCoroutine(changingObjectMaterialRoutine);
        }

        QualityManager.SetQualityLevel(graphicsLevel);

        Messenger.Send(EventId.QualitySettingsChanged, new EventInfo_SimpleEvent());
    }

    private void SetCalibrateBtn(tk2dUIToggleButtonGroup toggle)
    {
        if (calibrateBtn.gameObject == null)
        {
            return;
        }

        calibrateBtn.gameObject.SetActive(toggle.SelectedIndex == (int)ControlOption.gyroscope);

        if (bottomButtonsAligner)
            bottomButtonsAligner.Align();
    }

    public void UpdateChangeNickPrice()
    {
        if (nickChangePrice)
            nickChangePrice.gameObject.SetActive(false);
        if (nickChangeCurrencySprite)
            nickChangeCurrencySprite.gameObject.SetActive(false);
        if (ProfileInfo.nickEntered && GameData.IsHangarScene && GameData.changeNickPrice != null)
        {
            if (nickChangePrice)
            {
                nickChangePrice.gameObject.SetActive(true);
                nickChangePrice.SetText(GameData.changeNickPrice.LocalizedValue);
                GameData.changeNickPrice.SetMoneySpecificColorIfCan(nickChangePrice.TextMesh);
            }
            if (nickChangeCurrencySprite)
            {
                nickChangeCurrencySprite.SetSprite(GameData.changeNickPrice.currency == ProfileInfo.PriceCurrency.Gold ? "goldSmall" : "silverSmall");
                nickChangeCurrencySprite.gameObject.SetActive(true);
            }

        }
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

        bodykitController.RefreshCamouflage();
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
            return soundVolume;
        }
        set
        {
            if (HelpTools.Approximately(value, soundVolume))
                return;

            soundVolume = value;

            Messenger.Send(EventId.SoundVolumeChanged, new EventInfo_SimpleEvent());
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
        if (isCalibrating)
        {
            ProfileInfo.initialAcceleration = Input.acceleration;
            InitialAcceleration = ProfileInfo.initialAcceleration;
            isCalibrating = false;
            Instance.settingsPages[1].SettingsPageBtn.SimulateClick();
            //CheckIfInitialAccelerationIsSet();
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
                        foreach (var settingsPage in settingsPages)
                        {
                            if (settingsPage.SettingsPage.name == "Main" && GUIPager.ActivePage != "EnterName")
                                settingsPage.SettingsPageBtn.SimulateClick();
                        }
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

    public void PageSelectionClick(tk2dUIItem item)
    {
        foreach (var page in settingsPages)
        {
            var currentPage = page.SettingsPage;
            var currentToggleBtn = page.SettingsPageBtn.GetComponent<tk2dUIToggleControl>();
            var showHideScript = currentPage.GetComponent<ShowHideGUIPage>();

            if (page.SettingsPageBtn == item)
            {
                if (showHideScript) showHideScript.MoveToDefaultPositionAndShow();
                else currentPage.SetActive(true);

                if (currentToggleBtn != null)
                {
                    currentToggleBtn.IsOn = true;
                }

                isCalibrating = item == calibrateBtn;
            }
            else
            {
                if (showHideScript) showHideScript.Hide();
                else currentPage.SetActive(false);

                if (currentToggleBtn != null)
                {
                    currentToggleBtn.IsOn = false;
                }
            }
        }

        if (bottomButtonsAligner)
            bottomButtonsAligner.Align();
    }

    public static void ChangePlayerSettings(string settings, int id)
    {
        if (String.IsNullOrEmpty(settings)) return;

        var ei = settings.Split(',');
        var playerName = ei[0];
        var hideHisFlag = ei[1].Trim() == "True";

        BattleController.allVehicles[id].data.hideMyFlag = hideHisFlag;
        BattleController.allVehicles[id].data.playerName = playerName;
        BattleController.allVehicles[id].Statistics.playerName = playerName;
        var indicator = TankIndicators.GetIndicator(id);

        if (indicator == null)
        {
            return;
        }
            
        indicator.playerName.text = playerName;
        Messenger.Send(EventId.FlagSettingsChanged, null);
    }

    public static string SerializedTransferableSettings
    {
        get { return ProfileInfo.PlayerName + ", " + ProfileInfo.isHideMyFlag; }
    }

    public static void SetFirstSettingsTab()
    {
        Instance.settingsPages[0].SettingsPageBtn.SimulateClick();
        Instance.SetPlayerIdLbl();
    }

    private void SaveParams()
    {
        MusicVolume = musicVolumeScrollbar.Value / SOUND_VOLUME_SCROLLER_MULTIPLIER;
        SoundVolume = soundVolumeScrollbar.Value / SOUND_VOLUME_SCROLLER_MULTIPLIER;
        graphicsLevel = graphicsOptionsBtnGroup.SelectedIndex;

        if (activatedTurretCenterButton)
            PlayerPrefs.SetInt("TurretButtonActive", Convert.ToInt32(activatedTurretCenterButton.IsOn));

        if (reverseControls)
            PlayerPrefs.SetInt("SwapControls", Convert.ToInt32(reverseControls.IsOn));

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

        if (controlOptionBtnGroup)
        {
            ProfileInfo.controlOption = controlOptionBtnGroup.SelectedIndex;
        }

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
                Messenger.Send(EventId.AvatarSettingsChanged, null);
            }

            TransferableSettingsChanged = true;
        }

        PlayerPrefs.SetInt("FixateTurret", Convert.ToInt32(fixTurret.IsOn));

        PlayerPrefs.SetFloat("MusicVolume", MusicVolume);
        PlayerPrefs.SetFloat("SoundVolume", SoundVolume);
        PlayerPrefs.SetInt("GraphicsLevel", graphicsLevel);
        PlayerPrefs.Save();

        if (HangarController.Instance != null)
            HangarController.Instance.ShowUserInfo();
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
        Messenger.Send(EventId.SettingsSubmited, null);
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

    private static void CheckIfGyroControl(EventId id = 0, EventInfo info = null)
    {
        if (JoystickManager.Instance != null)
            foreach (JoystickController joystick in JoystickManager.Instance.Items)
                joystick.Visible = joystick.IsOn && ProfileInfo.ControlOption != ControlOption.gyroscope;

        MiscTools.SetScreenAutoOrientation(ProfileInfo.ControlOption != ControlOption.gyroscope);
    }

    private void OnLanguageChange(EventId id, EventInfo ei)
    {
        CheckVoiceDisabling();
    }

    private void CheckVoiceDisabling()
    {
        voiceDisabled.gameObject.SetActive(ProfileInfo.IsVoiceDisablingAvailable);
    }

    public void ClickInputSettings(tk2dUIItem item)
    {
        Debug.Log("Item" + item.name);
    }

    public void ResetSettings()
    {
        Init();
    }

    private bool isUltraQualityExists () {
        return graphicsOptionsBtnGroup.gameObject != null && (int)GraphicsLevel.UltraQuality < graphicsOptionsBtnGroup.ToggleBtns.Length;
    }

    private bool isDeviceForUltraQuality () {
        return (SystemInfo.deviceType == DeviceType.Desktop) ||
               (SystemInfo.deviceType == DeviceType.Console);
    }

    protected override void OnMessageBoxChangeVisibility(EventId id, EventInfo info)
    {
        EventInfo_B eInfo = (EventInfo_B)info;
        if (exitToMainMenuOnMessageBoxAppears && IsVisible && eInfo.bool1)
        {
            ResetSettings();
            GUIPager.ToMainMenu();
        }
    }
}
