using UnityEngine;

public class GoBackTutorial : Tutorial
{
    public override bool IsActive
    {
        get
        {
            return RecentlyFinishedTutorialIndex == index && 
                    GUIPager.ActivePage == page && 
                    (BuyModuleUpgradeTutorial.Instance != null || 
                    BuyPatternTutorial.Instance != null ||
                    BuyVehicleTutorial.Instance != null);
        }
    }

    protected override void Awake()
    {
        holder = GetComponent<TutorialHolder>();

        Dispatcher.Subscribe(EventId.ModuleReceived, Refresh, 4);
        Dispatcher.Subscribe(EventId.CamouflageBought, Refresh, 4);
        Dispatcher.Subscribe(EventId.VehicleInstalled, Refresh, 4);
        Dispatcher.Subscribe(EventId.PageChanged, Refresh);
    }

    protected override void OnDestroy()
    {
        Dispatcher.Unsubscribe(EventId.ModuleReceived, Refresh);
        Dispatcher.Unsubscribe(EventId.CamouflageBought, Refresh);
        Dispatcher.Unsubscribe(EventId.VehicleInstalled, Refresh);
        Dispatcher.Unsubscribe(EventId.PageChanged, Refresh);
    }

    protected override void InstantiateTutorialParts()
    {
        MenuController.InstantiateTutorialPart(
            holder:      holder,
            path:        "Tutorials/ArrowPointerWrapper",
            anchor:      TutorialHolder.CamAnchors.upperLeft,
            position:    TutorialsController.MainMenuButtons.BackBtn.localPosition + Vector3.right * BackArrowXOffset,
            yPos:        TutorialsController.MainMenuButtons.BackBtn.localPosition.y + BackArrowYOffset,
            eulerAngles: Vector3.forward * -135,
            partName:    "backArrow");
    }
}
