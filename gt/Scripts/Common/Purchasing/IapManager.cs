using System;
using System.Collections.Generic;
#if !(UNITY_WSA || UNITY_WEBGL)
using TapjoyUnity;
#endif
using UnityEngine;
using UnityEngine.Purchasing;

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
        if(Screen.fullScreen)
            Screen.SetResolution(960, 600, false);
        SocialSettings.GetSocialService().ShowPayment(productId);
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
    }
    void Awake()
    {
        Instance = this;
    }

    public static IapManager Instance { get; private set; }
    public static bool IsInitialized()
    {
        return Instance.storeController != null && Instance.extensionProvider != null;
    }

    void Start()
    {
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
    }
    
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
    }

    public void OnInitializeFailed(InitializationFailureReason error)
    {
        // Purchasing set-up has not succeeded. Check error for reason. Consider sharing this reason with the user.
        Debug.LogFormat("OnInitializeFailed InitializationFailureReason: {0}", error);
    }
    
    public PurchaseProcessingResult ProcessPurchase(PurchaseEventArgs args)
    {
        Debug.Log(string.Format("ProcessPurchase: Product: '{0}'", args.purchasedProduct.definition.storeSpecificId));
        iapValidator.onPurchasedCallback(args.purchasedProduct);
        return PurchaseProcessingResult.Pending;
    }

    public void OnPurchaseFailed(Product product, PurchaseFailureReason failureReason)
    {
        XdevsSplashScreen.SetActiveWaitingIndicator(false);
        Debug.LogFormat("OnPurchaseFailed: FAIL. Product: '{0}', PurchaseFailureReason: {1}", product.definition.storeSpecificId, failureReason);
    }

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

        Debug.LogFormat("Purchase OK: {0}", id);
        Debug.LogFormat("Receipt: {0}", iapPayment.receipt);
        Debug.LogFormat("{0} has been purchased.", iapPayment.product.metadata.localizedTitle);

        ProfileInfo.SaveToServer();
        OnPurchased(iapPayment);
#if !(UNITY_WSA || UNITY_WEBGL)
        Tapjoy.TrackPurchase(id, isoCurrencyCode, (double)localizedPrice);
#endif

        GoogleAnalyticsWrapper.LogItem(
            new ItemHitBuilder()
                .SetTransactionID(iapPayment.receipt)
                .SetSKU(id)
                .SetName(iapPayment.product.metadata.localizedTitle)
                .SetQuantity(1)
                .SetPrice(Convert.ToDouble(localizedPrice))
                .SetCurrencyCode(isoCurrencyCode));
    }

    private void onDeclined(IapPayment iapPayment)
    {
        XdevsSplashScreen.SetActiveWaitingIndicator(false);
        storeController.ConfirmPendingPurchase(iapPayment.product);
        Debug.LogFormat("Purchase failed: {0}", iapPayment.product.definition.id);
    }
}