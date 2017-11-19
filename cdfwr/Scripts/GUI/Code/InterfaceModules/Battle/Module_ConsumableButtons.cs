using UnityEngine;
using System.Collections.Generic;

public class Module_ConsumableButtons : InterfaceModuleBase
{
    public List<BattleConsumableButton> buttons;
    protected override void Awake()
    {
        base.Awake();
        Dispatcher.Subscribe(EventId.MainTankAppeared, OnMainTankAppeared);
        //Dispatcher.Subscribe(EventId.OnStatTableChangeVisibility, OnStatTableChangeVisibility);

    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
        Dispatcher.Unsubscribe(EventId.MainTankAppeared, OnMainTankAppeared);
        //Dispatcher.Unsubscribe(EventId.OnStatTableChangeVisibility, OnStatTableChangeVisibility);
    }

    private void OnMainTankAppeared(EventId id, EventInfo info)
    {
        SetActive(true);
        int emptyCount = 0;
        for (int i = 0; i < ConsumableInventory.CAPACITY; i++)
        {
            buttons[i].Init(i < ConsumableInventory.battleInventoryList.Count ? ConsumableInventory.battleInventoryList[i] : -1);
            if (buttons[i].IsEmpty)
            {
                emptyCount++;
            }
            if (emptyCount == ConsumableInventory.CAPACITY)
            {
                SetActive(false);
            }
        }
    }

    void Update()
    {
        if (XDevs.Input.GetButton("UseConsumable1"))
        {
            if (!buttons[0].gameObject.activeInHierarchy)
            {
                return;
            }
            buttons[0].GetComponent<tk2dUIItem>().SimulateClick();
        }
        if (XDevs.Input.GetButton("UseConsumable2"))
        {
            if (!buttons[1].gameObject.activeInHierarchy)
            {
                return;
            }
            buttons[1].GetComponent<tk2dUIItem>().SimulateClick();
        }
        if (XDevs.Input.GetButton("UseConsumable3"))
        {
            if (!buttons[2].gameObject.activeInHierarchy)
            {
                return;
            }
            buttons[2].GetComponent<tk2dUIItem>().SimulateClick();
        }
    }
}
