using Http;
using UnityEngine;
using XD;

public class RateThisGame : MonoBehaviour, IQueueablePage
{
    [SerializeField] private GameObject rateThisGameWrapper;
    [SerializeField] private GameObject ratingBonusWrapper;
    [SerializeField] private Color lblRateThisGame_GameNameColor = Color.white;
    [SerializeField] private Color lblRateThisGameColor = Color.white;

    private void Awake()
    {
        if (StaticType.SocialSettings.Instance<ISocialSettings>().IsWebPlatform)
            return;

        Dispatcher.Subscribe(EventId.AfterHangarInit, CheckForRateThisGamePopup);
    }

    private void OnDestroy()
    {
        Dispatcher.Unsubscribe(EventId.AfterHangarInit, CheckForRateThisGamePopup);
    }

    private void CheckForRateThisGamePopup(EventId id, EventInfo info)
    {
        if (/*true || */ProfileInfo.launchesCount >= 5 && !HangarController.FirstEnter && XD.StaticContainer.BattleController.Rank == 1 &&
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

      
        if (GameData.isAwardForRateGameEnabled)
        {
            ratingBonusWrapper.SetActive(true);
            
            HorizontalLayout horizontalLayout = ratingBonusWrapper.GetComponent<HorizontalLayout>();
            if (horizontalLayout != null)
                horizontalLayout.Align();
        }
    }

    public void Activated() { }

    #region Обработка кнопок окна

    /*private void RedirectToPlayMarket(tk2dUIItem uiItem)
    {
        Application.OpenURL(ProfileInfo.MarketURL);
        Manager.RateThisGame();
        ClosePopUpWindow();
    }

    private void OnRateCancel(tk2dUIItem uiItem)
    {
        Manager.RateThisGameCancel();
        ClosePopUpWindow();
    }*/

    public void ClosePopUpWindow()
    {
        GUIPager.ToMainMenu();
    }

    #endregion
}