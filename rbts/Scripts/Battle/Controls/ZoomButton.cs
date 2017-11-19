using System;
using UnityEngine;

public class ZoomButton : MonoBehaviour
{
    [SerializeField] private tk2dUIItem sprZoom;
    [Header("При переходе в зум режим - деактивируется")]
    [SerializeField] protected ActivatedUpDownButton activationScript;

    public static ZoomButton Instance { get; private set; }

    public tk2dUIItem UIItem { get { return sprZoom; } }

    void Awake()
    {
        Instance = this;
        Messenger.Subscribe(EventId.ZoomStateChanged, OnZoomStateChanged);
        Messenger.Subscribe(EventId.VehicleKilled, OnTankKilled);
        sprZoom.OnDown += ChangeZoomState;
    }

    void Start()
    {
        UpdateButton();//Должно выполниться после BattleCamera.Awake(), поэтому в старте
    }

    void OnDestroy()
    {
        Instance = null;
        Messenger.Unsubscribe(EventId.ZoomStateChanged, OnZoomStateChanged);
        Messenger.Unsubscribe(EventId.VehicleKilled, OnTankKilled);
    }

    private void OnZoomStateChanged(EventId id, EventInfo ei)
    {
        if (activationScript)
            activationScript.Activated = BattleCamera.Instance.IsZoomed;
        UpdateButton();
    }

    private void ChangeZoomState()
    {
        if (!StatTable.OnScreen)
            Messenger.Send(EventId.ChangeZoomState, new EventInfo_SimpleEvent());
    }

    protected virtual void UpdateButton() { }

    protected void OnTankKilled(EventId eid, EventInfo ei)
    {
        EventInfo_II info = (EventInfo_II)ei;

        if (info.int1 != BattleController.MyPlayerId)
            return;
        //if (activationScript)
        //    activationScript.Activated = false;
        UpdateButton();
    }
}
