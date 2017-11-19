using System;
using UnityEngine;
using Object = UnityEngine.Object;

public abstract class ShopItemCell : MonoBehaviour
{
    public GameObject icoVipObject;

    private const float CELL_SPACING = 33.0f;

    private static float cellOffsetX;

    protected abstract string ButtonNamePrefix { get; }

    protected abstract Object RelatedShopWindow { get; }

    public IShopItem ShopItem { get; private set; }

    public static void ResetCellOffset() { cellOffsetX = GUIController.halfScreenWidth; }

    public virtual void Set<TShopItemCell>(IShopItem shopItem)
        where TShopItemCell : ShopItemCell
    {
        ShopItem = shopItem;

        Shop<TShopItemCell> shopWindow = (Shop<TShopItemCell>)RelatedShopWindow;

        //SetScrollableArea(shopWindow.ScrollableArea);

        icoVipObject.SetActive(shopItem.VipCondition);
    }

    public T GetItem<T>()
    {
        return (T) ShopItem;
    }

    /*private void SetScrollableArea(tk2dUIScrollableArea scrollableArea)
    {
        var cellOffsetY
            = GameData.IsGame(Game.SpaceJet | Game.BattleOfWarplanes)
                ? transform.localPosition.y
                : HangarController.Instance.bottomGuiPanel.mesh.bounds.center.y;
    }*/
}
