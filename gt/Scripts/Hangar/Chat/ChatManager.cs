using System;
using Tanks.Models;
using UnityEngine;

public class ChatManager : MonoBehaviour
{
    public GameObject prefabContainer;
    public GameObject chatPrefab;
    public float downloadInvocationRepeatRate = 5f;
    public ChatController chatController;
    public ActivatedUpDownButton chatToggleButton;
    
    private GameObject chat;
    private bool firstInvocation = true;

    public static ChatManager Instance;

    protected delegate void WWWResultCallback(WWW result);

    void Awake()
    {
        Instance = this;

        if (chatPrefab == null || prefabContainer == null)
        {
            Debug.LogError("You have to assign Chat Prefab in Controller_hangar's Chat Manager component!");
            Debug.LogError("You have to assign Prefab Container in Controller_hangar's Chat Manager component!");
            return;
        }

        Dispatcher.Subscribe(EventId.AfterHangarInit, Init);
        Dispatcher.Subscribe(EventId.ClanChanged, OnClanChanged);
    }

	void Start()
	{
		chatToggleButton.Activated = false;
	}

    void OnDestroy()
    {
        Dispatcher.Unsubscribe(EventId.AfterHangarInit, Init);
        Dispatcher.Unsubscribe(EventId.ClanChanged, OnClanChanged);

        Instance = null;
    }

    private void Init(EventId id, EventInfo info)
    {
        if (chat == null)
        {
            //Debug.LogWarning("Instantiating chatPrefab");

            chat = Instantiate(chatPrefab);
            chat.name = chatPrefab.name;
            var pos = chat.transform.localPosition;
            chat.transform.parent = prefabContainer.transform;
            chat.transform.localPosition = pos;
        }

        chat.SetActive(false);
        
        chatController = chat.GetComponent<ChatController>();

        if (ProfileInfo.Clan == null)
            chatController.InitClanChatRoom();

        this.InvokeRepeating(LoadChatMessages, 0, downloadInvocationRepeatRate);

        chatToggleButton.Activated = true;
    }

    private void OnChatButtonClicked()
    {
        if (chat == null || RightPanel.Instance.rightPanel.scrollableArea.IsSwipeScrollingInProgress)
            return;

        GUIPager.SetActivePage("Chat");
        Dispatcher.Send(EventId.VoiceRequired, new EventInfo_I((int) VoiceEventKey.ChatEnter));
        chat.SetActive(true);
        LoadChatMessages();
    }

    public void LoadChatMessages()
    {
        // �� ����������� ���������, ���� ���� ���� ������, � ������������������� ����.
        if (!chat.GetActive())
        {
            if (!firstInvocation)
                return;

            firstInvocation = false;
        }

        var req = Http.Manager.Instance().CreateRequest("/chat/getMessages");

        // Request production server instead
        //var instance = Http.Manager.Instance();
        //instance.server =
        //    GameData.IsGame(Game.IronTanks)
        //        ? "irontanks.extreme-developers.com"
        //        : "futuretanks.extreme-developers.com";
        //var req = instance.CreateRequest("/chat/getMessages");

        // Iterate over ChatRoom enum
        foreach (ChatRoom room in Enum.GetValues(typeof(ChatRoom)))
        {
            if (chatController.chatRooms.ContainsKey(room) 
                && chatController.chatRooms[room].lastId.HasValue)
            {
                req.Form.AddField(room.ToString().ToLower(), 
                    chatController.chatRooms[room].lastId.Value);
            }
        }

        StartCoroutine(req.Call(
            successCallback: delegate(Http.Response result)
            {
                ChatDataLoaded(result);
                XdevsSplashScreen.SetActiveWaitingIndicator(false);
            },
            failCallback: delegate(Http.Response result)
            {
                XdevsSplashScreen.SetActiveWaitingIndicator(false);
                Debug.LogError("Can't load chat data. Error: " + result.error);
                Debug.LogError("Result text: " + result.text);
            }));

        if (ProfileInfo.Clan != null)
            LoadClanMessages();
    }

    private void LoadClanMessages()
    {
        var req = Http.Manager.Instance().CreateRequest("/chat/getClanMessages");

        if (chatController.chatRooms.ContainsKey(ChatRoom.Clan) 
            && chatController.chatRooms[ChatRoom.Clan].lastId.HasValue)
        {
            req.Form.AddField(ChatRoom.Clan.ToString().ToLower(),
                chatController.chatRooms[ChatRoom.Clan].lastId.Value);
        }

        StartCoroutine(req.Call(
            successCallback: delegate(Http.Response result)
            {
                ChatDataLoaded(result, ChatRoom.Clan);
                XdevsSplashScreen.SetActiveWaitingIndicator(false);
            },
            failCallback: delegate(Http.Response result)
            {
                XdevsSplashScreen.SetActiveWaitingIndicator(false);
                Debug.LogError("Can't load chat data. Error: " + result.error);
                Debug.LogError("Result text: " + result.text);
            }));
    }

    private void ChatDataLoaded(Http.Response result, ChatRoom receivedForRoom = ChatRoom.Unspecified)
    {
        chatController.ChatDataReceived(result.Data, receivedForRoom);
    }

    public void SendChatMessage(string message, ChatRoom room)
    {
        //Debug.LogWarning("Sending message: \"" + message + "\" from \"" + room + "\" room");
        var setMessageRoute = room == ChatRoom.Clan ? "/chat/setClanMessage" : "/chat/setMessage";
       
        var req = Http.Manager.Instance().CreateRequest(setMessageRoute);

        req.Form.AddField("room", room.ToString().ToLower());
        req.Form.AddField("message", message);
        if (chatController.chatRooms[room].lastId.HasValue)
            req.Form.AddField("lastMessageId", chatController.chatRooms[room].lastId.Value);

        XdevsSplashScreen.SetActiveWaitingIndicator(true);

        StartCoroutine(req.Call(
            successCallback: delegate(Http.Response result)
            {
                ChatDataLoaded(result, room);
                XdevsSplashScreen.SetActiveWaitingIndicator(false);
            },
            failCallback: delegate(Http.Response result)
            {
                XdevsSplashScreen.SetActiveWaitingIndicator(false);
                Debug.LogError("Can't send message to our server. Error: " + result.error);
            }));
    }

    private void OnClanChanged(EventId id, EventInfo info)
    {
        if (chatController == null)
            return;

        if (chatController.chatRooms.ContainsKey(ChatRoom.Clan))
            chatController.RemoveChatRoom(ChatRoom.Clan);

        if (ProfileInfo.Clan != null)
            LoadClanMessages();
        else
            chatController.InitClanChatRoom();
    }
}