namespace ServiceSettingsKeys
{
    public enum Service
    {
        UnityAds,
        GooglePlayGames,
        Odnoklassniki,
        PlayerSettingsOptions,
        Facebook,
    }

    public enum Field
    {
        // Unity Ads:
        GameID,
        RewardedZoneKey,

        // Google Play Games:
        GettingGoldEventKey,
        GettingSilverEventKey,
        SpendingGoldEventKey,
        SpendingSilverEventKey,

        // Odnoklassniki
        AppId,
        GameGroupId,
        BaseUrlForPostImages,
        AppShortname,

        // XdevsSplashScreen
        SplashscreenPath,

        // Facebook
        Namespace,
        AppLinkURL,
    }
}
