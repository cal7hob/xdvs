namespace ServiceSettingsKeys
{
    public enum Service
    {
        UnityAds,
        GooglePlayGames
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
        SpendingSilverEventKey
    }
}
