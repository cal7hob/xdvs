using System.Collections;
using UnityEngine;

class GoToBattleAfterBuyingCamoTutorial : GoToPatternShopTutorial
{
    public new static GoToBattleAfterBuyingCamoTutorial Instance
    {
        get; private set;
    }

    public bool JustBoughtCamouflage
    {
        get; private set;
    }

    public override bool IsActive
    {
        get
        {
            return ProfileInfo.RecentlyFinishedTutorialIndex == index &&
                   GUIPager.ActivePage == page &&
                   HasCamouflage;
        }
    }

    protected override bool DidThisBefore
    {
        get
        {
            return !Instance.JustBoughtCamouflage && IsActive;
        }
    }

    protected override void Awake()
    {
        Instance = this;

        base.Awake();

        Messenger.Subscribe(EventId.CamouflageBought, Refresh);
        Messenger.Subscribe(EventId.AfterHangarInit, Refresh);
        Messenger.Subscribe(EventId.CamouflageBought, OnCamouflageBought);
    }

    protected override void OnDestroy()
    {
        Instance = null;

        base.OnDestroy();

        Messenger.Unsubscribe(EventId.CamouflageBought, Refresh);
        Messenger.Unsubscribe(EventId.AfterHangarInit, Refresh);
        Messenger.Unsubscribe(EventId.CamouflageBought, OnCamouflageBought);
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
        InstantiateTutorialPart(
            path:           "Tutorials/ArrowPointerWrapper",
            anchor:         TutorialHolder.CamAnchors.lowerLeft,
            position:       TutorialsController.MainMenuButtons.GoToBattleBtn.localPosition,
            yPos:           TutorialsController.MainMenuButtons.GoToBattleBtn.localPosition.y + ArrowYOffset,
            eulerAngles:    Vector3.zero,
            partName:       "ArrowPointerWrapper");
    }

    protected override IEnumerator RefreshingRoutine()
    {
        holder.Wrapper.SetActive(IsActive);

        Messenger.Send(EventId.ChangeElementStateRequest, new EventInfo_U(new ChangeElementStateRequestInfo(this, typeof(RightPanel), !IsActive)));

        yield return new WaitForEndOfFrame();   
    }

    private void OnCamouflageBought(EventId id, EventInfo info)
    {
        JustBoughtCamouflage = true;
    }
}
