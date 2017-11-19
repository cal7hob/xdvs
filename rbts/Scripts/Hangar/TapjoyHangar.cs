#if (UNITY_ANDROID || UNITY_IOS)
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
            return true;
#else
            return false;
#endif
        }
    }

#if (UNITY_ANDROID || UNITY_IOS)

#pragma warning disable 649 // Используется только в сборке
    private TapjoyComponent tapjoy;
#pragma warning restore 649

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

        Messenger.Subscribe(EventId.AfterHangarInit, Init);
    }

    void Start()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        // Attach our thread to the java vm; obviously the main thread is already attached but this is good practice..
        if (Application.platform == RuntimePlatform.Android)
            UnityEngine.AndroidJNI.AttachCurrentThread();
#endif
    }

    private static IEnumerator DoConnectAfter()
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

        if (GameData.isTapJoyEnabled)
        {
            RequestContent();
        }   
    }

    private void HandleTapjoyConnectFailed()
    {
        Debug.Log("Tapjoy connect failed!!!");
        StartCoroutine(DoConnectAfter());
    }

    private void InstantiateTapjoy()
    {
        if(!IsTapjoyeble)
            return;

        Debug.Log("Instantiating TapJoy");
        var tapjoyPrefab = Resources.Load<TapjoyComponent>("Common/TapjoyUnity"); ;
        tapjoyPrefab.settings.AndroidSettings.SdkKey = GameData.tapJoyAndroidSdkKey;
        tapjoyPrefab.settings.AndroidSettings.PushKey = GameData.tapJoyAndroidGcmSenderId;
        tapjoyPrefab.settings.IosSettings.SdkKey = GameData.tapJoyIosSdkKey;
        tapjoy = Instantiate(tapjoyPrefab);
        tapjoy.name = "TapjoyUnity";
        DontDestroyOnLoad(tapjoy);

    }

    public void Init(EventId id, EventInfo info)
    {
        if (GameData.isAndroidGingerBread || isInitialized || !IsTapjoyeble)
            return;

        InstantiateTapjoy();

        if (tapjoy == null)
            return;

        if (!Tapjoy.IsConnected)
        {
            Debug.Log("Connecting tapjoy");
            Tapjoy.Connect();
        }

        Tapjoy.OnConnectSuccess += HandleTapjoyConnectSuccess;
        Tapjoy.OnConnectFailure += HandleTapjoyConnectFailed;

        isInitialized = true;
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

        TJPlacement.OnContentReady += placement => Bank.Instance.ShowTapjoyFrames(activate: true);
 
        TJPlacement.OnContentShow += placement =>
        {
            Debug.Log("Placement content show. Requesting new content");
            BankPlacement.RequestContent();
        };
    }
#else
    public static bool IsTapjoyInitialized { get { return false; } }
#endif
}