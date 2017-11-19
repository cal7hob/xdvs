

public class Module_ButtonOpenBattleStats : InterfaceModuleBase
{
    protected override void Awake()
    {
        base.Awake();
        Messenger.Subscribe(EventId.MainTankAppeared, OnMainTankAppeared);
        Messenger.Subscribe(EventId.StatTableVisibilityChange, OnStatTableChangeVisibility);
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
        Messenger.Unsubscribe(EventId.MainTankAppeared, OnMainTankAppeared);
        Messenger.Unsubscribe(EventId.StatTableVisibilityChange, OnStatTableChangeVisibility);
    }

    private void OnMainTankAppeared(EventId id, EventInfo info)
    {
#if UNITY_IOS
        SetActive(true);
#endif
    }

    public override void AfterStateChange()
    {
        Messenger.Send(EventId.BtnBackInBattleChangeVisibility, new EventInfo_B(IsActive));
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
