using System;
using System.Collections.Generic;
using UnityEngine;
using System.Collections;

public class CallbackProcessor : MonoBehaviour
{
    private static CallbackProcessor instance;
    void Awake()
    {
        instance = this;
    }
    public static void QueueCallback(Action action)
    {
        lock (instance.m_queueLock)
        {
            instance.m_queuedCallbacks.Add(action);
        }
    }

    void Update()
    {
        MoveQueuedEventsToExecuting();

        while (m_executingCallbacks.Count > 0)
        {
            Action e = m_executingCallbacks[0];
            m_executingCallbacks.RemoveAt(0);
            e();
        }
    }

    private void MoveQueuedEventsToExecuting()
    {
        lock (m_queueLock)
        {
            while (m_queuedCallbacks.Count > 0)
            {
                Action e = m_queuedCallbacks[0];
                m_executingCallbacks.Add(e);
                m_queuedCallbacks.RemoveAt(0);
            }
        }
    }

    private System.Object m_queueLock = new System.Object();
    private List<Action> m_queuedCallbacks = new List<Action>();
    private List<Action> m_executingCallbacks = new List<Action>();
}
