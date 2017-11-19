

public class Module_ButtonOpenBattleSettings : InterfaceModuleBase
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
        SetActive(true);
    }

    private void OnStatTableChangeVisibility(EventId id, EventInfo info)
    {
        SetActive(!((EventInfo_B)info).bool1);
    }
}
