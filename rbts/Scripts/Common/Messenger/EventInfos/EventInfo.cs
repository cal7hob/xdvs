using System;
using System.Text;
using ExitGames.Client.Photon;
using UnityEngine;

/*
    Types designation (in necessary order):
 *
 * I	- int (enums also)
 * F	- float
 * D	- double
 * B    - bool
 * S	- string
 * V3	- Vector3
 * E	- TankEffect
 * U    - object[] - universal (exceptional cases only)
*/

public enum CheatType
{
    None			    = 0,
    Speedup			    = 1,
    ObscuredValueChange = 2,
}

public enum EventId
{
    /// <summary>
    /// Simple game events (without parameters) (10..99) - EventInfo_SimpleEvent
    /// </summary>
    AfterLocalizationLoad				= 10,
    AfterHangarInit						= 11,
    MainTankAppeared					= 12,
    TouchableAreaChanged                = 13,
    AfterFacebookMainInfoLoaded         = 14,
    LeftRoom 							= 15,
    ProfileInfoLoadedFromServer			= 16,
    OnLanguageChange					= 17,
    AfterFacebookInitialized			= 18,
    ServerDataReceived					= 19,
    AfterWebPlatformDefined				= 20,
    FriendInviteSuccess                 = 21,
    TeamScoreChanged					= 22,
    WindowModeChanged                   = 23,
    WeeklyAwardsChanged                 = 24,
    GameModeChanged						= 25,
    MyTankRespawned						= 26,
    PageChanged                         = 27, // on GUIPager.SetActivePage() event
    TroubleDisconnect		    		= 28,
    PlayerFled							= 29,
    ScoresBoxActivated                  = 30,
    NickNameChanged                     = 31,
    GameProlonged						= 32,
    VKAndroidInitialized                = 33,
    OnExitToHangar						= 34, // Before load hangar
    SettingsSubmited                    = 35,
    ClanChanged                         = 36,
    FlagSettingsChanged                 = 37,
    AvatarSettingsChanged               = 38,
    ScoresHighlightedItemsReady         = 39,
    ResolutionChanged                   = 40,
    BankInitialized                     = 41,
    VehicleShopFilled                   = 42,
    PatternShopFilled                   = 43,
    DecalShopFilled                     = 44,
    SpecialOffersInitialized            = 45,
    GameUpdateRequired                  = 46,
    PhotonRoomCustomPropertiesChanged   = 47,
    ChangeZoomState                     = 48,
    ModuleReceived                      = 49,  
    WentToBattle                        = 50,
    GameCenterDisabled                  = 51, // Отправляется на тех платформах на которых нет гейм центра, например на винде, чтобы отключить кнопки вызова                                                гуи ачивок и лидербордов.
    TutorialOrderOverriden              = 52,
    TutorialsInitialized                = 53,
    VipOffersInstantiated               = 54,
    MyVehicleCrashing                   = 55,
    //SACLOSReloaded                      = 56, // Deprecated, see WeaponReloaded
    BattleGUIIntialized                 = 57,
    //todo: replace deleted eventid here
    ShopInfoLoadedFromServer            = 59,
    //IRCMReloaded                        = 60, // Deprecated, see WeaponReloaded
    IRCMLaunched                        = 61,
    PhotonRoomListReceived              = 62,
    PhotonJoinedRoom                    = 63,
    BeforeReconnecting                  = 64,
    NickNameManuallyChanged             = 65,
    SACLOSLaunched                      = 66,
    QualitySettingsChanged              = 67, // Отличие от QualityLevelChanged: вызывается после переключения материалов.
    ShopManagerItemsLoaded              = 68,
    JoinRoomFailed                      = 69,
    MasterDisconnectAnswer              = 70,
    RoomBusyListTrimmed                 = 71,
    PlayerLevelChanged                  = 72,//Когда с сервера пришел опыт, и оказалось что уровень игрока вырос / ну или снизился )
    BattleQuestUpdated                  = 73, // Произошли изменения в боевом квесте (прогресс, признак выполнения)
    TeamChange                          = 74, // Изменился состав команд 
    FriendsScoresHighlightedItemsReady  = 75,
    BattleSettingsSubmited              = 76,
    BattleTutorialSkipping              = 77,
    OnReadyToStartWindowsQueue          = 78,
    ProfileServerChoosed                = 79,
    MainVehWeaponReloaded               = 80,
    RewardedVideoClicked                = 81,
    FullCalcBattleStatistics            = 82, //Когда общая статистика расчитана
    SoundVolumeChanged                  = 83,
    VIPAccountPurchased                 = 84,

    /// <summary>
    /// Int (100..199) - EventInfo_I
    /// </summary>
    TankJoinedBattle                    = 100, // playerId
    VehicleRespawned				    = 101, // playerId
    TankOutOfTime                       = 102, // playerId
    PlayerKickout						= 103, // playerId
    GoldAcquired						= 104, // amount
    SilverAcquired						= 105, // amount
    ExperienceAcquired					= 106, // amount
    VehicleSelected                     = 107, // vehicleId
    FuelAcquired						= 108, // amount
    TankLeftTheGame						= 109, // playerId
    CheatDetected						= 110, // whattaFuckId
    TakeYourTeamId                      = 111, // teamId for player
    FuelUpdated                         = 112, // new fuel amount
    MaxFuelUpdated                      = 113, // new max fuel
    VipTimerUpdated                     = 114, // vip account expiration time updated
    GoldRushPlayerStakes				= 115, // stake (gold)
    RevengeDone							= 116, // victimId - I have revenge!
    HangarTimerTick                     = 117, // (int)GameData.instance.GetCorrectedTime ()
    QualityLevelChanged                 = 118, // new quality level
    MyTankShoots                        = 119, // shellId
    BodyKitSelected                     = 120, // bodyKitId
    CamouflageBought                    = 121, // camoId
    VehicleBought                       = 122, // vehicleId
    VehicleInstalled                    = 123, // vehicleId
    ModuleBought                        = 124, // moduleId
    NewPlayerConnected                  = 125, // playerId
    PlayerDisconnected                  = 126, // playerId
    VehicleCrashing                     = 127, // playerId
    VoiceRequired                       = 128, // voice event id 
    TutorialIndexChanged                = 129, // completed tutorial index
    BattleLessonAccomplished            = 130, // completed battle lesson enum
    BattleEnd                           = 131, // (int)endBattleCause
    QuestCompleted                      = 132, // (int)curentQuest.type
    NormalDisconnectNotice              = 133, // playerId
    PatternSelected                     = 134, // patternId
    WeaponOverheated                    = 135, // (int)GunShellInfo.ShellType
    PatternExpired                      = 136, // patternId
    DecalExpired                        = 137, // decalId
    TankShotMissed                      = 138, // attackerId
    StartTurretRotation                 = 139, // playerId
    StopTurretRotation                  = 140, // playerId
    BattleLessonStarted                 = 141, // started battle lesson enum
    HangarVehicleGeometryLoaded         = 142, // vehicleId
    ConsumableBought                    = 143, // consumableId
    VIPConsumableClicked                = 144, // consumableId

    /// <summary>
    /// Int, Int (200..299) - EventInfo_II
    /// </summary>
    VehicleKilled                       = 200, // deadId, attackerId
    TryingTakeItem						= 201, // itemId, playerId
    TankHealthChanged					= 202, // playerId, newHealth
    ProfileMoneyChange					= 203, // new gold, new silver
    TankAvailabilityChanged             = 204, // playerId, state (0/1)
    BonusDestroyed                      = 205, // (int)BonusType, bonus id from photon
    EngineStateChanged                  = 206, // playerId, (int)engineState
    NewLayerInTeamMask                  = 207, // teamId, layer id
    OffLayerInTeamMask                  = 208, // teamId, layer id
    VehicleRotationStateChanged         = 209, // playerId, (int)rotationState
    ConsumableUsed                      = 210, // player id, consumableId

    /// <summary>
    /// Int, Int, Vector3 (300..399) - EventInfo_IIV
    /// </summary>
    HelicopterKilled                    = 300, // victimId, attackerId, position

    //Int, Vector3 (400..499) - EventInfo_IV

    /// <summary>
    /// Int, int, int (500 - 599) - EventInfo_III
    /// </summary>
    ItemTaken							= 500, // (int)bonusType, amount, playerId (who gets item)
    SecondaryWeaponUsed                 = 501, // playerId, weaponId, shellId

    //Int, Int, Int, Vector3 (600 - 699) - EventInfo IIIV
    // ...

    // string, string (700 - 799) - EventInfo_S
    ShowNextPage						= 700, // str1 = "previous page, next page"
    PhotonDisconnectWithCause           = 701, // str1 - disconnect cause
    
    // int, int, int, int (800 - 899) - EventInfo_IIII
    
    /// <summary>
    /// bool (900 - 999) - EventInfo_B
    /// </summary>
    NowImMaster				            = 900, // iAmRoomCreator
    VipStatusUpdated                    = 901, // new player vip status
    StatTableVisibilityChange           = 902,
    MessageBoxChangeVisibility          = 904,
    IRCMLaunchRequired                  = 906,
    MissileThreat                       = 907,
    SACLOSLaunchRequired                = 908,
    MapSelectionAppeared                = 909, // true - appear, false - disappear
    ZoomStateChanged                    = 910, // zoom state
    HighPingAlarm                       = 911, // true - enabled, false - disabled
    BtnBackInBattleChangeVisibility     = 912,
    NotifierChangeVisibility            = 913,
    BattleSettingsChangeVisibility      = 914,
    BattleChatCommandsChangeVisibility  = 915,
    IsMainCameraSighted                   = 916,
    
    /// <summary>
    /// int, bool (1200 - 1299) - EventInfo_IB
    /// </summary>
    SACLOSAimed = 1200,
    ChangeConsumableInventoryState      = 1201,//int - id расходки, bool - новый статус (true - вставить в инвентарь, false - убрать)
    BurstFireStateChanged               = 1202, // playerId, state

    /// <summary>
    /// int, int, int, Vector3 (1300 - 1399) - EventInfo IIIIV
    /// </summary>
    ShellHit                            = 1300, // victim, damage, owner, (int)damageSource, hit local position (in victim coordinates)
    PortionOfDamage                     = 1301, // victim, damage, attacker, (int)damageSource, position

    /// <summary>
    /// bool, int, int, int, int (1400 - 1499) - EventInfo BIIII
    /// </summary>
    ShellStateChanged                   = 1400,  // active, victim, owner, gun shell type, id

    /// <summary>
    /// object[] (1500 - 1699) - EventInfo_U (универсальный).
    /// </summary>
    // Для всех типов, сериализация/десериализация которых зарегана в фотоне.
    // Применять с осторожностью и только в том случае, если не подходит ни один из вышеперечисленных наборов параметров.
    FirstRequestToMaster                = 1500,
    VehicleTakesDamage                  = 1501, // victim, damage, attacker, (int)damageSource, position (Vector3)
    BattleChatCommand                   = 1502,
    ChangeElementStateRequest           = 1503,// class ChangeElementStateRequestInfo
    /// <summary>
    /// Don't use directly! Use VehicleController.EffectRequest instead.
    /// </summary>
    VehicleEffectRequest                = 1504, //playerId, effectData /Запрос наложения эффекта.
    DeadZoneObjectStateChanged          = 1505, // IDeadZone deadZoneObject, bool state
    DiscountStateChanged                = 1506, // EntityType, object id, bool state

    /// <summary>
    /// object[] (1700 - 1799) - EventInfo_IIB
    /// </summary>
    TargetAimed = 1700,	// int1 - playerId of aiming one, int2 - playerId of target, bool1 - isSomebodyInGunsight
    

    Manual = 10002, //Для ручного запуска метода, который может быть вызван по событию, т.е. он должен иметь аргументы EventId id, EventInfo info
}

public abstract class EventInfo
{
    private bool cancelled = false;
    public abstract short Serialize(StreamBuffer outBuffer);
    public abstract void Deserialize(StreamBuffer inStream, short length);

    public void CancelForNextPriority()
    {
        cancelled = true;
    }

    public bool Cancelled
    {
        get
        {
            return cancelled;
        }
    }

    public static short CommonSerialize(EventInfo instance, StreamBuffer outStream)
    {
        return instance.Serialize(outStream);
    }

    public static EventInfo CommonDeserialize(EventId id, StreamBuffer inStream, short length)
    {
        return CommonDeserialize((int)id, inStream, length);
    }

    public static EventInfo CommonDeserialize(int id, StreamBuffer inStream, short length)
    {
        EventInfo eventInfo;
        if (id >= 10 && id <= 99)
            return new EventInfo_SimpleEvent();

        if (id <= 199)
            eventInfo = new EventInfo_I();
        else if (id <= 299)
            eventInfo = new EventInfo_II();
        else if (id <= 399)
            eventInfo = new EventInfo_IIV();
        else if (id <= 499)
            eventInfo = new EventInfo_IV();
        else if (id <= 599)
            eventInfo = new EventInfo_III();
        else if (id <= 699)
            eventInfo = new EventInfo_IIIV();
        else if (id <= 799)
            eventInfo = new EventInfo_S();
        else if (id <= 899)
            eventInfo = new EventInfo_IIII();
        else if (id <= 999)
            eventInfo = new EventInfo_B();
        else if (id <= 1299)
            eventInfo = new EventInfo_IB();
        else if (id <= 1399)
            eventInfo = new EventInfo_IIIIV();
        else if (id <= 1499)
            eventInfo = new EventInfo_IIIIB();
        else if (id <= 1699)
            eventInfo = new EventInfo_U();
        else if (id <= 1799)
            eventInfo = new EventInfo_IIB();
        else
        {
            DT.LogError("Dispatcher: unknown event id ({0})", id);
            return null;
        }

        eventInfo.Deserialize(inStream, length);

        return eventInfo;
    }

    /* SERIALIZE / DESERIALIZE functions */

    public static void SerializeVector3(Vector3 vector, byte[] bytes, ref int index)
    {
        Protocol.Serialize(vector.x, bytes, ref index);
        Protocol.Serialize(vector.y, bytes, ref index);
        Protocol.Serialize(vector.z, bytes, ref index);
    }

    public static Vector3 DeserializeVector3(byte[] bytes, int startIndex)
    {
        return new Vector3
        {
            x = BitConverter.ToSingle(bytes, startIndex),
            y = BitConverter.ToSingle(bytes, startIndex + 4),
            z = BitConverter.ToSingle(bytes, startIndex + 8)
        };
    }

    public static void SerializeString(string str, byte[] bytes, ref int index)
    {
        byte[] tmp = Encoding.UTF8.GetBytes(str);
        BitConverter.GetBytes(tmp.Length).CopyTo(bytes, index);
        index += 4;
        tmp.CopyTo(bytes, index);
        index += tmp.Length;
    }

    public static string DeserializeString(byte[] bytes, ref int index)
    {
        int length = BitConverter.ToInt32(bytes, index);
        index += 4;
        string res = Encoding.UTF8.GetString(bytes, index, length);
        index += length;

        return res;
    }
}
