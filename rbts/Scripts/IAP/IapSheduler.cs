using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Security.Cryptography;
using UnityEngine;

/// <summary>
/// Планировщик верификации платежей
/// </summary>
public class IapSheduler : MonoBehaviour {

    public event Action<IapPayment> onValidateFinishedEvent;

    enum State {
        Idle,
        Busy
    }

    State currentState {
        get {
            return m_state;
        }
        set {
            m_lastStateChangeTime = Time.realtimeSinceStartup;
            m_state = value;
        }
    }

    State m_state;
    float m_lastStateChangeTime;

    private IapBillValidator    m_parent;
    private Queue<IapPayment>   m_queue;
    private IapPayment          m_payment;
    
    public static IapSheduler Create (GameObject go, IapBillValidator parent) {
        IapSheduler shed = go.AddComponent<IapSheduler>() as IapSheduler;
        shed.m_parent = parent;
        return shed;
    }

    void Awake () {
        m_queue = new Queue<IapPayment> ();
    }

    void Start () {
        currentState = State.Idle;
        m_lastStateChangeTime -= m_parent.pauseBetweenRequests;
    }

    /// <summary>
    /// Запланировать проверку платежа
    /// </summary>
    /// <param name="payment">Payment.</param>
    public void sheduleToVerify (IapPayment payment) {
        m_queue.Enqueue (payment);
    }

    void Update () {
        if (currentState == State.Busy) {
            return;
        }
        float delta = Time.realtimeSinceStartup - m_lastStateChangeTime;
        if (delta < m_parent.pauseBetweenRequests) {
            return;
        }

        if (m_queue.Count > 0) {
            currentState = State.Busy;
            m_payment = m_queue.Dequeue();
            prepapreRequest ();
            return;
        }
    }

    void prepapreRequest () {
        if (isExpired (m_payment)) {
            Debug.Log ("Validation of payment "+m_payment.product.definition.id+" expired!");
            m_payment.validateState = IapPayment.ValidateState.Declined;
            if (null != onValidateFinishedEvent) {
                onValidateFinishedEvent (m_payment);
            }
            m_payment = null;
            currentState = State.Idle;
            return;
        }
        var req = Http.Manager.Instance ().CreateRequest ("/billing/" + m_parent.platform + "/");
        req.Form.AddField ("itemId", m_payment.product.definition.storeSpecificId);
        req.Form.AddField ("itemName", m_payment.product.metadata.localizedTitle);
        req.Form.AddField ("receipt", m_payment.receipt);
        Http.Manager.StartAsyncRequest (req, requestFinished, requestFailed);
    }

    bool isExpired (IapPayment payment) {
        DateTime expired = payment.created.AddHours (m_parent.validationTimeout);
        return DateTime.Now > expired;
    }

    void requestFinished (Http.Response result)
    {
        var prefs = new JsonPrefs (result.Data);
        var paymentStatus = prefs.ValueInt ("payment", 0);
        if (paymentStatus == 1) {
            m_payment.validateState = IapPayment.ValidateState.Approved;
            if (null != onValidateFinishedEvent) {
                Debug.Log ("InApp purchase successfully validated!");
                onValidateFinishedEvent (m_payment);
            }
        }
        else {
            Debug.LogWarning ("Check bill result false!");
            m_payment.validateState = IapPayment.ValidateState.Declined;
            if (null != onValidateFinishedEvent) {
                onValidateFinishedEvent (m_payment);
            }
        }

        m_state = State.Idle;
        m_payment = null;
    }

    private void requestFailed (Http.Response result)
    {
        m_state = State.Idle;
        m_queue.Enqueue (m_payment);
        m_payment = null;
    }
}

