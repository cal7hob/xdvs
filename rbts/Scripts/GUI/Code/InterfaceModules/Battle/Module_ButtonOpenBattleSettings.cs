

public class Module_ButtonOpenBattleSettings : InterfaceModuleBase
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
        SetActive(true);
    }

    private void OnStatTableChangeVisibility(EventId id, EventInfo info)
    {
        SetActive(!((EventInfo_B)info).bool1);
    }
}
