using System;
using System.Collections;
using UnityEngine;

class BuyPatternTutorial : GoToPatternShopTutorial
{
    public new static BuyPatternTutorial Instance
    {
        get; private set;
    }

    public bool JustReceivedCamouflage
    {
        get; private set;
    }

    public override bool IsActive
    {
        get
        {
            if (!base.IsActive)
                return false;

            if (PatternShop.Instance == null)
                return false;

            var selectedItem = PatternShop.Selectors[PatternShop.Instance.BodyKitInViewId].ShopItem;

            return CanBuySelectedCamouflage
                = !selectedItem.LockCondition &&
                  !selectedItem.VipCondition &&
                  !selectedItem.ComingSoonCondition;
        }
    }

    protected override void Awake()
    {
        base.Awake();

        Instance = this;

        Dispatcher.Subscribe(EventId.CamouflageBought, Refresh);
        Dispatcher.Subscribe(EventId.BodyKitSelected, Refresh);
        Dispatcher.Subscribe(EventId.CamouflageBought, OnCamouflageBought, 2);
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();

        Instance = null;

        Dispatcher.Unsubscribe(EventId.CamouflageBought, Refresh);
        Dispatcher.Unsubscribe(EventId.BodyKitSelected, Refresh);
        Dispatcher.Unsubscribe(EventId.CamouflageBought, OnCamouflageBought);
    }

    protected override void Init(EventId id = 0, EventInfo info = null)
    {
        if (isInitialized)
            return;

        base.Init(id, info);

        index = (int)Tutorials.buyCamouflage;
        page = TutorialPages.PatternShop.ToString();

        Refresh();
    }

    protected override void InstantiateTutorialParts()
    {
        InstantiateTutorialPart(
            prefab:         TutorPrefab.ArrowPointer,
            anchor:         TutorialHolder.CamAnchors.lowerLeft,
            position:       HangarController.Instance.rentBox.BtnBuy.transform.position - Vector3.right * BuyBtnArrowXOffset,
            yPos:           HangarController.Instance.rentBox.BtnBuy.transform.position.y + BuyBtnArrowYOffset + GetYPosCorrection(),
            eulerAngles:    Vector3.forward * 90);
    }

    protected override IEnumerator RefreshingRoutine()
    {
        holder.Wrapper.SetActive(IsActive);
        yield return new WaitForEndOfFrame();
    }

    private void OnCamouflageBought(EventId id, EventInfo info)
    {
        JustReceivedCamouflage = true;
    }
}