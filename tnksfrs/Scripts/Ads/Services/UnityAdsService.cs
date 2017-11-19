//using UnityEngine.Advertisements;

public class UnityAdsService : ThirdPartyAdsService
{
    public override bool Initialize()
    {
        return false;

        //#if UNITY_IOS
        //Advertisement.debugLevel = Advertisement.DebugLevel.Error;
        //#endif

        //if (!Advertisement.isSupported || !IsSupportedOnCurrentPlatform)
        //    return false;

        //Advertisement.Initialize(
        //    ServiceSettings.Services
        //        [ServiceSettingsKeys.Service.UnityAds]
        //        [ServiceSettingsKeys.Field.GameID]);

        //return true;
    }

    public override void Show()
    {
        if (!IsSupportedOnCurrentPlatform)
        {
            ClosingCallback(false);
            return;
        }

        //string unityAdsRewardedZoneKey =
        //    ServiceSettings.Services
        //        [ServiceSettingsKeys.Service.UnityAds]
        //        [ServiceSettingsKeys.Field.RewardedZoneKey];

        //if (Advertisement.IsReady(unityAdsRewardedZoneKey))
        //{
        //    Advertisement.Show(
        //        zoneId: unityAdsRewardedZoneKey,
        //        options: new ShowOptions
        //                    {
        //                        gamerSid = ProfileInfo.playerId.ToString(),
        //                        resultCallback = showResult =>
        //                        {
        //                            ClosingCallback(showResult != ShowResult.Failed);
        //                            this.ReportGAEvent(GAEvent.Subject.UnityAds, showResult, GAEvent.Label.ForReward);
        //                        }
        //                    });

        //    return;
        //}

        //if (Advertisement.IsReady())
        //{
        //    Advertisement.Show(
        //        zoneId: null,
        //        options: new ShowOptions
        //                    {
        //                        gamerSid = ProfileInfo.playerId.ToString(),
        //                        resultCallback = showResult =>
        //                        {
        //                            ClosingCallback(showResult != ShowResult.Failed);
        //                            this.ReportGAEvent(GAEvent.Subject.UnityAds, showResult, GAEvent.Label.Default);
        //                        }
        //                    });

        //    return;
        //}

        ClosingCallback(false);
    }

    public override void Terminate() { }
}
