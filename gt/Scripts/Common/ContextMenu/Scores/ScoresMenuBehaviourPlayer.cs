using Tanks.Models;
using UnityEngine;

public class ScoresMenuBehaviourPlayer : ScoresMenuBehaviour
{
    [SerializeField] private Player scoresPlayer;

    public void OnAddToFriends(tk2dUIItem uiItem)
    {
        FriendsManager.AddToFriends(scoresPlayer.Id);
        contextMenu.HideContextMenu();
    }

    public void OnRemoveFromFriends(tk2dUIItem uiItem)
    {
        FriendsManager.RemoveFromFriends(scoresPlayer.Id);
        contextMenu.HideContextMenu();
    }

    public void ShowPlayerInfo(tk2dUIItem uiItem)
    {
        PlayerInfo.Instance.Show(scoresPlayer.Id, scoresPlayer.NickName);
        contextMenu.HideContextMenu();
    }

    public void ShowContextMenu(tk2dUIItem uiItem, tk2dUILayout layout, Player player)
    {
        scoresPlayer = player;

        ((PlayerContextMenu)contextMenu)
            .SetAddRemoveFriendsLabel(!FriendsManager.isAlreadyFriends(player, (PlayerContextMenu)contextMenu));

        ShowContextMenu(uiItem, layout);
    }
}
