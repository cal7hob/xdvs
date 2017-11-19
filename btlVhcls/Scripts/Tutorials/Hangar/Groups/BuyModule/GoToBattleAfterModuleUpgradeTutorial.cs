using System.Collections;
using UnityEngine;

public class GoToBattleAfterModuleUpgradeTutorial : GoToModuleShopTutorial
{
    public new static GoToBattleAfterModuleUpgradeTutorial Instance { get; private set; }    

    protected override bool DidThisBefore
    {
        get { return !BuyModuleUpgradeTutorial.Instance.JustReceivedModule && IsActive; }
    }

    public override bool IsActive
    {
        get { return ProfileInfo.RecentlyFinishedTutorialIndex == index && GUIPager.ActivePageName == page && HasUpgradedModule; }
    }

    protected override void Awake()
    {
        Instance = this;

        base.Awake();

        Dispatcher.Subscribe(EventId.ModuleBought, Refresh);
        Dispatcher.Subscribe(EventId.ModuleReceived, Refresh);
    }

    protected override void OnDestroy()
    {
        Instance = null;

        base.OnDestroy();

        Dispatcher.Unsubscribe(EventId.ModuleBought, Refresh);
        Dispatcher.Unsubscribe(EventId.ModuleReceived, Refresh);
    }

    protected override void Init(EventId id = 0, EventInfo info = null)
    {
        base.Init(id, info);

        index = (int)Tutorials.vehicleUpgrade;
        page = TutorialPages.MainMenu.ToString();
    }

    protected override void InstantiateTutorialParts()
    {
        InstantiateTutorialPart(
            prefab:         TutorPrefab.ArrowPointer,
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
}
