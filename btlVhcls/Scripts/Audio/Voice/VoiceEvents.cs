public enum VoiceEventKey // Лучше не менять порядок и добавлять новые в конец.
{
    // Боевые события:
    SACLOSLaunchRequired,
    MissileThreat,
    IRCMLaunchRequired,
    Crashing,
    ShellHit,
    EnemyKilled,
    PickedUpBonusArmor,
    PickedUpBonusDamage,
    PickedUpBonusROF,
    PickedUpBonusFuel,
    QuestCompleted,
    PickedUpBonusGold,
    PickedUpBonusSilver,
    PickedUpBonusExperience,
    BattleEndTimeouted,

    // Туторы:
    GreetingsComander,
    HelicopterMoveLessonJoystick,
    HelicopterMoveLessonButtons,
    HelicopterUpDownLessonJoystick,
    HelicopterUpDownLessonButtons,
    BonusPickUpLesson,
    FireLessonJoystick,
    FireLessonButtons,
    UsingDefenceLessonJoystick,
    UsingDefenceLessonButtons,
    KillEnemiesLesson,
    EnterNameLesson,
    GoToBattleLesson,
    VehicleUpgradeLesson,
    BuyCamouflageLesson,
    BuyNewVehicleLesson,

    // Ангарные события:
    CurrentQuests,
    MapSelection,
    NotEnoughFuel,
    ChatEnter,
    RateGameRequired,
    UpdateGameRequired,
    NotEnoughMoney,
    VehicleShopEnter,
    VehicleInstall,
    ModuleShopEnter,
    PatternShopEnter,
    AfterBattleStatistic,
    LeaderBoardWindow,
    ModuleDelivered,
    DecalShopEnter,

    // Боевые события (новые):
    ShotRequired,
    GoodShot,
    MissedShot,
    WeaponOverheated,
    MyTankDestroyed,

    // Ангарные события (новые):
    OffersEnter,
    PatternConsumed,
    DecalConsumed,

    // Туторы (новые):
    TankMoveLessonJoystick,
    TankMoveLessonButtons,
    FlightMoveLessonJoystick,
    FlightMoveLessonButtons,
    TurretLessonTouch,
    TurretLessonButtons,
    ThrottleLessonTouch,
    ThrottleLessonButtons,
    AircraftMissileLessonTouch, // TODO: добавить отправку, если нужно.
    AircraftMissileLessonButtons, // TODO: добавить отправку, если нужно.
    HelicopterHealthbarLesson,
    TanksAndFlightHealthbarLesson,
    FirstAwardLesson, // Не используется.

    // Новые ключи:
    LeaderBoardWindowVIP,

    // Боевой чат (мои реплики):
    ChatMyAttack,
    ChatMyAffirmative,
    ChatMyHelpMe,
    ChatMyNotInterfere,
    ChatMyNegative,
    ChatMyThanks,

    // Боевой чат (другой игрок):
    ChatOthersAttack,
    ChatOthersAffirmative,
    ChatOthersHelpMe,
    ChatOthersNotInterfere,
    ChatOthersNegative,
    ChatOthersThanks,

    // Боевые события (новые):
    MissedShotMachineGun,
    GoodShotMachineGun
}
