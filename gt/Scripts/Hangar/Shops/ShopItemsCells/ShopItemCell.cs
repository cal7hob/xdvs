using System;
using UnityEngine;
using Object = UnityEngine.Object;

public abstract class ShopItemCell : MonoBehaviour
{
    public tk2dUIToggleControl toggle;
    public ActivatedUpDownButton lockScript;
    public tk2dBaseSprite sprImage;
    public tk2dBaseSprite sprLockedImage; // Если кому-то нужно изменение прозрачности/спрайта на недоступной по уровню технике – можно дублировать спрайт, поместить его в lockedObject.
    public GameObject icoVipObject;
    public tk2dTextMesh lblLockedUntilLevel;
    public SaleSticker saleSticker;
    public tk2dTextMesh[] descriptionLabels;

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

        toggle.SendMessageTarget = shopWindow.gameObject;
        toggle.SendMessageOnToggleMethodName = shopWindow.OnToggleMethodName;
        toggle.GetComponent<tk2dUIItem>().SendMessageOnClickMethodName = string.Empty;

        SetScrollableArea(shopWindow.ScrollableArea);

        toggle.name = string.Format("{0}_{1}", ButtonNamePrefix, shopItem.Id);

        if (descriptionLabels != null)
            for (int i = 0; i < descriptionLabels.Length; i++)
                if (descriptionLabels[i] != null)
                    descriptionLabels[i].text = shopItem.Description;

        lockScript.Activated = !shopItem.LockCondition;

        if (lblLockedUntilLevel != null && shopItem.LockCondition)
            lblLockedUntilLevel.text = shopItem.AvailabilityLevel.ToString();

        icoVipObject.SetActive(shopItem.VipCondition);
    }

    public T GetItem<T>()
    {
        return (T) ShopItem;
    }

    private void SetScrollableArea(tk2dUIScrollableArea scrollableArea)
    {
        var cellOffsetY = MenuController.Instance.bottomGuiPanel.mesh.bounds.center.y;

        tk2dUILayout layout = toggle.GetComponent<tk2dUILayout>();

        float w = (layout.GetMaxBounds() - layout.GetMinBounds()).x;

        layout.transform.parent = scrollableArea.contentContainer.transform;
        layout.transform.localPosition = new Vector3(cellOffsetX, cellOffsetY, -1);

        cellOffsetX += (w + CELL_SPACING);

        scrollableArea.ContentLength = cellOffsetX + (layout.GetMaxBounds() - layout.GetMinBounds()).x;
    }
}
