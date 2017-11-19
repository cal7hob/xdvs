using System;
using System.Collections;
using UnityEngine;
using System.Collections.Generic; 
using System.Linq;
using CodeStage.AntiCheat.ObscuredTypes;
using Http;
using Matchmaking;
using XD;
using Random = UnityEngine.Random;
using Hashtable = ExitGames.Client.Photon.Hashtable;

public class BotDispatcher : MonoBehaviour, ISubscriber
{
    public string Description
    {
        get
        {
            return "[BotDispatcher] " + name;
        }

        set
        {
            name = value;
        }
    }

    public void Reaction(Message message, params object[] parameters)
    {
        switch (message)
        {
            case Message.LayoutRequest:
                switch (parameters.Get<PSYWindow>())
                {
                    case PSYWindow.UnitSelection:
                        //CheckOnStart();
                        break;
                }
                break;
        }
    }

    public static Dictionary<BotBehaviours, int> botTypePriorities = new Dictionary<BotBehaviours, int>();
    public static int BotsCommonMask
    {
        get;
        private set;
    }

    private Dictionary<int, List<IUnitBattle>>          vehicleInfos = null;
    private const int                                   CHEATER_BOT_ROOM_LEVEL = 5;

    [SerializeField]
    private bool                                        debugTools = false;
    [SerializeField]
    private bool                                        enable = true;
    [SerializeField]
    private bool                                        botTest = false;
    [SerializeField]
    private int                                         botsTestQuantity = 0;
    [SerializeField]
    private BotBehaviours                               testBotBehavior = BotBehaviours.Target;

    public readonly Dictionary<int, BotAI>              botDict = new Dictionary<int, BotAI>();
    private readonly List<VehicleController>[]          teamBots = {new List<VehicleController>(), new List<VehicleController>()};
    private readonly Dictionary<int, VehicleController> allBots = new Dictionary<int, VehicleController>();
    private bool                                        waitingForBotsInProgress;
    private BotNames                                    tutorialBotNames;
    private bool                                        botGenerationDelayed;

    private const int DEFAULT_BOT_ID = 900;

    private Vector3 enemiesEpicenter;
    private Vector3 friendsEpicenter;

    public static Vector3 EnemiesEpicenter
    {
        get
        {
            return Instance.enemiesEpicenter;
        }
    }

    public static Vector3 FriendsEpicenter
    {
        get
        {
            return Instance.friendsEpicenter;
        }
    }

    public static Dictionary<Game, string> VehicleTypes = new Dictionary<Game, string>()
    {
        {Game.Armada2, "tank"},
    };

    public enum BotBehaviours
    {
        Target,
        Fighter,
        Agressor,
        TutorialBot
    }

    public static BotDispatcher Instance
    {
        get; private set;
    }

    private static int NextBotId
    {
        get
        {
            int maxPlayerId = SafeLinq.Max(StaticContainer.BattleController.UnitsData.Values.Select(v => v.playerId));
            return maxPlayerId < DEFAULT_BOT_ID ? DEFAULT_BOT_ID : maxPlayerId + 1;
        }
    }

    private void Awake()
    {
        //if ((GameData.IsGame(Game.SpaceJet) || GameData.IsGame(Game.BattleOfWarplanes)) && !ProfileInfo.IsBattleTutorial) //todo: убрать когда появятся нормальные боты
        //{
        //    Destroy(gameObject);
        //    return;
        //}

        Instance = this;
        LoadTankInfo();
        BotsCommonMask = MiscTools.GetLayerMask("Bot1", "Bot2");
    }

    private void Start()
    {
        var botSettings =
            Instantiate(
                Resources.Load(String.Format("{0}/{1}/{2}", XD.StaticContainer.GameManager.CurrentResourcesFolder, "Bots", "BotSettings")))
                as GameObject;
        botSettings.name = "BotSettings";

        Subscribes();

        StaticType.GameController.AddSubscriber(this);
    }

    private void Subscribes()
    {
        Dispatcher.Subscribe(EventId.MainTankAppeared, OnMainTankAppeared);
        Dispatcher.Subscribe(EventId.AfterLocalizationLoad, AfterLocalizationLoaded);
        Dispatcher.Subscribe(EventId.TankKilled, OnBotKilled);
        Dispatcher.Subscribe(EventId.TankJoinedBattle, OnNewVehConnected, 4);
        Dispatcher.Subscribe(EventId.TankLeftTheGame, OnVehLeftTheGame);
        Dispatcher.Subscribe(EventId.RoomBusyListTrimmed, OnRoomBusyListTrimmed);
        Dispatcher.Subscribe(EventId.NowImMaster, OnIamMaster);
        Dispatcher.Subscribe(EventId.BeforeReconnecting, OnReconnect);
        Dispatcher.Subscribe(EventId.NewPlayerConnected, OnNewPlayerConnected);
    }

#if UNITY_EDITOR
    private void Update()
    {
        TeamDebugTools();
    }
#endif

    private void OnDestroy()
    {
        Dispatcher.Unsubscribe(EventId.MainTankAppeared, OnMainTankAppeared);
        Dispatcher.Unsubscribe(EventId.AfterLocalizationLoad, AfterLocalizationLoaded);
        Dispatcher.Unsubscribe(EventId.TankKilled, OnBotKilled);
        Dispatcher.Unsubscribe(EventId.TankJoinedBattle, OnNewVehConnected);
        Dispatcher.Unsubscribe(EventId.TankLeftTheGame, OnVehLeftTheGame);
        Dispatcher.Unsubscribe(EventId.RoomBusyListTrimmed, OnRoomBusyListTrimmed);
        Dispatcher.Unsubscribe(EventId.NowImMaster, OnIamMaster);
        Dispatcher.Unsubscribe(EventId.BeforeReconnecting, OnReconnect);
        Dispatcher.Unsubscribe(EventId.NewPlayerConnected, OnNewPlayerConnected);        

        foreach (var botAi in botDict.Values)
        {
            BotUnsubscribes(botAi);
        }

        Instance = null;

        StaticType.GameController.RemoveSubscriber(this);
    }

    public TBotController CreateBot<TBotController>(BotBehaviours botBehaviour, bool randomEquipment = true) where TBotController : VehicleController
    {
        #region setting bot parameters

        int roomLevel = (int)PhotonNetwork.room.CustomProperties["lv"];
        switch (roomLevel)
        {
            case MatchMaker.SANDBOX_MATCHMAKING_GROUP:
                roomLevel = MatchMaker.SANDBOX_VEHICLE_GROUP;
                break;
            case MatchMaker.CHEATER_MATCHMAKING_GROUP:
                roomLevel = CHEATER_BOT_ROOM_LEVEL;
                break;
        }

        roomLevel = Mathf.Clamp(roomLevel, 1, SafeLinq.Max(vehicleInfos.Keys));

        List<IUnitBattle> groupVehs = null;
        if (!vehicleInfos.TryGetValue(roomLevel, out groupVehs))
        {
            Debug.LogError("НЕ УДАЛОСЬ Получить бота из списка! roomLevel: " + roomLevel);

            foreach (KeyValuePair<int, List<IUnitBattle>> pair in vehicleInfos)
            {
                Debug.LogErrorFormat("{0}: {1}", pair.Key, pair.Value == null ? "NULL" : pair.Value.Count.ToString());
            }
            return null;
        }

        if (groupVehs == null)
        {
            Debug.LogError("НЕ УДАЛОСЬ СОЗДАТЬ БОТА! roomLevel: " + roomLevel);

            foreach (KeyValuePair<int, List<IUnitBattle>> pair in vehicleInfos)
            {
                Debug.LogErrorFormat("{0}: {1}", pair.Key, pair.Value == null ? "NULL" : pair.Value.Count.ToString());
            }
            return null;
        }

        int ID = Random.Range(0, groupVehs.Count);
        IUnitBattle UnitBattle = StaticContainer.Profile.BattleTutorialCompleted ? groupVehs[ID] : StaticContainer.MainData.GetUnitBattle(1);
        
        // решили оставить одни id для имен техники ботов
        var botVehicleName = string.Format("{0}", groupVehs[ID].ID.ToString("D2"));

        var vehicleData = new TankData(
            playerName: "Bot",
            playerLevel: GetAveragePlayerLevel(),
            country: "xx",
            newbie: false,
            socialPlatform: SocialPlatform.Undefined,
            socialUID: String.Empty,
            parameters: UnitBattle.Settings.Clone(),
            patternId: randomEquipment ? /*Random.Range(0, PatternShop.Selectors.Count - 1)*/0 : 0,
            decalId: randomEquipment ? /*Random.Range(0, DecalShop.Selectors.Count - 1)*/0 : 0,
            teamId: 1,
            hideMyFlag: false,
            innerId: 0,
            vip: false,
            clanName: String.Empty,
            unitBattle: UnitBattle);

        vehicleData.playerId = NextBotId;
        vehicleData.innerId = -vehicleData.playerId;
        vehicleData.teamId = StaticContainer.Profile.BattleTutorialCompleted ? MatchMaker.GetTeamForNewPlayer(vehicleData) : 1;

        vehicleData.hideMyFlag = GameData.Mode == GameData.GameMode.Team; //Скрывать флаги у ботов в командном режиме
        BotName botName = StaticContainer.Profile.BattleTutorialCompleted ? Manager.Instance().battleServer.botNames.GetName(true) : tutorialBotNames.GetName(true);
        vehicleData.playerName = botName.Name;
        vehicleData.country = botName.Country;

        #endregion

        #region instantiating bot vehicle

        double kickAt = StaticContainer.Profile.BattleTutorialCompleted ? PhotonNetwork.time + GameData.botLifetime * Random.Range(0.9f, 1f) : PhotonNetwork.time + 100000f;
        string prefab = String.Format("{0}/{1}/{2}", StaticContainer.GameManager.CurrentResourcesFolder, "Bots", botVehicleName);
        GameObject botGO
            = PhotonNetwork.InstantiateSceneObject(
                prefabName: prefab,
                position: Vector3.right * 10000,
                rotation: Quaternion.identity,
                @group: 0,
                data: new object[] { vehicleData, botBehaviour, kickAt });

        if (botGO == null)
        {
            Debug.LogError("Bot: " + prefab + " is not found!", this);
            return null;
        }
        
        TBotController bot = botGO.GetComponent<TBotController>();
        bot.name = String.Format("{0}_{1}_{2} (bot)", VehicleTypes[GameData.CurrentGame], vehicleData.playerId, vehicleData.Team);
        bot.GetComponent<VehicleController>().UnitBattle = UnitBattle;
        bot.MakeRespawn(false, true, true);

        #endregion

        return bot;
    }

    #region прокачка бота
    private static void GiveModuleForBot(int maxUpgradeLevel, IUnitBattle botUserVehicle, Dictionary<XD.VehicleParameter, ObscuredFloat> botVehicleParams, XD.ModuleType moduleType, BotBehaviours botBehaviour)
    {
        if (!StaticContainer.Profile.BattleTutorialCompleted)
        {
            return;
        }

        var currentUpgradeLevel = 0;

        switch (botBehaviour)
        {
            case BotBehaviours.Target:
                return;
            //currentUpgradeLevel = (int) (maxUpgradeLevel*Random.Range(0.1f, 0.3f));
            //break;
            case BotBehaviours.Fighter:
                currentUpgradeLevel = (int)(maxUpgradeLevel * Random.Range(0.4f, 0.6f));
                break;
            case BotBehaviours.Agressor:
                currentUpgradeLevel = (int)(maxUpgradeLevel * Random.Range(0.6f, 1f));
                break;
        }

        botUserVehicle.ApplyModule(botVehicleParams, moduleType, currentUpgradeLevel, true);
    }

    private static void GiveModuleForBot(Dictionary<XD.VehicleParameter, ObscuredFloat> botVehicleParams, VehicleInfo botVehicleInfo, XD.ModuleType moduleType, BotBehaviours botBehaviour)
    {
        var maxUpgradeLevel = botVehicleInfo.GetMaxUpgradeLevel(moduleType) - 1;
        var currentUpgradeLevel = 0;
        List<VehicleInfo.ModuleUpgrade> upgradesList = new List<VehicleInfo.ModuleUpgrade>();
        List<XD.VehicleParameter> parameters = new List<XD.VehicleParameter>();

        switch (botBehaviour)
        {
            case BotBehaviours.Target:
                return;
            //currentUpgradeLevel += (int) (maxUpgradeLevel*Random.Range(0.1f, 0.3f));
            //break;
            case BotBehaviours.Fighter:
                currentUpgradeLevel += (int)(maxUpgradeLevel * Random.Range(0.4f, 0.6f));
                break;
            case BotBehaviours.Agressor:
                currentUpgradeLevel += (int)(maxUpgradeLevel * Random.Range(0.6f, 1));
                break;
        }

        switch (moduleType)
        {
            case XD.ModuleType.Armor:
                upgradesList = botVehicleInfo.armorUpgrades;
                parameters.Add(XD.VehicleParameter.Armor);
                break;
            case XD.ModuleType.Cannon:
                upgradesList = botVehicleInfo.cannonUpgrades;
                parameters.Add(XD.VehicleParameter.Damage);
                parameters.Add(XD.VehicleParameter.RocketDamage);
                break;
            case XD.ModuleType.Engine:
                upgradesList = botVehicleInfo.engineUpgrades;
                parameters.Add(XD.VehicleParameter.Speed);
                break;
            case XD.ModuleType.Reloader:
                upgradesList = botVehicleInfo.reloaderUpgrades;
                parameters.Add(XD.VehicleParameter.RoF);
                parameters.Add(XD.VehicleParameter.IRCMRoF);
                break;
            case XD.ModuleType.Tracks:
                upgradesList = botVehicleInfo.tracksUpgrades;
                parameters.Add(XD.VehicleParameter.Speed);
                break;
        }

        if (moduleType == XD.ModuleType.Armor)
        {
            currentUpgradeLevel = (int)(currentUpgradeLevel * 0.5f);
        }

        foreach (var vehicleParameter in parameters)
        {
            botVehicleParams[vehicleParameter] += upgradesList[currentUpgradeLevel].primaryGain;
        }
    }
    #endregion

    private IEnumerator FindTeamsEpicenters()
    {
        float delay = 5;

        while (true)
        {
            Vector3 enemiesPosSumVector = Vector3.zero;
            Vector3 friendsPosSumVector = Vector3.zero;
            int enemyPointsCount = 0;
            int friendsPointsCount = 0;

            if (StaticContainer.BattleController.Units.Count == 1)
            {
                var enemySpawnPoints = SpawnPoints.Points[StaticContainer.BattleController.CurrentUnit.data.teamId == 0 ? 1 : 0];
                var friendsSpawnPoints = SpawnPoints.Points[StaticContainer.BattleController.CurrentUnit.data.teamId == 0 ? 0 : 1];

                foreach (Transform spawnPoint in enemySpawnPoints)
                {
                    enemiesPosSumVector += spawnPoint.position;
                }

                foreach (Transform spawnPoint in friendsSpawnPoints)
                {
                    friendsPosSumVector += spawnPoint.position;
                }

                enemyPointsCount = enemySpawnPoints.Count;
                friendsPointsCount = friendsSpawnPoints.Count;
            }
            else
            {
                foreach (VehicleController vehicle in XD.StaticContainer.BattleController.Units.Values)
                {
                    if (!vehicle.IsAvailable)
                    {
                        continue;
                    }

                    if (vehicle.Transform == null)
                    {
                        continue;
                    }

                    if (vehicle.IsMainsFriend || vehicle.IsMine)
                    {
                        friendsPosSumVector += vehicle.transform.position;
                        friendsPointsCount++;
                        continue;
                    }

                    enemiesPosSumVector += vehicle.transform.position;
                    enemyPointsCount++;
                }
            }

            if (enemyPointsCount > 0)
            {
                enemiesEpicenter = enemiesPosSumVector / enemyPointsCount;
            }

            if (friendsPointsCount > 0)
            {
                friendsEpicenter = friendsPosSumVector / friendsPointsCount;
            }

            RaycastHit hit;
            Physics.Raycast(enemiesEpicenter + transform.up * 10, Vector3.down, out hit, BattleController.TerrainLayerMask);
            enemiesEpicenter.y = hit.point.y;
            Physics.Raycast(friendsEpicenter + transform.up * 10, Vector3.down, out hit, BattleController.TerrainLayerMask);
            friendsEpicenter.y = hit.point.y;

            yield return new WaitForSeconds(delay);
        }
    }

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.magenta;
        Gizmos.DrawWireSphere(enemiesEpicenter, 4);

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(friendsEpicenter, 4);
    }
#endif

    public static bool IsPlayerBot(int playerId)
    {
        return Instance != null && Instance.botDict.ContainsKey(playerId);
    }

    public void RegisterBotAI(BotAI ai)
    {
        if (ai != null)
            botDict.Add(ai.VehicleData.playerId, ai);
        else
            Debug.LogError("Trying to register NULL BotAI!");
    }

    public void RemoveBot(int botPlayerId)
    {
        if (!PhotonNetwork.isMasterClient)
        {
            Debug.LogError("Only master can add/remove bots");
            return;
        }

        VehicleController bot;
        if (!allBots.TryGetValue(botPlayerId, out bot))
            return;

        RemoveBot(bot);
    }

    public void RemoveBot(VehicleController bot)
    {
        if (!PhotonNetwork.isMasterClient)
        {
            Debug.LogError("Only master can add/remove bots");
            return;
        }

        if (!bot.IsBot)
        {
            Debug.LogError("Trying to remove player as a bot! Cancelled.");
            return;
        }

        //Заставляем комнату забыть бота
        Hashtable properties = new Hashtable
        {
            {bot.KeyForBotDamage, null},
            {bot.KeyForBotHealth, null},
            {bot.KeyForBotDeaths, null},
            {bot.KeyForBotKills, null},
            {bot.KeyForBotScore, null},
            {bot.KeyForBotExistance, null},
            {bot.KeyForBotSpeed, null},
            {bot.KeyForBotAttack, null},
            {bot.KeyForBotRoF, null},
            {bot.KeyForBotTurretSpeed, null}
        };

        BotUnsubscribes(bot.BotAI);
        PhotonNetwork.room.SetCustomProperties(properties);
        PhotonNetwork.Destroy(bot.gameObject);
    }

    public static void BotUnsubscribes(BotAI botAI)
    {
        Dispatcher.Unsubscribe(EventId.TankTakesDamage, botAI.CurrentBehaviour.OnVehicleTakesDamage);
        Dispatcher.Unsubscribe(EventId.TankKilled, botAI.CurrentBehaviour.OnVehicleKilled);
        Dispatcher.Unsubscribe(EventId.TankLeftTheGame, botAI.CurrentBehaviour.OnVehicleLeftTheGame);
        Dispatcher.Unsubscribe(EventId.BonusDestroyed, botAI.CurrentBehaviour.OnBonusDestroyed);
    }

    public static bool IsBotId(int playerId)
    {
        return playerId >= DEFAULT_BOT_ID;
    }

    public static int GetBotsLayerMaskForTeam(int teamId)
    {
        int mask = 0;
        // Не используется Instance.teamBots, потому что на момент запроса там еще неполная информация обо всех ботах.
        foreach (VehicleController vehicle in XD.StaticContainer.BattleController.Units.Values)
        {
            if (vehicle.IsBot && vehicle.data.teamId == teamId && vehicle.OwnLayer != 0)
                mask |= 1 << vehicle.OwnLayer;
        }

        return mask;
    }

    public static int GetNewBotLayer(int team)
    {
        return team == StaticContainer.GameManager.Team ? LayerMask.NameToLayer("Bot1") : LayerMask.NameToLayer("Bot2");
        
        //int maxBotLayer = LayerMask.NameToLayer("Bot9");
        //
        //for (int botLayer = minBotLayer; botLayer <= maxBotLayer; botLayer++)
        //{
        //    bool layerIsFree = true;
        //    foreach (VehicleController vehicle in XD.StaticContainer.BattleController.Units.Values)
        //    {
        //        if (vehicle.OwnLayer == botLayer)
        //        {
        //            layerIsFree = false;
        //            break;
        //        }
        //    }
        //    if (layerIsFree)
        //        return botLayer;
        //}
        //
        //Debug.LogWarning("Cannot get new bot layer (all is busy)");
        //return 0;
    }

    private void CheckOnStart()
    {
        if (!PhotonNetwork.isMasterClient)
        {
            return;
        }

        //if (!Localizer.Loaded)
        //{
        //    return;
        //}

        tutorialBotNames = BotNames.GetDummyNames();
        StartCoroutine(CheckIfBotsRequired(false));
        //StartCoroutine(BotEnterCoroutine());

        if (GameData.IsTeamMode)
        {
            StartCoroutine(FindTeamsEpicenters());
        }
    }

    private void OnMainTankAppeared(EventId id, EventInfo ei)
    {
        CheckOnStart();
    }

    private void AfterLocalizationLoaded(EventId id, EventInfo ei)
    {
        tutorialBotNames = BotNames.GetDummyNames();

        StartCoroutine(CheckIfBotsRequired(false));
        //StartCoroutine(BotEnterCoroutine());
    }

    private void OnRoomBusyListTrimmed(EventId id, EventInfo ei)
    {
        StartCoroutine(CheckIfBotsRequired(false));
    }

    private IEnumerator CheckIfBotsRequired(bool waitBeforeBotCreating)
    {
        if (!enable || waitingForBotsInProgress || !GameData.isBotsEnabled || !StaticContainer.Profile.BattleTutorialCompleted || !PhotonNetwork.isMasterClient)  // для тутора ботов спауним отдельно, на уроке уничтожения техники
        {
            yield break;
        }

        waitingForBotsInProgress = true;
        int totalVehicles = XD.StaticContainer.BattleController.Units.Count + (int)PhotonNetwork.room.CustomProperties["rp"];
        if (totalVehicles > GameData.maxPlayers)
        {
            RemoveUnnecessaryBots(totalVehicles - GameData.maxPlayers);
            waitingForBotsInProgress = false;
            yield break;
        }

        while (botGenerationDelayed)
        {
            yield return null;
        }

        int botsAlreadyInRoom = (int)PhotonNetwork.room.CustomProperties["bcn"];

        int botsQuantity = Mathf.Min(GameData.maxPlayers - totalVehicles, GameData.minBotCount - botsAlreadyInRoom);

        botsQuantity = botTest ? botsTestQuantity : botsQuantity;

        if (waitBeforeBotCreating)
        {
            yield return null;
        }

        
        for (int i = 0; i < botsQuantity; i++)
        {
            YieldInstruction wait = new WaitForSeconds(Random.Range(0.9f, 1f));
            yield return wait;
            CreateBotForCurrentProject();
        }
        RefreshBotCounter();
        waitingForBotsInProgress = false;
    }

    public IUnitBehaviour CreateBotForCurrentProject()
    {
        if (GameData.IsGame(Game.Armada2))
        {
            IUnitBehaviour unit = StaticContainer.BattleController.CurrentUnit;
            return CreateBot<TankBotControllerAR>(!botTest ? SelectRandomBotType() : testBotBehavior, unit == null ? false : !unit.Data.newbie);
        }

        return null;
    }

    private void RefreshBotCounter()
    {
        if (!PhotonNetwork.isMasterClient)
            return;

        Hashtable properties = new Hashtable { { "bcn", botDict.Count } };
        PhotonNetwork.room.SetCustomProperties(properties);
    }

    private void OnBotKilled(EventId id, EventInfo info)
    {
        if (ProfileInfo.TutorialIndex == (int)Tutorials.BattleTutorial)
        {
            return;
        }

        EventInfo_II eiII = (EventInfo_II)info;

        var attackerId = eiII.int2;
        var killedId = eiII.int1;

        if (botDict.ContainsKey(killedId))
        {
            StartCoroutine(RespawnBot(killedId));
        }
    }

    private static IEnumerator RespawnBot(int playerId)
    {
        yield return new WaitForSeconds(GameData.respawnTime);
        VehicleController bot;
        if (XD.StaticContainer.BattleController.Units.TryGetValue(playerId, out bot)
            && bot != null)
        {
            bot.MakeRespawn(false, true, false);
        }
    }

    private void OnNewVehConnected(EventId id, EventInfo ei)
    {
        EventInfo_I info = ei as EventInfo_I;
        if (info == null)
            return;

        VehicleController veh = XD.StaticContainer.BattleController.Units[info.int1];
        if (veh.IsBot)
        {
            teamBots[veh.data.teamId].Add(veh);
            allBots[veh.data.playerId] = veh;
        }

        if (!PhotonNetwork.isMasterClient)
            return;

        botGenerationDelayed = false;
        if (!veh.PhotonView.isMine) // Реагировать на вход нелокальных участников
            StartCoroutine(CheckIfBotsRequired(true));
    }

    private void OnVehLeftTheGame(EventId id, EventInfo ei)
    {
        EventInfo_I info = ei as EventInfo_I;
        int playerId = info.int1;
        VehicleController bot;
        if (allBots.TryGetValue(playerId, out bot))
            UnregisterBot(bot);
        //if (PhotonNetwork.isMasterClient)
        //{
        //    RefreshBotCounter();
        //    StartCoroutine(CheckIfBotsRequired(true));
        //}
    }

    private void RemoveUnnecessaryBots(int count)
    {
        for (int botNumber = 0; botNumber < count; botNumber++)
        {
            List<VehicleController> truncTeam = teamBots[0].Count >= teamBots[1].Count ? teamBots[0] : teamBots[1];
            if (truncTeam.Count == 0)
            {
                return;
            }

            VehicleController botToRemove = truncTeam[0];
            for (int i = 1; i < truncTeam.Count; i++)
            {
                if (truncTeam[i].Statistics.Stats[StatisticParameter.Experience] < botToRemove.Statistics.Stats[StatisticParameter.Experience])
                {
                    botToRemove = truncTeam[i];
                }
            }

            botToRemove.HPSystem.Death();
            RemoveBot(botToRemove);
        }
    }   

    private void UnregisterBot(VehicleController bot)
    {
        int botPlayerId = bot.data.playerId;
        botDict.Remove(botPlayerId);
        allBots.Remove(botPlayerId);
        teamBots[bot.data.teamId].Remove(bot);

        foreach (var botAI in botDict.Values)
        {
            BotUnsubscribes(botAI);
            botAI.CurrentBehaviour.BroAttackers.Remove(botPlayerId);
        }
    }

    private void OnIamMaster(EventId id, EventInfo ei)
    {
        EventInfo_B info = ei as EventInfo_B;
        bool iAmRoomCreator = info.bool1;
        if (iAmRoomCreator)
            return;

        //StartCoroutine(CheckIfBotsRequired(false));
        //StartCoroutine(BotEnterCoroutine());

        foreach (var botAI in botDict.Values)
        {
            StopCoroutine(botAI.LocalAvoidanceRoutine);
            StartCoroutine(botAI.LocalAvoidanceRoutine);
        }

        StartCoroutine(RespawnAllCauseMaster());
    }

    private void OnReconnect(EventId id, EventInfo ei)
    {
        StopAllCoroutines();
    }

    private void OnNewPlayerConnected(EventId id, EventInfo ei)
    {
        if (!PhotonNetwork.isMasterClient)
            return;

        if (XD.StaticContainer.BattleController.Units.Count == GameData.maxPlayers && botDict.Count > 0) // Есть, куда "впихнуть" всё-таки попавшего игрока
        {
            RemoveUnnecessaryBots(1);
            botGenerationDelayed = true;
        }
    }

    private IEnumerator BotEnterCoroutine()
    {
        yield return new WaitForFixedUpdate();
        if (botTest)
        {
            yield break;
        }

        if (!StaticContainer.Profile.BattleTutorialCompleted || !GameData.isBotsEnabled)
        {
            yield break;
        }

        float lastEnterTime = Time.time;
        float botEnterDelay = 0f;
        WaitForSeconds oneSecondWaiting = new WaitForSeconds(5f);
        //while (PhotonNetwork.isMasterClient)
        int totalVehicles = StaticContainer.BattleController.Units.Count +
                                   RoomInfoManager.GetPlaceReserve(PhotonNetwork.room);
        while (totalVehicles < GameData.maxPlayers)
        {
            if (GameData.isBotsEnabled)
            {
                while (waitingForBotsInProgress)
                {
                    yield return null;
                }

                totalVehicles = StaticContainer.BattleController.Units.Count +
                                   RoomInfoManager.GetPlaceReserve(PhotonNetwork.room);
                if (totalVehicles < GameData.maxPlayers)
                {
                    float lastEnterDeltaTime = Time.time - lastEnterTime;
                    int botsCount = allBots.Count;
                    if (lastEnterDeltaTime >= botEnterDelay && botsCount < GameData.maxBotCount)
                    {
                        CreateBotForCurrentProject();
                        RefreshBotCounter();
                        lastEnterTime = Time.time;
                        botEnterDelay = Random.Range(GameData.minBotEngageDelay, GameData.maxBotEngageDelay);
                    }
                }
            }
            yield return oneSecondWaiting;
        }
    }

    private void LoadTankInfo()
    {
        vehicleInfos = new Dictionary<int, List<IUnitBattle>>();

        IUnitBattle[] vehicles = StaticContainer.MainData.BattleUnits;

        foreach (var vehicle in vehicles)
        {
            if (vehicle.IsUnderdone || ((IUnitHangar)vehicle).IsPremium) // Не добавлять танки "в разработке" и скрытые.
            {
                continue;
            }

            int group = vehicle.VehicleGroup;

            if (!vehicleInfos.ContainsKey(group))
            {
                vehicleInfos.Add(group, new List<IUnitBattle>());
            }

            vehicleInfos[group].Add(vehicle);
        }
    }

    private int GetAveragePlayerLevel()
    {
        float sum = 0;
        int playersCount = 0;
        foreach (VehicleController vehicle in XD.StaticContainer.BattleController.Units.Values)
        {
            if (vehicle.IsBot)
            {
                continue;
            }

            sum += vehicle.data.playerLevel;
            playersCount++;
        }

        return Mathf.RoundToInt(sum / playersCount);
    }

    private IEnumerator RespawnAllCauseMaster()
    {
        yield return null;
        VehicleController[] unavailable = allBots.Values.Where(x => !x.IsAvailable).ToArray();
        foreach (var bot in unavailable)
        {
            if (!bot.IsAvailable)
                bot.MakeRespawn(false, true, false);
            yield return new WaitForFixedUpdate();
        }
    }

#if UNITY_EDITOR
    private void TeamDebugTools()
    {
        if (!debugTools)
        {
            return;
        }

        if (GameData.Mode != GameData.GameMode.Team)
        {
            return;
        }

        if (Input.GetKeyDown(KeyCode.Alpha3))
        {
            GameData.isBotsEnabled = !GameData.isBotsEnabled;
            Debug.LogFormat("Bots spawning {0}", GameData.isBotsEnabled ? "resumed" : "stopped");
        }

        if (Input.GetKeyDown(KeyCode.Alpha1) || Input.GetKeyDown(KeyCode.Alpha2))
        {
            int myTeamId = StaticContainer.BattleController.CurrentUnit.data.teamId;
            int teamId = Input.GetKeyDown(KeyCode.Alpha1) ? myTeamId : 1 - myTeamId;
            var botEnum = allBots.Values.Where(x => x.data.teamId == teamId);
            if (!botEnum.Any())
                return;

            VehicleController bot = botEnum.First();
            RemoveBot(bot);
            RefreshBotCounter();
        }

        /*        if (Input.GetKeyDown(KeyCode.Alpha8))
                    CreateBotForCurrentProject();*/
    }
#endif

    public static BotBehaviours SelectRandomBotType()
    {
        var botBehaviour = MiscTools.GetRandomFromSeveral(botTypePriorities.Values.ToArray(), botTypePriorities.Keys.ToArray())[0];

        return botBehaviour;
    }
}