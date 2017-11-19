using System;
using Disconnect;
using CodeStage.AntiCheat.ObscuredTypes;
using CodeStage.AntiCheat.Detectors;
using Http;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Matchmaking;
using Pool;
using UnityEngine.SceneManagement;

using Hashtable = ExitGames.Client.Photon.Hashtable;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class GameManager : MonoBehaviour
{
    public enum MapId
    {
        // HANGARS
        LoadingScene = 0, // Индексы не менять!
        Hangar_WWT2_Standart = 21,
        Hangar_WWT2_Premium = 22,

        //WWT2
        scnb_WWT2_1 = 701,
        scnb_WWT2_2 = 702,
        scnb_WWT2_3 = 703,
        scnb_WWT2_4 = 704,
        scnb_WWT2_5 = 705,
        scnb_WWT2_6 = 706,

        random_map = 1000
    }

    public const string PHOTON_ROOM_VERSION = "v7.3";

    public static Dictionary<Interface, Dictionary<bool, string>> photonIds = new Dictionary<Interface, Dictionary<bool, string>>()
    {
        {Interface.WWT2,new Dictionary<bool,string>()
            {
                { false, "c0695475-5d6d-409c-b0ef-3eb58de68f55" }, // NORMAL_PHOTON_APPLICATION_ID
				{ true,  "c0695475-5d6d-409c-b0ef-3eb58de68f55" }  // SPECIAL_PHOTON_APPLICATION_ID - for cheaters
			}
        },
    };

    public const string LOADING_SCENE_NAME = "LoadingScene";
    private const float MAX_ENTER_ROOM_WAITING = 6f;

    public static readonly ObscuredInt BONUS_START_PRICE = 1;
    public static readonly ObscuredInt PROLONG_START_PRICE = 4;
    public static readonly ObscuredFloat NEWBIE_DAMAGE_RATIO = 0.5f;
    public static readonly ObscuredFloat CRIT_DAMAGE_RATIO = 1.31f;
    public static readonly ObscuredFloat NORM_DAMAGE_RATIO = 0.71f;
    public static readonly ObscuredFloat RANDOM_DAMAGE_RATIO_LOWER_BOUND = 0.9f;
    public static readonly ObscuredFloat RANDOM_DAMAGE_RATIO_UPPER_BOUND = 1.1f;
    public static readonly ObscuredFloat BONUS_PRICE_INCREASE_RATIO = 2.0f;
    public static readonly ObscuredFloat PROLONG_PRICE_INCREASE_RATIO = 2.0f;
    public static readonly ObscuredInt NEWBIE_BATTLES_AMOUNT = 5;
    public static readonly ObscuredInt NEWBIE_VEHICLE_ID = 1;

    public static GameManager Instance { get; private set; }

    public tk2dBaseSprite[] spritesForReloadTexture;

    private static bool hangarLoaded;
    private static string vehiclePrefabName;
    private static ObscuredInt vehicleGroup;
    private static ObscuredInt mainVehicleCamouflageId;
    private static ObscuredInt mainVehicleDecalId;
    private static ObscuredFloat missileAimingDuration = 4.0f;
    private static VehicleUpgrades vehicleUpgrades;
    private static Dictionary<VehicleInfo.VehicleParameter, ObscuredFloat> mainVehicleParameters;

    private bool isRoomCreator;
    private bool joinFailed;
    private bool battleOptionsReceived;
    private int matchedTeam = -1;
    private double myJoinRoomTime = -1;
    private int mapId;

    public static double MyJoinRoomTime { get { return Instance.myJoinRoomTime; } }

    public static int MatchedTeam { get { return Instance.matchedTeam; } }

    public static bool BattleOptionsReceived
    {
        get
        {
            return Instance.battleOptionsReceived;
        }
    }

    public static string PhotonRoomVersion
    {
        get
        {
            var ver = PHOTON_ROOM_VERSION + Http.Manager.PhotonPostfix;

            return ver;
        }
    }

    public static string CurrentResourcesFolder
    {
        get
        {
            return GameData.CurInterface.ToString();
        }
    }

    public static string GetResourceFolderForGame(Game game)
    {
        return GameData.GameToInterface(game).ToString();
    }

    public static string PrefabNamePrefix
    {
        get
        {
            // TODO: заменить на GameData.InterfaceShortName, когда переменуется во всех проектах.
            return string.Empty;
        }
    }

    public static MapId CurrentMap
    {
        get
        {
            return (MapId)Enum.Parse(typeof(MapId), SceneManager.GetActiveScene().name);
        }
    }

    public static float MissileAimingDuration
    {
        get { return missileAimingDuration; }
    }

    public static bool IsStandAlone
    {
        get
        {
#if UNITY_STANDALONE
            return true;
#else
            return false;
#endif
        }
    }

    /* UNITY SECTION */

    void Awake()
    {
        BattleConnectManager.AddPhotonMessageTarget(gameObject);
        if (SceneManager.GetActiveScene().name == LOADING_SCENE_NAME)
        {
            ReloadTextures();
            return;
        }

        if (SceneManager.GetActiveScene().name == GameData.GetHangarSceneName(false) || SceneManager.GetActiveScene().name == GameData.GetHangarSceneName(true))
            hangarLoaded = true;

#if UNITY_EDITOR
        if (!GameData.ServerDataReceived)
        {
            // Грузим сцену лоадинг если зашли с боевой карты в редакторе.
            Loading.gotoLoadingScene();
            return;
        }
#endif
        Instance = this;

        if (HangarController.FirstEnter)
            SpeedHackDetector.StartDetection(OnSpeedHackDetected);

        ReloadTextures();

        Dispatcher.Subscribe(EventId.BeforeReconnecting, BeforeReconnecting);
        Dispatcher.Subscribe(EventId.FirstRequestToMaster, OnFirstRequestToMaster);
        Dispatcher.Subscribe(EventId.NowImMaster, OnImMaster, 2);
        Dispatcher.Subscribe(EventId.PhotonRoomListReceived, OnRoomListReceived);
        Dispatcher.Subscribe(EventId.PhotonJoinedRoom, OnPhotonJoinedRoom);
        Dispatcher.Subscribe(EventId.ProfileMoneyChange, OnProfileMoneyChange);
        Dispatcher.Subscribe(EventId.JoinRoomFailed, OnJoinRoomFailed);
        Dispatcher.Subscribe(EventId.TakeYourTeamId, OnTeamIdReceived);
        Dispatcher.Subscribe(EventId.TankLeftTheGame, OnVehicleLeftTheGame);
        Dispatcher.Subscribe(EventId.TankJoinedBattle, OnVehicleJoinTheGame);
    }

    private void Start()
    {
        //PhotonNetwork.logLevel = PhotonLogLevel.Full;

        if (SceneManager.GetActiveScene().name == LOADING_SCENE_NAME)
            return;

        mapId = (int)CurrentMap;
        if (!GameData.IsHangarScene)
        {
            Manager.BattleServer.PrepareToBattle();
            ConnectToPhoton();
        }
    }

    void OnDestroy()
    {
        BattleConnectManager.RemovePhotonMessageTarget(gameObject);

        foreach (tk2dBaseSprite sprite in spritesForReloadTexture)
            sprite.Collection.UnloadTextures();

        Dispatcher.Unsubscribe(EventId.BeforeReconnecting, BeforeReconnecting);
        Dispatcher.Unsubscribe(EventId.FirstRequestToMaster, OnFirstRequestToMaster);
        Dispatcher.Unsubscribe(EventId.TakeYourTeamId, OnTeamIdReceived);
        Dispatcher.Unsubscribe(EventId.NowImMaster, OnImMaster);
        Dispatcher.Unsubscribe(EventId.PhotonRoomListReceived, OnRoomListReceived);
        Dispatcher.Unsubscribe(EventId.PhotonJoinedRoom, OnPhotonJoinedRoom);
        Dispatcher.Unsubscribe(EventId.ProfileMoneyChange, OnProfileMoneyChange);
        Dispatcher.Unsubscribe(EventId.JoinRoomFailed, OnJoinRoomFailed);
        Dispatcher.Unsubscribe(EventId.TankLeftTheGame, OnVehicleLeftTheGame);
        Dispatcher.Unsubscribe(EventId.TankJoinedBattle, OnVehicleJoinTheGame);

        Instance = null;
    }

    private void OnPhotonJoinedRoom(EventId id, EventInfo ei)
    {
        PhotonNetwork.FetchServerTimestamp();
        myJoinRoomTime = PhotonNetwork.time;
        if (BattleConnectManager.Instance.FirstConnect)
            BattleController.battleInventory = ConsumableInventory.GetBattleInventoryDic();

        StartCoroutine(OwnFirstSpawn());
    }

    public static void SetMainVehicle(UserVehicle vehicle)
    {
        vehicleUpgrades = vehicle.Upgrades;
        vehiclePrefabName = vehicle.HangarVehicle.name;
        vehicleGroup = vehicle.Info.vehicleGroup;
        mainVehicleParameters = vehicle.GetRealParameters();
        mainVehicleCamouflageId = vehicle.Upgrades.CamouflageId;
        mainVehicleDecalId = vehicle.Upgrades.DecalId;
    }

    public static void ReturnToHangar()
    {
        //Application.LoadLevel(GameData.IsGame(Game.IronTanks) ? "LoadingScene" : "LoadingSceneTeam");
        Loading.gotoLoadingScene();
    }

#if UNITY_EDITOR

    [MenuItem("HelpTools/Reload Textures in scene %#r")]
    public static void ReloadTexturesEditor()
    {
        GameObject go = GameObject.Find("GameManager");

        GameManager target = go ? go.GetComponent<GameManager>() : null;

        if (!target)
            Debug.Log("There is no GameManager component on the scene. Operation aborted.");
        else
            target.ReloadTextures();
    }

#endif

    public void ConnectToPhoton()
    {
        PhotonNetwork.playerName = ProfileInfo.PlayerName;
        PhotonNetwork.PhotonServerSettings.AppID = photonIds[GameData.CurInterface][false];

        if (PhotonNetwork.offlineMode)
        {
            MatchMaker.CreateRoom((int)MapId.scnb_WWT2_1, 1);
            return;
        }

        try
        {
            if (!PhotonNetwork.connected)
                PhotonNetwork.ConnectUsingSettings(PhotonRoomVersion);
        }
        catch (Exception)
        {
            Debug.Log("Photon connection failed!");
        }
    }
    

    /* PRIVATE SECTION */

    private static void OnSpeedHackDetected()
    {
        Dispatcher.Send(EventId.CheatDetected, new EventInfo_I((int)CheatType.Speedup));
    }

    private void OnRoomListReceived(EventId id, EventInfo ei)
    {
        if (BattleController.PlayerInBattle && BattleConnectManager.Instance.FirstConnect)
        {
            BattleController.EndBattle(BattleController.EndBattleCause.AlreadyInBattle);
            return;
        }
        PhotonNetwork.FetchServerTimestamp();

        string selectedRoomName = null;
        #region Поиск по имени комнаты для возврата
        if (!string.IsNullOrEmpty(BattleConnectManager.Instance.LastRoomName))
        {
            RoomInfo[] roomInfos = PhotonNetwork.GetRoomList();
            foreach (var roomInfo in roomInfos)
            {
                if (roomInfo.name == BattleConnectManager.Instance.LastRoomName)
                {
                    selectedRoomName = roomInfo.name;
                    break;
                }
            }
        }
        #endregion

        if (selectedRoomName == null)
        {
            int dummy;
            string preSelectedRoom = null;
            if (RoomInfoManager.SelectedRooms != null)
                RoomInfoManager.SelectedRooms.TryGetValue((MapId) mapId, out preSelectedRoom);
            selectedRoomName = MatchMaker.SelectRoom(mapId, true, preSelectedRoom, out dummy, vehicleGroup);
        }
        JoinRoom(selectedRoomName);
    }

    private static void SetPlayerProperties(TankData data, PlayerStat stats)
    {
        Hashtable properties = new Hashtable
        {
            { "hl", (int)data.armor },
            { "at", (int)data.attack },
            { "ra", (int)data.rocketAttack },
            { "sp", (float)data.speed },
            { "rf", (float)data.rof },
            { "ir", (float)data.ircmRof },
            { "sc", stats == null ? 0 : stats.score},
            { "dt", stats == null ? 0 : stats.deaths },
            { "kl", stats == null ? 0 : stats.kills }
        };

        PhotonNetwork.player.SetCustomProperties(properties);
    }

    private void BeforeReconnecting(EventId id, EventInfo ei)
    {
        StopAllCoroutines();
        CancelInvoke();
    }

    private void ReloadTextures()
    {
        if (spritesForReloadTexture == null || spritesForReloadTexture.Length == 0)
        {
            Debug.Log("No sprites specified in GameData for reloading textures. Operation cancelled.");
            return;
        }

        foreach (tk2dBaseSprite spr in spritesForReloadTexture)
            spr.GetComponent<Renderer>().sharedMaterial.mainTexture = spr.GetComponent<Renderer>().sharedMaterial.mainTexture as Texture2D;
    }

    private IEnumerator OwnFirstSpawn()
    {
        SocialPlatform playerPlatform = SocialSettings.Platform;
        string playerUid = SocialSettings.GetSocialService().Uid();
        TankData vehicleData;
        if (BattleConnectManager.Instance.FirstConnect)
            vehicleData = new TankData(
                playerName: ProfileInfo.PlayerName,
                playerLevel: ProfileInfo.Level,
                country: ProfileInfo.CountryCode,
                newbie: ProfileInfo.IsNewbie,
                socialPlatform: playerPlatform,
                socialUID: playerUid,
                hangarParameters: mainVehicleParameters,
                patternId: mainVehicleCamouflageId,
                decalId: mainVehicleDecalId,
                teamId: 0,
                hideMyFlag: ProfileInfo.isHideMyFlag,
                profileId: ProfileInfo.profileId,
                vip: ProfileInfo.IsPlayerVip,
                clanName: ProfileInfo.Clan != null ? ProfileInfo.Clan.Name : string.Empty,
                regeneration: 0,
                shield: 0,
                takenDamageRatio: 1f
                );
        else
        {
            vehicleData = BattleConnectManager.Instance.MyLastTankData;
        }
        vehicleData.playerId = PhotonNetwork.player.ID;
        PhotonNetwork.player.SetCustomProperties(new Hashtable { {"ex", true} });

        if (PhotonNetwork.isMasterClient) // Пропустить кадр, чтобы не пытаться писать в кэш фотона в секции обработок его события
            yield return null;

        if (GameData.Mode == GameData.GameMode.Deathmatch) //Если дезматч, то обойдемся без запроса к мастеру
        {
            matchedTeam = 0;
            if (!PhotonNetwork.isMasterClient) // Позволить сначала появиться клонам
                yield return new WaitForFixedUpdate();
        }
        else
        {
            #region Отправка первого запроса мастеру и ожидание ответа с номером команды.
            matchedTeam = -2;
            Dispatcher.Send(EventId.FirstRequestToMaster, new EventInfo_U(vehicleData),
                Dispatcher.EventTargetType.ToMaster);
            float startAnswerWaiting = Time.time;
            while (matchedTeam < 0)
            {
                if (matchedTeam == -1) // Не пустил мастер
                {
                    DisconnectFor("NoPlaceInRoomForPlayer");
                    BattleConnectManager.Instance.ForcedDisconnect();
                    yield break;
                }
                if (Time.time - startAnswerWaiting >= MAX_ENTER_ROOM_WAITING) // Истекло время ожидания ответа от мастера
                {
                    DisconnectFor("NoResponseFromMaster");
                    BattleConnectManager.Instance.ForcedDisconnect();
                    yield break;
                }

                yield return null;
            }
            #endregion
        }
        #region Отправка серверу сигнала о входе в комнату и ожидания ответа с боевыми опциями
        if (!ProfileInfo.IsBattleTutorial)
        {
            if (BattleConnectManager.Instance.FirstConnect)
                Instance.StartBattleReport(GameData.isBotsEnabled);
            while (!BattleOptionsReceived)
                yield return null;
            if (BattleConnectManager.Instance.FirstConnect && (GameData.isBotsEnabled || BattleController.CheckPlayersCount()))
                Manager.BattleServer.StartTimer((int)(PhotonNetwork.time - myJoinRoomTime));
        }
        
        #endregion
        vehicleData.teamId = matchedTeam;
        if (PhotonNetwork.room == null)
        {
            GameData.CriticalError(Localizer.GetText("NTPConnectionError"));
            yield break;
        }

        if (ProfileInfo.IsNewbie)
        {
            mainVehicleParameters[VehicleInfo.VehicleParameter.Damage] /= NEWBIE_DAMAGE_RATIO;
            mainVehicleParameters[VehicleInfo.VehicleParameter.RocketDamage] /= NEWBIE_DAMAGE_RATIO;
        }

        Vector3 spawnPosition;
        Quaternion spawnRotation;
        if (string.IsNullOrEmpty(BattleConnectManager.Instance.LastRoomName) || BattleConnectManager.Instance.RespawnAfterReconnect)
        {
            Transform spawnPoint = SpawnPoints.instance.GetRandomPoint(vehicleData.teamId);
            spawnPosition = spawnPoint.position;
            spawnRotation = spawnPoint.rotation;
        }
        else
        {
            spawnPosition = BattleConnectManager.Instance.MyLastPosition;
            spawnRotation = BattleConnectManager.Instance.MyLastRotation;
        }

        SetPlayerProperties(vehicleData, BattleConnectManager.Instance.MyLastPlayerStat);

        VehicleController vehicleController
            = PhotonNetwork.Instantiate(
                    prefabName: string.Format("{0}/{1}/{2}", CurrentResourcesFolder, "BattleVehicles/PlayerVehicles", vehiclePrefabName),
                    position: spawnPosition,
                    rotation: spawnRotation,
                    group: 0,
                    data: new object[] { vehicleData })
                .GetComponent<VehicleController>();
        vehicleController.name = "Vehicle_" + PhotonNetwork.player.ID;
        PreWarmBattlePools(vehicleController);
        BattleController.SetMainVehicle(vehicleController);
        Dispatcher.Send(EventId.MainTankAppeared, new EventInfo_SimpleEvent());
    }

    public static void PreWarmBattlePools(VehicleController vehicle)
    {
        PoolManager.PreWarm<ParticleEffect>(vehicle.hitPrefabPath, 3);
        PoolManager.PreWarm<ParticleEffect>(vehicle.terrainHitPrefabPath, 3);
        PoolManager.PreWarm<ParticleEffect>(vehicle.shotPrefabPath, 3);
        PoolManager.PreWarm<Shell>(vehicle.shellPrefabPath, 3);
        PoolManager.PreWarm<ParticleEffect>(vehicle.explosionPrefabPath, 2);
    }

    // Matchmaker itself.

    public void JoinRoom(string roomName)
    {
        if (string.IsNullOrEmpty(roomName))
            return;

        StartCoroutine(JoinRoom_Coroutine(roomName));
    }

    public void StartBattleReport(bool roomWasFulled)
    {
        Manager.BattleServer.StartBattle(
               room: PhotonNetwork.room,
               vehicleUpgrades: vehicleUpgrades,
               vehicleParameters: mainVehicleParameters,
               consumables: BattleController.battleInventory,
               isCreateRoom: isRoomCreator,
               roomWasFulled: roomWasFulled,
               result: delegate (bool result)
               {
                   if (result)
                   {
                       battleOptionsReceived = true;
                       if (isRoomCreator)
                       {
                           PhotonNetwork.room.open = true; //комната октрывается здесь
                       }
                   }
                   else
                   {
                       battleOptionsReceived = false;
                       BattleConnectManager.Instance.ForcedDisconnect();
                   }
               });
    }

    private IEnumerator JoinRoom_Coroutine(string roomName)
    {
        string mapName;
        int dummy;
        RoomInfo suitableRoom = null;
        // Проверка: открыта ли комната.
        do
        {
            RoomInfo[] rooms = PhotonNetwork.GetRoomList();
            suitableRoom = null;
            foreach (var room in rooms)
            {
                if (room.name == roomName)
                {
                    suitableRoom = room;
                    break;
                }
            }

            if (suitableRoom == null || !suitableRoom.open)
            {
                roomName = MatchMaker.SelectRoom(mapId, true, null, out dummy, vehicleGroup);
                JoinRoom(roomName);
                yield break;
            }

            mapName = Enum.Parse(typeof(MapId), suitableRoom.customProperties["mp"].ToString()).ToString();
            if (suitableRoom.open)
                break;

            yield return new WaitForSeconds(1);
        } while (true);

        // Попытка подключения.
        PhotonNetwork.JoinRoom(suitableRoom.name);

        //Manager.ReportStats (
        //    location: "matchmaker",
        //    action: "joinRoom",
        //    query: new Dictionary<string, string>
        //    {
        //        { "name", suitableRoom.name },
        //        { "level", suitableRoom.customProperties["lv"].ToString() },
        //        { "mapId", suitableRoom.customProperties["mp"].ToString() },
        //        { "mapName", mapName },
        //        { "mode", suitableRoom.customProperties["gm"].ToString() },
        //    });

        // Ожидание и проверка результата подключения.
        while (PhotonNetwork.connectionStateDetailed == ClientState.Joining)
            yield return null;

        if (!joinFailed)
        {
            #region Google Analytics: joining battle

            GoogleAnalyticsWrapper.LogEvent(
                new CustomEventHitBuilder()
                    .SetParameter(GAEvent.Category.JoinBattle)
                    .SetParameter<GAEvent.Action>()
                    .SetSubject(GAEvent.Subject.MapName, mapName)
                    .SetParameter<GAEvent.Label>()
                    .SetSubject(GAEvent.Subject.VehicleID, ProfileInfo.CurrentVehicle)
                    .SetValue(ProfileInfo.Level));

            GoogleAnalyticsWrapper.LogEvent(
                new CustomEventHitBuilder()
                    .SetParameter(GAEvent.Category.JoinBattle)
                    .SetParameter<GAEvent.Action>()
                    .SetSubject(GAEvent.Subject.MapName, mapName)
                    .SetParameter<GAEvent.Label>()
                    .SetSubject(GAEvent.Subject.PlayerLevel, ProfileInfo.Level)
                    .SetValue(Convert.ToInt64(ProfileInfo.Fuel)));

            GoogleAnalyticsWrapper.LogEvent(
                new CustomEventHitBuilder()
                    .SetParameter(GAEvent.Category.JoinBattle)
                    .SetParameter<GAEvent.Action>()
                    .SetSubject(GAEvent.Subject.MapName, mapName)
                    .SetParameter<GAEvent.Label>()
                    .SetSubject(GAEvent.Subject.GameMode, GameData.Mode)
                    .SetValue(ProfileInfo.Level));

            GoogleAnalyticsWrapper.LogScreen(GAScreens.Battle);

            #endregion
        }
        else
        {
            joinFailed = false;
            roomName = MatchMaker.SelectRoom(mapId, true, null, out dummy, vehicleGroup);
            JoinRoom(roomName);
        }
    }

    private void OnTeamIdReceived(EventId eid, EventInfo ei)
    {
        EventInfo_I info = (EventInfo_I) ei;
        matchedTeam = info.int1;
    }

    private void OnImMaster(EventId id, EventInfo ei)
    {
        EventInfo_B info = (EventInfo_B)ei;

        if (info.bool1)
        {
            isRoomCreator = true;
            PhotonNetwork.room.SetCustomProperties(new Hashtable { { "ct", PhotonNetwork.time } });
        }

        StartCoroutine(CheckIfRoomIsOld());
    }

    private void OnFirstRequestToMaster(EventId id, EventInfo ei)
    {
        if (!PhotonNetwork.isMasterClient)
            return;

        EventInfo_U info = (EventInfo_U)ei;
        TankData data = (TankData)info[0];
        int teamId = MatchMaker.GetTeamForNewPlayer(data);
        Dispatcher.Send(EventId.TakeYourTeamId, new EventInfo_I(teamId), Dispatcher.EventTargetType.ToSpecific, data.playerId);
    }

    private void OnFirstMasterResponse(EventId id, EventInfo ei)
    {
        EventInfo_U info = (EventInfo_U) ei;
        TankData data = (TankData) info[0];
    }

    private IEnumerator CheckIfRoomIsOld()
    {
        if (!PhotonNetwork.room.visible)
            yield break;

        YieldInstruction wait = new WaitForSeconds(1);

        double roomCreatedAt = (double)PhotonNetwork.room.customProperties["ct"];

        while (PhotonNetwork.isMasterClient)
        {
            double roomAge = (PhotonNetwork.time - roomCreatedAt);

            if (roomAge < 0 || roomAge > GameData.BattleRoomLifetime * 60)
            {
                PhotonNetwork.room.visible = false;
                PhotonNetwork.room.open = false;
                yield break;
            }

            yield return wait;
        }
    }

    private void OnProfileMoneyChange(EventId id, EventInfo ei)
    {
        EventInfo_II info = (EventInfo_II)ei;

        int silver = info.int2;
        int gold = info.int1;

        GPGSWrapper.LogBalanceChange(silver, gold);
    }

    private void OnJoinRoomFailed(EventId id, EventInfo ei)
    {
        joinFailed = true;
    }

    private void OnVehicleLeftTheGame(EventId id, EventInfo ei)
    {
        if (PhotonNetwork.isMasterClient)
            RecalcCountryMembers();
    }

    private void OnVehicleJoinTheGame(EventId id, EventInfo ei)
    {
        if (PhotonNetwork.isMasterClient)
            RecalcCountryMembers();
    }

    private void RecalcCountryMembers()
    {
        int[] teamMembers = MatchMaker.CountTeamMembers();
        RoomCountryInfo[] cntr = PhotonNetwork.room.customProperties["cntr"] as RoomCountryInfo[];
        for (int i = 0; i < cntr.Length; i++)
        {
            cntr[i].players = teamMembers[i];
        }
        PhotonNetwork.room.SetCustomProperties(new Hashtable { { "cntr", cntr } });
    }

    private void DisconnectFor(string cause)
    {
        var query = new Dictionary<string, string>
            {
                { "tankId", ProfileInfo.currentVehicle.ToString() },
                { "cause", cause}
            };

        Http.Manager.ReportStats(
        location: "battle",
        action: "disconnect",
        query: query);

        Dispatcher.Send(EventId.PhotonDisconnectWithCause, new EventInfo_S(cause));
    }
}