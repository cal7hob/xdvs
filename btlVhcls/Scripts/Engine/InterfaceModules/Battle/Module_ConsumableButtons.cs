using UnityEngine;
using System.Collections.Generic;
using CodeStage.AntiCheat.ObscuredTypes;

public class Module_ConsumableButtons : InterfaceModuleBase
{
    [SerializeField] private List<BattleConsumableButton> buttonsForTouchInterface;
    [SerializeField] private List<BattleConsumableButton> buttonsForPCInterface;
    [SerializeField] private List<BattleConsumableButton> superWeaponsButtonsForTouchInterface;
    [SerializeField] private List<BattleConsumableButton> superWeaponsButtonsForPCInterface;
    [SerializeField] private GameObject[] objectsForTouchInterface;
    [SerializeField] private GameObject[] objectsForPCInterface;

    private List<BattleConsumableButton> Buttons { get { return BattleGUI.IsTargetPlatformForShowingJoysticks ? buttonsForTouchInterface : buttonsForPCInterface; } }
    private List<BattleConsumableButton> SuperWeaponsButtons { get { return BattleGUI.IsTargetPlatformForShowingJoysticks ? superWeaponsButtonsForTouchInterface : superWeaponsButtonsForPCInterface; } }

    protected override void Awake()
    {
        if (!GameData.isConsumableEnabled)
        {
            gameObject.SetActive(false);
            return;
        }

        base.Awake();

        Dispatcher.Subscribe(EventId.MainTankAppeared, OnMainTankAppeared);
        Dispatcher.Subscribe(EventId.BattleEnd, OnBattleEnd);
        MiscTools.SetObjectsActivity(BattleGUI.IsTargetPlatformForShowingJoysticks, objectsForTouchInterface);
        MiscTools.SetObjectsActivity(!BattleGUI.IsTargetPlatformForShowingJoysticks, objectsForPCInterface);
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
        Dispatcher.Unsubscribe(EventId.MainTankAppeared, OnMainTankAppeared);
        Dispatcher.Unsubscribe(EventId.BattleEnd, OnBattleEnd);
    }

    private void OnMainTankAppeared(EventId id, EventInfo info)
    {
        if (!ProfileInfo.IsBattleTutorialCompleted)//В туторе кнопки расходки налазили бы на кнопку Прервать тутор
            return;

        //Если все ни одну расходку (без учета супер оружия) в бой не взяли - выключаем все 3 кнопки расходки.
        if (ConsumablesInventoryPanel.GetBattleInventoryDic().Count == 0)
            for (int i = 0; i < GameData.consumablesInventoryCapacity; i++)
                Buttons[i].gameObject.SetActive(false);

        //Если в бой не взяли супер оружие - выключаем кнопку
        if (SuperWeaponsButtons != null && SuperWeaponsButtons.Count > 0)//Если в игре есть супероружие
        {
            if (SuperWeaponsInventoryPanel.GetSuperWeaponsInventoryDic().Count == 0)
                for (int i = 0; i < GameData.consumablesSuperWeaponInventoryCapacity; i++)
                    SuperWeaponsButtons[i].gameObject.SetActive(false);
        }

        SetActive(true);

        for (int i = 0; i < GameData.consumablesInventoryCapacity; i++)
            if(Buttons[i].gameObject.activeSelf)
                Buttons[i].Init(i < ConsumablesInventoryPanel.inventoryList.Count ? ConsumablesInventoryPanel.inventoryList[i] : -1, i);
        if(SuperWeaponsButtons != null && SuperWeaponsButtons.Count > 0)//Если в игре есть супероружие
        {
            for (int i = 0; i < GameData.consumablesSuperWeaponInventoryCapacity; i++)
                if (SuperWeaponsButtons[i].gameObject.activeSelf)
                    SuperWeaponsButtons[i].Init(i < SuperWeaponsInventoryPanel.inventoryList.Count ? SuperWeaponsInventoryPanel.inventoryList[i] : -1, GameData.consumablesInventoryCapacity + i);
        }
        
    }

    private void OnBattleEnd(EventId id, EventInfo info)
    {
        SetActive(false);
    }
}
