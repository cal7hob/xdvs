using System.Collections;
using System.Linq;
using UnityEngine;
using System.Collections.Generic;

public class GoToPatternShopTutorial : Tutorial
{
    public override bool IsActive
    {
        get
        {
            return ProfileInfo.TutorialIndex == index &&
                   GUIPager.ActivePage == page &&
                   !HasCamouflage &&
                   HasEnoughMoneyToBuyCamouflage &&
                   (GoToBattleAfterModuleUpgradeTutorial.Instance == null || !BuyModuleUpgradeTutorial.Instance.JustReceivedModule);
        }
    }

    protected static bool HasCamouflage
    {
        get
        {
            if (ProfileInfo.currentVehicle == VehicleOffersController.FreeVehicleIds[GameData.ClearGameFlags(GameData.CurrentGame)])
                return ProfileInfo.vehicleUpgrades[ProfileInfo.currentVehicle].OwnedCamouflages.Count > 1;

            foreach (var vehicleUpgrade in ProfileInfo.vehicleUpgrades)
                if (vehicleUpgrade.Value.OwnedCamouflages.Count > 0)
                    return true;

            return false;
        }
    }

    protected static bool CanBuySelectedCamouflage
    {
        get; set;
    }

    protected static bool HasEnoughMoneyToBuyCamouflage
    {
        get
        {
            var cheapestPriceGold = int.MaxValue;
            var cheapestPriceSilver = int.MaxValue;
            var cheapestCamoForGold = 0;
            var cheapestCamoForSilver = 0;

            foreach (var camo in PatternPool.Instance.Items.Where(c => !c.isVip))
            {
                if (camo.Price.value < cheapestPriceGold && camo.Price.currency == ProfileInfo.PriceCurrency.Gold)
                {
                    cheapestPriceGold = camo.Price.value;
                    cheapestCamoForGold = camo.id;
                }

                if (camo.Price.value < cheapestPriceSilver && camo.Price.currency == ProfileInfo.PriceCurrency.Silver)
                {
                    cheapestPriceSilver = camo.Price.value;
                    cheapestCamoForSilver = camo.id;
                }
            }

            if (ProfileInfo.Silver >= cheapestPriceSilver)
            {
                NeededPatternCellId = cheapestCamoForSilver;
                return true;
            }

            if (ProfileInfo.Gold >= cheapestPriceGold)
            {
                NeededPatternCellId = cheapestCamoForGold;
                return true;
            }

            return false;
        }
    }

    protected static int NeededPatternCellId
    {
        get; private set;
    }

    protected override void Init(EventId id = 0, EventInfo info = null)
    {
        if (isInitialized)
            return;

        base.Init(id, info);

        index = (int)Tutorials.buyCamouflage;
        page = TutorialPages.MainMenu.ToString();

        Refresh();
    }

    protected override void InstantiateTutorialParts()
    {
        var character
            = InstantiateTutorialPart(
                path:           "Tutorials/sprCharacterFromRes",
                anchor:         CharacterAnchor,
                position:       TutorialsController.MainMenuButtons.VehicleShopBtn.localPosition + Vector3.right * CharacterXOffset,
                yPos:           CharacterYOffset,
                eulerAngles:    Vector3.zero,
                partName:       "Character");

        var characterSprFromRes = character.GetComponent<SpriteFromRes>();

        var characterSpriteDimensions
            = characterSprFromRes
                ? ((tk2dSlicedSprite)characterSprFromRes.Sprite).dimensions
                : character.GetComponent<tk2dSlicedSprite>().dimensions;

        InstantiateTutorialPart(
            path:           "Tutorials/ArrowPointerWrapper",
            anchor:         TutorialHolder.CamAnchors.lowerLeft,
            position:       TutorialsController.MainMenuButtons.PatternShopBtn.localPosition,
            yPos:           TutorialsController.MainMenuButtons.PatternShopBtn.localPosition.y + ArrowYOffset,
            eulerAngles:    Vector3.zero,
            partName:       "ArrowPointerWrapper");

        InstantiateTutorialPart(
            path:                   TutorialMessagePath,
            anchor:                 TutorialMessageAnchor,
            position:               new Vector3(characterSpriteDimensions.x / 2 + MessageXOffset, characterSpriteDimensions.y, 0),
            yPos:                   characterSpriteDimensions.y + MessageYOffset,
            eulerAngles:            Vector3.zero,
            partName:               "tutorialMessage_3",
            isLocalizationNeded:    true,
            parent:                 character.transform);
    }

    protected override IEnumerator RefreshingRoutine()
    {
        holder.Wrapper.SetActive(IsActive);

        Messenger.Send(EventId.ChangeElementStateRequest, new EventInfo_U(new ChangeElementStateRequestInfo(this, typeof(ScoresController), !IsActive)));

        yield return new WaitForEndOfFrame();
        
        if (IsActive && !didPlayVoice)
        {
            didPlayVoice = true;
            Messenger.Send(EventId.VoiceRequired, new EventInfo_I((int)VoiceEventKey.BuyCamouflageLesson));
        }
    }
}