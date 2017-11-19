using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using CodeStage.AntiCheat.ObscuredTypes;

public class ConsumablesInventoryPanel : InventoryBase
{
    
    public static List<int> inventoryList = new List<int>();

    protected override void Awake()
    {
        base.Awake();
        Dispatcher.Subscribe(EventId.ConsumableBought, OnConsumableBought);
        Dispatcher.Subscribe(EventId.ChangeConsumableInventoryState, ChangeConsumableInventoryState);
        Dispatcher.Subscribe(EventId.WentToBattle, OnWentToBattle);
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
        Dispatcher.Unsubscribe(EventId.ConsumableBought, OnConsumableBought);
        Dispatcher.Unsubscribe(EventId.ChangeConsumableInventoryState, ChangeConsumableInventoryState);
        Dispatcher.Unsubscribe(EventId.WentToBattle, OnWentToBattle);
    }

    public static Dictionary<int, ObscuredInt> GetBattleInventoryDic()
    {
        Dictionary<int, ObscuredInt> dic = new Dictionary<int, ObscuredInt>();
        for (int i = 0; i < inventoryList.Count; i++)
            if(inventoryList[i] >= 0)
                dic[inventoryList[i]] = Mathf.Clamp(ProfileInfo.consumableInventory[inventoryList[i]].count, 0, GameData.consumableInfos[inventoryList[i]].maxInBattle);

        return dic;
    }

    public override void FillInventory()
    {
        if (!IsAwaked)
            return;

        #region Сначала пытаемся поставить в панель итемы из профиля
        for (int i = 0; i < GameData.consumablesInventoryCapacity; i++)
            if (i < ProfileInfo.consumableInventoryPanelItems.Count)// если в профиле сохранен текущий слот
                Dispatcher.Send(EventId.ChangeConsumableInventoryState, new EventInfo_U(ProfileInfo.consumableInventoryPanelItems[i], true, i));
        #endregion

        if (!HasEmptyCell())
            return;

        #region затем заполняем оставшиеся пустые ячейки
        List<int> unaddedPurchasedConsumables = new List<int>();
        List<int> data = ConsumablesPage.Instance.GetConsumablesData();
        for (int i = 0; i < data.Count; i++)
            if (ProfileInfo.HaveConsumable(data[i]) && !HasFactoryItemWithContentId(data[i]))
                unaddedPurchasedConsumables.Add(data[i]);

        int cycleLength = Math.Min(EmptyCellsCount, unaddedPurchasedConsumables.Count);//определяем сколько расходки нужно добавить в инвентарь
        for (int i = 0; i < cycleLength; i++)
            Dispatcher.Send(EventId.ChangeConsumableInventoryState, new EventInfo_U(unaddedPurchasedConsumables[i], true, -1));
        #endregion
    }

    protected override bool HaveContent(int contentId)
    {
        return ProfileInfo.HaveConsumable(contentId);
    }

    protected override int GetCapacity()
    {
        return GameData.consumablesInventoryCapacity;
    }

    protected override bool IsMyItem(int id)
    {
        return GameData.consumableInfos != null && GameData.consumableInfos.ContainsKey(id) && !GameData.consumableInfos[id].isSuperWeapon;
    }

    private void ChangeConsumableInventoryState(EventId id, EventInfo ei)
    {
        EventInfo_U info = (EventInfo_U)ei;

        int consId = (int)info[0];
        bool status = (bool)info[1];
        int slot = (int)info[2];

        //Debug.LogErrorFormat("ChangeConsumableInventoryState. {0}", consId);

        SetContentToInventory(consId, status, slot);
    }

    protected override void SendEventToApplyChanges(int contentId, bool state, int slot)
    {
        Dispatcher.Send(EventId.ConsumableInventoryStateChanged, new EventInfo_U(contentId, state, slot));
    }

    private void OnConsumableBought(EventId id, EventInfo ei)
    {
        EventInfo_I info = (EventInfo_I)ei;
        int consId = info.int1;

        List<IInventoryItem> installedItems = GetFactoryItemsWithContentId(consId);
        if (installedItems.Count == 0 && HasEmptyCell())
            SetContentToInventory(consId, true, -1);
    }

    private void OnWentToBattle(EventId id, EventInfo ei)
    {
        inventoryList.Clear();
        for (int i = 0; i < factory.Items.Count; i++)
            inventoryList.Add( ((IInventoryItem)factory.Items[i]).ContentId);
    }
}
