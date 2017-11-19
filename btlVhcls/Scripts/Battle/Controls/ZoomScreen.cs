using UnityEngine;

public class ZoomScreen : InterfaceModuleBase
{
    protected override void Awake ()
    {
        base.Awake();
        HideScreen();
        Dispatcher.Subscribe(EventId.ZoomStateChanged, OnZoomStateChanged);
        Dispatcher.Subscribe(EventId.TankKilled, OnTankKilled);
        Dispatcher.Subscribe(EventId.OnStatTableChangeVisibility, OnStatTableChangeVisibility);
        Dispatcher.Subscribe(EventId.BattleEnd, OnBattleEnd);
        //Dispatcher.Subscribe(EventId.BeforeReconnecting, HideScreen);
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
        Dispatcher.Unsubscribe(EventId.ZoomStateChanged, OnZoomStateChanged);
        Dispatcher.Unsubscribe(EventId.TankKilled, OnTankKilled);
        Dispatcher.Unsubscribe(EventId.OnStatTableChangeVisibility, OnStatTableChangeVisibility);
        Dispatcher.Unsubscribe(EventId.BattleEnd, OnBattleEnd);
        //Dispatcher.Unsubscribe(EventId.BeforeReconnecting, HideScreen);
    }

    private void OnZoomStateChanged(EventId id, EventInfo ei)
    {
        if(BattleCamera.Instance != null && BattleController.MyVehicle != null)
            SetActive(BattleCamera.Instance.IsZoomed);
    }

    protected void OnTankKilled(EventId eid, EventInfo ei)
    {
        EventInfo_III info = (EventInfo_III)ei;

        if (info.int1 != BattleController.MyPlayerId)
            return;

        HideScreen();
    }

    private void HideScreen(EventId eid = 0, EventInfo ei = null)
    {
        SetActive(false);
    }

    /// <summary>
    ////Поидее надо эту функцию поместить в какой нить манагер, чтобы не зумСкрин выключал сам себя при появлении таблицы статы
    /// </summary>
    private void OnStatTableChangeVisibility(EventId id, EventInfo info)
    {
        if(BattleController.BattleAccomplished)
        {
            HideScreen();
        }
        else if( ((EventInfo_B)info).bool1 && BattleCamera.Instance && BattleCamera.Instance.IsZoomed)
        {
            //Debug.LogErrorFormat("OnStatTableChangeVisibility -> ChangeZoomState");
            Dispatcher.Send(EventId.ChangeZoomState, new EventInfo_SimpleEvent());
        }
            
    }

    private void OnBattleEnd(EventId id, EventInfo info)
    {
        HideScreen();
    }
}
