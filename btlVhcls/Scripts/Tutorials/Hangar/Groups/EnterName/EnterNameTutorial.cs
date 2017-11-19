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
                   GUIPager.ActivePageName == page;
        }
    }

    protected override float MessageYOffset
    {
        get
        {
            if (GameData.IsGame(Game.IronTanks))
            {
                switch (ScreenExtensions.Aspect)
                {
                    case ScreenExtensions.AspectRatio.SixteenToNine:
                    case ScreenExtensions.AspectRatio.SixteenToTen:
                        return -50.0f;

                    case ScreenExtensions.AspectRatio.FourToThree:
                        return 100.0f;
                }
            }

            if (GameData.IsGame(Game.FutureTanks))
            {
                switch (ScreenExtensions.Aspect)
                {
                    case ScreenExtensions.AspectRatio.SixteenToNine:
                        return -109.0f;

                    case ScreenExtensions.AspectRatio.SixteenToTen:
                        return -64.0f;

                    case ScreenExtensions.AspectRatio.FourToThree:
                        return 66.0f;
                }
            }

            if (GameData.IsGame(Game.ToonWars))
            {
                switch (ScreenExtensions.Aspect)
                {
                    case ScreenExtensions.AspectRatio.SixteenToNine:
                        return 75.0f;

                    case ScreenExtensions.AspectRatio.SixteenToTen:
                        return 100.0f;

                    case ScreenExtensions.AspectRatio.FourToThree:
                        return 230.0f;
                }
            }

            if (GameData.IsGame(Game.SpaceJet))
            {
                switch (ScreenExtensions.Aspect)
                {
                    case ScreenExtensions.AspectRatio.SixteenToNine:
                        return -109.0f;

                    case ScreenExtensions.AspectRatio.SixteenToTen:
                        return -64.0f;

                    case ScreenExtensions.AspectRatio.FourToThree:
                        return 66.0f;
                }
            }

            if (GameData.IsGame(Game.BattleOfWarplanes | Game.WingsOfWar))
            {
                switch (ScreenExtensions.Aspect)
                {
                    case ScreenExtensions.AspectRatio.SixteenToNine:
                        return 80.0f;

                    case ScreenExtensions.AspectRatio.SixteenToTen:
                        return 120.0f;

                    case ScreenExtensions.AspectRatio.FourToThree:
                        return 250.0f;
                }
            }

            return base.MessageYOffset;
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
        var character
            = InstantiateTutorialPart(
                prefab:         TutorPrefab.CharacterFromRes,
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
            prefab:              TutorPrefab.TutorialMessage,
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

        while (!VoiceDispatcher.IsInitialized) {
            yield return null;
        }

        if (IsActive && !didPlayVoice)
        {
            didPlayVoice = true;
            Dispatcher.Send(EventId.VoiceRequired, new EventInfo_I((int)VoiceEventKey.EnterNameLesson));
        }
    }
}
