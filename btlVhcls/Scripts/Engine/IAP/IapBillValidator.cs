using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Security.Cryptography;
using UnityEngine;
using UnityEngine.Purchasing;

public class IapBillValidator : MonoBehaviour {

    public event Action<IapPayment> onPurchaseValidateStartedEvent;
    public event Action<IapPayment> onPurchaseValidatedEvent;
    public event Action<IapPayment> onPurchaseDeclinedEvent;

    public string   userDataPrefix = "iap_";
    public float    pauseBetweenRequests = 5;   // seconds
    public float    validationTimeout = 1f;     // hours

    public string platform {
        get {return m_platform;}
    }

    private string m_platform;
    public static string signatureHeader = "x-bill-signature";
    public static string version = "0.1";

    private IapSheduler m_sheduler;

    void Awake () {
        m_platform = "skip";
#if UNITY_ANDROID && !UNITY_EDITOR
        m_platform = GameData.IsGame(Game.AmazonBuild) ? "amazon" : "android";
#elif (UNITY_IPHONE || UNITY_STANDALONE_OSX) && !UNITY_EDITOR
        m_platform = "apple";
#elif (UNITY_WP8 || UNITY_WSA) && !UNITY_EDITOR
        m_platform = "winphone";
#elif UNITY_EDITOR
        m_platform = "skip";
#endif

        m_sheduler = IapSheduler.Create (gameObject, this);
        m_sheduler.onValidateFinishedEvent += onValidationFinished;
    }

    private void onValidationFinished (IapPayment payment) {
        if (payment.isValidated) {
            if (null != onPurchaseValidatedEvent) {
                onPurchaseValidatedEvent (payment);
            }
        }
        else {
            if (null != onPurchaseDeclinedEvent) {
                onPurchaseDeclinedEvent (payment);
            }
        }
    }

    /// <summary>
    /// Ons the purchased callback.
    /// </summary>
    /// <param name="e">E.</param>
    public void onPurchasedCallback(Product purchasedProduct)
    {
        try
        {
            Http.Manager.ReportStats("Billing", "PaymentRecieved", new Dictionary<string, string>() {
                {"Item", purchasedProduct.definition.id},
                {"Receipt", purchasedProduct.receipt}
            });
            IapPayment payment = new IapPayment(purchasedProduct);
            if (null != onPurchaseValidateStartedEvent)
            {
                onPurchaseValidateStartedEvent(payment);
            }
            if (m_platform == "skip")
            {
                if (null != onPurchaseValidatedEvent)
                {
                    onPurchaseValidatedEvent(payment);
                }
                return;
            }

            m_sheduler.sheduleToVerify(payment);
        }
        catch (Exception exception)
        {
            Http.Manager.ReportException("IapBillValidator.onPurchasedCallback", exception);
        }
    }
    
    /// <summary>
    /// Dicts to string.
    /// </summary>
    /// <returns>The to string.</returns>
    /// <param name="dict">Dict.</param>
    public string DictToString(Dictionary<string, string> dict)
    {
        string toString = "{";
        foreach (string key in dict.Keys)
        {
            toString += key + "=" + dict[key] + ",\n";
        }
        return toString;
    }
}

