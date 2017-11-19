using UnityEngine;

public class ZoomScreen : InterfaceModuleBase
{
    protected override void Awake ()
    {
        base.Awake();
        HideScreen();
        Dispatcher.Subscribe(EventId.ZoomStateChanged, OnZoomStateChanged);
        Dispatcher.Subscribe(EventId.TankKilled, OnTankKilled);
        //Dispatcher.Subscribe(EventId.BeforeReconnecting, HideScreen);
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
        Dispatcher.Unsubscribe(EventId.ZoomStateChanged, OnZoomStateChanged);
        Dispatcher.Unsubscribe(EventId.TankKilled, OnTankKilled);
        //Dispatcher.Unsubscribe(EventId.BeforeReconnecting, HideScreen);
    }

    private void OnZoomStateChanged(EventId id, EventInfo ei)
    {
        var info = ei as EventInfo_B;
        var isZoomIn = info.bool1;
        wrapper.gameObject.SetActive(isZoomIn);

    }

    protected void OnTankKilled(EventId eid, EventInfo ei)
    {
        EventInfo_II info = (EventInfo_II)ei;

        if (info.int1 != BattleController.MyPlayerId)
            return;

        HideScreen();
    }

    private void HideScreen(EventId eid = 0, EventInfo ei = null)
    {
        wrapper.gameObject.SetActive(false);
    }
}
