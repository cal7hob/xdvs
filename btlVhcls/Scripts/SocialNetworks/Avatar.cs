using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;
using Tanks.Models;

using SocialNetworks.Avatars;

public class Avatar : MonoBehaviour
{
    public bool loadAvatarOnEnable = false;
    public tk2dBaseSprite unknownAvatarSprite;
    [SerializeField]
    private tk2dBaseSprite avatarTexture;
    public PlayerStateVisualizer playerStateVisualizer;

    [SerializeField]
    private Player player;

    [SerializeField]
    private string avatarUrl;

    private const string PREFS_AVATAR_PREFIX = "UserPicture_";

    private bool isSubscribed;

    private IAvatarRecord avatarRec;
    private bool isDestroyed = false;

    void Awake() { avatarTexture.gameObject.SetActive(false); }

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

        avatarTexture.gameObject.SetActive(false);
        if (unknownAvatarSprite != null)
            unknownAvatarSprite.gameObject.SetActive(true);

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
        avatarTexture.gameObject.SetActive(false);
        if (unknownAvatarSprite != null)
            unknownAvatarSprite.gameObject.SetActive(false);
    }

    public void Show ()
    {
        if (avatarRec != null)
        {
            avatarTexture.gameObject.SetActive(true);
        }
        else
        {
            if (unknownAvatarSprite != null)
                unknownAvatarSprite.gameObject.SetActive(true);
        }
    }

    public PlayerVisualState GetPlayerState()
    {
        PlayerVisualState state = PlayerVisualState.None;

        if (player.Id == ProfileInfo.profileId)
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

            if (unknownAvatarSprite != null)
                unknownAvatarSprite.gameObject.SetActive(false);

            avatarTexture.gameObject.SetActive(true);

            avatarTexture.Collection = rec.Collection;
            avatarTexture.SetSprite(rec.Index);
        });
    }

}
