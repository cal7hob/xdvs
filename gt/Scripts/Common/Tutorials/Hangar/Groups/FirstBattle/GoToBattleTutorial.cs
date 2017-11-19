using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GoToBattleTutorial : Tutorial
{
    public override bool IsActive
    {
        get { return ProfileInfo.TutorialIndex == index && GUIPager.ActivePage == page; }
    }

    protected override void Awake()
    {
        base.Awake();
        Dispatcher.Subscribe(EventId.WentToBattle, CloseTutorial);
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
        Dispatcher.Unsubscribe(EventId.WentToBattle, CloseTutorial);
    }

    protected override void Init(EventId id = 0, EventInfo info = null)
    {
        base.Init(id, info);

        index = (int)Tutorials.goToBattle;
        page = TutorialPages.MainMenu.ToString();

        Refresh();
    }

    protected override void InstantiateTutorialParts()
    {
        var character  = MenuController.InstantiateTutorialPart(
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
            holder:                 holder,
            path:                   TutorialMessagePath,
            anchor:                 TutorialMessageAnchor,
            position:               characterSpriteDimensions + Vector2.right * MessageXOffset,
            yPos:                   characterSpriteDimensions.y + MessageYOffset,
            eulerAngles:            Vector3.zero,
            partName:               "tutorialMessage_1",
            isLocalizationNeded:    true,
            parent:                 character.transform);

        MenuController.InstantiateTutorialPart(
            holder:         holder,
            path:           "Tutorials/ArrowPointerWrapper",
            anchor:         TutorialHolder.CamAnchors.lowerLeft,
            position:       TutorialsController.MainMenuButtons.GoToBattleBtn.localPosition,
            yPos:           TutorialsController.MainMenuButtons.GoToBattleBtn.localPosition.y + ArrowYOffset,
            eulerAngles:    Vector3.zero,
            partName:       "ArrowPointerWrapper");
    }

    //protected override void CloseTutorial(EventId id = 0, EventInfo info = null)
    //{
    //    ProfileInfo.accomplishedTutorials[Tutorials.goToBattle] = true;
    //    base.CloseTutorial(id, info);
    //}

    protected override IEnumerator RefreshingRoutine()
    {
        holder.Wrapper.SetActive(IsActive);

        Dispatcher.Send(EventId.ChangeElementStateRequest, new EventInfo_U(new ChangeElementStateRequestInfo(this, new HashSet<System.Type>() { typeof(ScoresController), typeof(RightPanel) }, !IsActive)));

        yield return new WaitForEndOfFrame();

        if (IsActive)
        {
            if (!didPlayVoice)
            {
                didPlayVoice = true;
                Dispatcher.Send(EventId.VoiceRequired, new EventInfo_I((int)VoiceEventKey.GoToBattleLesson));
            }
        }
    }
}
