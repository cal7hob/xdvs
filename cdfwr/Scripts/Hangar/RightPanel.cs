using UnityEngine;
using System.Collections.Generic;

public class RightPanel : MonoBehaviour
{
    public ScrollableVerticalPanel rightPanel;
    public static RightPanel Instance { get; private set; }
    private bool state;
    private HashSet<System.Object> panelHiders = new HashSet<System.Object>();

    private void Awake()
    {
        Instance = this;
        Dispatcher.Subscribe(EventId.MessageBoxChangeVisibility, OnMessageBoxChangeVisibility);
        Dispatcher.Subscribe(EventId.ChangeElementStateRequest, OnChangeElementStateRequest);
    }

    private void OnDestroy()
    {
        Dispatcher.Unsubscribe(EventId.MessageBoxChangeVisibility, OnMessageBoxChangeVisibility);
        Dispatcher.Unsubscribe(EventId.ChangeElementStateRequest, OnChangeElementStateRequest);
        Instance = null;
    }

    private void OnMessageBoxChangeVisibility(EventId id, EventInfo info)
    {
        UpdateVisibility();
    }

    private void OnChangeElementStateRequest(EventId id, EventInfo info)
    {
        EventInfo_U eventInfoU = info as EventInfo_U;
        ChangeElementStateRequestInfo stateInfo = (ChangeElementStateRequestInfo)eventInfoU[0];
        if (!stateInfo.ForMe(GetType()))
            return;
        if (!panelHiders.Contains(stateInfo.sender) && !stateInfo.state)//если просят выключить панель и заявки от него еще нету
        {
            panelHiders.Add(stateInfo.sender);
            UpdateVisibility();
        }
        else if (stateInfo.state && panelHiders.Contains(stateInfo.sender))
        {
            panelHiders.Remove(stateInfo.sender);
            UpdateVisibility();
        }
    }

    private void UpdateVisibility()
    {
        state = !(MessageBox.IsShown || panelHiders.Count > 0);

        if (state != gameObject.activeSelf)
            gameObject.SetActive(state);
    }
}
