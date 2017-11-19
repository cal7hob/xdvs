using System;
using System.Collections.Generic;
using UnityEngine;

public abstract class ScrollableItemsBehaviour : MonoBehaviour
{
    [SerializeField] protected ItemsPanel ItemsPanel;
    [SerializeField] protected float leftPadding = 0;
    [SerializeField] protected float topPadding = 0;
    [SerializeField] protected float horizontalOffset = 0;
    [SerializeField] protected float verticalOffset = 0;
    [SerializeField] protected tk2dUIScrollableArea scrollableArea;

    public float LeftPadding { get { return leftPadding; } }
    public float TopPagging { get { return topPadding; } }
    public float HorizontalOffset { get { return horizontalOffset; } }
    public float VerticalOffset { get { return verticalOffset; } }

    public tk2dUIScrollableArea ScrollableArea { get { return scrollableArea; } }

    protected void Awake()
    {
        scrollableArea = scrollableArea ?? GetComponent<tk2dUIScrollableArea>();
    }

    
    protected abstract void ItemsReposition();
    public abstract void UpdateContentLength();
    public abstract Vector3 GetPos(int index, Vector2 itemSize);

    public void SetActiveBankLot(int lotIndex, bool activate)
    {
        if (ItemsPanel.ScrollableItems.Count == 0)
            return;

        if (ItemsPanel.ScrollableItems[lotIndex].gameObject.activeSelf != activate)
        {
            ItemsPanel.ScrollableItems[lotIndex].gameObject.SetActive(activate);
            ItemsReposition();
        }
    }

    public void SetActiveBankLot(GameObject lot, bool activate)
    {
        if (lot.activeSelf != activate)
        {
            lot.SetActive(activate);
            ItemsReposition();
        }
    }
}
