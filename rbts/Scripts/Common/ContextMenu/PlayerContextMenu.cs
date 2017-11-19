using Tanks;

public class PlayerContextMenu : ContextMenu
{
	public enum AddToFriendsBtnStates
	{
		addToFriends,
		removeFromFriends
	};

	public void ToggleAddToFriendsBtnState(AddToFriendsBtnStates state)
	{
		foreach (var contextMenuItem in menuItems)
		{
			if (contextMenuItem.name == "AddRemoveFriends")
			{
				switch (state)
				{
					case AddToFriendsBtnStates.removeFromFriends:
						contextMenuItem.textMesh.name = "lblRemoveFromFriends";
						contextMenuItem.uiItem.SendMessageOnClickMethodName = "OnRemoveFromFriends";
						contextMenuItem.textMesh.text = Localizer.GetText("lblRemoveFromFriends");
						break;
					case AddToFriendsBtnStates.addToFriends:
						contextMenuItem.textMesh.name = "lblAddToFriends";
						contextMenuItem.uiItem.SendMessageOnClickMethodName = "OnAddToFriends";
						contextMenuItem.textMesh.text = Localizer.GetText("lblAddToFriends");
						break;
				}
				break;
			}
		}
	}

	public void SetAddRemoveFriendsLabel(bool alreadyFriends)
	{
		ToggleAddToFriendsBtnState(alreadyFriends ? AddToFriendsBtnStates.removeFromFriends : AddToFriendsBtnStates.addToFriends);
	}
}
