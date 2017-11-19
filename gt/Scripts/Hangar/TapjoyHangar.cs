#if !(UNITY_WSA || UNITY_WEBGL)
using TapjoyUnity;
using TapjoyUnity.Internal;
#endif
using UnityEngine;
using System.Collections;
using System.ComponentModel;

public enum TapjoyCategories
{
    VehicleBuying,
    NicknameChanging,
    FuelBuying,
    LeaveBattle,
    RespawnHastenBuying,
    FacebookIntegration,
    RespawnBonusBuying,
    SpecialOffer,
    JoinBattle,
    PickedUpBonus,
    VIPAccountBuying,
    CurrentQuest,
    ProlongGameForMoney,
    StickerBuying,
    CamouflageBuying
}

public enum TapjoyEvents
{
    NotEnoughMoney,
    Bought,
    Success,
    Canceled,
    Accepted,
    FreeVehicle,
    GotViaMoimirInvitation,
    GotViaFacebookInvitation,
    LeftBattleManually,
    LeftBattleDisconnected,
    Completed,
    Failed
}

public class TapjoyHangar : MonoBehaviour
{
    public static bool IsTapjoyeble
    {
        get
        {
#if (UNITY_ANDROID || UNITY_IOS) && !UNITY_EDITOR
            return GameData.isTapJoyEnabled;
#else
            return false;
#endif
        }
    }

#if !(UNITY_WP8 || UNITY_WP8_1 || UNITY_WSA || UNITY_WSA_8_0 || UNITY_WSA_8_1 || UNITY_WSA_10_0 || UNITY_WEBGL)

    private TapjoyComponent tapjoy;

    public static bool IsTapjoyInitialized
    {
        get
        {
            //Debug.LogFormat("TapJoy: BankPlacement != null: {0}, Tapjoy.IsConnected {1}, BankPlacement.IsContentReady(): {2}", BankPlacement != null, Tapjoy.IsConnected, BankPlacement != null && BankPlacement.IsContentReady());

            //Debug.LogFormat("IsTapjoyInitialized: {0}", BankPlacement != null && Tapjoy.IsConnected && BankPlacement.IsContentReady());

            return BankPlacement != null && Tapjoy.IsConnected && BankPlacement.IsContentReady();
        }
    }

    public static TJPlacement BankPlacement { get; private set; }
    public static TapjoyHangar Instance { get; private set; }

    private bool isInitialized = false;

    void Awake()
    {
        if (Instance != null)
            return;

        DontDestroyOnLoad(gameObject);
        Instance = this;

        Dispatcher.Subscribe(EventId.AfterHangarInit, Init);
    }

    void Start()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        // Attach our thread to the java vm; obviously the main thread is already attached but this is good practice..
        if (Application.platform == RuntimePlatform.Android)
            UnityEngine.AndroidJNI.AttachCurrentThread();
#endif
    }

    private static IEnumerator DoReconnect()
    {
        yield return new WaitForSeconds(2);
        Debug.Log("retrying to connect TapJoy");
        Tapjoy.Connect();
    }

    // CONNECT
    private void HandleTapjoyConnectSuccess()
    {
        Debug.Log("TapJoy connect success");
        Tapjoy.SetUserID(ProfileInfo.profileId.ToString());
        RequestContent();
    }

    private void HandleTapjoyConnectFailed()
    {
        Debug.Log("Tapjoy connect failed!!!");
        StartCoroutine(DoReconnect());
    }

    private void InstantiateTapjoy()
    {
#if (UNITY_ANDROID || UNITY_IOS) && !UNITY_EDITOR
        Debug.Log("Instantiating TapJoy");
        var tapjoyPrefab = Resources.Load<TapjoyComponent>("Common/TapjoyUnity"); ;
        tapjoyPrefab.settings.AndroidSettings.SdkKey = GameData.tapJoyAndroidSdkKey;
        tapjoyPrefab.settings.AndroidSettings.PushKey = GameData.tapJoyAndroidGcmSenderId;
        tapjoyPrefab.settings.IosSettings.SdkKey = GameData.tapJoyIosSdkKey;
        tapjoy = Instantiate(tapjoyPrefab);
        tapjoy.name = "TapjoyUnity";
        DontDestroyOnLoad(tapjoy);
#endif
    }

    public void Init(EventId id, EventInfo info)
    {
        if (GameData.isAndroidGingerBread || isInitialized)
        {
            TrySendTapjoyEngagement();
            return;
        }

        InstantiateTapjoy();

        if (tapjoy == null)
            return;

        if (!Tapjoy.IsConnected)
        {
            Debug.Log("Connecting tapjoy");
            Tapjoy.Connect();
        }

        Tapjoy.OnConnectSuccess += HandleTapjoyConnectSuccess;
        Tapjoy.OnConnectSuccess += TrySendTapjoyEngagement;
        Tapjoy.OnConnectFailure += HandleTapjoyConnectFailed;

        TJPlacement.OnContentShow += delegate (TJPlacement placement)
        {
            Debug.Log("Placement content show. Requesting new content");
            BankPlacement.RequestContent();
        };

        isInitialized = true;
    }

    private static void TrySendTapjoyEngagement()
    {
        if (!IsTapjoyeble)
        {
            return;
        }

        if (!Tapjoy.IsConnected)
        {
            Instance.StartCoroutine(DoReconnect());
            Tapjoy.OnConnectSuccess += TrySendTapjoyEngagement;
            return;
        }

        if (ProfileInfo.Level >= GameData.tapjoyEngagementLevel && !ProfileInfo.didTapjoyEngagementSend)
        {

#if UNITY_ANDROID
            Tapjoy.ActionComplete(GameData.tapjoyAndroidEngagementId);
#elif UNITY_IOS
        Tapjoy.ActionComplete(GameData.tapjoyIosEngagementId);
#endif

            ProfileInfo.didTapjoyEngagementSend = true;
            ProfileInfo.SaveToServer();
        }
    }

    private static void RequestContent()
    {
        if (BankPlacement == null)
        {
            Debug.Log("Creating TapJoy Placement");
            BankPlacement = TJPlacement.CreatePlacement("Bank");
        }

        Debug.Log("Requesting TapJoy placement content");
        BankPlacement.RequestContent();
    }
#else

    public static bool IsTapjoyInitialized { get { return false; } }

#endif
}