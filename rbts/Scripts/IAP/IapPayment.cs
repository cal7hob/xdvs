using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Purchasing;

/// <summary>
/// Класс для хранения состояния и процесса проверки платежа
/// </summary>
public class IapPayment {

    public enum State {
        New,
        Sheduled,
        Verifying,
        Finishing,
        Done
    };

    public State currentState {
        get { return m_state; }
        set { m_state = value; }
    }

    public enum ValidateState {
        Unknown,
        Approved,
        Declined
    }
    public ValidateState validateState {
        get {return m_validateState;}
        set {m_validateState = value;}
    }

    public bool isValidated {
        get {return validateState == ValidateState.Approved;}
    }
    
    public Product product { get { return m_product; } }
    public string receipt {get {return m_receipt;}}
    public DateTime created {
        get {return m_created;}
    }

    private State           m_state         = State.New;
    private ValidateState   m_validateState = ValidateState.Unknown;
    private string          m_receipt;
    private DateTime        m_created;
    private DateTime        m_verified;
    private Product         m_product;
    
    public IapPayment(Product purchasedProduct)
    {
        m_product = purchasedProduct;
        m_receipt = purchasedProduct.receipt;
        m_created = DateTime.Now;
    }
}

