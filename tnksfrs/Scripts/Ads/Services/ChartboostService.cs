using System.Collections;
using ChartboostSDK;
using UnityEngine;

public class ChartboostService : ThirdPartyAdsService
{
    private const float EMERGENCY_CLOSING_TIME = 10.0f;

    private bool isShowing;
    private bool anyEventFired;
    private CBLocation defaultLocation;

    public override bool Initialize()
    {
        defaultLocation = CBLocation.Default;

        if (!IsSupportedOnCurrentPlatform)
            return false;

        Chartboost.setCustomId(ProfileInfo.playerId.ToString());

        Chartboost.setAutoCacheAds(true);

        Chartboost.didCacheInterstitial += InterstitialCachingCallback;
        Chartboost.didFailToLoadInterstitial += InterstitialLoadingFailureCallback;
        Chartboost.didDisplayInterstitial += InterstitialDisplayingCallback;
        Chartboost.didClickInterstitial += InterstitialClickCallback;
        Chartboost.didDismissInterstitial += InterstitialDismissingCallback;
        Chartboost.didCloseInterstitial += InterstitialClosingCallback;

        return true;
    }

    public override void Show()
    {
        if (!IsSupportedOnCurrentPlatform)
        {
            ClosingCallback(false);
            return;
        }

        anyEventFired = false;
        isShowing = true;

        if (Chartboost.hasInterstitial(defaultLocation))
        {
            Chartboost.showInterstitial(defaultLocation);
            return;
        }
        
        Chartboost.cacheInterstitial(defaultLocation);

        CoroutineHelper.Start(EmergencyClosing());
    }

    public override void Terminate()
    {
        if (!IsSupportedOnCurrentPlatform)
            return;

        Chartboost.didCacheInterstitial -= InterstitialCachingCallback;
        Chartboost.didFailToLoadInterstitial -= InterstitialLoadingFailureCallback;
        Chartboost.didDisplayInterstitial -= InterstitialDisplayingCallback;
        Chartboost.didClickInterstitial -= InterstitialClickCallback;
        Chartboost.didDismissInterstitial -= InterstitialDismissingCallback;
        Chartboost.didCloseInterstitial -= InterstitialClosingCallback;
    }

    public override void Setup(AdsSettings settings)
    {
        base.Setup(settings);

        if (settings.ShowingMode == AdsShowingMode.Nowhere)
            return;

        if (!Chartboost.hasInterstitial(defaultLocation))
            Chartboost.cacheInterstitial(defaultLocation);
    }

    #region Callbacks

    private void InterstitialCachingCallback(CBLocation location)
    {
        anyEventFired = true;

        if (isShowing)
            Chartboost.showInterstitial(defaultLocation);
    }

    private void InterstitialLoadingFailureCallback(CBLocation location, CBImpressionError error)
    {
        anyEventFired = true;

        Debug.LogError(error);

        if (ClosingCallback == null)
            return;

        isShowing = false;

        ClosingCallback(false);

        this.ReportGAEvent(GAEvent.Subject.Chartboost, GAEvent.Action.Failed, GAEvent.Label.Default);
    }

    private void InterstitialDisplayingCallback(CBLocation cbLocation)
    {
        anyEventFired = true;
        this.ReportGAEvent(GAEvent.Subject.Chartboost, GAEvent.Action.Displayed, GAEvent.Label.Default);
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

    private void InterstitialClosingCallback(CBLocation location)
    {
        anyEventFired = true;
        isShowing = false;
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
