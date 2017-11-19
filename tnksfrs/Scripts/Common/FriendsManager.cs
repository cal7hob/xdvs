using Http;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using XD;

public class FriendsManager : MonoBehaviour, IFriendsManager
{
    public List<string>         socialFriendsUids = new List<string>();

    [SerializeField]
    private float               friendsUpdateInterval = 60 * 5; // Seconds

    private double              nextFriendsUpdateTime;
    private string              social = null;

    public List<object> FriendsScoresData
    {
        get;
        private set;
    }

    #region IStatic
    public bool IsEmpty
    {
        get
        {
            return false;
        }
    }

    public StaticType StaticType
    {
        get
        {
            return StaticType.UI;
        }
    }

    public void SaveInstance()
    {
        StaticContainer.Set(StaticType, this);
    }

    public void DeleteInstance()
    {
        StaticContainer.Set(StaticType, null);
    }
    #endregion

    #region ISender
    public string Description
    {
        get
        {
            return "[FriendsManager] " + name;
        }

        set
        {
            name = value;
        }
    }

    private List<ISubscriber> subscribers = null;

    public List<ISubscriber> Subscribers
    {
        get
        {
            if (subscribers == null)
            {
                subscribers = new List<ISubscriber>();
            }
            return subscribers;
        }
    }

    public void AddSubscriber(ISubscriber subscriber)
    {
        if (Subscribers.Contains(subscriber))
        {
            return;
        }
        Subscribers.Add(subscriber);
    }

    public void RemoveSubscriber(ISubscriber subscriber)
    {
        Subscribers.Remove(subscriber);
    }

    public void Event(Message message, params object[] parameters)
    {
        for (int i = 0; i < Subscribers.Count; i++)
        {
            Subscribers[i].Reaction(message, parameters);
        }
    }
    #endregion

    #region ISubscriber       
    public void Reaction(Message message, params object[] parameters)
    {
        switch (message)
        {
            case Message.PlayerActionRequest:
                switch (parameters.Get<PlayerActionRequest>())
                {
                    case PlayerActionRequest.AddFriend:
                        AddToFriends(parameters.Get<int>());
                        break;

                    case PlayerActionRequest.RemoveFriend:
                        RemoveFromFriends(parameters.Get<int>());
                        break;
                }
                break;

            case Message.DataResponse:
                switch (parameters.Get<DataKey>())
                {
                    case DataKey.AddFriend:
                        break;

                    case DataKey.RemoveFriend:
                        break;
                }
                break;

        }
    }
    #endregion

    private void Awake()
    {
        GetSocialFriends();
        StaticType.SocialSettings.Instance<ISocialSettings>().LoginSucceed += OnSocialLogin;
        Dispatcher.Subscribe(EventId.AfterFacebookMainInfoLoaded, Handler);
    }

    private void Handler(EventId id, EventInfo info)
    {
        GetSocialFriends();
    }

    private void OnDestroy()
    {
        if (StaticType.SocialSettings.Instance<ISocialSettings>() != null)
        {
            StaticType.SocialSettings.Instance<ISocialSettings>().LoginSucceed -= OnSocialLogin;
        }

        Dispatcher.Unsubscribe(EventId.AfterFacebookMainInfoLoaded, Handler);
    }

    private void OnSocialLogin()
    {
        GetSocialFriends();
    }

    private void GetSocialFriends()
    {
        socialFriendsUids = StaticType.SocialSettings.Instance<ISocialSettings>().GetSocialService().GetFriendsAppUsersIds();

        if (socialFriendsUids.Count > 0)
        {
            social = SocialSettings.Platform.ToString().ToLower();
            if (social.Equals("moimir"))
            {
                social = "mailru";
            }
        }

        ForceUpdateFriends();
    }

    private void AddSocialFriendsToRequest(Http.Request request)
    {
        if (social != null && socialFriendsUids.Count > 0)
        {
            request.Form.AddField("friends", MiniJSON.Json.Serialize(socialFriendsUids));
            request.Form.AddField("social", social);
        }
    }

    public void AddToFriends(int playerId, Action successCallback = null)
    {
        var request = Manager.Instance().CreateRequest("/player/addFriend");
        request.Form.AddField("playerId", playerId);

        AddSocialFriendsToRequest(request);

        Manager.StartAsyncRequest(
            request: request,
            failCallback: result => MessageBox.Show(MessageBox.Type.Info, "Failed to add friend"),
            successCallback: result =>
         {
             successCallback.SafeInvoke();
         });
    }

    public void RemoveFromFriends(int playerId, Action successCallback = null)
    {
        var request = Manager.Instance().CreateRequest("/player/removeFriend");
        request.Form.AddField("playerId", playerId);

        AddSocialFriendsToRequest(request);

        Manager.StartAsyncRequest(
            request: request,
            failCallback: result => MessageBox.Show(MessageBox.Type.Info, "Failed to remove friend"),
            successCallback: result =>
         {
             successCallback.SafeInvoke();
         });
    }

    public void UpdateFriends(double timeStamp)
    {
        if (nextFriendsUpdateTime < GameData.CurrentTimeStamp)
        {
            ForceUpdateFriends();
        }
    }

    public void ForceUpdateFriends()
    {
        var request = Manager.Instance().CreateRequest("/statistics/friends");
        AddSocialFriendsToRequest(request);

        Manager.StartAsyncRequest(request,
            successCallback =>
            {
            },
            result => Debug.Log("Friends update failed"));

        nextFriendsUpdateTime = GameData.CurrentTimeStamp + friendsUpdateInterval;
    }

    private IEnumerator FriendsUpdated(Response result)
    {
        var data = new JsonPrefs(result.Data);
        if (!data.Contains("status") || !data.Contains("stats"))
        {
            yield break;
        }

        switch (data.ValueInt("status"))
        {
            case 0:
                Debug.LogError("Failed to update friends list or no friends");
                yield break;

            case 1:
                var friendsScoresData = data.ValueObjectList("stats");
                FriendsScoresData = friendsScoresData;

                foreach (var o in friendsScoresData)
                {
                    yield return null;
                }

                break;
        }
    }
}
