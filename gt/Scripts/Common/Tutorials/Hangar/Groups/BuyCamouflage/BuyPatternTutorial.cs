using System;
using System.Collections;
using UnityEngine;

class BuyPatternTutorial : GoToPatternShopTutorial
{
    public new static BuyPatternTutorial Instance
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
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();

        Instance = null;

        Dispatcher.Unsubscribe(EventId.CamouflageBought, Refresh);
        Dispatcher.Unsubscribe(EventId.BodyKitSelected, Refresh);
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
        MenuController.InstantiateTutorialPart(
            holder:         holder,
            path:           "Tutorials/ArrowPointerWrapper",
            anchor:         TutorialHolder.CamAnchors.lowerLeft,
            position:       MenuController.Instance.rentBox.BtnBuy.transform.position - Vector3.right * BuyBtnArrowXOffset,
            yPos:           MenuController.Instance.rentBox.BtnBuy.transform.position.y + BuyBtnArrowYOffset + GetYPosCorrection(),
            eulerAngles:    Vector3.forward * 90);
    }

    protected override IEnumerator RefreshingRoutine()
    {
        holder.Wrapper.SetActive(IsActive);
        yield return new WaitForEndOfFrame();
    }
}