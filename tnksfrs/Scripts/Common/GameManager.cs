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
using XD;
using Hashtable = ExitGames.Client.Photon.Hashtable;
using XD.ExternalPlatforms;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class GameManager : MonoBehaviour, IGameManager
{
    private Dictionary<Interface, Dictionary<bool, string>> photonIds = new Dictionary<Interface, Dictionary<bool, string>>()
    {
        {
            Interface.Armada2, new Dictionary<bool,string>()
            {
                { false, "da95abbf-9b7c-46da-be3d-9642c3c60cfc" }, // NORMAL_PHOTON_APPLICATION_ID
				{ true, "da95abbf-9b7c-46da-be3d-9642c3c60cfc" }   // SPECIAL_PHOTON_APPLICATION_ID - for cheaters
			}
        }
    };

    private const string                                            PHOTON_ROOM_VERSION = "v7.3";
    private const string                                            DEBUG_PHOTON_ROOM_VERSION_POSTFIX = "d";
    private const string                                            ROOM_TEST_VERSION = "v8.0";

    private int                                                     unitID = -1;
    private bool                                                    hangarLoaded = false;
    private string                                                  vehiclePrefabName = "";
    private ObscuredInt                                             mainUnitCamouflageId = new ObscuredInt();
    private ObscuredInt                                             mainUnitDecalId = new ObscuredInt();
    private ObscuredFloat                                           missileAimingDuration = 4.0f;
    private VehicleUpgrades                                         unitUpgrades = null;
    private XD.Settings                                             unitSettings = null;

    private bool                                                    isRoomCreator;
    private bool                                                    joinFailed;
    private bool                                                    battleOptionsReceived;
    private int                                                     matchedTeam = -1;
    private int                                                     mapId = 0;

    private double                                                  timeInRoom = -1;

    public double TimeInRoom
    {
        get
        {
            return timeInRoom;
        }
    }

    public int Team
    {
        get
        {
            return matchedTeam;
        }

        set
        {
        }
    }

    public bool BattleOptionsReceived
    {
        get
        {
            return battleOptionsReceived;
        }

        set
        {
            battleOptionsReceived = value;
        }
    }

    public string PhotonRoomVersion
    {
        get
        {
            return StaticType.DevelopmentData.Instance<IDevelopmentData>().PhotonVersion;
        }
    }

    public string CurrentResourcesFolder
    {
        get
        {
            return GameData.CurInterface.ToString();
        }
    }

    public int CurrentUnitID 
    {
        get
        {
            return unitID;
        }
    }

    public MapId CurrentMap
    {
        get
        {
            return (MapId)Enum.Parse(typeof(MapId), SceneManager.GetActiveScene().name);
        }
    }

    public float MissileAimingDuration
    {
        get
        {
            return missileAimingDuration;
        }
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

    private void Awake()
    {
        if (!StaticContainer.Get(StaticType).IsEmpty)
        {
            Destroy(this);
            return;
        }
        
        DontDestroyOnLoad(this);        
        SaveInstance();

        if (SceneManager.GetActiveScene().name == Constants.LOADING_SCENE_NAME)
        {
            return;
        }

        if (SceneManager.GetActiveScene().name.StartsWith("Hangar_")) // ебаненько
        {
            hangarLoaded = true;
        }

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
        Init();

        AddSubscriber(StaticType.UI.Instance());
        //AddSubscriber(StaticType.Statistics.Instance());
        AddSubscriber(StaticType.MusicDispatcher.Instance());
        AddSubscriber(StaticType.BattleEventQueue.Instance());

        StaticType.SceneManager.AddSubscriber(this);
        StaticType.StaticContainer.AddSubscriber(this);        
    }    

    private void Init()
    {
        StaticContainer.Connector.AddPhotonMessageTarget(gameObject);

        if (SceneManager.GetActiveScene().name == Constants.LOADING_SCENE_NAME)
        {
            return;
        }

        if (HangarController.FirstEnter)
        {
            SpeedHackDetector.StartDetection(OnSpeedHackDetected);
        }

        mapId = (int)CurrentMap;

        if (StaticContainer.SceneManager.InBattle)
        {
            Manager.Instance().battleServer.PrepareToBattle();
            ConnectToPhoton();
        }
    }

    private void OnDestroy()
    {
        DeleteInstance();       
        
        StaticContainer.Connector.RemovePhotonMessageTarget(gameObject);

        /*foreach (tk2dBaseSprite sprite in spritesForReloadTexture)
        {
            sprite.Collection.UnloadTextures();
        }*/

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
        //Dispatcher.Unsubscribe(EventId.OnExitToHangar, OnExitToHangar);
    }

    /* PHOTON SECTION */

    private void OnPhotonJoinedRoom(EventId id, EventInfo ei)
    {
        PhotonNetwork.FetchServerTimestamp();
        timeInRoom = PhotonNetwork.time;
        StartCoroutine(OwnFirstSpawn());
    }
    
    public void SetMainVehicle(IUnitHangar vehicle)
    {
        unitUpgrades = vehicle.Upgrades;
        unitSettings = vehicle.Settings.Clone();
        unitID = vehicle.ID;
        vehiclePrefabName = vehicle.Prefab;

        ;
        
        mainUnitCamouflageId = vehicle.Upgrades.CamouflageId;
        mainUnitDecalId = vehicle.Upgrades.DecalId;
    }

    public void SetMainVehicle(IUnitBattle unit)
    {
        Debug.LogFormat("Try SetMainVehicle as IVehicleBattle in GameManager - '{0}'".FormatString("color:cyan"), unit.Name);
        unitUpgrades = unit.Upgrades;
        unitSettings = unit.Settings.Clone();
        unitID = unit.ID;
        vehiclePrefabName = unit.Prefab;        
        mainUnitCamouflageId = unit.Upgrades.CamouflageId;
        mainUnitDecalId = unit.Upgrades.DecalId;
        StaticContainer.MainData.AddUsingUnit(unit);

        if (StaticContainer.SceneManager.InBattle)
        {
            Event(Message.LayoutRequest, ARPage.Battle);
            Event(Message.UnitBattleGot, PhotonNetwork.player.NickName, unit, unit.Picture, Team, unit.UnitClass, unit.ID);
        }
    }

    public static Consumables GetInstalledConsumables(int vehID)
    {
        IUnitHangar veh = StaticContainer.MainData.GetUnitHangar(vehID);
        if (veh == null)
        {
            Debug.LogError("Consumables == NULL, " + vehID);
            return null;
        }

        return veh.InstalledConsumables;
    }    

    public void ConnectToPhoton()
    {
        ColoredDebug.Log("[" + name + " ConnectToPhoton! Offline: " + PhotonNetwork.offlineMode + "; Connected:  " + PhotonNetwork.connected + "]", this, "cyan");
        PhotonNetwork.playerName = ProfileInfo.PlayerName;
        PhotonNetwork.PhotonServerSettings.AppID = photonIds[GameData.CurInterface][false];

        if (PhotonNetwork.offlineMode)
        {
            MatchMaker.CreateRoom((int)MapId.Battle_SciFiArena, 1);
            return;
        }

        try
        {
            if (!PhotonNetwork.connected)
            {
                PhotonNetwork.ConnectUsingSettings(PhotonRoomVersion);
            }
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
        if (randomRoutine != null)
        {
            StopCoroutine(randomRoutine);
        }

        randomRoutine = StartCoroutine(WaitForRandomTime(new Clamper(0.5f, 4f)));
    }

    private Coroutine randomRoutine = null;

    private IEnumerator WaitForRandomTime(Clamper time)
    {
        float wait = time.RandomValue();
        //Debug.LogErrorFormat("{0} wait for room received {1}", name, wait);
        yield return new WaitForSeconds(wait);

        int roomMapCount = 0;
        
        RoomInfo[] rooms = PhotonNetwork.GetRoomList();
        for (int i = 0; i < rooms.Length; i++)
        {
            if (rooms[i].CustomProperties.ContainsKey("mp"))
            {
                if ((int)rooms[i].CustomProperties["mp"] == (int)CurrentMap)
                {
                    roomMapCount++;
                }
            }
        }

        if (roomMapCount == 0)
        {
            wait = time.RandomValue();
            //Debug.LogErrorFormat("{0} wait for room received {1}", name, wait);
            yield return new WaitForSeconds(wait);
        }

        if (BattleController.PlayerInBattle && StaticContainer.Connector.FirstConnect)
        {
            StaticType.BattleController.Instance<IBattleController>().EndBattle(EndBattleCause.AlreadyInBattle);
            yield break;
        }

        PhotonNetwork.FetchServerTimestamp();
        string selectedRoomName = null;

        #region Поиск по имени комнаты для возврата
        if (!string.IsNullOrEmpty(StaticContainer.Connector.LastRoomName))
        {
            RoomInfo[] roomInfos = PhotonNetwork.GetRoomList();
            foreach (var roomInfo in roomInfos)
            {
                if (roomInfo.Name == StaticContainer.Connector.LastRoomName)
                {
                    selectedRoomName = roomInfo.Name;
                    break;
                }
            }
        }
        #endregion

        int dummy;
        string preSelectedRoom = null;

        if (RoomInfoManager.SelectedRooms != null)
        {
            RoomInfoManager.SelectedRooms.TryGetValue((MapId)mapId, out preSelectedRoom);
        }

        selectedRoomName = MatchMaker.SelectRoom(mapId, true, preSelectedRoom, out dummy, StaticType.MainData.Instance<IMainData>().Group);
        JoinRoom(selectedRoomName);
    }

    private static void SetPlayerProperties(TankData data, PlayerStat stats)
    {
        Hashtable properties = new Hashtable
        {
            { "hl", (int)data.armor },
            { "at", (int)data.attack },
            { "uid", (int)data.unitId },
            { "sp", data.movingSpeed },
            { "tsp", data.turretSpeed },
            { "rf", data.rof },
            { "ir", data.ircmRof },
            { "da", data.DamageAbsorption },
            { "dap", data.DamageAbsorptionProbability },
            { "sc", stats == null ? 0 : stats.Stats[StatisticParameter.Experience]},
            { "dt", stats == null ? 0 : stats.Stats[StatisticParameter.Deaths] },
            { "kl", stats == null ? 0 : stats.Stats[StatisticParameter.Kills] },
            { "dm", stats == null ? 0 : stats.Stats[StatisticParameter.Damage] }
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

        string playerUid = "";
        if (StaticType.SocialSettings.Instance<ISocialSettings>().GetSocialService() != null)
        {
            playerUid = /*StaticType.PlatformsFactory.Instance<IPlatformsFactory>().GetLastPlatformActive.UserId;*/ StaticType.SocialSettings.Instance<ISocialSettings>().GetSocialService().Uid();
        }

        /*if (PlatformsFactory.lastPlatformActive != null)
        {
            playerUid = PlatformsFactory.lastPlatformActive.UserId;  //StaticType.SocialSettings.Instance<ISocialSettings>().GetSocialService().Uid();
        }*/

        TankData vehicleData;
        PhotonNetwork.player.SetCustomProperties(new Hashtable { {"ex", true} });
        IUnitBattle unitBattle = (IUnitBattle)StaticContainer.MainData.GetUnitHangar(unitID);

        if (StaticContainer.Connector.FirstConnect)
        {
            vehicleData = new TankData(
                playerName: ProfileInfo.PlayerName,
                playerLevel: StaticType.Profile.Instance<IProfile>().LevelCalculator.Level,
                country: ProfileInfo.CountryCode,
                newbie: ProfileInfo.IsNewbie,
                socialPlatform: playerPlatform,
                socialUID: playerUid,
                parameters: unitBattle.Settings.Clone(),
                patternId: mainUnitCamouflageId,
                decalId: mainUnitDecalId,
                teamId: 0,
                hideMyFlag: ProfileInfo.isHideMyFlag,
                innerId: ProfileInfo.playerId,
                vip: ProfileInfo.IsPlayerVip,
                clanName: ProfileInfo.Clan != null ? ProfileInfo.Clan.Name : string.Empty,
                unitBattle: unitBattle);
        }
        else
        {
            vehicleData = BattleConnectManager.Instance.MyLastTankData;
        }

        vehicleData.playerId = PhotonNetwork.player.ID;

        if (PhotonNetwork.isMasterClient) // Пропустить кадр, чтобы не пытаться писать в кэш фотона в секции обработок его события
        {
            yield return null;
        }

        if (GameData.Mode == GameData.GameMode.Deathmatch) //Если дезматч, то обойдемся без запроса к мастеру
        {
            matchedTeam = 0;
            if (!PhotonNetwork.isMasterClient) // Позволить сначала появиться клонам
            {
                yield return new WaitForFixedUpdate();
            }
        }
        else
        {
            #region Отправка первого запроса мастеру и ожидание ответа с номером команды.
            matchedTeam = -2;
            Dispatcher.Send(EventId.FirstRequestToMaster, new EventInfo_U(vehicleData), Dispatcher.EventTargetType.ToMaster);
            float startAnswerWaiting = Time.time;
            
            while (matchedTeam < 0)
            {
                if (matchedTeam == -1) // Не пустил мастер
                {
                    DisconnectFor("NoPlaceInRoomForPlayer");
                    StaticContainer.Connector.ForcedDisconnect();
                    yield break;
                }

                if (Time.time - startAnswerWaiting >= Constants.MAX_ENTER_ROOM_WAITING) // Истекло время ожидания ответа от мастера
                {
                    DisconnectFor("NoResponseFromMaster");
                    StaticContainer.Connector.ForcedDisconnect();
                    yield break;
                }

                yield return null;
            }
            #endregion
        }

        #region Отправка серверу сигнала о входе в комнату и ожидания ответа с боевыми опциями
        if (StaticContainer.Profile.BattleTutorialCompleted)
        {
            if (StaticContainer.Connector.FirstConnect)
            {
                StartBattleReport(GameData.isBotsEnabled);
            }
            else
            {
                Debug.LogError("It is not first connect!");
            }

            while (!BattleOptionsReceived)
            {
                yield return null;
            }

            if (StaticContainer.Connector.FirstConnect && (GameData.isBotsEnabled || BattleController.CheckPlayersCount()))
            {
                Manager.Instance().battleServer.StartTimer((int) (PhotonNetwork.time - timeInRoom));
            }
        }
        
        #endregion
        vehicleData.teamId = matchedTeam;
        if (PhotonNetwork.room == null)
        {
            GameData.CriticalError("UI_MB_NTPConnectionError");
            yield break;
        }

        SetPlayerProperties(vehicleData, BattleConnectManager.Instance.MyLastPlayerStat);
        GameObject point = GameObject.FindGameObjectWithTag("CameraBehaviourPoint");
        if (point == null)
        {
            point = new GameObject("CameraBehaviuorPoint");
        }

        Instantiate(Resources.Load("System/CameraBehaviour"), point.transform.position, point.transform.rotation);
        
        if (PhotonNetwork.offlineMode || GameData.Mode == GameData.GameMode.Deathmatch)
        {
            CreateVehicle(vehicleData, unitID);
            SetMainVehicle(StaticContainer.MainData.GetUnitBattle(unitID));
        }
        else
        {
            StaticContainer.StaticFactory.Create(StaticType.Master, null, true);            
        }
    }

    public void CreateVehicle(TankData vehicleData, int vehID, bool onClick = true)
    {
        Vector3 spawnPosition;
        Quaternion spawnRotation;
        if (string.IsNullOrEmpty(StaticContainer.Connector.LastRoomName) || StaticContainer.Connector.RespawnAfterReconnect || onClick)
        {
            Transform spawnPoint = SpawnPoints.instance.GetRandomPoint(vehicleData.teamId);
            spawnPosition = spawnPoint.position;
            spawnRotation = spawnPoint.rotation;
        }
        else
        {
            spawnPosition = StaticContainer.Connector.MyLastPosition;
            spawnRotation = StaticContainer.Connector.MyLastRotation;
        }

        VehicleController vehicleController
            = PhotonNetwork.Instantiate(
                    prefabName: string.Format("{0}/{1}", StaticContainer.GameManager.CurrentResourcesFolder + "/BattleVehicles", vehiclePrefabName),
                    position: spawnPosition,
                    rotation: spawnRotation,
                    group: 0,
                    data: new object[] { vehicleData }).GetComponent<VehicleController>();

        vehicleController.name = "Vehicle_" + PhotonNetwork.player.ID + "_" + vehicleController.Data.UnitBattle.Name;
        
        StaticType.BattleController.Instance<IBattleController>().SetMainVehicle(vehicleController);
        Dispatcher.Send(EventId.MainTankAppeared, new EventInfo_SimpleEvent());
        StartCoroutine(SendEventRoutine(vehicleController));

        BattleConnectManager.Instance.MyLastPlayerStat = vehicleController.Statistics;
        SetPlayerProperties(vehicleData, BattleConnectManager.Instance.MyLastPlayerStat);
        //Debug.LogError("CreateVehicle: " + vehicleController.name + ", hp: " + vehicleController.Settings[Setting.HP] + ", dataHP: " + vehicleData.maxArmor + ", dataCurHP: " + vehicleData.armor);
    }

    private IEnumerator SendEventRoutine(IUnitBehaviour unit)
    {
        yield return new WaitForEndOfFrame();
        Event(Message.UnitBattleCreated, unit);
    }

    // Matchmaker itself.
    
    public void JoinRoom(string roomName)
    {
        if (string.IsNullOrEmpty(roomName))
        {
            return;
        }

        StartCoroutine(JoinRoom_Coroutine(roomName));
    }

    public void StartBattleReport(bool roomWasFulled)
    {
        Manager.Instance().battleServer.StartBattle(
               room: PhotonNetwork.room,
               vehicleUpgrades: unitUpgrades,
               vehicleParameters: unitSettings,
               isCreateRoom: isRoomCreator,
               roomWasFulled: roomWasFulled,
               result: delegate (bool result)
               {
                   if (result)
                   {
                       battleOptionsReceived = true;
                       if (isRoomCreator)
                       {
                           PhotonNetwork.room.IsOpen = true; //TODO !!!комната октрывается здесь
                       }
                   }
                   else
                   {
                       battleOptionsReceived = false;
                       StaticContainer.Connector.ForcedDisconnect();
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
                roomName = MatchMaker.SelectRoom(mapId, true, null, out dummy, StaticType.MainData.Instance<IMainData>().Group);
                JoinRoom(roomName);
                yield break;
            }

            mapName = Enum.Parse(typeof(MapId), suitableRoom.CustomProperties["mp"].ToString()).ToString();
            if (suitableRoom.IsOpen)
            {
                break;
            }

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
        {
            yield return null;
        }

        if (!joinFailed)
        {
            #region Google Analytics: joining battle

            GoogleAnalyticsWrapper.LogEvent(
                new CustomEventHitBuilder()
                    .SetParameter(GAEvent.Category.JoinBattle)
                    .SetParameter<GAEvent.Action>()
                    .SetSubject(GAEvent.Subject.MapName, mapName)
                    .SetParameter<GAEvent.Label>()
                    .SetSubject(GAEvent.Subject.VehicleID, ProfileInfo.CurrentUnit)
                    .SetValue(StaticType.Profile.Instance<IProfile>().LevelCalculator.Level));

            GoogleAnalyticsWrapper.LogEvent(
                new CustomEventHitBuilder()
                    .SetParameter(GAEvent.Category.JoinBattle)
                    .SetParameter<GAEvent.Action>()
                    .SetSubject(GAEvent.Subject.MapName, mapName)
                    .SetParameter<GAEvent.Label>()
                    .SetSubject(GAEvent.Subject.PlayerLevel, StaticType.Profile.Instance<IProfile>().LevelCalculator.Level)
                    .SetValue(Convert.ToInt64(0)));

            GoogleAnalyticsWrapper.LogEvent(
                new CustomEventHitBuilder()
                    .SetParameter(GAEvent.Category.JoinBattle)
                    .SetParameter<GAEvent.Action>()
                    .SetSubject(GAEvent.Subject.MapName, mapName)
                    .SetParameter<GAEvent.Label>()
                    .SetSubject(GAEvent.Subject.GameMode, GameData.Mode)
                    .SetValue(StaticType.Profile.Instance<IProfile>().LevelCalculator.Level));

            GoogleAnalyticsWrapper.LogScreen(GAScreens.Battle);
            #endregion
        }
        else
        {
            joinFailed = false;
            roomName = MatchMaker.SelectRoom(mapId, true, null, out dummy, StaticType.MainData.Instance<IMainData>().Group);
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
        {
            return;
        }

        EventInfo_U info = (EventInfo_U)ei;
        TankData data = (TankData)info[0];
        int teamId = MatchMaker.GetTeamForNewPlayer(data);
        ColoredDebug.Log(name + " OnFirstRequestToMaster: [" + teamId + "]", this, "orange");
        Dispatcher.Send(EventId.TakeYourTeamId, new EventInfo_I(teamId), Dispatcher.EventTargetType.ToSpecific, data.playerId);
    }
    /*
    private void OnExitToHangar(EventId id, EventInfo info)
    {
        StaticContainer.MainData.OnReturnFromBattle();
    }*/

    private void OnFirstMasterResponse(EventId id, EventInfo ei)
    {
        EventInfo_U info = (EventInfo_U) ei;
        TankData data = (TankData) info[0];
    }

    private IEnumerator CheckIfRoomIsOld()
    {
        if (!PhotonNetwork.room.IsVisible)
        {
            yield break;
        }

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
        ColoredDebug.Log("[" + name + " OnJoinRoomFailed!]", this, "yellow");
        joinFailed = true;
    }

    private void OnVehicleLeftTheGame(EventId id, EventInfo ei)
    {
        if (PhotonNetwork.isMasterClient)
        {
            RecalcCountryMembers();
        }
    }

    private void OnVehicleJoinTheGame(EventId id, EventInfo ei)
    {
        if (PhotonNetwork.isMasterClient)
        {
            RecalcCountryMembers();
        }
    }

    private void RecalcCountryMembers()
    {
        if (!PhotonNetwork.connected)
        {
            return; 
        }

        int[] teamMembers = MatchMaker.CountTeamMembers();
        
        RoomCountryInfo[] cntr = PhotonNetwork.room.CustomProperties["cntr"] as RoomCountryInfo[];
        for (int i = 0; i < cntr.Length; i++)
        {
            cntr[i].players = teamMembers[i];
        }

        if (PhotonNetwork.room.IsOpen)
        {
            PhotonNetwork.room.SetCustomProperties(new Hashtable { { "cntr", cntr } });
        }
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
            return StaticType.GameManager;
        }
    }

    public void SaveInstance()
    {
        StaticContainer.Set(StaticType, this);
    }

    public void DeleteInstance()
    {
        if (StaticContainer.Get(StaticType) == this)
        {
            StaticContainer.Set(StaticType, null);
        }
    }
    #endregion

    #region ISender
    public string Description
    {
        get
        {
            return "[GameManager] " + name;
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
        //Debug.Log(Description + " Reaction on " + message);
        switch (message)
        {
            case Message.LoadMapComplete:
                Init();
                break;

            case Message.StaticInit: 
                break;
        }
    }

    #endregion
}