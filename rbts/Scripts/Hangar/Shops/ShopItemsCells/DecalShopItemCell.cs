using UnityEngine;

public class DecalShopItemCell : BodykitShopItemCell
{
    protected override Object RelatedShopWindow
    {
        get { return ShopManager.Instance.decalShop; }
    }
}
