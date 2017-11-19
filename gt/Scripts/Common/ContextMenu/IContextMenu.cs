using Tanks.Models;

public interface IContextMenu
{
    void Update();
    void ShowContextMenu(tk2dUIItem uiItem, Player player);
    void SetContextMenuPosition(tk2dUIItem uiItem);
    void OnAddToFriends(tk2dUIItem uiItem);
    void OnRemoveFromFriends(tk2dUIItem uiItem);
    void ShowPlayerInfo(tk2dUIItem uiItem);
    void HideOnTouchOutsideBounds();
	void AddSpecialButtons();
}
