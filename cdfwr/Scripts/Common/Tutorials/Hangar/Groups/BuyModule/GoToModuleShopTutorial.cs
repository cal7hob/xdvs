using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GoToModuleShopTutorial : Tutorial
{
    public override bool IsActive
    {
        get
        {
            //return ProfileInfo.TutorialIndex == index && !HasUpgradedModule && GUIPager.ActivePage == page;
            return !HasUpgradedModule && GUIPager.ActivePage == page;
        }
    }

    protected static bool HasUpgradedModule
    {
        get
        {
            foreach (var vehicleUpgrade in ProfileInfo.vehicleUpgrades)
            {
                foreach (var moduleLevel in vehicleUpgrade.Value.ModuleLevels)
                {
                    if (moduleLevel.Value > 0)
                    {
                        return true;
                    }
                }
            }
            return false;
        }
    }

    protected override void Awake()
    {
        base.Awake();

        Dispatcher.Subscribe(EventId.ModuleBought, Refresh);
        Dispatcher.Subscribe(EventId.ModuleReceived, Refresh);
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();

        Dispatcher.Unsubscribe(EventId.ModuleBought, Refresh);
        Dispatcher.Unsubscribe(EventId.ModuleReceived, Refresh);
    }

    protected override void Init(EventId id = 0, EventInfo info = null)
    {
        base.Init(id, info);

        index = (int)Tutorials.vehicleUpgrade;
        page = TutorialPages.MainMenu.ToString();

        Refresh();
    }

    protected override void InstantiateTutorialParts()
    {
        var character = MenuController.InstantiateTutorialPart(
                holder: holder,
                path: "Tutorials/sprCharacterFromRes",
                anchor: CharacterAnchor,
                position: TutorialsController.MainMenuButtons.VehicleShopBtn.localPosition + Vector3.right * CharacterXOffset,
                yPos: CharacterYOffset,
                eulerAngles: Vector3.zero,
                partName: "Character");

        var characterSprFromRes = character.GetComponentInChildren<tk2dSlicedSprite>();

        var characterSpritePosition
            = characterSprFromRes.transform.position;

        MenuController.InstantiateTutorialPart(
            holder: holder,
            path: "Tutorials/ArrowPointerWrapper",
            anchor: TutorialHolder.CamAnchors.lowerLeft,
            position: TutorialsController.MainMenuButtons.ModuleShopBtn.localPosition,
            yPos: TutorialsController.MainMenuButtons.ModuleShopBtn.localPosition.y + ArrowYOffset,
            eulerAngles: Vector3.zero,
            partName: "ArrowPointerWrapper");

        MenuController.InstantiateTutorialPart(
            holder: holder,
            path: TutorialMessagePath,
            anchor: TutorialMessageAnchor,
            position: characterSpritePosition + Vector3.right * MessageXOffset,
            yPos: characterSpritePosition.y + MessageYOffset,
            eulerAngles: Vector3.zero,
            partName: "tutorialMessage_2",
            isLocalizationNeded: true,
            parent: character.transform);
    }

    protected override IEnumerator RefreshingRoutine()
    {
        holder.Wrapper.SetActive(IsActive);

        Dispatcher.Send(EventId.ChangeElementStateRequest, new EventInfo_U(new ChangeElementStateRequestInfo(this, typeof(ScoresController), !IsActive)));

        yield return new WaitForEndOfFrame();

        if (IsActive && !didPlayVoice)
        {
            didPlayVoice = true;
            // Dispatcher.Send(EventId.VoiceRequired, new EventInfo_I((int)VoiceEventKey.VehicleUpgradeLesson));
        }
    }
}
