using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using XDevs.Notifications.Models;
using JSONObject = System.Collections.Generic.Dictionary<string, object>;

public class NotificationsManager : MonoBehaviour, IQueueablePage
{
    [SerializeField] private NotificationWindow notificationWindowPrefab;
    [SerializeField] private Transform notificationWindowParentWrapper;
    [SerializeField] private bool debug;

    [SerializeField] private List<Notification> notifications;
    public List<Notification> Notifications { get { return notifications; } }

    public event System.Action OnNotificationsChanged;

    public static bool Dbg
    {
        get { return Instance != null && Instance.debug; }
    }

    public static NotificationsManager Instance { get; private set; }

    private void Awake()
    {
        Instance = this;
        Messenger.Subscribe(EventId.AfterHangarInit, ProcessNotifications);
        Messenger.Subscribe(EventId.ProfileInfoLoadedFromServer, ProcessNotifications);
        OnNotificationsChanged += NotificationsChangedHandler;
    }

    private void OnDestroy()
    {
        Messenger.Unsubscribe(EventId.AfterHangarInit, ProcessNotifications);
        Messenger.Unsubscribe(EventId.ProfileInfoLoadedFromServer, ProcessNotifications);
        OnNotificationsChanged -= NotificationsChangedHandler;
        Instance = null;
    }

    public void ProcessNotifications(EventId id, EventInfo ei)
    {
        if (Dbg)
            Debug.LogError("ProcessNotifications(), ProfileInfo.notificationsList: "
                + MiniJSON.Json.Serialize(ProfileInfo.notificationsList));

        if (ProfileInfo.notificationsList == null)
            return;

        var notificationsModel = new NotificationsModel(ProfileInfo.notificationsList);
        ProfileInfo.notificationsList = null;

        if (notificationsModel.Notifications == null || notificationsModel.Notifications.Count < 1)
            return;

        StartCoroutine(ProcessNotificationsCoroutine(notificationsModel.Notifications));
    }

    private IEnumerator ProcessNotificationsCoroutine(List<Notification> notifications)
    {
        if (Dbg)
            Debug.LogError("ProcessNotificationsCoroutine()");

        if (this.notifications == null)
            this.notifications = new List<Notification>();

        var notificationWasAdded = false;

        foreach (var notification in notifications)
        {
            if (Dbg)
                Debug.LogError("Got notification: " + notification);

            while (!notification.loaded)
                yield return null;

            if (!this.notifications.Contains(notification))
            {
                if (Dbg)
                    Debug.LogError("Notification added: " + notification);

                this.notifications.Add(notification);
                notificationWasAdded = true;
            }
        }

        if (notificationWasAdded)
            OnNotificationsChanged.SafeInvoke();
    }

    public void NotificationsChangedHandler()
    {
        if (Dbg)
            Debug.LogError("NotificationsChangedHandler()");

        if (notifications.Count > 0)
        {
            if (GUIPager.ActivePage != "NotificationWindow")
            {
                // TODO: решить что делать с этим. если переходим на страницу, например, в Банк.
                GUIPager.EnqueuePage("NotificationWindow", false, true);
            }
            else
                BeforeActivation();
        }
        else
        {
            if (GUIPager.ActivePage == "NotificationWindow")
            {
                GUIPager.ToMainMenu();
                Destroy(NotificationWindow.Instance.gameObject);
            }
        }
    }

#if UNITY_EDITOR
    public static void TestFromEditor()
    {
        var notificationsMockupJson = @"
            {
              ""Notifications"": [
                {
                  ""id"": 1,
                  ""header"": ""Hello world!"",
                  ""text"": ""^cffffLorem ipsum ^CD78438FFdolor sit amet^cffff, consectetur adipiscing elit. Maecenas consequat leo ut nisi ultricies imperdiet. Nulla viverra ac nulla et volutpat. Phasellus ac porttitor justo. Proin mauris magna, luctus sit amet lorem ultricies, laoreet finibus ligula. Cras scelerisque tellus et quam pulvinar faucibus. Aenean blandit at orci at ornare. Phasellus fringilla nisi justo, vel posuere magna ultricies non. In hac habitasse platea dictumst. Vestibulum rutrum ex sit amet purus maximus hendrerit. Morbi molestie vel nunc at posuere. Mauris sollicitudin quis dolor vel ultrices. Sed id felis quis leo ultricies hendrerit. Integer at congue orci. Mauris tristique dictum sollicitudin. Pellentesque nec nunc ac tellus convallis placerat varius a risus. Nulla pulvinar dolor vitae quam iaculis tristique.\r\nNullam tortor metus, suscipit eget purus eu, ultrices fringilla elit. Fusce pretium at nisi nec viverra. In non odio eleifend, tincidunt urna vel, maximus ex. Curabitur ut eros ac nibh iaculis consectetur quis eu sapien. Praesent commodo elit in urna eleifend imperdiet. Sed id egestas nisl. Curabitur vel nulla est. Praesent tortor augue, iaculis quis nunc eget, cursus aliquet ipsum. Nullam posuere dapibus magna vitae euismod. Proin commodo quam erat, scelerisque tincidunt neque finibus eu. Mauris eget nisi faucibus, sollicitudin nulla sed, rhoncus felis. Sed ante lacus, viverra a eleifend nec, commodo nec dui. Nulla condimentum placerat turpis sed pulvinar."",
                  ""imageUri"": ""http://scifi-tanks.com/files/screenshots/reflexing/Discount@2x.png"",
                  ""sender"": {
                    ""playerId"": 666,
                    ""nickName"": ""Доброжелатель""
                  },
                  ""buttons"": [
                {
                      ""button"": ""Close"",
                    },
                    {
                  ""button"": ""Action"",
                  ""action"": {
                    ""type"": ""Page"",
                    ""location"": ""Bank"",
                    ""tab"": ""vip"",
                    ""value"": ""xdevs.7_vip""
                  }
                }
                  ]
                },
                {
                  ""id"": 2,
                  ""header"": ""Переход по ссылке!"",
                  ""text"": ""Тестим баг"",
                  ""imageUri"": ""http://66.media.tumblr.com/2b18f0d4adc2f486dfba515c4d8afe15/tumblr_mjdfzhJgpj1rz5ayyo1_500.jpg"",
                  ""sender"": {
                    ""playerId"": 666,
                    ""nickName"": ""Доброжелатель""
                  },
                  ""buttons"": [
                    {
                      ""button"": ""Action"",
                      ""action"": {
                        ""type"": ""URL"",
                        ""value"": ""https://example.com""
                      }
                    }
                  ]
                },
                {
                  ""id"": 3,
                  ""header"": ""Hail Satan!"",
                  ""text"": ""The Earth at your very feet, man and woman for your pleasures and desires. You make the heavens tremble and the earth quake, when you call out to your followers and priests.  You appear like lightning over the highlands; you throw your power in fire across the Earth. Your deafening in command, whistling like the South Winds, You split apart great mountains. You trample the disobedient like rampant bulls; heaven and earth tremble for you are great in every way. Your frightful cry descending from the darkness devours its victims, to your pleasure. Your quivering hand causes the midday heat to hover over the sea, and sweat the brow of society. You are the commander of the day and the destroyer of the night, You are truly The Lord and Master.  Satan you are in my heart and my soul and I follow you till death, when I will stand beside you. You are my ruler! Hail Satan"",
                  ""imageUri"": ""http://66.media.tumblr.com/2b18f0d4adc2f486dfba515c4d8afe15/tumblr_mjdfzhJgpj1rz5ayyo1_500.jpg"",
                  ""sender"": {
                    ""playerId"": 666,
                    ""nickName"": ""Доброжелатель""
                  },
                  ""buttons"": [
                    {
                      ""button"": ""Close"",
                    }
                  ]
                }
              ]
            }
            ";

        var testNotificationsDict = MiniJSON.Json.Deserialize(notificationsMockupJson) as JSONObject;
        ProfileInfo.notificationsList = testNotificationsDict[ProfileInfo.notificationsJsonObjectName] as List<object>;

        Messenger.Send(EventId.ProfileInfoLoadedFromServer, null);
    }
#endif

    public void BeforeActivation()
    {
        if (Dbg)
        {
            //foreach (var notification in notifications)
            //{
            //    Debug.LogError("Got notification: " + notification);
            //}
        }

        if (notifications.Count > 0)
        {
            if (NotificationWindow.Instance == null)
                NotificationWindow.Instantiate(notificationWindowPrefab, notificationWindowParentWrapper);

            var notification = notifications[0];

            if (Dbg)
                Debug.LogError("Showing notification: " + notification);

            if (NotificationWindow.Instance != null)
                NotificationWindow.Instance.FillData(notification,
                    () =>
                    {
                        notifications.Remove(notification);
                        OnNotificationsChanged.SafeInvoke();
                    });
        }
    }

    public void Activated()
    {
    }
}
