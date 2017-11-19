using System;
using UnityEngine;
using System.Collections.Generic;
using XDevs.ButtonsPanel;


public interface IAligner
{
    void Align();
    void AddItem(Renderer r, float paddingBefore, float paddingAfter, int position);
    List<GameObject> GetItemsList();
    bool IsVertical();
    void Clear();
}

public interface IAlignerItem
{
    GameObject GetGameObject();
}

/// <summary>
/// Класс для удобства накидывания ссылок на интерфейс IAligner (т.к. в Юнити нельзя сериализовать переменную типа Интерфейс), чтобы избежать во многих местах приведения MonoBehaviour к IAligner
/// </summary>
public abstract class UniAlignerBase : MonoBehaviour, IAligner
{
    public abstract void Align();
    public abstract void AddItem(Renderer r, float paddingBefore, float paddingAfter, int position = -1);
    public abstract void AddItem(tk2dUILayout l, float paddingBefore, float paddingAfter, int position = -1);
    public abstract List<GameObject> GetItemsList();
    public abstract bool IsVertical();
    public abstract void Clear();

    protected List<StateEventSender> stateEventSenders = new List<StateEventSender>();//Список для хранения ссылок на кнопки, для автоматического выравнивания при изменении их статуса

    public event Action<StateEventSender, bool> alignItemStateChanged;

    public virtual void OnEnable()
    {
        Align();
    }

    public virtual void OnButtonStateChanged(StateEventSender btn, bool state)
    {
        alignItemStateChanged.SafeInvoke(btn, state);
        Align();
    }

    public virtual void Start()
    {
        foreach (var obj in GetItemsList())
        {
            if (obj != null)
            {
                AddPanelButtonSubscription(obj);
                tk2dTextMesh label = obj.GetComponent<tk2dTextMesh>();
                if (label)
                {
                    label.OnTextChange += Align;
                    label.OnFontChange += Align;
                    label.OnScaleChange += Align;
                }
            }
        }

        Align();
    }

    protected virtual void OnDestroy ()
    {
        foreach (var obj in GetItemsList())
        {
            if (obj != null)
            {
                RemovePanelButtonSubscription(obj);
                tk2dTextMesh label = obj.GetComponent<tk2dTextMesh>();
                if (label)
                    label.OnTextChange -= Align;
            }
        }
    }

    protected void AddPanelButtonSubscription(GameObject btn)
    {
        StateEventSender stateEventSenderScript = btn.GetComponent<StateEventSender>();
        if (!stateEventSenderScript) {
            stateEventSenderScript = btn.AddComponent<StateEventSender> ();
        }
        if (stateEventSenderScript && !stateEventSenders.Contains(stateEventSenderScript))
        {
            stateEventSenders.Add(stateEventSenderScript);
            stateEventSenderScript.StateChanged += OnButtonStateChanged;
        }
    }

    protected void RemovePanelButtonSubscription(GameObject btn)
    {
        StateEventSender stateEventSenderScript = btn.GetComponent<StateEventSender>();
        if (stateEventSenderScript && stateEventSenders.Contains(stateEventSenderScript))
        {
            stateEventSenders.Remove(stateEventSenderScript);
            stateEventSenderScript.StateChanged -= OnButtonStateChanged;
        }
    }
}
