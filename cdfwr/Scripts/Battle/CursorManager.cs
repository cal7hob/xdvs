using Rewired;
using UnityEngine;

public class CursorManager : MonoBehaviour
{
    public static bool UnlockButtonIsDown { get { return XDevs.Input.GetButton("UnlockMouse"); } }

    public static bool IsGUIOnScreen { get; private set; }
    public static bool MouseLeftBtnDown { get { return Input.GetMouseButtonDown(0); } }

    void Awake()
    {
        Dispatcher.Subscribe(EventId.OnBattleChatCommandsChangeVisibility, OnGUIToggle);
        Dispatcher.Subscribe(EventId.MessageBoxChangeVisibility, OnGUIToggle);
        Dispatcher.Subscribe(EventId.OnBattleSettingsChangeVisibility, OnGUIToggle);
        Dispatcher.Subscribe(EventId.OnStatTableChangeVisibility, OnGUIToggle);

        Dispatcher.Subscribe(EventId.MainTankAppeared, OnMainTankAppeared);
    }

    void OnDestroy()
    {
        Dispatcher.Unsubscribe(EventId.OnBattleChatCommandsChangeVisibility, OnGUIToggle);
        Dispatcher.Unsubscribe(EventId.MessageBoxChangeVisibility, OnGUIToggle);
        Dispatcher.Unsubscribe(EventId.OnBattleSettingsChangeVisibility, OnGUIToggle);
        Dispatcher.Unsubscribe(EventId.OnStatTableChangeVisibility, OnGUIToggle);

        Dispatcher.Unsubscribe(EventId.MainTankAppeared, OnMainTankAppeared);

        ReInput.InputSourceUpdateEvent -= RewiredInputUpdateHandler;
        IsGUIOnScreen = false;
    }

    private void OnMainTankAppeared(EventId id, EventInfo info)
    {
        if (!BattleCamera.Instance.IsMouseControlled)
        {
            Cursor.lockState = CursorLockMode.None;
            Destroy(this);
        }

        ReInput.InputSourceUpdateEvent += RewiredInputUpdateHandler;
    }

    private static void OnGUIToggle(EventId id, EventInfo info)
    {
        var ei = info as EventInfo_B;
        IsGUIOnScreen = ei.bool1;

        Cursor.lockState = IsGUIOnScreen ? CursorLockMode.None : 
            UnlockButtonIsDown ? CursorLockMode.None : CursorLockMode.Locked;
    }

    private static void RewiredInputUpdateHandler()
    {
        if (MouseLeftBtnDown && !UnlockButtonIsDown && !IsGUIOnScreen && Cursor.lockState != CursorLockMode.Locked)
        {
            Cursor.lockState = CursorLockMode.Locked;
        }

        if (UnlockButtonIsDown)
        {
            Cursor.lockState = CursorLockMode.None;
        }

        if (XDevs.Input.GetButtonUp("UnlockMouse") && !IsGUIOnScreen)
        {
            Cursor.lockState = CursorLockMode.Locked;
        }
    }
}
