using UnityEngine;

public class DownloadNewerVersion : MonoBehaviour, IQueueablePage
{
    [SerializeField] private GameObject btnBack;
    [SerializeField] private tk2dUIItem btnQuitGame;
    [SerializeField] private tk2dTextMesh lblDescription;
    [SerializeField] private Color lblDescription_GameNameColor = Color.white;
    [SerializeField] private Color lblDescriptionColor = Color.white;

    public static bool IsNeededToShowUpdateGameWindow
    {
        get
        {
            return !SocialSettings.IsWebPlatform &&
                (ProfileInfo.ImportantUpdate || !string.IsNullOrEmpty(ProfileInfo.Version)/* || true*/);
        }
    }

    private void Awake()
    {
        Messenger.Subscribe(EventId.GameUpdateRequired, ShowUpdateGameWindow);
        Messenger.Subscribe(EventId.AfterHangarInit, CheckForShowUpdateGameWindow);
    }

    private void OnDestroy()
    {
        Messenger.Unsubscribe(EventId.GameUpdateRequired, ShowUpdateGameWindow);
        Messenger.Unsubscribe(EventId.AfterHangarInit, CheckForShowUpdateGameWindow);
    }

    public void CheckForShowUpdateGameWindow(EventId id, EventInfo info)
    {
        if (!IsNeededToShowUpdateGameWindow)
            return;

        GUIPager.EnqueuePage("UpdateGame", true, true);
    }

    public void ShowUpdateGameWindow(EventId id, EventInfo info)
    {
        if (!IsNeededToShowUpdateGameWindow)
            return;

        BeforeActivation();

        GUIPager.SetActivePage("UpdateGame", true, true);
    }

    public void BeforeActivation()
    {
        lblDescription.text = Localizer.GetText("lblNewVersionUpdate",
               lblDescriptionColor.To2DToolKitColorFormatString(),
               lblDescription_GameNameColor.To2DToolKitColorFormatString(),
               Application.productName,
               lblDescriptionColor.To2DToolKitColorFormatString());

        btnBack.SetActive(!ProfileInfo.ImportantUpdate);
        btnQuitGame.gameObject.SetActive(ProfileInfo.ImportantUpdate);
    }

    public void Activated()
    {
    }

    #region Обработка кнопок окна

    public void RedirectToPlayMarket()
    {
        Application.OpenURL(ProfileInfo.MarketURL);
        HangarController.QuitGame();
    }

    public void ClosePopUpWindow()
    {
         GUIPager.ToMainMenu();
    }

    private void QuitGame()
    {
        HangarController.QuitGame();
    }

    #endregion
}
