using System;
using UnityEngine;

public class Fullscreen : MonoBehaviour
{
    [SerializeField] private LabelLocalizationAgent fullscreenLabelAgent;

    void Awake()
    {
#if !(UNITY_WEBGL || UNITY_WEBPLAYER)
        gameObject.SetActive(false);
#else
        Messenger.Subscribe(EventId.WindowModeChanged, WindowModeChanged);
#endif
    }

    private void OnDestroy()
    {
        Messenger.Unsubscribe(EventId.WindowModeChanged, WindowModeChanged);
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
}

