using System;
using UnityEngine;

public class Fullscreen : AbstractClassForButtons
{
    public LabelLocalizationAgent fullscreenLabelAgent;
    void Awake()
    {
#if !(UNITY_WEBGL || UNITY_WEBPLAYER)
        gameObject.SetActive(false);
#else
        Dispatcher.Subscribe(EventId.WindowModeChanged, WindowModeChanged);
#endif
    }

    private void OnDestroy()
    {
        Dispatcher.Unsubscribe(EventId.WindowModeChanged, WindowModeChanged);
    }

    private void ChangeState()
    {
        if (Screen.fullScreen)
        {
            GoWindow();
        }
        else
        {
            GoFullscreen();
        }

        Dispatcher.Send(EventId.BattleBtnPressed, new EventInfo_SimpleEvent());
    }

    private void GoFullscreen()
    {
        Screen.SetResolution(Screen.currentResolution.width, Screen.currentResolution.height, true);
    }

    private void GoWindow()
    {
        Screen.SetResolution(960, 600, false);
    }

    private void WindowModeChanged(EventId id, EventInfo info)
    {
        if (fullscreenLabelAgent != null)
        {
            fullscreenLabelAgent.key = Screen.fullScreen ? "lblWindow" : "lblFullscreen";
            fullscreenLabelAgent.LocalizeLabel();
        }
    }

    public override Rect Coord()
    {
        tk2dBaseSprite sprite;
        if (GetComponentInChildren<tk2dSprite>() == null)
        {
            sprite = GetComponentInChildren<tk2dSlicedSprite>();
        }
        else
        {
            sprite = GetComponentInChildren<tk2dSprite>();
        }
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

