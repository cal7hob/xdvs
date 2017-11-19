using Http;
using System;
using System.Collections;
using System.Collections.Generic;
using Tanks.Models;
using UnityEngine;

public class FriendsManager : MonoBehaviour
{
	public static FriendsManager Instance { get; private set; }
    public static List<string> socialFriendsUids = new List<string>();
    
    public static float friendsUpdateInterval = 60 * 5; // Seconds
	private static double nextFriendsUpdateTime;
    private static string social = null;

    public static List<object> FriendsScoresData { get; private set; }

    private void Awake()
    {
        Instance = this;
        GetSocialFriends();
        SocialSettings.Instance.LoginSucceed += OnSocialLogin;
        Dispatcher.Subscribe(EventId.AfterFacebookMainInfoLoaded, Handler);
    }

    private void Handler(EventId id, EventInfo info)
    {
        GetSocialFriends();
    }

    private void OnDestroy()
    {
        if (SocialSettings.Instance != null) SocialSettings.Instance.LoginSucceed -= OnSocialLogin;
        Dispatcher.Unsubscribe(EventId.AfterFacebookMainInfoLoaded, Handler);
        Instance = null;
    }

    private static void OnSocialLogin()
    {
        GetSocialFriends();
    }

    private static void GetSocialFriends()
    {
        socialFriendsUids = SocialSettings.GetSocialService().GetFriendsAppUsersIds();

        if (socialFriendsUids.Count > 0)
		    social = SocialSettings.Platform.ToString().ToLower();

        ForceUpdateFriends();
    }

    private static void AddSocialFriendsToRequest(Http.Request request)
    {
        if (social != null && socialFriendsUids.Count > 0)
        {
            request.Form.AddField("friends", MiniJSON.Json.Serialize(socialFriendsUids));
            request.Form.AddField("social", social);
        }
    }

    public static void AddToFriends(int playerId, Action successCallback = null)
	{
		var request = Manager.Instance().CreateRequest("/player/addFriend");
		request.Form.AddField("playerId", playerId);

        AddSocialFriendsToRequest(request);

        Manager.StartAsyncRequest(
            request:            request,
            failCallback:       result => MessageBox.Show(MessageBox.Type.Info, "Failed to add friend"),
            successCallback:    result =>
            {
                HangarController.Instance.StartCoroutine(FriendsUpdated(result));

                successCallback.SafeInvoke();
            });
	}

    public static void RemoveFromFriends(int playerId, Action successCallback = null)
    {
        var request = Manager.Instance().CreateRequest("/player/removeFriend");
        request.Form.AddField("playerId", playerId);

        AddSocialFriendsToRequest(request);

        Manager.StartAsyncRequest(
            request:            request,
            failCallback:       result => MessageBox.Show(MessageBox.Type.Info, "Failed to remove friend"),
            successCallback:    result =>
            {
                HangarController.Instance.StartCoroutine(FriendsUpdated(result));

                successCallback.SafeInvoke();
            });
    }

    public static bool isAlreadyFriends(Player player, PlayerContextMenu contextMenu)
    {
        bool alreadyFriends = ScoresController.Instance.friendsScores.CheckIfAlreadyFriends(player.Id);
        bool isSocialFriend = player.Social != null && socialFriendsUids.Contains(player.Social.Uid);

        if (ProfileInfo.profileId == player.Id || isSocialFriend)
            contextMenu.HideMenuItem("AddRemoveFriends");
        else
            contextMenu.ShowMenuItem("AddRemoveFriends");

        return !alreadyFriends;
    }

    public static void UpdateFriends(double timeStamp)
    {
        if (nextFriendsUpdateTime < GameData.CurrentTimeStamp)
        {
            ForceUpdateFriends();
        }
    }

    public static void ForceUpdateFriends()
    {
        var request = Manager.Instance().CreateRequest("/statistics/friends");
        AddSocialFriendsToRequest(request);

        Manager.StartAsyncRequest(request,
            successCallback =>
            {
                if (HangarController.Instance != null)
                    HangarController.Instance.StartCoroutine(FriendsUpdated(successCallback));
            }, 
            result => Debug.Log("Friends update failed"));

        nextFriendsUpdateTime = GameData.CurrentTimeStamp + friendsUpdateInterval;
    }

    private static IEnumerator FriendsUpdated(Response result)
    {
        if (ScoresController.Instance == null
            || ScoresController.Instance.friendsScores == null)
            yield break;

        var data = new JsonPrefs(result.Data);
        if (!data.Contains("status") || !data.Contains("stats"))
            yield break;

        switch (data.ValueInt("status"))
        {
            case 0:
                Debug.LogError("Failed to update friends list or no friends");
                yield break;

            case 1:
                var friendsScoresData = data.ValueObjectList("stats");
                FriendsScoresData = friendsScoresData;

                ScoresController.Instance.friendsScores.Clear();

                foreach (var o in friendsScoresData)
                {
                    ScoresController.Instance.friendsScores.AddItem(o as Dictionary<string, object>);
                    yield return null;
                }

                ScoresController.Instance.friendsScores.Reposition();
                Dispatcher.Send(EventId.FriendsScoresHighlightedItemsReady, null);
                break;
        }
    }
}
