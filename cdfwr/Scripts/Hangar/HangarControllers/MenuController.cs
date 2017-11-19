using System;
using System.Linq;
using CodeStage.AntiCheat.ObscuredTypes;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Disconnect;
using UnityEngine.SceneManagement;
using UnityEngine.Serialization;

[ExecuteInEditMode]
public class MenuController : MonoBehaviour
{
    public HangarController hangarController;
    public Achievments achievments;
    public ChatManager chatManager;
    public ClansManager clansManager;
    public tk2dCamera Tk2dGuiCamera;

    [FormerlySerializedAs("tankPanelBackground")]

    public MeshFilter bottomGuiPanel;
    [Space]
    [Header("Bars")]
    public DeltaProgressBar armorBar;
    public DeltaProgressBar attackBar;
    public DeltaProgressBar rocketAttackBar;
    public DeltaProgressBar speedBar;
    public DeltaProgressBar rofBar;
    public DeltaProgressBar reloadSpeedBar;
    public DeltaProgressBar magazineBar;
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

    //  private static bool firstHangarEnter = true;

    public bool isBattleEntering;
    //  private float lastTick;

    public List<SpecialOffer> allOffersList = new List<SpecialOffer>();

    public static MenuController Instance { get; private set; }

    //  public static bool FirstEnter { get { return firstHangarEnter; } }

    //  public static bool DemoVersion { get { return Instance.demoVersion; } }

    public static bool ExitingGame { get; private set; }

    //  public bool IsInitialized { get; private set; }

    public LabelLocalizationAgent InaccessibleBoxLabel { get { return inaccessibleBoxLabel; } }

    public HangarBuyingBox BuyingBox { get { return buyingBox; } }

    public Dictionary<int, float> ArmorMax { get; private set; }

    public Dictionary<int, float> DamageMax { get; private set; }

    public Dictionary<int, float> RocketDamageMax { get; private set; }

    public Dictionary<int, float> SpeedMax { get; private set; }

    public Dictionary<int, float> ROFMax { get; private set; }

    public Dictionary<int, float> IRCMROFMax { get; private set; }

    public Dictionary<int, float> ReloadMax { get; private set; }

    public Dictionary<int, float> MagazinaMax { get; private set; }



    public Camera GuiCamera { get; private set; }


    public static HangarBuyingBox CurrentActionBox { get; private set; }
    //   [HideInInspector]
    //  public HangarController hangarController;
    /*    UNITY SECTION    */

    void Awake()
    {
        Instance = this;
        /*

        bottomGuiPanel = hangarController.bottomGuiPanel;
        armorBar = hangarController.armorBar;
        attackBar = hangarController.attackBar;
        rocketAttackBar = hangarController.rocketAttackBar;
        speedBar = hangarController.speedBar;
        rofBar = hangarController.rofBar;
        ircmRofBar = hangarController.ircmRofBar;
        userLevelBar = hangarController.userLevelBar;
        lblUserLevel = hangarController.lblUserLevel;
        lblUserLevelEuropianFontScale = hangarController.lblUserLevelEuropianFontScale;//в китайском языке цифры большие, приходится вручную скейлить (пока только в SpaceJet)
        lblUserLevelChineseFontScale = hangarController.lblUserLevelChineseFontScale;
        lblUserName = hangarController.lblUserName;
        lblVehicleName = hangarController.lblVehicleName;
        lblSilver = hangarController.lblSilver;
        lblGold = hangarController.lblGold;
        buyingBox = hangarController.buyingBox;
        saleBuyingBox = hangarController.saleBuyingBox;
        saleRentBox = hangarController.saleRentBox;
        recieveFreeTankBox = hangarController.recieveFreeTankBox;
        deliverBox = hangarController.deliverBox;
        rentBox = hangarController.rentBox;
        rentingBox = hangarController.rentingBox;
        rentedBox = hangarController.rentedBox;
        installBox = hangarController.installBox;
        installedBox = hangarController.installedBox;
        inaccessibleBox = hangarController.inaccessibleBox;
        inaccessibleBoxLabel = hangarController.inaccessibleBoxLabel;
        comingSoonBox = hangarController.comingSoonBox;
        bankCurrencyToggle = hangarController.bankCurrencyToggle;
        demoVersion = hangarController.demoVersion;
        btnBank = hangarController.btnBank;
        experienceLabel = hangarController.experienceLabel;
        hasDoubleExpTanksIndicator = hangarController.hasDoubleExpTanksIndicator;
        doubleExpWrapper = hangarController.doubleExpWrapper;
        btnVipShop = hangarController.btnVipShop;
        btnBuyVip = hangarController.btnBuyVip;
        vipAccountShop = hangarController.vipAccountShop;
        fuelBarManager = hangarController.fuelBarManager;
        backSound = hangarController.backSound;
        nextSound = hangarController.nextSound;
        tankSelectSound = hangarController.tankSelectSound;
        patternSelectSound = hangarController.patternSelectSound;
        decalSelectSound = hangarController.decalSelectSound;
        buyingSound = hangarController.buyingSound;
        buyingConsumablesSound = hangarController.buyingConsumablesSound;
        moduleInstallSound = hangarController.moduleInstallSound;
        toBattleSound = hangarController.toBattleSound;
        toBattleSoundTeamMode = hangarController.toBattleSoundTeamMode;
        tankInstallSound = hangarController.tankInstallSound;
        checkBoxSound = hangarController.checkBoxSound;
        smallArrowSound = hangarController.smallArrowSound;
        
        quitGameQuestionTextColor = hangarController.quitGameQuestionTextColor;
        quitGameQuestionGameColor = hangarController.quitGameQuestionGameColor;
*/
    }


    public void RecalcMaxStats(Shop.ItemType itemType = (Shop.ItemType.Vehicle | Shop.ItemType.Module))
    {
        ArmorMax = new Dictionary<int, float>();
        DamageMax = new Dictionary<int, float>();
        RocketDamageMax = new Dictionary<int, float>();
        ROFMax = new Dictionary<int, float>();
        IRCMROFMax = new Dictionary<int, float>();
        SpeedMax = new Dictionary<int, float>();

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

            if ((Shop.ItemType.Vehicle & itemType) != Shop.ItemType.None)
            {
                baseArmorMax = SafeLinq.Max(hangarVehiclesToGroup.Value.Select(hangarVehicle => (float)hangarVehicle.Info.baseArmor));
                baseDamageMax = SafeLinq.Max(hangarVehiclesToGroup.Value.Select(hangarVehicle => (float)hangarVehicle.Info.baseDamage));
                baseRocketDamageMax = SafeLinq.Max(hangarVehiclesToGroup.Value.Select(hangarVehicle => (float)hangarVehicle.Info.baseRocketDamage));
                baseROFMax = SafeLinq.Max(hangarVehiclesToGroup.Value.Select(hangarVehicle => (float)hangarVehicle.Info.baseROF));
                baseIRCMROFMax = SafeLinq.Max(hangarVehiclesToGroup.Value.Select(hangarVehicle => (float)hangarVehicle.Info.baseIRCMROF));
                baseSpeedMax = SafeLinq.Max(hangarVehiclesToGroup.Value.Select(hangarVehicle => (float)hangarVehicle.Info.baseSpeed));
                baseMagazineMax = SafeLinq.Max(hangarVehiclesToGroup.Value.Select(hangarVehicle => (float)hangarVehicle.Info.baseMagazine));
                baseReloadMax = SafeLinq.Max(hangarVehiclesToGroup.Value.Select(hangarVehicle => (float)hangarVehicle.Info.baseReloadTime));
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

                armorMax = SafeLinq.Max(fullUpgradedVehicles.Select(upgradedVehicle => upgradedVehicle.Armor));
                damageMax = SafeLinq.Max(fullUpgradedVehicles.Select(upgradedVehicle => upgradedVehicle.Damage));
                rocketDamageMax = SafeLinq.Max(fullUpgradedVehicles.Select(upgradedVehicle => upgradedVehicle.RocketDamage));
                rofMax = SafeLinq.Max(fullUpgradedVehicles.Select(upgradedVehicle => upgradedVehicle.RoF));
                ircmRofMax = SafeLinq.Max(fullUpgradedVehicles.Select(upgradedVehicle => upgradedVehicle.IRCMRoF));
                speedMax = SafeLinq.Max(fullUpgradedVehicles.Select(upgradedVehicle => upgradedVehicle.Speed));
            }

            if ((Shop.ItemType.Pattern & itemType) != Shop.ItemType.None)
            {
                armorMax = armorMax + SafeLinq.Max(PatternShop.Selectors.Select(patternSelector => (float)patternSelector.Value.GetItem<Bodykit>().armorGain)) * baseArmorMax;
                damageMax = damageMax + SafeLinq.Max(PatternShop.Selectors.Select(patternSelector => (float)patternSelector.Value.GetItem<Bodykit>().damageGain)) * baseDamageMax;
                rocketDamageMax = rocketDamageMax + SafeLinq.Max(PatternShop.Selectors.Select(patternSelector => (float)patternSelector.Value.GetItem<Bodykit>().rocketDamageGain)) * baseRocketDamageMax;
                rofMax = rofMax + SafeLinq.Max(PatternShop.Selectors.Select(patternSelector => (float)patternSelector.Value.GetItem<Bodykit>().rofGain)) * baseROFMax;
                ircmRofMax = ircmRofMax + SafeLinq.Max(PatternShop.Selectors.Select(patternSelector => (float)patternSelector.Value.GetItem<Bodykit>().ircmRofGain)) * baseIRCMROFMax;
                speedMax = speedMax + SafeLinq.Max(PatternShop.Selectors.Select(patternSelector => (float)patternSelector.Value.GetItem<Bodykit>().speedGain)) * baseSpeedMax;
            }

            if ((Shop.ItemType.Decal & itemType) != Shop.ItemType.None)
            {
                armorMax = armorMax + SafeLinq.Max(DecalShop.Selectors.Select(decalSelector => (float)decalSelector.Value.GetItem<Bodykit>().armorGain)) * baseArmorMax;
                damageMax = damageMax + SafeLinq.Max(DecalShop.Selectors.Select(decalSelector => (float)decalSelector.Value.GetItem<Bodykit>().damageGain)) * baseDamageMax;
                rocketDamageMax = rocketDamageMax + SafeLinq.Max(DecalShop.Selectors.Select(decalSelector => (float)decalSelector.Value.GetItem<Bodykit>().rocketDamageGain)) * baseRocketDamageMax;
                rofMax = rofMax + SafeLinq.Max(DecalShop.Selectors.Select(decalSelector => (float)decalSelector.Value.GetItem<Bodykit>().rofGain)) * baseROFMax;
                ircmRofMax = ircmRofMax + SafeLinq.Max(DecalShop.Selectors.Select(decalSelector => (float)decalSelector.Value.GetItem<Bodykit>().ircmRofGain)) * baseIRCMROFMax;
                speedMax = speedMax + SafeLinq.Max(DecalShop.Selectors.Select(decalSelector => (float)decalSelector.Value.GetItem<Bodykit>().speedGain)) * baseSpeedMax;
                reloadMax = reloadMax + SafeLinq.Max(DecalShop.Selectors.Select(decalSelector => (float)decalSelector.Value.GetItem<Bodykit>().reloadGain)) * baseReloadMax;
                magazineMax = magazineMax + SafeLinq.Max(DecalShop.Selectors.Select(decalSelector => (float)decalSelector.Value.GetItem<Bodykit>().magazineGain)) * baseMagazineMax;

            }

            ArmorMax.Add(hangarVehiclesToGroup.Key, armorMax);
            DamageMax.Add(hangarVehiclesToGroup.Key, damageMax);
            RocketDamageMax.Add(hangarVehiclesToGroup.Key, rocketDamageMax);
            ROFMax.Add(hangarVehiclesToGroup.Key, rofMax);
            IRCMROFMax.Add(hangarVehiclesToGroup.Key, ircmRofMax);
            SpeedMax.Add(hangarVehiclesToGroup.Key, speedMax);
            ReloadMax.Add(hangarVehiclesToGroup.Key, reloadMax);
            MagazinaMax.Add(hangarVehiclesToGroup.Key, magazineMax);


        }
    }

    public void RefuelByTime(double timeInterval)
    {
        ProfileInfo.Fuel += timeInterval / GameData.refuellingTime;
        fuelBarManager.FilledCanAmount = (int)ProfileInfo.Fuel;
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

    //------------------------------
    //------------------------------
    public static void NextSound()
    {
        PlaySound(Instance.nextSound);
    }
    public static void BackSound()
    {
        PlaySound(Instance.backSound);
    }
    public static void BuyingSound()
    {
        PlaySound(Instance.buyingSound);
    }
    public static void SmallArrowSound()
    {
        PlaySound(Instance.smallArrowSound);
    }
    public static void BuyingConsumablesSound()
    {
        PlaySound(Instance.buyingConsumablesSound);
    }
    public static void CheckBoxSound()
    {
        PlaySound(Instance.checkBoxSound);
    }
    public static void ToBattleSound()
    {
        PlaySound(Instance.toBattleSound);
    }
    public static void ModuleInstallSound()
    {
        PlaySound(Instance.moduleInstallSound);
    }
    public static void TankSelectionSound()
    {
        PlaySound(Instance.tankSelectSound);
    }
    public static void DecalSelectSound()
    {
        PlaySound(Instance.decalSelectSound);
    }
    private static void PlaySound(AudioClip clip)
    {
        if (clip != null)
        {
            AudioDispatcher.PlayClip(clip);
        }
    }


    public void OnBackClick(string buttonName)
    {
        if (GUIPager.ActivePage == "MainMenu" && !SocialSettings.IsWebPlatform && !hangarController.isWaitingForSaving)
        {
            GUIPager.SetActivePage("QuitGame", false);

            MessageBox.Show(MessageBox.Type.Question,
                Localizer.GetText("QuitGameConfirm",
                    quitGameQuestionTextColor.To2DToolKitColorFormatString(),
                    quitGameQuestionGameColor.To2DToolKitColorFormatString(),
                    Application.productName,
                    quitGameQuestionTextColor.To2DToolKitColorFormatString()),
                    HangarController.QuitGame);

            MenuController.BackSound();
            return;
        }
        else if (GUIPager.ActivePage == "Settings")//Отменяем изменения если вышли по кнопке назад, а не ОК
        {
            Settings.Instance.ResetSettings();
        }

        GUIPager.Back();
        MenuController.BackSound();
    }

    public void OnPageChange(string from, string to)
    {
        if (to == "MainMenu")
        {
            GUIPager.PagesHistory.Clear();
            SetActiveDoubleExpText(ProfileInfo.currentVehicle);
            if (MessageBox.IsShown)
            {
                ScoresController.Instance.SetActive(false);
            }
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
    public static void SetActionBoxType(ActionBoxType type)
    {
        Instance.SetActionBoxType_(type);
    }

    public void SetActionBoxType_(ActionBoxType type)
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

    public static void SetCurrentBox(HangarBuyingBox box, bool activate)
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
    public void ForUpdate()
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
    public static void ShowUserInfo()
    {
        Instance.ShowUserInfo_();
    }
    private void ShowUserInfo_()
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

    public static void SetActiveDoubleExpText(int currentVehicleId)
    {
        Instance.doubleExpWrapper.SetActive(ProfileInfo.doubleExpVehicles.Contains(currentVehicleId));
    }

    public void OnMessageBoxChangeVisibility(EventId id, EventInfo info)
    {
        if (GUIPager.ActivePage == "Chat" && ((EventInfo_B)info).bool1)//Чтобы часть месседжбокса не перекрывалась маской для обрезки сообщений чата
        { GUIPager.SetActivePage("MainMenu"); }
    }



    public static void OnPatternShopClick(string buttonName)
    {
        GUIPager.SetActivePage("PatternShop");
        Dispatcher.Send(EventId.VoiceRequired, new EventInfo_I((int)VoiceEventKey.PatternShopEnter));
    }

    public static void OnDecalsShopClick(string buttonName)
    {
        GUIPager.SetActivePage("DecalShop");
        Dispatcher.Send(EventId.VoiceRequired, new EventInfo_I((int)VoiceEventKey.DecalShopEnter));
    }

    public static void OnBattleClick(string buttonName)
    {
        GUIPager.SetActivePage("MapSelection", true, true);
        Dispatcher.Send(EventId.VoiceRequired, new EventInfo_I((int)VoiceEventKey.MapSelection));
    }
    public static void OnBattleQuestClick(string buttonName)
    {
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


    public void ShowCommonStat(tk2dUIItem item)
    {
        NextSound();
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



    public static void ShowVehiclePrice()
    {
        Instance.ShowVehiclePrice_();
    }

    private void ShowVehiclePrice_()
    {
        if (Shop.VehicleInView.Info.isComingSoon)
        {
            SetActionBoxType(ActionBoxType.ComingSoon);
            return;
        }

        if (Shop.CurrentVehicle.Info.id == Shop.VehicleInView.Info.id)
        {
            SetActionBoxType(ActionBoxType.Installed);
            return;
        }

        if (ProfileInfo.vehicleUpgrades.ContainsKey(Shop.VehicleInView.Info.id))
        {
            SetActionBoxType(ActionBoxType.Install);
            return;
        }
        //if (Shop.VehicleInView.Info.id == VehicleOffersController.FreeVehicleIds[GameData.ClearGameFlags(GameData.CurrentGame)])
        //{
        //    SetActionBoxType(ActionBoxType.FreeTank);
        //    return;
        //}


        if (Shop.VehicleInView.Info.availabilityLevel > ProfileInfo.Level && !Shop.VehicleInView.Info.isVip)
        {
            SetActionBoxType(ActionBoxType.Inaccessible);
            InaccessibleBoxLabel.Parameter = Shop.VehicleInView.Info.availabilityLevel.ToString();
            return;
        }

        if (VehicleOffersController.Instance.AnyItemIsOnSale && VehicleOffersController.Instance.CheckIfItemOnSale(Shop.VehicleInView.Info.id))
        {
            SetActionBoxType(ActionBoxType.Sale);
        }
        else
        {
            SetActionBoxType(ActionBoxType.Buy);
        }

        CurrentActionBox.Price = Shop.VehicleInView.Info.price;
        CurrentActionBox.IsProductVip = Shop.VehicleInView.Info.isVip;
    }
    //------------------------------
    /*
        public static void InstantiateTutorialBuyVehicle(TutorialHolder holder, Vector3 XOffset, float YOffset) 
        {
            Instance.InstantiateTutorialPart(
                   holder: holder,
                       path: "Tutorials/ArrowPointerWrapper",
                       anchor: TutorialHolder.CamAnchors.lowerLeft,
                       position: MenuController.Instance.BuyingBox.BtnBuy.transform.position + XOffset,
                       yPos: MenuController.Instance.BuyingBox.BtnBuy.transform.position.y + YOffset,
                       eulerAngles: Vector3.forward * 90);
        }

        public static void InstantiateTutorialCamo(TutorialHolder holder, Vector3 XOffset, float YOffset) 
        {
            Instance.InstantiateTutorialPart(
               holder: holder,
               path: "Tutorials/ArrowPointerWrapper",
               anchor: TutorialHolder.CamAnchors.lowerLeft,
               position: TutorialsController.MainMenuButtons.GoToBattleBtn.localPosition,
               yPos: TutorialsController.MainMenuButtons.GoToBattleBtn.localPosition.y + YOffset,
               eulerAngles: Vector3.zero,
               partName: "ArrowPointerWrapper");
        }
        public static GameObject InstantiateTutorialPattern(TutorialHolder holder, Vector3 XOffset, float YOffset)
        {
            return Instance.InstantiateTutorialPart(
                    holder: holder,
                    path: "Tutorials/sprCharacterFromRes",
                    anchor: TutorialHolder.CamAnchors.upperLeft,
                    position: TutorialsController.MainMenuButtons.VehicleShopBtn.localPosition + XOffset,
                    yPos: YOffset,
                    eulerAngles: Vector3.zero,
                    partName: "Character");
        }

        public static void InstantiateTutorialArrowPointerWrapper(TutorialHolder holder, Vector3 position, Vector3 XOffset, float YOffset)
        {
            Instance.InstantiateTutorialPart(
               holder: holder,
               path: "Tutorials/ArrowPointerWrapper",
                anchor: TutorialHolder.CamAnchors.lowerLeft,
                position: position,
                yPos: position.y + YOffset,
                eulerAngles: Vector3.zero,
                partName: "ArrowPointerWrapper");
        }
        /*public static void InstantiateTutorialArrowPointerWrapper(TutorialHolder holder, Vector3 XOffset, float YOffset)
        {
            Instance.InstantiateTutorialPart(
               holder: holder,
               path: "Tutorials/ArrowPointerWrapper",
                anchor: TutorialHolder.CamAnchors.lowerLeft,
                position: TutorialsController.MainMenuButtons.PatternShopBtn.localPosition,
                yPos: TutorialsController.MainMenuButtons.PatternShopBtn.localPosition.y + YOffset,
                eulerAngles: Vector3.zero,
                partName: "ArrowPointerWrapper");
        }*/

    /*  public static void InstantiateTutorialTutorialMessage(TutorialHolder holder, string path,TutorialHolder.CamAnchors anchor, Vector2 XOffset, float YOffset,  GameObject character)
      {
          var characterSprFromRes = character.GetComponent<SpriteFromRes>();

          var characterSpriteDimensions = characterSprFromRes
                  ? ((tk2dSlicedSprite)characterSprFromRes.Sprite).dimensions
                  : character.GetComponent<tk2dSlicedSprite>().dimensions;



          Instance.InstantiateTutorialPart(
             holder: holder,
             path: path,
              anchor: anchor,
              position: characterSpriteDimensions + XOffset,
              yPos: characterSpriteDimensions.y + YOffset,
              eulerAngles: Vector3.zero,
              partName: "tutorialMessage_3",
              isLocalizationNeded: true,
              parent: character.transform);
      }*/

    public static GameObject InstantiateTutorialPart
        (
        TutorialHolder holder,
        string path,
        TutorialHolder.CamAnchors anchor,
        Vector3 position,
        float yPos,
        Vector3 eulerAngles,
        string partName = null,
        bool isLocalizationNeded = false,
        Transform parent = null)
    {
        var tutorialPrefab = Resources.Load<TutorialSprite>(path);
        tutorialPrefab.Initialize();

        var tutorialPart = Instantiate(tutorialPrefab.gameObject);

        parent = parent ?? holder.Anchors[(int)anchor].transform;

        tutorialPart.transform.SetParent(
            parent: parent,
            worldPositionStays: true);

        var itemPosition = parent.InverseTransformPoint(position);

        itemPosition.z = 5;
        itemPosition.y = yPos;

        tutorialPart.transform.localEulerAngles = eulerAngles;
        tutorialPart.transform.localPosition = itemPosition;

        if (!string.IsNullOrEmpty(partName))
        {
            tutorialPart.name = partName;
        }

        if (isLocalizationNeded)
        {
            tutorialPart.AddComponent(typeof(LabelLocalizationAgent));
        }

        return tutorialPart;
    }

}
