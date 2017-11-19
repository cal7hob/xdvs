using UnityEngine;

public class ZoomScreen : InterfaceModuleBase
{
    protected override void Awake ()
    {
        base.Awake();
        HideScreen();
        Messenger.Subscribe(EventId.ZoomStateChanged, OnZoomStateChanged);
        Messenger.Subscribe(EventId.VehicleKilled, OnTankKilled);
        Messenger.Subscribe(EventId.StatTableVisibilityChange, OnStatTableChangeVisibility);
        Messenger.Subscribe(EventId.BattleEnd, OnBattleEnd);
        //Dispatcher.Subscribe(EventId.BeforeReconnecting, HideScreen);
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
        Messenger.Unsubscribe(EventId.ZoomStateChanged, OnZoomStateChanged);
        Messenger.Unsubscribe(EventId.VehicleKilled, OnTankKilled);
        Messenger.Unsubscribe(EventId.StatTableVisibilityChange, OnStatTableChangeVisibility);
        Messenger.Unsubscribe(EventId.BattleEnd, OnBattleEnd);
        //Dispatcher.Unsubscribe(EventId.BeforeReconnecting, HideScreen);
    }

    private void OnZoomStateChanged(EventId id, EventInfo ei)
    {
        if(BattleCamera.Instance != null && BattleController.MyVehicle != null)
            SetActive(BattleCamera.Instance.IsZoomed);
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
            Messenger.Send(EventId.ChangeZoomState, new EventInfo_SimpleEvent());
        }
            
    }

    private void OnBattleEnd(EventId id, EventInfo info)
    {
        HideScreen();
    }
}
