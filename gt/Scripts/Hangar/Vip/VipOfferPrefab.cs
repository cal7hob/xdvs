using UnityEngine;
using System.Collections;

public class VipOfferPrefab : BankLotBase
{
    public tk2dTextMesh expirationLable;
    public tk2dTextMesh localizedPrice;
    public GameObject buyButton;
    public tk2dSlicedSprite offerBackground;
    public OldPriceRenderer oldPriceRenderer;
    public HorizontalLayout durationAligner;

    public string OfferIapId { get; private set; }

    public int VipDurationDays { get; set; }

    private void OnClick()
    {

        if (VipShopPage.Instance.vipsPanel.ScrollableItemsBehaviour.ScrollableArea.IsSwipeScrollingInProgress)
        {
            return;
        }        
        IapManager.BuyProductId(offerBackground.transform, OfferIapId, () =>
            {
                MenuController.BuyingSound();
            });
    }

    public override void Initialize(params object[] parameters)
    {
        var num = (int) parameters[0];
        var currentStoreId = parameters[1] as string;
        var vipDurationDays = (int) parameters[2];
        var localPrice = parameters[3] as string;

        localizedPrice.name = string.Format("lblBuyVip{0}", num);
        expirationLable.name = string.Format("lblVipOfferTime{0}", num);
        OfferIapId = currentStoreId;
        localizedPrice.text = PriceLocalizationAgent.GetLocalizedString(currentStoreId, localizedPrice, "lblBuyVip" + num); ;
        VipDurationDays = vipDurationDays;
    }
}
