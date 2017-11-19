public class ScoresItemCreateClan : ScoresItem
{
    protected override void Awake()
    {
        base.Awake();
        UiItem.OnClickUIItem += OnScoresItemCreateClanClickHandler;
    }

    private void OnDestroy()
    {
        UiItem.OnClickUIItem -= OnScoresItemCreateClanClickHandler;
    }

    private void OnScoresItemCreateClanClickHandler(tk2dUIItem uiItem)
    {
        ClansManager.Instance.OpenClansWebPage();
    }
}
