using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using CodeStage.AntiCheat.ObscuredTypes;
using System.Linq;

public class InventoryBase : MonoBehaviour
{
    public const int EMPTY_CELL_ID = -1;
    public int Capacity { get { return GetCapacity(); } }
    public int EmptyCellId { get { return EMPTY_CELL_ID; } }
    public Factory factory;
    private bool isAwaked = false;
    public bool IsAwaked { get { return isAwaked; } set { isAwaked = value; } }

    public int EmptyCellsCount { get { return GetFactoryItemsWithContentId(EmptyCellId).Count; } }

    private List<IInventoryItem> list = new List<IInventoryItem>();//to avoid many list creations

    protected virtual void Awake()
    {
        IsAwaked = true;
    }

    protected virtual void Start()
    {
        FillInventory();
    }

    protected virtual void OnDestroy()
    {
    }

    public void CreateEmptyItems()
    {
        List<object[]> data = new List<object[]>();
        for (int i = 0; i < Capacity; i++)
            data.Add(new object[] { i, EmptyCellId});//Упаковка данных
        factory.CreateAll(data, true, new ParamDict().Add("inventoryPanel", this));
    }

    public bool Add(int contentId, int slot = -1)
    {
        if (!HaveContent(contentId) || HasFactoryItemWithContentId(contentId) || !HasEmptyCell())
            return false;

        for(int i = 0; i < Capacity; i++)
        {
            IInventoryItem iInventoryItem = (IInventoryItem)factory.Items[i];
            if (iInventoryItem.IsEmpty && (slot == -1 || slot == i))
            {
                iInventoryItem.ContentId = contentId;
                return true;
            }
        }
            
        return false;
    }

    public void Clear(int contentId)
    {
        if (contentId == EmptyCellId)
            return;
        for (int i = 0; i < Capacity; i++)
        {
            IInventoryItem iInventoryItem = (IInventoryItem)factory.Items[i];
            if (iInventoryItem.ContentId == contentId)
            {
                iInventoryItem.ContentId = EmptyCellId;
                return;
            }
        }
    }

    public bool HasEmptyCell()
    {
        return EmptyCellsCount > 0;
    }

    public bool HasFactoryItemWithContentId(int contentId)
    {
        return GetFactoryItemsWithContentId(contentId).Count > 0;
    }

    public List<IInventoryItem> GetFactoryItemsWithContentId(int contentId)
    {
        list.Clear();
        for (int i = 0; i < factory.Items.Count; i++)
        {
            IInventoryItem iInventoryItem = (IInventoryItem)factory.Items[i];
            if (iInventoryItem.ContentId == contentId)
                list.Add(iInventoryItem);
        }
        return list;
    }

    /// <summary>
    /// Установка контента в панель инвентаря. При указании номера слота - в конкретный слот или никак
    /// </summary>
    /// <param name="contentId">Ид контента</param>
    /// <param name="status">true - установить, false - снять</param>
    /// <param name="slot">желаемый номер слота</param>
    public void SetContentToInventory(int contentId, bool status, int slot = -1)
    {
        if (contentId == EmptyCellId || !IsMyItem(contentId))
            return;

        bool isInstalled = false;//Установлен в слот
        if (status)
        {
            //Debug.LogFormat("{0}. Try to add content {1} to slot {2}!", GetType().ToString(), contentId, slot);
            isInstalled = Add(contentId, slot);
            if (!isInstalled && HaveContent(contentId))
                Debug.LogWarningFormat("{0}. Cant add content {1} to slot {2}!",GetType().ToString(), contentId, slot);
        }
        else
            Clear(contentId);

        SendEventToApplyChanges(contentId, isInstalled, slot);
    }

    /// <summary>
    /// Посылаем событие что поменялся инвентарь всем заинтересованным...
    /// </summary>
    protected virtual void SendEventToApplyChanges(int contentId, bool state, int slot)
    {
    }

    public virtual void FillInventory()
    {
        if (!IsAwaked)
            return;
    }

    protected virtual bool HaveContent(int contentId)
    {
        return false;
    }

    protected virtual bool IsMyItem(int id)
    {
        return false;
    }

    protected virtual int GetCapacity()
    {
        return 0;
    }
}
