//using UnityEngine;
//using System;
//using System.Collections;
//using System.Collections.Generic;
//using System.IO;
//#if !UNITY_WSA
//using TapjoyUnity;
//#endif


//public class BankAndroid_Unibill : MonoBehaviour
//{
//    private GUIContent[] comboBoxList;
//    private int selectedItemIndex;
//    private PurchasableItem[] items;
//    private IapBillValidator m_validator;
//    private Action successCallback;

//    public static BankAndroid_Unibill Instance { get; private set; }

//    void Awake()
//    {
//        Instance = this;
//    }
//    public event Action<IapPayment> OnPurchased  = delegate { };
//    void OnDestroy()
//    {
//#if !(UNITY_WEBPLAYER || UNITY_WEBGL)

//            Unibiller.onPurchaseFailed -= OnPurchaseFailed;
//            Unibiller.onPurchaseCompleteEvent -= OnPurchaseComplete;
//            Unibiller.onBillerReady -= onBillerReady;
//            Unibiller.onTransactionsRestored -= onTransactionsRestored;
//#endif

//        Instance = null;
//    }

//    void Start()
//    {
//#if !(UNITY_WEBPLAYER || UNITY_WEBGL)

//        if (UnityEngine.Resources.Load ("unibillInventory.json") == null) {
//            Debug.LogError("You must define your purchasable inventory within the inventory editor!");
//            this.gameObject.SetActive(false);
//            return;
//        }

//        m_validator = gameObject.GetComponent<IapBillValidator>();
//        //m_validator.onPurchaseValidateStartedEvent += onValidationStarted;
//        m_validator.onPurchaseValidatedEvent += onPurchased;
//        m_validator.onPurchaseDeclinedEvent += onDeclined;

//        // We must first hook up listeners to Unibill's events.
//        Unibiller.onBillerReady += onBillerReady;
//        Unibiller.onTransactionsRestored += onTransactionsRestored;
//        //Unibiller.onPurchaseCompleteEvent += m_validator.onPurchasedCallback;

//        // Now we're ready to initialise Unibill.
//        if (!Unibiller.Initialised)
//        {
//            Unibiller.Initialise();
//        }

//        initCombobox();
//#endif
//    }


//#if !(UNITY_WEBPLAYER || UNITY_WEBGL)
//    /// <summary>
//    /// This will be called when Unibill has finished initialising.
//    /// </summary>
//    private void onBillerReady(UnibillState state) {
//        Debug.Log("UnibillerReady:" + state);
//    }

//    /// <summary>
//    /// This will be called after a call to Unibiller.restoreTransactions().
//    /// </summary>
//    private void onTransactionsRestored (bool success) {
//        Debug.Log("Transactions restored.");
//    }

//    /// <summary>
//    /// This will be called when a purchase completes.
//    /// </summary>
//    private void onPurchased(IapPayment p) {
//        XdevsSplashScreen.SetActiveWaitingIndicator(false);
//#if UNITY_STANDALONE_WIN
//        return;
//#endif
//        //PurchaseEvent e = p.purchaseEvent;

////        Debug.Log ("Purchase OK: " + p.item.Id);
////        Debug.Log ("Receipt: " + p.receipt);
////        Debug.Log (string.Format ("{0} has now been purchased {1} times.",
////                                  p.item.name,
////                                  Unibiller.GetPurchaseCount(p.item)));

////        ProfileInfo.SaveToServer ();
////        OnPurchased(p);
////#if !UNITY_WSA
////        Tapjoy.TrackPurchase (p.item.Id, p.item.isoCurrencySymbol, (double)p.item.priceInLocalCurrency);
////#endif

////        GoogleAnalyticsWrapper.LogItem (
////            new ItemHitBuilder()
////                .SetTransactionID(p.receipt)
////                .SetSKU(p.item.Id)
////                .SetName(p.item.name)
////                .SetQuantity(1)
////                .SetPrice(Convert.ToDouble(p.item.priceInLocalCurrency))
////                .SetCurrencyCode(p.item.isoCurrencySymbol));
//    }

//    /// <summary>
//    /// This will be called if a user opts to cancel a purchase
//    /// after going to the billing system's purchase menu.
//    /// </summary>
//    private void onCancelled(PurchasableItem item) {
//        XdevsSplashScreen.SetActiveWaitingIndicator(false);
//        Debug.Log("Purchase cancelled: " + item.Id);
//    }

//    /// <summary>
//    /// This will be called is an attempted purchase fails on server validation.
//    /// </summary>
//    private void onDeclined(IapPayment p) {
//        //XdevsSplashScreen.SetActiveWaitingIndicator(false);
//        //PurchasableItem item = p.item;
//        //Debug.Log("Purchase failed: " + item.Id);

//    private void initCombobox() {
//        items = Unibiller.AllPurchasableItems;
//        comboBoxList = new GUIContent[items.Length];
//        for (int t = 0; t < items.Length; t++) {
//            comboBoxList[t] = new GUIContent(string.Format("{0} - {1}", items[t].localizedTitle, items[t].localizedPriceString));
//        }
//    }

//    private void OnPurchaseComplete(PurchaseEvent e)
//    {
//        Debug.Log("PURCHASE COMPLETED");
//        if (successCallback != null)
//        {
//            successCallback();
//        }
//    }

//    private static void OnPurchaseFailed(PurchaseFailedEvent e)
//    {
//        Debug.Log("PURCHASE FAILED " + e.PurchasedItem.Id);
//        Instance.onCancelled(e.PurchasedItem);
//        XdevsSplashScreen.SetActiveWaitingIndicator(false);
//    }

//#endif

//    public void BuyUnibillerItemClick(Transform waitingIndicatorParent, string unibillerItemId, Action successCallback = null)
//    {
//        if (XdevsSplashScreen.Instance.waitingIndicator.IsShowed)
//        {
//            DT.Log("Other purchase is not completed");
//            return;
//        }
//        XdevsSplashScreen.SetActiveWaitingIndicator(true, waitingIndicatorParent);

//#if !(UNITY_WEBGL || UNITY_WEBPLAYER)
//        this.successCallback = successCallback;
        
//        Unibiller.initiatePurchase(unibillerItemId);

//        Debug.Log("Buying: " + unibillerItemId);
//        Unibiller.onPurchaseFailed += OnPurchaseFailed;
//        Unibiller.onPurchaseCompleteEvent += OnPurchaseComplete;
//#else
//        SocialSettings.Instance.BuyUnibillerItemClick(unibillerItemId);
//#endif

//    }
//}
