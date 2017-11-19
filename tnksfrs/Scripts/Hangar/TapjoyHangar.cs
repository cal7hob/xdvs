#if !UNITY_WSA
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
#if !UNITY_WSA

    public static bool IsTapjoyInitialized
    {
        get
        {
            return BankPlacement != null && Tapjoy.IsConnected && BankPlacement.IsContentReady();
        }
    }

    public static TJPlacement BankPlacement { get; private set; }

    private bool isInitialized = false;

    void Awake()
    {
        DontDestroyOnLoad(gameObject);
    }

    void OnDestroy()
    {
        Dispatcher.Unsubscribe(EventId.AfterHangarInit, PreInit);
        Dispatcher.Unsubscribe(EventId.NickNameManuallyChanged, InitTapJoyOffers);
    }

    void Start()
    {
        Dispatcher.Subscribe(EventId.AfterHangarInit, PreInit);

#if UNITY_ANDROID && !UNITY_EDITOR
        // Attach our thread to the java vm; obviously the main thread is already attached but this is good practice..
        if (Application.platform == RuntimePlatform.Android)
            UnityEngine.AndroidJNI.AttachCurrentThread();
#endif
#if (UNITY_ANDROID || UNITY_IOS) && !UNITY_EDITOR
        Debug.LogWarning("Instantiating TapJoy");
        var tj = Instantiate(Resources.Load<TapjoyComponent>("Common/TapjoyUnity"));
        tj.name = "TapjoyUnity";
        DontDestroyOnLoad(tj);

        // Tapjoy Connect Events
        Tapjoy.OnConnectSuccess += HandleTapjoyConnectSuccess;
        Tapjoy.OnConnectFailure += HandleTapjoyConnectFailed;
#endif
    }

    IEnumerator DoConnectAfter()
    {
        yield return new WaitForSeconds(2);
        Tapjoy.Connect();
    }

    // CONNECT
    public void HandleTapjoyConnectSuccess()
    {
        Debug.Log("TapJoy connect success");
        InitDone();
    }

    public void HandleTapjoyConnectFailed()
    {
        Debug.LogWarning("C#: HandleTapjoyConnectFailed, Reconnect");
        Debug.LogWarning("Tapjoy failed!!!");
        StartCoroutine(DoConnectAfter());
    }

    public void PreInit(EventId id, EventInfo info)
    {
        if (ProfileInfo.launchesCount > 1)
        {
            InitTapJoyOffers();
        }
        else
        {
            Dispatcher.Subscribe(EventId.NickNameManuallyChanged, InitTapJoyOffers);
        }
    }

    public void InitTapJoyOffers(EventId id = 0, EventInfo info = null)
    {

#if (UNITY_ANDROID || UNITY_IOS) && !UNITY_EDITOR
        if (!GameData.isTapJoyEnabled || GameData.isAndroidGingerBread) return;

        Tapjoy.SetUserID(ProfileInfo.playerId.ToString());
        Debug.LogWarning("Init tapjoy offers");

        TJPlacement.OnPurchaseRequest += delegate(TJPlacement placement, TJActionRequest request, string productId) { Debug.LogWarning("On Purchase"); };
        TJPlacement.OnRequestFailure += delegate(TJPlacement placement, string error) { Debug.LogWarning("Placement request failure " + error); };
        TJPlacement.OnRequestSuccess += delegate(TJPlacement placement) { Debug.LogWarning("Placement request success"); };
        TJPlacement.OnRewardRequest += delegate(TJPlacement placement, TJActionRequest request, string itemId, int quantity) { Debug.LogWarning("Placement reward " + quantity); };
        TJPlacement.OnContentDismiss += delegate(TJPlacement placement) { Debug.LogWarning("Placement content dismissed"); };
        TJPlacement.OnContentShow += delegate(TJPlacement placement)
        {
            Debug.LogWarning("Placement content show. Requesting new content");
            BankPlacement.RequestContent();
        };

        Debug.LogWarning("Creating TapJoy Placement");
        if(BankPlacement == null)
        {
            BankPlacement = TJPlacement.CreatePlacement("Bank");
        }
        isInitialized = true;
        InitDone();
#endif
    }

    void InitDone ()
    {
        if (!Tapjoy.IsConnected || !isInitialized) {
            return;
        }
        Debug.Log("\n\n\n ====================== INIT DONE \n\n\n");

        if (BankPlacement != null) {
            Debug.Log("Request tapjoy placement content");
            BankPlacement.RequestContent();
        }
    }
#endif
}
