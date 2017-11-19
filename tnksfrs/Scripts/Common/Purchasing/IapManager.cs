using System;
using System.Collections.Generic;
#if !UNITY_WSA
using TapjoyUnity;
#endif
using UnityEngine;
using UnityEngine.Purchasing;
using XD;

public class IapManager : MonoBehaviour, IStoreListener
{
    public event Action OnReady = delegate { };
    public event Action<IapPayment> OnPurchased = delegate { };
    
    public static IStoreController StoreController
    {
        get { return Instance.storeController; }
        set { Instance.storeController = value; }
    }

    private IStoreController    storeController;        // The Unity Purchasing system.
    private IExtensionProvider  extensionProvider;      // The store-specific Purchasing subsystems.
    private IapBillValidator    iapValidator;

    private Action successCallback;
	public Store CurrentStore
	{
		get 
		{
            #if UNITY_IOS
			return Store.AppleAppStore;	
            #elif UNITY_STANDALONE_OSX
            return Store.MacAppStore;	
            #elif UNITY_ANDROID
            return Store.GooglePlay;
			#elif UNITY_WSA
			return Store.WindowsStore;
			#else 
			return Store.GooglePlay;	
			#endif
		}
	}
    public static void BuyProductId(Transform waitingIndicatorParent, string productId, Action successCallback = null)
    {
#if UNITY_WEBGL
        StaticType.SocialSettings.Instance<ISocialSettings>().GetSocialService().ShowPayment(productId);
        return;
#endif

        if (IsInitialized())
        {
            Product product = StoreController.products.WithID(productId);

            if (product != null && product.availableToPurchase)
            {
                //this.successCallback = successCallback;
                if (XdevsSplashScreen.Instance.waitingIndicator.IsShowed)
                {
                    DT.Log("Other purchase is not completed");
                    return;
                }
                XdevsSplashScreen.SetActiveWaitingIndicator(true, waitingIndicatorParent);
                Instance.successCallback = successCallback;
                Debug.Log(string.Format("Purchasing product asychronously: '{0}'", product.definition.id));
                StoreController.InitiatePurchase(product);

            }
            else
            {
                Debug.Log("BuyProductID: FAIL. Not purchasing product, either is not found or is not available for purchase");
            }
        }
        else
        {
            // ... report the fact Purchasing has not succeeded initializing yet. Consider waiting longer or 
            // retrying initiailization.
            Debug.Log("BuyProductID FAIL. Not initialized.");
        }
    } //+
    void Awake()
    {
        Instance = this;
    }

    public static IapManager Instance { get; private set; }
    public static bool IsInitialized()
    {
        return Instance.storeController != null && Instance.extensionProvider != null;
    } //+

    void Start()
    {
#if DEBUG_TAG_TF
        Debug.Log("TF: IapManager.Start()");
#endif
        if (storeController == null)
        {
            InitializePurchasing();
        }
    }

    private void InitializePurchasing()
    {
        if (IsInitialized())
        {
            return;
        }

        iapValidator = gameObject.GetComponent<IapBillValidator>();
        iapValidator.onPurchaseValidatedEvent += onPurchased;
        iapValidator.onPurchaseDeclinedEvent += onDeclined;

        var builder = ConfigurationBuilder.Instance(StandardPurchasingModule.Instance());

        var products = ProductDatabase.GetProducts();

        foreach (var xdevsProduct in products)
        {
            var idOverrides = new IDs();
            foreach (var storeSpecificId in xdevsProduct.IdOverrides)
            {
                idOverrides.Add(storeSpecificId.Id, storeSpecificId.Store.ToString());
            }
            builder.AddProduct(xdevsProduct.Id, xdevsProduct.Type, idOverrides);
        }

        /*builder.AddProduct(kProductIDSubscription, ProductType.Consumable, new IDs(){
                { kProductNameAppleSubscription, AppleAppStore.Name },
                { kProductNameGooglePlaySubscription, GooglePlay.Name },
            });*/

        UnityPurchasing.Initialize(this, builder);
    } //+
    
    //  
    // --- IStoreListener
    //

    public void OnInitialized(IStoreController controller, IExtensionProvider extensions)
    {
        // Purchasing has succeeded initializing. Collect our Purchasing references.
        Debug.Log("OnInitialized: PASS");

        storeController = controller;
        extensionProvider = extensions;

        OnReady();
    } //+

    public void OnInitializeFailed(InitializationFailureReason error)
    {
        // Purchasing set-up has not succeeded. Check error for reason. Consider sharing this reason with the user.
        Debug.Log("OnInitializeFailed InitializationFailureReason:" + error);
    } //+
    
    public PurchaseProcessingResult ProcessPurchase(PurchaseEventArgs args)
    {
        Debug.Log(string.Format("ProcessPurchase: Product: '{0}'", args.purchasedProduct.definition.storeSpecificId));
        iapValidator.onPurchasedCallback(args.purchasedProduct);
        return PurchaseProcessingResult.Pending;
    } //+

    public void OnPurchaseFailed(Product product, PurchaseFailureReason failureReason)
    {
        XdevsSplashScreen.SetActiveWaitingIndicator(false);
        Debug.Log(string.Format("OnPurchaseFailed: FAIL. Product: '{0}', PurchaseFailureReason: {1}", product.definition.storeSpecificId, failureReason));
        /*AppsFlyerImpl.TrackRichEvent(
            AFInAppEventType.PURCHASE_FAILED,
            new Dictionary<string, string>() {
                {AFInAppEventParameterName.PRICE, product.metadata.localizedPrice.ToString()},
                {AFInAppEventParameterName.CURRENCY, product.metadata.isoCurrencyCode},
                {AFInAppEventParameterName.CONTENT_ID, product.definition.id},
                {AFInAppEventParameterName.REASON, failureReason.ToString()},
            }
        );*/
    } //+

    /// <summary>
    /// This will be called when a purchase validated.
    /// </summary>
    private void onPurchased(IapPayment iapPayment)
    {
        XdevsSplashScreen.SetActiveWaitingIndicator(false);
#if UNITY_STANDALONE_WIN
        return;
#endif
        storeController.ConfirmPendingPurchase(iapPayment.product);
        var id = iapPayment.product.definition.id;
        var localizedPrice = iapPayment.product.metadata.localizedPrice;
        var isoCurrencyCode = iapPayment.product.metadata.isoCurrencyCode;

        Debug.Log("Purchase OK: " + id);
        Debug.Log("Receipt: " + iapPayment.receipt);
        Debug.Log(string.Format("{0} has been purchased.",
                                  iapPayment.product.metadata.localizedTitle));

        ProfileInfo.SaveToServer();
        OnPurchased(iapPayment);
#if !UNITY_WSA
        Tapjoy.TrackPurchase(id, isoCurrencyCode, (double)localizedPrice);
#endif

        /*AppsFlyerImpl.TrackRichEvent(
            AFInAppEventType.PURCHASE,
            new Dictionary<string, string>() {
                {AFInAppEventParameterName.REVENUE, localizedPrice.ToString()},
                {AFInAppEventParameterName.CURRENCY, isoCurrencyCode},
                {AFInAppEventParameterName.CONTENT_ID, id},
            });*/

        GoogleAnalyticsWrapper.LogItem(
            new ItemHitBuilder()
                .SetTransactionID(iapPayment.receipt)
                .SetSKU(id)
                .SetName(iapPayment.product.metadata.localizedTitle)
                .SetQuantity(1)
                .SetPrice(Convert.ToDouble(localizedPrice))
                .SetCurrencyCode(isoCurrencyCode));
    } //+

    private void onDeclined(IapPayment iapPayment)
    {
        XdevsSplashScreen.SetActiveWaitingIndicator(false);
        storeController.ConfirmPendingPurchase(iapPayment.product);
        Debug.Log("Purchase failed: " + iapPayment.product.definition.id);
        /*AppsFlyerImpl.TrackRichEvent(
            AFInAppEventType.PURCHASE_NOT_VALIDATED,
            new Dictionary<string, string>() {
                {AFInAppEventParameterName.PRICE, iapPayment.product.metadata.localizedPrice.ToString()},
                {AFInAppEventParameterName.CURRENCY, iapPayment.product.metadata.isoCurrencyCode},
                {AFInAppEventParameterName.CONTENT_ID, iapPayment.product.definition.id},
            }
        );*/
    } //+
}