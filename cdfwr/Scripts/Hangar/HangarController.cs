using System;
using System.Linq;
using CodeStage.AntiCheat.ObscuredTypes;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Disconnect;
using UnityEngine.SceneManagement;
using UnityEngine.Serialization;

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

public class HangarController : MonoBehaviour
{
    public static event Action<double> OnTimerTick;

    public bool isWaitingForSaving;

    [FormerlySerializedAs("tankPanelBackground")]
    public MeshFilter bottomGuiPanel;
    [Space]
    [Header("Bars")]
    public DeltaProgressBar armorBar;
    public DeltaProgressBar attackBar;
    public DeltaProgressBar rocketAttackBar;
    public DeltaProgressBar speedBar;
    public DeltaProgressBar rofBar;
    public DeltaProgressBar ircmRofBar;
    public ProgressBar userLevelBar;
    [Space]
    [Header("Labels")]
    public tk2dTextMesh lblUserLevel;
    public float lblUserLevelEuropianFontScale = 1;//в китайском языке цифры большие, приходится вручную скейлить (пока только в SpaceJet)
    public float lblUserLevelChineseFontScale = 1;
    public tk2dTextMesh lblUserName;
    public tk2dTextMesh lblVehicleName;
    public tk2dTextMesh lblSilver;
    public tk2dTextMesh lblGold;
    [Space]
    [Header("Boxes")]

    public HangarBuyingBox buyingBox;

    public HangarSaleBuyingBox saleBuyingBox;
    public HangarSaleRentBox saleRentBox;
    public GameObject recieveFreeTankBox;
    public HangarDeliverBox deliverBox;
    public HangarRentBox rentBox;
    public HangarRentingBox rentingBox;
    public HangarRentedBox rentedBox;
    public GameObject installBox;
    public GameObject installedBox;
    public GameObject inaccessibleBox;
    public LabelLocalizationAgent inaccessibleBoxLabel;
    public GameObject comingSoonBox;
    public tk2dUIToggleButtonGroup bankCurrencyToggle;
    public bool demoVersion;
    public GameObject btnBank;
    public tk2dTextMesh experienceLabel;
    public GameObject hasDoubleExpTanksIndicator;
    public GameObject doubleExpWrapper;
    public GameObject btnVipShop;
    public GameObject btnBuyVip;
    public GameObject vipAccountShop;
    public FuelBarManager fuelBarManager;
    [Space]
    [Header("Sounds")]
    public AudioClip backSound;
    public AudioClip nextSound;
    public AudioClip tankSelectSound;
    public AudioClip patternSelectSound;
    public AudioClip decalSelectSound;
    public AudioClip buyingSound;
    public AudioClip buyingConsumablesSound;
    public AudioClip moduleInstallSound;
    public AudioClip toBattleSound;
    public AudioClip toBattleSoundTeamMode;
    public AudioClip tankInstallSound;
    public AudioClip checkBoxSound;
    public AudioClip smallArrowSound;
    [Space]
    public Color quitGameQuestionTextColor = Color.white;
    public Color quitGameQuestionGameColor = Color.white;

    public bool forceShadowPlaneShow;

    private static bool firstHangarEnter = true;

    private bool isBattleEntering;
    private float lastTick;

    public List<SpecialOffer> allOffersList = new List<SpecialOffer>();

    public static HangarController Instance { get; private set; }

    public static bool FirstEnter { get { return firstHangarEnter; } }

    public static bool DemoVersion { get { return Instance.demoVersion; } }

    public static bool ExitingGame { get; private set; }

    public bool IsInitialized { get; private set; }

    public LabelLocalizationAgent InaccessibleBoxLabel { get { return inaccessibleBoxLabel; } }

    public HangarBuyingBox BuyingBox { get { return buyingBox; } }

    public Dictionary<int, float> ArmorMax { get; private set; }

    public Dictionary<int, float> DamageMax { get; private set; }

    public Dictionary<int, float> RocketDamageMax { get; private set; }

    public Dictionary<int, float> SpeedMax { get; private set; }

    public Dictionary<int, float> IRCMROFMax { get; private set; }

    public Dictionary<int, float> ROFMax { get; private set; }

    public Dictionary<int, float> MagazineMax { get; private set; }

    public Dictionary<int, float> ReloadMax { get; private set; }

    public Camera GuiCamera { get; private set; }

    public tk2dCamera Tk2dGuiCamera { get; private set; }

    public static HangarBuyingBox CurrentActionBox { get; private set; }

    private const int RELOAD_MAX = 8;

    /*    UNITY SECTION    */

    void Awake()
    {
        if (Tk2dGuiCamera == null)
        {
            Tk2dGuiCamera = transform.Find("Hangar2D").GetComponent<tk2dCamera>();
        }

        GuiCamera = Tk2dGuiCamera.GetComponent<Camera>();

        if (!GameData.ServerDataReceived)
        {
            Loading.gotoLoadingScene();
            return;
        }

        IsInitialized = false;
        Instance = this;
        Screen.sleepTimeout = SleepTimeout.NeverSleep;
        ObscuredPrefs.onAlterationDetected = OnObscuredPrefsAlteration;
        GUIPager.OnPageChange += OnPageChange;
        lastTick = GameData.instance.GetCorrectedTime();
        fuelBarManager.TotalCanAmount = ProfileInfo.MaxFuel;
        doubleExpWrapper.SetActive(false);//Пока не выбрана техника - выключаем инфу о двойном опыте
        Dispatcher.Subscribe(EventId.ProfileInfoLoadedFromServer, ProfileLoadedFromServer);
        Dispatcher.Subscribe(EventId.MessageBoxChangeVisibility, OnMessageBoxChangeVisibility);
        Dispatcher.Subscribe(EventId.OnLanguageChange, OnLanguageChange);
        CodeStage.AntiCheat.Detectors.SpeedHackDetector.isRunning = true;
        if (PhotonNetwork.connectionState != ConnectionState.Disconnected)
        {
            PhotonNetwork.Disconnect();
        }
    }

    void Start() { StartCoroutine(RealStart()); }

    IEnumerator RealStart()
    {
        ShowUserInfo();

        Dispatcher.Send(EventId.AfterLocalizationLoad, new EventInfo_SimpleEvent());

        yield return null;
        yield return null;

        if (firstHangarEnter)
        {
            GUIPager.SetActivePage("MainMenu");

            // Profile crash checking.
            if (ProfileInfo.CurrentVehicle == 0 && ProfileInfo.Silver == 0)
            {
                ProfileInfo.LoadDefaults();
            }

            GoogleAnalyticsWrapper.LogTiming(
                new TimingHitBuilder()
                    .SetTimingCategory("Loading")
                    .SetTimingInterval(
                        Convert.ToInt64(
                                TimeSpan.FromSeconds(
                                    Convert.ToDouble(
                                        Time.realtimeSinceStartup))
                            .TotalMilliseconds)));

            if (ProfileInfo.ImportantUpdate)
            {
                Dispatcher.Send(EventId.GameUpdateRequired, null);

                // Даём возможность аналитике отправиться?..
                yield return new WaitForSeconds(1f);

                XdevsSplashScreen.SetActive(false);
                XdevsSplashScreen.SetActiveWaitingIndicator(false);

                yield break;
            }
        }

        RefuelByTime(GameData.CurrentTimeStamp - ProfileInfo.lastVisit);

        fuelBarManager.TotalCanAmount = ProfileInfo.MaxFuel;

        Dispatcher.Send(EventId.GameModeChanged, new EventInfo_SimpleEvent());

        this.InvokeRepeating(HangarTimerTick, 0, 1);

        //if (firstHangarEnter)
        //{
        //	string filename = GameData.IsGame(Game.IronTanks) ? "TanksDB.json" : "TanksDB_Team.json";
        //	HelpTools.ImportLocalDB(Path.Combine(Application.streamingAssetsPath, filename));
        //}

        ShopManager.Instance.Load();

        GUIController.ListenButtonClick("btnShopWindow", OnVehicleShopEnter);
        GUIController.ListenButtonClick("btnArmory", OnArmoryEnter);
        GUIController.ListenButtonClick("btnBack", OnBackClick);
        GUIController.ListenButtonClick("btnSettings", OnSettingsClick);
        GUIController.ListenButtonClick("btnBank", OnBankClick);
        GUIController.ListenButtonClick("btnToBattle", OnBattleQuestsClick);
        GUIController.ListenButtonClick("btnQuestsOk", OnBattleClick);
        GUIController.ListenButtonClick("btnPattern", OnPatternShopClick);
        GUIController.ListenButtonClick("btnWeapon", OnDecalsShopClick);
        //GUIController.ListenButtonClick("btnSale", SpecialOffer.OnSpecialOfferClicked);
        GUIController.ListenButtonClick("btnRecieveFreeTank", VehicleOffersController.OnRecieveFreeVehicleClick);

        GUIController.ListenButtonClick("btnVipShop", ToVipShopButtonsClickListener);
        GUIController.ListenButtonClick("btnBuyVip", ToVipShopButtonsClickListener);

        GUIController.ListenButtonClick("btnKitsMode", NavigateBuyKit);
        GUIController.ListenButtonClick("btnSilverMode", NavigateBuySilver);
        GUIController.ListenButtonClick("btnGoldMode", NavigateBuyGold);

        ProfileInfo.HinduBugFix();

        ShowUserInfo();

        hasDoubleExpTanksIndicator.SetActive(ProfileInfo.doubleExpVehicles.Count > 0);

        IsInitialized = true;

        GameData.allOffersList.Clear();

        //Закомментировал, т.к. есть правильная аутентификация в Achievments. Здесь запускать ее нельзя. Илья. 12.10.2016
        //GPGSWrapper.SocialAuthenticatation(
        //    success =>
        //    {
        //        if (success)
        //            Debug.LogFormat(
        //                "Authenticatation for GPG user \"{0}\" succeed!",
        //                Social.localUser.userName);
        //    });

        Dispatcher.Send(EventId.AfterHangarInit, new EventInfo_SimpleEvent());

        OnTimerTick += CheckForNewDay;
        OnTimerTick += QuestsUI.SetQuestsTimer;
        OnTimerTick += FriendsManager.UpdateFriends;

        if (!firstHangarEnter)
        {
            ProfileInfo.SaveToServer();
        }

        yield return new WaitForSeconds(0.5f);

        XdevsSplashScreen.SetActive(false);
        XdevsSplashScreen.SetActiveWaitingIndicator(false);

        CacheManager.ClearOutdatedCache();

        //Debug.LogWarningFormat(
        //    "ProfileInfo.daysInARow = {0}\nMaxKillsInARow = {1}\nTotalMileage = {2}\nBattlesCount = {3}\nTotalPlayedTime = {4}",
        //    ProfileInfo.daysInARow,
        //    BattleStatisticsManager.OverallBattleStats["MaxKillsInARow"],
        //    BattleStatisticsManager.OverallBattleStats["TotalMileage"],
        //    BattleStatisticsManager.OverallBattleStats["BattlesCount"],
        //    BattleStatisticsManager.OverallBattleStats["TotalPlayedTime"]);

        // Костыль для лечения старого бага с отжатием бонусного топлива.
        if (ProfileInfo.vehicleUpgrades.ContainsKey(GameData.EXTRA_FUEL_VEHICLE_ID) &&
            ProfileInfo.MaxFuel < GameData.MAX_GAME_FUEL_AMOUNT)
        {
            ProfileInfo.MaxFuel = GameData.MAX_GAME_FUEL_AMOUNT;
        }
        else if (!ProfileInfo.vehicleUpgrades.ContainsKey(GameData.EXTRA_FUEL_VEHICLE_ID) &&
                 !ProfileInfo.IsPlayerVip &&
                 ProfileInfo.MaxFuel > GameData.STANDART_FUEL_CAN_AMOUNT)
        {
            ProfileInfo.MaxFuel = GameData.STANDART_FUEL_CAN_AMOUNT;
        }

        #region Определяем, какое окно показывать

        // after vip hangar reload
        if (VipManager.IsHangarReloadRequired || SocialSettings.IsHangarReloadRequired)
        {
            VipManager.IsHangarReloadRequired = false;
            SocialSettings.IsHangarReloadRequired = false;

            GUIPager.SetActivePage("MainMenu");
            Dispatcher.Send(EventId.OnReadyToStartWindowsQueue, null);

            yield break;
        }

        Action passingAfterBattleScreen = () =>
        {
            XdevsSplashScreen.SetActiveWaitingIndicator(false);

            // Не показываем окно послебоевой статистики после боевого тутора
            if (FirstEnter)
            {
                EnterNickName.Instance.PopUpNicknameWindow();
            }
            else if (BattleStatisticsManager.BattleStats["ConnectionFailed"] != 1)
            {
                AfterBattleStatistic.Instance.Show();
            }
            else
            {
                GUIPager.SetActivePage("MainMenu");
            }

            Dispatcher.Send(EventId.OnReadyToStartWindowsQueue, null);
        };

        // Включаем рекламу после боя только если прошли тутор (TODO: условие прохождения тутора)
        if (!FirstEnter && ProfileInfo.TutorialIndex >= Enum.GetValues(typeof(Tutorials)).Cast<int>().Max())
        {
            XdevsSplashScreen.SetActiveWaitingIndicator(true);

            ThirdPartyAdsManager.Show(
                targetPlace: AdsShowingMode.AfterBattle,
                closingCallback: passingAfterBattleScreen);
        }
        else
        {
            passingAfterBattleScreen();
        }

        #endregion
    }

    private void ProfileLoadedFromServer(EventId id, EventInfo info)
    {
        ProfileInfo.HinduBugFix();
        ShowUserInfo();
    }

    void Update()
    {
        if (XDevs.Input.GetButtonDown("Back") &&
            GUIPager.ActivePage != "MainMenu" &&
            GUIPager.ActivePage != "EnterName" &&
            GUIPager.ActivePage != "DailyBonus" &&
            GUIPager.ActivePage != "UpdateGame" &&
            !MessageBox.IsShown)
        {
            OnBackClick("btnBack");
        }
        else if (XDevs.Input.GetButtonDown("Back") && GUIPager.ActivePage == "MainMenu" && !SocialSettings.IsWebPlatform)
        {
            OnBackClick("btnBack");
        }
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
    }

    void OnApplicationPause(bool paused)
    {
        if (!paused)
        {
            return;
        }

        //Debug.LogError("App is paused.");
    }

    void OnDestroy()
    {
#if UNITY_EDITOR
        HelpTools.CleanSceneFromTextures();
#endif
        if (GameData.timeGettingError)
        {
            return;
        }

        GUIPager.OnPageChange -= OnPageChange;

        OnTimerTick = null;

        ProfileInfo.lastVisit = GameData.CurrentTimeStamp;

        firstHangarEnter = false;

        Dispatcher.Unsubscribe(EventId.ProfileInfoLoadedFromServer, ProfileLoadedFromServer);
        Dispatcher.Unsubscribe(EventId.MessageBoxChangeVisibility, OnMessageBoxChangeVisibility);
        Dispatcher.Unsubscribe(EventId.OnLanguageChange, OnLanguageChange);

        BattleConnectManager.ClearPhotonMessageTargets();
        Instance = null;
        ObscuredPrefs.onAlterationDetected -= OnObscuredPrefsAlteration;
    }
    public static void HangarReenter() { firstHangarEnter = true; }

    /*    PUBLIC SECTION    */
    public static void CheckForNewDay(double timeStamp)
    {
        if (GameData.CorrectedCurrentTimeStamp >= ProfileInfo.nextDayServerTime || !ProfileInfo.dailyBonusIsObtained)
        {
            DailyBonus.ShowDailyBonusPage();

            ProfileInfo.doubleExpVehicles.Clear();

            foreach (var vehicle in ProfileInfo.vehicleUpgrades)
            {
                ProfileInfo.doubleExpVehicles.Add(vehicle.Key);
                VehicleShop.Selectors[vehicle.Key].sprDoubleExp.SetActive(true);
            }
        }
    }

    //public static bool CheckPlaySecondDayInARaw()
    //{
    //    var timeDiff = MiscTools.TimestampToDate(GameData.CurrentTimeStamp) - MiscTools.TimestampToDate(ProfileInfo.nextDayServerTime);
    //    return timeDiff.TotalDays >= 0 && timeDiff.TotalDays <= 1;
    //}

    public void ShowUserInfo()
    {
        lblUserLevel.text = ProfileInfo.Level.ToString();
        lblVehicleName.text = Shop.CurrentVehicle != null ?
            ((string)Shop.CurrentVehicle.Info.vehicleName).Replace("_", " ") : "";
        lblUserName.text = ProfileInfo.PlayerName;
        lblGold.text = ProfileInfo.Gold.ToString("N0", GameData.instance.cultureInfo.NumberFormat);
        lblSilver.text = ProfileInfo.Silver.ToString("N0", GameData.instance.cultureInfo.NumberFormat);
        userLevelBar.Percentage = ((float)ProfileInfo.Experience - ProfileInfo.PrevExperience) / (ProfileInfo.NextExperience - ProfileInfo.PrevExperience);

        if (experienceLabel != null)
        {
            experienceLabel.text = ProfileInfo.Experience.ToString("N0", GameData.instance.cultureInfo.NumberFormat);
        }
    }

    public static DateTime UnixTimeStampToDateTime(double unixTimeStamp)
    {
        // Unix timestamp is seconds past epoch.
        DateTime dtDateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
        dtDateTime = dtDateTime.AddSeconds(unixTimeStamp);

        return dtDateTime;
    }

    public static double DateTimeToUnixTimeStamp(DateTime dateTime)
    {
        DateTime defaultTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
        TimeSpan difference = dateTime.ToUniversalTime() - defaultTime;

        return Math.Floor(difference.TotalSeconds);
    }

    public void EnterBattle(GameManager.MapId map)
    {
        if (isBattleEntering)
        {
            return;
        }

        int fuelRequired = 0;
        if (!ProfileInfo.IsBattleTutorial)
        {
            MapSelector mapSelector = MapFramesCreator.GetMapItemById(map);

            if (ProfileInfo.lastMapId != (int)GameManager.MapId.random_map)
            {
                fuelRequired = mapSelector == null ? 0 : mapSelector.FuelRequired;
            }
            else
            {
                mapSelector = MapFramesCreator.GetMapItemById(GameManager.MapId.random_map);
                fuelRequired = mapSelector == null ? 0 : mapSelector.FuelRequired;
            }
        }

        if (ProfileInfo.Fuel < fuelRequired)
        {
            RefillGasTank.instance.ShowRefillGasTankWindow();
            Dispatcher.Send(EventId.VoiceRequired, new EventInfo_I((int)VoiceEventKey.NotEnoughFuel));
            return;
        }
        HangarController.Instance.PlaySound(HangarController.Instance.toBattleSound);
        isBattleEntering = true;

        ProfileInfo.FuelRequired = fuelRequired;

        StartCoroutine(BattleEntering(map));
    }

    public void SetActiveDoubleExpText(int currentVehicleId)
    {
        doubleExpWrapper.SetActive(ProfileInfo.doubleExpVehicles.Contains(currentVehicleId));
    }

    public void RecalcMaxStats(Shop.ItemType itemType = (Shop.ItemType.Vehicle | Shop.ItemType.Module))
    {
        ArmorMax = new Dictionary<int, float>();
        DamageMax = new Dictionary<int, float>();
        RocketDamageMax = new Dictionary<int, float>();
        ROFMax = new Dictionary<int, float>();
        IRCMROFMax = new Dictionary<int, float>();
        SpeedMax = new Dictionary<int, float>();
        ReloadMax = new Dictionary<int, float>();
        MagazineMax = new Dictionary<int, float>();

        Dictionary<int, List<HangarVehicle>> hangarVehiclesToGroups = VehicleShop.Selectors
            .GroupBy(vehicleSelector => (int)vehicleSelector.Value.UserVehicle.Info.vehicleGroup)
            .ToDictionary(
                group => group.Key,
                group => group.ToList()
                    .Select(vehicleSelector => vehicleSelector.Value.UserVehicle.HangarVehicle)
                    .ToList());

        foreach (KeyValuePair<int, List<HangarVehicle>> hangarVehiclesToGroup in hangarVehiclesToGroups)
        {
            float baseArmorMax = 0;
            float baseDamageMax = 0;
            float baseRocketDamageMax = 0;
            float baseROFMax = 0;
            float baseIRCMROFMax = 0;
            float baseSpeedMax = 0;
            float baseReloadMax = 0;
            float baseMagazineMax = 0;



            #region старый тип расчёта максимальных статов

            //if ((Shop.ItemType.Vehicle & itemType) != Shop.ItemType.None)
            //{
            //    baseArmorMax = SafeLinq.Max(hangarVehiclesToGroup.Value.Select(hangarVehicle => (float)hangarVehicle.Info.baseArmor));
            //    baseDamageMax = SafeLinq.Max(hangarVehiclesToGroup.Value.Select(hangarVehicle => (float)hangarVehicle.Info.baseDamage));
            //    baseRocketDamageMax = SafeLinq.Max(hangarVehiclesToGroup.Value.Select(hangarVehicle => (float)hangarVehicle.Info.baseRocketDamage));
            //    baseROFMax = SafeLinq.Max(hangarVehiclesToGroup.Value.Select(hangarVehicle => (float)hangarVehicle.Info.baseROF));
            //    baseIRCMROFMax = SafeLinq.Max(hangarVehiclesToGroup.Value.Select(hangarVehicle => (float)hangarVehicle.Info.baseIRCMROF));
            //    baseSpeedMax = SafeLinq.Max(hangarVehiclesToGroup.Value.Select(hangarVehicle => (float)hangarVehicle.Info.baseSpeed));
            //    baseReloadMax = SafeLinq.Max(hangarVehiclesToGroup.Value.Select(hangarVehicle => (float)hangarVehicle.Info.baseReloadTime));
            //    baseMagazineMax = SafeLinq.Max(hangarVehiclesToGroup.Value.Select(hangarVehicle => (float)hangarVehicle.Info.baseMagazine));
            //}

            //float armorMax = baseArmorMax;
            //float damageMax = baseDamageMax;
            //float rocketDamageMax = baseRocketDamageMax;
            //float rofMax = baseROFMax;
            //float ircmRofMax = baseIRCMROFMax;
            //float speedMax = baseSpeedMax;
            //float reloadMax = baseReloadMax;
            //float magazineMax = baseMagazineMax;

            //if ((Shop.ItemType.Module & itemType) != Shop.ItemType.None)
            //{
            //    List<UserVehicle> fullUpgradedVehicles
            //        = hangarVehiclesToGroup.Value.Select(
            //            hangarVehicle => new UserVehicle(
            //                hangarVehicle.Info,
            //                hangarVehicle,
            //                VehicleUpgrades.GetFullModuleUpgrades(hangarVehicle.Info))).ToList();

            //    armorMax = SafeLinq.Max(fullUpgradedVehicles.Select(upgradedVehicle => upgradedVehicle.Armor));
            //    damageMax = SafeLinq.Max(fullUpgradedVehicles.Select(upgradedVehicle => upgradedVehicle.Damage));
            //    rocketDamageMax = SafeLinq.Max(fullUpgradedVehicles.Select(upgradedVehicle => upgradedVehicle.RocketDamage));
            //    rofMax = SafeLinq.Max(fullUpgradedVehicles.Select(upgradedVehicle => upgradedVehicle.RoF));
            //    ircmRofMax = SafeLinq.Max(fullUpgradedVehicles.Select(upgradedVehicle => upgradedVehicle.IRCMRoF));
            //    speedMax = SafeLinq.Max(fullUpgradedVehicles.Select(upgradedVehicle => upgradedVehicle.Speed));
            //    reloadMax = SafeLinq.Max(fullUpgradedVehicles.Select(upgradedVehicle => upgradedVehicle.ReloadSpeed));
            //    magazineMax = SafeLinq.Max(fullUpgradedVehicles.Select(upgradedVehicle => upgradedVehicle.Magazine));

            //}

            //if ((Shop.ItemType.Pattern & itemType) != Shop.ItemType.None)
            //{
            //    armorMax = armorMax +
            //               SafeLinq.Max(
            //                   PatternShop.Selectors.Select(
            //                       patternSelector => (float)patternSelector.Value.GetItem<Bodykit>().armorGain));// * baseArmorMax;
            //    damageMax = damageMax +
            //                SafeLinq.Max(
            //                    PatternShop.Selectors.Select(
            //                        patternSelector => (float)patternSelector.Value.GetItem<Bodykit>().damageGain));// * baseDamageMax;
            //    rocketDamageMax = rocketDamageMax +
            //                      SafeLinq.Max(
            //                          PatternShop.Selectors.Select(
            //                              patternSelector =>
            //                                  (float)patternSelector.Value.GetItem<Bodykit>().rocketDamageGain));// * baseRocketDamageMax;
            //    rofMax = rofMax +
            //             SafeLinq.Max(
            //                 PatternShop.Selectors.Select(
            //                     patternSelector => (float)patternSelector.Value.GetItem<Bodykit>().rofGain));// * baseROFMax;
            //    ircmRofMax = ircmRofMax +
            //                 SafeLinq.Max(
            //                     PatternShop.Selectors.Select(
            //                         patternSelector => (float)patternSelector.Value.GetItem<Bodykit>().ircmRofGain));// * baseIRCMROFMax;
            //    speedMax = speedMax +
            //               SafeLinq.Max(
            //                   PatternShop.Selectors.Select(
            //                       patternSelector => (float)patternSelector.Value.GetItem<Bodykit>().speedGain));// * baseSpeedMax;
            //    reloadMax = reloadMax +
            //                SafeLinq.Max(
            //                    PatternShop.Selectors.Select(
            //                        patternSelector => (float)patternSelector.Value.GetItem<Bodykit>().reloadGain));// * baseReloadMax;
            //    magazineMax = magazineMax +
            //                  SafeLinq.Max(
            //                      PatternShop.Selectors.Select(
            //                          patternSelector => (float)patternSelector.Value.GetItem<Bodykit>().magazineGain));// * baseMagazineMax;
            //}

            //if ((Shop.ItemType.Decal & itemType) != Shop.ItemType.None)
            //{
            //    armorMax = armorMax +
            //               SafeLinq.Max(
            //                   DecalShop.Selectors.Select(
            //                       decalSelector => (float)decalSelector.Value.GetItem<Bodykit>().armorGain));// * baseArmorMax;
            //    damageMax = damageMax +
            //                SafeLinq.Max(
            //                    DecalShop.Selectors.Select(
            //                        decalSelector => (float)decalSelector.Value.GetItem<Bodykit>().damageGain));// * baseDamageMax;
            //    rocketDamageMax = rocketDamageMax +
            //                      SafeLinq.Max(
            //                          DecalShop.Selectors.Select(
            //                              decalSelector =>
            //                                  (float)decalSelector.Value.GetItem<Bodykit>().rocketDamageGain));// * baseRocketDamageMax;
            //    rofMax = rofMax +
            //             SafeLinq.Max(
            //                 DecalShop.Selectors.Select(
            //                     decalSelector => (float)decalSelector.Value.GetItem<Bodykit>().rofGain));// * baseROFMax;
            //    ircmRofMax = ircmRofMax +
            //                 SafeLinq.Max(
            //                     DecalShop.Selectors.Select(
            //                         decalSelector => (float)decalSelector.Value.GetItem<Bodykit>().ircmRofGain));// * baseIRCMROFMax;
            //    speedMax = speedMax +
            //               SafeLinq.Max(
            //                   DecalShop.Selectors.Select(
            //                       decalSelector => (float)decalSelector.Value.GetItem<Bodykit>().speedGain));// * baseSpeedMax;
            //    reloadMax = reloadMax +
            //                SafeLinq.Max(
            //                    DecalShop.Selectors.Select(
            //                        decalSelector => (float)decalSelector.Value.GetItem<Bodykit>().reloadGain));// * baseReloadMax;
            //    magazineMax = magazineMax +
            //                  SafeLinq.Max(
            //                      DecalShop.Selectors.Select(
            //                          decalSelector => (float)decalSelector.Value.GetItem<Bodykit>().magazineGain));// * baseMagazineMax;
            //}

            #endregion

            //if ((Shop.ItemType.Vehicle & itemType) != Shop.ItemType.None)
            //{
            //    baseArmorMax = SafeLinq.Max(hangarVehiclesToGroup.Value.Select(hangarVehicle => (float)hangarVehicle.Info.baseArmor));
            //    baseDamageMax = SafeLinq.Max(hangarVehiclesToGroup.Value.Select(hangarVehicle => (float)hangarVehicle.Info.baseDamage));
            //    baseRocketDamageMax = SafeLinq.Max(hangarVehiclesToGroup.Value.Select(hangarVehicle => (float)hangarVehicle.Info.baseRocketDamage));
            //    baseROFMax = SafeLinq.Max(hangarVehiclesToGroup.Value.Select(hangarVehicle => (float)hangarVehicle.Info.baseROF));
            //    baseIRCMROFMax = SafeLinq.Max(hangarVehiclesToGroup.Value.Select(hangarVehicle => (float)hangarVehicle.Info.baseIRCMROF));
            //    baseSpeedMax = SafeLinq.Max(hangarVehiclesToGroup.Value.Select(hangarVehicle => (float)hangarVehicle.Info.baseSpeed));
            //    baseReloadMax = SafeLinq.Max(hangarVehiclesToGroup.Value.Select(hangarVehicle => (float)hangarVehicle.Info.baseReloadTime));
            //    baseMagazineMax = SafeLinq.Max(hangarVehiclesToGroup.Value.Select(hangarVehicle => (float)hangarVehicle.Info.baseMagazine));
            //}

            //float armorMax = baseArmorMax;
            //float damageMax = baseDamageMax;
            //float rocketDamageMax = baseRocketDamageMax;
            //float rofMax = baseROFMax;
            //float ircmRofMax = baseIRCMROFMax;
            //float speedMax = baseSpeedMax;
            //float reloadMax = baseReloadMax;
            //float magazineMax = baseMagazineMax;

            //if ((Shop.ItemType.Module & itemType) != Shop.ItemType.None)
            //{
            //    List<UserVehicle> fullUpgradedVehicles
            //        = hangarVehiclesToGroup.Value.Select(
            //            hangarVehicle => new UserVehicle(
            //                hangarVehicle.Info,
            //                hangarVehicle,
            //                VehicleUpgrades.GetFullModuleUpgrades(hangarVehicle.Info))).ToList();

            //    armorMax = SafeLinq.Max(fullUpgradedVehicles.Select(upgradedVehicle => upgradedVehicle.Armor));
            //    damageMax = SafeLinq.Max(fullUpgradedVehicles.Select(upgradedVehicle => upgradedVehicle.Damage));
            //    rocketDamageMax = SafeLinq.Max(fullUpgradedVehicles.Select(upgradedVehicle => upgradedVehicle.RocketDamage));
            //    rofMax = SafeLinq.Max(fullUpgradedVehicles.Select(upgradedVehicle => upgradedVehicle.RoF));
            //    ircmRofMax = SafeLinq.Max(fullUpgradedVehicles.Select(upgradedVehicle => upgradedVehicle.IRCMRoF));
            //    speedMax = SafeLinq.Max(fullUpgradedVehicles.Select(upgradedVehicle => upgradedVehicle.Speed));
            //    reloadMax = SafeLinq.Max(fullUpgradedVehicles.Select(upgradedVehicle => upgradedVehicle.ReloadSpeed));
            //    magazineMax = SafeLinq.Max(fullUpgradedVehicles.Select(upgradedVehicle => upgradedVehicle.Magazine));

            //}

            //if ((Shop.ItemType.Pattern & itemType) != Shop.ItemType.None)
            //{
            //    armorMax = armorMax +
            //               SafeLinq.Max(
            //                   PatternPool.Instance.Items.Select(
            //                       patternSelector => (float)patternSelector.armorGain));// * baseArmorMax;
            //    damageMax = damageMax +
            //                SafeLinq.Max(
            //                    PatternPool.Instance.Items.Select(
            //                        patternSelector => (float)patternSelector.damageGain));// * baseDamageMax;
            //    rocketDamageMax = rocketDamageMax +
            //                      SafeLinq.Max(
            //                          PatternPool.Instance.Items.Select(
            //                              patternSelector =>
            //                                  (float)patternSelector.rocketDamageGain));// * baseRocketDamageMax;
            //    rofMax = rofMax +
            //             SafeLinq.Max(
            //                 PatternPool.Instance.Items.Select(
            //                     patternSelector => (float)patternSelector.rofGain));// * baseROFMax;
            //    ircmRofMax = ircmRofMax +
            //                 SafeLinq.Max(
            //                     PatternPool.Instance.Items.Select(
            //                         patternSelector => (float)patternSelector.ircmRofGain));// * baseIRCMROFMax;
            //    speedMax = speedMax +
            //               SafeLinq.Max(
            //                  PatternPool.Instance.Items.Select(
            //                       patternSelector => (float)patternSelector.speedGain));// * baseSpeedMax;
            //    reloadMax = reloadMax +
            //                SafeLinq.Max(
            //                    PatternPool.Instance.Items.Select(
            //                        patternSelector => (float)patternSelector.reloadGain));// * baseReloadMax;
            //    magazineMax = magazineMax +
            //                  SafeLinq.Max(
            //                     PatternPool.Instance.Items.Select(
            //                          patternSelector => (float)patternSelector.magazineGain));// * baseMagazineMax;
            //}

            //if ((Shop.ItemType.Decal & itemType) != Shop.ItemType.None)
            //{
            //    armorMax = armorMax +
            //               SafeLinq.Max(
            //                   DecalPool.Instance.Items.Select(
            //                       decalSelector => (float)decalSelector.armorGain));// * baseArmorMax;
            //    damageMax = damageMax +
            //                SafeLinq.Max(
            //                    DecalPool.Instance.Items.Select(
            //                        decalSelector => (float)decalSelector.damageGain));// * baseDamageMax;
            //    rocketDamageMax = rocketDamageMax +
            //                      SafeLinq.Max(
            //                          DecalPool.Instance.Items.Select(
            //                              decalSelector =>
            //                                  (float)decalSelector.rocketDamageGain));// * baseRocketDamageMax;
            //    rofMax = rofMax +
            //             SafeLinq.Max(
            //                 DecalPool.Instance.Items.Select(
            //                     decalSelector => (float)decalSelector.rofGain));// * baseROFMax;
            //    ircmRofMax = ircmRofMax +
            //                 SafeLinq.Max(
            //                     DecalPool.Instance.Items.Select(
            //                         decalSelector => (float)decalSelector.ircmRofGain));// * baseIRCMROFMax;
            //    speedMax = speedMax +
            //               SafeLinq.Max(
            //                   DecalPool.Instance.Items.Select(
            //                       decalSelector => (float)decalSelector.speedGain));// * baseSpeedMax;
            //    reloadMax = reloadMax +
            //                SafeLinq.Max(
            //                    DecalPool.Instance.Items.Select(
            //                        decalSelector => (float)decalSelector.reloadGain));// * baseReloadMax;
            //    magazineMax = magazineMax +
            //                  SafeLinq.Max(
            //                      DecalPool.Instance.Items.Select(
            //                          decalSelector => (float)decalSelector.magazineGain));// * baseMagazineMax;
            //}



            if ((Shop.ItemType.Vehicle & itemType) != Shop.ItemType.None)
            {
                baseArmorMax =
                    SafeLinq.Max(
                        hangarVehiclesToGroup.Value.Select(hangarVehicle => (float)hangarVehicle.Info.baseArmor));
                baseDamageMax =
                    SafeLinq.Max(
                        hangarVehiclesToGroup.Value.Select(hangarVehicle => (float)hangarVehicle.Info.baseDamage));
                baseRocketDamageMax =
                    SafeLinq.Max(
                        hangarVehiclesToGroup.Value.Select(hangarVehicle => (float)hangarVehicle.Info.baseRocketDamage));
                baseROFMax =
                    SafeLinq.Max(hangarVehiclesToGroup.Value.Select(hangarVehicle => (float)hangarVehicle.Info.baseROF));
                baseIRCMROFMax =
                    SafeLinq.Max(
                        hangarVehiclesToGroup.Value.Select(hangarVehicle => (float)hangarVehicle.Info.baseIRCMROF));
                baseSpeedMax =
                    SafeLinq.Max(
                        hangarVehiclesToGroup.Value.Select(hangarVehicle => (float)hangarVehicle.Info.baseSpeed));
                baseReloadMax =
                    SafeLinq.Max(
                        hangarVehiclesToGroup.Value.Select(hangarVehicle => (float)hangarVehicle.Info.baseReloadTime));
                baseMagazineMax =
                    SafeLinq.Max(
                        hangarVehiclesToGroup.Value.Select(hangarVehicle => (float)hangarVehicle.Info.baseMagazine));
            }

            float armorMax = baseArmorMax;
            float damageMax = baseDamageMax;
            float rocketDamageMax = baseRocketDamageMax;
            float rofMax = baseROFMax;
            float ircmRofMax = baseIRCMROFMax;
            float speedMax = baseSpeedMax;
            float reloadMax = baseReloadMax;
            float magazineMax = baseMagazineMax;

            if ((Shop.ItemType.Module & itemType) != Shop.ItemType.None)
            {
                List<UserVehicle> fullUpgradedVehicles
                    = hangarVehiclesToGroup.Value.Select(
                        hangarVehicle => new UserVehicle(
                            hangarVehicle.Info,
                            hangarVehicle,
                            VehicleUpgrades.GetFullModuleUpgrades(hangarVehicle.Info))).ToList();


                armorMax = SafeLinq.Max(fullUpgradedVehicles.Select(upgradedVehicle => upgradedVehicle.Armor)) +
                           SafeLinq.Max(
                               PatternPool.Instance.Items.Select(patternSelector => (float)patternSelector.armorGain)) * baseArmorMax +
                           SafeLinq.Max(DecalPool.Instance.Items.Select(decalSelector => (float)decalSelector.armorGain)) * baseArmorMax;

                damageMax = SafeLinq.Max(fullUpgradedVehicles.Select(upgradedVehicle => upgradedVehicle.Damage)) +
                            SafeLinq.Max(
                                PatternPool.Instance.Items.Select(patternSelector => (float)patternSelector.damageGain)) * baseDamageMax +
                            SafeLinq.Max(
                                DecalPool.Instance.Items.Select(decalSelector => (float)decalSelector.damageGain)) * baseDamageMax;

                rofMax = SafeLinq.Max(fullUpgradedVehicles.Select(upgradedVehicle => upgradedVehicle.RoF)) +
                         SafeLinq.Max(
                             PatternPool.Instance.Items.Select(patternSelector => (float)patternSelector.rofGain)) * baseROFMax +
                         SafeLinq.Max(DecalPool.Instance.Items.Select(decalSelector => (float)decalSelector.rofGain)) * baseROFMax; ;

                speedMax = SafeLinq.Max(fullUpgradedVehicles.Select(upgradedVehicle => upgradedVehicle.Speed)) +
                           SafeLinq.Max(
                               PatternPool.Instance.Items.Select(patternSelector => (float)patternSelector.speedGain)) * baseSpeedMax +
                           SafeLinq.Max(DecalPool.Instance.Items.Select(decalSelector => (float)decalSelector.speedGain)) * baseSpeedMax;


                reloadMax = RELOAD_MAX;// SafeLinq.Max(fullUpgradedVehicles.Select(upgradedVehicle => upgradedVehicle.ReloadSpeed)) +
                //SafeLinq.Max(
                //    PatternPool.Instance.Items.Select(patternSelector => (float)patternSelector.reloadGain)) +
                //SafeLinq.Max(
                //    DecalPool.Instance.Items.Select(decalSelector => (float)decalSelector.reloadGain));

                magazineMax = SafeLinq.Max(fullUpgradedVehicles.Select(upgradedVehicle => upgradedVehicle.Magazine)) +
                              SafeLinq.Max(
                                  PatternPool.Instance.Items.Select(
                                      patternSelector => (float)patternSelector.magazineGain)) +
                              SafeLinq.Max(
                                  DecalPool.Instance.Items.Select(decalSelector => (float)decalSelector.magazineGain));




                ArmorMax.Add(hangarVehiclesToGroup.Key, armorMax);
                DamageMax.Add(hangarVehiclesToGroup.Key, damageMax);
                RocketDamageMax.Add(hangarVehiclesToGroup.Key, rocketDamageMax);
                ROFMax.Add(hangarVehiclesToGroup.Key, rofMax);
                IRCMROFMax.Add(hangarVehiclesToGroup.Key, ircmRofMax);
                SpeedMax.Add(hangarVehiclesToGroup.Key, speedMax);
                ReloadMax.Add(hangarVehiclesToGroup.Key, reloadMax);
                MagazineMax.Add(hangarVehiclesToGroup.Key, magazineMax);
            }
        }
    }

    public void FillParameterDelta(DeltaProgressBar bar, float max, float prim, float sec)
    {


        if (bar == null)
        {
            return;
        }

        bar.Max = max;
        bar.PrimaryValue = prim;
        bar.SecondaryValue = sec;

        bar.Repaint();
    }

    public void PlaySound(AudioClip clip)
    {
        if (clip != null)
        {
            AudioDispatcher.PlayClip(clip);
        }
    }

    public void OnVehicleShopEnter(string buttonName)
    {
        GUIPager.SetActivePage("VehicleShopWindow");
        Dispatcher.Send(EventId.VoiceRequired, new EventInfo_I((int)VoiceEventKey.VehicleShopEnter));
    }

    private void OnArmoryEnter(string buttonName)
    {
        GUIPager.SetActivePage("Armory");
        Dispatcher.Send(EventId.VoiceRequired, new EventInfo_I((int)VoiceEventKey.ModuleShopEnter));
    }

    public void OnBackClick(string buttonName)
    {
        if (GUIPager.ActivePage == "MainMenu" && !SocialSettings.IsWebPlatform && !isWaitingForSaving)
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
        else if (GUIPager.ActivePage == "Settings")//Отменяем изменения если вышли по кнопке назад, а не ОК
        {
            Settings.Instance.ResetSettings();
        }

        GUIPager.Back();

        PlaySound(backSound);
    }

    public void OnCheckBox(tk2dUIItem boxItem)
    {
        //Debug.Log("checkBox " + boxItem.name);
        PlaySound(checkBoxSound);
    }

    public void NavigateBuySilver(string buttonName)
    {
        PlaySound(nextSound);
    }
    public void NavigateBuyGold(string buttonName)
    {
        PlaySound(nextSound);
    }
    public void NavigateBuyKit(string buttonName)
    {
        PlaySound(nextSound);
    }

    public void OnBuyVip(object desiredItem)
    {
        PlaySound(nextSound);
        VipShop(ref desiredItem);
    }

    /// <summary>
    /// Пришлось сделать такую функцию вместо NavigateToVipShop, т.к. NavigateToVipShop должен иметь булевский аргумент.
    /// </summary>
    private void ToVipShopButtonsClickListener(string btnName)
    {
        NavigateToVipShop(showMessageBox: false);
    }

    public void NavigateToVipShop(bool showMessageBox = false, Action negativeCallback = null, Action positiveCallback = null)
    {
        if (RightPanel.Instance.rightPanel.scrollableArea.IsSwipeScrollingInProgress || !VipShopPage.Instance.IsInitialized)
            return;

        if (showMessageBox)
        {
            MessageBox.Show(
                _type: MessageBox.Type.Question,
                _text: Localizer.GetText("onlyForVip"),
                _callBack: answer =>
                {
                    if (answer == MessageBox.Answer.Yes)
                    {
                        if (positiveCallback != null)
                            positiveCallback();

                        PassEnterToVipShop();
                    }
                    else if (answer == MessageBox.Answer.No && negativeCallback != null)
                    {
                        negativeCallback();
                    }
                });
        }
        else
            PassEnterToVipShop();
    }

    private void VipShop(ref object desiredItem)
    {
        if (RightPanel.Instance.rightPanel.scrollableArea.IsSwipeScrollingInProgress || !VipShopPage.Instance.IsInitialized)
        {
            return;
        }

        if (desiredItem == null || desiredItem is string)
        {
            PassEnterToVipShop();
            return;
        }

        string itemTypeAvailability = String.Empty;

        if (desiredItem is UserVehicle)
        {
            itemTypeAvailability = Localizer.GetText("thisTankAvailable");
        }

        if (desiredItem is Pattern)
        {
            itemTypeAvailability = Localizer.GetText("thisPatternAvailable");
        }

        if (desiredItem is Decal)
        {
            itemTypeAvailability = Localizer.GetText("thisDecalAvailable");
        }

        MessageBox.Show(
            _type: MessageBox.Type.Question,
            _text: string.Format("{0} {1}", itemTypeAvailability, Localizer.GetText("onlyForVip")),
            _callBack: answer =>
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

                if (PushNotifications.Pusher != null)
                {
                    PushNotifications.OnPushesScheduled += GameData.QuitGame;
                }
                else
                {
                    GameData.QuitGame();
                }

                PushNotifications.Instance.ScheduleLocalNotifications();
            };

            ThirdPartyAdsManager.Show(
                targetPlace: AdsShowingMode.OnQuit,
                closingCallback: passingApplicationQuit);
        }
        else
        {
            Application.CancelQuit();
            GUIPager.Back();
        }
    }

    private void OnSettingsClick(string buttonName)
    {
        PlaySound(nextSound);
        GUIPager.SetActivePage("Settings", true, true);
        Settings.SetFirstSettingsTab();
    }

    private void PassEnterToVipShop()
    {
        GUIPager.SetActivePage("VipAccountShop");
        // Set back bank page to gold (because vip tab is empty).
        bankCurrencyToggle.SelectedIndex = (int)ProfileInfo.PriceCurrency.Gold;
    }

    /*** BANK ***/
    public void GoToBank(Bank.Tab tab, bool addToHistory = true, bool voiceRequired = false)
    {
        if (!Bank.Instance.IsInitialized)
        {
#if !(UNITY_WEBPLAYER || UNITY_WEBGL)
            //
#endif
            Debug.LogError("Bank is not initialized;");
            return;
        }

        GUIPager.SetActivePage("Bank", addToHistory, true, voiceRequired ? (int)VoiceEventKey.NotEnoughMoney : -1);
        Dispatcher.Send(EventId.BankOpened, null);
        bankCurrencyToggle.SelectedIndex = (int)tab;
    }

    public void GoToBank(ProfileInfo.PriceCurrency currency, bool addToHistory = true, bool voiceRequired = false)
    {
        GoToBank(Bank.CurrencyToTab(currency), addToHistory, voiceRequired);
    }

    private void OnBankClick(string buttonName)
    {
        PlaySound(nextSound);
        GoToBank(Bank.Tab.Gold);
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
        Dispatcher.Send(EventId.VoiceRequired, new EventInfo_I((int)VoiceEventKey.PatternShopEnter));
    }

    private static void OnDecalsShopClick(string buttonName)
    {
        GUIPager.SetActivePage("DecalShop");
        HangarCameraController.Instance.OnWeaponShopOpen();
        Dispatcher.Send(EventId.VoiceRequired, new EventInfo_I((int)VoiceEventKey.DecalShopEnter));
    }

    private static void OnBattleClick(string buttonName)
    {
        GUIPager.SetActivePage("MapSelection", true, true);
        Dispatcher.Send(EventId.VoiceRequired, new EventInfo_I((int)VoiceEventKey.MapSelection));
    }

    private void OnBattleQuestsClick(string buttonName)
    {
        if (!ObscuredPrefs.GetBool("DidFirstBattlePlayed") && !Debug.isDebugBuild)
        {
            GameManager.MapId mapId = MapFramesCreator.AvailableMaps.FirstOrDefault();
            ObscuredPrefs.SetBool("DidFirstBattlePlayed", true);
            Debug.LogFormat("Go to map {0}", mapId);
            EnterBattle(mapId);
            return;
        }

        if (QuestsInfo.IsAllQuestsCompleted)
        {
            GUIPager.SetActivePage("MapSelection", true, true);
            Dispatcher.Send(EventId.VoiceRequired, new EventInfo_I((int)VoiceEventKey.MapSelection));
        }
        else
        {
            GUIPager.SetActivePage("Quests", true, !GameData.IsGame(Game.CodeOfWar));
            QuestsUI.Instance.MarkerizeQuestList();
            Dispatcher.Send(EventId.VoiceRequired, new EventInfo_I((int)VoiceEventKey.CurrentQuests));
        }
    }

    private void HangarTimerTick()
    {
        double tickDeltaTime = GameData.instance.GetCorrectedTime() - lastTick;

        RefuelByTime(tickDeltaTime);

        lastTick = GameData.instance.GetCorrectedTime();

        Dispatcher.Send(EventId.HangarTimerTick, new EventInfo_I((int)lastTick));

        if (OnTimerTick != null)
        {
            OnTimerTick(GameData.CurrentTimeStamp);
        }
    }

    private void OnObscuredPrefsAlteration()
    {
        MessageBox.Show(MessageBox.Type.Info, "Data modification detected");

        GameData.QuitGame();

        ShowUserInfo();

        VehicleShop.Instance.ShowVehicle(
            vehicleId: ProfileInfo.CurrentVehicle,
            showPrice: false,
            playSound: false);
    }

    private void OnPageChange(string from, string to)
    {
        if (to == "MainMenu")
        {
            GUIPager.PagesHistory.Clear();
            SetActiveDoubleExpText(ProfileInfo.currentVehicle);
            if (MessageBox.IsShown)
                ScoresController.Instance.SetActive(false);
        }

        if (!String.IsNullOrEmpty(from) && to == "MainMenu")
        {
            ModuleShop.Instance.CheckIfModuleUpgradePossible();
        }

        if ((from == "Bank" || from == "FreeTankDetails") && to == "MainMenu" && VehicleShop.Instance)
        {
            VehicleShop.Instance.ShowVehicle(
               vehicleId: Shop.CurrentVehicle.Info.id,
               showPrice: false,
               playSound: false);
        }
    }

    public void SetActionBoxType(ActionBoxType type)
    {
        installBox.SetActive(type == ActionBoxType.Install);
        installedBox.SetActive(type == ActionBoxType.Installed);

        SetCurrentBox(saleBuyingBox, type == ActionBoxType.Sale);
        SetCurrentBox(buyingBox, type == ActionBoxType.Buy);
        SetCurrentBox(saleRentBox, type == ActionBoxType.RentSale);
        SetCurrentBox(rentBox, type == ActionBoxType.Rent);

        buyingBox.gameObject.SetActive(type == ActionBoxType.Buy);
        deliverBox.gameObject.SetActive(type == ActionBoxType.Deliver);
        rentingBox.gameObject.SetActive(type == ActionBoxType.Renting);
        rentedBox.gameObject.SetActive(type == ActionBoxType.Rented);
        inaccessibleBox.SetActive(type == ActionBoxType.Inaccessible);
        if (comingSoonBox)
        {
            comingSoonBox.SetActive(type == ActionBoxType.ComingSoon);
        }
        recieveFreeTankBox.gameObject.SetActive(type == ActionBoxType.FreeTank);
    }

    private static void SetCurrentBox(HangarBuyingBox box, bool activate)
    {
        if (activate)
        {
            CurrentActionBox = box;
            box.gameObject.SetActive(true);
        }
        else
        {
            box.gameObject.SetActive(false);
        }
    }

    public void RefuelByTime(double timeInterval)
    {
        ProfileInfo.Fuel += timeInterval / GameData.refuellingTime;
        fuelBarManager.FilledCanAmount = (int)ProfileInfo.Fuel;
    }

    private void ShowCommonStat(tk2dUIItem item)
    {
        PlaySound(nextSound);
#if UNITY_WEBPLAYER || UNITY_WEBGL
        Statistics.Instance.ShowStatistics();
#else
        if (!GameData.IsGame(Game.IronTanks))
        {
            Statistics.Instance.ShowStatistics();
        }
        else
        {
            GUIPager.SetActivePage("CommonStat", true, true);
            Statistics.Instance.SetupButtons();
        }
#endif

    }

    private void OnMessageBoxChangeVisibility(EventId id, EventInfo info)
    {
        if (GUIPager.ActivePage == "Chat" && ((EventInfo_B)info).bool1)//Чтобы часть месседжбокса не перекрывалась маской для обрезки сообщений чата
        { GUIPager.SetActivePage("MainMenu"); }
    }

    private void OnLanguageChange(EventId id, EventInfo info)
    {
        float scale = Localizer.IsAsianFont(Localizer.Language) ? lblUserLevelChineseFontScale : lblUserLevelEuropianFontScale;//захардкодил китайский скейл, потому что китайский шрифт один на все проекты
        lblUserLevel.scale = new Vector3(scale, scale, 1);
    }

    private IEnumerator BattleEntering(GameManager.MapId _mapId)
    {
        PhotonNetwork.offlineMode = ProfileInfo.IsBattleTutorial;

        Dispatcher.Send(EventId.WentToBattle, null);

        BattleStatisticsManager.ResetBattleStats();

        fuelBarManager.FilledCanAmount = (int)ProfileInfo.Fuel;

        GameManager.SetMainVehicle(
            ProfileInfo.IsBattleTutorial
                ? Shop.GetVehicle(GameData.CurrentTutorialVehicleId)
                : Shop.CurrentVehicle);

        CodeStage.AntiCheat.Detectors.SpeedHackDetector.isRunning = false;

        XdevsSplashScreen.SetActive(
            en: true,
            showLabels: true, // Show advice.
            tex: "");  // Default texture – MobileScreen.

        // Чтобы индикатор вернулся в неудаляемый объект, а то в банке оставался и пипец - нуллреференс.
        XdevsSplashScreen.SetActiveWaitingIndicator(false);

        Action passingBattleLoading = () =>
        {
            Loading.loadScene(_mapId.ToString());

            isBattleEntering = false;

            //Application.LoadLevel(
            //    Application.CanStreamedLevelBeLoaded(selectedMapName + "_PC")
            //        ? selectedMapName + "_PC"
            //        : selectedMapName);

            //Dictionary<VehicleInfo.VehicleParameter, ObscuredFloat> realVehicleParameters = Shop.CurrentVehicle.GetRealParameters();

            //Http.Manager.ReportStats(
            //    location:   "battle",
            //    action:     "enter",
            //    query:      new Dictionary<string, string>
            //                {
            //                    { "experience", ProfileInfo.Experience.ToString() },
            //                    { "goldAmount", ProfileInfo.Gold.ToString() },
            //                    { "silverAmount", ProfileInfo.Silver.ToString() },
            //                    { "fuel", ProfileInfo.Fuel.ToString() },
            //                    { "tankId", Shop.CurrentVehicle.info.id.ToString() },
            //                    { "tankName", Shop.CurrentVehicle.info.name },
            //                    { "camoId", Shop.CurrentVehicle.Upgrades.CamouflageId.ToString() },
            //                    { "decalId", Shop.CurrentVehicle.Upgrades.DecalId.ToString() },
            //                    { "fairMaxArmor", realVehicleParameters[VehicleInfo.VehicleParameter.Armor].ToString() },
            //                    { "fairMaxDamage", realVehicleParameters[VehicleInfo.VehicleParameter.Damage].ToString() },
            //                    { "fairMaxSpeed", realVehicleParameters[VehicleInfo.VehicleParameter.Speed].ToString() },
            //                    { "fairMaxRof", realVehicleParameters[VehicleInfo.VehicleParameter.RoF].ToString() }
            //                });
        };

        // Save GameMode.
        ProfileInfo.SaveToServer();

        if (!ProfileInfo.IsBattleTutorial)
        {
            AudioClip enterSound = toBattleSoundTeamMode;
            // PlaySound(enterSound);
            yield return new WaitForSeconds(enterSound.length);
        }

        ThirdPartyAdsManager.Show(
            targetPlace: AdsShowingMode.BeforeBattle,
            closingCallback: passingBattleLoading);
    }

    public void OnArrowClick(tk2dUIItem arrowItem)
    {
        PlaySound(smallArrowSound);
    }
}