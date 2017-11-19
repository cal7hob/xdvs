using System.Collections.Generic;
using UnityEngine;

public class ItemsPanel : MonoBehaviour
{
    [SerializeField] private ScrollableItemsBehaviour scrollableBehaviour;
    [SerializeField] private Transform parent;
    public List<ScrollableItem> ScrollableItems = new List<ScrollableItem>();

    public virtual Transform Parent
    {
        get
        {
            return ScrollableItemsBehaviour.ScrollableArea == null ? parent : ScrollableItemsBehaviour.ScrollableArea.contentContainer.transform;
        }
    }

    public virtual ScrollableItemsBehaviour ScrollableItemsBehaviour { get { return scrollableBehaviour; } }

    protected ScrollableItem Create(GameObject itemPrefab)
    {
        if (itemPrefab == null)
            return null;

        GameObject go = Instantiate(itemPrefab, Parent);
        ScrollableItem scrollableItem = go.GetComponent<ScrollableItem>();

        return scrollableItem;
    }

    public virtual ScrollableItem CreateLotByGivenPrefab(GameObject itemPrefab)
    {
        var lot = Create(itemPrefab);

        if (lot == null)
            return null;

        lot.SetPosition(scrollableBehaviour.GetPos(ScrollableItems.Count, lot.Size));
        ScrollableItems.Add(lot);

        return lot;
    }
}
