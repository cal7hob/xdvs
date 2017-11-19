using UnityEngine;
using Tanks.Models;

public class ScoresItemPlayer : ScoresItem, ICanHazScore
{
    [SerializeField] private tk2dTextMesh nameLabel;
    [SerializeField] private tk2dBaseSprite sprFlag;
    [SerializeField] private tk2dTextMesh scoreLabel;
    [SerializeField] private Avatar avatar;
    [SerializeField] private PlayerStateVisualizer playerStateVisualizer;

    [SerializeField] private Player player;
    [SerializeField] private int? score;
    [SerializeField] private string countryCode;

    private bool isSubscribed;

    private ScoresMenuBehaviourPlayer scoresMenuBehaviourPlayer;

    /// <summary>
    /// Returns true if this is you
    /// </summary>
    public override bool IsHighlightedItem
    {
        get { return player.Id == ProfileInfo.profileId; }
    }

    // Place находится в nameLabel, чтобы не сдвигать ник игрока при увеличении разрядности place
    // placeLabel выключен в префабе
    public override int Place
    {
        get { return place; }
        set
        {
            base.Place = value;

            UpdateNameLabel(player.NickName);
        }
    }

    public int? Score
    {
        get { return score; }
        set
        {
            score = value;

            SetScore(value);
        }
    }

    public string CountryCode
    {
        get { return countryCode; }
        set
        {
            countryCode = value;
            //DT.LogError("player {0} has countryCode <{1}>", player.NickName, player.CountryCode);

            var flagSpriteName =
               string.IsNullOrEmpty(countryCode) || countryCode.ToLowerInvariant() == "unknown"
               ? null
               : countryCode.ToLowerInvariant();

            if (flagSpriteName != null)
                sprFlag.SafeSetSprite(flagSpriteName, GameData.UNKNOWN_FLAG_NAME);
            else
                sprFlag.gameObject.SetActive(false);
        }
    }

    protected override void Awake()
    {
        base.Awake();
        UiItem.OnClickUIItem += ClickUIItemHandler;
    }

    private void OnDestroy()
    {
        UiItem.OnClickUIItem -= ClickUIItemHandler;

        if (!isSubscribed)
            return;

        Messenger.Unsubscribe(EventId.VipStatusUpdated, VipStatusUpdated_Handler);
        isSubscribed = false;
    }

    public ScoresItemPlayer Init(Player player, int place, string countryCode,
        ScoresMenuBehaviourPlayer scoresMenuBehaviourPlayer)
    {
        this.player = player;
        if (deltaParent)
            deltaParent.SetActive(false);

        if (playerStateVisualizer != null)
            playerStateVisualizer.SetState(GetPlayerState());

        Place = place;
        Score = player.Score;
        CountryCode = countryCode;

        UpdateNameLabel(player.NickName);

        if (GameData.IsGame(Game.SpaceJet | Game.BattleOfWarplanes | Game.Armada | Game.WWR) && IsHighlightedItem)
            nameLabel.color = scoreLabel.color = Color.white;

        avatar.Init(player);

        if (!IsHighlightedItem || SocialSettings.IsLoggedIn)
            avatar.DownloadAvatar();

        this.scoresMenuBehaviourPlayer = scoresMenuBehaviourPlayer;

        return this;
    }

    // Обновляем только lastActiveTime, social, isVip
    public void UpdateData(Player player)
    {
        player.LastActivityTimestamp = player.LastActivityTimestamp;
        player.Social = player.Social;
        player.IsVip = player.IsVip;

        if (playerStateVisualizer != null)
            playerStateVisualizer.SetState(GetPlayerState());

        avatar.Init(player);

        if (IsHighlightedItem && !SocialSettings.IsLoggedIn)
            return;

        avatar.DownloadAvatar();
    }

    public override void UpdateNameLabel(string nickName)
    {
        base.UpdateNameLabel(nickName);

        player.NickName = nickName;

        nameLabel.text = string.Format("{0}{1}{2}. {3}",
                colorNamePlace.To2DToolKitColorFormatString(),
                place.ToString("N0", GameData.instance.cultureInfo.NumberFormat),
                colorName.To2DToolKitColorFormatString(),
                nickName);
    }

    public PlayerVisualState GetPlayerState()
    {
        var state = PlayerVisualState.None;

        if (IsHighlightedItem)
        {
            // subscribe for vip status change events
            if (playerStateVisualizer != null && !isSubscribed)
            {
                Messenger.Subscribe(EventId.VipStatusUpdated, VipStatusUpdated_Handler);
                isSubscribed = true;
            }

            state = PlayerVisualState.Self;

            if (ProfileInfo.IsPlayerVip)
                state |= PlayerVisualState.Vip;
        }
        else
        {
            if (player.IsVip)
                state = PlayerVisualState.Vip;
        }

        return state;
    }

    public void VipStatusUpdated_Handler(EventId eventId, EventInfo eventInfo)
    {
        if (playerStateVisualizer != null)
            playerStateVisualizer.SetState(GetPlayerState());
    }

    private void SetScore(int? score)
    {
        if (score.HasValue)
            scoreLabel.text = score.Value.ToString("N0", GameData.instance.cultureInfo.NumberFormat);
    }

    private void ClickUIItemHandler(tk2dUIItem clickedUiItem)
    {
        scoresMenuBehaviourPlayer.ShowContextMenu(clickedUiItem, layout, player);
    }
}
