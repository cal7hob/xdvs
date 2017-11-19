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
    public const float DEFAULT_VEHICLE_CAMERA_DISTANCE = 0.5f;
    public const float DEFAULT_CAM_TO_TANK_DISTANCE = 0.4f;
    public const int MAX_NAME_LENGTH = 14;

    public GameObject settingsBtnBattle;
    public GameObject wrapper;

    [Header("Main:")]
    public tk2dTextMesh lblLanguage;
    public GameObject lblLanguageNext;
    public GameObject lblLanguagePrev;
    public tk2dTextMesh lblPlayerId;
    public tk2dUIScrollbar musicVolumeScrollbar;
    public tk2dUIScrollbar soundVolumeScrollbar;
    public tk2dUITextInput nickName;
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
    public tk2dUIToggleButton mouseControlledCamera;
    public tk2dUIToggleButton autoFire;
    public tk2dUIToggleButtonGroup controlOptionBtnGroup;
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

    private const float SOUND_VOLUME_SCROLLER_MULTIPLIER = 2.0f;

    private static float musicVolume;
    private static float soundVolume;
    private static int graphicsLevel = 4;//Настройки качества по умолчанию
    private static List<Localizer.LocalizationLanguage> languagesList;
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
    public static GraphicsLevel GraphicsLevel { get { return (GraphicsLevel)graphicsLevel; } }

    protected override void Create()
    {
        base.Create();
        Instance = this;

        Dispatcher.Subscribe(EventId.SettingsSubmited, OnSettingsSubmited);
        Dispatcher.Subscribe(EventId.SettingsSubmited, CheckIfGyroControl);
        Dispatcher.Subscribe(EventId.OnLanguageChange, OnLanguageChange);

        Transform btnChooseServerReset = transform.FindInHierarhy("btnChooseServer");
        if (btnChooseServerReset != null)
        {
            btnChooseServerReset.gameObject.SetActive(Http.Manager.Instance().GetAvailableServers().Count > 1);
        }

        nickName.OnTextChange += OnNickChanged;
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
        Dispatcher.Unsubscribe(EventId.SettingsSubmited, OnSettingsSubmited);
        Dispatcher.Unsubscribe(EventId.SettingsSubmited, CheckIfGyroControl);
        Dispatcher.Unsubscribe(EventId.OnLanguageChange, OnLanguageChange);
        musicVolumeScrollbar.OnScroll -= SetMusicVolume;
        soundVolumeScrollbar.OnScroll -= SetSoundVolume;
        nickName.OnTextChange -= OnNickChanged;
        Instance = null;
        base.Destroy();
    }

    protected override void Init()
    {
        base.Init();
        if (isUltraQualityExists())
        {
            if (controlOptionBtnGroup != null)
            {
                controlOptionBtnGroup.SelectedIndex = ProfileInfo.controlOption;
                SetCalibrateBtn(controlOptionBtnGroup);
                controlOptionBtnGroup.OnChange += SetCalibrateBtn;
            }

            graphicsOptionsBtnGroup.ToggleBtns[(int)GraphicsLevel.lowQuality].gameObject.SetActive(!isDeviceForUltraQuality()); // так просил Слава
            graphicsOptionsBtnGroup.ToggleBtns[(int)GraphicsLevel.ultraQuality].gameObject.SetActive(isDeviceForUltraQuality());
            if (verticalLayout) verticalLayout.Align();
        }

        if (PlayerPrefs.HasKey("GraphicsLevel"))
        {
            graphicsLevel = PlayerPrefs.GetInt("GraphicsLevel");
        }
        else
        {
            switch (SystemInfo.deviceType)
            {
                case DeviceType.Desktop:
                case DeviceType.Console:
                    if (isUltraQualityExists() && isDeviceForUltraQuality())
                    {
                        graphicsLevel = (int)GraphicsLevel.ultraQuality;
                    }
                    else
                    {
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
            graphicsLevel = (int)GraphicsLevel.mediumQuality;
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

        musicVolume = PlayerPrefs.GetFloat("MusicVolume", DEFAULT_MUSIC_VOLUME);
        soundVolume = PlayerPrefs.GetFloat("SoundVolume", DEFAULT_SOUND_VOLUME);

        musicVolumeScrollbar.Value = musicVolume * SOUND_VOLUME_SCROLLER_MULTIPLIER;
        soundVolumeScrollbar.Value = soundVolume * SOUND_VOLUME_SCROLLER_MULTIPLIER;

        if (PlayerPrefs.GetInt("TurretButtonActive", 1) != 0 && activatedTurretCenterButton != null)
        {
            activatedTurretCenterButton.IsOn = true;
        }
        if (PlayerPrefs.GetInt("SwapControls", 0) != 0 && reverseControls != null)
        {
            reverseControls.IsOn = true;
        }
        invert.IsOn = ProfileInfo.isInvert;
        fireOnDoubleTap.IsOn = ProfileInfo.isFireOnDoubleTap;
        hideMyFlag.IsOn = ProfileInfo.isHideMyFlag;
        autoFire.IsOn = ProfileInfo.isAutoFire;


#if TOUCH_SCREEN

        if (sliderControl)
            sliderControl.IsOn = ProfileInfo.isSliderControl;

#endif

        pushForDailyBonus.IsOn = ProfileInfo.isPushForDailyBonus;
        pushForUpgrade.IsOn = ProfileInfo.isPushForUpgrade;
        pushForFuel.IsOn = ProfileInfo.isPushForFuel;
        voiceDisabled.IsOn = ProfileInfo.isVoiceDisabled;

        if (SystemInfo.deviceType == DeviceType.Handheld)
        {
            mouseControlledCamera.IsOn = (PlayerPrefs.GetInt("MouseControl", 0) == 1);
        }
        else
        {
            mouseControlledCamera.IsOn = (PlayerPrefs.GetInt("MouseControl", 1) == 1);
        }

        autoFire.IsOn = ProfileInfo.isAutoFire;
        avatarAndFlagBtnGroup.SelectedIndex = ProfileInfo.avatarOption;
        InitialAcceleration = ProfileInfo.initialAcceleration;
        //Принудилово на джойстик-управление, т.к. в билде "случайно" были включены радиобаттоны для переключения джойстик / гироскоп
        if (GameData.IsGame(Game.CodeOfWar))
            ProfileInfo.controlOption = (int)ControlOption.joystick;

        UpdateChangeNickPrice();

        SetLanguage();
        CheckVoiceDisabling();
        nickName.Text = ProfileInfo.PlayerName;
        SetPlayerIdLbl();

        if (btnAccountManagement != null)
        {
            btnAccountManagement.SetActive(ProfileInfo.Level >= GameData.accountManagementMinLevel);
        }

    }

    private void SetMusicVolume(tk2dUIScrollbar scrollbar)
    {
        musicVolume = musicVolumeScrollbar.Value / SOUND_VOLUME_SCROLLER_MULTIPLIER;
        MusicBox.Volume = musicVolume;
    }
    private void SetSoundVolume(tk2dUIScrollbar scrollbar)
    {
        soundVolume = soundVolumeScrollbar.Value / SOUND_VOLUME_SCROLLER_MULTIPLIER;
    }

    private void OnNickChanged(tk2dUITextInput textInput)
    {
        if (nickName.Text.Length > MAX_NAME_LENGTH)
            nickName.Text = nickName.Text.Substring(0, MAX_NAME_LENGTH);
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
               /* ProfileInfo.IsPlayerVip ? "Premium" : */"Standart");

            Instance.graphicsOptionsBtnGroup.SelectedIndex = graphicsLevel;
            changingObjectMaterialRoutine = QualityManager.Instance.ChangeObjectMaterials(Shop.VehicleInView.HangarVehicle.gameObject);
            //yield return QualityManager.Instance.StartCoroutine(changingObjectMaterialRoutine); закомменчено, чтобы ангарная техника оставалась со своим матом
        }
        else
        {
            levelObjectName = string.Format("Level_{0}", GameManager.CurrentMap.ToString().Replace("scnb_", ""));
        }

        levelObject = Map.LevelObjectsRoot ?? GameObject.Find(levelObjectName);

        changingObjectMaterialRoutine = QualityManager.Instance.ChangeObjectMaterials(levelObject);

        yield return QualityManager.Instance.StartCoroutine(changingObjectMaterialRoutine);

        QualityManager.SetQualityLevel(graphicsLevel);
        Dispatcher.Send(EventId.QualitySettingsChanged, new EventInfo_SimpleEvent());
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
                nickChangeCurrencySprite.SetSprite(GameData.changeNickPrice.currency == ProfileInfo.PriceCurrency.Gold ? "gold" : "silver");
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
        get { return musicVolume; }
    }

    public static float SoundVolume
    {
        get
        {
            if (Settings.Instance != null)
            {
                return soundVolume;
            }
            else if (BattleSettings.Instance != null)
            {
                return BattleSettings.Instance.SoundVolume;
            }
            return 0;
        }
    }

    public void NextLanguage()
    {
        MenuController.SmallArrowSound();
        if (++languageIndex >= languagesList.Count)
        {
            languageIndex = 1;
        }

        lblLanguage.text = languagesList[languageIndex].ToString();
        Localizer.Language = languagesList[languageIndex];
    }

    public void PreviousLanguage()
    {
        MenuController.SmallArrowSound();
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
        PlayerName = nickName.Text;
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
                nickName.Text = ProfileInfo.PlayerName;//При ошибке возвращаем предыдущий ник
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
        //MenuController.nextSound);
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
        {
            bottomButtonsAligner.Align();
        }
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

        if (indicator == null) return;
        indicator.playerName.text = playerName;
        Dispatcher.Send(EventId.FlagSettingsChanged, null);
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
        musicVolume = musicVolumeScrollbar.Value / SOUND_VOLUME_SCROLLER_MULTIPLIER;
        soundVolume = soundVolumeScrollbar.Value / SOUND_VOLUME_SCROLLER_MULTIPLIER;
        graphicsLevel = graphicsOptionsBtnGroup.SelectedIndex;

        if (activatedTurretCenterButton != null)
        {
            PlayerPrefs.SetInt("TurretButtonActive", activatedTurretCenterButton.IsOn ? 1 : 0);
        }
        if (reverseControls != null)
        {
            PlayerPrefs.SetInt("SwapControls", reverseControls.IsOn ? 1 : 0);
        }


        ProfileInfo.isInvert = invert.IsOn;
        ProfileInfo.isFireOnDoubleTap = fireOnDoubleTap.IsOn;
        ProfileInfo.isPushForDailyBonus = pushForDailyBonus.IsOn;
        ProfileInfo.isPushForUpgrade = pushForUpgrade.IsOn;
        ProfileInfo.isPushForFuel = pushForFuel.IsOn;
        ProfileInfo.languageIndex = languageIndex;
        ProfileInfo.initialAcceleration = InitialAcceleration;
        ProfileInfo.isVoiceDisabled = voiceDisabled.IsOn;
        ProfileInfo.isAutoFire = autoFire.IsOn;

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
                Dispatcher.Send(EventId.AvatarSettingsChanged, null);
            }

            TransferableSettingsChanged = true;
        }

        PlayerPrefs.SetFloat("MusicVolume", musicVolume);
        PlayerPrefs.SetFloat("SoundVolume", soundVolume);
        PlayerPrefs.SetInt("GraphicsLevel", graphicsLevel);
        PlayerPrefs.SetInt("MouseControl", mouseControlledCamera.IsOn ? 1 : 0);
        PlayerPrefs.Save();

        MenuController.ShowUserInfo();
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
        if (GameData.IsHangarScene)
        {
            GUIPager.SetActivePage("MainMenu");

            Shop.VehicleInView.BodykitController.SetShadowPlane();

            VehicleShop.ForcedShowVehicle(Shop.CurrentVehicle.Info.id);
        }
        else
        {
            Instance.wrapper.SetActive(false);

            StatTable.instance.gameObject.SetActive(StatTable.State != StatTable.TableState.AfterDeath);

            if (BattleController.MyVehicle != null)
            {
                foreach (var vehicleController in BattleController.allVehicles)
                    vehicleController.Value.BodykitController.SetShadowPlane();
            }

            TopPanelValues.NickName = ProfileInfo.PlayerName;
            Dispatcher.Send(EventId.FlagSettingsChanged, null);
            TopPanelValues.SetEarnedGold(ProfileInfo.Gold);
            TopPanelValues.SetEarnedSilver(ProfileInfo.Silver);

            if (!TransferableSettingsChanged) return;

            var properties = new Hashtable() { { "st", SerializedTransferableSettings } };
            PhotonNetwork.player.SetCustomProperties(properties);

            TransferableSettingsChanged = false;

            CheckIfGyroControl();
        }
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
    public void ClickInputSettings(tk2dUIItem item)
    {
        Debug.Log("Item" + item.name);
    }

    public void ResetSettings()
    {
        Init();
    }

    private bool isUltraQualityExists()
    {
        return graphicsOptionsBtnGroup.gameObject != null && (int)GraphicsLevel.ultraQuality < graphicsOptionsBtnGroup.ToggleBtns.Length;
    }

    private bool isDeviceForUltraQuality()
    {
        return (SystemInfo.deviceType == DeviceType.Desktop) ||
               (SystemInfo.deviceType == DeviceType.Console);
    }

}
