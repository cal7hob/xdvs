using System;
using System.Text;
using UnityEngine;

/*
    Types designation (in necessary order):
 *
 * I	- int (enums also)
 * F	- float
 * D	- double
 * S	- string
 * V3	- Vector3
 * E	- TankEffect
 * U    - object[] - universal (exceptional cases only)
*/

public enum CheatType
{
    None = 0,
    Speedup = 1,
    ObscuredValueChange = 2,
}

public enum EventId
{
    /// <summary>
    /// Simple game events (without parameters) (10..99) - EventInfo_SimpleEvent
    /// </summary>
    AfterLocalizationLoad = 10,
    AfterHangarInit = 11,
    MainTankAppeared = 12,
    TouchableAreaChanged = 13,
    AfterFacebookMainInfoLoaded = 14,
    LeftRoom = 15,
    ProfileInfoLoadedFromServer = 16,
    OnLanguageChange = 17,
    AfterFacebookInitialized = 18,
    ServerDataReceived = 19,
    AfterWebPlatformDefined = 20,
    FriendInviteSuccess = 21,
    TeamScoreChanged = 22,
    WindowModeChanged = 23,
    WeeklyAwardsChanged = 24,
    GameModeChanged = 25,
    MyTankRespawned = 26,
    PageChanged = 27, // on GUIPager.SetActivePage() event
    TroubleDisconnect = 28,
    PlayerFled = 29,
    ScoresBoxActivated = 30,
    NickNameChanged = 31,
    GameProlonged = 32,
    VKAndroidInitialized = 33,
    OnExitToHangar = 34, // Before load hangar
    SettingsSubmited = 35,
    ClanChanged = 36,
    FlagSettingsChanged = 37,
    AvatarSettingsChanged = 38,
    ScoresHighlightedItemsReady = 39,
    ResolutionChanged = 40,
    BankInitialized = 41,
    VehicleShopFilled = 42,
    PatternShopFilled = 43,
    DecalShopFilled = 44,
    SpecialOffersInitialized = 45,
    GameUpdateRequired = 46,
    PhotonRoomCustomPropertiesChanged = 47,
    //ChangeZoomState = 48,
    ModuleReceived = 49,
    WentToBattle = 50,
    GameCenterDisabled = 51, // Отправляется на тех платформах на которых нет гейм центра, например на винде, чтобы отключить кнопки вызова                                                гуи ачивок и лидербордов.
    TutorialOrderOverriden = 52,
    TutorialsInitialized = 53,
    VipOffersInstantiated = 54,
    MyVehicleCrashing = 55,
    //SACLOSReloaded                      = 56, // Deprecated, see WeaponReloaded
    BattleGUIIntialized = 57,
    BattleBtnPressed = 58,
    ShopInfoLoadedFromServer = 59,
    //IRCMReloaded                        = 60, // Deprecated, see WeaponReloaded
    IRCMLaunched = 61,
    PhotonRoomListReceived = 62,
    PhotonJoinedRoom = 63,
    BeforeReconnecting = 64,
    NickNameManuallyChanged = 65,
    SACLOSLaunched = 66,
    QualitySettingsChanged = 67, // Отличие от QualityLevelChanged: вызывается после переключения материалов.
    ShopManagerItemsLoaded = 68,
    JoinRoomFailed = 69,
    MasterDisconnectAnswer = 70,
    RoomBusyListTrimmed = 71,
    PlayerLevelChanged = 72,//Когда с сервера пришел опыт, и оказалось что уровень игрока вырос / ну или снизился )
    BattleQuestUpdated = 73, // Произошли изменения в боевом квесте (прогресс, признак выполнения)
    TeamChange = 74, // Изменился состав команд 
    FriendsScoresHighlightedItemsReady = 75,
    BattleSettingsSubmited = 76,
    BattleTutorialSkipping = 77,
    OnReadyToStartWindowsQueue = 78,
    ProfileServerChoosed = 79,
    OnKillAlertDestroy = 80,
    AdvancedSettingsPageSwitch = 81,
    RewardedVideoClicked = 82,
    BankOpened = 83,
    ScoresBoxArrowClick = 84,
    VipAccountPurchased = 85,
    VipShopOpen = 86,
    DeathAnimationDone = 87,
    MainVehWeaponReloaded = 88,
    ForceReload = 89,
    MassRespawn = 90,



    /// <summary>
    /// Int (100..199) - EventInfo_I
    /// </summary>
    TankJoinedBattle = 100, // playerId
    TankRespawned = 101, // playerId
    TankOutOfTime = 102, // playerId
    PlayerKickout = 103, // playerId
    GoldAcquired = 104, // amount
    SilverAcquired = 105, // amount
    ExperienceAcquired = 106, // amount
    VehicleSelected = 107, // vehicleId
    FuelAcquired = 108, // amount
    TankLeftTheGame = 109, // playerId
    CheatDetected = 110, // whattaFuckId
    TakeYourTeamId = 111, // teamId for player
    FuelUpdated = 112, // new fuel amount
    MaxFuelUpdated = 113, // new max fuel
    VipTimerUpdated = 114, // vip account expiration time updated
    GoldRushPlayerStakes = 115, // stake (gold)
    RevengeDone = 116, // victimId - I have revenge!
    HangarTimerTick = 117, // (int)GameData.instance.GetCorrectedTime ()
    QualityLevelChanged = 118, // new quality level
    MyTankShots = 119, // shellType
    BodyKitSelected = 120, // bodyKitId
    CamouflageBought = 121, // camoId
    VehicleBought = 122, // vehicleId
    VehicleInstalled = 123, // vehicleId
    ModuleBought = 124, // moduleId
    NewPlayerConnected = 125, // playerId
    PlayerDisconnected = 126, // playerId
    VehicleCrashing = 127, // playerId
    VoiceRequired = 128, // voice event id 
    TutorialIndexChanged = 129, // completed tutorial index
    BattleLessonAccomplished = 130, // completed battle lesson enum
    BattleEnd = 131, // (int)endBattleCause
    QuestCompleted = 132, // (int)curentQuest.type
    NormalDisconnectNotice = 133, // playerId
    PatternSelected = 134, // patternId
    WeaponOverheated = 135, // (int)GunShellInfo.ShellType
    PatternExpired = 136, // patternId
    DecalExpired = 137, // decalId
    TankShotMissed = 138, // attackerId
    StartTurretRotation = 139, // playerId
    StopTurretRotation = 140, // playerId
    WeaponReloaded = 141, // (int)GunShellInfo.ShellType
    BattleLessonStarted = 142, // started battle lesson enum
    HangarVehicleGeometryLoaded = 143, // vehicleId   
    ConsumableBought = 144, // consumableId
    VipConsumableClicked = 145, // playerId
    HideEnemy = 146,// playerId
    ShowEnemy = 147,// playerId
    TeamWin = 148,//Team Winner Id
    DestroyThisGameObject = 149,// gameObjectId


    /// <summary>
    /// Int, Int (200..299) - EventInfo_II
    /// </summary>
    TankKilled = 200, // deadId, attackerId
    TryingTakeItem = 201, // itemId, playerId
    TankHealthChanged = 202, // playerId, newHealth
    TankEffectCancelled = 203, // playerId, effectId
    ProfileMoneyChange = 204, // new gold, new silver
    TankAvailabilityChanged = 205, // playerId, state (0/1)
    StartBurstFire = 206, // playerId, shellId
    StopBurstFire = 207, // playerId, shellId
    BonusDestroyed = 208, // (int)BonusType, bonus id from photon
    EngineStateChanged = 209, // playerId, (int)engineState
    NewLayerInTeamMask = 210, // teamId, layer id
    OffLayerInTeamMask = 211, // teamId, layer id
    VehicleRotationStateChanged = 212, // playerId, (int)rotationState
    ConsumableUsed = 213, // consumableId
    TankKilledInfo = 214, // int1 = VictimId, Int2 = shellTypeID


    /// <summary>
    /// Int, Int, Vector3 (300..399) - EventInfo_IIV
    /// </summary>


    //Int, Vector3 (400..499) - EventInfo_IV

    /// <summary>
    /// Int, int, int (500 - 599) - EventInfo_III
    /// </summary>
    ItemTaken = 500, // (int)bonusType, amount, playerId (who gets item)
    SecondaryWeaponUsed = 501, // playerId, weaponId, shellId
    StartBurstAimedFire = 502, // playerId, shellId, victimId
    StopBurstAimedFire = 503, // playerId, shellId, victimId


    //Int, Int, Int, Vector3 (600 - 699) - EventInfo IIIV
    // ...

    // string, string (700 - 799) - EventInfo_S
    ShowNextPage = 700, // str1 = "previous page, next page"
    PhotonDisconnectWithCause = 701, // str1 - disconnect cause


    // int, int, int, int (800 - 899) - EventInfo_IIII

    /// <summary>
    /// bool (900 - 999) - EventInfo_B
    /// </summary>
    NowImMaster = 900, // iAmRoomCreator
    VipStatusUpdated = 901, // new player vip status
    OnStatTableChangeVisibility = 902,
    //ScoresBoxToggle                     = 903, // deprecated
    MessageBoxChangeVisibility = 904,
    //SACLOSAimed                         = 905,
    IRCMLaunchRequired = 906,
    MissileThreat = 907,
    SACLOSLaunchRequired = 908,
    MapSelectionAppeared = 909, // true - appear, false - disappear
    ZoomStateChanged = 910,
    HighPingAlarm = 911, // true - enabled, false - disabled
    BtnBackInBattleChangeVisibility = 912,
    OnNotifierChangeVisibility = 913,
    OnBattleSettingsChangeVisibility = 914,
    OnBattleChatCommandsChangeVisibility = 915,

    // int, int, double (1000 - 1099) - EventInfo_IID

    /// <summary>
    /// int, TankEffect (1100 - 1199) - EventInfo_IE
    /// </summary>

    TankEffectApply = 1100,	// playerId, effect
    ShellEffect = 1101, // playerId, effect
    TankEffectRequest = 1102, //playerId, effect /Запрос наложения эффекта. Отправляется только клиенту-владельцу (owner транспорта, на который накладывается эффект).

    /// <summary>
    /// int, bool (1200 - 1299) - EventInfo_IB
    /// </summary>
    SACLOSAimed = 1200,

    /// <summary>
    /// int, int, int, Vector3 (1300 - 1399) - EventInfo IIIIV
    /// </summary>
    ShellHit = 1300, // victim, damage, owner, gun shell type, hit local position (in victim coordinates)

    /// <summary>
    /// bool, int, int, int, int (1400 - 1499) - EventInfo BIIII
    /// </summary>
    ShellStateChanged = 1400,  // active, victim, owner, gun shell type, id

    /// <summary>
    /// object[] (1500 - 1699) - EventInfo_U (универсальный).
    /// </summary>
    // Для всех типов, сериализация/десериализация которых зарегана в фотоне.
    // Применять с осторожностью и только в том случае, если не подходит ни один из вышеперечисленных наборов параметров.
    FirstRequestToMaster = 1500,
    TankTakesDamage = 1501, // victim, damage, attacker, shellType, position (Vector3)
    BattleChatCommand = 1502,
    ChangeElementStateRequest = 1503,// class ChangeElementStateRequestInfo

    /// <summary>
    /// object[] (1700 - 1799) - EventInfo_IIB
    /// </summary>
    TargetAimed = 1700, // int1 - playerId of aiming one, int2 - playerId of target, bool1 - isSomebodyInGunsight

    /// <summary>
    /// object[] (1800 - 1899) - EventInfo_V
    /// </summary>

    /// <summary>
    /// object[] (1800 - 1899) - EventInfo_V2
    /// </summary>
    OnScreenTap = 1900,//vector2 

    Manual = 10002, //Для ручного запуска метода, который может быть вызван по событию, т.е. он должен иметь аргументы EventId id, EventInfo info
}

public abstract class EventInfo
{
    private bool cancelled = false;
    public abstract byte[] Serialize();
    public abstract void Deserialize(byte[] serialized, int startIndex);

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

    public static byte[] CommonSerialize(EventInfo instance)
    {
        return instance.Serialize();
    }

    public static EventInfo CommonDeserialize(EventId id, byte[] serialized, int startIndex)
    {
        return CommonDeserialize((int)id, serialized, startIndex);
    }

    public static EventInfo CommonDeserialize(int id, byte[] serialized, int startIndex)
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
        else if (id <= 1099)
            eventInfo = new EventInfo_IID();
        else if (id <= 1199)
            eventInfo = new EventInfo_IE();
        else if (id <= 1299)
            eventInfo = new EventInfo_IB();
        else if (id <= 1399)
            eventInfo = new EventInfo_IIIIV();
        else if (id <= 1499)
            eventInfo = new EventInfo_BIIII();
        else if (id <= 1699)
            eventInfo = new EventInfo_U();
        else if (id <= 1799)
            eventInfo = new EventInfo_IIB();
        else if (id <= 1899)
            eventInfo = new EventInfo_V();
        else
        {
            DT.LogError("Dispatcher: unknown event id ({0})", id);
            return null;
        }

        eventInfo.Deserialize(serialized, startIndex);

        return eventInfo;
    }

    /* SERIALIZE / DESERIALIZE functions */
    public static byte[] SerializeVector2(Vector2 vo)
    {
        byte[] bytes = new byte[2 * 4];
        BitConverter.GetBytes(vo.x).CopyTo(bytes, 0);
        BitConverter.GetBytes(vo.y).CopyTo(bytes, 4);

        return bytes;
    }

    public static byte[] SerializeVector3(Vector3 vo)
    {
        byte[] bytes = new byte[3 * 4];
        BitConverter.GetBytes(vo.x).CopyTo(bytes, 0);
        BitConverter.GetBytes(vo.y).CopyTo(bytes, 4);
        BitConverter.GetBytes(vo.z).CopyTo(bytes, 8);

        return bytes;
    }

    public static Vector2 DeserializeVector2(byte[] bytes, int startIndex)
    {
        return new Vector2
        {
            x = BitConverter.ToSingle(bytes, startIndex),
            y = BitConverter.ToSingle(bytes, startIndex + 4)
        };
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