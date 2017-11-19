using UnityEngine;

public class VerticalScrollableItemsBehaviour : ScrollableItemsBehaviour
{
    [SerializeField] protected int cols = 0;

    protected override void ItemsReposition()
    {
    }

    public override void UpdateContentLength()
    {
        if (scrollableArea == null)
            return;

        var contentLength = 0f;
        var itemsCount = ItemsPanel.ScrollableItems.Count;

        for (int i = 0; i < itemsCount; i++)
        {
            var item = ItemsPanel.ScrollableItems[i];
            contentLength += (item.Size.y + VerticalOffset) * (i % cols > 0 ? 0 : 1);
        }

        scrollableArea.ContentLength = contentLength;
    }

    public override Vector3 GetPos(int index, Vector2 itemSize)
    {
        int colIndex;
        int rowIndex;
        float x;
        float y;

        colIndex = index % cols;
        rowIndex = (int)((float)index / cols);

        x = leftPadding + itemSize.x * 0.5f + colIndex * (itemSize.x + horizontalOffset);
        y = -topPadding - 0.5f * itemSize.y - rowIndex * (itemSize.y + verticalOffset);

        return new Vector3(x, y, 0);
    }
}