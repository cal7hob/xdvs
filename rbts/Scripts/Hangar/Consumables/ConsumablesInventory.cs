using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using CodeStage.AntiCheat.ObscuredTypes;

public class ConsumablesInventory : MonoBehaviour
{
    public const int CAPACITY = 3;
    public const int EMPTY_CELL_ID = -1;
    public ConsumablesInventoryItem[] inventoryItems;

    public static List<int> battleInventoryList = new List<int>(CAPACITY);

    public void Reset()
    {
        battleInventoryList = new List<int>(CAPACITY);
        for (int i = 0; i < CAPACITY; i++)
        {
            battleInventoryList.Add(EMPTY_CELL_ID);
            inventoryItems[i].Id = EMPTY_CELL_ID;
        }
    }

    public bool Add(int id, int slot = -1)
    {
        if (battleInventoryList.Contains(id))
            return false;
        if (!HasEmptyCell())
            return false;

        for(int i = 0; i < CAPACITY; i++)
            if(battleInventoryList[i] == -1 && (slot == -1 || (slot >= 0 && slot == i)))
            {
                inventoryItems[i].Id = battleInventoryList[i] = id;
                return true;
            }

        return false;
    }

    public void Remove(int id)
    {
        if (!battleInventoryList.Contains(id))
            return;
        for (int i = 0; i < CAPACITY; i++)
            if (battleInventoryList[i] == id)
            {
                inventoryItems[i].Id = battleInventoryList[i] = EMPTY_CELL_ID;
                return;
            }
    }

    public bool HasEmptyCell()
    {
        return battleInventoryList.Contains(EMPTY_CELL_ID);
    }

    /// <summary>
    /// Если находим ячейку, заполненную расходкой с ид==id - возвращаем ее.
    /// </summary>
    public ConsumablesInventoryItem GetInventoryCellByConsumableId(int id)
    {
        for (int i = 0; i < inventoryItems.Length; i++)
            if (inventoryItems[i].Id == id)
                return inventoryItems[i];
        return null;
    }

    public bool HasConsumable(int id)
    {
        ConsumablesInventoryItem item = GetInventoryCellByConsumableId(id);
        return item != null;
    }

     public int EmptyCellsCount
    {
        get
        {
            int count = 0;
            for (int i = 0; i < CAPACITY; i++)
                if (inventoryItems[i].IsEmpty)
                    count++;
            return count;
        }
    }

    public static Dictionary<int, ObscuredInt> GetBattleInventoryDic()
    {
        Dictionary<int, ObscuredInt> dic = new Dictionary<int, ObscuredInt>();
        for (int i = 0; i < battleInventoryList.Count; i++)
        {
            if (battleInventoryList[i] >= 0 && ProfileInfo.consumableInventory.ContainsKey(battleInventoryList[i]))
            {
                dic[battleInventoryList[i]] =
                    Mathf.Clamp(
                        value: ProfileInfo.consumableInventory[battleInventoryList[i]],
                        min: 0,
                        max: GameData.consumableInfos[battleInventoryList[i]].maxInBattle);
            }
        }

        return dic;
    }

    public static List<int> DefaultInventoryList
    {
        get
        {
            List<int> list = new List<int>();
            for (int i = 0; i < CAPACITY; i++)
                list.Add(EMPTY_CELL_ID);
            return list;
        }
    }
}
