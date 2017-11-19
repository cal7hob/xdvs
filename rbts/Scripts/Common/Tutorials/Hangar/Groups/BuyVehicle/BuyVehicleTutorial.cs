using System.Collections;
using UnityEngine;

public class BuyVehicleTutorial : GoToVehicleShopTutorial
{
    public new static BuyVehicleTutorial Instance { get; private set; }

    public bool JustBoughtVehicle { get; private set; }
    public bool JustInstalledVehicle { get; private set; }

    private int selectedVehicleCellId;

    public override bool IsActive
    {
        get
        {
            return ProfileInfo.TutorialIndex == index &&
                   GUIPager.ActivePage == page &&
                   HasEnoughMoneyToBuyVehicle &&
                   IsNeededVehicleSelected &&
                   !JustInstalledVehicle;
        }
    }

    private bool IsNeededVehicleSelected
    {
        get { return selectedVehicleCellId == NeededShopVehicleCellId; }
    }

    protected override void Awake()
    {
        base.Awake();

        Instance = this;

        Messenger.Subscribe(EventId.VehicleBought, Refresh, 4);
        Messenger.Subscribe(EventId.VehicleBought, OnVehicleBought);
        Messenger.Subscribe(EventId.VehicleInstalled, OnVehicleInstall);
        Messenger.Subscribe(EventId.VehicleInstalled, Refresh, 4);
        Messenger.Subscribe(EventId.VehicleSelected, OnShopItemSelected);
        Messenger.Subscribe(EventId.VehicleSelected, Refresh, 4);
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();

        Instance = null;

        Messenger.Unsubscribe(EventId.VehicleBought, Refresh);
        Messenger.Unsubscribe(EventId.VehicleBought, OnVehicleBought);
        Messenger.Unsubscribe(EventId.VehicleInstalled, OnVehicleInstall);
        Messenger.Unsubscribe(EventId.VehicleInstalled, Refresh);
        Messenger.Unsubscribe(EventId.VehicleSelected, OnShopItemSelected);
        Messenger.Unsubscribe(EventId.VehicleSelected, Refresh);
    }

    protected override void Init(EventId id = 0, EventInfo info = null)
    {
        base.Init(id, info);

        index = (int)Tutorials.buyVehicle;
        page = TutorialPages.VehicleShopWindow.ToString();

        Refresh();
    }

    protected override void InstantiateTutorialParts()
    {
        InstantiateTutorialPart(
            path:           "Tutorials/ArrowPointerWrapper",
            anchor:         TutorialHolder.CamAnchors.lowerLeft,
            position:       HangarController.Instance.BuyingBox.BtnBuy.transform.position - Vector3.right * BuyBtnArrowXOffset,
            yPos:           HangarController.Instance.BuyingBox.BtnBuy.transform.position.y + BuyBtnArrowYOffset + GetYPosCorrection(),
            eulerAngles:    Vector3.forward * 90);
    }

    protected override IEnumerator RefreshingRoutine()
    {
        holder.Wrapper.SetActive(IsActive);
        yield return new WaitForEndOfFrame();
    }

    private void OnShopItemSelected(EventId id, EventInfo info)
    {
        selectedVehicleCellId = ((EventInfo_I)info).int1;
    }

    private void OnVehicleBought(EventId id, EventInfo info)
    {
        JustBoughtVehicle = true;
    }

    private void OnVehicleInstall(EventId id, EventInfo info)
    {
        JustInstalledVehicle = true;
    }
}
