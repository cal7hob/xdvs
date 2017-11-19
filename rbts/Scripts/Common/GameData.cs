// TODO: Заглушка, пока у нас одна игра. Лечит кучу геморроя.
#define IS_FTRobotsInvasion

// place game build definers below for WinRT (DO NOT USE IT FOR OTHER PLATFORMS)
//#define IS_VK_APP
// end of define area

using System;
using System.Security.Cryptography;
using System.Text;
using System.Globalization;
using System.Text.RegularExpressions;
using CodeStage.AntiCheat.ObscuredTypes;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Disconnect;
using Matchmaking;
#if UNITY_EDITOR
using UnityEditor;
#endif


public enum Interface
{
    IronTanks           = 0, // Индексы не менять.
    FutureTanks         = 1,
    ToonWars            = 2,
    SpaceJet            = 3,
    ApocalypticCars     = 4,
    BattleOfWarplanes   = 5,
    BattleOfHelicopters = 6,
    Armada              = 7,
    WWR                 = 8,
    FTRobotsInvasion    = 9,
}


[Flags]
public enum Game : ulong
{
    Undefined                           = 0,            // project game was not set
    
    IronTanks                           = 0x00000001,   // 0000 0000 0000 0001
    WWR                                 = 0x00000004,   // 0000 0000 0000 0000 0000 0000 0100
    FutureTanks                         = 0x00000010,   // 0000 0000 0001 0000
    ToonWars                            = 0x00000100,   // 0000 0001 0000 0000
    SpaceJet                            = 0x00001000,   // 0001 0000 0000 0000
    ApocalypticCars                     = 0x00010000,   // 0000 0000 0000 0001 0000 0000 0000 0000
    BattleOfWarplanes                   = 0x00100000,   // 0000 0000 0001 0000 0000 0000 0000 0000
    BattleOfHelicopters                 = 0x01000000,   // 0000 0001 0000 0000 0000 0000 0000 0000
    Armada                              = 0x00000002,   // 0000 0000 0000 0000 0000 0000 0001 0000

    FTRobotsInvasion                    = 0x00000008,   // 0000 0000 0000 0000 0000 0000 1000

    AmazonBuild = 0x20000,                              // 0000 0000 0000 0010 0000 0000 0000 0000
    IronTanksAmazon = IronTanks | AmazonBuild,
    FutureTanksAmazon = FutureTanks | AmazonBuild,
    ToonWarsAmazon = ToonWars | AmazonBuild,
    SpaceJetAmazon = SpaceJet | AmazonBuild,
    BattleOfWarplanesAmazon = BattleOfWarplanes | AmazonBuild,
    ApocalypticCarsAmazon = ApocalypticCars | AmazonBuild,
    BattleOfHelicoptersAmazon = BattleOfHelicopters | AmazonBuild,
    ArmadaAmazon = Armada | AmazonBuild,
    WWRAmazon = WWR | AmazonBuild,
    FTRobotsInvasionAmazon = FTRobotsInvasion | AmazonBuild,

    CNBuild = 0x10000000,                           // 0001 0000 0000 0000 0000 0000 0000 0000
    IronTanksCN = IronTanks | CNBuild,              // 0001 0000 0000 0000 0000 0000 0000 0001
}

/// <summary>
/// Местонахождение сервера в мире
/// </summary>
public enum WorldRegion {
    Debug = -1,
    Europe = 0,
    Asia = 1,
}



public class GameData : MonoBehaviour
{
    [Serializable]
    public enum GameMode
    {
        Unknown = 0,
        Deathmatch = 1,
        Team = 2,
    }

    public static string GameModeLocalizationKey{get{return string.Format("lbl{0}Mode", Mode); }}

    public static Game CurrentGame
    {
        get
        {
            Game currentGame = Game.Undefined;

#if IS_IRONTANKS
            currentGame = Game.IronTanks;
#elif IS_FUTURETANKS
            currentGame = Game.FutureTanks;
#elif IS_TOONWARS
            currentGame = Game.ToonWars;
#elif IS_SPACEJET
            currentGame = Game.SpaceJet;
#elif IS_APOCALYPTICCARS
            currentGame = Game.ApocalypticCars;
#elif IS_BATTLEOFWARPLANES
            currentGame = Game.BattleOfWarplanes;
#elif IS_BATTLEOFHELICOPTERS
            currentGame = Game.BattleOfHelicopters;
#elif IS_ARMADA
            currentGame = Game.Armada;
#elif IS_WWR
            currentGame = Game.WWR;
#elif IS_FTRobotsInvasion
            currentGame = Game.FTRobotsInvasion;
#endif
#if IS_AMAZON_APP
            currentGame |= Game.AmazonBuild;
#endif

            return currentGame;
        }
    }

    public static bool isAndroidGingerBread
    {
        get
        {
            var regex = new Regex(@"Android.*?2\.3\.\d");
            return regex.Match(SystemInfo.operatingSystem).Success;
        }
    }

    private static bool inited = false;
    private static bool isTk2dPlatformSet = false;

    #region Bonus Chances

    public class BonusChances
    {
        /////////////////////////////////////////////////////////
        // Минимальное количество игроков (из разных кланов) в комнате,
        /// при котором начинают спауниться бонусы.
        public int minPlayersForSpawn = 3;

        /////////////////////////////////////////////////////////
        // Шансы появления бонусов
        public int goldChanceMin = 10;
        public int goldChance = 200;
        public int silverChance = 500;
        public int experienceChance = 600;
    }

    public static BonusChances BonusChancesData { get; private set; }

    #endregion

    #region Bonus Amount

    public class BonusAmounts
    {
        private class Divider
        {
            private readonly int experienceInstantPlayer;
            private readonly int experienceDropPlayer;
            private readonly int experienceInstantBot;
            private readonly int experienceDropBot;
            private readonly int silverPlayer;
            private readonly int silverBot;

            public Divider()
            {
                experienceInstantPlayer = 1;
                experienceDropPlayer = 1;
                experienceInstantBot = 1;
                experienceDropBot = 1;
                silverPlayer = 1;
                silverBot = 1;
            }

            public Divider(JsonPrefs dividerPrefs)
            {
                experienceInstantPlayer = dividerPrefs.ValueInt("expInstantPlayer", 1);
                experienceDropPlayer = dividerPrefs.ValueInt("expDropPlayer", 1);
                experienceInstantBot = dividerPrefs.ValueInt("expInstantBot", 1);
                experienceDropBot = dividerPrefs.ValueInt("expDropBot", 1);
                silverPlayer = dividerPrefs.ValueInt("silverPlayer", 1);
                silverBot = dividerPrefs.ValueInt("silverBot", 1);
            }

            public float GetValue(BonusItem.BonusType bonusType, bool forBot, bool instant)
            {
                switch (bonusType)
                {
                    case BonusItem.BonusType.Gold:
                        return 1;

                    case BonusItem.BonusType.Silver:
                        return forBot ? silverBot : silverPlayer;

                    case BonusItem.BonusType.Experience:
                        return instant
                            ? forBot ? experienceInstantBot : experienceInstantPlayer
                            : forBot ? experienceDropBot : experienceDropPlayer;

                    default:
                        return 1;
                }
            }
        }

        public float minRandomCoefficient;
        public float maxRandomCoefficient;

        private readonly Dictionary<int, Divider> dividers;

        public BonusAmounts(List<object> data)
        {
            dividers = new Dictionary<int, Divider>();

            foreach (object obj in data)
            {
                JsonPrefs dividerPrefs = new JsonPrefs(obj);
                dividers.Add(dividerPrefs.ValueInt("tankGroupDelta"), new Divider(dividerPrefs));
            }
        }

        public float GetDividerValue(int tankGroupDelta, BonusItem.BonusType bonusType, bool forBot, bool instant)
        {
            return GetDivider(tankGroupDelta).GetValue(bonusType, forBot, instant);
        }

        private Divider GetDivider(int tankGroupDelta)
        {
            Divider divider;

            if (dividers.TryGetValue(tankGroupDelta, out divider))
                return divider;

            Debug.LogWarningFormat("Suitable divider for tankGroupDelta = {0} not found! Using 1.", tankGroupDelta);

            return new Divider();
        }
    }

    public static BonusAmounts BonusAmountsData { get; private set; }

#endregion

    //////////////////////////////////////////////////////////////
    // Options for load from server
    public static int minPlayersForBattleStart = 1;
    public static int maxPlayers = 10;
    public static int minPlayers = 4;
    public static int respawnTime = 10;
    public static float maxInactivityTime = 60;
    public static int hastenPrice = 1;
    public static int cheatTreshold = 7000;
    public static float shellHitSyncInterval = 0.3f;

    public static int chatMinAccessLevel = 3;
    public static int chatMinComplainLevel = 9;
    public static bool chatIsComplaintsEnabled = true;
    public static int chatModeratorAvailableBanTime = 3600;

    public static float ClanmateScoreBonusRatio = 0.5f;
    public static float ClanmateSilverBonusRatio = 0.5f;

    public static int accountManagementMinLevel = 4;
    public static int newbieKitsAfterBattleShowingRate = 0;//Через сколько боев показывать стартовые наборы
    public static List<GameMode> gameModes;

    public static string localizationStamp = ""; // Время последнего обновления локализации на сервере

    #region Настройки боевого чата
    public static int battleChatPanelItemsMaxCount = 3;//максимальное количество сообщений в панели чата
    public static int battleChatPanelItemLiveTime = 10;//Время показа сообщения в чате, на танк индикаторе и на миникарте
    public static int battleChatMessageSendInterval = 15;//пауза м/д отправками сообщений, чтоб юзеры не спамили
    #endregion

    

    private static float battleDuration = 300;
    private static float prolongTimeAddition = 180;
    private static float goldRushMinTime = 30;
    private static int maxPing = 300; // Максимальный средний пинг для присутствия в комнате
    private static int maxMasterPing = 300; // Максимальный средний пинг для присутствия в комнате для мастера

    public static int MaxPing { get { return PhotonNetwork.isMasterClient ? maxMasterPing : maxPing; } }
    
    private PushNotifications pushNotifications;

    public static float pingDetectionPeriod = 8f; // Длительность периода для рассчета среднего значения пинга клиента
    public static float pingDetectionInterval = 1f; // Интервал снятия текущего значения пинга
    public static ObscuredInt playerSandboxLevel = 6;
    public static ObscuredBool isReconnectEnabled = true;
    public static ObscuredFloat reconnectTimeout = BattleConnectManager.RECONNECT_TIMEOUT;
    public static ObscuredFloat critDamageRatio = GameManager.CRIT_DAMAGE_RATIO;
    public static ObscuredFloat normDamageRatio = GameManager.NORM_DAMAGE_RATIO;
    public static ObscuredInt levelUpAwardCoefficientSilver;
    public static ObscuredFloat levelUpAwardCoefficientGold;
    public static ObscuredBool isBotsEnabled = true;
    public static ObscuredFloat botLifetime;
    public static ObscuredInt minBotCount = 3;
    public static ObscuredInt maxBotCount = 3;
    public static int minBotEngageDelay = 5;
    public static int maxBotEngageDelay = 5;
    public static int targetHumanChance = 70;

    //////////////////////////////////////////////////////////////
    // TODO: do not forget to delete or change it if any changes will occure
    public const int EXTRA_FUEL_VEHICLE_ID = 6;
    public const int STANDART_FUEL_CAN_AMOUNT = 10;
    public const int EXTRA_FUEL_CAN_AMOUNT = 2;
    public const int MAX_GAME_FUEL_AMOUNT = 12;
    public const string UNKNOWN_FLAG_NAME = "xx";

    [SerializeField]
    private string playerBundleVersion;
    [SerializeField]
    private string buildNumber = "0";//номер сборки - чтобы не поднимать версию. Например версия 1.91 (сборка 0)
    [SerializeField]
    private string forcedGUID;

    private static string bundleId = "";
    private static string bundleVersion;

    public static Dictionary<string, object> vehiclesDataStorage;
    
    public static ObscuredString vipOffersStorage = null;
    public static IList<IVipOffer> vipOffers = null;

    #region Расходка / Consumables

    public static bool isConsumableEnabled = false;//если false - расходка в клиенте не используется
    public static int tankIndicatorUsedConsumableShowingTime = 2;//танк индикатор: Время отображения значка расходки при ее применении
    public const string CONSUMABLES_SPRITE_FRAMED_VERSION_SUFFIX = "_substrate";
    
    public static Dictionary<int, ConsumableInfo> consumableInfos;
    public static Dictionary<int, ConsumableKitInfo> consumableKitInfos;
    public static List<object> consumableKitOffersList = new List<object>();

    public static bool HaveConsumableKitsAnyDiscount { get { return consumableKitInfos != null && consumableKitInfos.Any(kitOffer => (kitOffer.Value.discount != null && kitOffer.Value.discount.IsActive)); } }
    public static int MaxConsumableKitsDiscount { get { return consumableKitInfos.Values.Where(kitOffer => (kitOffer.discount != null && kitOffer.discount.IsActive)).OrderByDescending(item => item.discount.val).ToList()[0].discount.val; } }

    #endregion

    public static bool IsProlongTimeEnabled { get; private set; }
    public static float ProlongTimeShow { get; private set; }
    public static bool IsGoldRushEnabled { get; private set; }
    
    public string AuthenticationKey = "";

    public static string InterfaceShortName { get { return GetInterfaceShortName(CurInterface); } }

    public static string GetInterfaceShortName(Interface customInterface)
    {
        switch(customInterface)
        {
            case Interface.IronTanks: return "IT";
            case Interface.FutureTanks: return "FT";
            case Interface.ToonWars: return "TW";
            case Interface.BattleOfWarplanes: return "BW";
            case Interface.BattleOfHelicopters: return "BH";
            case Interface.Armada: return "AR";
            case Interface.WWR: return "WWR";
            default: return new string((from item in customInterface.ToString().ToCharArray() where Char.IsUpper(item) select item).ToArray());
        }
    }

    private static bool serverDataReceived = false;
    private static DateTime serverTime;
    private static float serverTimeReceiveSinceStart;
    private static bool timeLoaded;
    private MessageBox.Answer userAnswer;
    
    // Cache for facebook friends scores
    public static string facebookScores = "";
    public static ObscuredInt fuelForInvite = 10;

    // Награда за оценку игры
    public static bool isAwardForRateGameEnabled = false;
    public static ProfileInfo.Price awardForRateGame = new ProfileInfo.Price (5000, ProfileInfo.PriceCurrency.Silver);

    // Награда за социальную активность
    public static bool isBonusForSocialActivityEnabled = false;
    public static ProfileInfo.Price awardForSocialActivity = new ProfileInfo.Price(50, ProfileInfo.PriceCurrency.Gold);

    // Награда за подключение соцсети
    public static ProfileInfo.Price socialActivationReward = new ProfileInfo.Price (7, ProfileInfo.PriceCurrency.Gold);

    public static ProfileInfo.Price changeNickPrice = new ProfileInfo.Price(10, ProfileInfo.PriceCurrency.Gold);

    // Флаги включены или выключены
    public static bool countryFlagsIsOn;

    // Уровень игрока до которого панель с рейтингом будет свёрнута
    public static int hideScoresTillLevel = 15;

    // Хранилище для загрузки наград в WeeklyAwardsInfo
    public static JsonPrefs tournamentAwardsJSONPrefs;
    public static long weeklyTournamentEndTime;

    // Список карт
    public static List<object> mapsList;
    public static Dictionary<GameManager.MapId, MapInfo> allMapsDic = new Dictionary<GameManager.MapId, MapInfo>();
    public static Dictionary<GameManager.MapId, MapInfo> availableMapsDic = new Dictionary<GameManager.MapId, MapInfo>();

    // TapJoy реклама (вкл/выкл)
    public static bool isTapJoyEnabled;

    // TapJoy SDK keys
    public static string tapJoyAndroidSdkKey;
    public static string tapJoyAndroidGcmSenderId;
    public static string tapJoyIosSdkKey;

    // Pushwoosh SDK credentials
    public static string pushwooshAppCode;
    public static string pushwooshFCMSenderId;
    public static string pushwooshApiToken;

    // Кол-во дней, на которые будет выключена реклама, при покупке в банке
    public static int adsFreeDaysQuantity;

    // Вся реклама выключена
    public static bool adsFree;

    // Максимально разрешённое количество игроков в клане
    public static int maxClanMembers = 10;

    public static ObscuredInt refuellingTime = 60;

    // Офферы на установку наших игр
    public static List<XDevs.Offers.GameOffer> gamesOffers = new List<XDevs.Offers.GameOffer> ();

    public static List<object> bankOffersList = new List<object>();
    public static List<object> vehicleOffersList = new List<object>();
    public static List<object> decalOffersList = new List<object>();
    public static List<object> patternOffersList = new List<object>();
    public static List<object> vipsOffersList = new List<object>();

    public static Dictionary <BankKits.Type, Dictionary<string, BankKits.Data>> bankKits = new Dictionary<BankKits.Type, Dictionary<string, BankKits.Data>>();//стартерпаки в банке

    public static bool isBlackFriday;

    public static GameData instance;

    public static bool IsTeamMode { get { return Mode == GameMode.Team; } }
    public static bool IsMode(GameMode _mode) { return Mode == _mode; }

    public static float GoldRushMinTime { get { return goldRushMinTime; } }

    public static Dictionary<string, object> socialPrices = new Dictionary<string, object>();
    public static Dictionary<string, object> socialGroups = new Dictionary<string, object>();
    public static Dictionary<string, object> vkImagesAchievements = new Dictionary<string, object>();
    public static List<object> vkImagesLevels = new List<object>();

    public static int CurrentTutorialVehicleId
    {
        get
        {
            return 11;
        }
    }

    public static Interface CurInterface
    {
        get
        {
#if UNITY_EDITOR
            if (!EditorApplication.isPlaying)
            {
                if (UnityEditor.PlayerSettings.applicationIdentifier == androidBundleIds[Game.IronTanks] ||
                    UnityEditor.PlayerSettings.applicationIdentifier == androidBundleIds[Game.IronTanksCN] ||
                    UnityEditor.PlayerSettings.applicationIdentifier == androidBundleIds[Game.IronTanksAmazon]
                   )
                {
                    return Interface.IronTanks;
                }
                if (UnityEditor.PlayerSettings.applicationIdentifier == androidBundleIds[Game.FutureTanks] ||
                        UnityEditor.PlayerSettings.applicationIdentifier == androidBundleIds[Game.FutureTanksAmazon])
                    return Interface.FutureTanks;
                if (UnityEditor.PlayerSettings.applicationIdentifier == androidBundleIds[Game.ToonWars] ||
                        UnityEditor.PlayerSettings.applicationIdentifier == androidBundleIds[Game.ToonWarsAmazon])
                    return Interface.ToonWars;
                if (UnityEditor.PlayerSettings.applicationIdentifier == androidBundleIds[Game.SpaceJet] ||
                        UnityEditor.PlayerSettings.applicationIdentifier == androidBundleIds[Game.SpaceJetAmazon])
                    return Interface.SpaceJet;
                if (UnityEditor.PlayerSettings.applicationIdentifier == androidBundleIds[Game.BattleOfWarplanes] ||
                        UnityEditor.PlayerSettings.applicationIdentifier == androidBundleIds[Game.BattleOfWarplanesAmazon])
                    return Interface.BattleOfWarplanes;
                if (UnityEditor.PlayerSettings.applicationIdentifier == androidBundleIds[Game.ApocalypticCars] ||
                        UnityEditor.PlayerSettings.applicationIdentifier == androidBundleIds[Game.ApocalypticCarsAmazon])
                    return Interface.ApocalypticCars;
                if (UnityEditor.PlayerSettings.applicationIdentifier == androidBundleIds[Game.BattleOfHelicopters] ||
                         UnityEditor.PlayerSettings.applicationIdentifier == androidBundleIds[Game.BattleOfHelicoptersAmazon])
                    return Interface.BattleOfHelicopters;
                if (UnityEditor.PlayerSettings.applicationIdentifier == androidBundleIds[Game.Armada] ||
                    UnityEditor.PlayerSettings.applicationIdentifier == androidBundleIds[Game.ArmadaAmazon])
                    return Interface.Armada;
                if (UnityEditor.PlayerSettings.applicationIdentifier == androidBundleIds[Game.FTRobotsInvasion] ||
                    UnityEditor.PlayerSettings.applicationIdentifier == androidBundleIds[Game.FTRobotsInvasionAmazon])
                    return Interface.FTRobotsInvasion;
                Debug.LogError("UNKNOWN BUNDLE! Cant recognize interface type!");
                return Interface.FutureTanks;
            }
#endif
            if (GameData.IsGame(Game.FutureTanks))
                return Interface.FutureTanks;
            else if (GameData.IsGame(Game.IronTanks))
                return Interface.IronTanks;
            else if (GameData.IsGame(Game.ToonWars))
                return Interface.ToonWars;
            else if (GameData.IsGame(Game.SpaceJet))
                return Interface.SpaceJet;
            else if (GameData.IsGame(Game.ApocalypticCars))
                return Interface.ApocalypticCars;
            else if (GameData.IsGame(Game.BattleOfWarplanes))
                return Interface.BattleOfWarplanes;
            else if (GameData.IsGame(Game.BattleOfHelicopters))
                return Interface.BattleOfHelicopters;
            else if (GameData.IsGame(Game.Armada))
                return Interface.Armada;
            else if (GameData.IsGame(Game.WWR))
                return Interface.WWR;
            else if (GameData.IsGame(Game.FTRobotsInvasion))
                return Interface.FTRobotsInvasion;
            else
            {
                Debug.LogError("Unknown Interface!!!");
                return Interface.FutureTanks;
            }
        }
    }

    public static string GetForcedGUID()
    {
        return instance != null && !string.IsNullOrEmpty(instance.forcedGUID) ? instance.forcedGUID : null;
    }

    public static void Init()
    {
        if (inited)
            return;

        var qualityManagerObj = GameObject.Find("QualityManager");
        var qualityManager = qualityManagerObj ? qualityManagerObj.GetComponent<QualityManager>() : Instantiate(Resources.Load<QualityManager>("Common/QualityManager"));
        qualityManager.name = "QualityManager";
        inited = true;

        Debug.LogWarning(string.Format("<color=yellow>Game = {0}</color>", CurrentGame.ToString()));
#if UNITY_EDITOR
        //Лог для поиска по времени в файле лога редактора
        Debug.LogWarning(string.Format("<color=yellow>LocalTime = {0}</color>", DateTime.Now.ToString()));
#endif

        PhotonSerializersRegistration();
        ObscuredValuesInit();
    }

    public static Game InterfaceToGame(Interface iface)
    {
        return (Game)Enum.Parse(typeof(Game), iface.ToString());
    }

    public static Interface GameToInterface(Game _game)
    {
        int iName = (int)ClearGameFlags(_game);
        return (Interface)Enum.Parse(typeof(Interface), ((Game)iName).ToString());
    }
    public static Game ClearGameFlags(Game _game)
    {
        int iName = ((int)(_game)) & ~((int)(Game.CNBuild)) & ~((int)(Game.AmazonBuild));
        return (Game)iName;
    }

    // Locale data
    public CultureInfo cultureInfo;

    public static Dictionary<Game, string> androidBundleIds = new Dictionary<Game, string>() {
        {Game.IronTanks,           "com.extremedevelopers.irontanks"},
        {Game.IronTanksCN,         "com.extremedevelopers.irontankscn"},
        {Game.FutureTanks,         "com.extremedevelopers.futuretanks"},
#if UNITY_ANDROID
        {Game.ToonWars,            "com.extremedevelopers.toonboom"},
#else                              
        {Game.ToonWars,            "com.extremedevelopers.toonwars"},
#endif                             
        {Game.SpaceJet,            "com.extremedevelopers.spacejet"},
        {Game.BattleOfWarplanes,   "com.extremedevelopers.battleofwarplanes"},
        {Game.ApocalypticCars,     "com.extremedevelopers.apocalypticcars"}, // TODO: заменить на реальный ИД или создать его.
        {Game.BattleOfHelicopters, "com.extremedevelopers.battleofhelicopters"},
        {Game.Armada,              "com.extremedevelopers.armada"},
        {Game.WWR,                 "com.extremedevelopers.wwr"},
        {Game.FTRobotsInvasion,    "com.extremedevelopers.tanksvsrobots"},
        {Game.IronTanksAmazon,      "com.extremedevelopers.irontanksam"},// TODO: заменить на реальный ИД или создать его.
        {Game.FutureTanksAmazon,    "com.extremedevelopers.futuretanksam"},
        {Game.ToonWarsAmazon,       "com.extremedevelopers.toonboomam"},
        {Game.SpaceJetAmazon,       "com.extremedevelopers.spacejet"}, // TODO: replace it with genuine path!!!
        {Game.BattleOfWarplanesAmazon,"com.extremedevelopers.battleofwarplanes"},
        {Game.ApocalypticCarsAmazon,  "com.extremedevelopers.apocalypticcars"}, // TODO: заменить на реальный ИД или создать его.
        {Game.BattleOfHelicoptersAmazon,"com.extremedevelopers.battleofhelicopters"},
        {Game.ArmadaAmazon,         "com.extremedevelopers.armada"},
        {Game.FTRobotsInvasionAmazon, "com.extremedevelopers.tanksvsrobots"},
    };

    private static GameMode gameMode = GameMode.Unknown;

    public static GameMode Mode
    {
        get {
            //Защита от тутора в командном режиме
            if (!ProfileInfo.IsBattleTutorialCompleted) {
                return GameMode.Deathmatch;
            }

            return gameMode;
        }
        set
        {
            if (value == GameMode.Unknown || !gameModes.Contains(value)) {
                if (gameModes.Count > 0)
                    value = gameModes[0];
                else
                    value = DefaultGameMode;
            }
            if (gameMode != value)
            {
                gameMode = value;
                Messenger.Send(EventId.GameModeChanged, new EventInfo_SimpleEvent());
            }
        }
    }

    public static GameMode DefaultGameMode
    {
        get
        {
            return GameMode.Deathmatch;
        }
    }

    public static float BattleDuration { get { return battleDuration; } }

    public static float BattleRoomLifetime { get; private set; }
    
    public static float ProlongTimeAddition { get { return prolongTimeAddition; } }

    public static bool IsHangarScene
    {
        get { return HangarController.Instance != null; }
    }

    public static string HangarSceneName{get{return GetHangarSceneName(ProfileInfo.IsPlayerVip);}}

    public static string GetHangarSceneName(bool isVip)
    {
        if (CurInterface == Interface.SpaceJet || CurInterface == Interface.WWR)//Пока переименовали сцены "по уму" только в SJ и WWR
        {
            return string.Format(
            "scnh_{0}_{1}",
            GetInterfaceShortName(CurInterface).ToLower(),
            isVip
                ? "premium"
                : "standart");
        }
        else
        {
            return string.Format(
                "Hangar_{0}_{1}",
                CurInterface,
                isVip
                    ? "Premium"
                    : "Standart");
        }
    }

    public static bool TimeLoaded
    {
        get { return timeLoaded; }
    }

    public static DateTime ServerTime
    {
        get { return serverTime; }
    }

    public static bool ServerDataReceived
    {
        get { return serverDataReceived; }
    }

    /*	UNITY SECTION	*/

    private static void SetGuiAtlasAccordingRamAndRes()
    {
        if(isTk2dPlatformSet) return;

#if UNITY_WEBGL || UNITY_WEBPLAYER || (UNITY_WSA && UNITY_WSA_8_1)
        tk2dSystem.CurrentPlatform = "2x";
#else
        tk2dSystem.CurrentPlatform = (Screen.width < 1280 || SystemInfo.systemMemorySize < 700) ? "1x" : "2x";
#endif
        Debug.Log("tk2dSystem.CurrentPlatform: " + tk2dSystem.CurrentPlatform);

        isTk2dPlatformSet = true;
    }

    void Awake()
    {
        if (instance != null)
        {
            Destroy(gameObject);
            return;
        }

        Messenger.Subscribe(EventId.AfterHangarInit, OnHangarInit);

        BankData.Init();
        SetGuiAtlasAccordingRamAndRes();
        Init();
        instance = this;
        DontDestroyOnLoad (gameObject);

        BattleRoomLifetime = 1;
        BonusChancesData = new BonusChances ();

        serverTime = DateTime.MinValue;
        timeLoaded = false;
        //serverDataReceived = false;

        // Load locale data from device
        try {
#if UNITY_ANDROID
            cultureInfo = new CultureInfo (GetLocale ());
            Debug.Log ("Loaded locale " + GetLocale ());
#elif UNITY_WEBPLAYER || UNITY_WEBGL
            cultureInfo = CultureInfo.CurrentCulture;
#elif UNITY_WSA
            // Делаем копию, т.к. далее мы сменим формат чисел на инвариантный.
            // WSA не дает АПИ для смены локали текущего треда
            cultureInfo = new CultureInfo(CultureInfo.CurrentCulture.Name);
#elif UNITY_WP8
            cultureInfo = System.Threading.Thread.CurrentThread.CurrentCulture;
#else
            cultureInfo = new CultureInfo ("en-US");
#endif
        }
        catch (System.Exception)
        {
            cultureInfo = new CultureInfo ("en-US");
            /*Debug.Log ("Loaded locale "+cultureInfo.Name);*/
        }

        // Выставляем инвариантную культуру для избегания проблем с конвертирование чисел в строки и обратно
#if UNITY_WSA && !UNITY_EDITOR
        CultureInfo.CurrentCulture.NumberFormat = CultureInfo.InvariantCulture.NumberFormat;
#else
        System.Threading.Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;
#endif

        Messenger.Subscribe (EventId.CheatDetected, CheatDetected);
        Messenger.Subscribe (EventId.ProfileServerChoosed, StartInit, 1);

        Shell.Init();
    }

    void StartInit (EventId id, EventInfo info)
    {
        StartCoroutine (ReceiveServerData ());
    }

    void OnDestroy()
    {
        deltaToRealTimeSinceStartup = currentTimeStamp - Time.realtimeSinceStartup;
        //timeStamp = GameData.CurrentTimeStamp - GetCorrectedTime ();
        Messenger.Unsubscribe (EventId.CheatDetected, CheatDetected);
        Messenger.Unsubscribe(EventId.AfterHangarInit, OnHangarInit);
        GameSettings.Dispose();
    }

    private void OnHangarInit(EventId id, EventInfo info)
    {
        if (pushNotifications != null)
            return;

        pushNotifications = Instantiate(Resources.Load<PushNotifications>("Common/PushNotifications"));
        pushNotifications.name = "PushNotifications";
        DontDestroyOnLoad(pushNotifications);
    }

    /* PUBLIC SECTION */

    public static void CriticalError (string text)
    {
        //if(Application.loadedLevelName != GameManager.LOADING_SCENE_NAME)
            //SplashScreen.SetActive(false);
        XdevsSplashScreen.SetActiveWaitingIndicator(false);
        MessageBox.Show(MessageBox.Type.Critical, text);
        XDevs.Loading.ServerChooser.ClearChoose ();
    }

    private IEnumerator ReceiveServerData ()
    {
        if (serverDataReceived) {
            Debug.LogWarning ("Server data already received! Break double loading!");
            yield break;
        }

        while (!Localizer.Loaded)
            yield return null;

        //Debug.Log ("Start profile load");
        timeGettingError = true;
        yield return ProfileInfo.Load ();
        //Debug.Log ("Done profile load");

        Http.Request req = null;
        WWW www = null;
        bool connectionSucceeded = false;
        float connectionTime = 0;
        while (!connectionSucceeded) {
            try {
                req = Http.Manager.Instance ().CreateRequest ("/options/load");
                req.Form.AddField ("SystemInfo", MiniJSON.Json.Serialize (CollectSystemInfo ()));
                www = req.CreateWWW ();
            }
            catch (Exception e) {
                Debug.LogException (e);
                CriticalError (Localizer.GetText ("NTPConnectionError"));
                yield break;
            }

            while (!www.isDone) {
                connectionTime += Time.deltaTime;
                if (connectionTime > 10.0f) {
                    connectionTime = 0;
                    break;
                }
                yield return new WaitForSeconds (0.1f);
            }

            if (www.isDone && string.IsNullOrEmpty (www.error)) {
                connectionSucceeded = true;
            }
            else if (!www.isDone || req.GetResponse ().IsNetworkError) {
                Debug.LogError (www.error);
                userAnswer = MessageBox.Answer.Uncertain;

                MessageBox.Show(MessageBox.Type.Question, Localizer.GetText("RetryConnectQuestion"), x => { userAnswer = x; });

                while (userAnswer == MessageBox.Answer.Uncertain) {
                    yield return new WaitForSeconds (0.3f);
                }

                if (userAnswer == MessageBox.Answer.No) {
                    QuitGame ();
                }
                www = null;
                req = null;
            }
            else {
                break;
            }
        }

        if (req.GetResponse().HttpStatusCode == 502) {
            CriticalError(Localizer.GetText("ServerUnavailable"));
            yield break;
        }
        if (req.GetResponse ().HttpStatusCode == 503) {
            var p = new JsonPrefs (req.GetResponse().text);
            int status = p.ValueInt ("status", -1);
            int error = p.ValueInt ("error", -1);
            string msg = p.ValueString ("message");
            if (status == 0 && error > 0 && !string.IsNullOrEmpty (msg)) {
                CriticalError (msg);
            }
            yield break;
        }
        else if (req.GetResponse().ServerError == Http.Error.PlayerBanned) {
            CriticalError(Localizer.GetText("lblBannedMsg"));
            yield break;
        }
        else if (req.GetResponse().HaveErrors) {
            CriticalError(Localizer.GetText("NTPConnectionError"));
            yield break;
        }

        try {
            //Debug.LogWarning (req.GetResponse ().text);
            var data = new JsonPrefs (req.GetResponse ().Data);
            if (!data.Contains ("version")) {
                Debug.LogError ("'Version' key in loaded options not found, options: " + req.GetResponse ().text);
                CriticalError ("Server data error");
                yield break;
            }
            //Debug.Log ( MiniJSON.Json.Serialize (data.ValueObjectDict("version")));
            data.BeginGroup ("version");

            ProfileInfo.Version = data.ValueString ("version", "") != "0" ? data.ValueString ("version") : null;
            ProfileInfo.ImportantUpdate = data.ValueInt ("important", 0) == 1;
            ProfileInfo.MarketURL = data.ValueString ("market", "");
            data.EndGroup ();

            if (ProfileInfo.ImportantUpdate)
            {
                serverDataReceived = true;
                yield break;
            }

            if (!data.Contains ("time")) {
                Debug.LogError ("'time' key in loaded options not found, options: " + req.GetResponse ().text);
                CriticalError ("Server data error");
                yield break;
            }
            long tm = data.ValueLong ("time", 0);
            serverTime = new DateTime (1970, 1, 1, 0, 0, 0, DateTimeKind.Utc).AddSeconds (tm);
            serverTimeReceiveSinceStart = Time.realtimeSinceStartup;
            timeLoaded = true;

            if (!data.Contains ("location")) {
                Debug.LogError ("'location' key in loaded options not found, options: " + req.GetResponse ().text);
                CriticalError ("Server data error");
                yield break;
            }
            ProfileInfo.CountryCode = data.ValueString ("location/country_code", "").ToLower ();

            if (!data.Contains ("playerId")) {
                Debug.LogError ("'playerId' key in loaded options not found, options: " + req.GetResponse ().text);
                CriticalError ("Server data error");
                yield break;
            }
            ProfileInfo.profileId = data.ValueInt ("playerId", 0);//Convert.ToInt32 (data["playerId"]);
            ProfileInfo.SetPlayerPrivilege (data.ValueInt ("playerPrivilege"));

            Http.Manager.SessionToken = data.ValueString ("token", "");

            ///////////////////////////////////////////////////////////
            // Options loading
            if (!data.Contains ("options")) {
                Debug.LogError ("'options' key in loaded options not found, options: " + req.GetResponse ().text);
                CriticalError ("Server data error");
                yield break;
            }

            data.BeginGroup ("options");
            
            if (!LoadServerOptions (data)) {
                Debug.LogError ("'options' load error, options: " + req.GetResponse ().text);
                CriticalError ("Server data error");
                yield break;
            }
            data.EndGroup ();
        }
        catch (Exception e) {
            Http.Manager.ReportException ("GameData.ReceiveServerData", e);
            Debug.LogException (e);
        }

        yield return Localizer.LoadServerLocalization ();

        yield return InitTime ();

        this.InvokeRepeating(GameTimerTick, 0, 1);

        serverDataReceived = true;
        Messenger.Send (EventId.ServerDataReceived, null);
    }

    private object CollectSystemInfo () {
        var info = new Dictionary<string, object>();
        info["deviceType"] = SystemInfo.deviceType;
        info["graphicsDeviceType"] = SystemInfo.graphicsDeviceType;
        info["systemMemorySize"] = SystemInfo.systemMemorySize;
        info["graphicsMemorySize"] = SystemInfo.graphicsMemorySize;
        info["processorType"] = SystemInfo.processorType;
        info["processorCount"] = SystemInfo.processorCount;
        info["processorFrequency"] = SystemInfo.processorFrequency;
        info["graphicsDeviceType"] = SystemInfo.graphicsDeviceType;

        return info;
    }

    private static void PhotonSerializersRegistration()
    {
        ExitGames.Client.Photon.PhotonPeer.RegisterType(typeof(Messenger.PhotonEventAdapter), (byte)'A', Messenger.PhotonEventAdapter.SerializeAdapter,
                        Messenger.PhotonEventAdapter.DeserializeAdapter);
        ExitGames.Client.Photon.PhotonPeer.RegisterType(typeof(VehicleData), (byte)'T', VehicleData.Serialize,
                VehicleData.Deserialize);
        ExitGames.Client.Photon.PhotonPeer.RegisterType(typeof(BonusItem.BonusInfo), (byte)'B', BonusItem.BonusInfo.Serialize,
                BonusItem.BonusInfo.Deserialize);
        ExitGames.Client.Photon.PhotonPeer.RegisterType(typeof(PlayerDisconnectInfo), (byte)'D', PlayerDisconnectInfo.Serialize,
                PlayerDisconnectInfo.Deserialize);
        ExitGames.Client.Photon.PhotonPeer.RegisterType(typeof(RoomCountryInfo), (byte)'R', RoomCountryInfo.Serialize,
                RoomCountryInfo.Deserialize);
        ExitGames.Client.Photon.PhotonPeer.RegisterType(typeof(VehicleEffectData), (byte)'E', VehicleEffectData.Serialize,
                VehicleEffectData.Deserialize);
    }

    private static void ObscuredValuesInit()
    {
        string deviceId = SystemInfo.deviceUniqueIdentifier;
        byte[] deviceIdBytes = Encoding.UTF8.GetBytes(deviceId);
#if UNITY_WP8
        var hash = new SHA256Managed ();
        deviceIdBytes = hash.ComputeHash (deviceIdBytes);
#else
        MD5 md5 = MD5.Create();
        deviceIdBytes = md5.ComputeHash(deviceIdBytes);
#endif

        int newKey = deviceIdBytes[0] + deviceIdBytes[1] * 256 + deviceIdBytes[2] * 65536 + deviceIdBytes[3] * 65536 * 256;
        ObscuredInt.SetNewCryptoKey(newKey);
        ObscuredFloat.SetNewCryptoKey(newKey);

        string newStringKey = Encoding.UTF8.GetString(deviceIdBytes, 0, deviceIdBytes.Length);
        ObscuredString.SetNewCryptoKey(newStringKey);
        ObscuredPrefs.SetNewCryptoKey(newStringKey);
    }

    private bool LoadServerOptions (JsonPrefs options)
    {
        #region GameData Section
        if (!options.Contains ("GameData")) {
            Debug.LogError ("ProfileInfo.LoadServerOptions: 'GameData' key not found");
            return false;
        }

        options.BeginGroup ("GameData");

        maxPlayers = options.ValueInt ("maxPlayers", maxPlayers);
        respawnTime = options.ValueInt ("respawnTime", respawnTime);
        minPlayersForBattleStart = options.ValueInt("minPlayersForBattleStart", minPlayersForBattleStart);
        maxInactivityTime = options.ValueFloat ("maxInactivityTime", maxInactivityTime);

        battleDuration = options.ValueFloat ("battleDuration", battleDuration);

        maxPing = options.ValueInt("maxPing", maxPing);
        maxMasterPing = options.ValueInt("maxMasterPing", maxMasterPing);
        pingDetectionPeriod = options.ValueFloat("pingDetectionPeriod", pingDetectionPeriod);
        pingDetectionInterval = options.ValueFloat("pingDetectionInterval", pingDetectionInterval);

        BattleRoomLifetime = options.ValueFloat ("battleRoomLifetime", BattleRoomLifetime);
        prolongTimeAddition = options.ValueFloat ("prolongTimeAddition", prolongTimeAddition);
        goldRushMinTime = options.ValueFloat ("goldRushMinTime", goldRushMinTime);

        IsProlongTimeEnabled = options.ValueBool("isProlongTimeEnabled", IsProlongTimeEnabled);
        IsGoldRushEnabled = options.ValueBool("isGoldRushEnabled", IsProlongTimeEnabled);
        ProlongTimeShow = options.ValueFloat("prolongTimeShow", ProlongTimeShow);

        hastenPrice = options.ValueInt ("hastenPrice", hastenPrice);
        cheatTreshold = options.ValueInt ("cheatTreshold", cheatTreshold);

        fuelForInvite = options.ValueInt ("fuelForInvite", fuelForInvite);

        isAwardForRateGameEnabled = options.ValueBool ("rateGame/enabled", false);
        awardForRateGame = options.ValuePrice ("rateGame/price");

        isBonusForSocialActivityEnabled = options.ValueBool("bonusForSocialActivity/enabled", false);
        awardForSocialActivity = options.ValuePrice("bonusForSocialActivity/price");

        shellHitSyncInterval = options.ValueFloat("shellHitSyncInterval", shellHitSyncInterval);

        socialActivationReward = options.ValuePrice ("socialActivationReward");
        changeNickPrice = options.ValuePrice ("changeNickPrice");

        countryFlagsIsOn = options.ValueBool("countryFlagsIsOn", true);

        playerSandboxLevel = options.ValueInt("playerSandboxLevel", 6);

        isReconnectEnabled = options.ValueBool("isReconnectEnabled", true);
        reconnectTimeout = options.ValueFloat("reconnectTimeout", BattleConnectManager.RECONNECT_TIMEOUT);

        critDamageRatio = options.ValueFloat("critDamageRatio", GameManager.CRIT_DAMAGE_RATIO);
        normDamageRatio = options.ValueFloat("normDamageRatio", GameManager.NORM_DAMAGE_RATIO);

        levelUpAwardCoefficientSilver = options.ValueInt("levelUpAwardCoeffSilver", 500);
        levelUpAwardCoefficientGold = options.ValueFloat("levelUpAwardCoeffGold", 1.0f);

        isConsumableEnabled = options.ValueBool("isConsumableEnabled", true);
        if(!GUIPager.disabledDynamicPages.Contains("ConsumablesPage") && !isConsumableEnabled)
            GUIPager.disabledDynamicPages.Add("ConsumablesPage");

        #region gameModes (Параноидальные проверки)
        Dictionary<string, object> gameModesList = options.ValueObjectDict("gameModes");
        gameModes = new List<GameMode>();
        if (gameModesList != null)
            foreach(var gameModePair in gameModesList)
            {
                GameMode _gameMode;
                if (!HelpTools.TryParseToEnum(gameModePair.Key, out _gameMode))
                    continue;
                bool modeEnabled = Convert.ToBoolean(gameModePair.Value);
                if (_gameMode != GameMode.Unknown && !gameModes.Contains(_gameMode) && modeEnabled)
                    gameModes.Add(_gameMode);
            }
        if (gameModes.Count == 0)//Если так ничего и не добавили - добавляем дефолт
            gameModes.Add(DefaultGameMode);
        #endregion


        options.EndGroup ();
        #endregion
        #region BonusesData Section
        options.BeginGroup ("BonusesData");

        BonusChancesData.minPlayersForSpawn = options.ValueInt("minPlayersForBonusesSpawn", BonusChancesData.minPlayersForSpawn);

        BonusChancesData.goldChance = options.ValueInt ("goldChance", BonusChancesData.goldChance);
        BonusChancesData.goldChanceMin = options.ValueInt ("goldChanceMin", BonusChancesData.goldChanceMin);
        BonusChancesData.silverChance = options.ValueInt ("silverChance", BonusChancesData.silverChance);
        BonusChancesData.experienceChance = options.ValueInt ("experienceChance", BonusChancesData.experienceChance);

        BonusAmountsData = new BonusAmounts(options.ValueObjectList("amountDividers"));

        BonusAmountsData.minRandomCoefficient = options.ValueFloat("silverRandCoeffMin", 1.0f);
        BonusAmountsData.maxRandomCoefficient = options.ValueFloat("silverRandCoeffMax", 1.0f);

        options.EndGroup ();
        #endregion
        #region HangarController section
        if (options.Contains("HangarController"))
        {
            options.BeginGroup("HangarController");
            hideScoresTillLevel = options.ValueInt("hideScoresTillLevel", hideScoresTillLevel);
            refuellingTime = options.ValueInt("refuellingTime");
            options.EndGroup();
        }
        #endregion
        #region Social Settings Section
        if (!options.Contains("SocialSettings"))
        {
            Debug.LogError("There is no 'SocialSettings' section in options from server.");
            return false;
        }
        options.BeginGroup("SocialSettings");
        socialPrices = options.ValueObjectDict("prices");
        socialGroups = options.ValueObjectDict("groups");
        vkImagesAchievements = options.ValueObjectDict("vkImages/achievements");
        vkImagesLevels = options.ValueObjectList("vkImages/levels");
        options.EndGroup();
        #endregion
        #region SpecialOffer Section
        if (options.Contains("DiscountOfferItems"))
        {
            options.BeginGroup("DiscountOfferItems");

            bankOffersList = options.Contains("BankOffers") ? options.ValueObjectList("BankOffers") : null;
            vehicleOffersList = options.Contains("TankOffers") ? options.ValueObjectList("TankOffers") : null;
            decalOffersList = options.Contains("DecalOffers") ? options.ValueObjectList("DecalOffers") : null;
            patternOffersList = options.Contains("PatternOffers") ? options.ValueObjectList("PatternOffers") : null;
            vipsOffersList = options.Contains("VIPOffers") ? options.ValueObjectList("VIPOffers") : null;
            
            isBlackFriday = options.Contains("isBlackFriday") && options.ValueBool("isBlackFriday");

            options.EndGroup();
        }
        #endregion
        #region Kits Section
        bankKits.Clear();
        Dictionary<string, object> kitsDict = options.Contains("Kits") ? options.ValueObjectDict("Kits") : null;
        if(kitsDict != null)
        {
            var kitsDictPrefs = new JsonPrefs(kitsDict);
            foreach (KeyValuePair<string, object> kitTypePair in kitsDict)
            {
                //Чтобы не делать еще уровень вложенности, запихнул эту переменную сюда
                //Возможно надо запихнуть это в словарь Kits->Newbie->kitsAfterBattleShowingRate
                if (kitTypePair.Key  == "kitsAfterBattleShowingRate")
                {
                    newbieKitsAfterBattleShowingRate = kitsDictPrefs.ValueInt("kitsAfterBattleShowingRate");
                    continue;
                }

                BankKits.Type kitType;
                if (!HelpTools.TryParseToEnum(kitTypePair.Key, out kitType))
                    continue;
                var typedKitsDict = kitsDictPrefs.ValueObjectDict(kitTypePair.Key);
                foreach (KeyValuePair<string,object> kitPair in typedKitsDict)
                {
                    if (!bankKits.ContainsKey(kitType))
                        bankKits.Add(kitType, new Dictionary<string, BankKits.Data>());
                    var kitPrefs = new JsonPrefs(kitPair.Value);
                    bankKits[kitType][kitPair.Key] = new BankKits.Data(kitPair.Key, 0, 0, kitType);
                    if (kitPrefs.Contains("displayedAmount"))
                        bankKits[kitType][kitPair.Key].displayedAmount = kitPrefs.ValueInt("displayedAmount");
                    if (kitPrefs.Contains("dailyReward"))
                        bankKits[kitType][kitPair.Key].dailyReward = kitPrefs.ValueInt("dailyReward");
                    if (kitPrefs.Contains("content"))
                    {
                        Dictionary<string,object>content = kitPrefs.ValueObjectDict("content");
                        Dictionary<BankKits.Content, int> parsedDic = new Dictionary<BankKits.Content, int>();
                        if (kitPrefs != null)
                        {
                            foreach(var pair in content)
                            {
                                BankKits.Content contentType;
                                if (!HelpTools.TryParseToEnum(pair.Key, out contentType))
                                    continue;
                                int id = 0;
                                if (!int.TryParse(pair.Value.ToString(), out id))
                                    continue;
                                parsedDic[contentType] = id;
                            }
                        }
                        if (parsedDic.Count > 0)
                            bankKits[kitType][kitPair.Key].content = parsedDic;
                    }
                }
            }
        }
        #endregion
        #region BotSettings
        if (!options.Contains("BotSettings"))
        {
            Debug.LogError("There is no 'BotSettings' section in options from server");
            return false;
        }
        options.BeginGroup("BotSettings");
        isBotsEnabled = options.ValueBool("isBotsEnabled", false);
        botLifetime = options.ValueInt("botLifetime", 300);
        minBotCount = options.ValueInt("minBotCount", 0);
        maxBotCount = options.ValueInt("maxBotCount", 0);
        minBotEngageDelay = options.ValueInt("minBotEngageDelay");
        maxBotEngageDelay = options.ValueInt("maxBotEngageDelay");
        targetHumanChance = options.ValueInt("targetHumanChance");
        options.EndGroup();
        #endregion
        #region DailyBonus Section
        if (options.Contains("DailyBonus"))
        {
            var bonuses = options.ValueObjectList("DailyBonus");
            ProfileInfo.dailyBonusDaysCount = bonuses.Count;
            DailyBonus.dailyBonusesDict = new Dictionary<int, ProfileInfo.Price>();
            foreach (var o in bonuses)
            {
                var prefs = new JsonPrefs(o);
                DailyBonus.dailyBonusesDict.Add(prefs.ValueInt("day"), prefs.ValuePrice("price"));
            }
        }
        #endregion
        #region Tanks, Modules, Decals and Patterns section
        if (!options.Contains("TanksData"))
        {
            Debug.LogError("There is no 'TanksData' section in options from server");
            return false;
        }

        //File.WriteAllText("D:\\Temp\\data.txt", options.ToString());

        vehiclesDataStorage = options.ValueObjectDict ("TanksData");
        LoadConsumableInfos();
        ConsumableKitInfo.LoadConsumableKitInfos(options);
        #endregion
        #region MatchmakerSettings section
        if (!options.Contains("MatchmakerSettings"))
        {
            Debug.LogError("There is no 'MatchmakerSettings' section in options from server");
            return false;
        }

        Dictionary<string, object> mmSettings = options.ValueObjectDict("MatchmakerSettings");
        if (mmSettings.ContainsKey("rules"))
            MatchMaker.SetMatchmakerRules(mmSettings["rules"] as List<object>);
        else
        {
            Debug.LogError("There is no rules section in matchmaker settings");
            return false;
        }

        if (mmSettings.ContainsKey("specialCountries"))
        {
            LoadSpecialCountries(mmSettings["specialCountries"] as List<object>);
        }
        else
            Debug.Log("There is no special countries section in matchmaker settings");
        #endregion
        #region TournamentAwards section
        #region Test mockup
        //var testTournamentAwards = "{\"TournamentAwards\":{\"world\":[{\"place\":1,\"price\":{\"currency\":\"gold\",\"value\":300}},{\"place\":2,\"price\":{\"currency\":\"gold\",\"value\":200}},{\"place\":3,\"price\":{\"currency\":\"gold\",\"value\":100}},{\"place\":4,\"price\":{\"currency\":\"gold\",\"value\":50}},{\"place\":5,\"price\":{\"currency\":\"gold\",\"value\":50}},{\"place\":6,\"price\":{\"currency\":\"gold\",\"value\":50}},{\"place\":7,\"price\":{\"currency\":\"gold\",\"value\":50}},{\"place\":8,\"price\":{\"currency\":\"gold\",\"value\":50}},{\"place\":9,\"price\":{\"currency\":\"gold\",\"value\":50}},{\"place\":10,\"price\":{\"currency\":\"gold\",\"value\":50}}],\"country\":[{\"place\":1,\"price\":{\"currency\":\"gold\",\"value\":100}},{\"place\":2,\"price\":{\"currency\":\"gold\",\"value\":50}},{\"place\":3,\"price\":{\"currency\":\"gold\",\"value\":10}},{\"place\":4,\"price\":{\"currency\":\"gold\",\"value\":5}},{\"place\":5,\"price\":{\"currency\":\"gold\",\"value\":5}},{\"place\":6,\"price\":{\"currency\":\"gold\",\"value\":5}},{\"place\":7,\"price\":{\"currency\":\"gold\",\"value\":5}},{\"place\":8,\"price\":{\"currency\":\"gold\",\"value\":5}},{\"place\":9,\"price\":{\"currency\":\"gold\",\"value\":5}},{\"place\":10,\"price\":{\"currency\":\"gold\",\"value\":5}}],\"region\":[{\"place\":1,\"price\":{\"currency\":\"silver\",\"value\":50000}},{\"place\":2,\"price\":{\"currency\":\"silver\",\"value\":25000}},{\"place\":3,\"price\":{\"currency\":\"silver\",\"value\":10000}}],\"clans\":[{\"place\":1,\"price\":{\"currency\":\"silver\",\"value\":10000}},{\"place\":2,\"price\":{\"currency\":\"gold\",\"value\":10000}},{\"place\":3,\"price\":{\"currency\":\"silver\",\"value\":10}}],\"endTime\":1435517999}}";

        // Broken test mockup with wrong structure:
        //var testTournamentAwards = "{\"TournamentAwards\":{\"world\":[{\"place\":1,\"currency\":\"gold\",\"value\":300},{\"place\":2,\"currency\":\"gold\",\"value\":200},{\"place\":3,\"currency\":\"gold\",\"value\":100},{\"place\":4,\"price\":50},{\"place\":5},{\"place\":6,\"price\":{\"currency\":\"gold\",\"value\":50}},{\"place\":7,\"price\":{\"currency\":\"gold\",\"value\":50}},{\"place\":8,\"price\":{\"currency\":\"gold\",\"value\":50}},{\"place\":9,\"price\":{\"currency\":\"gold\",\"value\":50}},{\"place\":10,\"price\":{\"currency\":\"gold\",\"value\":50}}],\"country\":[{\"place\":1,\"price\":{\"currency\":\"gold\",\"value\":100}},{\"place\":2,\"price\":{\"currency\":\"gold\",\"value\":50}},{\"place\":3,\"price\":{\"currency\":\"gold\",\"value\":10}},{\"place\":4,\"price\":{\"currency\":\"gold\",\"value\":5}},{\"place\":5,\"price\":{\"currency\":\"gold\",\"value\":5}},{\"place\":6,\"price\":{\"currency\":\"gold\",\"value\":5}},{\"place\":7,\"price\":{\"currency\":\"gold\",\"value\":5}},{\"place\":8,\"price\":{\"currency\":\"gold\",\"value\":5}},{\"place\":9,\"price\":{\"currency\":\"gold\",\"value\":5}},{\"place\":10,\"price\":{\"currency\":\"gold\",\"value\":5}}],\"region\":[{\"place\":1,\"price\":{\"currency\":\"silver\",\"value\":50000}},{\"place\":2,\"price\":{\"currency\":\"silver\",\"value\":25000}},{\"place\":3,\"price\":{\"currency\":\"silver\",\"value\":10000}}],\"clans\":[{\"place\":1,\"price\":{\"currency\":\"silver\",\"value\":10000}},{\"place\":2,\"price\":{\"currency\":\"gold\",\"value\":10000}},{\"place\":3,\"price\":{\"currency\":\"silver\",\"value\":10}}],\"endTime\":1435517999}}";

        //tournamentAwardsStorage = new JsonPrefs(testTournamentAwards).ValueObjectDict("TournamentAwards");
        #endregion

        if (!options.Contains("TournamentAwards"))
        {
            Debug.LogError("There is no 'TournamentAwards' section in options from server");
            return false;
        }

        tournamentAwardsJSONPrefs = new JsonPrefs(options.ValueObjectDict("TournamentAwards"));
        weeklyTournamentEndTime = tournamentAwardsJSONPrefs.ValueLong("endTime");

        #endregion
        #region Chat Section
        if (!options.Contains("Chat")) {
            Debug.LogError("There is no 'Chat' section in options from server");
            return false;
        }

        options.BeginGroup("Chat");

        chatMinAccessLevel = options.ValueInt("minAccessLevel", chatMinAccessLevel);
        chatMinComplainLevel = options.ValueInt("minComplainLevel", chatMinComplainLevel);
        chatIsComplaintsEnabled = options.ValueBool("isComplaintsEnabled", chatIsComplaintsEnabled);
        chatModeratorAvailableBanTime = options.ValueInt("moderatorAvailableBanTime", chatModeratorAvailableBanTime);

        options.EndGroup ();
        #endregion
        #region ProfileInfo Section // Загрузка различных наград за действия игрока
        if (!options.Contains ("ProfileInfo")) {
            Debug.LogError ("There is no 'ProfileInfo' section in options from server");
            return false;
        }
        options.BeginGroup ("ProfileInfo");

        options.EndGroup ();
        #endregion
        #region Map Selection Section // Загрузка параметров выбора карт
        if (!options.Contains("Maps")){
            Debug.LogError ("There is no 'Maps' section in options from server");
            return false;
        }
        mapsList = options.ValueObjectList("Maps");

        allMapsDic.Clear();

        for (int i = 0; i < mapsList.Count; i++)
        {
            Dictionary<string, object> dic = (Dictionary<string, object>)mapsList[i];
            GameManager.MapId mapId = (GameManager.MapId)Convert.ToInt32(dic["mapId"]);
            MapInfo mapInfo = new MapInfo(dic, i);
            allMapsDic[mapId] = mapInfo;
            UpdateAvailableMaps();
        }

        #endregion
        #region Ads Section // Загрузка параметров рекламы от сетей TapJoy, Unity Ads и Chartboost
        options.BeginGroup("AdsServices");

        if (options.Contains("TapJoy"))
            isTapJoyEnabled = options.ValueBool("TapJoy/show", false);// (bool) options.ValueObjectDict("TapJoy")["show"];
        else
            Debug.Log("There is no \"TapJoy\" section in option from server!");

        if (options.Contains("UnityAds"))
            ThirdPartyAdsManager.SetupService("UnityAds", options.ValueObjectDict("UnityAds"));
        else
            Debug.Log("There is no \"UnityAds\" section in option from server!");

        if (options.Contains("ChartBoost"))
            ThirdPartyAdsManager.SetupService("ChartBoost", options.ValueObjectDict("ChartBoost"));
        else
            Debug.Log("There is no \"ChartBoost\" section in option from server!");

        adsFree = !(options.Contains("UnityAds") || options.Contains("ChartBoost"));

        // TODO: Распарсить настройки для рекламы своих проектов.
        //XDevsAdsManager.Setup();

        options.EndGroup();
        #endregion
        #region TapjoySettings

        options.BeginGroup("TapjoySettings");

        if (options.Contains("androidSdkKey"))
            tapJoyAndroidSdkKey = options.ValueString("androidSdkKey");
        else
            Debug.Log("TapJoy: There is no \"androidSdkKey\" section in option from server!");

        if (options.Contains("androidGcmSenderId"))
            tapJoyAndroidGcmSenderId = options.ValueString("androidGcmSenderId");
        else
            Debug.Log("TapJoy: There is no \"androidGcmSenderId\" section in option from server!");

        if (options.Contains("iosSdkKey"))
            tapJoyIosSdkKey = options.ValueString("iosSdkKey");
        else
            Debug.Log("TapJoy: There is no \"iosSdkKey\" section in option from server!");

        //Debug.LogFormat("Tapjoy Android SDK: {0}, IOS SDK: {1}, GCM key: {2}", tapJoyAndroidSdkKey, tapJoyIosSdkKey, tapJoyAndroidGcmSenderId);

        options.EndGroup();

        #endregion

        #region PushwooshSettings

        options.BeginGroup("PushwooshSettings");

        if (options.Contains("appCode"))
            pushwooshAppCode = options.ValueString("appCode");
        else
            Debug.Log("Pushwoosh: There is no \"appCode\" section in option from server!");

        if (options.Contains("gcmId"))
            pushwooshFCMSenderId = options.ValueString("gcmId");
        else
            Debug.Log("Pushwoosh: There is no \"gcmId\" section in option from server!");

        if (options.Contains("authToken"))
            pushwooshApiToken = options.ValueString("authToken");
        else
            Debug.Log("Pushwoosh: There is no \"gcmId\" section in option from server!");

        options.EndGroup();

        #endregion

        #region ClanSettings Section
        if (!options.Contains("ClanSettings"))
        {
            Debug.LogError("There is no 'ClanSettings' section in options from server");
            return false;
        }

        options.BeginGroup("ClanSettings");

        maxClanMembers = options.ValueInt("maxMembers", maxClanMembers);

        options.EndGroup();
        #endregion
        #region XDevsOffer Section
        if (options.Contains("XDevsOffer"))
        {
            options.BeginGroup("XDevsOffer");

            var games = options.ValueObjectDict("games");
            foreach (var g in games)
            {
                gamesOffers.Add (XDevs.Offers.GameOffer.fromDictionary (g.Key, g.Value as Dictionary<string, object>));
            }

            options.EndGroup();
        }
        #endregion
        #region PhotonSettings Section Настройки параметров соединения с облаком Фотона
        if (options.Contains ("PhotonSettings")) {
            options.BeginGroup ("PhotonSettings");

            var sProtocol = options.ValueString("protocol");
            ExitGames.Client.Photon.ConnectionProtocol protocol;
            if (HelpTools.TryParseToEnum (sProtocol, out protocol)) {
                PhotonNetwork.PhotonServerSettings.Protocol = protocol;
            }

            var sRegion = options.ValueString("region");
            CloudRegionCode region;
            if (HelpTools.TryParseToEnum (sRegion, out region)) {
                PhotonNetwork.PhotonServerSettings.PreferredRegion = region;
            }
            
            options.EndGroup ();
        }
        #endregion
        #region ClientLocalization
        if (options.Contains ("ClientLocalization")) {
            options.BeginGroup ("ClientLocalization");

            localizationStamp = options.ValueString("current");

            Debug.LogFormat ("Localization stamp {0}", localizationStamp);

            options.EndGroup ();
        }
        #endregion
        return true;
    }

    private void LoadConsumableInfos()
    {
        if (!vehiclesDataStorage.ContainsKey("consumables"))
        {
            Debug.LogError("No consumable info presented in data received from server!");
            return;
        }

        List<object> consList = vehiclesDataStorage["consumables"] as List<object>;
        vehiclesDataStorage.Remove("consumables");

        if (consList == null)
        {
            Debug.LogError("Invalid consumable info format in server data");
            return;
        }

        consumableInfos = new Dictionary<int, ConsumableInfo>(consList.Count);

        foreach (Dictionary<string, object> consDict in consList)
        {
            ConsumableInfo newConsumable = new ConsumableInfo(consDict);
            consumableInfos.Add(newConsumable.id, newConsumable);
        }
    }

    private void LoadSpecialCountries(List<object> countries)
    {
        if (countries == null)
        {
            Debug.LogError("Special countries list is null");
            return;
        }

        HashSet<string> specialCountries = new HashSet<string>();
        ProfileInfo.fromSpecialCountry = false;
        foreach (string country in countries)
        {
            string lower = country.ToLower();
            specialCountries.Add(lower);
            if (lower == ProfileInfo.CountryCode)
                ProfileInfo.fromSpecialCountry = true;
        }
        MatchMaker.SetSpecialCountries(specialCountries);
    }

#region Global time calculation and timer
    public static ObscuredDouble timeStamp;
    private double currentTimeStamp;
    private float lastPausedAt = 0;
    private static float timeCorrection = 0;
    public float GetCorrectedTime () { return Time.time + timeCorrection; }

    private static double deltaToRealTimeSinceStartup;
    public static bool timeGettingError = true;

    /// <summary>
    /// "Ангарное время".
    /// </summary>
    public static double CurrentTimeStamp
    {
        get
        {
            return instance != null
                ? instance.currentTimeStamp
                : Time.realtimeSinceStartup + deltaToRealTimeSinceStartup;
        }
    }

    /// <summary>
    /// "Ангарное время" с корректировкой на количество секунд в 1970 году.
    /// Может пригодиться для сравнения ангарного времени с временем, приходящим с сервера.
    /// </summary>
    public static double CorrectedCurrentTimeStamp
    {
        get { return DateTimeToUnixTimeStamp (CurrentDateTime.AddSeconds (31536000)); }
    }

    public static DateTime CurrentDateTime
    {
        get { return UnixTimeStampToDateTime (CurrentTimeStamp); }
    }

    public static DateTime CorrectedDateTime
    {
        get { return UnixTimeStampToDateTime(CorrectedCurrentTimeStamp); }
    }


    private void GameTimerTick ()
    {
        currentTimeStamp = timeStamp + (Time.realtimeSinceStartup - serverTimeReceiveSinceStart);
    }

    public static IEnumerator InitTime ()
    {
        DateTime networkTime = ServerTime;
        networkTime = ServerTime;

        if (networkTime.Year < DateTime.UtcNow.Year) {
            timeGettingError = true;
            GameData.CriticalError (Localizer.GetText ("NTPConnectionError"));
            yield break;
        }

        timeStamp = (networkTime.Subtract (new DateTime (1971, 1, 1, 0, 0, 0, DateTimeKind.Utc))).TotalSeconds;
        instance.currentTimeStamp = timeStamp;

        ProfileInfo.lastVisit = 0;
        ProfileInfo.lastProfileSaveTimestamp = 0;
        timeGettingError = false;
    }

    public static DateTime UnixTimeStampToDateTime (double unixTimeStamp)
    {
        // Unix timestamp is seconds past epoch.
        DateTime dtDateTime = new DateTime (1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
        dtDateTime = dtDateTime.AddSeconds (unixTimeStamp);

        return dtDateTime;
    }

    public static double DateTimeToUnixTimeStamp (DateTime dateTime)
    {
        DateTime defaultTime = new DateTime (1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
        TimeSpan difference = dateTime.ToUniversalTime () - defaultTime;

        return Math.Floor (difference.TotalSeconds);
    }

    void OnApplicationPause (bool paused)
    {
        if (paused) {
            lastPausedAt = Time.realtimeSinceStartup;
        }
        else {
            timeCorrection = timeCorrection + Time.realtimeSinceStartup - lastPausedAt;
        }
    }

#endregion

    void CheatDetected (EventId id, EventInfo info)
    {
#if UNITY_EDITOR
        Debug.LogError ("Cheat detected! Quit NAH!");
#endif
        QuitGame ();
    }

    // returns "en-US" / "ru-RU" / ...
    public static string GetLocale ()
    {
#if UNITY_ANDROID
        try
        {
            var locale = new AndroidJavaClass("java.util.Locale");
            var localeInst = locale.CallStatic<AndroidJavaObject>("getDefault");
            var name = localeInst.Call<string>("getLanguage") + "-" + localeInst.Call<string>("getCountry");
            return name;
        }
        catch(System.Exception)
        {
            return "Error";
        }
#else
        return "Not supported";
#endif
    }

    public string GetBundleId ()
    {
        if (string.IsNullOrEmpty (bundleId)) {
#if UNITY_ANDROID && !UNITY_EDITOR
            try {
                AndroidJavaClass unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
                AndroidJavaObject activity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
                bundleId = activity.Call<string> ("getPackageName");
            }
            catch (System.Exception e) {
                bundleId = androidBundleIds[CurrentGame];
            }
#else
            Debug.LogWarning(CurrentGame.ToString());
            bundleId = androidBundleIds[CurrentGame];
#endif
        }
        return bundleId;
    }

    public string GetBundleVersion ()
    {
        if (string.IsNullOrEmpty (bundleVersion)) {
#if UNITY_ANDROID && !UNITY_EDITOR
            try {
                AndroidJavaClass unityPlayer = new AndroidJavaClass ("com.unity3d.player.UnityPlayer");
                AndroidJavaObject activity = unityPlayer.GetStatic<AndroidJavaObject> ("currentActivity");
                AndroidJavaObject packageManager = activity.Call<AndroidJavaObject> ("getPackageManager");
                string packageId = GetBundleId ();
                AndroidJavaClass pmClass = new AndroidJavaClass ("android.content.pm.PackageManager");
                int flag = pmClass.GetStatic<int>("GET_META_DATA");
                AndroidJavaObject packageInfo = packageManager.Call<AndroidJavaObject> ("getPackageInfo", new object [] { GetBundleId (), flag });
                bundleVersion = packageInfo.Get<string> ("versionName");
            }
            catch (System.Exception e) {
                bundleVersion = playerBundleVersion;
            }
#else
            bundleVersion = playerBundleVersion;
#endif
        }
        return bundleVersion;
    }


#if UNITY_IPHONE
    [System.Runtime.InteropServices.DllImport ("__Internal")]
    private static extern bool XDevsCanOpenUrl (string url);
#endif

    /// <summary>
    /// Проверка приложения на его существование в системе. Работает только для Андроида.
    /// На всех остальных платформах всегда возвращает false
    /// </summary>
    /// <param name="packageId">Идентификатор приложения</param>
    /// <returns></returns>
    static public bool IsPackageIntalled(string packageId)
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        AndroidJavaClass up = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
        AndroidJavaObject ca = up.GetStatic<AndroidJavaObject>("currentActivity");
        AndroidJavaObject packageManager = ca.Call<AndroidJavaObject>("getPackageManager");
        //Debug.Log(" ********LaunchOtherApp ");
        AndroidJavaObject launchIntent = null;
        //if the app is installed, no errors. Else, doesn't get past next line
        try
        {
            launchIntent = packageManager.Call<AndroidJavaObject>("getLaunchIntentForPackage", packageId);
            //
            //        ca.Call("startActivity",launchIntent);
        }
        catch (System.Exception ex)
        {
            //Debug.Log("exception" + ex.Message);
        }
        return launchIntent != null;
#elif UNITY_WSA && UNITY_WP_8_1 && !UNITY_EDITOR
        var pkgs = Windows.Phone.Management.Deployment.InstallationManager.FindPackagesForCurrentPublisher();
        var game = pkgs.FirstOrDefault(i => i.Id.ProductId.ToLower() == packageId.ToLower());

        //Debug.Log("======================================== WP Package ==============================================");
        //    Debug.LogFormat("Author: {0}", game.Id.Author);
        //    Debug.LogFormat("Name: {0}", game.Id.Name);
        //    Debug.LogFormat("ProductId: {0}", game.Id.ProductId);
        //    Debug.LogFormat("Publisher: {0}", game.Id.Publisher);
        //    //Debug.LogFormat("FamilyName: {0}", game.Id.FamilyName);
        //    //Debug.LogFormat("FullName: {0}", game.Id.FullName);
        //    //Debug.LogFormat("ResourceId: {0}", game.Id.ResourceId);
        //    //Debug.LogFormat("PublisherId: {0}", game.Id.PublisherId);
        //Debug.Log("======================================== /WP Package ==============================================");


        return game != null;
#elif UNITY_WSA && UNITY_WSA_8_1 && !UNITY_EDITOR
        var pm = new Windows.Management.Deployment.PackageManager();
        Windows.ApplicationModel.Package game = null;
        try
        {
            var pkgs = pm.FindPackages();
            game = pkgs.FirstOrDefault(i => i.Id.FamilyName.ToLower() == packageId.ToLower());

            //Debug.Log("======================================== WSA Package ==============================================");
            //    Debug.LogFormat("Author: {0}", game.Id.Author);
            //    Debug.LogFormat("Name: {0}", game.Id.Name);
            //    Debug.LogFormat("ProductId: {0}", game.Id.ProductId);
            //    Debug.LogFormat("Publisher: {0}", game.Id.Publisher);
            //    //Debug.LogFormat("FamilyName: {0}", game.Id.FamilyName);
            //    //Debug.LogFormat("FullName: {0}", game.Id.FullName);
            //    //Debug.LogFormat("ResourceId: {0}", game.Id.ResourceId);
            //    //Debug.LogFormat("PublisherId: {0}", game.Id.PublisherId);
            //Debug.Log("======================================== /WP Package ==============================================");

        }
        catch (UnauthorizedAccessException)
        {
            Debug.LogError("packageManager.FindPackages() failed because access was denied. This program must be run from an elevated command prompt.");
        }
        catch (Exception ex)
        {
            Debug.LogErrorFormat("packageManager.FindPackages() failed, error message: {0}", ex.Message);
            Debug.LogErrorFormat("Full Stacktrace: {0}", ex.ToString());
        }

        return game != null;
#elif UNITY_IPHONE && !UNITY_EDITOR
        return XDevsCanOpenUrl (packageId);
#else
        return false;
#endif
    }


    /// <summary>
    /// use it to to check if game mode contains some flag. NOTE: if you need to know
    /// strict equality of game mode - use GameData.CurrentGame == Game.[game mode]
    /// </summary>
    /// <param name="requiredMode">required game/mode or set of game+mode</param>
    /// <returns>true if current game mode contains requested one</returns>
    public static bool IsGame(Game requiredMode)
    {
        int currGame = (int)CurrentGame;
        int reqGame = (int)requiredMode;
        return (currGame & reqGame) != 0;
    }

    public static void QuitGame ()
    {
        Debug.Log("GameData.QuitGame()");
#if UNITY_WEBPLAYER || UNITY_WEBGL
        Application.ExternalEval("(function (){location.reload();})();");
#else
        Application.Quit ();
#endif
    }
#if UNITY_EDITOR
    public void SetPlayerBundleVersion(string newVer)
    {
        playerBundleVersion = newVer;
    }

    public string GetPlayerBundleVersion()
    {
        return playerBundleVersion;
    }

    public void SetBuildNumber(string number)
    {
        buildNumber = number;
    }

    public string GetBuildNumber()
    {
        return buildNumber;
    }
#endif

    public static Camera CurSceneGuiCamera
    {
        get
        {
            if (HangarController.Instance != null && HangarController.Instance.GuiCamera)
                return HangarController.Instance.GuiCamera;
            if (BattleGUI.Instance != null && BattleGUI.Instance.GuiCamera != null)
                return BattleGUI.Instance.GuiCamera;
            return null;
        }
    }

    public static tk2dCamera CurSceneTk2dGuiCamera
    {
        get
        {
            if (HangarController.Instance != null)
                return HangarController.Instance.Tk2dGuiCamera;
            if (BattleGUI.Instance != null)
                return BattleGUI.Instance.Tk2dGuiCamera;
            return null;
        }
    }

    public static GameManager.MapId GetTutorialMapId()
    {
        foreach (var mapInfo in allMapsDic)
            if (mapInfo.Value.isTutorialMap)
                return mapInfo.Value.id;

        Debug.LogError(string.Format("Tutorial map not found!!!"));
        return availableMapsDic.First().Value.id;//throw new KeyNotFoundException("Tutorial map not selected in admin panel");
    }

    public static void UpdateAvailableMaps()
    {
        availableMapsDic.Clear();
        foreach (var mapPair in allMapsDic)
            if (mapPair.Key != GameManager.MapId.random_map && mapPair.Value.isEnabled && mapPair.Value.IsAvailableByLevel)
                availableMapsDic[mapPair.Key] = mapPair.Value;
    }
}
