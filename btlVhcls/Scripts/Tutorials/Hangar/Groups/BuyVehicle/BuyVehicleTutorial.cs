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
                   GUIPager.ActivePageName == page &&
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

        Dispatcher.Subscribe(EventId.VehicleBought, Refresh, 4);
        Dispatcher.Subscribe(EventId.VehicleBought, OnVehicleBought);
        Dispatcher.Subscribe(EventId.VehicleInstalled, OnVehicleInstall);
        Dispatcher.Subscribe(EventId.VehicleInstalled, Refresh, 4);
        Dispatcher.Subscribe(EventId.VehicleSelected, OnShopItemSelected);
        Dispatcher.Subscribe(EventId.VehicleSelected, Refresh, 4);
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();

        Instance = null;

        Dispatcher.Unsubscribe(EventId.VehicleBought, Refresh);
        Dispatcher.Unsubscribe(EventId.VehicleBought, OnVehicleBought);
        Dispatcher.Unsubscribe(EventId.VehicleInstalled, OnVehicleInstall);
        Dispatcher.Unsubscribe(EventId.VehicleInstalled, Refresh);
        Dispatcher.Unsubscribe(EventId.VehicleSelected, OnShopItemSelected);
        Dispatcher.Unsubscribe(EventId.VehicleSelected, Refresh);
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
            prefab:         TutorPrefab.ArrowPointer,
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
