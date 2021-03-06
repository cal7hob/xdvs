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

    public override bool IsActive
    {
        get
        {
            return RecentlyFinishedTutorialIndex == index &&
                   GUIPager.ActivePage == page &&
                   HasOtherVehicles;
        }
    }

    protected override void Awake()
    {
        Instance = this;

        base.Awake();

        Dispatcher.Subscribe(EventId.VehicleBought, Refresh);
        Dispatcher.Subscribe(EventId.AfterHangarInit, Refresh);
        Dispatcher.Subscribe(EventId.VehicleBought, OnVehicleBought);
    }

    protected override void OnDestroy()
    {
        Instance = null;

        base.OnDestroy();

        Dispatcher.Unsubscribe(EventId.VehicleBought, Refresh);
        Dispatcher.Unsubscribe(EventId.AfterHangarInit, Refresh);
        Dispatcher.Unsubscribe(EventId.VehicleBought, OnVehicleBought);
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
        MenuController.InstantiateTutorialPart(
            holder:         holder,
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
        Dispatcher.Send(EventId.ChangeElementStateRequest, new EventInfo_U(new ChangeElementStateRequestInfo(this, typeof(RightPanel), !IsActive)));
        yield return new WaitForEndOfFrame();
    }

    private void OnVehicleBought(EventId id, EventInfo info)
    {
        JustBoughtVehicle = true;
    }
}
