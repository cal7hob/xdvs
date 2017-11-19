using System.Collections.Generic;
using Tanks.Models;
using UnityEngine;

public class ChatPage : MonoBehaviour
{
    public tk2dUIScrollableArea scrollArea;
    public bool scrollToMessage;
    public int? lastId;
    public ChatRoom room;
    public tk2dUILayoutContainerSizer containerSizer;

    public List<Request> requests = new List<Request>();
    
    private float oldContentLength;

    public static ChatPage Create(ChatRoom room, tk2dUIScrollableArea parent)
    {
        var o = new GameObject("Content_" + room);

        o.transform.parent = parent.transform;
        o.transform.localPosition = Vector3.zero;

        var chatPage = o.AddComponent<ChatPage>();

        chatPage.containerSizer = o.AddComponent<tk2dUILayoutContainerSizer>();
        chatPage.containerSizer.spacing = ChatManager.Instance.chatController.spacingBetweenMessages;
        chatPage.scrollArea = parent;
        chatPage.room = room;
        
        return chatPage;
    }

    public void AddChatMessage(Message message)
    {
        ChatItem
            .Create(this, ChatManager.Instance.chatController.chatMessagePrefab)
            .Init(message);

        RestorePosition();
    }

    public void AddRequestToJoinClan(Request request)
    {
        if (requests.Contains(request))
            return;

        var requestToJoinClan = 
            ChatItem
                .Create(this, ChatManager.Instance.chatController.requestToJoinClanPrefab);

        if (requestToJoinClan == null)
            return;

        requestToJoinClan.Init(request);
            
        requests.Add(request);

        RestorePosition();
    }

    public void AddJoinClanMessage()
    {
        ChatItem
            .Create(this, ChatManager.Instance.chatController.joinClanMessagePrefab);

        RestorePosition();
    }

    public void Reposition()
    {
        //Debug.LogWarning("ChatPage.cs: Reposition()");
        scrollToMessage = true;
    }

    public void SaveOldContentLength()
    {
        if (gameObject.GetActive())
            oldContentLength = scrollArea.ContentLength;
    }

    void RestorePosition()
    {
        // Пытаемся восстановить позицию скролла
        if (gameObject.GetActive() 
            && scrollArea.Value != 0 
            && scrollArea.ContentLength > scrollArea.VisibleAreaLength)
        {
            scrollArea.Value += 0.001f +
                (scrollArea.ContentLength - oldContentLength) / scrollArea.ContentLength;
        }
    }

    void Update()
    {
        if (scrollToMessage && scrollArea.Value == 0)
        {
            scrollToMessage = false;
            scrollArea.Value = 0;
        }
    }
}