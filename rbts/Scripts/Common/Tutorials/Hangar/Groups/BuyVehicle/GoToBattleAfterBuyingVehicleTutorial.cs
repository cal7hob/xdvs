using System.Collections;
using UnityEngine;

public class GoToBattleAfterBuyingVehicleTutorial : GoToVehicleShopTutorial {

    public new static GoToBattleAfterBuyingVehicleTutorial Instance
    {
        get; private set;
    }

    public bool JustBoughtVehicle
    {
        get; private set;
    }

    protected override bool DidThisBefore
    {
        get
        {
            return !Instance.JustBoughtVehicle && IsActive;
        }
    }

    public override bool IsActive
    {
        get
        {
            return ProfileInfo.RecentlyFinishedTutorialIndex == index &&
                   GUIPager.ActivePage == page &&
                   HasOtherVehicles;
        }
    }

    protected override void Awake()
    {
        Instance = this;

        base.Awake();

        Messenger.Subscribe(EventId.WentToBattle, CloseTutorial); 
        Messenger.Subscribe(EventId.VehicleBought, Refresh);
        Messenger.Subscribe(EventId.AfterHangarInit, Refresh);
        Messenger.Subscribe(EventId.VehicleBought, OnVehicleBought);
    }

    protected override void OnDestroy()
    {
        Instance = null;

        base.OnDestroy();

        Messenger.Unsubscribe(EventId.WentToBattle, CloseTutorial);
        Messenger.Unsubscribe(EventId.VehicleBought, Refresh);
        Messenger.Unsubscribe(EventId.AfterHangarInit, Refresh);
        Messenger.Unsubscribe(EventId.VehicleBought, OnVehicleBought);
    }

    protected override void Init(EventId id = 0, EventInfo info = null)
    {
        if (isInitialized)
            return;

        index = (int)Tutorials.buyVehicle;
        page = TutorialPages.MainMenu.ToString();

        base.Init(id, info);

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

    private void OnVehicleBought(EventId id, EventInfo info)
    {
        JustBoughtVehicle = true;
    }
}
