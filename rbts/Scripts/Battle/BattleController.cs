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

    public static Dictionary<int, ObscuredInt> battleInventory = new Dictionary<int, ObscuredInt>(3);
    public static Dictionary<int, VehicleController> allVehicles;
    public static Dictionary<int, VehicleData> vehicleData;
    public AudioClip bonusGetSound;
    public float outOfMapDistance = 2000;

    private const float MONO_MESSAGE_TARGETS_UPDATING_FREQUENCY = 1.0f;

    private static BattleController instance;
    private static bool playerInBattle;
    private static int enemyLayer;
    private static int myPlayerId;
    private static int parallelWorldLayer;
    private static int defaultLayer;
    private static int playerLayer;
    private static int terrainMask;
    private static int obstacleMask;
    private static float battleDuration;
    
    private static Queue<AfterRespawnBonus> afterRespawnBonuses;
    
    private readonly Dictionary<int, PlayerStat> gameStat = new Dictionary<int, PlayerStat>(GameData.maxPlayers);

    [SerializeField] private Map map;

    private PhotonView photonView;
    private bool mayShowCriticalTime = true;
    private int lastOffender;
    private float timeInBattleUnity;
    private float timeInBattleReal;
    private double battleAccomplishedTime;
    private double myCreationTime = -1;
    private float realRoomCreationTime;
    private ObscuredInt currentProlongPrice;
    private VehicleController myVehicle;
    private bool roomWasFulled;
    private bool isBattleFinished = false;

    public GameData.GameMode BattleMode {get; private set;}

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
    public static bool BattleIsOver { get { return instance == null;} }

    public static bool IsBattleFinished{ get { return Instance == null ? true : Instance.isBattleFinished; } }

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
        get; private set;
    }

    public static int TerrainLayerMask
    {
        get { return terrainMask; }
    }

    public static int ObstacleMask
    {
        get { return obstacleMask; }
    }

    public static int ParallelWorldLayer
    {
        get { return parallelWorldLayer; }
    }

    public static int DefaultLayer
    {
        get { return defaultLayer; }
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
        get { return (int)(battleDuration - instance.timeInBattleReal); }
    }

    public static int MyPlayerId
    {
        get { return instance != null ? myPlayerId : -1; }
    }

    public static float TimeInBattleUnity
    {
        get { return instance.timeInBattleUnity; }
    }

    public static float TimeInBattleReal
    {
        get { return instance.timeInBattleReal; }
    }

    public static double MyCreationTime
    {
        get { return instance.myCreationTime; }
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

    public Map Map
    {
        get { return map; }
    }

    /* UNITY SECTION */

    void Awake()
    {
        if (instance != null)
            return;

        instance = this;
        BattleMode = GameData.Mode;
        LayerMask hitMask = MiscTools.GetLayerMask("Default", "Terrain", "Player", "Enemy", "Friend", "Bot1", "Bot2", "Bot3",
            "Bot4", "Bot5", "Bot6", "Bot7", "Bot8", "Bot9");
        HitMask = hitMask.value;
        CommonVehicleMask = LayerMask.GetMask("Player", "Enemy", "Friend") | BotDispatcher.BotsCommonMask;
        BattleConnectManager.AddPhotonMessageTarget(gameObject);
        battleDuration = ProfileInfo.IsBattleTutorial ? float.PositiveInfinity : GameData.BattleDuration;
        currentProlongPrice = GameManager.PROLONG_START_PRICE;
        afterRespawnBonuses = new Queue<AfterRespawnBonus>();
        myPlayerId = -1;
        myVehicle = null;
        playerInBattle = false;
        allVehicles = new Dictionary<int, VehicleController>(10);
        vehicleData = new Dictionary<int, VehicleData>(10);
        terrainMask = MiscTools.GetLayerMask(Layer.Key.Terrain);
        obstacleMask = MiscTools.GetLayerMask(Layer.Key.Terrain, Layer.Key.Default);
        photonView = GetComponent<PhotonView>();
        parallelWorldLayer = LayerMask.NameToLayer(Layer.Items[Layer.Key.ParallelWorld]);
        defaultLayer = LayerMask.NameToLayer(Layer.Items[Layer.Key.Default]);
        playerLayer = LayerMask.NameToLayer(Layer.Items[Layer.Key.Player]);
        enemyLayer = LayerMask.NameToLayer(Layer.Items[Layer.Key.Enemy]);

        if (!photonView)
        {
            DT.LogError(gameObject, "There is no photon view on Battle Controller. Disabled.");
            enabled = false;
        }

        InitAnother(); //// Инициализация других классов
        Subscriptions();

        if (GameData.IsGoldRushEnabled)
            GoldRush.ActivateForBattle();
        else
            GoldRush.Disactivate();
    }

    private void InitAnother()
    {
        StatTable.OnStopTimer += AfterCountdown;
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
        BattleConnectManager.ClearPhotonCaches();
    }

    void OnApplicationPause(bool paused)
    {
        if (paused)
            EndBattle(EndBattleCause.ApplicationPaused);
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
            Messenger.Send(EventId.VehicleKilled, new EventInfo_II(myPlayerId, myPlayerId));
        }

        if (Input.GetKeyDown(KeyCode.Keypad0) && MyVehicle)
            MyVehicle.Cheat();
#endregion
#endif

        if (XDevs.Input.GetButtonDown("Back"))
        {
            if (BattleSettings.OnScreen)
                BattleSettings.SetActive(false);
            else if (BattleChatCommands.OnScreen)
                BattleChatCommands.Instance.Hide();
            else if (!StatTable.OnScreen && myVehicle != null)
                StatTable.Show(gameStat, myPlayerId, StatTable.TableState.Quit);
        }

        if (BattleConnectManager.Instance.InBattle && roomWasFulled)
        {
            timeInBattleUnity += Time.deltaTime; // Считаем только для выявления читаков.
            timeInBattleReal = Time.realtimeSinceStartup - realRoomCreationTime;
        }

        if (!ProfileInfo.IsBattleTutorial && timeInBattleReal >= battleDuration && playerInBattle)
        {
            enabled = false;
            EndBattle(EndBattleCause.Timeouted);
            return;
        }

        float timeRemain = battleDuration - timeInBattleReal;

        // Не давать нажимать после 3 сек. до конца боя, чтобы дать время серверу на ответ
        if (myVehicle && timeRemain < 3 && TopPanelValues.CriticalTime)
        {
            TopPanelValues.ShowCriticalTime(false);
        }
        else if (myVehicle != null &&
                 !myVehicle.DoubleExperience &&
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

#if UNITY_EDITOR
        VideoOperator.UpdateCheckPressKey();
#endif
    }

    /* PHOTON SECTION */

    void OnPhotonCustomRoomPropertiesChanged(Hashtable changedProperties)
    {
        Dictionary<int, Hashtable> botProperties = null;
        foreach (var property in changedProperties)
        {
            string key = property.Key as string;
            if (key == null || property.Value == null)
                continue;

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
                    BattleConnectManager.Instance.LastMasterId = (int) property.Value;
                    continue;
            }

            if (key.StartsWith("bt")) // "bt" в начале свойств для ботов
            {
                if (botProperties == null)
                    botProperties = new Dictionary<int, Hashtable>(4);
                ConvertBotsPropertyToPlayers(key, property.Value, botProperties);
            }
        }

        if (botProperties == null)
            return;

        foreach (var botProperty in botProperties)
        {
            OnPlayerPropertiesChanged(botProperty.Key, botProperty.Value);
        }

        Messenger.Send(EventId.PhotonRoomCustomPropertiesChanged, new EventInfo_SimpleEvent());
    }

    /* PUBLIC SECTION */

    public static void SetMainVehicle(VehicleController vehicleController)
    {
        if (instance.myVehicle == null)
            instance.myVehicle = vehicleController;

        myPlayerId = vehicleController.data.playerId;

        playerInBattle = true;

        tk2dUIManager.Instance.UseMultiTouch = true;
        TopPanelValues.NickName = MyVehicle.data.playerName;
        MusicBox.Play();

        TopPanelValues.SetEarnedSilver(ProfileInfo.Silver);
        TopPanelValues.SetEarnedGold(ProfileInfo.Gold);
    }

    public static void ExitToHangar(bool normalExit)
    {
        BattleStatisticsManager.SetOtherBattleStats();

        Messenger.Send(EventId.OnExitToHangar, new EventInfo_SimpleEvent());

        instance.NormalDisconnect = normalExit;

        if (PhotonNetwork.isMasterClient && normalExit)
        {
            Hashtable properties = new Hashtable{{"lm", myPlayerId}}; // LastMasterId
            PhotonNetwork.room.SetCustomProperties(properties);
        }

        if (PhotonNetwork.connected)
            BattleConnectManager.Instance.ForcedDisconnect(ExitToHangarAfterDisconnect);
        else
            ExitToHangarAfterDisconnect();
    }

    public static void EndBattle(EndBattleCause cause)
    {
        if (instance == null)
            return;


        if (instance.myVehicle)
            instance.myVehicle.IsAvailable = false;

        playerInBattle = false;
        instance.enabled = false;
        instance.NormalDisconnect = true;

        BattleStatisticsManager.BattleStats["PhotonDisconnect"] = 0;

        TopPanelValues.ShowCriticalTime(false);

        if (cause == EndBattleCause.Timeouted || cause == EndBattleCause.FinishedTutorial)
        {
            BattleAccomplished = true;
            Messenger.Send(EventId.TankOutOfTime, new EventInfo_I(myPlayerId), Messenger.EventTargetType.ToAll);
        }
        else if (cause == EndBattleCause.Inactivity)
        {
            StatTable.MyVehicleRank = 101; // за неактивность
            Messenger.Send(EventId.PlayerKickout, new EventInfo_I(myPlayerId), Messenger.EventTargetType.ToAll);
        }

        Instance.isBattleFinished = true;
        Messenger.Send(EventId.BattleEnd, new EventInfo_I((int)cause));
        BattleStatisticsManager.SetOtherBattleStats();
        ProfileInfo.doubleExpVehicles.Remove(ProfileInfo.currentVehicle);
        BattleConnectManager.Instance.ForcedDisconnect(instance.DisconnectAndShow_Continue);
    }

    public static void RememberBonus(BonusItem.BonusType bonusType, int price)
    {
        AfterRespawnBonus bonus = new AfterRespawnBonus(bonusType, price);
        afterRespawnBonuses.Enqueue(bonus);
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
            StatTable.Hide();
        else
            StatTable.Show(gameStat, myPlayerId, StatTable.TableState.Quit);
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
            return;

        foreach (var property in properties)
        {
            if (property.Value == null) //Это удаление свойства комнаты (бред, но приходит иногда)
                continue;

            string key = (string)property.Key;
            if (key.StartsWith("ef"))
            {
                vehicleController.EffectManager.AcceptEffectSignal(key, (VehicleEffectData) property.Value);
                return;
            }

            switch (key)
            {
                case "hl":
                    int val = (int)property.Value;
                    vehicleController.Armor = val;
                    Messenger.Send(EventId.TankHealthChanged, new EventInfo_II(vehicleController.data.playerId, val));
                    break;
                case "ma":
                    vehicleController.MaxArmor = (int)property.Value;
                    break;
                case "at":
                    vehicleController.data.attack = (int)property.Value;
                    break;
                case "sp":
                    vehicleController.Speed = (float)property.Value;
                    break;
                case "rt":
                    vehicleController.RoF = (float)property.Value;
                    break;
                case "ex":
                    if (!vehicleController.PhotonView.isMine)
                        vehicleController.SyncExistance((bool)property.Value);
                    break;
                case "sc":
                    PlayerStat stat = vehicleController.Statistics;
                    int newScore = (int)property.Value;
                    if (stat.score >= 0)
                        ScoreCounter.RecalcTeamScore(stat.teamId, newScore - stat.score);
                    stat.score = newScore;
                    if (StatTable.OnScreen)
                        StatTable.Refresh(gameStat, myPlayerId);
                    break;
                case "dt":
                    vehicleController.Statistics.deaths = (int)property.Value;
                    if (StatTable.OnScreen)
                        StatTable.Refresh(gameStat, myPlayerId);
                    break;
                case "kl":
                    vehicleController.Statistics.kills = (int)property.Value;
                    if (StatTable.OnScreen)
                        StatTable.Refresh(gameStat, myPlayerId);
                    break;
                case "st":
                    Settings.ChangePlayerSettings((string)property.Value, playerId);
                    if (StatTable.OnScreen)
                        StatTable.Refresh(gameStat, myPlayerId);
                    break;
                case "rg": //Regeneration
                    if (vehicleController.PhotonView.isMine)
                        vehicleController.Regeneration = (int) property.Value;
                    break;
                case "td": //Taken damage ratio
                    vehicleController.TakenDamageRatio = (float) property.Value;
                    break;
            }
        }
    }

    public void ProlongGameForFree(float addTime)
    {
        if (BattleConnectManager.Instance.FirstConnect)
        {
            timeInBattleUnity = Mathf.Clamp(timeInBattleUnity - addTime, 0, 10000);
            timeInBattleReal = Mathf.Clamp(timeInBattleReal - addTime, 0, 10000);
        }

        TopPanelValues.ShowCriticalTime(false);

        mayShowCriticalTime = ProfileInfo.CanBuy(new ProfileInfo.Price(currentProlongPrice, ProfileInfo.PriceCurrency.Gold));

        Messenger.Send(EventId.GameProlonged, new EventInfo_SimpleEvent());
    }

    public void ProlongGameForMoney()
    {
        ProfileInfo.Price price = new ProfileInfo.Price(currentProlongPrice, ProfileInfo.PriceCurrency.Gold);

        //ProfileInfo.WriteOffBalance(price);

        currentProlongPrice = (int)(currentProlongPrice * GameManager.PROLONG_PRICE_INCREASE_RATIO);

        timeInBattleUnity -= GameData.ProlongTimeAddition;
        timeInBattleReal -= GameData.ProlongTimeAddition;
        realRoomCreationTime += GameData.ProlongTimeAddition;

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

        Messenger.Send(EventId.GameProlonged, new EventInfo_SimpleEvent());
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

            var battleTutorialPath = GameManager.CurrentResourcesFolder + "/GuiPrefabs/Battle/Tutorials/BattleTutorial";

            var battleTutorial = Instantiate(Resources.Load<BattleTutorial>(battleTutorialPath));

            battleTutorial.transform.SetParent(BattleGUI.Instance.transform, false);

            tutorialsController.name = "TutorialsController";
            battleTutorial.name = "BattleTutorial";
        }
    }

    private void Subscriptions()
    {
        Messenger.Subscribe(EventId.BeforeReconnecting, BeforeReconnecting);
        Messenger.Subscribe(EventId.TroubleDisconnect, OnTroubleDisconnect);
        Messenger.Subscribe(EventId.TankJoinedBattle, OnTankConnected);
        Messenger.Subscribe(EventId.TankLeftTheGame, OnTankLeftTheGame, 2);
        Messenger.Subscribe(EventId.VehicleKilled, OnVehicleKilled, 1);
        Messenger.Subscribe(EventId.HelicopterKilled, OnHelicopterKilled);
        Messenger.Subscribe(EventId.VehicleRespawned, OnVehicleRespawned, 1);
        Messenger.Subscribe(EventId.MainTankAppeared, OnMainTankAppeared);
        Messenger.Subscribe(EventId.ItemTaken, OnItemTaken, 2);
        Messenger.Subscribe(EventId.GoldAcquired, OnGoldAcquired);
    }

    private void Unsubscriptions()
    {
        Messenger.Unsubscribe(EventId.BeforeReconnecting, BeforeReconnecting);
        Messenger.Unsubscribe(EventId.TroubleDisconnect, OnTroubleDisconnect);
        Messenger.Unsubscribe(EventId.TankJoinedBattle, OnTankConnected);
        Messenger.Unsubscribe(EventId.TankLeftTheGame, OnTankLeftTheGame);
        Messenger.Unsubscribe(EventId.VehicleKilled, OnVehicleKilled);
        Messenger.Unsubscribe(EventId.HelicopterKilled, OnHelicopterKilled);
        Messenger.Unsubscribe(EventId.VehicleRespawned, OnVehicleRespawned);
        Messenger.Unsubscribe(EventId.MainTankAppeared, OnMainTankAppeared);
        Messenger.Unsubscribe(EventId.ItemTaken, OnItemTaken);
        Messenger.Unsubscribe(EventId.GoldAcquired, OnGoldAcquired);
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
        MessageBox.Show(MessageBox.Type.Info, Localizer.GetText("PhotonDisconnect"));
        BattleStatisticsManager.SetOtherBattleStats();
        Manager.BattleServer.EndBattle(BattleConnectManager.FAILURE_DISCONNECT_CAUSE,
            (isRequestFinished) => ExitToHangar(false));
    }

    private void OnTankConnected(EventId id, EventInfo ei)
    {
        if (StatTable.OnScreen)
            StatTable.Refresh(gameStat, myPlayerId);

        if (roomWasFulled)
            return;

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
            return;

        EventInfo_I info = ei as EventInfo_I;
        int playerId = info.int1;

        VehicleController veh;
        if (!allVehicles.TryGetValue(playerId, out veh))
            return;

        ForgetPlayerData(playerId);
        if (veh == null)
            return;

        veh.Explode();
        TankIndicators.RemoveIndicator(playerId);
        if (StatTable.OnScreen)
            StatTable.Refresh(gameStat, myPlayerId);
    }

    private void OnGoldAcquired(EventId id, EventInfo info)
    {
        if (!TopPanelValues.CriticalTime)
            mayShowCriticalTime = ProfileInfo.CanBuy(new ProfileInfo.Price(currentProlongPrice, ProfileInfo.PriceCurrency.Gold));
    }

    private void OnVehicleKilled(EventId id, EventInfo info)
    {
        EventInfo_II eventInfo = (EventInfo_II)info;

        var victimId = eventInfo.int1;
        var attackerId = eventInfo.int2;

        if (GameData.IsGame(Game.BattleOfHelicopters))
            OnVehicleKilledBOH(victimId);
        else
            OnVehicleKilled(victimId, attackerId);
    }

    private void OnHelicopterKilled(EventId id, EventInfo info)
    {
        EventInfo_IIV eventInfo = (EventInfo_IIV)info;

        var victimId = eventInfo.int1;
        var attackerId = eventInfo.int2;

        VehicleController victim, attacker;

        if (!allVehicles.TryGetValue(victimId, out victim) || !victim.IsAvailable)
            return;

        if (victimId == myPlayerId)
        {
            if (attackerId == myPlayerId)
                return;

            lastOffender = attackerId;

            if (CheckPlayersCount())
            {
                ScoreCounter.DeathInto(victim);

                if (allVehicles.TryGetValue(attackerId, out attacker) && CheckPlayersCount())
                    Http.Manager.BattleServer.Death(attacker.data.profileId);
                else
                    Debug.LogError("Attacker " + attackerId + " not found in all helicopters.");
            }
        }
        else if (attackerId == myPlayerId)
        {
            if (ProfileInfo.IsBattleTutorial)
                return;

            attacker = MyVehicle;

            int expAmount = BonusDispatcher.ExperienceBonusAmount(victim, attacker, true);
            if (CheckPlayersCount())
            {
                Http.Manager.BattleServer.Kill(victim.data.profileId, expAmount);
            }

            if (expAmount == 0)
                return;

            ScoreCounter.ScoreInto(attacker, expAmount);

            if (victimId == lastOffender)
            {
                lastOffender = 0;
                Messenger.Send(EventId.RevengeDone, new EventInfo_I(victimId));
            }

            Messenger.Send(EventId.ExperienceAcquired, new EventInfo_I(expAmount));

            if (!CheckPlayersCount() || attacker == victim)
                return;

            ScoreCounter.KillInto(attacker);
            if (gameStat.ContainsKey(victimId))
                Notifier.ShowBonus("Kill", "Kill_substrate", Localizer.GetText("lblBonus_kill", gameStat[victimId].playerName), 0, null);
        }
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

        //var playerId = info.int3;
        allVehicles[info.int3].TakeBonus((BonusItem.BonusType)info.int1, info.int2);
    }

    private void OnMainTankAppeared(EventId id, EventInfo ei)
    {
        myCreationTime = PhotonNetwork.time;
        if (BattleConnectManager.Instance.FirstConnect)
        {
            realRoomCreationTime = Time.realtimeSinceStartup;
            battleAccomplishedTime = realRoomCreationTime + battleDuration;
        }

        BattleAccomplished = false;
        ProfileInfo.WasInBattle = true;

        if (!BattleConnectManager.Instance.FirstConnect)
            return;

        if (CheckPlayersCount())
        {
            roomWasFulled = true;
            BattleGUI.SetStatusText(null);
        }
        else
            BattleGUI.SetStatusText(Localizer.GetText("WaitingForPlayers"));

        SubtractHangarConsumables();
    }

    private void OnVehicleKilled(int victimId, int attackerId)
    {
        VehicleController victim, attacker;
        if (!allVehicles.TryGetValue(victimId, out victim) || !victim.IsAvailable || !allVehicles.TryGetValue(attackerId, out attacker))
            return;

        victim.Explode();
        if (victim.PhotonView.isMine)
            OnMySideVictim(victim, attacker);
        if (attacker.PhotonView.isMine)
            OnMySideAttacker(victim, attacker);
    }

    private void OnVehicleKilledBOH(int victimId)
    {
        VehicleController victim;

        if (!allVehicles.TryGetValue(victimId, out victim) || !victim.IsAvailable)
            return;

        victim.Explode();

        if (victimId == myPlayerId)
            StatTable.Show(gameStat, myPlayerId, StatTable.TableState.AfterDeath);
    }

    private void SubtractHangarConsumables()
    {
        ProfileInfo.doubleExpVehicles.Remove(ProfileInfo.currentVehicle);
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
                        unnecessaryStats.Add(stat);
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
            BattleCamera.Instance.ReturnToStart();

        instance.enabled = false;
    }

    private void ApplyAfterRespawnBonus()
    {
        VehicleEffectData effectData;

        if (afterRespawnBonuses.Count == 0)
            return;

        foreach (AfterRespawnBonus bonus in afterRespawnBonuses)
        {
            switch (bonus.type)
            {
                case BonusItem.BonusType.Attack:
                    effectData = new VehicleEffectData(VehicleEffect.ParameterType.Attack,
                        VehicleEffect.ModifierType.Product, 1.2f, 30f);
                    break;
                case BonusItem.BonusType.Reload:
                    effectData = new VehicleEffectData(VehicleEffect.ParameterType.RoF, VehicleEffect.ModifierType.Product, 1.2f, 30f, -1);
                    break;
                default:
                    return;
            }

            myVehicle.RequestEffect(effectData);
        }

        afterRespawnBonuses.Clear();
    }

    private void AfterCountdown()
    {
        myVehicle.MakeRespawn(false, true, false);
    }

    private void CheckIfBattleAccomplished()
    {
        BattleAccomplished = MyVehicle!= null && Time.realtimeSinceStartup >= battleAccomplishedTime - 0.1; // -0.1 - защита от всяческих погрешностей
    }

    /// <summary>
    /// Преобразует свойство бота из room custom property в player custom property (как у обычных игроков)
    /// </summary>
    /// <param name="key">Имя ключа из room.CustomProperties</param>
    /// <param name="value">значение свойства</param>
    /// <param name="collection">Словарь для сбора свойств</param>
    private void ConvertBotsPropertyToPlayers(string key, object value, Dictionary<int, Hashtable> collection)
    {
        int botId;
        int firstDigitIndex = MiscTools.FindFirstInString(key, char.IsDigit);
        int firstLetterIndex = MiscTools.FindFirstInString(key, char.IsLetter, firstDigitIndex);
        
        if (firstDigitIndex == -1 || !int.TryParse(key.Substring(firstDigitIndex, firstLetterIndex - firstDigitIndex), out botId))
        {
            Debug.LogError("Incorrect bot playedId");
            return;
        }

        string playerProperty = key.Substring(firstLetterIndex); // Название соответствующего свойства у обычного игрока (не бота)
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
        }
    }

    private void OnMySideAttacker(VehicleController victim, VehicleController attacker)
    {
        if (victim == attacker || VehicleController.AreFriends(victim, attacker))
            return;

        int expAmount = BonusDispatcher.ExperienceBonusAmount(victim, attacker, true);
        if (CheckPlayersCount())
        {
            if (!attacker.IsBot)
                Http.Manager.BattleServer.Kill(victim.data.profileId, expAmount);
        }

        ScoreCounter.KillInto(attacker);
        ScoreCounter.ScoreInto(attacker, expAmount);

        if (!attacker.IsBot)
        {
            if (victim.data.playerId == lastOffender)
            {
                lastOffender = 0;
                Messenger.Send(EventId.RevengeDone, new EventInfo_I(victim.data.playerId));
            }

            if (gameStat.ContainsKey(victim.data.playerId))
                Notifier.ShowBonus("Kill", "Kill_substrate", Localizer.GetText("lblBonus_kill", victim.data.playerName), 0);

            Messenger.Send(EventId.ExperienceAcquired, new EventInfo_I(expAmount));
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

    public enum VehiclesSet
    {
        AllExcludeBots,
        AllInDeathMatchOrYourTeamExcludeBots,//For chat commands
    }

    public static List<int> GetVehiclesIdList(VehiclesSet setId)
    {
        List<int> receivers = new List<int>();
        switch (setId)
        {
            case VehiclesSet.AllExcludeBots:
                foreach (var vehiclePair in allVehicles)
                {
                    if (!vehiclePair.Value.IsBot)
                        receivers.Add(vehiclePair.Key);
                }
                break;
            case VehiclesSet.AllInDeathMatchOrYourTeamExcludeBots:
                foreach (var vehiclePair in allVehicles)
                {
                    if (!vehiclePair.Value.IsBot && (!Instance.IsTeamMode || (Instance.IsTeamMode && vehiclePair.Value.data.teamId == MyVehicle.data.teamId)))
                        receivers.Add(vehiclePair.Key);
                }
                break;
        }

        return receivers;
    }
}
