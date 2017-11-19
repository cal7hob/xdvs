using System;

namespace GAEvent
{
    public enum Subject
    {
        // Идентификаторы:
        VehicleID,
        ModuleType,
        CamouflageID,
        StickerID,
        VIPOfferID,
        MapName,
        Quest,
        Tutorial,
        BattleLesson,

        // Характеристики профиля:
        PlayerLevel,
        GoldAmount,
        SilverAmount,
        ExperienceAmount,

        // Бонусы в бою:
        BonusType,

        // Количества:
        From0To1,
        From2To5,
        From6To10,
        From11To25,
        MoreThan25,
        From0To1000,
        From1001To2000,
        From2001To3000,
        From3001To4000,
        MoreThan4000,

        // Предложения:
        FreeVehicle,

        // Поставщики рекламы:
        UnityAds,
        Chartboost,
        ExtremeDevelopers,

        // Причина дисконнекта:
        PhotonDisconnectCause,
        
        // Режим игры:
        GameMode
    }

    public enum Category
    {
        NicknameChanging,
        FuelBuying,
        VehicleBuying,
        ModuleBuying,
        ModuleDeliveryBuying,
        LevelUp,
        CamouflageBuying,
        StickerBuying,
        VIPAccountBuying,
        FacebookIntegration,
        JoinBattle,
        LeaveBattle,
        PickedUpBonus,
        RespawnHastenBuying,
        RespawnBonusBuying,
        GameProlongationBuying,
        SilverEarnedInBattle,
        GoldEarnedInBattle,
        ExperienceEarnedInBattle,
        Quest,
        SpecialOffer,
        Advertisement,
        Tutorials,
        BattleLessons,
        PhotonDisconnect
    }

    public enum Action
    {
        // Общее:
        ClosedWindow,
        Bought,
        NotEnoughMoney,
        Cancelled,
        Succeed,
        Failed,

        // Получение топлива:
        GotViaAndroidInvitation,
        GotViaFacebookInvitation,
        GotViaMoimirInvitation,
        GotViaOdnoklassnikiInvitation,
        GotViaVKInvitation,
        GotViaVKAndroidInvitation,

        // Квесты:
        Completed,

        // Выход из боя:
        LeftBattleManually,
        LeftBattleInactive,
        LeftBattleTimeouted,
        LeftBattleDisconnected,
        LeftBattlePaused,
        LeftBattleForSecondEnter,
        LeftBattleCompletedTutorial,
        LeftBattleUnknownCause,

        // Реклама:
        Finished,
        Skipped,
        Displayed
    }

    public enum Label
    {
        Accepted,
        Rejected,
        ForReward,
        Default,
        PlayerLevel
    }

    public static class Converter
    {
        public static Subject ToQuantitySubject(int source, ProfileInfo.PriceCurrency currency)
        {
            switch (currency)
            {
                case ProfileInfo.PriceCurrency.Silver:

                    if (source > 4000)
                        return Subject.MoreThan4000;

                    if (source > 3000)
                        return Subject.From3001To4000;

                    if (source > 2000)
                        return Subject.From2001To3000;

                    if (source > 1000)
                        return Subject.From1001To2000;
                
                    return Subject.From0To1000;

                case ProfileInfo.PriceCurrency.Gold:

                    if (source > 25)
                        return Subject.MoreThan25;

                    if (source > 10)
                        return Subject.From11To25;

                    if (source > 5)
                        return Subject.From6To10;

                    if (source > 1)
                        return Subject.From2To5;

                    return Subject.From0To1;

                default: throw new ArgumentOutOfRangeException("currency", currency, null);
            }
        }
    }
}
