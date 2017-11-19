using UnityEngine;

public class HorizontalScrollableItemsBehaviour : ScrollableItemsBehaviour
{
    protected override void ItemsReposition()
    {
        for (int i = 0; i < ItemsPanel.ScrollableItems.Count; i++)
        {
            var lot = ItemsPanel.ScrollableItems[i];
            lot.SetPosition(GetPos(i, lot.Size));
        }

        UpdateContentLength();
    }

    public override void UpdateContentLength()
    {
        if (scrollableArea)
        {
            var length = leftPadding;

            for (int i = 0; i < ItemsPanel.ScrollableItems.Count; i++)
            {
                var lot = ItemsPanel.ScrollableItems[i];

                if (lot.gameObject.activeSelf)
                {
                    length += lot.Size.x + HorizontalOffset;
                }
            }

            scrollableArea.ContentLength = length;
        }
    }

    public override Vector3 GetPos(int frameIndex, Vector2 itemSize)
    {
        var firstFramePos = new Vector3(itemSize.x * 0.5f + leftPadding, -topPadding, 0);
        var currentFramePos = firstFramePos;

        for (int i = 0; i < frameIndex; i++)
        {
            if (ItemsPanel.ScrollableItems[i].gameObject.activeSelf)
                currentFramePos += Vector3.right * (horizontalOffset + itemSize.x);
        }

        return currentFramePos;
    }
}
