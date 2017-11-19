using System.Collections;
using ChartboostSDK;
using UnityEngine;

public class ChartboostService : ThirdPartyAdsService
{
    private const float EMERGENCY_CLOSING_TIME = 10.0f;

    private bool anyEventFired;
    private CBLocation defaultLocation;

    public override bool HasRewardedVideos
    {
        get { return Chartboost.hasRewardedVideo(defaultLocation); }
    }

    public override bool Initialize()
    {
        if (!IsSupportedOnCurrentPlatform)
            return false;

        defaultLocation = CBLocation.Default;

        Chartboost.setAutoCacheAds(true);

        Chartboost.didFailToLoadInterstitial += InterstitialLoadingFailureCallback;
        Chartboost.shouldDisplayInterstitial += InterstitialDisplayingCallback;
        Chartboost.didClickInterstitial += InterstitialClickCallback;
        Chartboost.didDismissInterstitial += InterstitialDismissingCallback;

        Chartboost.didCacheRewardedVideo += RewardedVideoCachingCallback;
        Chartboost.shouldDisplayRewardedVideo += RewardedVideoDisplayingCallback;
        Chartboost.didCloseRewardedVideo += RewardedVideoClosingCallback;
        Chartboost.didFailToLoadRewardedVideo += RewardedVideoLoadingFailure;
        Chartboost.didDismissRewardedVideo += RewardedVideoDismissingCallback;
        Chartboost.didCompleteRewardedVideo += RewardedVideoCompletedCallback;

        Chartboost.cacheRewardedVideo(defaultLocation);

        return true;
    }

    private void RewardedVideoCompletedCallback(CBLocation location, int i)
    {
        RaiseOnRewardedVideoFinished();
    }

    private void RewardedVideoDismissingCallback(CBLocation location)
    {
        RaiseOnRewardedVideoClosed();
    }

    private void RewardedVideoLoadingFailure(CBLocation location, CBImpressionError error)
    {
        RaiseOnRewardedVideoFailedToLoad();
    }

    private void RewardedVideoClosingCallback(CBLocation location)
    {
        RaiseOnRewardedVideoClosed();
    }

    private bool RewardedVideoDisplayingCallback(CBLocation location)
    {
        RaiseOnRewardedVideoShown();
        return true;
    }

    private void RewardedVideoCachingCallback(CBLocation location)
    {
        RaiseOnRewardedVideoLoaded();
    }

    public override void ShowInterstitial()
    {
        if (!IsSupportedOnCurrentPlatform)
        {
            ClosingCallback(false);
            return;
        }

        anyEventFired = false;

        CoroutineHelper.Start(EmergencyClosing());

        if (Chartboost.hasInterstitial(defaultLocation))
            Chartboost.showInterstitial(defaultLocation);
        else
            Chartboost.cacheInterstitial(defaultLocation);
    }

    public override void ShowRewardedVideo()
    {
        Chartboost.showRewardedVideo(defaultLocation);
    }

    public override void Terminate()
    {
        if (!IsSupportedOnCurrentPlatform)
            return;

        Chartboost.didFailToLoadInterstitial -= InterstitialLoadingFailureCallback;
        Chartboost.shouldDisplayInterstitial -= InterstitialDisplayingCallback;
        Chartboost.didClickInterstitial -= InterstitialClickCallback;
        Chartboost.didDismissInterstitial -= InterstitialDismissingCallback;

        Chartboost.didCacheRewardedVideo -= RewardedVideoCachingCallback;
        Chartboost.shouldDisplayRewardedVideo -= RewardedVideoDisplayingCallback;
        Chartboost.didCloseRewardedVideo -= RewardedVideoClosingCallback;
        Chartboost.didFailToLoadRewardedVideo -= RewardedVideoLoadingFailure;
        Chartboost.didDismissRewardedVideo -= RewardedVideoDismissingCallback;
        Chartboost.didCompleteRewardedVideo -= RewardedVideoCompletedCallback;
    }

    public override void Setup(AdsSettings settings)
    {
        base.Setup(settings);

        Chartboost.setCustomId(ProfileInfo.profileId.ToString());

        if (settings.ShowingMode == AdsShowingMode.Nowhere)
            return;

        if (!Chartboost.hasInterstitial(defaultLocation))
            Chartboost.cacheInterstitial(defaultLocation);
    }

    #region Callbacks

    private void InterstitialLoadingFailureCallback(CBLocation location, CBImpressionError error)
    {
        anyEventFired = true;

        Debug.LogError(error);

        if (ClosingCallback == null)
        {
            Debug.LogError("Chartboost service closing callback is null!");
            return;
        }

        ClosingCallback(false);

        this.ReportGAEvent(GAEvent.Subject.Chartboost, GAEvent.Action.Failed, GAEvent.Label.Default);
    }

    private bool InterstitialDisplayingCallback(CBLocation location)
    {
        anyEventFired = true;
        this.ReportGAEvent(GAEvent.Subject.Chartboost, GAEvent.Action.Displayed, GAEvent.Label.Default);
        return true; // Кажись, чартбустовцы сигнатуру перепутали, ну да ладно.
    }

    private void InterstitialClickCallback(CBLocation location)
    {
        anyEventFired = true;
        ClosingCallback(true);
        this.ReportGAEvent(GAEvent.Subject.Chartboost, GAEvent.Action.Finished, GAEvent.Label.Default);
    }

    private void InterstitialDismissingCallback(CBLocation location)
    {
        anyEventFired = true;
        ClosingCallback(true);
        this.ReportGAEvent(GAEvent.Subject.Chartboost, GAEvent.Action.Skipped, GAEvent.Label.Default);
    }

    private IEnumerator EmergencyClosing()
    {
        float remainingTime = EMERGENCY_CLOSING_TIME;
        float stepSeconds = 1.0f;

        while (remainingTime > 0)
        {
            remainingTime -= stepSeconds;
            yield return new WaitForSeconds(stepSeconds);
        }

        if (!anyEventFired)
        {
            Debug.LogWarningFormat(
                "ChartboostService closing called because "
                    + "no useful Chartboost event was fired after {0} seconds.",
                EMERGENCY_CLOSING_TIME);

            ClosingCallback(false);
        }
    }

    #endregion
}
