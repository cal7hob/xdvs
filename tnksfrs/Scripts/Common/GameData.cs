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
using System.Linq;
using Disconnect;
using Matchmaking;
#if UNITY_EDITOR
using UnityEditor;
#endif
using XD;

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
    Armada2             = 9,
}


[Flags]
public enum Game : ulong
{
    Undefined                           = 0,            // project game was not set
    IronTanks                           = 0x00000001,   // 0000 0000 0000 0000 0000 0000 0001
    FutureTanks                         = 0x00000010,   // 0000 0000 0000 0000 0000 0001 0000
    ToonWars                            = 0x00000100,   // 0000 0000 0000 0000 0001 0000 0000
    SpaceJet                            = 0x00001000,   // 0000 0000 0000 0001 0000 0000 0000
    ApocalypticCars                     = 0x00010000,   // 0000 0000 0001 0000 0000 0000 0000
    BattleOfWarplanes                   = 0x00100000,   // 0000 0001 0000 0000 0000 0000 0000
    BattleOfHelicopters                 = 0x01000000,   // 0001 0000 0000 0000 0000 0000 0000
    Armada                              = 0x00000002,   // 0000 0000 0000 0000 0000 0000 0010   
    WWR                                 = 0x00000004,   // 0000 0000 0000 0000 0000 0000 0100                  
    Armada2                             = 0x00000008,   // 0000 0000 0000 0000 0000 0000 1000
        
    CNBuild = 0x10000000,                               // 0001 0000 0000 0000 0000 0000 0000 0000
    IronTanksCN = IronTanks | CNBuild,                  // 0001 0000 0000 0000 0000 0000 0000 0001
}


public class GameData : MonoBehaviour, IGameData
{
    #region IStatic
    public bool IsEmpty
    {
        get
        {
            return false;
        }
    }

    public StaticType StaticType
    {
        get
        {
            return StaticType.GameData;
        }
    }

    public void SaveInstance()
    {
        StaticContainer.Set(StaticType, this);
    }

    public void DeleteInstance()
    {
        StaticContainer.Set(StaticType, null);
    }
    #endregion

    #region ISender
    public string Description
    {
        get
        {
            return "[GameData] " + name;
        }

        set
        {
            name = value;
        }
    }

    private List<ISubscriber> subscribers = null;

    public List<ISubscriber> Subscribers
    {
        get
        {
            if (subscribers == null)
            {
                subscribers = new List<ISubscriber>();
            }
            return subscribers;
        }
    }

    public void AddSubscriber(ISubscriber subscriber)
    {
        if (Subscribers.Contains(subscriber))
        {
            return;
        }
        Subscribers.Add(subscriber);
    }

    public void RemoveSubscriber(ISubscriber subscriber)
    {
        Subscribers.Remove(subscriber);
    }

    public void Event(Message message, params object[] parameters)
    {
        for (int i = 0; i < Subscribers.Count; i++)
        {
            Subscribers[i].Reaction(message, parameters);
        }
    }
    #endregion

    #region ISubscriber       
    public void Reaction(Message message, params object[] parameters)
    {
        switch (message)
        {
            case Message.MessageBoxResult:
                switch (parameters.Get<MessageBoxType>())
                {
                    case MessageBoxType.Reconnect:
                        if (parameters.Get<bool>())
                        {
                            StartCoroutine(ReceiveServerData());
                        }
                        else
                        {
                            PlayerPrefs.SetInt("ServerId", -1);
                            Application.Quit();
                        }
                        break;

                    case MessageBoxType.ServerDataError:
                        Application.Quit();
                        break;
                }
                
                break;
        }
    }
    #endregion

    [Serializable]
    public enum GameMode
    {
        Unknown = 0,
        Deathmatch = 1,
        Team = 2,
    }

    public static string GameModeLocalizationKey
    {
        get
        {
            return string.Format("lbl{0}Mode", Mode);
        }
    }

    public static Game CurrentGame
    {
        get; private set;
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

    public static BonusChances BonusChancesData
    {
        get; private set;
    }

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
            {
                return divider;
            }

            //if (Stati)
            //{
            //    Debug.LogWarningFormat("Suitable divider for tankGroupDelta = {0} not found! Using 1.", tankGroupDelta);
            //}

            return new Divider();
        }
    }

    public static BonusAmounts BonusAmountsData
    {
        get; private set;
    }

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

    private static float battleDuration = 300;
    private static float prolongTimeAddition = 180;
    private static float goldRushMinTime = 30;
    private static int maxPing = 300; // Максимальный средний пинг для присутствия в комнате
    private static int maxMasterPing = 300; // Максимальный средний пинг для присутствия в комнате для мастера

    public static int MaxPing
    {
        get
        {
            return PhotonNetwork.isMasterClient ? maxMasterPing : maxPing;
        }
    }
    public static float pingDetectionPeriod = 8f; // Длительность периода для рассчета среднего значения пинга клиента
    public static float pingDetectionInterval = 1f; // Интервал снятия текущего значения пинга
    public static ObscuredInt playerSandboxLevel = 6;
    public static ObscuredBool isReconnectEnabled = true;
    public static ObscuredFloat reconnectTimeout = BattleConnectManager.RECONNECT_TIMEOUT;
    public static ObscuredFloat critDamageRatio = XD.Constants.CRIT_DAMAGE_RATIO;
    public static ObscuredFloat normDamageRatio = XD.Constants.NORM_DAMAGE_RATIO;
    public static ObscuredInt levelUpAwardCoefficientSilver;
    public static ObscuredFloat levelUpAwardCoefficientGold;
    public static ObscuredBool isBotsEnabled = true;
    public static ObscuredFloat botLifetime;
    public static ObscuredInt minBotCount = 1;
    public static ObscuredInt maxBotCount = 3;
    public static int minBotEngageDelay = 5;
    public static int maxBotEngageDelay = 5;
    public static int targetHumanChance = 70;

    //////////////////////////////////////////////////////////////

    // TODO: do not forget to delete or change it if any changes will occure
    public const string UNKNOWN_FLAG_NAME = "xx";

    [SerializeField]
    private string playerBundleVersion;
    [SerializeField]
    private string buildNumber = "0";//номер сборки - чтобы не поднимать версию. Например версия 1.91 (сборка 0)

    private static string bundleId = "";
    private static string bundleVersion;

    public static Dictionary<string, object> vehiclesDataStorage = null;
    public static ObscuredString vipOffersStorage = null;
    public static IList<IVipOffer> vipOffers = null;

    public static bool IsProlongTimeEnabled
    {
        get; private set;
    }
    public static float ProlongTimeShow
    {
        get; private set;
    }
    public static bool IsGoldRushEnabled
    {
        get; private set;
    }

    public string AuthenticationKey = "";

    public static string InterfaceShortName
    {
        get
        {
            return GetInterfaceShortName(CurInterface);
        }
    }

    public static string GetInterfaceShortName(Interface customInterface)
    {
        switch (customInterface)
        {
            case Interface.Armada2:
                return "AR";
            default:
                return new string((from item in customInterface.ToString().ToCharArray() where Char.IsUpper(item) select item).ToArray());
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

    // Список ВИПок
    public static List<object> vipProducts;

    // Список Товаров
    public static List<object> products;

    // TapJoy реклама (вкл/выкл)
    public static bool isTapJoyEnabled;

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

    public static bool IsTeamMode
    {
        get
        {
            return Mode == GameMode.Team;
        }
    }
    public static bool IsMode(GameMode _mode)
    {
        return Mode == _mode;
    }

    public static float GoldRushMinTime
    {
        get
        {
            return goldRushMinTime;
        }
    }

    public static Dictionary<string, object> socialPrices = new Dictionary<string, object>();
    public static Dictionary<string, object> socialGroups = new Dictionary<string, object>();
    public static Dictionary<string, object> vkImagesAchievements = new Dictionary<string, object>();
    public static List<object> vkImagesLevels = new List<object>();

    public static Interface CurInterface
    {
        get
        {
#if UNITY_EDITOR
            if (!EditorApplication.isPlaying)
            {
                if (UnityEditor.PlayerSettings.applicationIdentifier == androidBundleIds[Game.Armada2])
                    return Interface.Armada2;
                else
                {
                    Debug.LogError("UNKNOWN BUNDLE! Cant recognize interface type!");
                    return Interface.FutureTanks;
                }
            }
#endif
            if (GameData.IsGame(Game.Armada2))
            {
                return Interface.Armada2;
            }
            else
            {
                Debug.LogError("Unknown Interface!!!");
                return Interface.FutureTanks;
            }
        }
    }

    public static void Init()
    {
        #if DEBUG_FOR_WEB_GL
        Debug.LogError("TF: GameData.Init()");
        #endif
        if (inited)
            return;
#if IS_ARMADA2
        CurrentGame = Game.Armada2;
#endif

#if IS_VK_APP
        CurrentGame |= Game.VKBuild;
#endif
#if IS_AMAZON_APP
        CurrentGame |= Game.AmazonBuild;
#endif

        var qualityManagerObj = GameObject.Find("QualityManager");
        var qualityManager = qualityManagerObj ? qualityManagerObj.GetComponent<QualityManager>() : Instantiate(Resources.Load<QualityManager>("Common/QualityManager"));
        qualityManager.name = "QualityManager";

        inited = true;
		#if DEBUG_FOR_WEB_GL
		Debug.LogError("TF: GameData.Init() --> inited = TRUE");
		#endif

        Debug.LogWarning(string.Format("<color=yellow>Game = {0}</color>", CurrentGame.ToString()));
#if UNITY_EDITOR
        //Лог для поиска по времени в файле лога редактора
        Debug.LogWarning(string.Format("<color=yellow>LocalTime = {0}</color>", DateTime.Now.ToString()));
#endif
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
        int iName = ((int)(_game)) & ~((int)(Game.CNBuild));
        return (Game)iName;
    }

    // Locale data
    public CultureInfo cultureInfo;

    public static Dictionary<Game, string> androidBundleIds = new Dictionary<Game, string>() {
        {Game.Armada2,             "com.extremedevelopers.tankforce"},
    };

    private static GameMode gameMode = GameMode.Unknown;

    public static GameMode Mode
    {
        get
        {
            //Защита от тутора в командном режиме
            //if (!ProfileInfo.IsBattleTutorialCompleted)
            //{
            //    return GameMode.Deathmatch;
            //}
            //else
            //{
            //    return GameMode.Team;
            //}

            return gameMode;
        }
        set
        {
            if (value == GameMode.Unknown || !gameModes.Contains(value))
            {
                if (gameModes.Count > 0)
                    value = gameModes[0];
                else
                    value = DefaultGameMode;
            }
            if (gameMode != value)
            {
                gameMode = value;
                Dispatcher.Send(EventId.GameModeChanged, new EventInfo_SimpleEvent());
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

    public static float BattleDuration
    {
        get
        {
            return battleDuration;
        }
    }

    public static float BattleRoomLifetime
    {
        get; private set;
    }

    public static float ProlongTimeAddition
    {
        get
        {
            return prolongTimeAddition;
        }
    }

    public static string HangarSceneName
    {
        get
        {
            return ProfileInfo.IsPlayerVip ? "scnh_premium" : "scnh_standard";
        }
    }

    public static bool TimeLoaded
    {
        get
        {
            return timeLoaded;
        }
    }

    public static DateTime ServerTime
    {
        get
        {
            return serverTime;
        }
    }

    public static bool ServerDataReceived
    {
        get
        {
            return serverDataReceived;
        }
    }

    /*	UNITY SECTION	*/

    private static void SetGuiAtlasAccordingRamAndRes()
    {
        if (isTk2dPlatformSet)
            return;

//#if UNITY_WEBGL || UNITY_WEBPLAYER || (UNITY_WSA && UNITY_WSA_8_1)
//        tk2dSystem.CurrentPlatform = "2x";
//#else
//        tk2dSystem.CurrentPlatform = (Screen.width < 1280 || SystemInfo.systemMemorySize < 700) ? "1x" : "2x";
//#endif
//        Debug.Log("tk2dSystem.CurrentPlatform: " + tk2dSystem.CurrentPlatform);

        isTk2dPlatformSet = true;
    }

    private void Awake()
    {
        #if DEBUG_FOR_WEB_GL
        Debug.LogError("TF: GameData.Awake");
        #endif
        if (instance != null)
		{
			#if DEBUG_FOR_WEB_GL
			Debug.LogError("TF: GameData.Awake: if (instance != null)");
			#endif
            Destroy(gameObject);
            return;
        }        
        SaveInstance();

        SetGuiAtlasAccordingRamAndRes();
        Init();
		#if DEBUG_FOR_WEB_GL
		Debug.LogError("TF: GameData.Awake: PhotonSerializersRegistration();");
		#endif
        PhotonSerializersRegistration();
		#if DEBUG_FOR_WEB_GL
		Debug.LogError("TF: GameData.Awake: ObscuredValuesInit();");
		#endif
        ObscuredValuesInit();
		#if DEBUG_FOR_WEB_GL
		Debug.LogError("TF: GameData.Awake: instance = this;");
		#endif
        instance = this;
		#if DEBUG_FOR_WEB_GL
		Debug.LogError("TF: GameData.Awake: DontDestroyOnLoad(gameObject);");
		#endif
        DontDestroyOnLoad(gameObject);

        BattleRoomLifetime = 1;
        BonusChancesData = new BonusChances();

        serverTime = DateTime.MinValue;
        timeLoaded = false;
        //serverDataReceived = false;

        // Load locale data from device
        try
        {
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
            cultureInfo = new CultureInfo("en-US");
#endif
        }
        catch (System.Exception)
        {
            cultureInfo = new CultureInfo("en-US");
			//Debug.LogError ("TF: GameData: System.Exception: Loaded locale " + cultureInfo.Name);
        }

        // Выставляем инвариантную культуру для избегания проблем с конвертирование чисел в строки и обратно
#if UNITY_WSA && !UNITY_EDITOR
        CultureInfo.CurrentCulture.NumberFormat = CultureInfo.InvariantCulture.NumberFormat;
#else
        System.Threading.Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;
#endif

        Dispatcher.Subscribe(EventId.CheatDetected, CheatDetected);

        Dispatcher.Subscribe(EventId.AfterWebPlatformDefined, StartInit, 1);
		#if DEBUG_FOR_WEB_GL
			Debug.LogError("TF: GameData.Awake:Dispatcher.Subscribe(EventId.AfterWebPlatformDefined, StartInit, 1);");
		#endif
    }

    private void Start()
    {
        StaticType.UI.AddSubscriber(this);
    }

    private void StartInit(EventId id, EventInfo info)
    {
			#if DEBUG_FOR_WEB_GL
			Debug.LogError("TF: GameData.StartInit");
			#endif
			StartCoroutine(ReceiveServerData());
    }

    private void OnDestroy()
    {
        DeleteInstance();
        deltaToRealTimeSinceStartup = currentTimeStamp - Time.realtimeSinceStartup;
        //timeStamp = GameData.CurrentTimeStamp - GetCorrectedTime ();
        Dispatcher.Unsubscribe(EventId.CheatDetected, CheatDetected);
        StaticType.UI.RemoveSubscriber(this);
    }

    /* PUBLIC SECTION */

    public static void CriticalError(string text)
    {
        //if(Application.loadedLevelName != Constants.LOADING_SCENE_NAME)
        //SplashScreen.SetActive(false);
        //XdevsSplashScreen.SetActiveWaitingIndicator(false);        
        //MessageBox.Show(MessageBox.Type.Critical, text);        
        //Debug.LogError("MessageBoxType.ServerDataError");
        StaticContainer.UI.Reaction(Message.MessageBox, MessageBoxType.ServerDataError, "UI_MB_CriticalError", text, "UI_Quit");
    }

    private IEnumerator ReceiveServerData()
    {
        Debug.LogWarning("ReceiveServerData!");
        if (serverDataReceived)
        {
            Debug.LogWarning("Server data already received! Break double loading!");
            yield break;
        }

        //while (!Localizer.Loaded)
        //{
        //    yield return null;
        //}

        Debug.Log ("Start profile load");
        timeGettingError = true;
        yield return StartCoroutine(ProfileInfo.Load());
        Debug.Log ("Done profile load");

        Http.Request req = null;
        WWW www = null;
        bool connectionSucceeded = false;
        float connectionTime = 0;
        while (!connectionSucceeded)
        {
            try
            {
                req = Http.Manager.Instance().CreateRequest("/options/load");
                www = req.CreateWWW();
            }
            catch (Exception e)
            {
                Debug.Log("0. Receive Error: " + "NTPConnectionError".Translate());

                Debug.LogException(e);
                CriticalError("UI_MB_NTPConnectionError");
                yield break;
            }

            while (!www.isDone)
            {
                connectionTime += Time.deltaTime;
                if (connectionTime > 10.0f)
                {
                    connectionTime = 0;
                    break;
                }
                yield return new WaitForSeconds(0.1f);
            }

            if (www.isDone && string.IsNullOrEmpty(www.error))
            {
                connectionSucceeded = true;
            }
            else if (!www.isDone || req.GetResponse().IsNetworkError)
            {
                Debug.LogError(www.error);
                userAnswer = MessageBox.Answer.Uncertain;
                StaticContainer.UI.Reaction(Message.MessageBox, MessageBoxType.Reconnect, "UI_MB_CriticalError", "UI_MB_RetryConnectQuestion", "UI_Yes", "UI_No");

                while (userAnswer == MessageBox.Answer.Uncertain)
                {
                    yield return new WaitForSeconds(0.3f);
                }

                if (userAnswer == MessageBox.Answer.No)
                {
                    QuitGame();
                }

                www = null;
                req = null;
            }
            else
            {
                break;
            }
        }

        if (req.GetResponse().HttpStatusCode == 502)
        {
            Debug.Log("1. Receive Error: " + "ServerUnavailable".Translate());
            CriticalError("UI_MB_ServerUnavailable");
            yield break;
        }
        else if (req.GetResponse().ServerError == Http.Error.PlayerBanned)
        {
            Debug.Log("2. Receive Error: " + "lblBannedMsg".Translate());
            CriticalError("UI_MB_BannedMsg");
            yield break;
        }
        else if (req.GetResponse().HaveErrors)
        {
            Debug.LogErrorFormat("Error from server {0}", req.GetResponse().ServerError);
            Debug.Log("3. Receive Error: " + req.GetResponse().ServerError.ToString().Translate());
            CriticalError(string.Format("UI_MB_ReceiveError".Translate(), req.GetResponse().HttpStatusCode));
            yield break;
        }

        try
        {
            //Debug.LogWarning (req.GetResponse ().text);
            var data = new JsonPrefs(req.GetResponse().Data);
            if (!data.Contains("version"))
            {
                Debug.LogError("'Version' key in loaded options not found, options: " + req.GetResponse().text);
                CriticalError("UI_MB_ServerDataError");
                yield break;
            }
            //Debug.Log ( MiniJSON.Json.Serialize (data.ValueObjectDict("version")));
            data.BeginGroup("version");

            ProfileInfo.Version = data.ValueString("version", "") != "0" ? data.ValueString("version") : null;
            ProfileInfo.ImportantUpdate = data.ValueInt("important", 0) == 1;
            ProfileInfo.MarketURL = data.ValueString("market", "");
            data.EndGroup();

            if (ProfileInfo.ImportantUpdate)
            {
                serverDataReceived = true;
                yield break;
            }

            if (!data.Contains("time"))
            {
                Debug.LogError("'time' key in loaded options not found, options: " + req.GetResponse().text);
                CriticalError("UI_MB_ServerDataError");
                yield break;
            }
            long tm = data.ValueLong("time", 0);
            serverTime = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc).AddSeconds(tm);
            serverTimeReceiveSinceStart = Time.realtimeSinceStartup;
            timeLoaded = true;

            if (!data.Contains("location"))
            {
                Debug.LogError("'location' key in loaded options not found, options: " + req.GetResponse().text);
                CriticalError("UI_MB_ServerDataError");
                yield break;
            }
            ProfileInfo.CountryCode = data.ValueString("location/country_code", "").ToLower();

            if (!data.Contains("playerId"))
            {
                Debug.LogError("'playerId' key in loaded options not found, options: " + req.GetResponse().text);
                CriticalError("UI_MB_ServerDataError");
                yield break;
            }
            ProfileInfo.playerId = data.ValueInt("playerId", 0);//Convert.ToInt32 (data["playerId"]);
            ProfileInfo.SetPlayerPrivilege(data.ValueInt("playerPrivilege"));

            //if (!data.Contains ("token")) {
            //    Debug.LogError ("'token' key in loaded options not found, options: " + req.GetResponse ().text);
            //    CriticalError ("UI_MB_ServerDataError");
            //    yield break;
            //}
            Http.Manager.Instance().sessionToken = data.ValueString("token", "");

            ///////////////////////////////////////////////////////////
            // Options loading
            if (!data.Contains("options"))
            {
                Debug.LogError("'options' key in loaded options not found, options: " + req.GetResponse().text);
                CriticalError("UI_MB_ServerDataError");
                yield break;
            }

            data.BeginGroup("options");
            if (!LoadServerOptions(data))
            {
                Debug.LogError("'options' load error, options: " + req.GetResponse().text);
                CriticalError("UI_MB_ServerDataError");
                yield break;
            }
            data.EndGroup();
        }
        catch (Exception e)
        {
            Http.Manager.ReportException("GameData.ReceiveServerData", e);
            Debug.LogException(e);
        }

        yield return StartCoroutine(InitTime());

        this.InvokeRepeating(GameTimerTick, 0, 1);

        serverDataReceived = true;
        Dispatcher.Send(EventId.ServerDataReceived, null);
    }

    private static void PhotonSerializersRegistration()
    {
        ExitGames.Client.Photon.PhotonPeer.RegisterType(typeof(Dispatcher.PhotonEventAdapter), (byte)'A', Dispatcher.PhotonEventAdapter.SerializeAdapter,
                        Dispatcher.PhotonEventAdapter.DeserializeAdapter);
        ExitGames.Client.Photon.PhotonPeer.RegisterType(typeof(TankData), (byte)'T', TankData.Serialize,
                TankData.Deserialize);
        ExitGames.Client.Photon.PhotonPeer.RegisterType(typeof(BonusItem.BonusInfo), (byte)'B', BonusItem.BonusInfo.Serialize,
                BonusItem.BonusInfo.Deserialize);
        ExitGames.Client.Photon.PhotonPeer.RegisterType(typeof(PlayerDisconnectInfo), (byte)'D', PlayerDisconnectInfo.Serialize,
                PlayerDisconnectInfo.Deserialize);
        ExitGames.Client.Photon.PhotonPeer.RegisterType(typeof(RoomCountryInfo), (byte)'R', RoomCountryInfo.Serialize,
                RoomCountryInfo.Deserialize);
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

    private bool LoadServerOptions(JsonPrefs options)
    {
        #region GameData Section
        if (!options.Contains("GameData"))
        {
            Debug.LogError("ProfileInfo.LoadServerOptions: 'GameData' key not found");
            return false;
        }

        options.BeginGroup("GameData");

        if (options.Contains("ratingFactor"))
        {
            StaticType.Awards.Instance<IAwards>().SetRatingRatio(options.ValueFloat("ratingFactor"));
        }

        maxPlayers = options.ValueInt("maxPlayers", maxPlayers);
        respawnTime = options.ValueInt("respawnTime", respawnTime);
        minPlayersForBattleStart = options.ValueInt("minPlayersForBattleStart", minPlayersForBattleStart);
        maxInactivityTime = options.ValueFloat("maxInactivityTime", maxInactivityTime);

        battleDuration = options.ValueFloat("battleDuration", battleDuration);

        maxPing = options.ValueInt("maxPing", maxPing);
        maxMasterPing = options.ValueInt("maxMasterPing", maxMasterPing);
        pingDetectionPeriod = options.ValueFloat("pingDetectionPeriod", pingDetectionPeriod);
        pingDetectionInterval = options.ValueFloat("pingDetectionInterval", pingDetectionInterval);

        BattleRoomLifetime = options.ValueFloat("battleRoomLifetime", BattleRoomLifetime);
        prolongTimeAddition = options.ValueFloat("prolongTimeAddition", prolongTimeAddition);
        goldRushMinTime = options.ValueFloat("goldRushMinTime", goldRushMinTime);

        IsProlongTimeEnabled = options.ValueBool("isProlongTimeEnabled", IsProlongTimeEnabled);
        IsGoldRushEnabled = options.ValueBool("isGoldRushEnabled", IsProlongTimeEnabled);
        ProlongTimeShow = options.ValueFloat("prolongTimeShow", ProlongTimeShow);

        hastenPrice = options.ValueInt("hastenPrice", hastenPrice);
        cheatTreshold = options.ValueInt("cheatTreshold", cheatTreshold);

        fuelForInvite = options.ValueInt("fuelForInvite", fuelForInvite);

        isAwardForRateGameEnabled = options.ValueBool("rateGame/enabled", false);
        awardForRateGame = options.ValuePrice("rateGame/price");

        isBonusForSocialActivityEnabled = options.ValueBool("bonusForSocialActivity/enabled", false);
        awardForSocialActivity = options.ValuePrice("bonusForSocialActivity/price");

        shellHitSyncInterval = options.ValueFloat("shellHitSyncInterval", shellHitSyncInterval);

        CurrencyValue connectionSocialReward = options.ValuePrice("socialActivationReward").ToCurrencyValue();

        changeNickPrice = options.ValuePrice("changeNickPrice");

        countryFlagsIsOn = options.ValueBool("countryFlagsIsOn", true);

        playerSandboxLevel = options.ValueInt("playerSandboxLevel", 6);

        isReconnectEnabled = options.ValueBool("isReconnectEnabled", true);
        reconnectTimeout = options.ValueFloat("reconnectTimeout", BattleConnectManager.RECONNECT_TIMEOUT);

        critDamageRatio = options.ValueFloat("critDamageRatio", Constants.CRIT_DAMAGE_RATIO);
        normDamageRatio = options.ValueFloat("normDamageRatio", Constants.NORM_DAMAGE_RATIO);

        levelUpAwardCoefficientSilver = options.ValueInt("levelUpAwardCoeffSilver", 500);
        levelUpAwardCoefficientGold = options.ValueFloat("levelUpAwardCoeffGold", 1.0f);

        #region gameModes (Параноидальные проверки)
        Dictionary<string, object> gameModesList = options.ValueObjectDict("gameModes");
        gameModes = new List<GameMode>();
        if (gameModesList != null)
        {
            foreach (var gameModePair in gameModesList)
            {
                GameMode _gameMode;
                if (!HelpTools.TryParseToEnum(gameModePair.Key, out _gameMode))
                {
                    continue;
                }

                bool modeEnabled = Convert.ToBoolean(gameModePair.Value);
                if (_gameMode != GameMode.Unknown && !gameModes.Contains(_gameMode) && modeEnabled)
                {
                    gameModes.Add(_gameMode);
                }
            }
        }

        if (gameModes.Count == 0)//Если так ничего и не добавили - добавляем дефолт
        {
            gameModes.Add(DefaultGameMode);
        }
        #endregion
        options.EndGroup();

        #endregion
        #region BonusesData Section
        options.BeginGroup("BonusesData");

        BonusChancesData.minPlayersForSpawn = options.ValueInt("minPlayersForBonusesSpawn", BonusChancesData.minPlayersForSpawn);

        BonusChancesData.goldChance = options.ValueInt("goldChance", BonusChancesData.goldChance);
        BonusChancesData.goldChanceMin = options.ValueInt("goldChanceMin", BonusChancesData.goldChanceMin);
        BonusChancesData.silverChance = options.ValueInt("silverChance", BonusChancesData.silverChance);
        BonusChancesData.experienceChance = options.ValueInt("experienceChance", BonusChancesData.experienceChance);

        BonusAmountsData = new BonusAmounts(options.ValueObjectList("amountDividers"));

        BonusAmountsData.minRandomCoefficient = options.ValueFloat("silverRandCoeffMin", 1.0f);
        BonusAmountsData.maxRandomCoefficient = options.ValueFloat("silverRandCoeffMax", 1.0f);

        options.EndGroup();
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
        if (kitsDict != null)
        {
            var kitsDictPrefs = new JsonPrefs(kitsDict);
            foreach (KeyValuePair<string, object> kitTypePair in kitsDict)
            {
                //Чтобы не делать еще уровень вложенности, запихнул эту переменную сюда
                //Возможно надо запихнуть это в словарь Kits->Newbie->kitsAfterBattleShowingRate
                if (kitTypePair.Key == "kitsAfterBattleShowingRate")
                {
                    newbieKitsAfterBattleShowingRate = kitsDictPrefs.ValueInt("kitsAfterBattleShowingRate");
                    continue;
                }

                BankKits.Type kitType;
                if (!HelpTools.TryParseToEnum(kitTypePair.Key, out kitType))
                    continue;
                var typedKitsDict = kitsDictPrefs.ValueObjectDict(kitTypePair.Key);
                foreach (KeyValuePair<string, object> kitPair in typedKitsDict)
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
                        Dictionary<string, object> content = kitPrefs.ValueObjectDict("content");
                        Dictionary<BankKits.Content, int> parsedDic = new Dictionary<BankKits.Content, int>();
                        if (kitPrefs != null)
                        {
                            foreach (var pair in content)
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
        
        //if (options.Contains("DailyBonus"))
        //{
        //    var bonuses = options.ValueObjectList("DailyBonus");
        //    ProfileInfo.dailyBonusDaysCount = bonuses.Count;
        //    DailyBonus.dailyBonusesDict = new Dictionary<int, ProfileInfo.Price>();
        //    foreach (var o in bonuses)
        //    {
        //        var prefs = new JsonPrefs(o);
        //        DailyBonus.dailyBonusesDict.Add(prefs.ValueInt("day"), prefs.ValuePrice("price"));
        //    }
        //}
        #endregion
        #region Tanks, Modules, Decals and Patterns section
        if (!options.Contains("TanksData"))
        {
            Debug.LogError("There is no 'TanksData' section in options from server");
            return false;
        }

        vehiclesDataStorage = options.ValueObjectDict("TanksData");
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
        if (!options.Contains("Chat"))
        {
            Debug.LogError("There is no 'Chat' section in options from server");
            return false;
        }

        options.BeginGroup("Chat");

        chatMinAccessLevel = options.ValueInt("minAccessLevel", chatMinAccessLevel);
        chatMinComplainLevel = options.ValueInt("minComplainLevel", chatMinComplainLevel);
        chatIsComplaintsEnabled = options.ValueBool("isComplaintsEnabled", chatIsComplaintsEnabled);
        chatModeratorAvailableBanTime = options.ValueInt("moderatorAvailableBanTime", chatModeratorAvailableBanTime);

        options.EndGroup();
        #endregion
        #region ProfileInfo Section // Загрузка различных наград за действия игрока
        if (!options.Contains("ProfileInfo"))
        {
            Debug.LogError("There is no 'ProfileInfo' section in options from server");
            return false;
        }
        options.BeginGroup("ProfileInfo");

        options.EndGroup();
        #endregion
        #region Map Selection Section // Загрузка параметров выбора карт
        if (!options.Contains("Maps"))
        {
            Debug.LogError("There is no 'Maps' section in options from server");
            return false;
        }
        mapsList = options.ValueObjectList("Maps");
        #endregion
        #region Ads Section // Загрузка параметров рекламы от сетей TapJoy, Unity Ads и Chartboost
        options.BeginGroup("AdsServices");

        if (options.Contains("TapJoy"))
            isTapJoyEnabled = options.ValueBool("TapJoy/show", false);// (bool) options.ValueObjectDict("TapJoy")["show"];
        else
            Debug.Log("TF: There is no \"TapJoy\" section in option from server!");

        if (options.Contains("UnityAds"))
            ThirdPartyAdsManager.SetupService("UnityAds", options.ValueObjectDict("UnityAds"));
        else
            Debug.Log("TF: There is no \"UnityAds\" section in option from server!");

        if (options.Contains("ChartBoost"))
            ThirdPartyAdsManager.SetupService("ChartBoost", options.ValueObjectDict("ChartBoost"));
        else
            Debug.Log("TF: There is no \"ChartBoost\" section in option from server!");

        adsFree = !(options.Contains("UnityAds") || options.Contains("ChartBoost"));

        // TODO: Распарсить настройки для рекламы своих проектов.
        //XDevsAdsManager.Setup();

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
                gamesOffers.Add(XDevs.Offers.GameOffer.fromDictionary(g.Key, g.Value as Dictionary<string, object>));
            }

            options.EndGroup();
        }
        #endregion
        #region PhotonSettings Section Настройки параметров соединения с облаком Фотона
        if (options.Contains("PhotonSettings"))
        {
            options.BeginGroup("PhotonSettings");

            var sProtocol = options.ValueString("protocol");
            ExitGames.Client.Photon.ConnectionProtocol protocol;
            if (HelpTools.TryParseToEnum(sProtocol, out protocol))
            {
                PhotonNetwork.PhotonServerSettings.Protocol = protocol;
            }

            var sRegion = options.ValueString("region");
            CloudRegionCode region;
            if (HelpTools.TryParseToEnum(sRegion, out region))
            {
                PhotonNetwork.PhotonServerSettings.PreferredRegion = region;
            }

            options.EndGroup();
        }
        #endregion
        #region Awards Section
        if (options.Contains("Awards"))
        {
            StaticType.Awards.Instance<IAwards>().LoadFromServer(options.ValueObjectDict("Awards"));
        }

        if (options.Contains("VIPDiscounts"))
        {
            Debug.LogWarningFormat("Options Contains VIPDiscounts!");
        }
        #endregion
        //#region VIPSettings // Загрузка параметров VIP Settings
        //if (!options.Contains("VIPSettings"))
        //{
        //    Debug.LogError("There is no 'VIPSettings' section in options from server");
        //    return false;
        //}
        //Dictionary<string, object> itemsVIPProduts = options.ValueObjectDict("VIPSettings");
        //if (itemsVIPProduts.ContainsKey("items"))
        //{
        //    vipProducts = itemsVIPProduts["items"] as List<object>;
        //}
        //#endregion
        #region DonateCatalog // Загрузка параметров продукции
        if (!options.Contains("DonateCatalog"))
        {
            Debug.LogError("There is no 'DonateCatalog' section in options from server");
            return false;
        }
        products = options.ValueObjectList("DonateCatalog");
        #endregion

        Dictionary<string, object> slots = options.ValueObjectDict("TanksSlots");
        //, ref slots);
        Event(Message.ServerDataReceived, ServerDataType.SummaryData, slots, vehiclesDataStorage, options.ValueObjectList("DailyBonus"), connectionSocialReward);
        return true;
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
    public float GetCorrectedTime()
    {
        return Time.time + timeCorrection;
    }

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
        get
        {
            return DateTimeToUnixTimeStamp(CurrentDateTime.AddSeconds(31536000));
        }
    }

    public static DateTime CurrentDateTime
    {
        get
        {
            return UnixTimeStampToDateTime(CurrentTimeStamp);
        }
    }

    public static DateTime CorrectedDateTime
    {
        get
        {
            return UnixTimeStampToDateTime(CorrectedCurrentTimeStamp);
        }
    }


    private void GameTimerTick()
    {
        currentTimeStamp = timeStamp + (Time.realtimeSinceStartup - serverTimeReceiveSinceStart);
    }

    public static IEnumerator InitTime()
    {
        DateTime networkTime = ServerTime;
        networkTime = ServerTime;

        if (networkTime.Year < DateTime.UtcNow.Year)
        {
            timeGettingError = true;
            GameData.CriticalError("UI_MB_NTPConnectionError");
            yield break;
        }

        timeStamp = (networkTime.Subtract(new DateTime(1971, 1, 1, 0, 0, 0, DateTimeKind.Utc))).TotalSeconds;
        instance.currentTimeStamp = timeStamp;

        ProfileInfo.lastVisit = 0;
        ProfileInfo.lastProfileSaveTimestamp = 0;
        timeGettingError = false;
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

    void OnApplicationPause(bool paused)
    {
        if (paused)
        {
            lastPausedAt = Time.realtimeSinceStartup;
        }
        else
        {
            timeCorrection = timeCorrection + Time.realtimeSinceStartup - lastPausedAt;
        }
    }

    #endregion

    private void CheatDetected(EventId id, EventInfo info)
    {
#if UNITY_EDITOR
        Debug.LogError("Cheat detected! Quit NAH!");
#endif
        QuitGame();
    }

    // returns "en-US" / "ru-RU" / ...
    public static string GetLocale()
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

    public string GetBundleId()
    {
        if (string.IsNullOrEmpty(bundleId))
        {
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

    public string GetBundleVersion()
    {
        if (string.IsNullOrEmpty(bundleVersion))
        {
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
    /// Проверка приложения на его существование в состеме. Работает только для Андроида.
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
        int currGame = (int)GameData.CurrentGame;
        int reqGame = (int)requiredMode;
        return (currGame & reqGame) != 0;
    }

    public static void QuitGame()
    {
        Debug.Log("GameData.QuitGame()");
#if UNITY_WEBPLAYER || UNITY_WEBGL
        Application.ExternalEval("(function (){location.reload();})();");
#else
        Application.Quit();
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
            return null;
        }
    }
}