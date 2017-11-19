
using UnityEngine;
using System.Collections.Generic;
using System.Collections;

public class FriendInviteListController : MonoBehaviour {

    public FriendToInviteList FriendsToInvite;
    private static FriendInviteListController _instance;

    public static FriendInviteListController Instance
    {
        get { return _instance; }
    }

    public void Fill()
    {
        FriendsToInvite.Clear();
        foreach (var friend in VkMobile.FriendsToInvite)
        {
            FriendsToInvite.Add(friend);
        }
        FriendsToInvite.Reposition();
    }

    void Awake()
    {
        _instance = this;
    }

    void OnOkClick()
    {
        FriendsToInvite.Clear();
        GUIPager.Back();
    }
}
