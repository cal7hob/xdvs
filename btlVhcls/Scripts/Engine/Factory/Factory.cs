using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class Factory : MonoBehaviour, IItemsFactory
{
    [SerializeField] private GameObject prefab;
    [SerializeField] private bool alignItemsByCenter = false;
    [SerializeField] private Transform itemsParent;
    [SerializeField] private int rows = 0;
    [SerializeField] private int cols = 0;
    [SerializeField] private float leftPadding = 0;
    [SerializeField] private float rightPadding = 0;
    [SerializeField] private float topPadding = 0;
    [SerializeField] private float bottomPadding = 0;
    [SerializeField] private float horizontalOffset = 0;
    [SerializeField] private float verticalOffset = 0;
    [SerializeField] private tk2dUIScrollableArea scrollableArea;
    [SerializeField] private tk2dUILayout mainLayout;
    [SerializeField] protected Renderer bottomSpriteRenderer;
    [Header("Если префаб итема выровнен по центру - а панель")]
    [Header("вертикальная - используем addItemHalfSize%Offset")]
    [SerializeField] protected bool addItemHalfSizeXOffset = false;
    [SerializeField] protected bool addItemHalfSizeYOffset = false;

    public tk2dUIScrollableArea ScrollableArea { get { return scrollableArea; } }
    public float ItemLength { get { return IsVertical ? itemSize.y : itemSize.x; } }
    public float StartPadding { get { return IsVertical ? topPadding : leftPadding; } }
    public float EndPadding { get { return IsVertical ? bottomPadding : rightPadding; } }
    public float Offset { get { return IsVertical ? verticalOffset : horizontalOffset; } }
    public int ContentRowCount { get { return Mathf.CeilToInt((float)items.Count / (IsVertical ? (float)cols : (float)rows)); } }
    public tk2dUILayout MainLayout { get { return mainLayout; } }
    public Vector2 ItemAnchorSpecificOffset { get { return new Vector2(addItemHalfSizeXOffset ? itemHalfSize.x : 0, addItemHalfSizeYOffset ? itemHalfSize.y : 0); } }

    private bool isInited = false;
    private Vector2 itemSize;
    private Vector2 itemHalfSize;
    private float horizontalSizePlusOffset = 0;
    private float verticalSizePlusOffset = 0;

    private int lastCamHeight;
    private Dictionary<string, IItem> itemsDic = new Dictionary<string, IItem>();

    public Transform Parent { get { return itemsParent; }}
    public GameObject Prefab { get { return prefab; } }
    public bool IsVertical { get { return cols > 0; } }

    protected List<IItem> items = new List<IItem>();
    public List<IItem> Items { get { return items; } }

    protected virtual void Start()
    {
        ReshapeLayout();
    }

    public IItem Create(params object[] parameters)
    {
        IItem iface = CreateOne(parameters);
        if (alignItemsByCenter)
            AlignAllItems();//Если итемы по центру - то координаты изменятся у всех итемов
        else
            AlignItem(items.Count - 1);//Если итемы по левому краю, то все выравнивать не надо, только последний
        if (iface != null)
            UpdateContentLength();
        return iface;
    }

    /// <summary>
    /// Instantiate items or update it with data
    /// </summary>
    /// <param name="destroyAllIfNotEmpty">if true - force clear all items in factory, if false - you can reinitialize items with new data</param>
    public void CreateAll(IEnumerable allData, bool destroyAllIfNotEmpty = true, ParamDict commonParameters = null)
    {
        if (prefab == null)
            return;

        if (destroyAllIfNotEmpty && items.Count > 0)
            DestroyAll();

        if (items.Count == 0)
        {
            foreach (var data in allData)
                CreateOne(data, commonParameters);

            AlignAllItems();

            UpdateContentLength();
        }
        else
        {
            int i = 0;
            foreach (var data in allData)
            {
                items[i].Initialize(new object[] { data, commonParameters });
                i++;
            }
                
        }
    }

    private void AlignAllItems()
    {
        for(int i = 0; i < items.Count; i++)
            AlignItem(i);
    }

    private void AlignItem(int index)
    {
        items[index].MainTransform.localPosition = GetPosByIndex(index);
    }

    private IItem CreateOne(params object[] parameters)
    {
        if (prefab == null)
            return null;

        GameObject go = Instantiate(Prefab, Parent);
        go.transform.localPosition = Vector3.zero;

        IItem iface = go.GetComponent<IItem>();
        if (iface == null)
            return null;

        if (!isInited)
        {
            itemSize = iface.GetSize();
            itemHalfSize = iface.GetSize() / 2f;
            horizontalSizePlusOffset = itemSize.x + horizontalOffset;
            verticalSizePlusOffset = itemSize.y + verticalOffset;
            isInited = true;
        }

        items.Add(iface);
        iface.Initialize(parameters);
        itemsDic[iface.GetUniqId] = iface;

        return iface;
    }

    private void UpdateContentLength()
    {
        if (scrollableArea)
            scrollableArea.ContentLength = StartPadding + (float)ContentRowCount * ItemLength + ((float)ContentRowCount - 1f) * Offset + EndPadding;
    }

    public void ScroolToItem(int index)
    {
        if(!scrollableArea.scrollBar)
        {
            Debug.LogError("You want to scroll factory, but scrollableArea.scrollBar is not defined!");
            return;
        }
        if (Mathf.Approximately(scrollableArea.ContentLength, 0))
        {
            Debug.LogError("You want to scroll factory, but scrollableArea.ContentLength == 0. DEVISION BY ZERO.");
            return;
        }

        index = Mathf.Clamp(index, 0, items.Count - 1);


        //float pos = IsVertical ?
        //    (items[index].MainTransform.localPosition.y + topPadding + ItemAnchorSpecificOffset.y) :
        //    (items[index].MainTransform.localPosition.x - leftPadding - ItemAnchorSpecificOffset.x);
        //scrollableArea.scrollBar.Value = Math.Abs(pos) / scrollableArea.ContentLength;

        scrollableArea.scrollBar.Value = GetItemScroolProgressByIndex(index);
    }

    protected virtual Vector3 GetPosByIndex(int index)
    {
        int colIndex, rowIndex;
        GetItemColAndRowByIndex(index, out colIndex, out rowIndex);

        if (alignItemsByCenter)
        {
            float koef = 0;//koef = (float)index - (((float)items.Count - 1f) / 2f);
            Vector3 v;
            if (IsVertical)
            {
                //Подставить вместо cols реальное количество итемов в данной строчке (в последней строчке оно будет меньше cols)
                koef = (float)colIndex - (((float)GetRowColItemsCount(rowIndex) - 1f) / 2f);

                v = new Vector3(
                    horizontalSizePlusOffset * koef - (addItemHalfSizeXOffset ? itemHalfSize.x : 0f),
                    -topPadding - ItemAnchorSpecificOffset.y - rowIndex * verticalSizePlusOffset,
                    0);
            }
            else
            {
                koef = (float)rowIndex - (((float)GetRowColItemsCount(colIndex) - 1f) / 2f);

                v = new Vector3(
                    leftPadding + ItemAnchorSpecificOffset.x + colIndex * horizontalSizePlusOffset,
                    verticalSizePlusOffset * koef - (addItemHalfSizeXOffset ? itemHalfSize.y : 0f),
                    0);
            }

            //Debug.LogErrorFormat("Align item name = {0} with index {1}, result = {2}",MiscTools.GetFullTransformName(items[index].MainTransform), index, v);
            return v;
        }
        else
        {
            return new Vector3(
                leftPadding + ItemAnchorSpecificOffset.x + colIndex * horizontalSizePlusOffset,
                -topPadding - ItemAnchorSpecificOffset.y - rowIndex * verticalSizePlusOffset,
                0);
        }
    }

    private void GetItemColAndRowByIndex(int index, out int colIndex, out int rowIndex)
    {
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
    }

    private int GetRowColItemsCount(int index)
    {
        return IsVertical ?
            Mathf.Clamp(items.Count - index * cols, 0, cols) :
            Mathf.Clamp(items.Count - index * rows, 0, rows);
    }

    private int RowsColsCount { get { return Mathf.CeilToInt((float)items.Count / (IsVertical ? (float)cols : (float)rows)); } }

    private float GetItemScroolProgressByIndex(int index)
    {
        int colIndex, rowIndex;
        GetItemColAndRowByIndex(index, out colIndex, out rowIndex);
        float result = (IsVertical ? (float)rowIndex : (float)colIndex) / ((float)ContentRowCount - 1f);
        return result;
    }

    /// <summary>
    /// Решейпим Layout чтоб при смене разрешения растягивались элементы
    /// </summary>
    private void ReshapeLayout()
    {
        if (!MainLayout)
            return;

        float distanceToPanelBottom = 0;
        if(bottomSpriteRenderer)
            distanceToPanelBottom = bottomSpriteRenderer.bounds.max.y - GameData.CurSceneTk2dGuiCamera.ScreenExtents.yMin;
        var delta = (distanceToPanelBottom + GameData.CurSceneTk2dGuiCamera.ScreenExtents.yMin) - MainLayout.GetMinBounds().y;

        MainLayout.Reshape(new Vector3(0, delta, 0), Vector3.zero, true);
    }

    /// <summary>
    /// Ловим событие смены разрешения. Можно ловить событие OnResolutionChanged вместо этого апдейта.
    /// </summary>
    private void LateUpdate()
    {
        if (!MainLayout)
            return;
        if ((int)GameData.CurSceneTk2dGuiCamera.ScreenExtents.yMin != lastCamHeight)
        {
            lastCamHeight = (int)GameData.CurSceneTk2dGuiCamera.ScreenExtents.yMin;
            ReshapeLayout();
        }
    }

    public IItem GetItemByUniqId(object id)
    {
        string _id = id.ToString();
        if (itemsDic == null || !itemsDic.ContainsKey(_id))
            return null;
        return itemsDic[_id];
    }

    public void SimulateClickItem(string id)
    {
        IItem iface = GetItemByUniqId(id);
        if (iface != null && iface.MainUIItem != null)
            iface.MainUIItem.SimulateClick();
    }

    public void DestroyAll()
    {
        for(int i = 0; i < items.Count; i++)
            Destroy(items[i].MainTransform.gameObject);

        items.Clear();
        itemsDic.Clear();
        UpdateContentLength();
    }
}