using System;
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

public class GameManager : MonoBehaviour
{
    public enum MapId
    {
        // IRON TANKS + HANGARS
        LoadingScene = 0, // Индексы не менять!
        scnh_it_standart = 1,
        Battle_SciFiArena = 2,
        White_polygon = 3,
        scnh_ft_standart = 4,
        Battle_PowerPlant = 5,
        Battle_Perron = 6,
        Battle_Dome = 7,
        scnh_it_premium = 8,
        scnh_ft_premium = 9,
        scnh_tw_standart = 10,
        scnh_tw_premium = 11,
        scnh_sj_standart = 12,
        scnh_sj_premium = 13,
        scnh_ac_standart = 14,
        scnh_ac_premium = 15,
        scnh_bw_standart = 16,
        scnh_bw_premium = 17,
        scnh_bh_standart = 18,
        scnh_bh_premium = 19,
        Battle_Cubism = 20,
        scnh_ar_standart = 21,
        scnh_ar_premium = 22,
        scnh_mf_standart = 23,
        scnh_mf_premium = 24,
        scnh_bo_standart = 25,
        scnh_bo_premium = 26,
        scnh_ww_standart = 27,
        scnh_ww_premium = 28,

        // Iron Tanks
        Battle_SciFiArenaNew = 50,

        // FUTURE TANKS
        scnb_mars = 101,
        scnb_moon = 102,
        scnb_outpost = 103,
        scnb_snow = 104,
        scnb_desert = 105,
        scnb_hive = 106,
        scnb_crystallvalley = 107,
        scnb_building17 = 108,
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

        // BLOW OUT
        scnb_bo_arena = 401,
        scnb_bo_arizona = 402,
        scnb_bo_korea = 403,
        scnb_bo_siberia = 404,
        scnb_bo_sinai = 405,
        scnb_bo_wintercity = 406,
        scnb_bo_park = 407,

        // BATTLE OF WARPLANES
        scnb_bw_island = 501,
        scnb_bw_mohave = 502,
        scnb_bw_nyday = 503,
        scnb_bw_mountain = 504,

        // BATTLE OF HELICOPTERS
        scnb_bh_1 = 601,
        scnb_bh_2 = 602,
        scnb_bh_3 = 603,
        scnb_bh_4 = 604,

        //Armada
        scnb_ar_1 = 701,
        scnb_ar_2 = 702,
        scnb_ar_3 = 703,
        scnb_ar_4 = 704,
        scnb_ar_5 = 705,
        scnb_ar_6 = 706,
        scnb_ar_7 = 707,
        scnb_ar_8 = 708,

        //MetalForce
        scnb_mf_arizona = 801,
        scnb_mf_siberia = 802,
        scnb_mf_sinai = 803,
        scnb_mf_arctic = 804,
        scnb_mf_northkorea = 805, // Этой карты сейчас нет.
        scnb_mf_korea = 806,
        scnb_mf_wintercity = 807,

        scnb_ww_island = 901,
        scnb_ww_mohave = 902,
        scnb_ww_nyday = 903,
        scnb_ww_mountain = 904,

        random_map = 1000
    }

    public const string PHOTON_ROOM_VERSION = "v9.1";

    public static Dictionary<Interface, string> photonIds = new Dictionary<Interface, string>()
    {
        {Interface.IronTanks,           "d6c5112d-051e-43ac-929f-ca06c2764708"},
        {Interface.FutureTanks,         "361b2cb5-8e8b-4b5d-8426-6c327df826ff"},
        {Interface.ToonWars,            "a857f811-2141-4bd9-806d-84c95c81cc00"},
        {Interface.SpaceJet,            "91731a52-540b-4bd8-bf62-fc0ba7f1dafd"},
        {Interface.BlowOut,             "586f0d58-13cd-445d-8d80-052abad5c54b"},
        {Interface.BattleOfWarplanes,   "e2e4e66c-880f-470c-a461-30fa546de577"},
        {Interface.WingsOfWar,          ""},//TODO:
        {Interface.BattleOfHelicopters, "ab137c0e-a584-4a10-829e-e171cc5f1410"},
        {Interface.Armada,              "91517260-a9c0-46b2-bceb-fcec9fccadb7"},
        {Interface.MetalForce,          "4af18789-ac67-44f3-be3d-cf33cbe5a353"},
    };

    public const string LOADING_SCENE_NAME = "LoadingScene";
    private const float MAX_ENTER_ROOM_WAITING = 6f;

    public static readonly ObscuredInt BONUS_START_PRICE = 1;
    public static readonly ObscuredInt PROLONG_START_PRICE = 4;
    public static readonly ObscuredFloat NEWBIE_DAMAGE_RATIO = 2f;
    public static readonly ObscuredFloat CRIT_DAMAGE_RATIO = 1.31f;
    public static readonly ObscuredFloat NORM_DAMAGE_RATIO = 0.71f;
    public static readonly ObscuredFloat RANDOM_DAMAGE_RATIO_LOWER_BOUND = 0.9f;
    public static readonly ObscuredFloat RANDOM_DAMAGE_RATIO_UPPER_BOUND = 1.1f;
    public static readonly ObscuredFloat BONUS_PRICE_INCREASE_RATIO = 2.0f;
    public static readonly ObscuredFloat PROLONG_PRICE_INCREASE_RATIO = 2.0f;
    public static readonly ObscuredInt NEWBIE_BATTLES_AMOUNT = 5;
    public static readonly ObscuredInt NEWBIE_VEHICLE_ID = 1;

    public static GameManager Instance { get; private set; }

    private static string vehiclePrefabName;
    private static ObscuredInt mainVehicleId;
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
            return;

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

            if (SystemInfo.deviceType != DeviceType.Handheld) {
                // Setup phisyc for our vehicle
                //You can adjust from here for certain platforms. 24 is a good balance between PC and Mobile platforms.   
                Physics.defaultSolverIterations = 24;
            }
        }
    }

    void OnDestroy()
    {
        BattleConnectManager.RemovePhotonMessageTarget(gameObject);

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
        {
            Dictionary<int, ObscuredInt> dic = new Dictionary<int, ObscuredInt>();
            dic.Merge(ConsumablesInventoryPanel.GetBattleInventoryDic());
            dic.Merge(SuperWeaponsInventoryPanel.GetSuperWeaponsInventoryDic());
            BattleController.battleInventory = dic;
        }
            

        StartCoroutine(OwnFirstSpawn());
    }

    public static void SetMainVehicle(UserVehicle vehicle)
    {
        vehicleUpgrades = vehicle.Upgrades;
        vehiclePrefabName = vehicle.HangarVehicle.name;
        mainVehicleId = vehicle.Info.id;
        vehicleGroup = vehicle.Info.vehicleGroup;
        mainVehicleParameters = vehicle.GetRealParameters();
        mainVehicleCamouflageId = vehicle.Upgrades.CamouflageId;
        mainVehicleDecalId = vehicle.Upgrades.DecalId;
    }

    public static void ReturnToHangar()
    {
        XdevsSplashScreen.SetActiveWaitingIndicator(false);
        Loading.loadScene(GameData.HangarSceneName);
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
            //try to enter the room, showed in MapSelection
            if(RoomInfoManager.RoomsShowedInHangar != null)
                RoomInfoManager.RoomsShowedInHangar[GameData.Mode].TryGetValue((MapId) mapId, out preSelectedRoom);
            selectedRoomName = MatchMaker.SelectRoom(mapId, true, preSelectedRoom, out dummy, vehicleGroup);
        }
        JoinRoom(selectedRoomName);
    }

    private static void SetPlayerProperties(TankData data, PlayerStat stats)
    {
        Hashtable properties = new Hashtable
        {
            { "hl", (int)data.armor },
            { "mar", (int)data.maxArmor },
            { "at", (int)data.attack },
            { "ra", (int)data.rocketAttack },
            { "sp", (float)data.speed },
            { "rf", (float)data.rof },
            { "ir", (float)data.ircmRof },
            { "ex", true },
            { "sc", stats == null ? 0 : stats.score},
            { "dt", stats == null ? 0 : stats.deaths },
            { "kl", stats == null ? 0 : stats.kills },
            { "st", Settings.SerializedTransferableSettings },
            { "rg", (int)data.regeneration },
            { "sh", (int)data.shield },
            { "tdr", (float)data.takenDamageRatio }
        };

        PhotonNetwork.player.SetCustomProperties(properties);
    }

    private void BeforeReconnecting(EventId id, EventInfo ei)
    {
        StopAllCoroutines();
        CancelInvoke();
    }

    private IEnumerator OwnFirstSpawn()
    {
        SocialPlatform playerPlatform = SocialSettings.Platform;
        string playerUid = SocialSettings.GetSocialService().Uid();

        TankData vehicleData;

        if (BattleConnectManager.Instance.FirstConnect)
        {
            vehicleData
                = new TankData(
                    playerName:         ProfileInfo.PlayerName,
                    playerLevel:        ProfileInfo.Level,
                    country:            ProfileInfo.CountryCode,
                    newbie:             ProfileInfo.IsNewbie,
                    socialPlatform:     playerPlatform,
                    socialUID:          playerUid,
                    hangarParameters:   mainVehicleParameters,
                    patternId:          mainVehicleCamouflageId,
                    decalId:            mainVehicleDecalId,
                    teamId:             0,
                    hideMyFlag:         ProfileInfo.isHideMyFlag,
                    profileId:          ProfileInfo.profileId,
                    vip:                ProfileInfo.IsPlayerVip,
                    clanName:           ProfileInfo.Clan != null ? ProfileInfo.Clan.Name : string.Empty,
                    regeneration:       0,
                    shield:             0,
                    takenDamageRatio:   1);
        }
        else
        {
            vehicleData = BattleConnectManager.Instance.MyLastTankData;
        }

        vehicleData.playerId = PhotonNetwork.player.ID;

        PhotonNetwork.player.SetCustomProperties(new Hashtable { {"ex", true} });

        if (BattleConnectManager.IsMasterClient) // Пропустить кадр, чтобы не пытаться писать в кэш фотона в секции обработок его события
            yield return null;

        if (GameData.Mode == GameData.GameMode.Deathmatch) //Если дезматч, то обойдемся без запроса к мастеру
        {
            matchedTeam = 0;
            if (!BattleConnectManager.IsMasterClient) // Позволить сначала появиться клонам
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
            MessageBox.Show(MessageBox.Type.Info, Localizer.GetText("NTPConnectionError"), (MessageBox.Answer _answer) => { ReturnToHangar(); });
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

        // Manually allocate PhotonViewID
        int playerPhotonId = PhotonNetwork.AllocateViewID();
        PhotonView battleView = BattleController.Instance.PhotonView;
        battleView.RPC("VehicleSpawn", PhotonTargets.AllBuffered, 
            vehiclePrefabName, spawnPosition, spawnRotation, playerPhotonId,
            vehicleData, SuperWeaponsInventoryPanel.GetFirstWeaponId(), (int)mainVehicleId
        );

        //VehicleController vehicleController
        //    = PhotonNetwork.Instantiate(
        //            prefabName: string.Format("{0}/{1}", CurrentResourcesFolder, vehiclePrefabName),
        //            position:   spawnPosition,
        //            rotation:   spawnRotation,
        //            group:      0,
        //            data:       new object[]
        //                        {
        //                            vehicleData,
        //                            // Чтобы не ломать vehicleData:
        //                            SuperWeaponsInventoryPanel.GetFirstWeaponId(),
        //                            (int)mainVehicleId
        //                        })
        //                .GetComponent<VehicleController>();

        //vehicleController.name = "Vehicle_" + PhotonNetwork.player.ID;

        //BattleController.SetMainVehicle(vehicleController);

        //Dispatcher.Send(EventId.MainTankAppeared, new EventInfo_SimpleEvent());
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
        if (!BattleConnectManager.IsMasterClient)
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
        if (!PhotonNetwork.room.IsVisible)
            yield break;

        YieldInstruction wait = new WaitForSeconds(1);

        double roomCreatedAt = (double)PhotonNetwork.room.CustomProperties["ct"];

        while (BattleConnectManager.IsMasterClient)
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
        if (BattleConnectManager.IsMasterClient)
            RecalcCountryMembers();
    }

    private void OnVehicleJoinTheGame(EventId id, EventInfo ei)
    {
        if (BattleConnectManager.IsMasterClient)
            RecalcCountryMembers();
    }

    private void RecalcCountryMembers()
    {
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

        Dispatcher.Send(EventId.PhotonDisconnectWithCause, new EventInfo_S(cause));
    }
}