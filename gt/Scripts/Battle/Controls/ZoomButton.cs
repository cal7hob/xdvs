using System;
using UnityEngine;

public class ZoomButton : AbstractClassForButtons
{
    public tk2dUIItem sprZoom;
    [Header("При переходе в зум режим - меняет цвет")]
    [SerializeField]
    private tk2dBaseSprite zoomSprite;
    [SerializeField]
    private Color InZoomColor = new Color(1,1,1,0.5f);
    [SerializeField]
    private Color NonZoomColor = new Color(1,1,1,1);

    public static ZoomButton Instance { get; private set; }

    public tk2dUIItem UIItem { get { return sprZoom; } }

    void Awake()
    {
        Instance = this;
        Dispatcher.Subscribe(EventId.ZoomStateChanged, OnZoomStateChanged);
        Dispatcher.Subscribe(EventId.TankKilled, OnTankKilled);
        sprZoom.OnDown += ChangeZoomState;
    }

    void Start()
    {
        zoomSprite.color = NonZoomColor;
        UpdateButton();//Должно выполниться после BattleCamera.Awake(), поэтому в старте
    }

    void OnDestroy()
    {
        Instance = null;
        Dispatcher.Unsubscribe(EventId.ZoomStateChanged, OnZoomStateChanged);
        Dispatcher.Unsubscribe(EventId.TankKilled, OnTankKilled);
        sprZoom.OnDown -= ChangeZoomState;
    }

    private void OnZoomStateChanged(EventId id, EventInfo ei)
    {
        var info = ei as EventInfo_B;
        var isZoomedIn = info.bool1;

        zoomSprite.color = isZoomedIn ? InZoomColor : NonZoomColor;
        UpdateButton();
    }

    private static void ChangeZoomState()
    {
        if (StatTable.OnScreen)
        {
            return;
        }

        if (BattleCamera.Instance.IsZoomed)
        {
            BattleCamera.Instance.ZoomOut();

        }
        else
        {
            BattleCamera.Instance.ZoomIn();
        }
    }

    protected virtual void UpdateButton() { }

    protected void OnTankKilled(EventId eid, EventInfo ei)
    {
        EventInfo_II info = (EventInfo_II)ei;

        if (info.int1 != BattleController.MyPlayerId)
            return;

        UpdateButton();
    }

    public override Rect Coord()
    {
        var sprite = sprZoom.GetComponent<tk2dSprite>();
        var joyWorldTopRight = sprite.transform.TransformPoint(sprite.GetBounds().max);
        var joyScreenTopRight = BattleGUI.Instance.GuiCamera.WorldToScreenPoint(joyWorldTopRight);
        var joyWorldBottomLeft = sprite.transform.TransformPoint(sprite.GetBounds().min);
        var joyScreenBottomLeft = BattleGUI.Instance.GuiCamera.WorldToScreenPoint(joyWorldBottomLeft);

        var Area = new Rect
        {
            xMin = joyScreenBottomLeft.x,
            yMin = joyScreenBottomLeft.y,
            xMax = joyScreenTopRight.x,
            yMax = joyScreenTopRight.y,
        };
        return Area;
    }
}
