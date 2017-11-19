using Http;
using UnityEngine;

public class RateThisGame : MonoBehaviour, IQueueablePage
{
    [SerializeField] private GameObject rateThisGameWrapper;
    [SerializeField] private tk2dTextMesh lblRateThisGame;
    [SerializeField] private PriceRenderer rateBonus;
    [SerializeField] private Color lblRateThisGame_GameNameColor = Color.white;
    [SerializeField] private Color lblRateThisGameColor = Color.white;

    private void Awake()
    {
        if (SocialSettings.IsWebPlatform)
            return;

        Dispatcher.Subscribe(EventId.AfterHangarInit, CheckForRateThisGamePopup);
    }

    private void OnDestroy()
    {
        Dispatcher.Unsubscribe(EventId.AfterHangarInit, CheckForRateThisGamePopup);
    }

    private void CheckForRateThisGamePopup(EventId id, EventInfo info)
    {
        if (/*true || */ProfileInfo.launchesCount >= 5 && !HangarController.FirstEnter && StatTable.MyVehicleRank == 1 &&
            !ProfileInfo.ratedForBonus)
        {
            if (/*true || */GameData.isAwardForRateGameEnabled || !ProfileInfo.isGameRated)
            {
                GUIPager.EnqueuePage("RateThisGame", false, (int)VoiceEventKey.RateGameRequired);
            }
        }
    }

    public void BeforeActivation()
    {
        lblRateThisGame.text =
            Localizer.GetText(
                GameData.isAwardForRateGameEnabled ? "lblRateThisGamePromptForBonus" : "lblRateThisGamePrompt",
                lblRateThisGameColor.To2DToolKitColorFormatString(),
                lblRateThisGame_GameNameColor.To2DToolKitColorFormatString(),
                Application.productName,
                lblRateThisGameColor.To2DToolKitColorFormatString());

        rateBonus.gameObject.SetActive(GameData.isAwardForRateGameEnabled);

        if (GameData.isAwardForRateGameEnabled)
            rateBonus.Price = GameData.awardForRateGame;
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