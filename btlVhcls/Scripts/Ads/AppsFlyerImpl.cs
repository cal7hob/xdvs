using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class AFInAppEventType
{
    public const string LEVEL_ACHIEVED                = "af_level_achieved";
    public const string ADD_PAYMENT_INFO              = "af_add_payment_info";
    public const string ADD_TO_CART                   = "af_add_to_cart";
    public const string ADD_TO_WISH_LIST              = "af_add_to_wishlist";
    public const string COMPLETE_REGISTRATION         = "af_complete_registration";
    public const string TUTORIAL_COMPLETION           = "af_tutorial_completion";
    public const string INITIATED_CHECKOUT            = "af_initiated_checkout";
    public const string PURCHASE                      = "af_purchase";
    public const string RATE                          = "af_rate";
    public const string SEARCH                        = "af_search";
    public const string SPENT_CREDIT                  = "af_spent_credits";
    public const string ACHIEVEMENT_UNLOCKED          = "af_achievement_unlocked";
    public const string CONTENT_VIEW                  = "af_content_view";
    public const string LIST_VIEW                     = "af_list_view";
    public const string TRAVEL_BOOKING                = "af_travel_booking";
    public const string SHARE                         = "af_share";
    public const string INVITE                        = "af_invite";
    public const string LOGIN                         = "af_login";
    public const string RE_ENGAGE                     = "af_re_engage";
    public const string UPDATE                        = "af_update";
    public const string OPENED_FROM_PUSH_NOTIFICATION = "af_opened_from_push_notification";
    public const string LOCATION_CHANGED              = "af_location_changed";
    public const string LOCATION_COORDINATES          = "af_location_coordinates";
    public const string ORDER_ID                      = "af_order_id";

    // Our custom events
    public const string PURCHASE_NOT_VALIDATED        = "purchase_not_validated";
    public const string PURCHASE_FAILED               = "purchase_failed";
}

public class AFInAppEventParameterName
{
    // af_revenue is the only parameter that is used for revenue calculations. Use it for events that actually
    // represent revenue generation in your business logic.You can use af_price as a monetary parameter that will
    // not be counted as revenue(such as in an “Add to Cart” event).
    public const string REVENUE                = "af_revenue";                // Float
    public const string PRICE                  = "af_price";                  // Float
    public const string LEVEL                  = "af_level";                  // Int
    public const string SUCCESS                = "af_success";                // Boolean
    public const string CONTENT_TYPE           = "af_content_type";           // String
    public const string CONTENT_LIST           = "af_content_list";           // Array of strings
    public const string CONTENT_ID             = "af_content_id";             // String
    public const string CURRENCY               = "af_currency";               // String
    public const string REGISTRATION_METHOD    = "af_registration_method";    // String
    public const string QUANTITY               = "af_quantity";               // Int
    public const string PAYMENT_INFO_AVAILABLE = "af_payment_info_available"; // Boolean
    public const string RATING_VALUE           = "af_rating_value";           // Float
    public const string MAX_RATING_VALUE       = "af_max_rating_value";       // Float
    public const string SEARCH_STRING          = "af_search_string";          // String
    public const string DESCRIPTION            = "af_description";            // String
    public const string SCORE                  = "af_score";                  // Int
    public const string DESTINATION_A          = "af_destination_a";          // String
    public const string DESTINATION_B          = "af_destination_b";          // String
    public const string CLASS                  = "af_class";                  // String
    public const string DATE_A                 = "af_date_a";                 // String
    public const string DATE_B                 = "af_date_b";                 // String
    public const string EVENT_START            = "af_event_start";            // Unixtime
    public const string EVENT_END              = "af_event_end";              // Unixtime
    public const string LATITUDE               = "af_lat";                    // Int
    public const string LONGITUDE              = "af_long";                   // Int
    public const string CUSTOMER_USER_ID       = "af_customer_user_id";       // String
    public const string VALIDATED              = "af_validated";              // String
    public const string RECEIPT_ID             = "af_receipt_id";             // String
    public const string PARAM_1                = "af_param_1";                // String
    public const string PARAM_2                = "af_param_2";                // String
    public const string PARAM_3                = "af_param_3";                // String
    public const string PARAM_4                = "af_param_4";                // String
    public const string PARAM_5                = "af_param_5";                // String
    public const string PARAM_6                = "af_param_6";                // String
    public const string PARAM_7                = "af_param_7";                // String
    public const string PARAM_8                = "af_param_8";                // String
    public const string PARAM_9                = "af_param_9";                // String
    public const string PARAM_10               = "af_param_10";               // String

    // Our custom events
    public const string REASON                 = "reason";                    // String
}

public class AppsFlyerImpl : MonoBehaviour {

	private const string APPSFLYER_DEVKEY = "yPUHYSkUVVjaVPr9gbzqJB";
    public string appId = "";//Устанавливается скриптом  AppsFlyerOptions
    public static AppsFlyerImpl Instance { get; private set; }

	void Awake ()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);

            if (Debug.isDebugBuild)
                AppsFlyer.setIsDebug(true);

#if UNITY_IOS
            AppsFlyer.setAppsFlyerKey(APPSFLYER_DEVKEY);
            AppsFlyer.setAppID(appId);
    		AppsFlyer.getConversionData ();
#elif UNITY_ANDROID
            AppsFlyer.init(APPSFLYER_DEVKEY);
            AppsFlyer.setAppID(appId);
            AppsFlyer.loadConversionData("AppsFlyerTrackerCallbacks");
#elif UNITY_WSA
            AppsFlyer.setAppsFlyerKey(APPSFLYER_DEVKEY);//Поидее только для ИОС
            AppsFlyer.setAppID(appId);
#endif

            AppsFlyer.trackAppLaunch();
        }
        else
            DestroyImmediate(this);
    }

    public static void TrackRichEvent (string eventName, Dictionary<string, string> eventValues)
    {
#if (UNITY_ANDROID || UNITY_IOS || UNITY_WSA) && !UNITY_EDITOR
        if(Instance == null)
		{
			DT.LogError("AppsFlyerImpl.TrackRichEvent(). Instance == NULL!!! The event {0} cant be sent.", eventName);
			return;
		}
		Debug.LogFormat("TrackRichEvent, {0}\neventValues = {1}", eventName, eventValues.ToStringFull());
		AppsFlyer.trackRichEvent(eventName, eventValues);
#endif
    }
}
