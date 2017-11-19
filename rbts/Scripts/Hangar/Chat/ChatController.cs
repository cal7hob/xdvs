#if (UNITY_IPHONE || UNITY_ANDROID || UNITY_WP8) && !UNITY_EDITOR
    #define TOUCH_SCREEN_KEYBOARD
#endif

using System;
using System.Collections.Generic;
using Tanks.Models;
using UnityEngine;

public class ChatController : MonoBehaviour
{
    public ChatItemMessage chatMessagePrefab;
    public ChatItemRequestToJoinClan requestToJoinClanPrefab;
    public ChatItem joinClanMessagePrefab;

    public tk2dUIScrollableArea scrollArea;

    public tk2dUITextInput messageInput;
    public tk2dUIUpDownButton btnSend;

    public TabControl tabControl;

    public AudioClip clickSound;

    public Dictionary<ChatRoom?, ChatPage> chatRooms = new Dictionary<ChatRoom?, ChatPage>();
    public ChatPage currentChatPage;

    public int messagesToShow = 100;
    public int spacingBetweenMessages = 20;

    [SerializeField]
    private int maxMessageMeshXBound;
    [SerializeField]
    private string nickNameDelimiter = ", ";

    public const string UNKNOWN_AREA_NAME = "UNKNOWN";

    private int lastCamHeight; //TODO: Why not float?

    [SerializeField]
    private bool userTyped;
    [SerializeField]
    private bool nickClicked;

    public static ChatRoom? ChatRoomFromKey(string key)
    {
        try
        {
            return (ChatRoom)Enum.Parse(typeof(ChatRoom), key, true);
        }
        catch (Exception ex)
        {
            // Don't do it at home, baby
            // http://blogs.msdn.com/b/dotnet/archive/2009/02/19/why-catch-exception-empty-catch-is-bad.aspx

            Debug.LogError("Enum conversion failed: " + ex.Message);
            return null;
        }
    }

    public void InitCommonChatRooms(Chat chatModel)
    {
        //Debug.LogWarningFormat("<color=yellow>{0}</color>", "InitCommonChatRooms");

        // Iterate over ChatRoom enum
        foreach (ChatRoom room in Enum.GetValues(typeof(ChatRoom)))
        {
            if (!chatModel.Rooms.ContainsKey(room)
                || chatModel.Rooms[room].Name == UNKNOWN_AREA_NAME
                || chatRooms.ContainsKey(room)
                || room == ChatRoom.Clan)//Для кланов своя функция
                continue;
            //Debug.LogWarning("Before creating " + areaName + " room");

            var tempPage = ChatPage.Create(room, scrollArea);
            tempPage.gameObject.SetActive(false);
            chatRooms[room] = tempPage;

            tabControl.AddTab(tabControl.tabs.Count, chatModel.Rooms[room]);
        }
    }

    public void InitClanChatRoom(Chat chatModel = null)
    {
        //Debug.LogWarningFormat("<color=yellow>{0}</color>", "InitClanChatRoom");
    
        var clanRoomName = ChatRoom.Clan;

        if (chatRooms.ContainsKey(ChatRoom.Clan))
            return;

        var tempPage = ChatPage.Create(ChatRoom.Clan, scrollArea);
        tempPage.gameObject.SetActive(false);
        chatRooms[ChatRoom.Clan] = tempPage;

        if (chatModel != null && chatModel.Rooms.ContainsKey(ChatRoom.Clan))
        {
            tabControl.AddTab(0, chatModel.Rooms[ChatRoom.Clan]);
            
            if (currentChatPage == null)
                tabControl.SwitchTo(clanRoomName);
        }
        else
        {
            if (ProfileInfo.Level > GameData.accountManagementMinLevel)
            {
                tabControl.AddTab(0, new Tanks.Models.Room(ChatRoom.Clan.ToString(), "", ChatRoom.Clan));
            }

            chatRooms[ChatRoom.Clan].AddJoinClanMessage();
        }
    }

    private void InitChatRooms(Chat chatModel, ChatRoom? room)
    {
        //Debug.LogWarningFormat("<color=yellow>{0}</color>", "InitChatRooms");

        if (room == ChatRoom.Unspecified)
            InitCommonChatRooms(chatModel);

        if (room == ChatRoom.Clan)
            InitClanChatRoom(chatModel);

        if (currentChatPage != null) return;

        foreach (ChatRoom roomToSwitchTo in Enum.GetValues(typeof (ChatRoom)))
        {
            if (chatRooms.ContainsKey(roomToSwitchTo))
            {
                tabControl.SwitchTo(roomToSwitchTo);
                break;
            }
        }
    }

    public void ChatDataReceived(Dictionary<string, object> data, ChatRoom? receivedForRoom)
    {
        var someMessagesWereAdded = false;
        Chat chatModel;

        if (receivedForRoom == ChatRoom.Clan)
            chatModel = new Chat(ChatRoom.Clan, new Tanks.Models.Room(data), default(long));
        else
            chatModel = new Chat(data);

        //Заполняем тип комнаты в саму комнату, для удобства, чтоб в этом классе была вся необходимая инфа
        foreach (KeyValuePair<ChatRoom, Tanks.Models.Room> pair in chatModel.Rooms)
            pair.Value.type = pair.Key;

        InitChatRooms(chatModel, receivedForRoom);

        if (chatModel.Ban > 0)
        {
            //Debug.LogWarning("ChatDataReceived: chatModel.Ban > 0");
            ShowBannedForMessage(chatModel.Ban - Convert.ToInt64(GameData.CorrectedCurrentTimeStamp));
            //StartCoroutine(BannedWarning());
        }

        // Iterate over ChatRoom enum
        foreach (ChatRoom room in Enum.GetValues(typeof(ChatRoom)))
        {
            if (!chatRooms.ContainsKey(room)
                || !chatModel.Rooms.ContainsKey(room))
                continue;

            if(chatModel.Rooms[room].Messages.Count > 0)
            {
                foreach (var message in chatModel.Rooms[room].Messages)
                {
                    //Debug.LogWarning("Country: " + areaName + "MessageId: " + message.Id + "\nMessage: " + message.Content + "\ncreateTime: " + message.CreateTime + "\nNick: " + message.NickName + "\nCode: " + chatModel.Rooms[areaName].Code);
                    //Debug.LogWarning("Room: " + areaName + "; MessageId: " + message.Id + "\nMessage: " + message.Content + "\ncreateTime: " + message.CreateTime + "\nNick: " + message.NickName);

                    //if (lastId.ContainsKey(tab))
                    //    Debug.LogWarning("lastId[tab] == " + lastId[tab]);

                    //Debug.LogWarning("message.Id == " + message.Id);

                    if (chatRooms[room].lastId.HasValue)
                    {
                        // We got same message already
                        if (!(chatRooms[room].lastId < message.Id))
                            continue;
                        //Debug.LogWarning("Adding message in IF: " + message.Content);

                        chatRooms[room].AddChatMessage(message);
                        chatRooms[room].lastId = message.Id;
                        someMessagesWereAdded = true;
                    }
                    else
                    {
                        // First message, add anyway
                        //Debug.LogWarning("Adding message in ELSE: " + message.Content);

                        chatRooms[room].AddChatMessage(message);
                        chatRooms[room].lastId = message.Id;
                        someMessagesWereAdded = true;
                    }
                }
            }
            
            if (room == ChatRoom.Clan && chatModel.Rooms[room].Requests != null && chatModel.Rooms[room].Requests.Count > 0)
            {
                foreach (var request in chatModel.Rooms[ChatRoom.Clan].Requests)
                {
                    //Debug.LogWarning("Got request, id: " + request.Id);
                    chatRooms[room].AddRequestToJoinClan(request);
                }
            }
        }

        if (someMessagesWereAdded)
        {
            UpdatePage();

            if (gameObject.GetActive())
                AudioDispatcher.PlayClip(clickSound, false);
        }
    }

    public void RemoveChatRoom(ChatRoom roomToRemove)
    {
        Destroy(chatRooms[roomToRemove].gameObject);
        chatRooms.Remove(roomToRemove);
        tabControl.RemoveTab(roomToRemove);

        if (currentChatPage != null && currentChatPage.room == roomToRemove)
            currentChatPage = null;
    }

    public void InsertNickName(string nickName)
    {
        messageInput.Text += nickName + nickNameDelimiter;

        nickClicked = true;
        OnMessageChanged(messageInput);
        nickClicked = false;
#if UNITY_WEBPLAYER || UNITY_WEBGL
        messageInput.SetFocus(true);
#endif
    }

    //private IEnumerator WaitInaccessibleWarning()
    //{
    //    yield return new WaitForSeconds(3);
    //    MessageBox.HideHardMessage();
    //}

    //private IEnumerator BannedWarning()
    //{
    //    yield return new WaitForSeconds(3);
    //    MessageBox.HideHardMessage();
    //}

    private void SendChatMessage()
    {
        if (string.IsNullOrEmpty(messageInput.Text) || currentChatPage == null)
            return;

        if (currentChatPage.room == ChatRoom.Clan && ProfileInfo.Clan == null)
            return;

        //Debug.LogWarning("Current room: " + ChatRoomFromKey(chatPanelSwitch.CurrentPanel.key));
        //Debug.LogWarning("ProfileInfo.Level == " + ProfileInfo.Level);
        //Debug.LogWarning("chatAccessMinLevel == " + chatAccessMinLevel);
        //if (ProfileInfo.Level >= chatAccessMinLevel || Debug.isDebugBuild)

        if (ProfileInfo.Level >= GameData.chatMinAccessLevel)
        {
            ChatManager.Instance.SendChatMessage(messageInput.Text, currentChatPage.room);
            currentChatPage.scrollArea.Value = 0;
        }
        else
        {
            MessageBox.Show(MessageBox.Type.Info, Localizer.GetText("lblInaccessible", GameData.chatMinAccessLevel));
            //StartCoroutine(WaitInaccessibleWarning());
        }

        messageInput.Text = "";
        userTyped = false;
#if !(UNITY_WEBPLAYER || UNITY_WEBGL)
        messageInput.SetFocus(false);
#endif
        OnMessageChanged(messageInput);
    }

    private void UpdatePage()
    {
        //Debug.LogError("UpdatePage(): currentChatPage: " + currentChatPage);
        //Debug.LogError("UpdatePage(): chatPanelSwitch.CurrentPanel.key: " + chatPanelSwitch.CurrentPanel.key);

        currentChatPage.Reposition();
    }

    private void RoomChanged(string key)
    {
        var room = ChatRoomFromKey(key);

        //Debug.LogError("ChatController.RoomChanged: currentChatPage == " + currentChatPage);
        //Debug.LogError("ChatController.RoomChanged: room == " + room);
        //Debug.LogError("ChatController.RoomChanged: chatRooms == " + chatRooms);

        if (currentChatPage != null)
        {
            //Debug.LogError("currentChatPage != null", currentChatPage);
            if (room != ChatRoom.Clan && gameObject.GetActive())
                XdevsSplashScreen.SetActiveWaitingIndicator(true);

            currentChatPage.gameObject.SetActive(false);

            ChatManager.Instance.LoadChatMessages();
        }

        if (room == null || !chatRooms.ContainsKey(room))
            return;

        currentChatPage = chatRooms[room];
        currentChatPage.gameObject.SetActive(true);

        scrollArea.contentContainer = currentChatPage.gameObject;
        scrollArea.ContentLayoutContainer = currentChatPage.gameObject.GetComponent<tk2dUILayoutContainerSizer>();
        scrollArea.ContentLayoutContainer.Refresh();

        currentChatPage.scrollArea.Value = 0;
        UpdatePage();
    }

    /// <summary>
    /// <para>Используется для получения отображаемого текста чата.</para>
    /// <para>Если открыта виртуальная клавиатура, брать текст из неё, иначе будет баг.</para>
    /// </summary>
    private string GetMessageText(tk2dUITextInput textInput)
    {
#if TOUCH_SCREEN_KEYBOARD
        var keyboard = textInput.TouchScreenKeyboard;
        
        if (keyboard != null)
            return keyboard.text;
        else
            return textInput.Text;
#else
        return textInput.Text;
#endif
    }

    /// <summary>
    /// <para>Используется для установки отображаемого текста чата.</para>
    /// <para>Если открыта виртуальная клавиатура, менять текст в ней.</para>
    /// </summary>
    private void SetMessageText(tk2dUITextInput textInput, string text)
    {
#if TOUCH_SCREEN_KEYBOARD
        var keyboard = textInput.TouchScreenKeyboard;

        if (keyboard != null)
            keyboard.text = text;
        else
            textInput.Text = text;
#else
        textInput.Text = text;
#endif
    }

    /// <summary>
    /// <para>Используется для выключения коллайдера кнопки «Отправить»,
    /// чтобы её нельзя было нажать без текста.</para>
    /// <para>Изменяет количество вводимых в поле символов,
    /// в зависимости от общей ширины текста.</para>
    /// </summary>
    private void OnMessageChanged(tk2dUITextInput textInput)
    {
        var text = GetMessageText(textInput);

        //Debug.LogWarning("OnMessageChanged BEFORE test: getMessageText(textInput) == " + text + "(" + text.Length + ")");
        //Debug.LogWarning("textMeshWidth == " + textInput.inputLabel.GetEstimatedMeshBoundsForString(text).size.x);

        if (text.Length > 0)
        {
            if (!nickClicked)
                userTyped = true;

            if (userTyped)
            {
                btnSend.gameObject.GetComponent<Collider>().enabled = true;
            }
            else
            {
                btnSend.gameObject.GetComponent<Collider>().enabled = false;
            }

            if (textInput.inputLabel.GetEstimatedMeshBoundsForString(text).size.x > maxMessageMeshXBound)
            {
                HelpTools.ClampLabelText(textInput.inputLabel, maxMessageMeshXBound);

                SetMessageText(textInput, text);
                textInput.maxCharacterLength = text.Length;
            }
            else
            {
                // Почему 150, если в textMesh влезает меньше? Потому что 150 — ограничение на сервере.
                textInput.maxCharacterLength = 150;
            }
        }
        else
        {
            btnSend.gameObject.GetComponent<Collider>().enabled = false;
        }

        //text = getMessageText(textInput);
        //Debug.LogWarning("OnMessageChanged AFTER test: getMessageText(textInput) == " + text + "(" + text.Length + ")");
        //Debug.LogWarning("textMeshWidth == " + textInput.inputLabel.GetEstimatedMeshBoundsForString(text).size.x);
    }

    private void ShowBannedForMessage(long bannedForSeconds)
    {
        MessageBox.Show(MessageBox.Type.Info, Localizer.GetText("ChatBannedFor", Clock.GetTimerString(bannedForSeconds)));
    }

    private void SetUpChatHeight()
    {
        var delta = HangarController.Instance.Tk2dGuiCamera.ScreenExtents.yMin - gameObject.GetComponent<tk2dUILayout>().GetMinBounds().y;

        // Для того, чтобы при Reshape() изменялся tk2dUIScrollableArea.VisibleAreaLength,
        // нужно накинуть родительский tk2dUILayout на tk2dUIScrollableArea.BackgroundLayoutItem.
        gameObject.GetComponent<tk2dUILayout>().Reshape(new Vector3(0, delta, 0), Vector3.zero, true);
    }

    private void Awake()
    {
        messageInput.OnTextChange += OnMessageChanged;
        messageInput.inputLabel.maxChars = messageInput.maxCharacterLength;//Чтобы пофиксить бредовую баговую ситуацию с вводом большего чем maxChars количества символов
        tabControl.OnTabChanged += RoomChanged;
    }

    private void Start()
    {
        SetUpChatHeight();
    }

    private void Update()
    {
#if TOUCH_SCREEN_KEYBOARD
        var keyboard = messageInput.TouchScreenKeyboard;

        if (keyboard != null)
        {
            if (keyboard.done && !keyboard.wasCanceled)
            {
                SendChatMessage();
            }
        }
#endif
        //Debug.LogWarning("Input.GetKeyDown() == " + Input.anyKeyDown);
        //Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter) || 
        if ((Input.GetKeyDown("return") || Input.GetKeyDown("enter")) && userTyped)
            SendChatMessage();

        if (Debug.isDebugBuild)
        {
            if (Input.GetKey(KeyCode.LeftAlt))
            {
                if (Input.GetKeyDown(KeyCode.Z))
                {
                    messageInput.Text = "(V) ( ;,,;) (V)"; //"(V) (°,,,,°) (V)";
                    SendChatMessage();
                }
                if (Input.GetKeyDown(KeyCode.X))
                {
                    messageInput.Text = "(V) (;,,; ) (V)";
                    SendChatMessage();
                }
            }
        }
    }

    private void LateUpdate()
    {
        if ((int)HangarController.Instance.Tk2dGuiCamera.ScreenExtents.yMin != lastCamHeight)
        {
            //Debug.LogError("(int)cam.ScreenExtents.yMin == " + (int)cam.ScreenExtents.yMin);

            lastCamHeight = (int)HangarController.Instance.Tk2dGuiCamera.ScreenExtents.yMin;

            SetUpChatHeight();
        }
    }
}
