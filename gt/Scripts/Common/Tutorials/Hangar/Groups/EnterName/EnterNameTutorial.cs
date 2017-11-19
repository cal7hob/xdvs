using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnterNameTutorial : Tutorial
{
    public override bool IsActive
    {
        get
        {
            return ProfileInfo.TutorialIndex == index &&
                   !ProfileInfo.nickRejected &&
                   !ProfileInfo.nickEntered &&
                   GUIPager.ActivePage == page;
        }
    }

    protected override void Awake()
    {
        base.Awake();
        Dispatcher.Subscribe(EventId.NickNameManuallyChanged, CloseTutorial);
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
        Dispatcher.Unsubscribe(EventId.NickNameManuallyChanged, CloseTutorial);
    }

    protected override void Init(EventId id = 0, EventInfo info = null)
    {
        base.Init(id, info);

        index = (int)Tutorials.enterName;
        page = TutorialPages.EnterName.ToString();

        Refresh();

        if (ProfileInfo.nickEntered || ProfileInfo.nickRejected)
            CloseTutorial();
    }

    protected override void InstantiateTutorialParts()
    {
        var character = MenuController.InstantiateTutorialPart(
                holder:         holder,
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

        MenuController.InstantiateTutorialPart(
            holder:              holder,
            path:                TutorialMessagePath,
            anchor:              TutorialMessageAnchor,
            position:            characterSpriteDimensions + Vector2.right * MessageXOffset,
            yPos:                characterSpriteDimensions.y + MessageYOffset,
            eulerAngles:         Vector3.zero,
            partName:            "tutorialMessage_0",
            isLocalizationNeded: true,
            parent:              character.transform);
    }

    protected override IEnumerator RefreshingRoutine()
    {
        holder.Wrapper.SetActive(IsActive);

        Dispatcher.Send(EventId.ChangeElementStateRequest, new EventInfo_U(new ChangeElementStateRequestInfo(this, typeof(ScoresController), !IsActive)));

        yield return new WaitForEndOfFrame();

        if (IsActive && !didPlayVoice)
        {
            didPlayVoice = true;
            Dispatcher.Send(EventId.VoiceRequired, new EventInfo_I((int)VoiceEventKey.EnterNameLesson));
        }
    }
}
