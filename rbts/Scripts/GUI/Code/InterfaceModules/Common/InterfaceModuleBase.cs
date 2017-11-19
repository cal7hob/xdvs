using System;
using UnityEngine;
using System.Collections.Generic;

public interface IInterfaceModule
{
    void SetActive(bool en);
}

public class InterfaceModuleBase : MonoBehaviour, IInterfaceModule
{
    public StateEventSender wrapper;
    public bool initialState = false;
    public bool setInitialStateOnAwake = true;
    public bool setInitialStateOnStart = false;
    public List<EventId> eventsToHide = new List<EventId>();
    public List<EventId> eventsToShow = new List<EventId>();

    /// <summary>
    /// При перезаписи - вызывать base.Awake() или не забыть добавить ниже следующее...
    /// </summary>
    protected virtual void Awake()
    {
        if (setInitialStateOnAwake)
            SetActive(initialState);

        wrapper.StateChanged += OnWrapperStateChanged;

        #region Подписка на события из паблик списка с устранением дубликатов
        List<EventId> uniqEvents = new List<EventId>();
        for(int i = 0; i < eventsToShow.Count; i++)
        {
            if(!uniqEvents.Contains(eventsToShow[i]))
            {
                Messenger.Subscribe(eventsToShow[i], Show);
                uniqEvents.Add(eventsToShow[i]);
            }
        }
        uniqEvents.Clear();
        for (int i = 0; i < eventsToHide.Count; i++)
        {
            if (!uniqEvents.Contains(eventsToHide[i]))
            {
                Messenger.Subscribe(eventsToHide[i], Hide);
                uniqEvents.Add(eventsToHide[i]);
            }
        }
        #endregion
    }

    /// <summary>
    /// При перезаписи - вызывать base.OnDestroy() или не забыть добавить ниже следующее...
    /// </summary>
    protected virtual void OnDestroy()
    {
        wrapper.StateChanged -= OnWrapperStateChanged;

        #region Отписка от событий из паблик списка с устранением дубликатов
        for (int i = 0; i < eventsToShow.Count; i++)
            Messenger.Unsubscribe(eventsToShow[i], Show);

        for (int i = 0; i < eventsToHide.Count; i++)
            Messenger.Unsubscribe(eventsToHide[i], Hide);
        #endregion
    }

    protected virtual void Start()
    {
        if (setInitialStateOnStart)
            SetActive(initialState);
    }

    public void SetActive(bool en)
    {
        wrapper.gameObject.SetActive(en);
        AfterStateChange();
    }

    public void Show(EventId id = 0, EventInfo info = null)
    {
        SetActive(true);
    }

    public void Hide(EventId id = 0, EventInfo info = null)
    {
        SetActive(false);
    }

    public void Toggle()
    {
        if (wrapper.gameObject.activeSelf)
            Hide();
        else
            Show();
    }

    //Чтобы не обрабатывать нежелательные OnEnable, а только когда делаем SetActive()
    public virtual void AfterStateChange() { }

    public bool IsActive { get { return wrapper.gameObject.activeSelf; } }

    /// <summary>
    /// OnEnable и OnDisable у  wrapper
    /// </summary>
    protected virtual void OnWrapperStateChanged(StateEventSender sender, bool en) { }
}
