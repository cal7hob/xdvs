using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;
using Tanks.Models;

using SocialNetworks.Avatars;

public class Avatar : MonoBehaviour
{
    public bool loadAvatarOnEnable = false;
    [SerializeField]
    public PlayerStateVisualizer playerStateVisualizer;

    [SerializeField]
    private Player player;

    [SerializeField]
    private string avatarUrl;

    private const string PREFS_AVATAR_PREFIX = "UserPicture_";

    private bool isSubscribed;

    private IAvatarRecord avatarRec;
    private bool isDestroyed = false;

    void OnDestroy()
    {
        isDestroyed = true;
        if (isSubscribed)
        {
            Dispatcher.Unsubscribe(EventId.VipStatusUpdated, VipStatusUpdated_Handler);
            isSubscribed = false;
        }
    }

    public delegate void OnAvatarLoadedCallback(Texture2D texture);

    private bool IsSocialUser { get { return player != null && player.Social != null; } }

    private string UserpicPrefsKey
    {
        get
        {
            return IsSocialUser ? player.Social.Platform + PREFS_AVATAR_PREFIX + player.Social.Uid
                : Http.Manager.computeHash(avatarUrl);
        }
    }

    public void Init(Player player)
    {
        avatarUrl = null;

        this.player = player;

        if (playerStateVisualizer != null)
            playerStateVisualizer.SetState(GetPlayerState());
    }

    public void DownloadAvatar(float waitBeforeDownload = 0f, OnAvatarLoadedCallback callback = null)
    {
        if (!IsSocialUser && string.IsNullOrEmpty(avatarUrl))
            return;

        Download();
    }

    public void Hide ()
    {
      
    }

    public void Show ()
    {
        if (avatarRec != null)
        {
            
        }
        else
        {
            
        }
    }

    public PlayerVisualState GetPlayerState()
    {
        PlayerVisualState state = PlayerVisualState.None;

        if (player.Id == ProfileInfo.playerId)
        {
            // Subscribe for vip status change events.
            if (playerStateVisualizer != null && !isSubscribed)
            {
                Dispatcher.Subscribe(EventId.VipStatusUpdated, VipStatusUpdated_Handler);
                isSubscribed = true;
            }

            if (ProfileInfo.IsPlayerVip)
                state |= PlayerVisualState.Vip;

            // Я всегда онлайн, раз это вижу.
            state |= PlayerVisualState.Online;
        }
        else
        {
            if (player.IsVip)
                state |= PlayerVisualState.Vip;

            if (player.LastActivityTimestamp != 0)
            {
                state |= player.IsOnline ? PlayerVisualState.Online : PlayerVisualState.Offline;
            }
        }

        return state;
    }

    void OnEnable()
    {
        if (playerStateVisualizer != null)
            playerStateVisualizer.SetState(GetPlayerState());
    }

    private void VipStatusUpdated_Handler(EventId eventId, EventInfo eventInfo)
    {
        if (playerStateVisualizer != null)
            playerStateVisualizer.SetState(GetPlayerState());
    }

    private void Download()
    {
        Registry.Instance.GetAvatar(player.Social.Platform, player.Social.Uid, (bool res, IAvatarRecord rec) => {
            if (!res) {
                return;
            }

            if (isDestroyed) {
                return;
            }

            avatarRec = rec;
        });
    }

}
