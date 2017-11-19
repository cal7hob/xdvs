using System;
using UnityEngine;

public class BonusStatsLabel : MonoBehaviour
{
    public tk2dSlicedSprite sprBackground;
    public tk2dTextMesh lblBonusStats;

    private float lblBonusStatusDefaultYPosition;

    public float SlicedSpriteDefaultYLength { get; private set; }

    public float ExpandMultiplier { get; private set; }

    public static float textLineInterval;

    private void Awake()
    {
        Messenger.Subscribe(EventId.OnLanguageChange, OnLanguageChange);
        DefineTextLineInterval();
    }

    private void OnDestroy()
    {
        Messenger.Unsubscribe(EventId.OnLanguageChange, OnLanguageChange);
    }

    private void OnEnable()
    {
        DefineTextLineInterval();
    }

    private void OnLanguageChange(EventId evId, EventInfo ev)
    {
        DefineTextLineInterval();
    }

    private void DefineTextLineInterval()
    {
        textLineInterval = lblBonusStats.GetEstimatedMeshBoundsForString("Йy").size.y + 5;
    }

    public void Show(IStatGainer statGainer)
    {
        Vector2 slicedSpriteDimensions = Vector2.zero;

        if (sprBackground != null)
        {
            slicedSpriteDimensions = sprBackground.dimensions;

            SlicedSpriteDefaultYLength
                = HelpTools.Approximately(SlicedSpriteDefaultYLength, 0)
                    ? slicedSpriteDimensions.y
                    : SlicedSpriteDefaultYLength;
        }

        lblBonusStatusDefaultYPosition
            = HelpTools.Approximately(lblBonusStatusDefaultYPosition, 0)
                ? lblBonusStats.transform.position.y
                : lblBonusStatusDefaultYPosition;

        string status = string.Empty;

        ExpandMultiplier = 0;

        if (statGainer.Damage > 0)
        {
            status
                += string.Format(
                    "{0}{1}% {2}",
                    Environment.NewLine,
                    Convert.ToInt32(statGainer.Damage * 100).ToString("+0;-#"),
                    Localizer.GetText("ForDamageGain"));

            ExpandMultiplier++;
        }

        //if (statGainer.RocketDamage > 0)
        //{
        //    status
        //        += string.Format(
        //            "{0}{1}% {2}",
        //            Environment.NewLine,
        //            Convert.ToInt32(statGainer.RocketDamage * 100).ToString("+0;-#"),
        //            Localizer.GetText("ForDamageGain"));

        //    ExpandMultiplier++;
        //}

        if (statGainer.Armor > 0)
        {
            status
                += string.Format(
                    "{0}{1}% {2}",
                    Environment.NewLine,
                    Convert.ToInt32(statGainer.Armor * 100).ToString("+0;-#"),
                    Localizer.GetText("ForArmorGain"));

            ExpandMultiplier++;
        }

        if (statGainer.Speed > 0)
        {
            status
                += string.Format(
                    "{0}{1}% {2}",
                    Environment.NewLine,
                    Convert.ToInt32(statGainer.Speed * 100).ToString("+0;-#"),
                    Localizer.GetText("ForSpeedGain"));

            ExpandMultiplier++;
        }

        if (statGainer.RoF > 0)
        {
            status
                += string.Format(
                    "{0}{1}% {2}",
                    Environment.NewLine,
                    Convert.ToInt32(statGainer.RoF * 100).ToString("+0;-#"),
                    Localizer.GetText("ForRoFGain"));

            ExpandMultiplier++;
        }

        if (sprBackground != null)
            sprBackground.dimensions
                = new Vector2(
                    x: slicedSpriteDimensions.x,
                    y: SlicedSpriteDefaultYLength + (ExpandMultiplier * textLineInterval / sprBackground.scale.y));

        lblBonusStats.text = status.Trim();
    }
}
