using System;
using System.Linq;
using CodeStage.AntiCheat.ObscuredTypes;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Disconnect;
using UnityEngine.SceneManagement;
using UnityEngine.Serialization;
using XDevs;

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
    public Renderer topGUIPanel;

    public DeltaProgressBar armorBar;
    public DeltaProgressBar attackBar;
    public DeltaProgressBar rocketAttackBar;
    public DeltaProgressBar speedBar;
    public DeltaProgressBar rofBar;
    public DeltaProgressBar ircmRofBar;
    public DeltaProgressBar magazineBar;
    public DeltaProgressBar dpmBar;

    public tk2dTextMesh lblUserLevel;
    public float lblUserLevelEuropianFontScale = 1;//в китайском языке цифры большие, приходится вручную скейлить (пока только в SpaceJet)
    public float lblUserLevelChineseFontScale = 1;
    public tk2dTextMesh lblUserName;
    public tk2dTextMesh lblVehicleName;
    public tk2dTextMesh lblSilver;
    public tk2dTextMesh lblGold;
    public ProgressBar userLevelBar;
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

    private bool isBattleEntering;
    private float lastTick;

    // Список всех акций.. особо не нужен (нужен только для того, чтобы достать время одного из офферов), но хз, вдруг понадобится.
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
    public Dictionary<int, float> RoFMax { get; private set; }
    public Dictionary<int, float> MagazineMax { get; private set; }
    public Dictionary<int, float> DPMMax { get; private set; }

    public Camera GuiCamera { get; private set; }

    public tk2dCamera Tk2dGuiCamera { get; private set; }

    public static HangarBuyingBox CurrentActionBox { get; private set; }

    /*    UNITY SECTION    */

    void Awake()
    {
        if (Tk2dGuiCamera == null)
            Tk2dGuiCamera = transform.Find("Hangar2D").GetComponent<tk2dCamera>();

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
        Messenger.Subscribe(EventId.ProfileInfoLoadedFromServer, ProfileLoadedFromServer);
        Messenger.Subscribe(EventId.MessageBoxChangeVisibility, OnMessageBoxChangeVisibility);
        Messenger.Subscribe(EventId.OnLanguageChange, OnLanguageChange);
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

        Messenger.Send(EventId.AfterLocalizationLoad, new EventInfo_SimpleEvent());

        yield return null;
        yield return null;

        if (firstHangarEnter)
        {
            GUIPager.SetActivePage("MainMenu");

            // Profile crash checking.
            if (ProfileInfo.CurrentVehicle == 0 && ProfileInfo.Silver == 0)
                ProfileInfo.LoadDefaults();

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
                Messenger.Send(EventId.GameUpdateRequired, null);

                // Даём возможность аналитике отправиться?..
                yield return new WaitForSeconds(1f);

                XdevsSplashScreen.SetActive(false);
                XdevsSplashScreen.SetActiveWaitingIndicator(false);

                yield break;
            }
        }

        RefuelByTime(GameData.CurrentTimeStamp - ProfileInfo.lastVisit);

        fuelBarManager.TotalCanAmount = ProfileInfo.MaxFuel;

        ShopManager.Instance.Load();

        Messenger.Send(EventId.GameModeChanged, new EventInfo_SimpleEvent());

        this.InvokeRepeating(HangarTimerTick, 0, 1);

        //if (firstHangarEnter)
        //{
        //	string filename = GameData.IsGame(Game.IronTanks) ? "TanksDB.json" : "TanksDB_Team.json";
        //	HelpTools.ImportLocalDB(Path.Combine(Application.streamingAssetsPath, filename));
        //}

        GUIController.ListenButtonClick("btnShopWindow", OnVehicleShopEnter);
        GUIController.ListenButtonClick("btnArmory", OnArmoryEnter);
        GUIController.ListenButtonClick("btnBack", OnBackClick);
        GUIController.ListenButtonClick("btnSettings", OnSettingsClick);
        GUIController.ListenButtonClick("btnBank", OnBankClick);
        GUIController.ListenButtonClick("btnToBattle", OnBattleQuestsClick);
        GUIController.ListenButtonClick("btnQuestsOk", OnBattleClick);
        GUIController.ListenButtonClick("btnPattern", OnPatternShopClick);
        GUIController.ListenButtonClick("btnDecal", OnDecalsShopClick);
        //GUIController.ListenButtonClick("btnSale", SpecialOffer.OnSpecialOfferClicked);
        GUIController.ListenButtonClick("btnRecieveFreeTank", VehicleOffersController.OnRecieveFreeVehicleClick);
        GUIController.ListenButtonClick("btnVipShop", ToVipShopButtonsClickListener);
        GUIController.ListenButtonClick("btnBuyVip", ToVipShopButtonsClickListener);

        ProfileInfo.HinduBugFix();

        ShowUserInfo();

        hasDoubleExpTanksIndicator.SetActive(ProfileInfo.doubleExpVehicles.Count > 0);

        IsInitialized = true;

        allOffersList.Clear();

        //Закомментировал, т.к. есть правильная аутентификация в Achievments. Здесь запускать ее нельзя. Илья. 12.10.2016
        //GPGSWrapper.SocialAuthenticatation(
        //    success =>
        //    {
        //        if (success)
        //            Debug.LogFormat(
        //                "Authenticatation for GPG user \"{0}\" succeed!",
        //                Social.localUser.userName);
        //    });

        Messenger.Send(EventId.AfterHangarInit, new EventInfo_SimpleEvent());

        OnTimerTick += timeStamp => CheckForNewDay(timeStamp);
        OnTimerTick += QuestsUI.SetQuestsTimer;
        OnTimerTick += FriendsManager.UpdateFriends;
        OnTimerTick += CheckForDiscountsStates;

        if (!firstHangarEnter)
            ProfileInfo.SaveToServer();

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
            Messenger.Send(EventId.OnReadyToStartWindowsQueue, null);

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

            Messenger.Send(EventId.OnReadyToStartWindowsQueue, null);
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

    void ProfileLoadedFromServer(EventId id, EventInfo info)
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
            return;

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

        Messenger.Unsubscribe(EventId.ProfileInfoLoadedFromServer, ProfileLoadedFromServer);
        Messenger.Unsubscribe(EventId.MessageBoxChangeVisibility, OnMessageBoxChangeVisibility);
        Messenger.Unsubscribe(EventId.OnLanguageChange, OnLanguageChange);

        BattleConnectManager.ClearPhotonCaches();
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
            experienceLabel.text = ProfileInfo.Experience.ToString("N0", GameData.instance.cultureInfo.NumberFormat);
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

    public void EnterBattle(GameManager.MapId mapId)
    {
        if (isBattleEntering)
            return;

        if (!GameData.allMapsDic.ContainsKey(mapId))//проверка до разименования рандомной карты
        {
            Debug.LogErrorFormat("The map {0} is not found in server data!", mapId);
            return;
        }

        int fuelRequired = GameData.allMapsDic[mapId].fuelRequired;

        if(mapId == GameManager.MapId.random_map)
        {
            mapId = GameData.availableMapsDic.Keys.ToArray().GetRandomItem();
            Debug.LogFormat("Go to map {0}, Chosen random map", mapId);
        }
        else
            Debug.LogFormat("Go to map {0}", mapId);

        if (!GameData.allMapsDic.ContainsKey(mapId))//проверка после разименования рандомной карты
        {
            Debug.LogErrorFormat("The map {0} is not found in server data!", mapId);
            return;
        }
        
        if (ProfileInfo.Fuel < fuelRequired)
        {
            RefillGasTank.instance.ShowRefillGasTankWindow();
            Messenger.Send(EventId.VoiceRequired, new EventInfo_I((int)VoiceEventKey.NotEnoughFuel));
            return;
        }

        isBattleEntering = true;

        ProfileInfo.FuelRequired = ProfileInfo.IsBattleTutorialCompleted ? fuelRequired : 0;

        StartCoroutine(BattleEntering(mapId));
    }

    public void SetActiveDoubleExpText(int currentVehicleId)
    {
        doubleExpWrapper.SetActive(ProfileInfo.doubleExpVehicles.Contains(currentVehicleId));
    }

    public void RecalcMaxStats(VehicleInfo.VehicleType forVehicleType, Shop.ItemType itemType = (Shop.ItemType.Vehicle | Shop.ItemType.Module))
    {
        ArmorMax = new Dictionary<int, float>();
        DamageMax = new Dictionary<int, float>();
        RocketDamageMax = new Dictionary<int, float>();
        RoFMax = new Dictionary<int, float>();
        SpeedMax = new Dictionary<int, float>();
        MagazineMax = new Dictionary<int, float>();
        DPMMax = new Dictionary<int, float>();

        Dictionary<int, List<UserVehicle>> userVehiclesToGroups
            = VehicleShop.Selectors
                .GroupBy(vehicleSelector => (int)vehicleSelector.Value.UserVehicle.Info.vehicleGroup)
                .ToDictionary(
                    group => group.Key,
                    group => group.ToList()
                                .Select(vehicleSelector => vehicleSelector.Value.UserVehicle)
                                .ToList());

        foreach (KeyValuePair<int, List<UserVehicle>> userVehiclesToGroup in userVehiclesToGroups)
        {
            float baseArmorMax = 0;
            float baseDamageMax = 0;
            float baseRoFMax = 0;
            float baseSpeedMax = 0;
            float baseMagazineMax = 0;
            float baseDPMMax = 0;

            if ((Shop.ItemType.Vehicle & itemType) != Shop.ItemType.None)
            {
                baseArmorMax = SafeLinq.Max(userVehiclesToGroup.Value.Select(hangarVehicle => (float)hangarVehicle.Info.baseArmor));

                baseDamageMax = SafeLinq.Max(
                    userVehiclesToGroup.Value
                        .Where(hangarVehicle => hangarVehicle.Info.vehicleType == forVehicleType)
                        .Select(hangarVehicle => (float)hangarVehicle.Info.baseDamage));

                baseRoFMax = SafeLinq.Max(
                    userVehiclesToGroup.Value
                        .Where(hangarVehicle => hangarVehicle.Info.vehicleType == forVehicleType)
                        .Select(hangarVehicle => (float)hangarVehicle.Info.baseRoF));

                baseSpeedMax = SafeLinq.Max(userVehiclesToGroup.Value.Select(hangarVehicle => (float)hangarVehicle.Info.baseSpeed));
                baseMagazineMax = SafeLinq.Max(userVehiclesToGroup.Value.Select(hangarVehicle => (float)hangarVehicle.Info.baseMagazine));
                baseDPMMax = SafeLinq.Max(
                    userVehiclesToGroup.Value.Select(
                        hangarVehicle => 
                            (float)UserVehicle.CalcDPM(
                                damage: hangarVehicle.Info.baseDamage, 
                                rof: hangarVehicle.Info.baseRoF,
                                vehicleType: hangarVehicle.Info.vehicleType)));
            }

            float armorMax = baseArmorMax;
            float damageMax = baseDamageMax;
            float rofMax = baseRoFMax;
            float speedMax = baseSpeedMax;
            float magazineMax = baseMagazineMax;
            float dpmMax = baseDPMMax;

            if ((Shop.ItemType.Module & itemType) != Shop.ItemType.None)
            {
                List<UserVehicle> fullUpgradedVehicles
                    = userVehiclesToGroup.Value.Select(
                        userVehicle => new UserVehicle(
                    userVehicle.Info,
                    VehicleUpgrades.GetFullModuleUpgrades(userVehicle.Info))).ToList();

                armorMax = SafeLinq.Max(fullUpgradedVehicles.Select(upgradedVehicle => upgradedVehicle.Armor));

                damageMax = SafeLinq.Max(
                    fullUpgradedVehicles
                        .Where(upgradedVehicle => upgradedVehicle.Info.vehicleType == forVehicleType)
                        .Select(upgradedVehicle => upgradedVehicle.Damage));

                rofMax = SafeLinq.Max(
                    fullUpgradedVehicles
                        .Where(upgradedVehicle => upgradedVehicle.Info.vehicleType == forVehicleType)
                        .Select(upgradedVehicle => upgradedVehicle.RoF));

                speedMax = SafeLinq.Max(fullUpgradedVehicles.Select(upgradedVehicle => upgradedVehicle.Speed));
                magazineMax = SafeLinq.Max(fullUpgradedVehicles.Select(upgradedVehicle => upgradedVehicle.Magazine));
                dpmMax = SafeLinq.Max(
                    fullUpgradedVehicles.Select(
                        upgradedVehicle => (float)UserVehicle.CalcDPM(
                            damage: upgradedVehicle.Damage,
                            rof: upgradedVehicle.RoF,
                            vehicleType: upgradedVehicle.Info.vehicleType)));
            }

            if ((Shop.ItemType.Pattern & itemType) != Shop.ItemType.None)
            {
                armorMax = armorMax + SafeLinq.Max(PatternShop.Selectors.Select(patternSelector => (float)patternSelector.Value.GetItem<Bodykit>().armorGain)) * baseArmorMax;
                damageMax = damageMax + SafeLinq.Max(PatternShop.Selectors.Select(patternSelector => (float)patternSelector.Value.GetItem<Bodykit>().damageGain)) * baseDamageMax;
                rofMax = rofMax + SafeLinq.Max(PatternShop.Selectors.Select(patternSelector => (float)patternSelector.Value.GetItem<Bodykit>().rofGain)) * baseRoFMax;
                speedMax = speedMax + SafeLinq.Max(PatternShop.Selectors.Select(patternSelector => (float)patternSelector.Value.GetItem<Bodykit>().speedGain)) * baseSpeedMax;
                magazineMax = magazineMax + SafeLinq.Max(PatternShop.Selectors.Select(patternSelector => (float)patternSelector.Value.GetItem<Bodykit>().magazineGain)) * baseMagazineMax;
                dpmMax = 
                    UserVehicle.CalcDPM(
                        damage: damageMax,
                        rof: rofMax,
                        vehicleType: Shop.CurrentVehicle.Info.vehicleType);
            }

            if ((Shop.ItemType.Decal & itemType) != Shop.ItemType.None)
            {
                armorMax = armorMax + SafeLinq.Max(DecalShop.Selectors.Select(decalSelector => (float)decalSelector.Value.GetItem<Bodykit>().armorGain)) * baseArmorMax;
                damageMax = damageMax + SafeLinq.Max(DecalShop.Selectors.Select(decalSelector => (float)decalSelector.Value.GetItem<Bodykit>().damageGain)) * baseDamageMax;
                rofMax = rofMax + SafeLinq.Max(DecalShop.Selectors.Select(decalSelector => (float)decalSelector.Value.GetItem<Bodykit>().rofGain)) * baseRoFMax;
                speedMax = speedMax + SafeLinq.Max(DecalShop.Selectors.Select(decalSelector => (float)decalSelector.Value.GetItem<Bodykit>().speedGain)) * baseSpeedMax;
                magazineMax = magazineMax + SafeLinq.Max(DecalShop.Selectors.Select(decalSelector => (float)decalSelector.Value.GetItem<Bodykit>().magazineGain)) * baseMagazineMax;
                dpmMax =
                    UserVehicle.CalcDPM(
                        damage: damageMax,
                        rof: rofMax,
                        vehicleType: Shop.CurrentVehicle.Info.vehicleType);
            }

            ArmorMax.Add(userVehiclesToGroup.Key, armorMax);
            DamageMax.Add(userVehiclesToGroup.Key, damageMax);
            RoFMax.Add(userVehiclesToGroup.Key, rofMax);
            SpeedMax.Add(userVehiclesToGroup.Key, speedMax);
            MagazineMax.Add(userVehiclesToGroup.Key, magazineMax);
            DPMMax.Add(userVehiclesToGroup.Key, dpmMax);
        }
    }

    public void FillParameterDelta(DeltaProgressBar bar, float max, float prim, float sec)
    {
        if (bar == null)
            return;

        bar.Max = max;
        bar.PrimaryValue = prim;
        bar.SecondaryValue = sec;

        bar.Repaint();
    }

    public void PlayGUISound(AudioClip clip)
    {
        if (clip != null)
            AudioDispatcher.PlayClip(clip, false, AudioPlayer.Channel.GUI);
    }

    public void OnVehicleShopEnter(string buttonName)
    {
        GUIPager.SetActivePage("VehicleShopWindow");
        Messenger.Send(EventId.VoiceRequired, new EventInfo_I((int)VoiceEventKey.VehicleShopEnter));
    }

    private void OnArmoryEnter(string buttonName)
    {
        HangarCameraController.Instance.FindCamLookPoints();
        GUIPager.SetActivePage("Armory");
        Messenger.Send(EventId.VoiceRequired, new EventInfo_I((int)VoiceEventKey.ModuleShopEnter));
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

            PlayGUISound(backSound);

            return;
        }
        else if (GUIPager.ActivePage == "Settings")//Отменяем изменения если вышли по кнопке назад, а не ОК
        {
            Settings.Instance.ResetSettings();
        }

        GUIPager.Back();

        PlayGUISound(backSound);
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
        if (!VipManager.Instance.IsInitialized)
            return;

        if (showMessageBox)
        {
            MessageBox.Show(
                _type: MessageBox.Type.Question,
                _text: Localizer.GetText("allOnlyForVip"),
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

                Instance.StartCoroutine(PushNotifications.Instance.SettingLocalNotifications());
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

    private static void OnSettingsClick(string buttonName)
    {
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
            Debug.LogError("!Bank.Instance.IsInitialized! return;");
            return;
        }

        GUIPager.SetActivePage("Bank", addToHistory, true, voiceRequired ? (int)VoiceEventKey.NotEnoughMoney : -1);

        bankCurrencyToggle.SelectedIndex = (int)tab;
    }

    public void GoToBank(ProfileInfo.PriceCurrency currency, bool addToHistory = true, bool voiceRequired = false)
    {
        GoToBank(Bank.CurrencyToTab(currency), addToHistory, voiceRequired);
    }

    private void OnBankClick(string buttonName)
    {
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
        Messenger.Send(EventId.VoiceRequired, new EventInfo_I((int)VoiceEventKey.PatternShopEnter));
    }

    private static void OnDecalsShopClick(string buttonName)
    {
        GUIPager.SetActivePage("DecalShop");
        Messenger.Send(EventId.VoiceRequired, new EventInfo_I((int)VoiceEventKey.DecalShopEnter));
    }

    private static void OnBattleClick(string buttonName)
    {
        GUIPager.SetActivePage("MapSelection", true, true);
        Messenger.Send(EventId.VoiceRequired, new EventInfo_I((int)VoiceEventKey.MapSelection));
    }

    private void OnBattleQuestsClick(string buttonName)
    {
        if (!ObscuredPrefs.GetBool("DidFirstBattlePlayed") && !Debug.isDebugBuild)
        {
            ObscuredPrefs.SetBool("DidFirstBattlePlayed", true);
            EnterBattle(GameData.availableMapsDic.FirstOrDefault().Value.id);
            return;
        }

        if (QuestsInfo.IsAllQuestsCompleted)
        {
            GUIPager.SetActivePage("MapSelection", true, true);
            Messenger.Send(EventId.VoiceRequired, new EventInfo_I((int)VoiceEventKey.MapSelection));
        }
        else
        {
            GUIPager.SetActivePage("Quests", true, !GameData.IsGame(Game.Armada));
            QuestsUI.Instance.MarkerizeQuestList();
            Messenger.Send(EventId.VoiceRequired, new EventInfo_I((int)VoiceEventKey.CurrentQuests));
        }
    }

    private void HangarTimerTick()
    {
        double tickDeltaTime = GameData.instance.GetCorrectedTime() - lastTick;

        RefuelByTime(tickDeltaTime);

        lastTick = GameData.instance.GetCorrectedTime();

        Messenger.Send(EventId.HangarTimerTick, new EventInfo_I((int)lastTick));

        if (OnTimerTick != null)
            OnTimerTick(GameData.CurrentTimeStamp);
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
            ModuleShop.Instance.CheckIfModuleUpgradePossible();

        if ((from == "Bank" || from == "FreeTankDetails") && to == "MainMenu" && VehicleShop.Instance)
            VehicleShop.Instance.ShowVehicle(
                vehicleId: Shop.CurrentVehicle.Info.id,
                showPrice: false,
                playSound: false);
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
            comingSoonBox.SetActive(type == ActionBoxType.ComingSoon);
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
#if UNITY_WEBPLAYER || UNITY_WEBGL
        Statistics.Instance.ShowStatistics();
#else
        if (!GameData.IsGame(Game.IronTanks))
            Statistics.Instance.ShowStatistics();
        else
        {
            GUIPager.SetActivePage("CommonStat", true, true);
            Statistics.Instance.SetupButtons();
        }
#endif
        PlayGUISound(backSound);
    }

    private void OnMessageBoxChangeVisibility(EventId id, EventInfo info)
    {
        if (GUIPager.ActivePage == "Chat" && ((EventInfo_B)info).bool1)//Чтобы часть месседжбокса не перекрывалась маской для обрезки сообщений чата
            GUIPager.SetActivePage("MainMenu");
    }

    private void OnLanguageChange(EventId id, EventInfo info)
    {
        float scale = Localizer.IsAsianFont(Localizer.Language) ? lblUserLevelChineseFontScale : lblUserLevelEuropianFontScale;//захардкодил китайский скейл, потому что китайский шрифт один на все проекты
        lblUserLevel.scale = new Vector3(scale, scale, 1);
    }

    private IEnumerator BattleEntering(GameManager.MapId _mapId)
    {
        PhotonNetwork.offlineMode = ProfileInfo.IsBattleTutorial;

        Messenger.Send(EventId.WentToBattle, null);

        BattleStatisticsManager.ResetBattleStats();

        fuelBarManager.FilledCanAmount = (int)ProfileInfo.Fuel;

        GameManager.SetMainVehicle(
            ProfileInfo.IsBattleTutorial
                ? Shop.GetVehicle(GameData.CurrentTutorialVehicleId)
                : Shop.CurrentVehicle);

        CodeStage.AntiCheat.Detectors.SpeedHackDetector.isRunning = false;

        XdevsSplashScreen.SetActive(
            en: true,
            showLabels: true); // Show advice

        // Чтобы индикатор вернулся в неудаляемый объект, а то в банке оставался и пипец - нуллреференс.
        XdevsSplashScreen.SetActiveWaitingIndicator(false);

        Action passingBattleLoading = () =>
        {
            Loading.loadScene(_mapId.ToString());

            isBattleEntering = false;

        };

        // Save GameMode and Consumable inventory.
        ProfileInfo.SaveToServer();

        if (!ProfileInfo.IsBattleTutorial)
        {
            AudioClip enterSound = GameData.IsGame(Game.IronTanks) ? toBattleSoundIronTanks : toBattleSoundTeamMode;
            PlayGUISound(enterSound);
            yield return new WaitForSeconds(enterSound.length);
        }

        ThirdPartyAdsManager.Show(
            targetPlace: AdsShowingMode.BeforeBattle,
            closingCallback: passingBattleLoading);
    }

    private void CheckForDiscountsStates(double time)
    {
        if (GameData.consumableKitInfos != null)
            foreach (var consKitInfo in GameData.consumableKitInfos.Values)
                if (consKitInfo.discount != null && consKitInfo.discount.IsStateChanged)
                    Messenger.Send(EventId.DiscountStateChanged, new EventInfo_U(EntityTypes.ConsumableKit, consKitInfo.id, consKitInfo.discount.IsActive));
    }
}
