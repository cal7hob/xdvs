using System.Collections;
using UnityEngine;

public class BuyVehicleTutorial : GoToVehicleShopTutorial
{
    public new static BuyVehicleTutorial Instance { get; private set; }

    private int selectedVehicleCellId;

    public override bool IsActive
    {
        get
        {
            return ProfileInfo.TutorialIndex == index &&
                   GUIPager.ActivePage == page &&
                   HasEnoughMoneyToBuyVehicle &&
                   IsNeededVehicleSelected;
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
        Dispatcher.Subscribe(EventId.VehicleInstalled, Refresh, 4);
        Dispatcher.Subscribe(EventId.VehicleSelected, OnShopItemSelected);
        Dispatcher.Subscribe(EventId.VehicleSelected, Refresh, 4);
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();

        Instance = null;

        Dispatcher.Unsubscribe(EventId.VehicleBought, Refresh);
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
        MenuController.InstantiateTutorialPart(
            holder:         holder,
            path:           "Tutorials/ArrowPointerWrapper",
            anchor:         TutorialHolder.CamAnchors.lowerLeft,
            position:       MenuController.Instance.BuyingBox.BtnBuy.transform.position - Vector3.right * BuyBtnArrowXOffset,
            yPos:           MenuController.Instance.BuyingBox.BtnBuy.transform.position.y + BuyBtnArrowYOffset + GetYPosCorrection(),
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
}
