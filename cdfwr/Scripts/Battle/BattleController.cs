using System;
using System.Linq;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using CodeStage.AntiCheat.ObscuredTypes;
using Disconnect;
using Http;
using Matchmaking;

using XDevs.LiteralKeys;

#if !UNITY_WSA
using Rewired;
#endif

using Hashtable = ExitGames.Client.Photon.Hashtable;

public class BattleController : MonoBehaviour
{
    public enum EndBattleCause
    {
        Timeouted,
        Manually,
        Inactivity,
        ApplicationPaused,
        AlreadyInBattle,
        FinishedTutorial
    }

    public class AfterRespawnBonus
    {
        public BonusItem.BonusType type;
        public ObscuredInt price;

        public AfterRespawnBonus(BonusItem.BonusType _type, ObscuredInt _price)
        {
            type = _type;
            price = _price;
        }
    }

    public const int DEFAULT_TARGET_ID = -1;
    public const int OPPONENTS_NEEDED = 3;
    public const int TimeForRespawn = 3;
    public const bool DeathMatchType2 = true;
    public const bool TeamBattleType2 = true;

    public static Dictionary<int, ObscuredInt> battleInventory = new Dictionary<int, ObscuredInt>(3);
    public static Dictionary<int, VehicleController> allVehicles;
    public static Dictionary<int, TankData> vehicleData;
    public static Dictionary<int, string> weaponPrefabPaths;

    public static Dictionary<int, List<VehicleController>> playersByTeams = new Dictionary<int, List<VehicleController>>();//команда<соперники>
    public static Dictionary<int, List<int>> visibleEnemies = new Dictionary<int, List<int>>();

    public AudioClip bonusGetSound;
    public float outOfMapDistance = 2000;
    public StepSoundSetItem stepSoundItem;

    private const float MONO_MESSAGE_TARGETS_UPDATING_FREQUENCY = 1.0f;

    private static BattleController instance;
    private static bool playerInBattle;
    private static int enemyLayer;
    private static int myPlayerId;
    private static int parallelWorldLayer;
    private static int playerLayer;
    private static int terrainMask;
    private static double roomCreationTime = 0;
    private static float battleDuration;

    private static Queue<AfterRespawnBonus> afterRespawnBonuses;

    private readonly Dictionary<int, PlayerStat> gameStat = new Dictionary<int, PlayerStat>(GameData.maxPlayers);

    private LayerMask hitMask;
    private PhotonView photonView;
    private bool mayShowCriticalTime = true;
    private int lastOffender;
    private float timeInBattleUnity;
    private float timeInBattlePhoton;
    private float previousPhotonTime = -1;
    private double battleAccomplishedTime;
    private double myCreationTime = -1;
    private ObscuredInt currentProlongPrice;
    private VehicleController myVehicle;
    private bool roomWasFulled;
    private bool isBattleFinished = false;

    private List<VehicleController> oldChecked = new List<VehicleController>();
    private List<VehicleController> newChecked = new List<VehicleController>();
    private List<VehicleController> temp_;
    private float maxVisibleDistance = 500f;
    private RaycastHit hit_;
    private Vector3 pos;
    private Vector3 dir;
    private bool dead;


    public GameData.GameMode BattleMode { get; private set; }

    public bool IsTeamMode
    {
        get { return BattleMode == GameData.GameMode.Team; }
    }

    public static BattleController Instance
    {
        get { return instance; }
    }

    /// <summary>
    /// Показывает, закончена ли битва (с последнего кадра жизни битвы)
    /// </summary>
    public static bool BattleIsOver { get { return instance == null; } }

    public static bool IsBattleFinished { get { return Instance == null || Instance.isBattleFinished; } }

    public bool NormalDisconnect { get; private set; }

    private static bool battleAccomplished;
    public static bool BattleAccomplished
    {
        get { return battleAccomplished; }
        private set
        {
            battleAccomplished = value;
        }
    }

    public static bool PlayerInBattle
    {
        get { return playerInBattle; }
    }

    public static bool CheckPlayersCount()
    {
        return true;
        //return ProfileInfo.IsBattleTutorial || (PhotonNetwork.room != null && PhotonNetwork.room.playerCount >= GameData.minPlayersForBattleStart);
    }

    public static int LastOffender
    {
        get { return instance.lastOffender; }
    }

    public static int HitMask
    {
        get { return instance.hitMask.value; }
    }

    public static int TerrainLayerMask
    {
        get { return terrainMask; }
    }

    public static int ParallelWorldLayer
    {
        get { return parallelWorldLayer; }
    }

    public static int PlayerLayer
    {
        get { return playerLayer; }
    }

    public static int CommonVehicleMask { get; private set; }


    public static int EnemyLayer
    {
        get { return enemyLayer; }
    }

    public static int TimeRemaining
    {
        get { return (int)(battleDuration - instance.timeInBattlePhoton); }
    }

    public static int MyPlayerId
    {
        get { return instance != null ? myPlayerId : -1; }
    }

    public static float TimeInBattleUnity
    {
        get { return instance.timeInBattleUnity; }
    }

    public static float TimeInBattlePhoton
    {
        get { return instance.timeInBattlePhoton; }
    }

    public static double MyCreationTime
    {
        get { return instance.myCreationTime; }
    }

    public static double RoomTime
    {
        get
        {
            if (roomCreationTime < 0.01)
            {
                roomCreationTime = (double)PhotonNetwork.room.CustomProperties["ct"] / 1000;
            }

            return (PhotonNetwork.time - roomCreationTime) / 1000;
        }
    }

    public static AudioClip BonusGetSound
    {
        get { return instance != null ? instance.bonusGetSound : null; }
    }

    public static VehicleController MyVehicle
    {
        get { return instance != null ? instance.myVehicle : null; }
    }

    public static Dictionary<int, PlayerStat> GameStat
    {
        get { return instance != null ? instance.gameStat : null; }
    }

    public int CurrentProlongPrice
    {
        get { return currentProlongPrice; }
    }

    public float DeltaTimePhoton
    {
        get
        {
            float currentPhotonTime = (float)PhotonNetwork.time;
            if (previousPhotonTime <= 0 || Mathf.Abs(currentPhotonTime - previousPhotonTime) > GameData.reconnectTimeout)
            {
                previousPhotonTime = currentPhotonTime;
                return 0;
            }
            float result = previousPhotonTime < 0 ? 0 : currentPhotonTime - previousPhotonTime;
            previousPhotonTime = currentPhotonTime;
            return result;
        }
    }

    /* UNITY SECTION */
    private Coroutine changeVis; //todo: отрефакторить

    void Awake()
    {
        if (instance != null)
        {
            return;
        }

        instance = this;
        BattleMode = GameData.Mode;
        hitMask = MiscTools.GetLayerMask("Default", "Terrain", "Player", "Enemy", "Friend", "Bot1", "Bot2", "Bot3",
            "Bot4", "Bot5", "Bot6", "Bot7", "Bot8", "Bot9");
        CommonVehicleMask = LayerMask.GetMask("Player", "Enemy", "Friend") | BotDispatcher.BotsCommonMask;
        BattleConnectManager.AddPhotonMessageTarget(gameObject);
        battleDuration = ProfileInfo.IsBattleTutorial ? float.PositiveInfinity : GameData.BattleDuration;
        currentProlongPrice = GameManager.PROLONG_START_PRICE;
        afterRespawnBonuses = new Queue<AfterRespawnBonus>();
        myPlayerId = -1;
        myVehicle = null;
        playerInBattle = false;
        allVehicles = new Dictionary<int, VehicleController>(10);
        vehicleData = new Dictionary<int, TankData>(10);
        terrainMask = MiscTools.GetLayerMask(Layer.Key.Terrain);
        photonView = GetComponent<PhotonView>();
        parallelWorldLayer = LayerMask.NameToLayer(Layer.Items[Layer.Key.ParallelWorld]);
        playerLayer = LayerMask.NameToLayer(Layer.Items[Layer.Key.Player]);
        enemyLayer = LayerMask.NameToLayer(Layer.Items[Layer.Key.Enemy]);
        FillWeaponsDict();

        if (!photonView)
        {
            DT.LogError(gameObject, "There is no photon view on Battle Controller. Disabled.");
            enabled = false;
        }

        InitAnother(); //// Инициализация других классов
        Subscriptions();

        if (GameData.IsGoldRushEnabled)
        {
            GoldRush.ActivateForBattle();
        }
        else
        {
            GoldRush.Disactivate();
        }


    }

    private static void FillWeaponsDict()
    {
        weaponPrefabPaths = new Dictionary<int, string>();

        const string weaponsFolderPath = "CodeOfWar/Weapon/WeaponSoldier";
        var weaponKits = Resources.LoadAll<WeaponKit>(weaponsFolderPath);

        foreach (var weapon in weaponKits)
        {
            weaponPrefabPaths.Add(weapon.id, string.Format("{0}/{1}", weaponsFolderPath, weapon.name));
        }
    }

    private void InitAnother()
    {
        StatTable.OnStopTimer += AfterCountdown;
        VehicleEffect.Init();
        CodeStage.AntiCheat.Detectors.SpeedHackDetector.isRunning = true;
    }

    void Start()
    {
        AddBattleTutorialComponent();
    }

    void OnDestroy()
    {
        instance = null;
        vehicleData = null;
        allVehicles = null;
        gameStat.Clear();
        MusicBox.Stop();
        playerInBattle = false;
        myPlayerId = -1;
        battleInventory.Clear();
        myVehicle = null;
        Unsubscriptions();
        GoldRush.Disactivate();
        // ShellPoolManager.ClearAllPools();
        BattleConnectManager.ClearPhotonMessageTargets();
        weaponPrefabPaths = null;
        StopCoroutine(changeVis);
    }

    void OnApplicationPause(bool paused)
    {
        if (paused)
        {
            EndBattle(EndBattleCause.ApplicationPaused);
        }
    }

    void Update()
    {

#if UNITY_EDITOR
        #region Функционал для отладки

        if (Input.GetKeyDown(KeyCode.Alpha8))
        {
            ShowRoomCountryInfo();
        }

        if (Input.GetKeyDown(KeyCode.Alpha9))
        {
            ShowRoomInfo();
        }

        if (Input.GetKeyDown(KeyCode.K))
        {
            Dispatcher.Send(EventId.TankKilled, new EventInfo_II(myPlayerId, myPlayerId));
        }

        if (Input.GetKeyDown(KeyCode.I))
        {
            int damage = myVehicle.data.attack;

            Dispatcher.Send(
                EventId.TankTakesDamage,
                 new EventInfo_U(
                            myPlayerId,
                            damage,
                            myPlayerId,
                            (int)StaticContainer.DefaultShellType,
                            MyVehicle.transform.position), Dispatcher.EventTargetType.ToAll);

            Dispatcher.Send(EventId.TankHealthChanged, new EventInfo_II(MyVehicle.data.playerId, MyVehicle.Armor));
        }
        if (Input.GetKeyDown(KeyCode.Alpha0))
        {
            Dispatcher.Send(EventId.MyTankShots, new EventInfo_I((int)StaticContainer.DefaultShellType));
            Dispatcher.Send(
                  id: EventId.TankTakesDamage,
                  info: new EventInfo_U(
                              myPlayerId,
                              9999999,
                              myPlayerId,
                              (int)StaticContainer.DefaultShellType,
                              MyVehicle.transform.position));

            Dispatcher.Send(EventId.TankHealthChanged, new EventInfo_II(MyVehicle.data.playerId, MyVehicle.Armor));
            Dispatcher.Send(EventId.PlayerFled, new EventInfo_SimpleEvent());
            Manager.BattleServer.EndBattle(BattleConnectManager.NORMAL_DISCONNECT_CAUSE);
            BattleController.ExitToHangar(true);
        }


        /*        if (Input.GetKeyDown(KeyCode.O))
                {
                    int damage = myVehicle.data.rocketAttack;

                    Dispatcher.Send(
                        id: EventId.TankTakesDamage,
                        info: new EventInfo_U(
                                    myPlayerId,
                                    damage,
                                    myPlayerId,
                                    (int)ShellType.Missile_SACLOS,
                                    MyVehicle.transform.position));

                    Dispatcher.Send(EventId.TankHealthChanged, new EventInfo_II(MyVehicle.data.playerId, MyVehicle.Armor));
                }*/

        if (Input.GetKeyDown(KeyCode.Keypad0) && MyVehicle)
        {
            MyVehicle.CheatActivate();
        }
        else if (Input.GetKeyDown(KeyCode.Keypad1) && MyVehicle)
        {
            MyVehicle.ActivateSlowpoke();
        }
        #endregion
#endif
        if (XDevs.Input.GetButtonDown("OpenBattleSettings"))
        {
            if (!StatTable.OnScreen && myVehicle != null)
            {
                StatTable.Show(gameStat, myPlayerId, StatTable.TableState.Quit);
            }
            else if (StatTable.OnScreen && StatTable.instance.quitPart.activeInHierarchy)
            {
                StatTable.Hide();
            }
        }
        if (XDevs.Input.GetButtonDown("Back"))
        {
            if (BattleSettings.OnScreen)
            {
                BattleSettings.SetActive(false);
            }
            else if (BattleChatCommands.OnScreen)
            {
                BattleChatCommands.Instance.Hide();
            }
            else if (!StatTable.OnScreen && myVehicle != null)
            {
#if UNITY_WEBGL || UNITY_STANDALONE
                BattleSettings.SetActive(true);
#else
                StatTable.Show(gameStat, myPlayerId, StatTable.TableState.Quit);
#endif
            }
        }
        if (BattleConnectManager.Instance.InBattle && roomWasFulled)
        {
            timeInBattleUnity += Time.deltaTime; // Считаем только для выявления читаков.
            timeInBattlePhoton += DeltaTimePhoton;
        }
        else
        {
            previousPhotonTime = (float)PhotonNetwork.time; // Борьба с неправильным вычислением DeltaTimePhoton
        }

        if (!ProfileInfo.IsBattleTutorial && timeInBattlePhoton >= battleDuration && playerInBattle)
        {
            enabled = false;
            EndBattle(EndBattleCause.Timeouted);
            return;
        }

        float timeRemain = battleDuration - timeInBattlePhoton;

        // Не давать нажимать после 3 сек. до конца боя, чтобы дать время серверу на ответ
        if (myVehicle && timeRemain < 3 && TopPanelValues.CriticalTime)
        {
            TopPanelValues.ShowCriticalTime(false);
        }
        else if (myVehicle &&
                 mayShowCriticalTime &&
                 GameData.IsProlongTimeEnabled &&
                 timeRemain <= GameData.ProlongTimeShow &&
                 StatTable.State != StatTable.TableState.ExitWaiting)
        {
            if (ProfileInfo.CanBuy(new ProfileInfo.Price(currentProlongPrice, ProfileInfo.PriceCurrency.Gold)) &&
                !MyVehicle.DoubleExperience)
            {
                TopPanelValues.ShowCriticalTime(true, currentProlongPrice);
            }

            mayShowCriticalTime = false;
        }

        if (playerInBattle)
        {
            CheckMyEffects();
        }
    }

    public static void ProvokeServer()
    {
        Dispatcher.Send(EventId.MyTankShots, new EventInfo_I((int)StaticContainer.DefaultShellType));
        Dispatcher.Send(
              id: EventId.TankTakesDamage,
              info: new EventInfo_U(
                          myPlayerId,
                          9999999,
                          myPlayerId,
                          (int)StaticContainer.DefaultShellType,
                          MyVehicle.transform.position));
        Dispatcher.Send(EventId.TankHealthChanged, new EventInfo_II(MyVehicle.data.playerId, MyVehicle.Armor));
        Dispatcher.Send(EventId.PlayerFled, new EventInfo_SimpleEvent());
        Manager.BattleServer.EndBattle(BattleConnectManager.NORMAL_DISCONNECT_CAUSE);
        BattleController.ExitToHangar(true);
    }
    /* PHOTON SECTION */

    void OnPhotonCustomRoomPropertiesChanged(Hashtable changedProperties)
    {
        Dictionary<int, Hashtable> botProperties = null;
        foreach (var property in changedProperties)
        {
            string key = property.Key as string;
            if (key == null || property.Value == null)
            {
                continue;
            }

            switch (key)
            {
                case "stake":
                    GoldRush.TotalStake = (int)property.Value;
                    continue;
                case "goldLeader":
                    GoldRush.Leader = (int)property.Value;
                    if (StatTable.OnScreen)
                        StatTable.Refresh(gameStat, myPlayerId);
                    continue;
                case "awardPermission":
                    GoldRush.AwardPermission = (bool)property.Value;
                    continue;
                case "lm": // Last master's id
                    BattleConnectManager.Instance.LastMasterId = (int)property.Value;
                    continue;
            }

            if (key.StartsWith("bt")) // "bt" в начале свойств для ботов
            {
                if (botProperties == null)
                {
                    botProperties = new Dictionary<int, Hashtable>(4);
                }
                ConvertBotsPropertyToPlayers(key, property.Value, botProperties);
            }
        }

        if (botProperties == null)
        {
            return;
        }

        foreach (var botProperty in botProperties)
        {
            OnPlayerPropertiesChanged(botProperty.Key, botProperty.Value);
        }

        Dispatcher.Send(EventId.PhotonRoomCustomPropertiesChanged, new EventInfo_SimpleEvent());
    }

    /* PUBLIC SECTION */

    public static void SetMainVehicle(VehicleController vehicleController)
    {
        if (instance.myVehicle == null)
        {
            instance.myVehicle = vehicleController;
        }

        myPlayerId = vehicleController.data.playerId;

        playerInBattle = true;

        tk2dUIManager.Instance.UseMultiTouch = true;
        TopPanelValues.NickName = MyVehicle.data.playerName;
        MusicBox.Play();

        TopPanelValues.SetEarnedSilver(ProfileInfo.Silver);
        TopPanelValues.SetEarnedGold(ProfileInfo.Gold);

        roomCreationTime = (double)PhotonNetwork.room.CustomProperties["ct"];
    }

    public static void ExitToHangar(bool normalExit)
    {
        instance.timeInBattlePhoton = 0;
        BattleStatisticsManager.SetOtherBattleStats();

        Dispatcher.Send(EventId.OnExitToHangar, new EventInfo_SimpleEvent());

        instance.NormalDisconnect = normalExit;

        if (PhotonNetwork.isMasterClient && normalExit)
        {
            Hashtable properties = new Hashtable { { "lm", myPlayerId } }; // LastMasterId
            PhotonNetwork.room.SetCustomProperties(properties);
        }

        if (PhotonNetwork.connected)
        {
            BattleConnectManager.Instance.ForcedDisconnect(ExitToHangarAfterDisconnect);
        }
        else
        {
            ExitToHangarAfterDisconnect();
        }
    }

    public static void EndBattle(EndBattleCause cause)
    {
        if (instance == null)
        {
            return;
        }

        if (instance.myVehicle)
        {
            instance.myVehicle.IsAvailable = false;
        }

        playerInBattle = false;
        instance.enabled = false;
        instance.NormalDisconnect = true;

        BattleStatisticsManager.BattleStats["PhotonDisconnect"] = 0;

        TopPanelValues.ShowCriticalTime(false);

        if (cause == EndBattleCause.Timeouted)
        {
            BattleAccomplished = true;
            Dispatcher.Send(EventId.TankOutOfTime, new EventInfo_I(myPlayerId), Dispatcher.EventTargetType.ToAll);
        }
        else if (cause == EndBattleCause.Inactivity)
        {
            StatTable.MyVehicleRank = 101; // за неактивность
            Dispatcher.Send(EventId.PlayerKickout, new EventInfo_I(myPlayerId), Dispatcher.EventTargetType.ToAll);
            Dispatcher.Send(EventId.HideEnemy, new EventInfo_I(myPlayerId));
        }

        Instance.isBattleFinished = true;
        Dispatcher.Send(EventId.BattleEnd, new EventInfo_I((int)cause));
        BattleStatisticsManager.SetOtherBattleStats();
        if (ProfileInfo.accomplishedTutorials[Tutorials.goToBattle])
        {
            ProfileInfo.doubleExpVehicles.Remove(ProfileInfo.currentVehicle);
        }
        BattleConnectManager.Instance.ForcedDisconnect(instance.DisconnectAndShow_Continue);
    }

    public static void RememberBonus(BonusItem.BonusType bonusType, int price)
    {
        AfterRespawnBonus bonus = new AfterRespawnBonus(bonusType, price);
        afterRespawnBonuses.Enqueue(bonus);
    }

    public void FailureQuit(MessageBox.Answer _answer)
    {
        BattleStatisticsManager.SetOtherBattleStats();
        Manager.BattleServer.EndBattle(BattleConnectManager.FAILURE_DISCONNECT_CAUSE);
        ExitToHangar(false);
    }

    public void ShowStatTable(tk2dUIItem item)
    {
        if (StatTable.OnScreen && (StatTable.State == StatTable.TableState.BattleEnd ||
                                   StatTable.State == StatTable.TableState.ExitWaiting ||
                                   StatTable.State == StatTable.TableState.AfterDeath))
        {
            return;
        }

        if (StatTable.OnScreen)
        {
            StatTable.Hide();
        }
        else
        {
            StatTable.Show(gameStat, myPlayerId, StatTable.TableState.Quit);
        }
    }

    public void OnPhotonPlayerPropertiesChanged(object[] playerAndChangedProperties)
    {
        PhotonPlayer player = (PhotonPlayer)playerAndChangedProperties[0];
        Hashtable properties = (Hashtable)playerAndChangedProperties[1];

        OnPlayerPropertiesChanged(player.ID, properties);
    }

    public void OnPlayerPropertiesChanged(int playerId, Hashtable properties)
    {
        VehicleController vehicleController;
        if (!allVehicles.TryGetValue(playerId, out vehicleController))
        {
            return;
        }

        foreach (var property in properties)
        {
            if (property.Value == null) //Это удаление свойства комнаты (бред, но приходит иногда)
            {
                continue;
            }

            switch (property.Key as string)
            {
                case "hl":
                    int val = (int)property.Value;
                    vehicleController.Armor = val;
                    Dispatcher.Send(EventId.TankHealthChanged, new EventInfo_II(vehicleController.data.playerId, val));
                    break;
                case "mar":
                    vehicleController.MaxArmor = (int)property.Value;
                    break;
                case "at":
                    vehicleController.data.attack = (int)property.Value;
                    break;
                case "ra":
                    vehicleController.data.rocketAttack = (int)property.Value;
                    break;
                case "sp":
                    vehicleController.Speed = (float)property.Value;
                    break;
                case "rf":
                    vehicleController.ROF = (float)property.Value;
                    break;
                case "ir":
                    vehicleController.IRCMROF = (float)property.Value;
                    break;
                case "ex":
                    if (!vehicleController.IsMine)
                    {
                        vehicleController.SyncExistance((bool)property.Value);
                    }
                    break;
                case "sc":
                    PlayerStat stat = vehicleController.Statistics;
                    int newScore = (int)property.Value;
                    if (stat.score >= 0)
                    {
                        ScoreCounter.RecalcTeamScore(stat.teamId, newScore - stat.score);
                    }
                    stat.score = newScore;
                    if (StatTable.OnScreen)
                    {
                        StatTable.Refresh(gameStat, myPlayerId);
                    }
                    break;
                case "dt":
                    vehicleController.Statistics.deaths = (int)property.Value;
                    if (StatTable.OnScreen)
                    {
                        StatTable.Refresh(gameStat, myPlayerId);
                    }
                    break;
                case "kl":
                    vehicleController.Statistics.kills = (int)property.Value;
                    if (StatTable.OnScreen)
                    {
                        StatTable.Refresh(gameStat, myPlayerId);
                    }
                    break;
                case "st":
                    Settings.ChangePlayerSettings((string)property.Value, playerId);
                    if (StatTable.OnScreen)
                    {
                        StatTable.Refresh(gameStat, myPlayerId);
                    }
                    break;
                case "rg": //Regeneration
                    if (vehicleController.IsMine)
                        vehicleController.Regeneration = (int)property.Value;
                    break;
                case "sh": //Shield
                    vehicleController.Shield = (int)property.Value;
                    break;
                case "tdr": //Taken damage ratio
                    vehicleController.TakenDamageRatio = (float)property.Value;
                    break;
            }
        }
    }

    public void ProlongGameForFree(float addTime)
    {
        if (BattleConnectManager.Instance.FirstConnect)
        {
            timeInBattleUnity = Mathf.Clamp(timeInBattleUnity - addTime, 0, 10000);
            timeInBattlePhoton = Mathf.Clamp(timeInBattlePhoton - addTime, 0, 10000);
        }

        TopPanelValues.ShowCriticalTime(false);

        mayShowCriticalTime = ProfileInfo.CanBuy(new ProfileInfo.Price(currentProlongPrice, ProfileInfo.PriceCurrency.Gold));

        Dispatcher.Send(EventId.GameProlonged, new EventInfo_SimpleEvent());
    }

    public void ProlongGameForMoney()
    {
        ProfileInfo.Price price = new ProfileInfo.Price(currentProlongPrice, ProfileInfo.PriceCurrency.Gold);

        //ProfileInfo.WriteOffBalance(price);

        currentProlongPrice = (int)(currentProlongPrice * GameManager.PROLONG_PRICE_INCREASE_RATIO);

        timeInBattleUnity -= GameData.ProlongTimeAddition;
        timeInBattlePhoton -= GameData.ProlongTimeAddition;

        TopPanelValues.ShowCriticalTime(false);

        mayShowCriticalTime = ProfileInfo.CanBuy(new ProfileInfo.Price(currentProlongPrice, ProfileInfo.PriceCurrency.Gold));

        //Manager.ReportStats(
        //    location:   "battle",
        //    action:     "prolongGame",
        //    query:      new Dictionary<string, string>
        //    {
        //        { "duration", GameData.ProlongTimeAddition.ToString() },
        //        { "price", price.value.ToString() },
        //        { "currency", price.currency.ToString() }
        //    });

        GoldRush.PlayerStake(price.value / 2);

        Dispatcher.Send(EventId.GameProlonged, new EventInfo_SimpleEvent());
    }

    public void ForgetPlayerData(int playerId)
    {
        allVehicles.Remove(playerId);
        vehicleData.Remove(playerId);
        gameStat.Remove(playerId);
    }

    /* PRIVATE SECTION */

    private static void AddBattleTutorialComponent()
    {
        if (ProfileInfo.IsBattleTutorial)
        {
            var tutorialsController = new GameObject().AddComponent<TutorialsController>();
            var battleTutorialPath = string.Format("{0}/{1}", GameManager.CurrentResourcesFolder, "GuiPrefabs/Battle/Tutorials/BattleTutorial");
            var battleTutorial = Instantiate(Resources.Load<BattleTutorial>(battleTutorialPath));

            battleTutorial.transform.SetParent(BattleGUI.Instance.transform, false);

            tutorialsController.name = "Tutorials controller";
            battleTutorial.name = "Battle tutorial";
        }
    }

    private void Subscriptions()
    {
        Dispatcher.Subscribe(EventId.BeforeReconnecting, BeforeReconnecting);
        Dispatcher.Subscribe(EventId.TroubleDisconnect, OnTroubleDisconnect);
        Dispatcher.Subscribe(EventId.TankJoinedBattle, OnTankConnected);
        Dispatcher.Subscribe(EventId.TankLeftTheGame, OnTankLeftTheGame, 2);
        Dispatcher.Subscribe(EventId.ShellEffect, OnShellEffect);
        Dispatcher.Subscribe(EventId.TankKilled, OnVehicleKilled, 1);
        Dispatcher.Subscribe(EventId.TankRespawned, OnVehicleRespawned, 1);
        Dispatcher.Subscribe(EventId.TankEffectRequest, OnVehicleEffectRequest);
        Dispatcher.Subscribe(EventId.TankEffectApply, OnVehicleEffectChanges);
        Dispatcher.Subscribe(EventId.TankEffectCancelled, OnVehicleEffectChanges);
        Dispatcher.Subscribe(EventId.MainTankAppeared, OnMainTankAppeared);
        Dispatcher.Subscribe(EventId.ItemTaken, OnItemTaken, 2);
        Dispatcher.Subscribe(EventId.GoldAcquired, OnGoldAcquired);
        Dispatcher.Subscribe(EventId.MyTankRespawned, MyTankRespawned);
    }

    private void Unsubscriptions()
    {
        Dispatcher.Unsubscribe(EventId.BeforeReconnecting, BeforeReconnecting);
        Dispatcher.Unsubscribe(EventId.TroubleDisconnect, OnTroubleDisconnect);
        Dispatcher.Unsubscribe(EventId.TankJoinedBattle, OnTankConnected);
        Dispatcher.Unsubscribe(EventId.TankLeftTheGame, OnTankLeftTheGame);
        Dispatcher.Unsubscribe(EventId.TankEffectRequest, OnVehicleEffectRequest);
        Dispatcher.Unsubscribe(EventId.TankEffectApply, OnVehicleEffectChanges);
        Dispatcher.Unsubscribe(EventId.TankEffectCancelled, OnVehicleEffectChanges);
        Dispatcher.Unsubscribe(EventId.TankKilled, OnVehicleKilled);
        Dispatcher.Unsubscribe(EventId.TankRespawned, OnVehicleRespawned);
        Dispatcher.Unsubscribe(EventId.MainTankAppeared, OnMainTankAppeared);
        Dispatcher.Unsubscribe(EventId.ItemTaken, OnItemTaken);
        Dispatcher.Unsubscribe(EventId.ShellEffect, OnShellEffect);
        Dispatcher.Unsubscribe(EventId.GoldAcquired, OnGoldAcquired);
        Dispatcher.Unsubscribe(EventId.MyTankRespawned, MyTankRespawned);
    }



    private void BeforeReconnecting(EventId id, EventInfo ei)
    {
        allVehicles.Clear();
        vehicleData.Clear();
        gameStat.Clear();
        StopAllCoroutines();
        CancelInvoke();
        playerInBattle = false;
        myVehicle = null;
    }

    private void OnTroubleDisconnect(EventId id, EventInfo ei)
    {
        playerInBattle = false;
        MessageBox.Show(MessageBox.Type.Info, Localizer.GetText("PhotonDisconnect"), FailureQuit);
    }

    private void OnTankConnected(EventId id, EventInfo ei)
    {
        if (StatTable.OnScreen)
        {
            StatTable.Refresh(gameStat, myPlayerId);
        }

        if (roomWasFulled)
        {
            return;
        }

        if (CheckPlayersCount() && MyVehicle != null)
        {
            roomWasFulled = true;
            Manager.BattleServer.StartTimer((int)(PhotonNetwork.time - GameManager.MyJoinRoomTime));
            BattleGUI.SetStatusText(null);
        }
    }

    private void OnTankLeftTheGame(EventId id, EventInfo ei)
    {
        if (!PhotonNetwork.connected)
        {
            return;
        }

        EventInfo_I info = ei as EventInfo_I;
        int playerId = info.int1;

        VehicleController veh;
        if (!allVehicles.TryGetValue(playerId, out veh))
        {
            return;
        }

        ForgetPlayerData(playerId);
        if (veh == null)
        {
            return;
        }

        veh.Explode();
        TankIndicators.RemoveIndicator(playerId);
        if (StatTable.OnScreen)
        {
            StatTable.Refresh(gameStat, myPlayerId);
        }
    }

    private void VehicleOutOfTime(EventId id, EventInfo info)
    {
        EventInfo_I eventInfo = (EventInfo_I)info;

        int playerId = eventInfo.int1;

        VehicleController vehicleController = allVehicles[playerId];

        vehicleController.IsAvailable = false;

        TankIndicators.RemoveIndicator(vehicleController);
    }

    private void OnShellEffect(EventId id, EventInfo ei)
    {
        EventInfo_IE info = (EventInfo_IE)ei;

        if (info.int1 != myPlayerId)
        {
            return;
        }

        myVehicle.ApplyEffect(info.effect);
    }

    private void OnGoldAcquired(EventId id, EventInfo info)
    {
        if (!TopPanelValues.CriticalTime)
        {
            mayShowCriticalTime = ProfileInfo.CanBuy(new ProfileInfo.Price(currentProlongPrice, ProfileInfo.PriceCurrency.Gold));
        }
    }

    private void OnVehicleKilled(EventId id, EventInfo info)
    {
        EventInfo_II eventInfo = (EventInfo_II)info;

        var victimId = eventInfo.int1;
        var attackerId = eventInfo.int2;

        OnVehicleKilled(victimId, attackerId);
    }

    private void OnVehicleEffectChanges(EventId eventId, EventInfo eventInfo)
    {
        if (eventId == EventId.TankEffectApply)
        {
            EventInfo_IE info = (EventInfo_IE)eventInfo;
            int playerId = info.int1;
            VehicleController veh;
            if (!allVehicles.TryGetValue(playerId, out veh))
            {
                return;
            }

            if (veh.FixateEffect(info.effect) && playerId == myPlayerId)
            {
                IEPanel.Instance.AddCell(info.effect.UI_Id, info.effect.IconName);
            }
        }
        else if (eventId == EventId.TankEffectCancelled)
        {
            EventInfo_II info = (EventInfo_II)eventInfo;

            int playerId = info.int1;
            int effectId = info.int2;

            VehicleController veh;
            if (!allVehicles.TryGetValue(playerId, out veh))
            {
                return;
            }

            if (playerId == myPlayerId)
            {
                IEPanel.Instance.RemoveCell(myVehicle.Effects[effectId].UI_Id);
            }
            veh.TakeEffectAway(effectId);
        }
    }

    private void OnVehicleEffectRequest(EventId eventId, EventInfo ei)
    {
        EventInfo_IE info = (EventInfo_IE)ei;
        VehicleEffect effect = info.effect;
        VehicleController vehicle;
        if (!allVehicles.TryGetValue(info.int1, out vehicle) || !vehicle.IsMine)
        {
            return;
        }

        effect.StartTime = PhotonNetwork.time;
        effect.SetId(VehicleEffect.GetNewId());
        Dispatcher.Send(EventId.TankEffectApply, new EventInfo_IE(info.int1, effect), Dispatcher.EventTargetType.ToAll);
    }

    private void OnVehicleRespawned(EventId id, EventInfo info)
    {
        EventInfo_I eventInfo = (EventInfo_I)info;

        int playerId = eventInfo.int1;

        if (playerId == myPlayerId)
        {
            StatTable.Hide();

            ApplyAfterRespawnBonus();
        }
    }

    private void OnItemTaken(EventId id, EventInfo ei)
    {
        EventInfo_III info = (EventInfo_III)ei;
        allVehicles[info.int3].bonusUse.TakeBonus((BonusItem.BonusType)info.int1, info.int2);
    }

    private void OnMainTankAppeared(EventId id, EventInfo ei)
    {
        myCreationTime = PhotonNetwork.time;
        if (BattleConnectManager.Instance.FirstConnect)
        {
            battleAccomplishedTime = myCreationTime + battleDuration;
        }
        BattleAccomplished = false;
        ProfileInfo.WasInBattle = true;

        if (!BattleConnectManager.Instance.FirstConnect)
        {
            return;
        }

        if (CheckPlayersCount())
        {
            roomWasFulled = true;
            BattleGUI.SetStatusText(null);
        }
        else
        {
            BattleGUI.SetStatusText(Localizer.GetText("WaitingForPlayers"));
        }
        SubtractHangarConsumables();
        changeVis = StartCoroutine(ChangeVisability());
    }

    private void OnVehicleKilled(int victimId, int attackerId)
    {
        VehicleController victim, attacker;

        if (!allVehicles.TryGetValue(victimId, out victim) || !victim.IsAvailable || !allVehicles.TryGetValue(attackerId, out attacker))
        {
            return;
        }

        victim.IsAiming = false;
        victim.Explode();
        if (victim.IsMine)
        {
            OnMySideVictim(victim, attacker);
        }
        if (attacker.IsMine)
        {
            OnMySideAttacker(victim, attacker);
        }
    }

    private void SubtractHangarConsumables()
    {
        if (ProfileInfo.accomplishedTutorials[Tutorials.goToBattle])
        {
            ProfileInfo.doubleExpVehicles.Remove(ProfileInfo.currentVehicle);
        }
        ProfileInfo.vehicleUpgrades[ProfileInfo.currentVehicle].battlesCount++;
    }

    private void DisconnectAndShow_Continue()
    {
        #region Пока не найдена причина появления дублей в статах, жестко вычищаем их
        List<PlayerStat> stats = BattleStatisticsManager.Instance.AllVehiclesStatsSorted;
        if (stats != null)
        {
            HashSet<PlayerStat> unnecessaryStats = new HashSet<PlayerStat>();
            for (int i = 0; i < stats.Count; i++)
            {
                PlayerStat stat = stats[i];
                for (int i1 = 0; i1 < stats.Count; i1++)
                {
                    if (stats[i1].playerId == stat.playerId && i1 != i)
                    {
                        unnecessaryStats.Add(stat);
                    }
                }
            }
            foreach (var stat in unnecessaryStats)
            {
                stats.Remove(stat);
            }
        }
        #endregion

        StatTable.Show(
            statistics: BattleStatisticsManager.Instance.AllVehiclesStatsSorted != null
                ? BattleStatisticsManager.Instance.AllVehiclesStatsSorted.ToDictionary(x => x.playerId, x => x)
                : gameStat,
            ownId: myPlayerId,
            _state: StatTable.TableState.BattleEnd);

        Manager.BattleServer.EndBattle(BattleConnectManager.NORMAL_DISCONNECT_CAUSE);


        if (BattleCamera.Instance)
        {
            BattleCamera.Instance.ReturnToStart();
        }

        instance.enabled = false;
    }

    private void ApplyAfterRespawnBonus()
    {
        VehicleEffect effect = null;

        if (afterRespawnBonuses.Count == 0)
            return;

        foreach (AfterRespawnBonus bonus in afterRespawnBonuses)
        {
            switch (bonus.type)
            {
                case BonusItem.BonusType.Attack:
                    effect = new VehicleEffect(-1, VehicleEffect.ParameterType.Attack, VehicleEffect.ModifierType.Product, 1.5f, 30,
                        PhotonNetwork.time, BonusItem.BonusType.Attack, IECell.IEIcon.Bonus_Attack, -1);
                    break;
                case BonusItem.BonusType.Reload:
                    effect = new VehicleEffect(-1, VehicleEffect.ParameterType.RoF, VehicleEffect.ModifierType.Product, 1.2f, 30,
                        PhotonNetwork.time, BonusItem.BonusType.Reload, IECell.IEIcon.Bonus_ROF, -1);
                    break;
            }

            myVehicle.ApplyEffect(effect);
        }

        afterRespawnBonuses.Clear();
    }

    public void AfterCountdown()
    {
        if (BattleMode == GameData.GameMode.Team && TeamBattleType2)
        {
            dead = true;
            InvokeRepeating("AfterCountInTeam", 0, 1);
        }
        else if (BattleMode == GameData.GameMode.Deathmatch && DeathMatchType2)
        {
            dead = true;
            InvokeRepeating("AfterCountInDeathmatch", 0, 1);
        }
        else
        {
            myVehicle.MakeRespawn(false, true, false);
        }
    }


    private void AfterCountInTeam()
    {
        if (!dead)
        {
            return;
        }
        List<VehicleController> alies = new List<VehicleController>();
        List<VehicleController> enemies = new List<VehicleController>();
        alies.Clear();
        enemies.Clear();
        foreach (var soldier in allVehicles)
        {
            if (soldier.Value.TeamId == myVehicle.TeamId)
            {
                alies.Add(soldier.Value);
            }
        }
        foreach (var soldier in allVehicles)
        {
            if (soldier.Value.TeamId != myVehicle.TeamId)
            {
                enemies.Add(soldier.Value);
            }
        }
        bool allEnemyDead = true;
        bool allAllyDead = true;
        foreach (var ally in alies)
        {
            if (ally.data.armor > 0)
            {
                allAllyDead = false;
            }
        }
        foreach (var enemy in enemies)
        {
            if (enemy.data.armor > 0)
            {
                allEnemyDead = false;
            }
        }
        if (!allAllyDead && !allEnemyDead)
        {
            return;
        }
        Dispatcher.Send(EventId.MassRespawn, new EventInfo_SimpleEvent(), Dispatcher.EventTargetType.ToAll);
        //if (!PhotonNetwork.isMasterClient)
        //{
        //    return;
        //}

        if (allEnemyDead)
        {
            Dispatcher.Send(EventId.TeamWin, new EventInfo_I(myVehicle.TeamId), Dispatcher.EventTargetType.ToAll);
        }
        else
        {
            Dispatcher.Send(EventId.TeamWin, new EventInfo_I(enemies[0].TeamId), Dispatcher.EventTargetType.ToAll);
        }
        dead = false;
    }

    private void AfterCountInDeathmatch()
    {
        if (!dead)
        {
            return;
        }
        List<VehicleController> enemies = new List<VehicleController>();
        enemies.Clear();
        foreach (var soldier in allVehicles.Values)
        {
            enemies.Add(soldier);
        }

        int aliveCount = 0;
        foreach (var enemy in enemies)
        {
            if (enemy.data.armor > 0)
            {
                aliveCount++;
            }
        }
        if (aliveCount > 1)
        {
            return;
        }
        Dispatcher.Send(EventId.MassRespawn, new EventInfo_SimpleEvent(), Dispatcher.EventTargetType.ToAll);
        dead = false;
    }

    private void MyTankRespawned(EventId _id, EventInfo _info)
    {
        dead = false;
    }
    private void CheckMyEffects()
    {
        foreach (VehicleEffect effect in myVehicle.Effects.Values)
        {
            if (string.IsNullOrEmpty(effect.IconName))
            {
                continue;
            }

            IEPanel.Instance.GetCell(effect.UI_Id).Timer = Mathf.CeilToInt(effect.Remain);
        }
    }

    private void CheckIfBattleAccomplished()
    {
        BattleAccomplished = BattleController.MyVehicle != null && PhotonNetwork.time >= battleAccomplishedTime - 0.1; // -0.1 - защита от всяческих погрешностей
    }

    /// <summary>
    /// Преобразует свойство бота из room custom property в player custom property (как у обычных игроков)
    /// </summary>
    /// <param name="key">Имя ключа из room.customProperties</param>
    /// <param name="value">значение свойства</param>
    /// <param name="botProperties">Hashtable для сбора свойств</param>
    private void ConvertBotsPropertyToPlayers(string key, object value, Dictionary<int, Hashtable> collection)
    {
        int botId;
        int firstDigitIndex = MiscTools.FindFirstInString(key, char.IsDigit);
        if (firstDigitIndex == -1 || !int.TryParse(key.Substring(firstDigitIndex), out botId))
        {
            Debug.LogError("Incorrect bot playedId");
            return;
        }

        string playerProperty = key.Substring(2, firstDigitIndex - 2); // Название соответствующего свойства у обычного игрока (не бота)
        Hashtable properties;
        if (!collection.TryGetValue(botId, out properties))
        {
            properties = new Hashtable(4);
            collection.Add(botId, properties);
        }
        properties.Add(playerProperty, value);
    }

    private void OnMySideVictim(VehicleController victim, VehicleController attacker)
    {
        if (CheckPlayersCount())
        {
            ScoreCounter.DeathInto(victim);
            if (!victim.IsBot)
            {
                lastOffender = attacker.data.playerId;
                StatTable.Show(gameStat, myPlayerId, StatTable.TableState.AfterDeath);
                Http.Manager.BattleServer.Death(attacker.data.profileId);
            }
            else
            {
                if (BattleController.Instance.BattleMode == GameData.GameMode.Team && TeamBattleType2 || BattleController.Instance.BattleMode == GameData.GameMode.Deathmatch && DeathMatchType2)
                {
                    BattleController.Instance.AfterCountdown();
                }
            }
        }
    }

    private void OnMySideAttacker(VehicleController victim, VehicleController attacker)
    {
        if (victim == attacker || StaticContainer.AreFriends(victim, attacker))
        {
            return;
        }

        int expAmount = BonusDispatcher.ExperienceBonusAmount(victim, attacker, true);
        if (CheckPlayersCount())
        {
            if (!attacker.IsBot)
            {
                Http.Manager.BattleServer.Kill(victim.data.profileId, expAmount);
            }
        }

        ScoreCounter.KillInto(attacker);
        ScoreCounter.ScoreInto(attacker, expAmount);

        if (!attacker.IsBot)
        {
            if (!ProfileInfo.IsBattleTutorial)
            {
                if (victim.data.playerId == lastOffender)
                {
                    lastOffender = 0;
                    Dispatcher.Send(EventId.RevengeDone, new EventInfo_I(victim.data.playerId));
                }
                if (gameStat.ContainsKey(victim.data.playerId))
                    Notifier.ShowBonus("Kill", "bonus_kill", Localizer.GetText("lblBonus_kill", victim.data.playerName), 0);
            }
            Dispatcher.Send(EventId.ExperienceAcquired, new EventInfo_I(expAmount));
        }
    }

    private static void ExitToHangarAfterDisconnect()
    {
        Instance.CheckIfBattleAccomplished();
        GameManager.ReturnToHangar();
    }

    private static void ShowRoomCountryInfo()
    {
        Debug.Log("Room COUNTRY INFO:");
        Hashtable props = PhotonNetwork.room.CustomProperties;
        RoomCountryInfo[] cntr = props["cntr"] as RoomCountryInfo[];
        foreach (var info in cntr)
        {
            Debug.Log(info);
        }
    }

    private static void ShowRoomInfo()
    {
        Debug.Log(PhotonNetwork.room.ToStringFull());
    }

    public IEnumerator ChangeVisability()
    {
        while (true)
        {
            newChecked = (BattleMode == GameData.GameMode.Deathmatch) ?
                DeathMatchVisibilityCheck() :
                VisibilityCheck();
            foreach (var veh in allVehicles.Values)
            {
                if (StaticContainer.IsFriendOfMain(veh))
                {
                    newChecked.Add(veh);
                }
            }
            temp_ = new List<VehicleController>(newChecked);

            foreach (var old in oldChecked)
            {
                if (!temp_.Contains(old)) //потеряли врага из виду
                {
                    if ((BattleMode == GameData.GameMode.Deathmatch) ||
                        MyVehicle.data.teamId != old.data.teamId) //прячем только если речь идет о врагах
                    {
                        Dispatcher.Send(EventId.HideEnemy, new EventInfo_I(old.data.playerId));
                    }
                }
                else
                {
                    temp_.Remove(old);
                }
            }
            // в checked_ осталось то, что появилось нового
            foreach (var new_ in temp_)
            {
                Dispatcher.Send(EventId.ShowEnemy, new EventInfo_I(new_.data.playerId));
            }
            oldChecked = newChecked;
            yield return new WaitForSeconds(0.5f);
        }
    }

    public List<VehicleController> DeathMatchVisibilityCheck()
    {
        List<VehicleController> checked_ = new List<VehicleController>();
        if (allVehicles == null)
        {
            return checked_;
        }

        foreach (var another in allVehicles.Values)
        {
            if (another == null || !another.IsAvailable)
            {
                continue;
            }

            if (CanSee(myVehicle, another))
            {
                checked_.Add(another);
            }
        }
        return checked_;
    }

    public List<VehicleController> VisibilityCheck()
    {
        List<VehicleController> checked_ = new List<VehicleController>();
        if (allVehicles == null || MyVehicle == null)
        {
            return checked_;
        }
        List<VehicleController> myTeam = new List<VehicleController>();
        List<VehicleController> anotherTeam = new List<VehicleController>();
        foreach (KeyValuePair<int, List<VehicleController>> team in playersByTeams)
        {
            if (team.Key == MyVehicle.data.teamId)
            {
                myTeam = team.Value;
            }
            else
            {
                anotherTeam.AddRange(team.Value);
            }
        }
        foreach (var player in myTeam)
        {
            if (player == null || !player.IsAvailable)
            {
                continue;
            }
            foreach (var anotherPlayer in anotherTeam)
            {
                if (checked_.Contains(anotherPlayer) || anotherPlayer == null || !anotherPlayer.IsAvailable)
                {
                    continue;
                }
                if (CanSee(player, anotherPlayer))
                {
                    checked_.Add(anotherPlayer);
                }
            }
        }
        return checked_;
        //в checked_ множество врагов которых мы видим
    }

    public bool CanSee(VehicleController player, VehicleController another)
    {
        if (!CanSeeByAngle(player, another))
        {
            return false;
        }
        //if (player.name.Contains("Vehicle") && another.name.Contains("Vehicle"))
        //{
        //    return true;
        //}
        if (player.weapon == null || another.weapon == null)
        {
            return false;
        }
        if (player.data.armor <= 0 || another.data.armor <= 0)
        {
            return false;
        }
        pos = player.weapon.position + player.transform.up * 2;
        if (Physics.Raycast(pos, another.weapon.position - pos, out hit_, maxVisibleDistance))
        {
            if (hit_.collider.GetComponentInParent<VehicleController>() == another)
            {
                return true;
            }
        }
        return false;
    }

    private bool CanSeeByAngle(VehicleController player, VehicleController anotherPlayer)
    {
        if (player == null || anotherPlayer == null)
        {
            return false;
        }
        if (player.id == anotherPlayer.id)
        {
            return true;
        }

        dir = (anotherPlayer.transform.position - player.transform.position);
        dir -= Vector3.up * dir.y;
        if (player.weapon == null)
        {
            return false;
        }
        if (Vector3.Dot(player.weapon.forward - Vector3.up * player.weapon.forward.y, dir) < 0)
        {
            return false;
        }
        return true;
    }
}