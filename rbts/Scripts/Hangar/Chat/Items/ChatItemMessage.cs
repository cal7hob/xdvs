using System;
using Tanks.Models;
using UnityEngine;

public class ChatItemMessage : ChatItem
{
	[Serializable]
    private class Decoration
    {
        // В Iron Tanks у ownMessage такой же, как у lblTankName
        public Color NickColor = new Color32(255, 212, 163, 255);
        public Color MessageColor = new Color32(255, 212, 163, 255);
    }

    [SerializeField] private Decoration ownMessage;
    [SerializeField] private Decoration moderatorMessage;
    [SerializeField] private ActivatedUpDownButton objectsActivatedForMe;
    [SerializeField] private ActivatedUpDownButton objectsActivatedForOther;
    [SerializeField] private ActivatedUpDownButton objectsActivatedForModerator;
    [SerializeField] private tk2dBaseSprite levelFrame;
    [SerializeField] private tk2dBaseSprite avatarFrame;

    [SerializeField] private tk2dUIItem senderUiItem;

#if UNITY_EDITOR
#pragma warning disable 0414
    [SerializeField]
    private Message message;
#pragma warning restore 0414
#endif

    [SerializeField] private tk2dTextMesh messageLabel;

    protected override void Awake()
    {
        base.Awake();
        senderUiItem.OnClickUIItem += OnSenderClickUIItemHandler;
    }

    private void OnDestroy()
    {
        senderUiItem.OnClickUIItem -= OnSenderClickUIItemHandler;
    }

    private bool IsPlayer(int playerID)
    {
        return playerID == ProfileInfo.profileId;
    }

    private bool IsPlayerMentioned(string content)
    {
        return content.Contains(ProfileInfo.PlayerName);
    }

    public void Init(Message message)
    {
        base.Init(message.Sender);
#if UNITY_EDITOR
        #pragma warning disable 0414
        this.message = message;
        #pragma warning restore 0414

        gameObject.name += ": "
            + (message.Content.Length >= 20 ? message.Content.Substring(0, 20) : message.Content);
#endif

        var isPlayer = IsPlayer(message.Sender.Id);
        var isPlayerMentioned = IsPlayerMentioned(message.Content);

        if (!string.IsNullOrEmpty(message.Sender.NickName))
        {
            if (message.IsModerator)
                nameLabel.color = moderatorMessage.NickColor;
            else if (isPlayer || isPlayerMentioned)
                nameLabel.color = ownMessage.NickColor;
        }

        if (!string.IsNullOrEmpty(message.Content))
        {
            messageLabel.text = message.Content;

            if (isPlayer || isPlayerMentioned)
                messageLabel.color = ownMessage.MessageColor;
        }

        if (GameData.IsGame(Game.SpaceJet | Game.BattleOfWarplanes))
        {
            if(isPlayer)
                levelFrame.color = avatarFrame.color = ownMessage.NickColor;
            else if(message.IsModerator)
                levelFrame.color = avatarFrame.color = moderatorMessage.NickColor;
            else
                levelFrame.color = avatarFrame.color = nameLabel.color; 
        }

        //Выключаем объекты, затем включим нужные
        if (objectsActivatedForMe)
            objectsActivatedForMe.Activated = false;
        if (objectsActivatedForOther)
            objectsActivatedForOther.Activated = false;
        if (objectsActivatedForModerator)
            objectsActivatedForModerator.Activated = false;

        if (isPlayer)
        {
            if(objectsActivatedForMe)
                objectsActivatedForMe.Activated = true;
        }
        else if (message.IsModerator)
        {
            if(objectsActivatedForModerator)
                objectsActivatedForModerator.Activated = true;
        }
        else
        {
            if(objectsActivatedForOther)
                objectsActivatedForOther.Activated = true;
        }
    }

    private void OnSenderClickUIItemHandler(tk2dUIItem clickedUiItem)
    {
        ChatMenuBehaviour.Instance.ShowContextMenu(clickedUiItem, layout, player);
    }
}