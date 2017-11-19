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
using AssetBundles;

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
    public static Dictionary<int, TankData> vehicleData;
    public AudioClip bonusGetSound;
    public float outOfMapDistance = 2000;

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

    [SerializeField] private Map map;

    private LayerMask hitMask;
    private PhotonView photonView;
    private bool mayShowCriticalTime = true;
    private int lastOffender;
    private float timeInBattleUnity = 0;
    private float timeInBattlePhoton = 0;
    private float previousPhotonTime = -1;
    private double battleAccomplishedTime;
    private double myCreationTime = -1;
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

    public PhotonView PhotonView {
        get { return photonView; }
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
                roomCreationTime = (double)PhotonNetwork.room.CustomProperties["ct"] / 1000;

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

    public Map Map
    {
        get { return map; }
    }


    public enum BattleEvent // event #1 reserved by Dispatcher
    {
        BotSpawn = 2,
        BonusSpawn = 3,
        BattleItemSpawn = 4
    }

    /* UNITY SECTION */

    void Awake()
    {
        if (instance != null)
            return;

        instance = this;
        BattleMode = GameData.Mode;
        hitMask = MiscTools.GetLayerMask("Default", "Terrain", "Player", "Enemy", "Friend", "Bot1", "Bot2", "Bot3",
            "Bot4", "Bot5", "Bot6", "Bot7", "Bot8", "Bot9", "CritZone");
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

        if (!photonView)
        {
            DT.LogError(gameObject, "There is no photon view on Battle Controller. Disabled.");
            enabled = false;
        }

        InitAnother(); //// Инициализация других классов
        Subscriptions();

        PhotonNetwork.OnEventCall += OnPhotonEvent;

        if (GameData.IsGoldRushEnabled)
            GoldRush.ActivateForBattle();
        else
            GoldRush.Disactivate();
    }

    private void OnPhotonEvent(byte eventCode, object content, int senderId)
    {
        switch (eventCode)
        {
            case (byte)BattleEvent.BotSpawn:
                {
                    Hashtable ht = content as Hashtable;
                    int? botPhotonId = ht["botPhotonId"] as int?;
                    object[] netData = ht["data"] as object[];

                    string vehiclePrefabName = netData[0] as string;
                    Vector3 spawnPosition = Vector3.right * 10000;
                    Quaternion spawnRotation = Quaternion.identity;
                    int playerPhotonId = botPhotonId.Value;
                    TankData vehicleData = netData[1] as TankData;
                    BotDispatcher.BotBehaviours botBehaviour = (BotDispatcher.BotBehaviours)netData[2];
                    double kickAt = (double)netData[3];
                    int firstWeaponId = 0;
                    int mainVehicleId = (int)netData[4];

                    object[] data = new object[] { vehicleData, botBehaviour, kickAt, firstWeaponId, mainVehicleId };
                    string botName = string.Format("{0}_{1}_{2} (bot)", BotDispatcher.VehicleTypes[GameData.ClearGameFlags(GameData.CurrentGame)], vehicleData.playerId, botBehaviour);
                    StartCoroutine(BotSpawnCoroutine(vehiclePrefabName, spawnPosition, spawnRotation, playerPhotonId, data, botName));
                }
                break;

            case (byte)BattleEvent.BonusSpawn:
                {
                    Hashtable ht = content as Hashtable;
                    int bonusPhotonId = (int)ht["bonusPhotonId"];
                    object[] netData = ht["data"] as object[];

                    string prefabName = netData[0] as string;
                    Vector3 spawnPosition = (Vector3)netData[1];
                    bool isMapBonus = (bool)netData[2];
                    BonusItem.BonusInfo bonusInfo = (BonusItem.BonusInfo)netData[3];

                    BonusDispatcher.Instance.BonusSpawn(prefabName, spawnPosition, bonusPhotonId, isMapBonus, bonusInfo);
                }
                break;
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
        PhotonNetwork.OnEventCall -= OnPhotonEvent;
        GoldRush.Disactivate();
        ShellPoolManager.ClearAllPools();
        BattleConnectManager.ClearPhotonMessageTargets();
    }

    void OnApplicationPause(bool paused)
    {
        if (paused && (!GameData.AllowBackgroundConnection || (PhotonNetwork.BackgroundTimeout <= 0.1f)) ) {
            EndBattle (EndBattleCause.ApplicationPaused);
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
            Dispatcher.Send(EventId.TankKilled, new EventInfo_III(myPlayerId, myPlayerId, (int)GunShellInfo.ShellType.Usual));

            if (GameData.IsGame(Game.BattleOfHelicopters))
                Dispatcher.Send(
                    id: EventId.HelicopterKilled,
                    info: new EventInfo_IIV(
                                myPlayerId,
                                myPlayerId,
                                ((HelicopterController)myVehicle).LastPositionAlive),
                    target: Dispatcher.EventTargetType.ToAll);
        }

        if (Input.GetKeyDown(KeyCode.I))
        {
            int damage = myVehicle.data.attack;

            Dispatcher.Send(
                id:     EventId.TankTakesDamage,
                info:   new EventInfo_U(
                            /* victimId */      myPlayerId,
                            /* damage */        damage,
                            /* attackerId */    myPlayerId,
                            /* shellType */     (int)MyVehicle.DefaultShellType,
                            /* hits */          1,
                            /* hitPosition */   MyVehicle.transform.position));

            Dispatcher.Send(EventId.TankHealthChanged, new EventInfo_II(MyVehicle.data.playerId, MyVehicle.Armor));
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
                            (int)GunShellInfo.ShellType.Missile_SACLOS,
                            MyVehicle.transform.position));

            Dispatcher.Send(EventId.TankHealthChanged, new EventInfo_II(MyVehicle.data.playerId, MyVehicle.Armor));
        }*/

        if (Input.GetKeyDown(KeyCode.Keypad0) && MyVehicle)
            MyVehicle.Cheat();
        else if (Input.GetKeyDown(KeyCode.Keypad1) && MyVehicle)
            MyVehicle.ActivateSlowpoke();
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
            timeInBattlePhoton += DeltaTimePhoton;
        }
        else
            previousPhotonTime = (float)PhotonNetwork.time; // Борьба с неправильным вычислением DeltaTimePhoton

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
            CheckMyEffects();
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

        Dispatcher.Send(EventId.PhotonRoomCustomPropertiesChanged, new EventInfo_SimpleEvent());
    }

    [PunRPC]
    void VehicleSpawn(
        string vehiclePrefabName, Vector3 spawnPosition, Quaternion spawnRotation, int playerPhotonId, 
        TankData vehicleData, int firstWeaponId, int mainVehicleId)
    {
        object[] data = new object[] {vehicleData, firstWeaponId, mainVehicleId};
        StartCoroutine(VehicleSpawnCoroutine (vehiclePrefabName, spawnPosition, spawnRotation, playerPhotonId, data));
    }

    //[PunRPC]
    //void BotSpawn(
    //    string vehiclePrefabName, Vector3 spawnPosition, Quaternion spawnRotation, int playerPhotonId,
    //    TankData vehicleData, BotDispatcher.BotBehaviours botBehaviour, double kickAt, int firstWeaponId, int mainVehicleId)
    //{
    //    object[] data = new object[] { vehicleData, botBehaviour, kickAt, firstWeaponId, mainVehicleId };
    //    string botName = string.Format("{0}_{1}_{2} (bot)", BotDispatcher.VehicleTypes[GameData.ClearGameFlags(GameData.CurrentGame)], vehicleData.playerId, botBehaviour);
    //    StartCoroutine(BotSpawnCoroutine(vehiclePrefabName, spawnPosition, spawnRotation, playerPhotonId, data, botName));
    //}

    //[PunRPC]
    //void BonusSpawn (string prefabName, Vector3 spawnPosition, int photonViewId, bool isMapBonus, BonusItem.BonusInfo bonusInfo) {
    //    BonusDispatcher.Instance.BonusSpawn (prefabName, spawnPosition, photonViewId, isMapBonus, bonusInfo);
    //}

    [PunRPC]
    void BattleItemSpawn (string prefabName, Vector3 spawnPosition, Quaternion spawnRotation, int photonViewId, object[] data) {
        StartCoroutine(BattleItemSpawnCoroutine(prefabName, spawnPosition, spawnRotation, photonViewId, data));
    }

    IEnumerator VehicleSpawnCoroutine(string vehiclePrefabName, Vector3 spawnPosition, Quaternion spawnRotation, int playerPhotonId, object[] data)
    {
        string bundle = string.Format("{0}/vehicles", GameManager.CurrentResourcesFolder).ToLower();
        AssetBundleLoadAssetOperation request = AssetBundleManager.LoadAssetAsync(bundle, vehiclePrefabName, typeof(GameObject));
        if (request == null)
            yield break;
        yield return StartCoroutine(request);

        // Get the asset.
        GameObject obj = request.GetAsset<GameObject>();
        GameObject vehicle = Instantiate(obj, spawnPosition, spawnRotation) as GameObject;
        PhotonView view = vehicle.GetComponent<PhotonView>();
        view.viewID = playerPhotonId;

        while (!view.didAwake)
        {
            yield return null;
        }

        VehicleController vehicleController = vehicle.GetComponent<VehicleController>();
        vehicleController.name = "Vehicle_" + playerPhotonId;

        view.instantiationData = data;
        vehicleController.OnPhotonInstantiate(new PhotonMessageInfo(null, PhotonNetwork.ServerTimestamp, view));

        if (!BattleController.MyVehicle && view.isMine) {
            BattleController.SetMainVehicle(vehicleController);
            Dispatcher.Send(EventId.MainTankAppeared, new EventInfo_SimpleEvent());
        }
    }

    IEnumerator BotSpawnCoroutine(string vehiclePrefabName, Vector3 spawnPosition, Quaternion spawnRotation, int playerPhotonId, object[] data, string botName)
    {
        string bundle = string.Format("{0}/vehicles", GameManager.CurrentResourcesFolder).ToLower();
        AssetBundleLoadAssetOperation request = AssetBundleManager.LoadAssetAsync(bundle, vehiclePrefabName, typeof(GameObject));
        if (request == null)
            yield break;
        yield return StartCoroutine(request);

        // Get the asset.
        GameObject obj = request.GetAsset<GameObject>();
        GameObject vehicle = Instantiate(obj, spawnPosition, spawnRotation) as GameObject;
        PhotonView view = vehicle.GetComponent<PhotonView>();
        view.viewID = playerPhotonId;
        view.instantiationId = playerPhotonId;
        view.isRuntimeInstantiated = true;

        while (!view.didAwake)
        {
            yield return null;
        }

        VehicleController vehicleController = vehicle.GetComponent<VehicleController>();
        vehicleController.name = botName;

        view.instantiationData = data;
        vehicleController.OnPhotonInstantiate(new PhotonMessageInfo(null, PhotonNetwork.ServerTimestamp, view));

        vehicleController.MakeRespawn(false, true, true);
    }

    private IEnumerator BattleItemSpawnCoroutine(string prefabName, Vector3 spawnPosition, Quaternion spawnRotation, int photonViewId, object[] data)
    {
        string bundle = string.Format("{0}/vehicles", GameManager.CurrentResourcesFolder).ToLower();
        AssetBundleLoadAssetOperation request = AssetBundleManager.LoadAssetAsync(bundle, prefabName, typeof(GameObject));

        if (request == null)
            yield break;

        yield return StartCoroutine(request);

        // Get the asset.
        GameObject obj = request.GetAsset<GameObject>();
        GameObject vehicle = Instantiate(obj, spawnPosition, spawnRotation);

        PhotonView view = vehicle.GetComponent<PhotonView>();

        view.viewID = photonViewId;
        view.instantiationId = photonViewId;
        view.isRuntimeInstantiated = true;

        while (!view.didAwake)
            yield return null;

        BattleItem battleItem = vehicle.GetComponent<BattleItem>();

        view.instantiationData = data;

        battleItem.OnPhotonInstantiate(new PhotonMessageInfo(null, PhotonNetwork.ServerTimestamp, view));
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

        roomCreationTime = (double)PhotonNetwork.room.CustomProperties["ct"];
    }

    public static void ExitToHangar(bool normalExit)
    {
        BattleStatisticsManager.SetOtherBattleStats();

        Dispatcher.Send(EventId.OnExitToHangar, new EventInfo_SimpleEvent());

        instance.NormalDisconnect = normalExit;

        if (BattleConnectManager.IsMasterClient && normalExit)
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

        if (cause == EndBattleCause.Timeouted)
        {
            BattleAccomplished = true;
            Dispatcher.Send(EventId.TankOutOfTime, new EventInfo_I(myPlayerId), Dispatcher.EventTargetType.ToAll);
        }
        else if (cause == EndBattleCause.Inactivity)
        {
            StatTable.MyVehicleRank = 101; // за неактивность
            Dispatcher.Send(EventId.PlayerKickout, new EventInfo_I(myPlayerId), Dispatcher.EventTargetType.ToAll);
        }

        Instance.isBattleFinished = true;
        Dispatcher.Send(EventId.BattleEnd, new EventInfo_I((int)cause));
        BattleStatisticsManager.SetOtherBattleStats();
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
                    string settings = (string)property.Value;
                    if (!string.IsNullOrEmpty(settings))
                    {
                        var ei = settings.Split(',');
                        var playerName = ei[0];
                        var hideHisFlag = ei[1].Trim() == "True";

                        allVehicles[playerId].data.hideMyFlag = hideHisFlag;
                        allVehicles[playerId].data.playerName = playerName;
                        allVehicles[playerId].Statistics.playerName = playerName;

                        Dispatcher.Send(EventId.FlagSettingsChanged, new EventInfo_I(playerId));
                    }

                    if (StatTable.OnScreen)
                        StatTable.Refresh(gameStat, myPlayerId);
                    break;
                case "rg": //Regeneration
                    if (vehicleController.PhotonView.isMine)
                        vehicleController.Regeneration = (int) property.Value;
                    break;
                case "sh": //Shield
                    vehicleController.Shield = (int) property.Value;
                    break;
                case "tdr": //Taken damage ratio
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

            var battleTutorialPath = GameManager.CurrentResourcesFolder + "/GuiPrefabs/Battle/Tutorials/BattleTutorial";

            var battleTutorial = Instantiate(Resources.Load<BattleTutorial>(battleTutorialPath));

            battleTutorial.transform.SetParent(BattleGUI.Instance.transform, false);

            tutorialsController.name = "TutorialsController";
            battleTutorial.name = "BattleTutorial";
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
        Dispatcher.Subscribe(EventId.DeathAnimationDone, OnDeathAnimationDone);
        Dispatcher.Subscribe(EventId.HelicopterKilled, OnHelicopterKilled);
        Dispatcher.Subscribe(EventId.TankRespawned, OnVehicleRespawned, 1);
        Dispatcher.Subscribe(EventId.TankEffectRequest, OnVehicleEffectRequest);
        Dispatcher.Subscribe(EventId.TankEffectApply, OnVehicleEffectChanges);
        Dispatcher.Subscribe(EventId.TankEffectCancelled, OnVehicleEffectChanges);
        Dispatcher.Subscribe(EventId.MainTankAppeared, OnMainTankAppeared);
        Dispatcher.Subscribe(EventId.ItemTaken, OnItemTaken, 2);
        Dispatcher.Subscribe(EventId.GoldAcquired, OnGoldAcquired);
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
        Dispatcher.Unsubscribe(EventId.DeathAnimationDone, OnDeathAnimationDone);
        Dispatcher.Unsubscribe(EventId.HelicopterKilled, OnHelicopterKilled);
        Dispatcher.Unsubscribe(EventId.TankRespawned, OnVehicleRespawned);
        Dispatcher.Unsubscribe(EventId.MainTankAppeared, OnMainTankAppeared);
        Dispatcher.Unsubscribe(EventId.ItemTaken, OnItemTaken);
        Dispatcher.Unsubscribe(EventId.ShellEffect, OnShellEffect);
        Dispatcher.Unsubscribe(EventId.GoldAcquired, OnGoldAcquired);
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
        Manager.BattleServer.EndBattle(BattleConnectManager.FAILURE_DISCONNECT_CAUSE, (bool isRequestFinished) => { ExitToHangar(false); });
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

        if (PhotonNetwork.connected && StatTable.OnScreen)
            StatTable.Refresh(gameStat, myPlayerId);
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
            return;

        myVehicle.ApplyEffect(info.effect);
    }

    private void OnGoldAcquired(EventId id, EventInfo info)
    {
        if (!TopPanelValues.CriticalTime)
            mayShowCriticalTime = ProfileInfo.CanBuy(new ProfileInfo.Price(currentProlongPrice, ProfileInfo.PriceCurrency.Gold));
    }

    private void OnVehicleKilled(EventId id, EventInfo info)
    {
        EventInfo_III eventInfo = (EventInfo_III)info;

        var victimId = eventInfo.int1;
        var attackerId = eventInfo.int2;

        if (GameData.IsGame(Game.BattleOfHelicopters))
            OnVehicleKilledBOH(victimId);
        else
            OnVehicleKilled(victimId, attackerId);
    }

    private void OnDeathAnimationDone (EventId id, EventInfo info) 
    {
        StatTable.Show (gameStat, myPlayerId, StatTable.TableState.AfterDeath);
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
                Dispatcher.Send(EventId.RevengeDone, new EventInfo_I(victimId));
            }

            Dispatcher.Send(EventId.ExperienceAcquired, new EventInfo_I(expAmount));

            if (!CheckPlayersCount() || attacker == victim)
                return;

            ScoreCounter.KillInto(attacker);
            if (gameStat.ContainsKey(victimId))
                Notifier.ShowBonus("Kill","bonus_kill", Localizer.GetText("lblBonus_kill", gameStat[victimId].playerName), 0, null);
        }
    }

    private void OnVehicleEffectChanges(EventId eventId, EventInfo eventInfo)
    {
        if (eventId == EventId.TankEffectApply)
        {
            EventInfo_IE info = (EventInfo_IE)eventInfo;

            int playerId = info.int1;

            VehicleController veh;
            if (!allVehicles.TryGetValue(playerId, out veh))
                return;

            if (veh.FixateEffect(info.effect) && playerId == myPlayerId)
                IEPanel.Instance.AddCell(info.effect.UI_Id, info.effect.IconName);
        }
        else if (eventId == EventId.TankEffectCancelled)
        {
            EventInfo_II info = (EventInfo_II)eventInfo;

            int playerId = info.int1;
            int effectId = info.int2;

            VehicleController veh;
            if (!allVehicles.TryGetValue(playerId, out veh))
                return;

            if (playerId == myPlayerId)
            {
                IEPanel.Instance.RemoveCell(myVehicle.Effects[effectId].UI_Id);
            }
            veh.TakeEffectAway(effectId);
        }
    }

    private void OnVehicleEffectRequest(EventId eventId, EventInfo ei)
    {
        EventInfo_IE info = (EventInfo_IE) ei;
        VehicleEffect effect = info.effect;
        VehicleController vehicle;
        if (!allVehicles.TryGetValue(info.int1, out vehicle) || !vehicle.PhotonView.isMine)
            return;
        
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

        allVehicles[info.int3].TakeBonus((BonusItem.BonusType)info.int1, info.int2);
    }

    private void OnMainTankAppeared(EventId id, EventInfo ei)
    {
        myCreationTime = PhotonNetwork.time;
        if (BattleConnectManager.Instance.FirstConnect)
            battleAccomplishedTime = myCreationTime + battleDuration;
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

        ShellPoolManager.PreloadShells();
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
        //ProfileInfo.doubleExpVehicles.Remove(ProfileInfo.currentVehicle);
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
                    effect = new VehicleEffect(-1, VehicleEffect.ParameterType.RoF, VehicleEffect.ModifierType.Product, 2, 30,
                        PhotonNetwork.time, BonusItem.BonusType.Reload, IECell.IEIcon.Bonus_ROF, -1);
                    break;
            }

            myVehicle.ApplyEffect(effect);
        }

        afterRespawnBonuses.Clear();
    }

    private void AfterCountdown()
    {
        myVehicle.MakeRespawn(false, true, false);
    }

    private void CheckMyEffects()
    {
        foreach (VehicleEffect effect in myVehicle.Effects.Values)
        {
            if (string.IsNullOrEmpty(effect.IconName))
                continue;

            IEPanel.Instance.GetCell(effect.UI_Id).Timer = Mathf.CeilToInt(effect.Remain);
        }
    }

    private void CheckIfBattleAccomplished()
    {
        BattleAccomplished = BattleController.MyVehicle!= null && PhotonNetwork.time >= battleAccomplishedTime - 0.1; // -0.1 - защита от всяческих погрешностей
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
            if (!ProfileInfo.IsBattleTutorial)
            {
                if (victim.data.playerId == lastOffender)
                {
                    lastOffender = 0;
                    Dispatcher.Send(EventId.RevengeDone, new EventInfo_I(victim.data.playerId));
                }
                if (gameStat.ContainsKey(victim.data.playerId))
                    Notifier.ShowBonus("Kill","bonus_kill", Localizer.GetText("lblBonus_kill", victim.data.playerName), 0);
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