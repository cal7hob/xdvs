using Http;
using UnityEngine;

public class RateThisGame : MonoBehaviour, IQueueablePage
{
    [SerializeField] private GameObject rateThisGameWrapper;
    [SerializeField] private tk2dTextMesh lblRateThisGame;
    [SerializeField] private GameObject ratingBonusWrapper;
    [SerializeField] private tk2dBaseSprite sprGameRateBonusCurrency;
    [SerializeField] private TextTie lblGameRateBonusAmount;
    [SerializeField] private Color lblRateThisGame_GameNameColor = Color.white;
    [SerializeField] private Color lblRateThisGameColor = Color.white;

    private void Awake()
    {
        if (SocialSettings.IsWebPlatform)
            return;

        Messenger.Subscribe(EventId.AfterHangarInit, CheckForRateThisGamePopup);
    }

    private void OnDestroy()
    {
        Messenger.Unsubscribe(EventId.AfterHangarInit, CheckForRateThisGamePopup);
    }

    private void CheckForRateThisGamePopup(EventId id, EventInfo info)
    {
        if (/*true || */ProfileInfo.launchesCount >= 5 && !HangarController.FirstEnter && StatTable.MyVehicleRank == 1 &&
            !ProfileInfo.ratedForBonus)
        {
            if (/*true || */GameData.isAwardForRateGameEnabled || !ProfileInfo.isGameRated)
            {
                GUIPager.EnqueuePage("RateThisGame", false, true, (int)VoiceEventKey.RateGameRequired);
            }
        }
    }

    public void BeforeActivation()
    {
        ratingBonusWrapper.SetActive(false);

        lblRateThisGame.text =
            Localizer.GetText(
                GameData.isAwardForRateGameEnabled ? "lblRateThisGamePromptForBonus" : "lblRateThisGamePrompt",
                lblRateThisGameColor.To2DToolKitColorFormatString(),
                lblRateThisGame_GameNameColor.To2DToolKitColorFormatString(),
                Application.productName,
                lblRateThisGameColor.To2DToolKitColorFormatString());

        if (GameData.isAwardForRateGameEnabled)
        {
            ratingBonusWrapper.SetActive(true);
            lblGameRateBonusAmount.SetText(GameData.awardForRateGame.LocalizedValue);
            GameData.awardForRateGame.SetMoneySpecificColorIfCan(lblGameRateBonusAmount.TextMesh);
            sprGameRateBonusCurrency.SetSprite(GameData.awardForRateGame.currency == ProfileInfo.PriceCurrency.Gold
                ? "gold"
                : "silver");
            HorizontalLayout horizontalLayout = ratingBonusWrapper.GetComponent<HorizontalLayout>();
            if (horizontalLayout != null)
                horizontalLayout.Align();
        }
    }

    public void Activated() { }

    #region Обработка кнопок окна

    private void RedirectToPlayMarket(tk2dUIItem uiItem)
    {
        Application.OpenURL(ProfileInfo.MarketURL);
        Manager.RateThisGame();
        ClosePopUpWindow();
    }

    private void OnRateCancel(tk2dUIItem uiItem)
    {
        Manager.RateThisGameCancel();
        ClosePopUpWindow();
    }

    public void ClosePopUpWindow()
    {
        GUIPager.ToMainMenu();
    }

    #endregion
}