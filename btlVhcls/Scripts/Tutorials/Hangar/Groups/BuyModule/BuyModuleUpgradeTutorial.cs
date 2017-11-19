using System.Collections;
using UnityEngine;

public class BuyModuleUpgradeTutorial : GoToModuleShopTutorial
{
    public new static BuyModuleUpgradeTutorial Instance { get; private set; }

    public bool JustReceivedModule { get; private set; }

    protected override void Awake()
    {
        base.Awake();

        Dispatcher.Subscribe(EventId.ModuleBought, Refresh);
        Dispatcher.Subscribe(EventId.ModuleReceived, Refresh);
        Dispatcher.Subscribe(EventId.ModuleReceived, OnModuleReceived);

        Instance = this;
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();

        Dispatcher.Unsubscribe(EventId.ModuleBought, Refresh);
        Dispatcher.Unsubscribe(EventId.ModuleReceived, Refresh);
        Dispatcher.Unsubscribe(EventId.ModuleBought, OnModuleReceived);

        Instance = null;
    }

    protected override void Init(EventId id = 0, EventInfo info = null)
    {
        base.Init(id, info);

        index = (int)Tutorials.vehicleUpgrade;
        page = TutorialPages.Armory.ToString();

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

    private void OnModuleReceived(EventId id, EventInfo info)
    {
        JustReceivedModule = true;
    }
}

