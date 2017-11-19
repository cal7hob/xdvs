

public class Module_ButtonOpenBattleStats : InterfaceModuleBase
{
    protected override void Awake()
    {
        base.Awake();
        Dispatcher.Subscribe(EventId.MainTankAppeared, OnMainTankAppeared);
        Dispatcher.Subscribe(EventId.OnStatTableChangeVisibility, OnStatTableChangeVisibility);
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
        Dispatcher.Unsubscribe(EventId.MainTankAppeared, OnMainTankAppeared);
        Dispatcher.Unsubscribe(EventId.OnStatTableChangeVisibility, OnStatTableChangeVisibility);
    }

    private void OnMainTankAppeared(EventId id, EventInfo info)
    {
#if UNITY_IOS
        SetActive(true);
#endif
    }

    private void OnBtnOpenStatisticksInBattle(tk2dUIItem btn)
    {
        if (BattleController.Instance)
            BattleController.Instance.ShowStatTable(btn);
    }

    private void OnStatTableChangeVisibility(EventId id, EventInfo info)
    {
#if UNITY_IOS
        SetActive(!((EventInfo_B)info).bool1);
#endif
    }
}
