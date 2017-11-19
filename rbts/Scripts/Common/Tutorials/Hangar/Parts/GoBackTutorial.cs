using UnityEngine;

public class GoBackTutorial : Tutorial
{
    public override bool IsActive
    {
        get
        {
            return  ProfileInfo.RecentlyFinishedTutorialIndex == index && 
                    GUIPager.ActivePage == page && (BuyModuleUpgradeTutorial.Instance != null &&              BuyModuleUpgradeTutorial.Instance.JustReceivedModule || 
                    BuyPatternTutorial.Instance != null && BuyPatternTutorial.Instance.JustReceivedCamouflage ||
                    BuyVehicleTutorial.Instance != null && BuyVehicleTutorial.Instance.JustInstalledVehicle);
        }
    }

    protected override void Awake()
    {
        holder = GetComponent<TutorialHolder>();

        Messenger.Subscribe(EventId.ModuleReceived, Refresh, 4);
        Messenger.Subscribe(EventId.CamouflageBought, Refresh, 4);
        Messenger.Subscribe(EventId.VehicleInstalled, Refresh, 4);
        Messenger.Subscribe(EventId.PageChanged, Refresh);
    }

    protected override void OnDestroy()
    {
        Messenger.Unsubscribe(EventId.ModuleReceived, Refresh);
        Messenger.Unsubscribe(EventId.CamouflageBought, Refresh);
        Messenger.Unsubscribe(EventId.VehicleInstalled, Refresh);
        Messenger.Unsubscribe(EventId.PageChanged, Refresh);
    }

    protected override void InstantiateTutorialParts()
    {
        InstantiateTutorialPart(
            path:        "Tutorials/ArrowPointerWrapper",
            anchor:      TutorialHolder.CamAnchors.upperLeft,
            position:    TutorialsController.MainMenuButtons.BackBtn.localPosition + Vector3.right * BackArrowXOffset,
            yPos:        TutorialsController.MainMenuButtons.BackBtn.localPosition.y + BackArrowYOffset,
            eulerAngles: Vector3.forward * -135,
            partName:    "backArrow");
    }
}
