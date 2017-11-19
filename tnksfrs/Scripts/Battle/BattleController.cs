using System;
using System.Linq;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using CodeStage.AntiCheat.ObscuredTypes;
using Disconnect;
using Http;
using Matchmaking;
using XD;
using XD.CONSOLE;
using XDevs.LiteralKeys;

#if !UNITY_WSA
using Rewired;
#endif

using Hashtable = ExitGames.Client.Photon.Hashtable;

public class BattleController : MonoBehaviour, IBattleController
{
    [SerializeField]
    private bool friendlyFire = false;

    private Skidmarks skidmarks = null;    

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
            return StaticType.BattleController;
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
            return "[BattleController] " + name;
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

    public void Event(Message message, params object[] _parameters)
    {
        for (int i = 0; i < Subscribers.Count; i++)
        {
            Subscribers[i].Reaction(message, _parameters);
        }
    }
    #endregion

    #region ISubscriber       
    public void Reaction(Message message, params object[] parameters)
    {
        //Debug.LogError(name + " Reaction on '" + message + "' with parameters: " + parameters.ToFullString(), this);
        switch (message)
        {
            case Message.Cheat:
                switch (parameters.Get<Cheat>())
                {
                    case Cheat.FixedTimeStep:
                        break;
                }
                break;

            case Message.ToHangar:
            case Message.QuitToHangar:
                ExitToHangar(true);
                break;

            case Message.MessageBoxResult:
                switch (parameters.Get<MessageBoxType>())
                {
                    case MessageBoxType.Notification:
                    case MessageBoxType.QuitToHangar:
                        if (!parameters.Get<bool>())
                        {
                            return;
                        }
                        break;
                }

                //Debug.LogError(name + " Reaction on '" + message + "' with parameters: " + parameters.ToFullString(), this);
                FailureQuit();
                break;
        }
    }
    #endregion

    public Dictionary<int, VehicleController> Units
    {
        get;
        set;
    }

    public bool FriendlyFire
    {
        get
        {
            return friendlyFire;
        }
    }

    public IUnitBehaviour[] GetUnitsByTeam(int team = -1)
    {
        if (team == -1)
        {
            team = CurrentUnit.Team;
        }

        return Units.Values.Where(vc => vc.Team == team && vc.IsAvailable).ToArray();
    }
    
    public Skidmarks Skidmarks
    {
        get
        {
            if (skidmarks == null)
            {
                skidmarks = FindObjectOfType<Skidmarks>();
            }
            return skidmarks;
        }
    }

    //public Dictionary<int, List<IUnitBehaviour>> UnitsTeam
    //{
    //    get
    //    {
    //        foreach(unit)
    //    }
    //}

    public Dictionary<int, TankData> UnitsData
    {
        get;
        set;
    }

    public const int                                    DEFAULT_TARGET_ID = -1;
    public const int                                    OPPONENTS_NEEDED = 3;
    public static Vector3                               targetPosition = new Vector3();
    public AudioClip                                    bonusGetSound = null;
    public float                                        outOfMapDistance = 2000;

    public List<IConsumableBattle>                      activeAbilities = new List<IConsumableBattle>(); 

    private const float                                 MONO_MESSAGE_TARGETS_UPDATING_FREQUENCY = 1.0f;

    private static BattleController                     instance = null;
    private static bool                                 playerInBattle = false;
    private static int                                  enemyLayer = 0;
    private static int                                  parallelWorldLayer = 0;
    private static int                                  playerLayer = 0;
    private static int                                  terrainMask = 0;
    private static double                               roomCreationTime = 0;
    private static float                                battleDuration = 0;
    
    private static Queue<AfterRespawnBonus>             afterRespawnBonuses = null;

    private readonly Dictionary<int, PlayerStat>        gameStat = new Dictionary<int, PlayerStat>(GameData.maxPlayers);

    [SerializeField]
    private bool                                        debug = false;

    [SerializeField]
    private Map                                         map = null;

    [SerializeField]
    private LayerMask                                   hitMask = 0;

    private PhotonView                                  photonView = null;
    private bool                                        mayShowCriticalTime = true;
    private int                                         lastOffender = 0;
    private float                                       timeInBattleUnity = 0;
    private float                                       timeInBattlePhoton = 0;
    private float                                       previousPhotonTime = -1;
    private double                                      battleAccomplishedTime = 0;
    private double                                      myCreationTime = 0;
    private ObscuredInt                                 currentProlongPrice = new ObscuredInt();
    private VehicleController                           myVehicle = null;
    private bool                                        roomWasFulled = false;
    private bool                                        battleTutorialCompleted = false;
    private int                                         myPlayerId = -2;

#region*** DEBUG ***
    public Buff            buffOnMyVeh = null;
    public Buff[]            buffsForMyVeh = {};
#endregion



    public GameData.GameMode BattleMode
    {
        get; private set;
    }

    public bool IsTeamMode
    {
        get
        {
            return BattleMode == GameData.GameMode.Team;
        }
    }

    public static BattleController Instance
    {
        get
        {
            return instance;
        }
    }
    
    /// <summary>
    /// Показывает, закончена ли битва (с последнего кадра жизни битвы)
    /// </summary>
    public static bool BattleIsOver
    {
        get
        {
            return instance == null;
        }
    }

    public bool NormalDisconnect
    {
        get; private set;
    }

    private static bool battleAccomplished;
    public static bool BattleAccomplished
    {
        get
        {
            return battleAccomplished;
        }
        private set
        {
            battleAccomplished = value;
        }
    }

    public static bool PlayerInBattle
    {
        get
        {
            return playerInBattle;
        }
    }

    public int Rank
    {
        get;
        set;
    }

    public bool IsEnoughPlayers
    {
        get;
        set;
    }

    public static bool CheckPlayersCount()
    {
        return true;
        //return ProfileInfo.IsBattleTutorial || (PhotonNetwork.room != null && PhotonNetwork.room.playerCount >= GameData.minPlayersForBattleStart);
    }

    public static int LastOffender
    {
        get
        {
            return instance.lastOffender;
        }
    }

    public LayerMask HitMask
    {
        get
        {
            return instance.hitMask;
        }
    }

    public static int TerrainLayerMask
    {
        get
        {
            return terrainMask;
        }
    }

    public static int ParallelWorldLayer
    {
        get
        {
            return parallelWorldLayer;
        }
    }

    public static int PlayerLayer
    {
        get
        {
            return playerLayer;
        }
    }

    public static int EnemyLayer
    {
        get
        {
            return enemyLayer;
        }
    }

    public static int TimeRemaining
    {
        get
        {
            return Mathf.Clamp((int)(battleDuration - TimeInBattlePhoton), 0, 10000);
        } //Only positive values
    }

    public int MyPlayerId
    {
        get
        {
            return myPlayerId;
        }
    }

    public static float TimeInBattleUnity
    {
        get
        {
            return instance.timeInBattleUnity;
        }
    }

    public static float TimeInBattlePhoton
    {
        get
        {
            if (instance !=null && instance.IsTeamMode)
            {
                return TimeAfterStartBattlePhoton;
            }
            if (instance != null)
            {
                return instance.timeInBattlePhoton;
            }
            return 0f;
        }
    }

    public static float TimeAfterStartBattlePhoton
    {
        get
        {
            return StaticContainer.Master.Timer.Current;
        }
    }

    public static double MyCreationTime
    {
        get
        {
            return instance.myCreationTime;
        }
    }

    public static double RoomTime
    {
        get
        {
            if (roomCreationTime < 0.01)
            {
                roomCreationTime = (double) PhotonNetwork.room.CustomProperties["ct"]/1000;
            }

            return (PhotonNetwork.time - roomCreationTime) / 1000;
        }
    }

    public static AudioClip BonusGetSound
    {
        get
        {
            return instance != null ? instance.bonusGetSound : null;
        }
    }

    public VehicleController CurrentUnit
    {
        get
        {
            return myVehicle;
        }
    }

    public Dictionary<int, PlayerStat> GameStat
    {
        get
        {
            return gameStat;
        }

        set
        {
            //gameStat = value;
        }
    }

    public Vector3 TargetPosition
    {
        get
        {
            return targetPosition;
        }

        set
        {
            targetPosition = value;
        }
    }

    public int CurrentProlongPrice
    {
        get
        {
            return currentProlongPrice;
        }
    }

    public float DeltaTimePhoton
    {
        get
        {
            float currentPhotonTime = (float)PhotonNetwork.time;
            float result = previousPhotonTime < 0 ? 0 : currentPhotonTime - previousPhotonTime;
            previousPhotonTime = currentPhotonTime;
            return result;
        }
    }

    public Map Map
    {
        get
        {
            return map;
        }
    }

    public void AddData(int playerID, VehicleController controller, TankData data, PlayerStat statistics)
    {
        if (!controller.IsBot && controller.IsMine)
        {
            myPlayerId = playerID;
        }

        Units[playerID] = controller;
        UnitsData[playerID] = data;
        GameStat[playerID] = statistics;

        Event(Message.PlayerLoaded, playerID, controller, data, statistics);
    }

    /* UNITY SECTION */    
    private void Awake()
    {
        SaveInstance();
        instance = this;

        BattleMode = GameData.Mode;
        if (hitMask == 0)
        {
            hitMask = MiscTools.GetLayerMask("Default", "Terrain", "Player", "Enemy", "Friend", "Bot1", "Bot2");
        }
        StaticContainer.Connector.AddPhotonMessageTarget(gameObject);

        battleDuration = StaticContainer.Profile.BattleTutorialCompleted ? GameData.BattleDuration : float.PositiveInfinity;
        currentProlongPrice = Constants.PROLONG_START_PRICE;
        afterRespawnBonuses = new Queue<AfterRespawnBonus>();
        myPlayerId = -1;
        myVehicle = null;
        playerInBattle = false;
        Units = new Dictionary<int, VehicleController>(10);
        UnitsData = new Dictionary<int, TankData>(10);
        terrainMask = MiscTools.GetLayerMask(Layer.Key.Terrain);
        photonView = GetComponent<PhotonView>();
        parallelWorldLayer = LayerMask.NameToLayer(Layer.Items[Layer.Key.ParallelWorld]);
        playerLayer = LayerMask.NameToLayer(Layer.Items[Layer.Key.Player]);
        enemyLayer = LayerMask.NameToLayer(Layer.Items[Layer.Key.Enemy]);

        if (!photonView)
        {
            DT.LogError(gameObject, "There is no photon view on Battle Controller. Disabled.");
            enabled = false;
        }

        //StartCoroutine(Timer());
        //StatTable.OnStopTimer += AfterCountdown;

        Subscribes();

        CodeStage.AntiCheat.Detectors.SpeedHackDetector.isRunning = true;

        if (GameData.IsGoldRushEnabled)
        {
            GoldRush.ActivateForBattle();
        }
        else
        {
            GoldRush.Disactivate();
        }
    }

    private IEnumerator Timer()
    {
        yield return new WaitForSeconds(3);
        AfterCountdown();
    }

    private void Start()
    {
        StaticType.UI.AddSubscriber(this);

        AddSubscriber(StaticType.UI.Instance());
        AddSubscriber(StaticType.Statistics.Instance());
        AddSubscriber(StaticType.MainData.Instance());
        AddSubscriber(StaticType.MusicDispatcher.Instance());


        battleTutorialCompleted = StaticContainer.Profile.BattleTutorialCompleted;
    }

    private void OnDestroy()
    {
        StaticType.UI.RemoveSubscriber(this);
        //Debug.LogError(name + " OnDestroy: " + GameStat.Count + ", playerID: " + PhotonNetwork.player.ID, this);

        DeleteInstance();
        instance = null;
        UnitsData = null;
        Units = null;
        GameStat.Clear();
        playerInBattle = false;
        myPlayerId = -1;
        myVehicle = null;
        Unsubscribes();
        GoldRush.Disactivate();
        ShellPoolManager.ClearAllPools();
        StaticContainer.Connector.ClearPhotonMessageTargets();
    }

    public IUnitBehaviour GetUnitByPlayerID(int playerID)
    {
        VehicleController controller = null;
        if (Units.TryGetValue(playerID, out controller))
        {
            return controller;
        }

        return null;
    }

    private void OnApplicationPause(bool paused)
    {
        if (paused)
        {
            EndBattle(EndBattleCause.ApplicationPaused);
        }
    }

    private void Update()
    {
        #region Функционал для отладки

#if UNITY_EDITOR

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
            Dispatcher.Send(EventId.TankKilled, new EventInfo_II(MyPlayerId, MyPlayerId));

            if (GameData.IsGame(Game.BattleOfHelicopters))
            {
                Dispatcher.Send(
                    id: EventId.HelicopterKilled,
                    info: new EventInfo_IIV(MyPlayerId, MyPlayerId, ((HelicopterController) myVehicle).LastPositionAlive),
                    target: Dispatcher.EventTargetType.ToAll);
            }
        }

        if (Input.GetKeyDown(KeyCode.Keypad1) && CurrentUnit)
        {
            CurrentUnit.ActivateSlowpoke();
        }

#endif

        #endregion

        if (XDevs.Input.GetButtonDown("Back"))
        {
            //if (myVehicle != null)
            //{                
            //    StaticType.GameController.Instance<IGameController>().RequestUnitSelection(WindowShowCause.Quit);
            //}
        }

        if (!playerInBattle)
        {
            return;
        }

        if (StaticContainer.Connector.InBattle && roomWasFulled)
        {
            timeInBattleUnity += Time.deltaTime; // Считаем только для выявления читаков.
            timeInBattlePhoton += DeltaTimePhoton;
        }
        else
        {
            if (previousPhotonTime < 0)
            {
                previousPhotonTime = (float) PhotonNetwork.time; // Борьба с неправильным вычислением DeltaTimePhoton
            }
        }

        if (battleTutorialCompleted && TimeAfterStartBattlePhoton >= battleDuration)
        {
            enabled = false;
            StaticType.GameController.Instance().Reaction(Message.Timeout);
            return;
        }

        float timeRemain = battleDuration - timeInBattlePhoton;

        // Не давать нажимать после 3 сек. до конца боя, чтобы дать время серверу на ответ
        if (myVehicle && timeRemain < 3 && StaticContainer.UI.CriticalTime)
        {
            //TopPanelValues.ShowCriticalTime(false);
        }
        else if (myVehicle && mayShowCriticalTime &&
                 GameData.IsProlongTimeEnabled &&
                 timeRemain <= GameData.ProlongTimeShow)
        {
            if (ProfileInfo.CanBuy(new ProfileInfo.Price(currentProlongPrice, ProfileInfo.PriceCurrency.Gold)) &&
                !CurrentUnit.DoubleExperience)
            {
                //TopPanelValues.ShowCriticalTime(true, currentProlongPrice);
            }

            mayShowCriticalTime = false;
        }

        //CheckMyEffects();
        UpdateConsumables();
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
                    //if (StatTable.OnScreen)
                    //    StatTable.Refresh(gameStat, myPlayerId);
                    continue;
                case "awardPermission":
                    GoldRush.AwardPermission = (bool)property.Value;
                    continue;
                case "lm": // Last master's id
                    StaticContainer.Connector.LastMasterId = (int)property.Value;
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

        Dispatcher.Send(EventId.PhotonRoomCustomPropertiesChanged, new EventInfo_SimpleEvent());
    }

    /* PUBLIC SECTION */

    public void SetMainVehicle(VehicleController vehicleController)
    {
        //Debug.LogError("Set main vehicle: " + vehicleController.name);

        //if (instance.myVehicle == null)
        myVehicle = vehicleController;

        myPlayerId = vehicleController.data.playerId;
        playerInBattle = true;

        //tk2dUIManager.Instance.UseMultiTouch = true;
        //TopPanelValues.NickName = CurrentUnit.data.playerName;

        //TopPanelValues.SetEarnedSilver(ProfileInfo.Silver);
        //TopPanelValues.SetEarnedGold(ProfileInfo.Gold);

        roomCreationTime = (double)PhotonNetwork.room.CustomProperties["ct"];
    }

    private bool exited = false;

    public void ExitToHangar(bool normalExit)
    {
        if (exited)
        {
            return;
        }
        exited = true;

        Event(Message.ToHangar, GameStat);
        //Debug.LogError(name + " Message.ToHangar: " + GameStat.Count + ", playerID: " + PhotonNetwork.player.ID, this);
        StaticType.GameController.Instance<IGameController>().BattleEnded = true;
        StaticType.BattleStatistics.Instance<IBattleStatistics>().SetOtherBattleStats();

        Dispatcher.Send(EventId.OnExitToHangar, new EventInfo_SimpleEvent());

        NormalDisconnect = normalExit;

        if (PhotonNetwork.isMasterClient && normalExit)
        {
            Hashtable properties = new Hashtable { { "lm", MyPlayerId } }; // LastMasterId
            PhotonNetwork.room.SetCustomProperties(properties);
        }

        Manager.Instance().battleServer.EndBattle(normalExit ? "NormalDisconnectForUserRequest" : "FailureDisconnect");
        //Event(Message.LockUI, true);

        if (PhotonNetwork.connected)
        {
            StaticContainer.Connector.ForcedDisconnect(ExitToHangarAfterDisconnect);
        }
        else
        {
            ExitToHangarAfterDisconnect();
        }
    }

    public void EndBattle(EndBattleCause cause)
    {
        //Debug.LogError(name + " EndBattle: " + cause, this);
        if (myVehicle)
        {
            myVehicle.IsAvailable = false;
        }

        playerInBattle = false;
        enabled = false;
        NormalDisconnect = true;

        BattleStatisticsManager.BattleStats["PhotonDisconnect"] = 0;

        //TopPanelValues.ShowCriticalTime(false);

        if (cause == EndBattleCause.Timeouted || cause == EndBattleCause.TeamDefeated || cause == EndBattleCause.AllTanksDestroyed || cause == EndBattleCause.BaseWasCaptured)
        {
            BattleAccomplished = true;
            Dispatcher.Send(EventId.TankOutOfTime, new EventInfo_I(MyPlayerId), Dispatcher.EventTargetType.ToAll);
        }
        else if (cause == EndBattleCause.Inactivity)
        {            
            Rank = 101;
            Dispatcher.Send(EventId.PlayerKickout, new EventInfo_I(MyPlayerId), Dispatcher.EventTargetType.ToAll);
        }

        Dispatcher.Send(EventId.BattleEnd, new EventInfo_I((int)cause));
        StaticType.BattleStatistics.Instance<IBattleStatistics>().SetOtherBattleStats();
        DisconnectAndShow();
        
        if (cause == EndBattleCause.FinishedTutorial)
        {
            Event(Message.PrepareToEndBattle, XD.BattleResult.Victory, false, GameStat);
            IStatisticItem damage = StaticContainer.Statistics.GetStatByParameter(StatisticParameter.Damage);            
            IUIEndBattle endBattle = new EndBattle(XD.BattleResult.Victory, 1, damage == null ? 0 : (int)damage.Value);
            Event(Message.LayoutRequest, PSYWindow.EndBattle, endBattle);
        }
    }

    public static void RememberBonus(BonusItem.BonusType bonusType, int price)
    {
        AfterRespawnBonus bonus = new AfterRespawnBonus(bonusType, price);
        afterRespawnBonuses.Enqueue(bonus);
    }

    public void FailureQuit()
    {
        StaticType.BattleStatistics.Instance<IBattleStatistics>().SetOtherBattleStats();
        ExitToHangar(false);
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
        if (!Units.TryGetValue(playerId, out vehicleController))
        {
            //Debug.LogError("!Units.TryGetValue: " + playerId);
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
                    vehicleController.HPSystem.SetArmor(val, -1);
                    Dispatcher.Send(EventId.TankHealthChanged, new EventInfo_II(vehicleController.data.playerId, val));
                    break;

                case "at":                    
                    vehicleController.Settings[Setting.Damage].Max = (int)property.Value;
                    break;

                case "uid":
                    vehicleController.data.unitId = (int)property.Value;
                    break;

                case "sp":
                    vehicleController.MovingSpeed = (float)property.Value;
                    break;

                case "tsp":
                    vehicleController.TurretSpeed = (float)property.Value;
                    break;

                case "rf":
                    vehicleController.ROF = (float)property.Value;
                    break;

                case "ir":
                    vehicleController.IRCMROF = (float)property.Value;
                    break;

                case "ex":
                    if (!vehicleController.PhotonView.isMine)
                    {
                        vehicleController.SyncExistance((bool) property.Value);
                    }
                    break;

                case "sc":
                    PlayerStat stat = vehicleController.Statistics;
                    int newScore = (int)property.Value;

                    if (stat.Stats[StatisticParameter.Experience] >= 0)
                    {
                        ScoreCounter.RecalcTeamScore(stat.Team, newScore - stat.Stats[StatisticParameter.Experience]);
                    }

                    stat.Stats[StatisticParameter.Experience] = newScore;
                    RefreshPlayers(GameStat, MyPlayerId);
                    break;

                case "dt":
                    vehicleController.Statistics.Stats[StatisticParameter.Deaths] = (int)property.Value;
                    RefreshPlayers(GameStat, MyPlayerId);
                    break;

                case "kl":
                    vehicleController.Statistics.Stats[StatisticParameter.Kills] = (int)property.Value;
                    RefreshPlayers(GameStat, MyPlayerId);
                    break;

                case "dm":
                    int result = (int) property.Value;

                    /*if (vehicleController.IsMine)
                    {
                        //int delta = result - vehicleController.Statistics.Stats[StatisticParameter.Damage];
                        //Debug.LogError("damage Changed: " + vehicleController.Statistics.Stats[StatisticParameter.Damage] + " -> " + (int) property.Value + ", delta: " + delta);
                        //vehicleController.Event(Message.StatisticUpdate, StatisticParameter.Damage, -1, (float)delta, -1);
                    }*/

                    vehicleController.Statistics.Stats[StatisticParameter.Damage] = result;
                    RefreshPlayers(GameStat, MyPlayerId);
                    break;

                case "st":
                    Settings.ChangePlayerSettings((string)property.Value, playerId);
                    RefreshPlayers(GameStat, MyPlayerId);
                    break;

                case "da":
                    vehicleController.Settings[Setting.DamageAbsorption].Set(new Clamper((float) property.Value));
                    //vehicleController.data.DamageAbsorption = (float)property.Value;
                    break;

                case "dap":
                    vehicleController.Settings[Setting.DamageAbsorptionProbability].Set(new Clamper((float)property.Value));
                    //vehicleController.data.DamageAbsorptionProbability = (float)property.Value;
                    break;
            }
        }
    }

    public void RefreshPlayers(Dictionary<int, PlayerStat> stat, int myPlayerId)
    {
        //А ТУТ БЛЯ ПУСТО!!! О__о
    }

    public void ProlongGameForFree(float addTime)
    {
        if (StaticContainer.Connector.FirstConnect)
        {
            timeInBattleUnity = Mathf.Clamp(timeInBattleUnity - addTime, 0, 10000);
            timeInBattlePhoton = Mathf.Clamp(timeInBattlePhoton - addTime, 0, 10000);
        }

        //TopPanelValues.ShowCriticalTime(false);

        mayShowCriticalTime = ProfileInfo.CanBuy(new ProfileInfo.Price(currentProlongPrice, ProfileInfo.PriceCurrency.Gold));

        Dispatcher.Send(EventId.GameProlonged, new EventInfo_SimpleEvent());
    }

    public void ProlongGameForMoney()
    {
        ProfileInfo.Price price = new ProfileInfo.Price(currentProlongPrice, ProfileInfo.PriceCurrency.Gold);

        //ProfileInfo.WriteOffBalance(price);

        currentProlongPrice = (int)(currentProlongPrice * XD.Constants.PROLONG_PRICE_INCREASE_RATIO);

        timeInBattleUnity -= GameData.ProlongTimeAddition;
        timeInBattlePhoton -= GameData.ProlongTimeAddition;

        //TopPanelValues.ShowCriticalTime(false);

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
        Units.Remove(playerId);
        UnitsData.Remove(playerId);
        GameStat.Remove(playerId);
    }

    /* PRIVATE SECTION */    
    private void Subscribes()
    {
        Dispatcher.Subscribe(EventId.BeforeReconnecting, BeforeReconnecting);
        Dispatcher.Subscribe(EventId.TroubleDisconnect, OnTroubleDisconnect);
        Dispatcher.Subscribe(EventId.TankJoinedBattle, OnTankConnected);
        Dispatcher.Subscribe(EventId.TankLeftTheGame, OnTankLeftTheGame, 2);
        Dispatcher.Subscribe(EventId.ShellEffect, OnShellEffect);
        Dispatcher.Subscribe(EventId.TankKilled, OnVehicleKilled, 1);
        Dispatcher.Subscribe(EventId.HelicopterKilled, OnHelicopterKilled);
        Dispatcher.Subscribe(EventId.TankRespawned, OnVehicleRespawned, 1);
        Dispatcher.Subscribe(EventId.TankEffectApply, OnVehicleEffectChanges);
        Dispatcher.Subscribe(EventId.TankEffectCancelled, OnVehicleEffectChanges);
        Dispatcher.Subscribe(EventId.MainTankAppeared, OnMainTankAppeared);
        Dispatcher.Subscribe(EventId.ItemTaken, OnItemTaken, 2);
        Dispatcher.Subscribe(EventId.GoldAcquired, OnGoldAcquired);
    }

    private void Unsubscribes()
    {
        Dispatcher.Unsubscribe(EventId.BeforeReconnecting, BeforeReconnecting);
        Dispatcher.Unsubscribe(EventId.TroubleDisconnect, OnTroubleDisconnect);
        Dispatcher.Unsubscribe(EventId.TankJoinedBattle, OnTankConnected);
        Dispatcher.Unsubscribe(EventId.TankLeftTheGame, OnTankLeftTheGame);
        Dispatcher.Unsubscribe(EventId.TankEffectApply, OnVehicleEffectChanges);
        Dispatcher.Unsubscribe(EventId.TankEffectCancelled, OnVehicleEffectChanges);
        Dispatcher.Unsubscribe(EventId.TankKilled, OnVehicleKilled);
        Dispatcher.Unsubscribe(EventId.HelicopterKilled, OnHelicopterKilled);
        Dispatcher.Unsubscribe(EventId.TankRespawned, OnVehicleRespawned);
        Dispatcher.Unsubscribe(EventId.MainTankAppeared, OnMainTankAppeared);
        Dispatcher.Unsubscribe(EventId.ItemTaken, OnItemTaken);
        Dispatcher.Unsubscribe(EventId.ShellEffect, OnShellEffect);
        Dispatcher.Unsubscribe(EventId.GoldAcquired, OnGoldAcquired);
    }

    private void BeforeReconnecting(EventId id, EventInfo ei)
    {
        Units.Clear();
        UnitsData.Clear();
        GameStat.Clear();
        StopAllCoroutines();
        CancelInvoke();
        playerInBattle = false;
        myVehicle = null;
    }

    private void OnTroubleDisconnect(EventId id, EventInfo ei)
    {
        Event(Message.MessageBox, MessageBoxType.Notification, "UI_MB_ApplicationErrorTitle", "UI_MB_PhotonDisconnect", "UI_Quit");
        //MessageBox.Show(MessageBox.Type.Info, Localizer.GetText("PhotonDisconnect"), FailureQuit);
        FailureQuit();
    }

    private void OnTankConnected(EventId id, EventInfo ei)
    {
        RefreshPlayers(GameStat, MyPlayerId);

        if (roomWasFulled)
        {
            return;
        }

        if (CheckPlayersCount() && CurrentUnit != null)
        {
            roomWasFulled = true;
            Manager.Instance().battleServer.StartTimer((int)(PhotonNetwork.time - StaticContainer.GameManager.TimeInRoom));
            //BattleGUI.SetStatusText(null);
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

        VehicleController victim;
        if (!Units.TryGetValue(playerId, out victim))
        {
            return;
        }

        ForgetPlayerData(playerId);
        if (victim == null)
        {
            return;
        }
        
        victim.HPSystem.Death();
        //TankIndicators.RemoveIndicator(playerId);
        RefreshPlayers(GameStat, MyPlayerId);
    }

    private void OnShellEffect(EventId id, EventInfo ei)
    {
        EventInfo_IE info = (EventInfo_IE)ei;

        if (info.int1 != MyPlayerId)
            return;

    }

    private void OnGoldAcquired(EventId id, EventInfo info)
    {
        if (!StaticContainer.UI.CriticalTime)
            mayShowCriticalTime = ProfileInfo.CanBuy(new ProfileInfo.Price(currentProlongPrice, ProfileInfo.PriceCurrency.Gold));
    }

    private void OnVehicleKilled(EventId id, EventInfo info)
    {
        EventInfo_II eventInfo = (EventInfo_II)info;

        var victimId = eventInfo.int1;
        var attackerId = eventInfo.int2;

        if (GameData.IsGame(Game.BattleOfHelicopters))
        {
            OnVehicleKilledBOH(victimId);
        }
        else
        {
            OnVehicleKilled(victimId, attackerId);
        }
    }

    private void OnHelicopterKilled(EventId id, EventInfo info)
    {
        EventInfo_IIV eventInfo = (EventInfo_IIV)info;

        var victimId = eventInfo.int1;
        var attackerId = eventInfo.int2;

        VehicleController victim, attacker;

        if (!Units.TryGetValue(victimId, out victim) || !victim.IsAvailable)
            return;

        if (victimId == MyPlayerId)
        {
            lastOffender = attackerId;

            if (CheckPlayersCount())
            {
                ScoreCounter.DeathInto(victim);
            }
        }
        else if (attackerId == MyPlayerId)
        {
            if (!StaticContainer.Profile.BattleTutorialCompleted)
            {
                return;
            }

            attacker = CurrentUnit;

            int expAmount = BonusDispatcher.ExperienceBonusAmount(victim, attacker, true);

            ScoreCounter.ScoreInto(attacker, expAmount);

            if (victimId == lastOffender)
            {
                lastOffender = 0;
                Dispatcher.Send(EventId.RevengeDone, new EventInfo_I(victimId));
            }

            Dispatcher.Send(EventId.ExperienceAcquired, new EventInfo_I(expAmount));

            if (CheckPlayersCount())
            {
                ScoreCounter.KillInto(attacker);
            }

            //if (gameStat.ContainsKey(victimId))
            //    Notifier.ShowBonus("Kill", "bonus_kill", Localizer.GetText("lblBonus_kill", gameStat[victimId].playerName), 0, null);
        }
    }

    private void OnVehicleEffectChanges(EventId eventId, EventInfo eventInfo)
    {       

    }

    private void OnVehicleRespawned(EventId id, EventInfo info)
    {
        EventInfo_I eventInfo = (EventInfo_I)info;

        int playerId = eventInfo.int1;

        if (playerId == MyPlayerId)
        {
            ApplyAfterRespawnBonus();
        }
    }

    private void OnItemTaken(EventId id, EventInfo ei)
    {
    }

    private void OnMainTankAppeared(EventId id, EventInfo ei)
    {
        myCreationTime = PhotonNetwork.time;
        if (StaticContainer.Connector.FirstConnect)
            battleAccomplishedTime = myCreationTime + battleDuration;
        BattleAccomplished = false;
        ProfileInfo.WasInBattle = true;

        if (!StaticContainer.Connector.FirstConnect)
            return;

        if (CheckPlayersCount())
        {
            roomWasFulled = true;
            //BattleGUI.SetStatusText(null);
        }
        /*else
            BattleGUI.SetStatusText(Localizer.GetText("WaitingForPlayers"));*/

        SubtractHangarConsumables();
    }

    private void OnVehicleKilled(int victimId, int attackerId)
    {
        VehicleController victim, attacker;
        if (debug)
        {
            Debug.Log(string.Format("BattleController.OnMySideAttacker: victim '{0}', attacker: '{1}'", victimId, attackerId).RichString("color:green"), this);
        }

        if (!Units.TryGetValue(victimId, out victim) || !victim.IsAvailable || !Units.TryGetValue(attackerId, out attacker))
        {
            return;
        }

        victim.HPSystem.Death(attackerId);
        if (victim.PhotonView.isMine)
        {
            OnMySideVictim(victim, attacker);
        }

        if (attacker.PhotonView.isMine)
        {
            OnMySideAttacker(victim, attacker);
        }
    }

    private void OnVehicleKilledBOH(int victimId)
    {
        VehicleController victim;

        if (!Units.TryGetValue(victimId, out victim) || !victim.IsAvailable)
        {
            return;
        }

        victim.HPSystem.Death();

        if (victimId == MyPlayerId)
        {
            StartCoroutine(Timer());
        }
            //StatTable.Show(gameStat, myPlayerId, XD.TableState.AfterDeath);
    }

    private void SubtractHangarConsumables()
    {
        ProfileInfo.doubleExpVehicles.Remove(ProfileInfo.currentVehicle);
        ProfileInfo.vehicleUpgrades[ProfileInfo.currentVehicle].battlesCount++;
    }

    private void DisconnectAndShow()
    {
        StaticContainer.Connector.ForcedDisconnect(DisconnectAndShow_Continue);
    }

    private void DisconnectAndShow_Continue()
    {
        Dictionary<int, PlayerStat> statistics = new Dictionary<int, PlayerStat>();
        foreach (var stat in BattleStatisticsManager.OutOfTimeVehicles)
        {
            GameStat[stat.Key] = stat.Value;
        }

        StaticType.GameController.Instance<IGameController>().RequestUnitSelection(WindowShowCause.BattleEnd);
        
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
                    effect = new VehicleEffect(VehicleEffect.GetNewId(), VehicleEffect.ParameterType.Attack, ModifierType.Multiply, 1.5f, 30,
                        PhotonNetwork.time, BonusItem.BonusType.Attack, IECell.IEIcon.Bonus_Attack);
                    break;
                //case BonusItem.BonusType.RocketAttack:
                //    effect = new VehicleEffect(VehicleEffect.GetNewId(), VehicleEffect.ParameterType.RocketAttack, VehicleEffect.ModifierType.Product, 1.5f, 30,
                //        PhotonNetwork.time, BonusItem.BonusType.RocketAttack, IECell.IEIcon.Bonus_RocketAttack);
                //    break;
                case BonusItem.BonusType.Reload:
                    effect = new VehicleEffect(VehicleEffect.GetNewId(), VehicleEffect.ParameterType.RoF, ModifierType.Multiply, 2, 30,
                        PhotonNetwork.time, BonusItem.BonusType.Reload, IECell.IEIcon.Bonus_ROF);
                    break;
            }

        }

        afterRespawnBonuses.Clear();
    }

    private void AfterCountdown()
    {
        myVehicle.MakeRespawn(false, true, false);
    }
    
    private void CheckIfBattleAccomplished()
    {
        BattleAccomplished = PhotonNetwork.time >= battleAccomplishedTime - 0.1; // -0.1 - защита от всяческих погрешностей
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
                StartCoroutine(Timer());
            }
        }
    }

    private void OnMySideAttacker(VehicleController victim, VehicleController attacker)
    {
        if (debug)
        {
            Debug.Log(string.Format("BattleController.OnMySideAttacker: victim '{0}', attacker: '{1}'", victim.name, attacker.name).RichString("color:green"), attacker);
        }

        int expAmount = BonusDispatcher.ExperienceBonusAmount(victim, attacker, true);
        ScoreCounter.KillInto(attacker);
        ScoreCounter.ScoreInto(attacker, expAmount);

        if (!attacker.IsBot)
        {
            if (StaticContainer.Profile.BattleTutorialCompleted)
            {
                if (victim.data.playerId == lastOffender)
                {
                    lastOffender = 0;
                    Dispatcher.Send(EventId.RevengeDone, new EventInfo_I(victim.data.playerId));
                }
                //if (gameStat.ContainsKey(victim.data.playerId))
                //    Notifier.ShowBonus("Kill", "bonus_kill", Localizer.GetText("lblBonus_kill", victim.data.playerName), 0);
            }
            Dispatcher.Send(EventId.ExperienceAcquired, new EventInfo_I(expAmount));
        }
    }


    /// <summary>
    /// Добавление в список активированной способности для отсчета кулдауна. TODO: Пенести в свой диспетчер!
    /// </summary>
    /// <param name="cons"> Расходник. </param>
    public static void AddActiveAbility(IConsumable cons)
    {
        if (instance.activeAbilities.Contains((IConsumableBattle)cons))
        {
            return;
        }

        instance.activeAbilities.Add((IConsumableBattle)cons);
    }

    public static void RemoveActiveAbility(IConsumable cons)
    {
        instance.activeAbilities.Remove((IConsumableBattle)cons);
    }

    /// <summary>
    /// Пересчитывает кулдауны активных расходок.
    /// </summary>
    private void UpdateConsumables()
    {
        for (int i = 0; i < activeAbilities.Count; i++)
        {
            activeAbilities[i].TimersUpdate();
        }
    }

    private static void ExitToHangarAfterDisconnect()
    {
        Instance.CheckIfBattleAccomplished();
        Loading.GoToLoadingScene();
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
}