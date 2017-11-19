using System;
using UnityEngine;

public class BonusStatsLabel : MonoBehaviour
{
    public tk2dSlicedSprite sprBackground;
    public tk2dTextMesh lblBonusStats;
    private const float bgWight = 300;
    private const float bgHeight = 150;
    private const float bgHeightRentingBox = 74;
    private float lblBonusStatusDefaultYPosition;

    public float SlicedSpriteDefaultYLength { get; private set; }

    public float ExpandMultiplier { get; private set; }

    public static float textLineInterval;

    private void Awake()
    {
        Dispatcher.Subscribe(EventId.OnLanguageChange, OnLanguageChange);
        DefineTextLineInterval();
    }

    private void OnDestroy()
    {
        Dispatcher.Unsubscribe(EventId.OnLanguageChange, OnLanguageChange);
    }

    private void OnEnabled()
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
        //   Vector2 slicedSpriteDimensions = Vector2.zero;

        if (sprBackground != null)
        {
            if (transform.name.Contains("RentingBox") || transform.name.Contains("RentedBox"))
            {
                sprBackground.dimensions = new Vector2(bgWight, bgHeightRentingBox);
            }
            else
            {
                sprBackground.dimensions = new Vector2(bgWight, bgHeight);
            }

            //slicedSpriteDimensions = sprBackground.dimensions;

            //SlicedSpriteDefaultYLength
            //    = HelpTools.Approximately(SlicedSpriteDefaultYLength, 0)
            //        ? slicedSpriteDimensions.y
            //        : SlicedSpriteDefaultYLength;
        }

        //lblBonusStatusDefaultYPosition
        //    = HelpTools.Approximately(lblBonusStatusDefaultYPosition, 0)
        //        ? lblBonusStats.transform.position.y
        //        : lblBonusStatusDefaultYPosition;

        //string status = string.Empty;

        //ExpandMultiplier = 0;

        //if (!HelpTools.Approximately(statGainer.Damage, 0))
        //{
        //    status
        //        += string.Format(
        //            "{0}{3}{1} {2}",
        //            Environment.NewLine,
        //           /* Convert.ToInt32*/(statGainer.Damage),
        //            Localizer.GetText("ForDamageGain"), (statGainer.Damage > 0 ? "+" : ""));

        //    ExpandMultiplier++;
        //}

        //if (!HelpTools.Approximately(statGainer.RocketDamage, 0))
        //{
        //    status
        //        += string.Format(
        //            "{0}{3}{1} {2}",
        //            Environment.NewLine,
        //         /*   Convert.ToInt32*/(statGainer.RocketDamage),
        //            Localizer.GetText("ForDamageGain"), (statGainer.RocketDamage > 0 ? "+" : ""));

        //    ExpandMultiplier++;
        //}

        //if (!HelpTools.Approximately(statGainer.Armor, 0))
        //{
        //    status
        //        += string.Format(
        //            "{0}{3}{1} {2}",
        //            Environment.NewLine,
        //           /* Convert.ToInt32*/(statGainer.Armor),
        //            Localizer.GetText("ForArmorGain"), (statGainer.Armor > 0 ? "+" : ""));

        //    ExpandMultiplier++;
        //}

        //if (!HelpTools.Approximately(statGainer.Speed, 0))
        //{
        //    status
        //        += string.Format(
        //            "{0}{3}{1} {2}",
        //            Environment.NewLine,
        //            /*Convert.ToInt32*/(statGainer.Speed),
        //            Localizer.GetText("ForSpeedGain"), (statGainer.Speed > 0 ? "+" : ""));

        //    ExpandMultiplier++;
        //}
        //if (!HelpTools.Approximately(statGainer.ROF, 0))
        //{
        //    status
        //        += string.Format(
        //            "{0}{3}{1} {2}",
        //            Environment.NewLine,
        //            /*Convert.ToInt32*/(statGainer.ROF),
        //            Localizer.GetText("ForROFGain"), (statGainer.ROF > 0 ? "+" : ""));

        //    ExpandMultiplier++;
        //}
        //if (!HelpTools.Approximately(statGainer.Magazine, 0))
        //{
        //    status
        //        += string.Format(
        //            "{0}{3}{1} {2}",
        //            Environment.NewLine,
        //            /*Convert.ToInt32*/(statGainer.Magazine),
        //            Localizer.GetText("ForMagazineGain"), (statGainer.Magazine > 0 ? "+" : ""));

        //    ExpandMultiplier++;
        //}
        //if (!HelpTools.Approximately(statGainer.Reload, 0))
        //{

        //    status
        //        += string.Format(
        //            "{0}{3}{1} {2}",
        //            Environment.NewLine,
        //            /*Convert.ToInt32*/(statGainer.Reload),
        //            Localizer.GetText("ForReloadGain"), (statGainer.Reload > 0 ? "+" : ""));

        //    ExpandMultiplier++;
        //}

        //= new Vector2(
        //    x: slicedSpriteDimensions.x,
        //    y: SlicedSpriteDefaultYLength + (ExpandMultiplier * textLineInterval / sprBackground.scale.y));

        //  lblBonusStats.text = status.Trim();
    }
}
