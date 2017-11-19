using System.Collections;
using UnityEngine;

public class SelectVehicleTutorial : GoToVehicleShopTutorial
{
    public new static SelectVehicleTutorial Instance { get; private set; }

    public bool JustBoughtVehicle { get; private set; }
    public bool JustInstalledVehicle { get; private set; }

    private int selectedVehicleCellId;
    private GameObject arrow;

    public override bool IsActive
    {
        get
        {
            return ProfileInfo.TutorialIndex == index &&
                   GUIPager.ActivePageName == page &&
                   HasEnoughMoneyToBuyVehicle &&
                   !IsNeededVehicleSelected &&
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
        Dispatcher.Subscribe(EventId.VehicleShopFilled, OnVehicleShopFilled);
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
        Dispatcher.Unsubscribe(EventId.VehicleShopFilled, OnVehicleShopFilled);
    }

    protected override void Init(EventId id = 0, EventInfo info = null)
    {
        base.Init(id, info);

        index = (int)Tutorials.buyVehicle;
        page = TutorialPages.VehicleShopWindow.ToString();

        Refresh();
    }

    protected override void InstantiateTutorialParts() { }

    protected override IEnumerator RefreshingRoutine()
    {
        holder.Wrapper.SetActive(IsActive);

        // Вырубаем стрелку отдельно, т.к. она может быть не внутри враппера:
        if (arrow != null)
            arrow.gameObject.SetActive(IsActive);

        yield return new WaitForEndOfFrame();
    }

    private void OnVehicleShopFilled(EventId id, EventInfo info)
    {
        TryInstantiateArrow();
        Refresh();
    }

    private void OnShopItemSelected(EventId id, EventInfo info)
    {
        selectedVehicleCellId = ((EventInfo_I)info).int1;
        Refresh();
    }

    private void OnVehicleBought(EventId id, EventInfo info)
    {
        JustBoughtVehicle = true;
    }

    private void OnVehicleInstall(EventId id, EventInfo info)
    {
        JustInstalledVehicle = true;
    }

    private void TryInstantiateArrow()
    {
        if (arrow != null) // Очень костыльно, но тогглы хз где могут уничтожиться.
        {
            Destroy(arrow.gameObject);
            arrow = null;
        }

        if (!VehicleShop.Selectors.ContainsKey(NeededShopVehicleCellId))
            return;

        tk2dUIToggleControl toggle = VehicleShop.Selectors[NeededShopVehicleCellId].toggle;

        arrow
            = InstantiateTutorialPart(
                prefab:         TutorPrefab.ArrowPointer,
                anchor:         TutorialHolder.CamAnchors.lowerLeft,
                position:       toggle.transform.position,
                yPos:           toggle.transform.position.y + ArrowYOffset,
                eulerAngles:    Vector3.zero,
                partName:       "ArrowPointerWrapper");

        arrow.transform.parent = toggle.transform;
        // Без этого стрелка почему-то в первый раз ставится ниже:
        arrow.transform.SetY(arrow.transform.parent.position.y + ArrowYOffset);
    }
}
