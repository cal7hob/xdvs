using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class Factory : MonoBehaviour, IItemsFactory
{
    [SerializeField] private GameObject prefab;
    [SerializeField] private Transform itemsParent;
    [SerializeField] private int rows = 0;
    [SerializeField] private int cols = 0;
    [SerializeField] private float leftPadding = 0;
    [SerializeField] private float topPadding = 0;
    [SerializeField] private float horizontalOffset = 0;
    [SerializeField] private float verticalOffset = 0;
    [SerializeField] private tk2dUIScrollableArea scrollableArea;
    [SerializeField] private tk2dUILayout mainLayout;

    [Header("Если префаб итема выровнен по центру - а панель")]
    [Header("вертикальная - используем addItemHalfSize%Offset")]
    [SerializeField] protected bool addItemHalfSizeXOffset = false;
    [SerializeField] protected bool addItemHalfSizeYOffset = false;

    public float ItemLength { get { return IsVertical ? itemSize.y : itemSize.x; } }
    public float Padding { get { return IsVertical ? topPadding : leftPadding; } }
    public float Offset { get { return IsVertical ? verticalOffset : horizontalOffset; } }
    public int ContentRowCount { get { return Mathf.CeilToInt((float)items.Count / (IsVertical ? (float)cols : (float)rows)); } }
    public tk2dUILayout MainLayout { get { return mainLayout; } }

    private bool isInited = false;
    private Vector2 itemSize;
    private Vector2 itemHalfSize;
    
    private int lastCamHeight;
    private Dictionary<string, IItem> itemsDic = new Dictionary<string, IItem>();

    public Transform Parent { get { return itemsParent; }}
    public GameObject Prefab { get { return prefab; } }
    public bool IsVertical { get { return cols > 0; } }

    protected List<IItem> items = new List<IItem>();
    public List<IItem> Items { get { return items; } }

    public IItem Create(params object[] parameters)
    {
        if (prefab == null)
            return null;

        GameObject go = Instantiate(Prefab, Parent);

        IItem iface = go.GetComponent<IItem>();
        if (iface == null)
            return null;

        if (!isInited)
        {
            itemSize = iface.GetSize();
            itemHalfSize = iface.GetSize() / 2f;
            isInited = true;
        }

        var posByIndex = GetPosByIndex(items.Count);
        go.transform.localPosition = new Vector3(posByIndex.x, posByIndex.y, 0); //go.transform.localPosition.z);

        items.Add(iface);
        iface.Initialize(parameters);
        itemsDic[iface.GetUniqId] = iface;

        UpdateContentLength();

        return iface;
    }

    private void UpdateContentLength()
    {
        if (scrollableArea)
            scrollableArea.ContentLength = 2f * Padding + (float)ContentRowCount * (ItemLength + Offset);
    }

    protected virtual Vector2 GetPosByIndex(int index)
    {
        int colIndex;
        int rowIndex;
        float x;
        float y;

        if (IsVertical)
        {
            colIndex = index % cols;
            rowIndex = (int)((float)index / (float)cols);
        }
        else
        {
            colIndex = (int)((float)index / (float)rows);
            rowIndex = index % rows;
        }

        x = leftPadding + (addItemHalfSizeXOffset ? itemHalfSize.x : 0) + colIndex * (itemSize.x + horizontalOffset);
        y = -topPadding - (addItemHalfSizeYOffset ? itemHalfSize.y : 0) - rowIndex * (itemSize.y + verticalOffset);

        return new Vector2(x, y);
    }

    public IItem GetItemByUniqId(string id)
    {
        if (itemsDic == null || !itemsDic.ContainsKey(id))
            return null;
        return itemsDic[id];
    }

    public void DestroyAll()
    {
        for (int i = 0; i < items.Count; i++)
            Destroy(items[i].MainTransform.gameObject);

        items.Clear();
        itemsDic.Clear();
        UpdateContentLength();
    }
}
