using Tanks.Models;
using UnityEngine;

public class ScoresItemClan : ScoresItem, ICanHazScore
{
    [SerializeField] private tk2dTextMesh nameLabel;
    [SerializeField] private tk2dTextMesh scoreLabel;
    [SerializeField] private tk2dBaseSprite sprClanImage;
    [SerializeField] private ClanStateVisualizer clanStateVisualizer;

    [SerializeField] private Clan clan;
    [SerializeField] private int? score;

    private ScoresMenuBehaviourClan scoresMenuBehaviourClan;

    /// <summary>
    /// Returns true if this is your clan
    /// </summary>
    public override bool IsHighlightedItem
    {
        get { return ProfileInfo.Clan != null && clan.Id == ProfileInfo.Clan.Id; }
    }

    // Place находится в nameLabel, чтобы не сдвигать ник игрока при увеличении разрядности place
    // placeLabel выключен в префабе
    public override int Place
    {
        get { return place; }
        set
        {
            base.Place = value;

            UpdateNameLabel(clan.Name);
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

    protected override void Awake()
    {
        base.Awake();
        UiItem.OnClickUIItem += ClickUIItemHandler;
    }

    private void OnDestroy()
    {
        UiItem.OnClickUIItem -= ClickUIItemHandler;
    }

    public ScoresItem Init(Clan clan, int place, ScoresMenuBehaviourClan scoresMenuBehaviourClan)
    {
        this.clan = clan;
        this.place = place;

        if (clanStateVisualizer != null)
            clanStateVisualizer.SetState(GetClanState());

        Place = place;
        Score = clan.Score;
        UpdateNameLabel(clan.Name);

        sprClanImage.SetSprite(clan.Image);

        this.scoresMenuBehaviourClan = scoresMenuBehaviourClan;

        return this;
    }

    public void UpdateData(Clan clan)
    {
        if (clanStateVisualizer != null)
            clanStateVisualizer.SetState(GetClanState());

        sprClanImage.SetSprite(clan.Image);
    }

    public override void UpdateNameLabel(string name)
    {
        base.UpdateNameLabel(name);

        nameLabel.text = colorNamePlace.To2DToolKitColorFormatString()
            + MiscTools.GetCultureSpecificFormatOfNumber(place)
            + ". " + colorName.To2DToolKitColorFormatString() + name;
    }

    public ClanVisualState GetClanState()
    {
        var state = ClanVisualState.None;

        if (IsHighlightedItem)
        {
            state = ClanVisualState.Self;
        }

        return state;
    }

    private void SetScore(int? score)
    {
        if (score.HasValue)
            scoreLabel.text = MiscTools.GetCultureSpecificFormatOfNumber(score.Value);
    }

    private void ClickUIItemHandler(tk2dUIItem clickedUiItem)
    {
        scoresMenuBehaviourClan.ShowContextMenu(clickedUiItem, layout, clan);
    }
}
