using UnityEngine;
using System.Collections.Generic;

public class Module_ConsumableButtons : InterfaceModuleBase
{
    [SerializeField] private List<BattleConsumableButton> buttonsForTouchInterface;
    [SerializeField] private List<BattleConsumableButton> buttonsForPCInterface;
    [SerializeField] private GameObject[] objectsForTouchInterface;
    [SerializeField] private GameObject[] objectsForPCInterface;

    private List<BattleConsumableButton> Buttons { get { return BattleGUI.IsTargetPlatformForShowingJoysticks ? buttonsForTouchInterface : buttonsForPCInterface; } }

    protected override void Awake()
    {
        if (!GameData.isConsumableEnabled)
        {
            gameObject.SetActive(false);
            return;
        }

        base.Awake();

        Messenger.Subscribe(EventId.MainTankAppeared, OnMainTankAppeared);
        Messenger.Subscribe(EventId.BattleEnd, OnBattleEnd);
        MiscTools.SetObjectsActivity(BattleGUI.IsTargetPlatformForShowingJoysticks, objectsForTouchInterface);
        MiscTools.SetObjectsActivity(!BattleGUI.IsTargetPlatformForShowingJoysticks, objectsForPCInterface);
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
        Messenger.Unsubscribe(EventId.MainTankAppeared, OnMainTankAppeared);
        Messenger.Unsubscribe(EventId.BattleEnd, OnBattleEnd);
    }

    private void OnMainTankAppeared(EventId id, EventInfo info)
    {
        if (!ProfileInfo.IsBattleTutorialCompleted || ConsumablesInventory.GetBattleInventoryDic().Count == 0)//В туторе кнопки расходки налазили бы на кнопку Прервать тутор
            return;

        SetActive(true);
        for (int i = 0; i < ConsumablesInventory.CAPACITY; i++)
            Buttons[i].Init(i < ConsumablesInventory.battleInventoryList.Count ? ConsumablesInventory.battleInventoryList[i] : -1, i);
    }

    private void OnBattleEnd(EventId id, EventInfo info)
    {
        SetActive(false);
    }
}
