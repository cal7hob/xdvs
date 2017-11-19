public abstract class BodykitShopItemCell : ShopItemCell
{
    private const string BUTTON_NAME_PREFIX = "btnBodykit";

    protected override string ButtonNamePrefix
    {
        get { return BUTTON_NAME_PREFIX; }
    }

    public override void Set<TShopItemCell>(IShopItem shopItem)
    {
        base.Set<TShopItemCell>(shopItem);

        Bodykit bodykit = (Bodykit)shopItem;

        sprImage.SetSprite(bodykit.IconSprite.Collection, bodykit.IconSprite.spriteId);

        if (sprLockedImage)
            sprLockedImage.SetSprite(bodykit.IconSprite.Collection, bodykit.IconSprite.spriteId);
        if(lockScript)
            lockScript.Activated = !shopItem.LockCondition;
    }
}
