using UnityEngine;

public class PatternShopItemCell : BodykitShopItemCell
{
    protected override Object RelatedShopWindow
    {
        get { return ShopManager.Instance.patternShop; }
    }
}
