using System.Collections.Generic;
using Tanks.Models;
using UnityEngine;

public class ChatMenuBehaviour : MenuBehaviour
{
    [SerializeField]
    private Player chatPlayer;
    [SerializeField]
    private tk2dUIScrollableArea chatScrollArea;

    private tk2dUILayout chatScrollAreaUILayout;
   
    private List<int> blacklistedPlayerIds = new List<int>();

    public static ChatMenuBehaviour Instance { get; private set; }

    protected override void Awake()
    {
        Instance = this;
        base.Awake();
        UpdateL10NAgents();

        chatScrollAreaUILayout = chatScrollArea.GetComponent<tk2dUILayout>();

        // Well, contextMenu hides when you click outside of it's bounds, and
        // when the message arrives and adds to chatScrollArea, it doesn't fire
        // OnScroll delegate, so, the purpose of this delegate is unclear.
        //
        // If it is needed, maybe it's worth adding it to ScoresMenuBehaviour's Awake().
        chatScrollArea.OnScroll += delegate { contextMenu.HideContextMenu(); };

        Messenger.Subscribe(EventId.OnLanguageChange, OnLanguageChanged);
    }

    void OnDestroy()
    {
        Messenger.Unsubscribe(EventId.OnLanguageChange, OnLanguageChanged);
        Instance = null;
    }

    private void OnLanguageChanged(EventId evId, EventInfo ev)
    {
        UpdateL10NAgents();
    }

    private void UpdateL10NAgents()
    {
        contextMenu.wrapper.transform.Find("ChatBan/lblChatBan").GetComponent<LabelLocalizationAgent>().Parameter =
             Clock.GetTimerString(GameData.chatModeratorAvailableBanTime, true);
    }

    public void ShowContextMenu(tk2dUIItem clickedUiItem, tk2dUILayout clickedItemsLayout, Player playerClickedOn)
    {
        chatPlayer = playerClickedOn;
        
        ((PlayerContextMenu)contextMenu)
            .SetAddRemoveFriendsLabel(!FriendsManager.isAlreadyFriends(playerClickedOn, (PlayerContextMenu)contextMenu));

        SetUpSpecialButtons();

        ShowContextMenu(clickedUiItem, clickedItemsLayout);
    }

    protected override void SetContextMenuPosition(tk2dUIItem clickedUiItem, tk2dUILayout clickedItemsLayout)
    {
        Vector3 menuPosShift;

        // ToonWars' background has nice pointing triangle, 
        // so we'll try to use it here.
        if (GameData.IsGame(Game.ToonWars))
            menuPosShift = new Vector2(140, -14); // custom ToonWars' shift
        else if(GameData.IsGame(Game.SpaceJet | Game.BattleOfWarplanes))
            menuPosShift = new Vector2(146, 0);
        else if(GameData.IsGame(Game.BattleOfHelicopters))
            menuPosShift = new Vector2(55, 0);
        else if (GameData.IsGame(Game.Armada))
            menuPosShift = new Vector2(55, 0);
        else if (GameData.IsGame(Game.IronTanks))
            menuPosShift = new Vector2(113, 0);
        else if (GameData.IsGame(Game.FutureTanks | Game.FTRobotsInvasion))
            menuPosShift = new Vector2(128, 0);
        else
        {
            Debug.LogError("ChatMenuBehaviour. Add Project Offset");
            // Y shift will equal message clickedItemsLayout's height + spacing
            menuPosShift = new Vector2(55, -(clickedItemsLayout.bMax - clickedItemsLayout.bMin).y
                - ChatManager.Instance.chatController.spacingBetweenMessages); 
        }

        var chatMenuPos = clickedItemsLayout.GetMaxBounds() + menuPosShift;
        contextMenu.transform.position = chatMenuPos;

        // Resetting ToonWars' background properties, which may be modified further
        contextMenu.sprBg.scale = new Vector2(contextMenu.sprBg.scale.x, Mathf.Abs(contextMenu.sprBg.scale.y));
        contextMenu.sprBg.anchor = tk2dBaseSprite.Anchor.UpperLeft;

        // Prevent ChatMenu from going under the BOTTOM chatScrollAreaUILayout's border
        if (sprBgRenderer.bounds.min.y < chatScrollAreaUILayout.GetMinBounds().y)
        {
            // Menu goes UP, so we flip background vertically for the arrow to point on clicked player
            chatMenuPos.y += contextMenu.sprBg.dimensions.y * contextMenu.sprBg.scale.y;
            contextMenu.sprBg.scale = new Vector2(contextMenu.sprBg.scale.x, -contextMenu.sprBg.scale.y);
            contextMenu.sprBg.anchor = tk2dBaseSprite.Anchor.LowerLeft;

            chatMenuPos.y -= (clickedItemsLayout.bMax.y - clickedItemsLayout.bMin.y) + (2 * menuPosShift.y);

            contextMenu.transform.position = chatMenuPos;
        }

        // Prevent ChatMenu from going under the TOP chatScrollAreaUILayout's border
        if (sprBgRenderer.bounds.max.y > chatScrollAreaUILayout.GetMaxBounds().y)
        {
            chatMenuPos.y = chatScrollAreaUILayout.GetMaxBounds().y;
        }

        contextMenu.transform.position = new Vector3(chatMenuPos.x, chatMenuPos.y, contextMenu.transform.position.z);
        base.SetContextMenuPosition(clickedUiItem, clickedItemsLayout);

        //Выставляем локальный Z в 0 чтобы избежать бага с невозможностью клика по контекстному меню
        contextMenu.transform.localPosition = new Vector3(contextMenu.transform.localPosition.x, contextMenu.transform.localPosition.y, 0);
    }

    #region ChatBan
    private void ChatBanQuestion(tk2dUIItem uiItem)
    {
        contextMenu.HideContextMenu();
        MessageBox.Show(MessageBox.Type.Question,
            Localizer.GetText("lblChatBanConfirmation", chatPlayer.NickName), ChatBan);
    }

    private void ChatBan(MessageBox.Answer answer)
    {
        if (answer == MessageBox.Answer.Yes)
            ChatBanRequest();
    }

    private void ChatBanRequest()
    {
        var request = Http.Manager.Instance().CreateRequest("/player/chatBan");
        request.Form.AddField("playerId", chatPlayer.Id);
        Http.Manager.StartAsyncRequest(request,
            delegate
            {
                MessageBox.Show(MessageBox.Type.Info,
                    Localizer.GetText("lblChatBanned",
                    chatPlayer.NickName,
                    Clock.GetTimerString(GameData.chatModeratorAvailableBanTime, true)));
            });
    }
    #endregion

    #region Complain
    private void ChatComplainQuestion(tk2dUIItem uiItem)
    {
        contextMenu.HideContextMenu();
        MessageBox.Show(MessageBox.Type.Question,
            Localizer.GetText("lblChatComplaintConfirmation", chatPlayer.NickName), ChatComplain);
    }

    private void ChatComplain(MessageBox.Answer answer)
    {
        if (answer == MessageBox.Answer.Yes)
            ChatSubmitComplaint(chatPlayer.Id);
    }

    private void ChatSubmitComplaint(int chatMenuPlayerId)
    {
        var request = Http.Manager.Instance().CreateRequest("/player/chatComplaint");
        request.Form.AddField("playerId", chatMenuPlayerId);
        Http.Manager.StartAsyncRequest(request,
            delegate
            {
                MessageBox.Show(MessageBox.Type.Info,
                    Localizer.GetText("lblChatComplaintSubmitted", chatPlayer.NickName));
            });
    }
    #endregion

    #region Blacklist
    private void OnChatBlacklist(tk2dUIItem uiItem)
    {
        contextMenu.HideContextMenu();
        MessageBox.Show(MessageBox.Type.Question,
            Localizer.GetText("lblChatBlacklistConfirmation", chatPlayer.NickName),
            answer =>
            {
                if (MessageBox.Answer.Yes == answer)
                {
                    ChatBlacklist(chatPlayer.Id);
                }
            });
    }

    private void ChatBlacklist(int chatMenuPlayerId)
    {
        var request = Http.Manager.Instance().CreateRequest("/chat/blacklist/add");
        request.Form.AddField("playerId", chatMenuPlayerId);
        Http.Manager.StartAsyncRequest(request,
            delegate
            {
                blacklistedPlayerIds.Add(chatMenuPlayerId);

                MessageBox.Show(MessageBox.Type.Info,
                    Localizer.GetText("lblChatBlacklisted", chatPlayer.NickName));
            });
    }
    #endregion

    private void OnAddToFriends(tk2dUIItem uiItem)
    {
        FriendsManager.AddToFriends(chatPlayer.Id);
        contextMenu.HideContextMenu();
    }

    private void OnRemoveFromFriends(tk2dUIItem uiItem)
    {
        FriendsManager.RemoveFromFriends(chatPlayer.Id);
        contextMenu.HideContextMenu();
    }

    private void ShowPlayerInfo(tk2dUIItem uiItem)
    {
        PlayerInfo.Instance.Show(chatPlayer.Id, chatPlayer.NickName);
        contextMenu.HideContextMenu();
    }

    private void SendChatMessage(tk2dUIItem uiItem)
    {
        ChatManager.Instance.chatController.InsertNickName(chatPlayer.NickName);
        contextMenu.HideContextMenu();
    }

    private void SetUpSpecialButtons()
    {
        // Убираем пункты меню «Пожаловаться» и «Забанить» в чате клана
        if (ChatManager.Instance.chatController.currentChatPage.room == ChatRoom.Clan)
        {
            contextMenu.HideMenuItem("ChatBan");
            contextMenu.HideMenuItem("ChatComplaint");
        }

        switch (ProfileInfo.PlayerPrivilege)
        {
            case ProfileInfo.PlayerPrivileges.Administrator:
                break;
            case ProfileInfo.PlayerPrivileges.Moderator:
                if (ProfileInfo.profileId == chatPlayer.Id)
                {
                    contextMenu.HideMenuItem("ChatBan");
                    contextMenu.HideMenuItem("ChatComplaint");
                }
                else
                {
                    contextMenu.ShowMenuItem("ChatBan");
                    if (GameData.chatIsComplaintsEnabled
                        && ProfileInfo.Level >= GameData.chatMinComplainLevel)
                        contextMenu.ShowMenuItem("ChatComplaint");
                }
                break;
            case ProfileInfo.PlayerPrivileges.None:
                if (ProfileInfo.profileId == chatPlayer.Id)
                {
                    contextMenu.HideMenuItem("ChatComplaint");
                }
                else
                {
                    if (GameData.chatIsComplaintsEnabled
                        && ProfileInfo.Level >= GameData.chatMinComplainLevel)
                        contextMenu.ShowMenuItem("ChatComplaint");
                    else
                    {
                        contextMenu.HideMenuItem("ChatComplaint");
                    }
                }
                contextMenu.HideMenuItem("ChatBan");
                break;
        }

        //TODO: Race condition с чёрным списком на сервере
        if (ProfileInfo.profileId == chatPlayer.Id || blacklistedPlayerIds.Contains(chatPlayer.Id))
            contextMenu.HideMenuItem("Blacklist");
        else
            contextMenu.ShowMenuItem("Blacklist");
    }
}
