using System;
using System.Linq;
using Disconnect;
using CodeStage.AntiCheat.ObscuredTypes;
using CodeStage.AntiCheat.Detectors;
using Http;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Matchmaking;
using UnityEngine.SceneManagement;

using Hashtable = ExitGames.Client.Photon.Hashtable;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class GameManager : MonoBehaviour
{
    public enum MapId
    {
        // IRON TANKS + HANGARS
        LoadingScene = 0, // Индексы не менять!
        Hangar_IronTanks_Standart = 1,
        Battle_SciFiArena = 2,
        White_polygon = 3,
        Hangar_FutureTanks_Standart = 4,
        Battle_PowerPlant = 5,
        Battle_Perron = 6,
        Battle_Dome = 7,
        Hangar_IronTanks_Premium = 8,
        Hangar_FutureTanks_Premium = 9,
        Hangar_ToonWars_Standart = 10,
        Hangar_ToonWars_Premium = 11,
        scnh_sj_standart = 12,
        scnh_sj_premium = 13,
        Hangar_ApocalypticCars_Standart = 14,
        Hangar_ApocalypticCars_Premium = 15,
        Hangar_BattleOfWarplanes_Standart = 16,
        Hangar_BattleOfWarplanes_Premium = 17,
        Hangar_BattleOfHelicopters_Standart = 18,
        Hangar_BattleOfHelicopters_Premium = 19,
        Battle_Cubism = 20,
        Hangar_Armada_Standart = 21,
        Hangar_Armada_Premium = 22,
        Hangar_WWR_Standart = 23,
        Hangar_WWR_Premium = 24,
        Hangar_FTRobotsInvasion_Standart = 25,
        Hangar_FTRobotsInvasion_Premium = 26,

        // Iron Tanks
        Battle_SciFiArenaNew = 50,

        // FUTURE TANKS
        scnb_mars = 101,
        scnb_moon = 102,
        //scnb_outpost = 103,
        scnb_snow = 104,
        scnb_desert = 105,
        scnb_hive = 106,
        //scnb_crystallvalley = 107,
        //scnb_building17 = 108,
        scnb_redsands = 109,

        // TOON WARS
        scnb_farm = 201,
        scnb_city = 202,
        scnb_oasis = 203,
        scnb_island = 204,
        scnb_ravine = 205,

        // SPACE JET
        scnb_sj_tutorial = 300,
        scnb_sj_aurora = 301,
        scnb_sj_blueabyss = 302,
        scnb_sj_infinity = 303,

        // APOCALYPTIC CARS
        scnac_airport = 401,
        scnac_pier = 402,
        scnac_city = 403,
        scnac_urban_test = 404,
        scnac_desert_test = 405,

        // BATTLE OF WARPLANES
        scnb_bw_island = 501,
        scnb_bw_mohave = 502,
        scnb_bw_nyday = 503,

        // BATTLE OF HELICOPTERS
        scnb_bh_1 = 601,
        scnb_bh_2 = 602,
        scnb_bh_3 = 603,
        scnb_bh_4 = 604,

        //Armada
        scnb_armada_1 = 701,
        scnb_armada_2 = 702,
        scnb_armada_3 = 703,
        scnb_armada_4 = 704,
        scnb_armada_5 = 705,
        scnb_armada_6 = 706,

        //WWR
        scnb_wwr_snow = 801,
        scnb_wwr_base = 802,
        scnb_wwr_3 = 803,

        // Future Tanks: Robots Invasion
        scnb_outpost = 901,
        scnb_building17 = 902,
        scnb_heaven = 903,
        scnb_crystallvalley = 904,

        random_map = 1000
    }

    public static Dictionary<Interface, string> photonIds = new Dictionary<Interface, string>()
    {
        {Interface.IronTanks,           "d6c5112d-051e-43ac-929f-ca06c2764708"},
        {Interface.FutureTanks,         "361b2cb5-8e8b-4b5d-8426-6c327df826ff"},
        {Interface.ToonWars,            "a857f811-2141-4bd9-806d-84c95c81cc00"},
        {Interface.SpaceJet,            "91731a52-540b-4bd8-bf62-fc0ba7f1dafd"},
        {Interface.ApocalypticCars,     "586f0d58-13cd-445d-8d80-052abad5c54b"},
        {Interface.BattleOfWarplanes,   "e2e4e66c-880f-470c-a461-30fa546de577"},
        {Interface.BattleOfHelicopters, "ab137c0e-a584-4a10-829e-e171cc5f1410"},
        {Interface.Armada,              "91517260-a9c0-46b2-bceb-fcec9fccadb7"},
        {Interface.WWR,                 "4af18789-ac67-44f3-be3d-cf33cbe5a353"},
        {Interface.FTRobotsInvasion,    "f5bf6dd3-b517-4442-a7b3-5cc8a19cf648"},
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
    public static readonly ObscuredInt NEWBIE_BATTLES_AMOUNT = 10;
    public static readonly ObscuredInt NEWBIE_VEHICLE_ID = 1;

    public static GameManager Instance { get; private set; }

    public tk2dBaseSprite[] spritesForReloadTexture;

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
            var ver = GameConstants.PHOTON_ROOM_VERSION + Manager.PhotonPostfix;

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
            // TODO: Р·Р°РјРµРЅРёС‚СЊ РЅР° GameData.InterfaceShortName, РєРѕРіРґР° РїРµСЂРµРјРµРЅСѓРµС‚СЃСЏ РІРѕ РІСЃРµС… РїСЂРѕРµРєС‚Р°С….
            if (GameData.IsGame(Game.SpaceJet))
                return "SJ_";

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

#if UNITY_EDITOR
        if (!GameData.ServerDataReceived)
        {
            Loading.gotoLoadingScene();
            return;
        }
#endif
        Instance = this;

        if (HangarController.FirstEnter)
            SpeedHackDetector.StartDetection(OnSpeedHackDetected);

        ReloadTextures();

        Messenger.Subscribe(EventId.BeforeReconnecting, BeforeReconnecting);
        Messenger.Subscribe(EventId.FirstRequestToMaster, OnFirstRequestToMaster);
        Messenger.Subscribe(EventId.NowImMaster, OnImMaster, 2);
        Messenger.Subscribe(EventId.PhotonRoomListReceived, OnRoomListReceived);
        Messenger.Subscribe(EventId.PhotonJoinedRoom, OnPhotonJoinedRoom);
        Messenger.Subscribe(EventId.ProfileMoneyChange, OnProfileMoneyChange);
        Messenger.Subscribe(EventId.JoinRoomFailed, OnJoinRoomFailed);
        Messenger.Subscribe(EventId.TakeYourTeamId, OnTeamIdReceived);
        Messenger.Subscribe(EventId.TankLeftTheGame, OnVehicleLeftTheGame);
        Messenger.Subscribe(EventId.TankJoinedBattle, OnVehicleJoinTheGame);
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

        Messenger.Unsubscribe(EventId.BeforeReconnecting, BeforeReconnecting);
        Messenger.Unsubscribe(EventId.FirstRequestToMaster, OnFirstRequestToMaster);
        Messenger.Unsubscribe(EventId.TakeYourTeamId, OnTeamIdReceived);
        Messenger.Unsubscribe(EventId.NowImMaster, OnImMaster);
        Messenger.Unsubscribe(EventId.PhotonRoomListReceived, OnRoomListReceived);
        Messenger.Unsubscribe(EventId.PhotonJoinedRoom, OnPhotonJoinedRoom);
        Messenger.Unsubscribe(EventId.ProfileMoneyChange, OnProfileMoneyChange);
        Messenger.Unsubscribe(EventId.JoinRoomFailed, OnJoinRoomFailed);
        Messenger.Unsubscribe(EventId.TankLeftTheGame, OnVehicleLeftTheGame);
        Messenger.Unsubscribe(EventId.TankJoinedBattle, OnVehicleJoinTheGame);

        GameSettings.Instance.ClearResourceLinks(); // При смене сцены - для очистки всех ресурсов, подтянут по ссылкам из GameSettings
        Instance = null;
    }

    private void OnPhotonJoinedRoom(EventId id, EventInfo ei)
    {
        PhotonNetwork.FetchServerTimestamp();
        myJoinRoomTime = PhotonNetwork.time;
        if (BattleConnectManager.Instance.FirstConnect)
            BattleController.battleInventory = ConsumablesInventory.GetBattleInventoryDic();

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
        Loading.loadScene(GameData.HangarSceneName);
    }

#if UNITY_EDITOR

    [MenuItem("HelpTools/Reload Textures in scene %#r")]
    public static void ReloadTexturesEditor()
    {
        var gameManager = GameObject.FindObjectOfType<GameManager>();

        if (!gameManager)
            Debug.Log("There is no GameManager component on the scene. Operation aborted.");
        else
            gameManager.ReloadTextures();
    }

#endif

#if UNITY_EDITOR

    [MenuItem("HelpTools/Change Material Quality in scene/Mobile_default")]
    public static void ChangeMaterialQualityEditor_mobile_default()
    {
        ChangeMaterialQuality(MaterialQualityLevel.mobile_default);
    }

    [MenuItem("HelpTools/Change Material Quality in scene/Mobile_max")]
    public static void ChangeMaterialQualityEditor_mobile_max()
    {
        ChangeMaterialQuality(MaterialQualityLevel.mobile_max);
    }

    [MenuItem("HelpTools/Change Material Quality in scene/PC_max")]
    public static void ChangeMaterialQualityEditor_pc_max()
    {
        ChangeMaterialQuality(MaterialQualityLevel.pc_max);
    }

#endif

    public static void ChangeMaterialQuality(MaterialQualityLevel qLvl)
    {
        MeshRenderer[] allSceneObjects = FindObjectsOfType<MeshRenderer>();
        foreach (var rend in allSceneObjects)
        {
            ChangeMaterialQuality(rend, qLvl);
        }
        Debug.Log("Ready!");
    }


    public static void ChangeMaterialQuality(MeshRenderer rend_, MaterialQualityLevel qLvl)
    {
        if (rend_ == null || rend_.sharedMaterial == null)
        {
            return;
        }

        string name_ = string.Format("{0}/Materials/{1}", GameManager.CurrentResourcesFolder, rend_.sharedMaterial.name);
        GetMaterialName(qLvl, ref name_);

        Material mat_ = null;

        if (CheckMaterial(rend_, name_, out mat_))
        {
            rend_.sharedMaterial = mat_;
        }
    }

    private static void GetMaterialName(MaterialQualityLevel qLvl, ref string name_)
    {
        name_.Replace(" (Instance)", string.Empty);

        switch (qLvl)
        {
            case MaterialQualityLevel.mobile_default:
                name_ = name_.Replace("_max", string.Empty);
                name_ = name_.Replace("_Umax", string.Empty);
                break;
            case MaterialQualityLevel.mobile_max:
                name_ = name_.Replace("_Umax", string.Empty);
                name_ += "_max";
                break;
            case MaterialQualityLevel.pc_max:
                name_ = name_.Replace("_max", string.Empty);
                name_ += "_Umax";
                break;
            default:
                break;
        }
    }

    private static bool CheckMaterial(MeshRenderer meshRenderer, string matName, out Material mat)
    {
        var slashIndex = matName.LastIndexOf("/", StringComparison.Ordinal) + 1;
        var realMatName = matName.Substring(slashIndex, matName.Length - slashIndex);
        if (/*meshRenderer.sharedMaterial.shader.name == "Unlit/Transparent" || */meshRenderer.sharedMaterial.name == realMatName)//если шейдер transparent или название материала соответсвует настоящему -> return
        {
            mat = null;
            return false;
        }

        mat = Resources.Load(matName, typeof(Material)) as Material;

        if (mat == null)
        {
            return false;
        }

        return true;
        //MaterialManager.RegisterMaterial(mat);
    }

    public void ConnectToPhoton()
    {
        PhotonNetwork.playerName = ProfileInfo.PlayerName;
        PhotonNetwork.PhotonServerSettings.AppID = photonIds[GameData.CurInterface];

        if (PhotonNetwork.offlineMode)
        {
            MatchMaker.CreateRoom((int)MapId.Battle_SciFiArena, 1);
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
        Messenger.Send(EventId.CheatDetected, new EventInfo_I((int)CheatType.Speedup));
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
                if (roomInfo.Name == BattleConnectManager.Instance.LastRoomName)
                {
                    selectedRoomName = roomInfo.Name;
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

    private static void SetPlayerProperties(VehicleData data, PlayerStat stats)
    {
        PhotonNetwork.player.CustomProperties.Clear();

        Hashtable properties = new Hashtable
        {
            { "hl", (int)data.armor },
            { "at", (int)data.attack },
            { "sp", (float)data.speed },
            { "rt", (float)data.roF },
            { "rg", (int)data.regeneration },
            { "tdr", (float)data.takenDamageRatio },
            { "sc", stats == null ? 0 : stats.score},
            { "dt", stats == null ? 0 : stats.deaths },
            { "kl", stats == null ? 0 : stats.kills },
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
        VehicleData vehicleData;
        if (BattleConnectManager.Instance.FirstConnect)
            vehicleData = new VehicleData(
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
                takenDamageRatio: 1f
                );
        else
        {
            vehicleData = BattleConnectManager.Instance.MyLastVehicleData;
        }
        vehicleData.playerId = PhotonNetwork.player.ID;
        PhotonNetwork.player.SetCustomProperties(new Hashtable { {"ex", true} });

        if (PhotonNetwork.isMasterClient) // Пропустить кадр, чтобы не пытаться писать в кэш фотона в секции обработок его события
            yield return null;

        if (GameData.Mode == GameData.GameMode.Deathmatch) //Если дезматч, то обойдемся без запроса к мастеру
        {
            matchedTeam = 0;
            if (!PhotonNetwork.isMasterClient) // Позволить сначала появиться клонам
                yield return GameConstants.FixedUpdateWaiter;
            else
            {
                while (BotDispatcher.BotsSpawnInProgress)
                    yield return GameConstants.FixedUpdateWaiter;
            }
        }
        else
        {
            #region Отправка первого запроса мастеру и ожидание ответа с номером команды.
            matchedTeam = -2;
            Messenger.Send(EventId.FirstRequestToMaster, new EventInfo_U(vehicleData),
                Messenger.EventTargetType.ToMaster);
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
        }

        Vector3 spawnPosition;
        Quaternion spawnRotation;
        if (string.IsNullOrEmpty(BattleConnectManager.Instance.LastRoomName) || BattleConnectManager.Instance.RespawnAfterReconnect)
        {
            SpawnPoints.SpawnData spawnPoint = SpawnPoints.instance.GetRandomPoint(vehicleData.teamId);
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
                    prefabName: string.Format("{0}/BattleVehicles/{1}", CurrentResourcesFolder, vehiclePrefabName),
                    position: spawnPosition,
                    rotation: spawnRotation,
                    group: 0,
                    data: new object[] { vehicleData })
                .GetComponent<VehicleController>();
        vehicleController.name = "Vehicle_" + PhotonNetwork.player.ID;
        BattleController.SetMainVehicle(vehicleController);
        Messenger.Send(EventId.MainTankAppeared, new EventInfo_SimpleEvent());
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
                           PhotonNetwork.room.IsOpen = true; //комната октрывается здесь
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
                if (room.Name == roomName)
                {
                    suitableRoom = room;
                    break;
                }
            }

            if (suitableRoom == null || !suitableRoom.IsOpen)
            {
                roomName = MatchMaker.SelectRoom(mapId, true, null, out dummy, vehicleGroup);
                JoinRoom(roomName);
                yield break;
            }

            mapName = Enum.Parse(typeof(MapId), suitableRoom.CustomProperties["mp"].ToString()).ToString();
            if (suitableRoom.IsOpen)
                break;

            yield return new WaitForSeconds(1);
        } while (true);

        // Попытка подключения.
        PhotonNetwork.JoinRoom(suitableRoom.Name);

        //Manager.ReportStats (
        //    location: "matchmaker",
        //    action: "joinRoom",
        //    query: new Dictionary<string, string>
        //    {
        //        { "name", suitableRoom.Name },
        //        { "level", suitableRoom.CustomProperties["lv"].ToString() },
        //        { "mapId", suitableRoom.CustomProperties["mp"].ToString() },
        //        { "mapName", mapName },
        //        { "mode", suitableRoom.CustomProperties["gm"].ToString() },
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
        VehicleData data = (VehicleData)info[0];
        int teamId = MatchMaker.GetTeamForNewPlayer(data);
        Messenger.Send(EventId.TakeYourTeamId, new EventInfo_I(teamId), Messenger.EventTargetType.ToSpecific, data.playerId);
    }

    private void OnFirstMasterResponse(EventId id, EventInfo ei)
    {
        EventInfo_U info = (EventInfo_U) ei;
        VehicleData data = (VehicleData) info[0];
    }

    private IEnumerator CheckIfRoomIsOld()
    {
        if (!PhotonNetwork.room.IsVisible)
            yield break;

        YieldInstruction wait = new WaitForSeconds(1);

        double roomCreatedAt = (double)PhotonNetwork.room.CustomProperties["ct"];

        while (PhotonNetwork.isMasterClient)
        {
            double roomAge = (PhotonNetwork.time - roomCreatedAt);

            if (roomAge < 0 || roomAge > GameData.BattleRoomLifetime * 60)
            {
                PhotonNetwork.room.IsVisible = false;
                PhotonNetwork.room.IsOpen = false;
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
        if (PhotonNetwork.connected && PhotonNetwork.isMasterClient)
            RecalcCountryMembers();
    }

    private void OnVehicleJoinTheGame(EventId id, EventInfo ei)
    {
        if (PhotonNetwork.isMasterClient)
            RecalcCountryMembers();
    }

    private void RecalcCountryMembers()
    {
        if (!PhotonNetwork.connected)
            return;

        int[] teamMembers = MatchMaker.CountTeamMembers();
        RoomCountryInfo[] cntr = PhotonNetwork.room.CustomProperties["cntr"] as RoomCountryInfo[];
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

        Messenger.Send(EventId.PhotonDisconnectWithCause, new EventInfo_S(cause));
    }
}
