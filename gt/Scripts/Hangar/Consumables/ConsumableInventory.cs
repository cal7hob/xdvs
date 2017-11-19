using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using CodeStage.AntiCheat.ObscuredTypes;

public class ConsumableInventory : MonoBehaviour
{
    public const int CAPACITY = 3;
    [SerializeField] private ConsumableInventoryItem[] inventoryItems;

    // словарь id слотов боевой расходки, чтобы вызывать UpdateSlot(), когда докупаем расходку.
    public static Dictionary<int, int> battleConsumablesSlotsIds = new Dictionary<int, int>(CAPACITY);

    public static List<int> battleInventoryList = new List<int>(CAPACITY);
    public static ConsumableInventoryItem[] InventoryItems { get { return Instance.inventoryItems; } }
    public static ConsumableInventory Instance { get; private set; }

    void Awake()
    {
        Instance = this;
    }

    void OnDestroy()
    {
        Instance = null;
    }

    public void Reset()
    {
        battleInventoryList = new List<int>(CAPACITY);
        for (int i = 0; i < CAPACITY; i++)
        {
            battleInventoryList.Add(-1);
            inventoryItems[i].Id = -1;
        }
    }

    public bool Add(int id)
    {
        if (battleInventoryList.Contains(id))
            return true;
        if (!HasEmptyCell())
            return false;

        for(int i = 0; i < CAPACITY; i++)
        {
            if(battleInventoryList[i] == -1)
            {
                inventoryItems[i].Id = battleInventoryList[i] = id;
                battleConsumablesSlotsIds[id] = i;
                return true;
            }
        }
        
        return false;//такого уже не может быть
    }

    public bool Remove(int id)
    {
        if (!battleInventoryList.Contains(id))
            return false;
        for (int i = 0; i < CAPACITY; i++)
            if (battleInventoryList[i] == id)
            {
                inventoryItems[i].Id = battleInventoryList[i] = -1;
                return false;
            }

        return false;//такого уже не может быть
    }

    public bool HasEmptyCell()
    {
        return battleInventoryList.Contains(-1);
    }

    public static Dictionary<int, ObscuredInt> GetBattleInventoryDic()
    {
        Dictionary<int, ObscuredInt> dic = new Dictionary<int, ObscuredInt>();
        for (int i = 0; i < battleInventoryList.Count; i++)
            if(battleInventoryList[i] >= 0)
                dic[battleInventoryList[i]] = Mathf.Clamp(ProfileInfo.consumableInventory[battleInventoryList[i]], 0, GameData.consumableInfos[battleInventoryList[i]].maxInBattle);

        return dic;
    }
}
