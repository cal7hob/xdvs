using UnityEngine;

public class ChatItemJoinClanMessage : ChatItem
{
    [SerializeField] private tk2dUIItem btnClansUiItem;

    protected override void Awake()
    {
        base.Awake();
        btnClansUiItem.OnClickUIItem += OpenClansWebPage;
    }

    private void OnDestroy()
    {
        btnClansUiItem.OnClickUIItem -= OpenClansWebPage;
    }

    private void OpenClansWebPage(tk2dUIItem uiItem)
    {
        ClansManager.Instance.OpenClansWebPage();
    }
}