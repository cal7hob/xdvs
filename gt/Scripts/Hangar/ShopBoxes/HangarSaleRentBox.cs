using UnityEngine;
using System.Collections;

public class HangarSaleRentBox : HangarRentBox {

    [SerializeField]
    private float saleRatio = 1;
    [SerializeField]
    private tk2dTextMesh lblOldPrice;
    [SerializeField]
    private GameObject sprOldSilver;
    [SerializeField]
    private GameObject sprOldGold;

    private tk2dTextMesh lblNewPrice;
    private GameObject sprNewSilver;
    private GameObject sprNewGold;

    public override ProfileInfo.Price Price
    {
        get
        {
            return base.Price;
        }
        set
        {
            OldPrice = value;
            ProfileInfo.Price salePrice = new ProfileInfo.Price(value);

            ShopOffer offer = null;

            if (PatternShop.Instance && PatternShop.Instance.IsInShop && PatternOffersController.Instance.CheckIfItemOnSale(PatternShop.Instance.BodyKitInViewId))
            {
                offer = PatternOffersController.Instance.Offers[PatternShop.Instance.BodyKitInViewId];   
            }
            else if (DecalShop.Instance && DecalShop.Instance.IsInShop && DecalOffersController.Instance.CheckIfItemOnSale(DecalShop.Instance.BodyKitInViewId))
            {
                offer = DecalOffersController.Instance.Offers[DecalShop.Instance.BodyKitInViewId];
            }

            if (offer != null)
            {
                salePrice = offer.DiscountPrice;
            }

            base.Price = salePrice;
        }
    }

    private ProfileInfo.Price OldPrice
    {
        set
        {
            ChangeActivePrice(false);
            base.Price = new ProfileInfo.Price(value);
            ChangeActivePrice(true);
        }
    }

    void Awake()
    {
        lblNewPrice = lblPrice;
        sprNewSilver = sprSilver;
        sprNewGold = sprGold;
    }

    private void ChangeActivePrice(bool newPrice)
    {
        lblPrice = newPrice ? lblNewPrice : lblOldPrice;
        sprSilver = newPrice ? sprNewSilver : sprOldSilver;
        sprGold = newPrice ? sprNewGold : sprOldGold;
    }
}
