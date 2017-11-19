using System;
using System.Collections;
using UnityEngine;
using System.Collections.Generic; 
using System.Linq;
using CodeStage.AntiCheat.ObscuredTypes;
using Http;
using Matchmaking;
using Random = UnityEngine.Random;
using Hashtable = ExitGames.Client.Photon.Hashtable;
//
public class BotDispatcher : MonoBehaviour
{
    [SerializeField] private bool drawBotPaths = false;
    [SerializeField] private bool pathsForSelected = false;
    [SerializeField] private bool showInnerStates = false;

    public static Dictionary<BotBehaviours, int> botTypePriorities = new Dictionary<BotBehaviours, int>();

    public static int BotsCommonMask
    {
        get
        {
            return botsCommonMask != 0 ?
                botsCommonMask :
                botsCommonMask = MiscTools.GetLayerMask("Bot1", "Bot2", "Bot3", "Bot4", "Bot5", "Bot6", "Bot7", "Bot8", "Bot9");
        }
    }
    Dictionary<int, List<VehicleInfo>> vehicleInfos;
    private const int CHEATER_BOT_ROOM_LEVEL = 5;

    public readonly Dictionary<int, BotAI> botDict = new Dictionary<int, BotAI>();
    private readonly List<VehicleController>[] teamBots = {new List<VehicleController>(), new List<VehicleController>()};
    private readonly Dictionary<int, VehicleController> allBots = new Dictionary<int, VehicleController>();
    private bool waitingForBotsInProgress;
    private BotNames tutorialBotNames;
    private bool botGenerationDelayed;
    private static int botsCommonMask = 0;

    private const int DEFAULT_BOT_ID = 900;

    private Vector3 enemiesEpicenter;
    private Vector3 friendsEpicenter;

    public static Vector3 EnemiesEpicenter { get { return Instance.enemiesEpicenter; } }
    public static Vector3 FriendsEpicenter { get { return Instance.friendsEpicenter; } }
    public static bool DrawBotPaths { get { return Instance.drawBotPaths; } }
    public static bool PathsForSelected { get { return Instance.pathsForSelected; } }
    public static bool ShowInnerStates { get { return Instance.showInnerStates; } }
    public static bool BotsSpawnInProgress { get; private set; }

    public static Dictionary<Game, string> VehicleTypes = new Dictionary<Game, string>()
    {
        {Game.BattleOfHelicopters, "Helicopter"},
        {Game.IronTanks, "tank"},
        {Game.Armada, "tank"},
        {Game.ToonWars, "tank"},
        {Game.ApocalypticCars, "AC_car"},
        {Game.BattleOfWarplanes, "aircraft"},
        {Game.FutureTanks, "SFTank"},
        {Game.SpaceJet, "ship"},
        {Game.WWR, "robot"},//TODO: Заменить на как придумаете на что
        {Game.FTRobotsInvasion, "bot"},
    };
    
    public enum BotBehaviours
    {
        Target,
        Fighter,
        Agressor,
        Tutorial
    }

    public static BotDispatcher Instance { get; private set; }

    private static int NextBotId
    {
        get
        {
            int maxPlayerId = SafeLinq.Max(BattleController.allVehicles.Values.Select(v => v.data.playerId));
            return maxPlayerId < DEFAULT_BOT_ID ? DEFAULT_BOT_ID : maxPlayerId + 1;
        }
    }

    void Awake()
    {
        Instance = this;
        LoadTankInfo();
    }

    void Start()
    {
        var botSettings
            = Instantiate(
                Resources.Load<GameObject>(string.Format("{0}/Bots/BotSettings", GameManager.CurrentResourcesFolder)));

        botSettings.name = "BotSettings";

        Subscribes();
    }

    private void Subscribes()
    {
        Messenger.Subscribe(EventId.MainTankAppeared, OnMainTankAppeared);
        Messenger.Subscribe(EventId.AfterLocalizationLoad, AfterLocalizationLoaded);
        Messenger.Subscribe(EventId.VehicleKilled, OnBotKilled);
        Messenger.Subscribe(EventId.TankJoinedBattle, OnNewVehConnected, 4);
        Messenger.Subscribe(EventId.TankLeftTheGame, OnVehLeftTheGame);
        Messenger.Subscribe(EventId.RoomBusyListTrimmed, OnRoomBusyListTrimmed);
        Messenger.Subscribe(EventId.NowImMaster, OnIamMaster);
        Messenger.Subscribe(EventId.BeforeReconnecting, OnReconnect);
        Messenger.Subscribe(EventId.NewPlayerConnected, OnNewPlayerConnected);
    }

    void OnDestroy()
    {
        Messenger.Unsubscribe(EventId.MainTankAppeared, OnMainTankAppeared);
        Messenger.Unsubscribe(EventId.AfterLocalizationLoad, AfterLocalizationLoaded);
        Messenger.Unsubscribe(EventId.VehicleKilled, OnBotKilled);
        Messenger.Unsubscribe(EventId.TankJoinedBattle, OnNewVehConnected);
        Messenger.Unsubscribe(EventId.TankLeftTheGame, OnVehLeftTheGame);
        Messenger.Unsubscribe(EventId.RoomBusyListTrimmed, OnRoomBusyListTrimmed);
        Messenger.Unsubscribe(EventId.NowImMaster, OnIamMaster);
        Messenger.Unsubscribe(EventId.BeforeReconnecting, OnReconnect);
        Messenger.Unsubscribe(EventId.NewPlayerConnected, OnNewPlayerConnected);

        foreach (var botAi in botDict.Values)
        {
            BotUnsubscribes(botAi);
        }

        Instance = null;
        BotsSpawnInProgress = false;
        StopAllCoroutines();
    }

    public VehicleController CreateBot(BotBehaviours botBehaviour, bool randomEquipment = true)
    {
        #region setting bot parameters

        int roomLevel = (int) PhotonNetwork.room.CustomProperties["lv"];
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
        List<VehicleInfo> groupVehs;
        if (!vehicleInfos.TryGetValue(roomLevel, out groupVehs))
        {
            Debug.LogError("No bots for room level ({0})");
            return null;
        }

        VehicleInfo botVehicleInfo = ProfileInfo.IsBattleTutorial
            ? VehiclePool.Instance.GetItemById(1)
            : groupVehs.GetRandomItem();

        //var botVehicleName = string.Format("{0}_{1}_{2}", botVehicleInfo.id.ToString("D2"), VehicleTypes[GameData.CurrentGame],
        //    botVehicleInfo.vehicleName); 

        // решили оставить одни id для имен техники ботов

        var botVehicleName = string.Format("{0}", botVehicleInfo.id.ToString("D2")); 

        var botUserVehicle = new UserVehicle(botVehicleInfo);

        var botVehicleParams = new Dictionary<VehicleInfo.VehicleParameter, ObscuredFloat>
        {
                {VehicleInfo.VehicleParameter.Armor, botVehicleInfo.baseArmor},
                {VehicleInfo.VehicleParameter.Damage, botVehicleInfo.baseDamage},
                {VehicleInfo.VehicleParameter.Speed, botVehicleInfo.baseSpeed},
                {VehicleInfo.VehicleParameter.RoF, botVehicleInfo.baseRoF},
                {VehicleInfo.VehicleParameter.Magazine, botVehicleInfo.baseMagazine}
            };

        //GiveModuleForBot(botVehicleParams, botVehicleInfo, TankModuleInfos.ModuleType.Armor, botBehaviour);
        //GiveModuleForBot(botVehicleParams, botVehicleInfo, TankModuleInfos.ModuleType.Reloader, botBehaviour);
        //GiveModuleForBot(botVehicleParams, botVehicleInfo, TankModuleInfos.ModuleType.Cannon, botBehaviour);
        //GiveModuleForBot(botVehicleParams, botVehicleInfo, TankModuleInfos.ModuleType.Tracks, botBehaviour);
        //GiveModuleForBot(botVehicleParams, botVehicleInfo, TankModuleInfos.ModuleType.Engine, botBehaviour);

        GiveModuleForBot(botVehicleInfo, botUserVehicle, botVehicleParams, TankModuleInfos.ModuleType.Cannon, botBehaviour);
        GiveModuleForBot(botVehicleInfo, botUserVehicle, botVehicleParams, TankModuleInfos.ModuleType.Tracks, botBehaviour);
        GiveModuleForBot(botVehicleInfo, botUserVehicle, botVehicleParams, TankModuleInfos.ModuleType.Reloader, botBehaviour);
        GiveModuleForBot(botVehicleInfo, botUserVehicle, botVehicleParams, TankModuleInfos.ModuleType.Engine, botBehaviour);
        GiveModuleForBot(botVehicleInfo, botUserVehicle, botVehicleParams, TankModuleInfos.ModuleType.Armor, botBehaviour);

        var vehicleData
            = new VehicleData(
                playerName:     "Bot",
                playerLevel:    GetAveragePlayerLevel(),
                country:        "xx",
                newbie:         false,
                socialPlatform: SocialPlatform.Undefined,
                socialUID:      string.Empty,
                hangarParameters:     botVehicleParams,
                patternId:      SelectRandomId(BotSettings.botCamoulageIds_s),
                decalId:        SelectRandomId(BotSettings.botDecalIds_s),
                teamId:         0,
                hideMyFlag:     false,
                profileId:      0,
                vip:            false,
                clanName:       string.Empty,
                regeneration: 0,
                takenDamageRatio: 1
                ) { playerId = NextBotId};

        vehicleData.profileId = -vehicleData.playerId;
        vehicleData.teamId = MatchMaker.GetTeamForNewPlayer(vehicleData);

        vehicleData.hideMyFlag = GameData.Mode == GameData.GameMode.Team; //Скрывать флаги у ботов в командном режиме
        BotName botName = ProfileInfo.IsBattleTutorial
            ? tutorialBotNames.GetName(true)
            : Manager.BattleServer.botNames.GetName(true);
        vehicleData.playerName = botName.Name;
        vehicleData.country = botName.Country;

        #endregion

        #region instantiating bot vehicle

        double kickAt = ProfileInfo.IsBattleTutorial
            ? PhotonNetwork.time + 100000f
            : PhotonNetwork.time + GameData.botLifetime * Random.Range(0.9f, 1f);

        VehicleController bot
            = PhotonNetwork.InstantiateSceneObject(
                prefabName: string.Format("{0}/Bots/{1}", GameManager.CurrentResourcesFolder, botVehicleName),
                position: Vector3.right * 10000,
                rotation: Quaternion.identity,
                group: 0,
                data: new object[] {vehicleData, botBehaviour, kickAt})
                .GetComponent<VehicleController>();

        bot.name =
            string.Format("{0}_{1}_{2} (bot)", VehicleTypes[GameData.ClearGameFlags(GameData.CurrentGame)],
                vehicleData.playerId, botBehaviour);

        bot.MakeRespawn(false, true, true);
        //TankIndicators.GetIndicator(vehicleData.playerId).Hidden = false;

        #endregion

        return bot;
    }

    #region прокачка бота
    private static void GiveModuleForBot(VehicleInfo botVehicleInfo, UserVehicle botUserVehicle, Dictionary<VehicleInfo.VehicleParameter, ObscuredFloat> botVehicleParams, TankModuleInfos.ModuleType moduleType, BotBehaviours botBehaviour)
    {
        if (ProfileInfo.IsBattleTutorial)
        {
            return;
        }

        int maxUpgradeLevel = botVehicleInfo.GetMaxUpgradeLevel(moduleType);
        int currentUpgradeLevel = 0;

        switch (botBehaviour)
        {
            case BotBehaviours.Target:
                currentUpgradeLevel = (int) (maxUpgradeLevel * Random.Range(0.1f, 0.3f));
                break;
            case BotBehaviours.Fighter:
                currentUpgradeLevel = (int) (maxUpgradeLevel * Random.Range(0.3f, 0.6f));
                break;
            case BotBehaviours.Agressor:
                currentUpgradeLevel = (int) (maxUpgradeLevel * Random.Range(0.5f, 0.7f));
                break;
        }

        botUserVehicle.ApplyModule(botVehicleParams, moduleType, currentUpgradeLevel, true);
    }

    private static void GiveModuleForBot(Dictionary<VehicleInfo.VehicleParameter, ObscuredFloat> botVehicleParams, VehicleInfo botVehicleInfo, TankModuleInfos.ModuleType moduleType, BotBehaviours botBehaviour)
    {
        var maxUpgradeLevel = botVehicleInfo.GetMaxUpgradeLevel(moduleType) - 1;
        var currentUpgradeLevel = 0;
        List<VehicleInfo.ModuleUpgrade> upgradesList = new List<VehicleInfo.ModuleUpgrade>();
        List<VehicleInfo.VehicleParameter> parameters = new List<VehicleInfo.VehicleParameter>();

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
            case TankModuleInfos.ModuleType.Armor:
                upgradesList = botVehicleInfo.armorUpgrades;
                parameters.Add(VehicleInfo.VehicleParameter.Armor);
                break;
            case TankModuleInfos.ModuleType.Cannon:
                upgradesList = botVehicleInfo.cannonUpgrades;
                parameters.Add(VehicleInfo.VehicleParameter.Damage);
                break;
            case TankModuleInfos.ModuleType.Engine:
                upgradesList = botVehicleInfo.engineUpgrades;
                parameters.Add(VehicleInfo.VehicleParameter.Speed);
                break;
            case TankModuleInfos.ModuleType.Reloader:
                upgradesList = botVehicleInfo.reloaderUpgrades;
                parameters.Add(VehicleInfo.VehicleParameter.RoF);
                break;
            case TankModuleInfos.ModuleType.Tracks:
                upgradesList = botVehicleInfo.tracksUpgrades;
                parameters.Add(VehicleInfo.VehicleParameter.Speed);
                break;
        }

        if (moduleType == TankModuleInfos.ModuleType.Armor)
        {
            currentUpgradeLevel = (int) (currentUpgradeLevel*0.5f);
        }

        foreach (var vehicleParameter in parameters)
        {
            botVehicleParams[vehicleParameter] += upgradesList[currentUpgradeLevel].primaryGain;
        }
    }

    private static int SelectRandomId(int[] ids)
    {
        return ids.GetRandomItemOrDefault();
    }

    #endregion

    private IEnumerator FindTeamsEpicenters()
    {
        WaitForSeconds delay = new WaitForSeconds(5f);

        while (BattleController.MyVehicle != null)
        {
            var enemiesPosSumVector = Vector3.zero;
            var friendsPosSumVector = Vector3.zero;
            var enemyPointsCount = 0;
            var friendsPointsCount = 0;

            if (BattleController.allVehicles.Count == 1)
            {
                var enemySpawnPoints = SpawnPoints.Points[BattleController.MyVehicle.data.teamId == 0 ? 1 : 0];
                var friendsSpawnPoints = SpawnPoints.Points[BattleController.MyVehicle.data.teamId == 0 ? 0 : 1];

                foreach (var spawnPoint in enemySpawnPoints)
                {
                    enemiesPosSumVector += spawnPoint.position;
                }

                foreach (var spawnPoint in friendsSpawnPoints)
                {
                    friendsPosSumVector += spawnPoint.position;
                }

                enemyPointsCount = enemySpawnPoints.Count;
                friendsPointsCount = friendsSpawnPoints.Count;
            }
            else
            {
                foreach (var vehicle in BattleController.allVehicles.Values)
                {
                    if (!vehicle.IsAvailable)
                    {
                        continue;
                    }

                    if (vehicle.IsMainsFriend || vehicle.IsMain)
                    {
                        friendsPosSumVector += vehicle.transform.position;
                        friendsPointsCount++;
                        continue;
                    }

                    enemiesPosSumVector += vehicle.transform.position;
                    enemyPointsCount++;
                }
            }
            
            enemiesEpicenter = enemiesPosSumVector / enemyPointsCount;
            friendsEpicenter = friendsPosSumVector / friendsPointsCount;
            
            RaycastHit hit;
            Physics.Raycast(enemiesEpicenter + transform.up * 10, Vector3.down, out hit, BattleController.TerrainLayerMask);
            enemiesEpicenter.y = hit.point.y;
            Physics.Raycast(friendsEpicenter + transform.up * 10, Vector3.down, out hit, BattleController.TerrainLayerMask);
            friendsEpicenter.y = hit.point.y;

            yield return delay;
        }
    }

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

    public void RemoveBot(VehicleController bot, bool withGenerationDelay = false)
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

        if (withGenerationDelay)
            botGenerationDelayed = true;

        //Заставляем комнату забыть бота
        Hashtable properties = new Hashtable
        {
            {bot.KeyForHealth, null},
            {bot.KeyForDeaths, null},
            {bot.KeyForKills, null},
            {bot.KeyForScore, null},
            {bot.KeyForExistance, null}
        };

        BotUnsubscribes(bot.BotAI);
        PhotonNetwork.room.SetCustomProperties(properties);
        PhotonNetwork.Destroy(bot.gameObject);
    }
    
    public static void BotUnsubscribes(BotAI botAI)
    {
        Messenger.Unsubscribe(EventId.VehicleTakesDamage, botAI.OnBotBehaviourTakesDamage);
        Messenger.Unsubscribe(EventId.VehicleKilled, botAI.OnBotBehaviourVehicleKilled);
        Messenger.Unsubscribe(EventId.TankLeftTheGame, botAI.OnBotBehaviourVehicleLeft);
        Messenger.Unsubscribe(EventId.BonusDestroyed, botAI.OnBotBehaviourBonusDestroyed);
    }

    public static bool IsBotId(int playerId)
    {
        return playerId >= DEFAULT_BOT_ID;
    }

    public static int GetBotsLayerMaskForTeam(int teamId)
    {
        int mask = 0;
        // Не используется Instance.teamBots, потому что на момент запроса там еще неполная информация обо всех ботах.
        foreach (VehicleController vehicle in BattleController.allVehicles.Values)
        {
            if (vehicle.IsBot && vehicle.data.teamId == teamId && vehicle.OwnLayer != 0)
                mask |= 1 << vehicle.OwnLayer;
        }

        return mask;
    }

    public static int GetNewBotLayer()
    {
        int minBotLayer = LayerMask.NameToLayer("Bot1");
        int maxBotLayer = LayerMask.NameToLayer("Bot9");

        for (int botLayer = minBotLayer; botLayer <= maxBotLayer; botLayer++)
        {
            bool layerIsFree = true;
            foreach (VehicleController vehicle in BattleController.allVehicles.Values)
            {
                if (vehicle.OwnLayer == botLayer)
                {
                    layerIsFree = false;
                    break;
                }
            }
            if (layerIsFree)
                return botLayer;
        }

        Debug.LogError("Cannot get new bot layer (all layers are busy)");
        return 0;
    }

    private void OnMainTankAppeared(EventId id, EventInfo ei)
    {
        Debug.Log("my team = " + BattleController.MyVehicle.data.teamId);
        if (!PhotonNetwork.isMasterClient || !Localizer.Loaded)
            return;

        tutorialBotNames = BotNames.GetDummyNames();
        StartCoroutine(CheckIfBotsRequired(false));
        StartCoroutine(BotEnterCoroutine());

        if (GameData.IsTeamMode)
        {
            StartCoroutine(FindTeamsEpicenters());
        }
    }

    private void AfterLocalizationLoaded(EventId id, EventInfo ei)
    {
        tutorialBotNames = BotNames.GetDummyNames();

        StartCoroutine(CheckIfBotsRequired(false));
        StartCoroutine(BotEnterCoroutine());
    }

    private void OnRoomBusyListTrimmed(EventId id, EventInfo ei)
    {
        StartCoroutine(CheckIfBotsRequired(false));
    }

    private IEnumerator CheckIfBotsRequired(bool waitBeforeBotCreating)
    {
        if (waitingForBotsInProgress || !GameData.isBotsEnabled || ProfileInfo.IsBattleTutorial || !PhotonNetwork.isMasterClient)  // для тутора ботов спауним отдельно, на уроке уничтожения техники
            yield break;

        waitingForBotsInProgress = true;
        int totalVehicles = BattleController.allVehicles.Count + (int) PhotonNetwork.room.CustomProperties["rp"];
        if (totalVehicles > GameData.maxPlayers)
        {
            RemoveUnnecessaryBots(totalVehicles - GameData.maxPlayers);
            waitingForBotsInProgress = false;
            yield break;
        }

        while (botGenerationDelayed)
            yield return null;

        int botsAlreadyInRoom = (int) PhotonNetwork.room.CustomProperties["bcn"];

        int botsQuantity = Mathf.Min(GameData.maxPlayers - totalVehicles, GameData.minBotCount - botsAlreadyInRoom);

        BotsSpawnInProgress = true;
        if (waitBeforeBotCreating)
            yield return null;

        for (int i = 0; i < botsQuantity; i++)
        {
            yield return GameConstants.FixedUpdateWaiter;
            CreateBotForCurrentProject();
        }
        RefreshBotCounter();
        BotsSpawnInProgress = false;
        waitingForBotsInProgress = false;
    }

     public void CreateBotForCurrentProject()
    {
        switch (GameData.ClearGameFlags(GameData.CurrentGame))
        {
            case Game.IronTanks:
            case Game.FutureTanks:
            case Game.ToonWars:
            case Game.FTRobotsInvasion:
                if (ProfileInfo.IsBattleTutorial)
                {
                    CreateBot(BotBehaviours.Tutorial, false);
                }
                else
                {
                    CreateBot(SelectRandomBotType(), !BattleController.MyVehicle.data.newbie);
                }
                break;
            default:
                throw new Exception(GameData.CurInterface + " case is not defined!");
        }
    }

    private void RefreshBotCounter()
    {
        if (!PhotonNetwork.isMasterClient)
            return;

        Hashtable properties = new Hashtable {{"bcn", botDict.Count}};
        PhotonNetwork.room.SetCustomProperties(properties);
    }

    private void OnBotKilled(EventId id, EventInfo info)
    {
        if (ProfileInfo.TutorialIndex == (int) Tutorials.battleTutorial)
        {
            return;
        }

        EventInfo_II eiII = (EventInfo_II) info;

        //var attackerId = eiII.int2;
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
        if (BattleController.allVehicles.TryGetValue(playerId, out bot)
            && bot != null)
        {
            bot.MakeRespawn(false, true, false);
        }
    }

    private void OnNewPlayerConnected(EventId id, EventInfo ei)
    {
        if (!PhotonNetwork.isMasterClient || GameData.Mode != GameData.GameMode.Deathmatch)
            return;

        if (BattleController.allVehicles.Count == GameData.maxPlayers && botDict.Count > 0) // Есть, куда "впихнуть" всё-таки попавшего игрока
        {
            RemoveUnnecessaryBots(1);
            botGenerationDelayed = true;
        }
    }

    private void OnNewVehConnected(EventId id, EventInfo ei)
    {
        EventInfo_I info = ei as EventInfo_I;
        if (info == null)
            return;

        VehicleController veh = BattleController.allVehicles[info.int1];
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
        if (PhotonNetwork.connected && PhotonNetwork.isMasterClient)
        {
            RefreshBotCounter();
            StartCoroutine(CheckIfBotsRequired(true));
        }
    }

    private void RemoveUnnecessaryBots(int count)
    {
        for (int botNumber = 0; botNumber < count; botNumber++)
        {
            List<VehicleController> truncTeam = teamBots[0].Count >= teamBots[1].Count ? teamBots[0] : teamBots[1];
            if (truncTeam.Count == 0)
                return;
            VehicleController botToRemove = truncTeam[0];
            for (int i = 1; i < truncTeam.Count; i++)
            {
                if (truncTeam[i].Statistics.score < botToRemove.Statistics.score)
                    botToRemove = truncTeam[i];
            }

            botToRemove.Explode();
            RemoveBot(botToRemove);
        }
        RefreshBotCounter();
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

        StartCoroutine(CheckIfBotsRequired(false));
        StartCoroutine(BotEnterCoroutine());

        StartCoroutine(RespawnAllCauseMaster());
    }

    private void OnReconnect(EventId id, EventInfo ei)
    {
        StopAllCoroutines();
    }
    
    private IEnumerator BotEnterCoroutine()
    {
        yield return GameConstants.FixedUpdateWaiter;
        if (ProfileInfo.IsBattleTutorial || !GameData.isBotsEnabled)
            yield break;

        float lastEnterTime = Time.time;
        float botEnterDelay = Random.Range(GameData.minBotEngageDelay, GameData.maxBotEngageDelay);
        WaitForSeconds oneSecondWaiting = new WaitForSeconds(1f);
        while (PhotonNetwork.isMasterClient)
        {
            if (GameData.isBotsEnabled)
            {
                while (waitingForBotsInProgress)
                    yield return null;

                int totalVehicles = BattleController.allVehicles.Count +
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
        vehicleInfos = new Dictionary<int, List<VehicleInfo>>(5);

        VehicleInfo[] vehicles = VehiclePool.Instance.Items;

        foreach (var vehicle in vehicles)
        {
            if (vehicle.isComingSoon || vehicle.isHidden) // Не добавлять танки "в разработке" и скрытые.
                continue;

            int group = vehicle.vehicleGroup;
            if (!vehicleInfos.ContainsKey(group))
                vehicleInfos.Add(group, new List<VehicleInfo>(3));

            vehicleInfos[group].Add(vehicle);
        }
    }

    private int GetAveragePlayerLevel()
    {
        float sum = 0;
        int playersCount = 0;
        foreach (VehicleController vehicle in BattleController.allVehicles.Values)
        {
            if (vehicle.IsBot)
                continue;

            sum += vehicle.data.playerLevel;
            playersCount++;
        }

        return Mathf.RoundToInt(sum / playersCount);
    }

    private IEnumerator RespawnAllCauseMaster()
    {
        yield return null;
        BotsSpawnInProgress = true;
        VehicleController[] unavailable = allBots.Values.Where(x => !x.IsAvailable).ToArray();
        foreach (var bot in unavailable)
        {
            if (!bot.IsAvailable)
                bot.MakeRespawn(false, true, false);
            yield return GameConstants.FixedUpdateWaiter;
        }
        BotsSpawnInProgress = false;
    }

#if UNITY_EDITOR
/*    private void TeamDebugTools()
    {
        if (GameData.Mode != GameData.GameMode.Team)
            return;

        if (Input.GetKeyDown(KeyCode.Alpha3))
        {
            GameData.isBotsEnabled = !GameData.isBotsEnabled;
            Debug.LogFormat("Bots spawning {0}", GameData.isBotsEnabled ? "resumed" : "stopped");
        }

        if (Input.GetKeyDown(KeyCode.Alpha1) || Input.GetKeyDown(KeyCode.Alpha2))
        {
            int myTeamId = BattleController.MyVehicle.data.teamId;
            int teamId = Input.GetKeyDown(KeyCode.Alpha1) ? myTeamId : 1 - myTeamId;
            var botEnum = allBots.Values.Where(x => x.data.teamId == teamId);
            if (!botEnum.Any())
                return;

            VehicleController bot = botEnum.First();
            RemoveBot(bot);
            RefreshBotCounter();
        }

        if (Input.GetKeyDown(KeyCode.Alpha8))
            CreateBotForCurrentProject();
    }
*/
    private void OnDrawGizmos()
    {
        if (!drawBotPaths)
            return;

        Gizmos.color = Color.red;
        Gizmos.DrawSphere(enemiesEpicenter, 2);

        Gizmos.color = Color.blue;
        Gizmos.DrawSphere(friendsEpicenter, 2);
    }
#endif

    public static BotBehaviours SelectRandomBotType()
    {
        var botBehaviour = MiscTools.GetRandomFromSeveral(botTypePriorities.Values.ToArray(), botTypePriorities.Keys.ToArray())[0];

        return botBehaviour;
    }
}