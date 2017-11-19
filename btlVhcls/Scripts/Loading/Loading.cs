using UnityEngine;
using System.Collections;
using UnityEngine.SceneManagement;
using AssetBundles;

public class Loading : MonoBehaviour
{
    public GameObject musicBoxPrefab;
    public GameObject appsFlyerPrefab;
    public static Loading Instance { get; private set; }

    public Camera GuiCamera { get; private set; }
    public tk2dCamera Tk2dGuiCamera { get; private set; }

    private const string ADVICE_RESOURCES_PATH = "Other";

    private static string m_loadScene;
    private static bool m_isAssetsManagerLoaded = false;

    public static string PreviousScene { get; private set; }

    public static void loadScene (string scene)
    {
        PreviousScene = m_loadScene = scene;
        gotoLoadingScene ();
    }

    public static void gotoLoadingScene ()
    {
        SceneManager.LoadSceneAsync ("LoadingScene");
    }

    private void Awake()
    {
        Instance = this;
        GuiCamera = GetComponent<Camera>();
        Tk2dGuiCamera = GetComponent<tk2dCamera>();
    }

    private void OnDestroy()
    {
        Instance = null;
    }

    private IEnumerator Start()
    {       
        GameData.Init();

        if (AtlasesManager.Instance == null)
            Instantiate(Resources.Load(string.Format("{0}/{1}/AtlasesManager{2}", GameManager.CurrentResourcesFolder, ADVICE_RESOURCES_PATH, GameData.InterfaceShortName.ToUpper())));

        if (XdevsSplashScreen.Instance == null) {
            string resPath = string.Format("{0}/GuiPrefabs/Common/Splash{1}", GameManager.CurrentResourcesFolder, GameData.InterfaceShortName.ToUpper());
            UberDebug.LogChannel("Loading", "Loading splash! {0}", resPath);
            GameObject go = Resources.Load<GameObject>(resPath);
            if (go) {
                UberDebug.LogChannel("Loading", "Istantiate splash!");
                Instantiate(go);
            }
        }

        if (MessageBox.Instance == null) {
            var path = string.Format("{0}/GuiPrefabs/Common/MessageBox{1}", GameManager.CurrentResourcesFolder, GameData.InterfaceShortName.ToUpper());
            GameObject go = Resources.Load<GameObject>(path);

            if (go) {
                GameObject mb = Instantiate(go);
                mb.transform.SetParent(XdevsSplashScreen.Instance.transform);
                mb.transform.localPosition = Vector3.zero;
            }
            else {
                Debug.LogError(string.Format("MessageBox not found at 'Resources/{0}'!", path));
            }
        }

        UberDebug.LogChannel("Loading", "Show splash!");
        XdevsSplashScreen.SetActive(true,HangarController.FirstEnter ? false : true);
        XdevsSplashScreen.SetActiveWaitingIndicator(true);

        if (!m_isAssetsManagerLoaded) {
            InitializeSourceURL();
            AssetBundleManager.logMode = AssetBundleManager.LogMode.JustErrors;

#if UNITY_EDITOR
            if (!AssetBundleManager.SimulateAssetBundleInEditor) {
#endif
                AssetBundleDownloaderAbstract downloader;
#if UNITY_WEBGL || (UNITY_WSA && !UNITY_WSA_10_0)
                downloader = GameData.instance.gameObject.AddComponent<AssetBundleCacher>();
#else
                downloader = GameData.instance.gameObject.AddComponent<AssetBundleDownloader>();
#endif
                while (!downloader.IsDone) {
                    XdevsSplashScreen.IsBundlesLoaded = !downloader.IsLoading;
                    XdevsSplashScreen.LoadedBundlesFullSizeInBytes = downloader.LoadingBytesFull;
                    XdevsSplashScreen.LoadedBundlesDownloadedSizeInBytes = downloader.LoadingBytes;
                    yield return null;
                }

#if !(UNITY_WSA && !UNITY_WSA_10_0)
                if (downloader is AssetBundleDownloader) {
                    AssetBundleManager.SetSourceAssetBundleAbsolteDirectory(((AssetBundleDownloader)downloader).AssetsPath);
                    UberDebug.LogChannel("Loading", "Assets Bundles URL: {0}", AssetBundleManager.BaseDownloadingURL);
                }
                Object.Destroy(downloader);
#endif
                XdevsSplashScreen.IsBundlesLoaded = true;
#if UNITY_EDITOR
            }
#endif

            yield return AssetBundleManager.Initialize();
            m_isAssetsManagerLoaded = true;
            GameData.instance.gameObject.AddComponent<Localizer>();
        }


        if (MusicBox.Instance == null)
            Instantiate(musicBoxPrefab);

        if (AdviceManager.Instance == null)
            Instantiate(Resources.Load(string.Format("{0}/{1}/Advices{2}", GameManager.CurrentResourcesFolder, ADVICE_RESOURCES_PATH, GameData.InterfaceShortName.ToUpper())));

#if (UNITY_ANDROID || UNITY_IOS || UNITY_WSA) && !UNITY_EDITOR
        if (AppsFlyerImpl.Instance == null)
            Instantiate(appsFlyerPrefab);
#endif

        yield return StartAndLoading();
    }

    private IEnumerator StartAndLoading()
    {
        while (!Localizer.Loaded)
            yield return null;

        yield return null;
        yield return null;

        if (HangarController.FirstEnter)
        {

            while (!GameData.ServerDataReceived)
                yield return null;

            if (ProfileInfo.ImportantUpdate)
            {
                yield return Load ();
                yield break;
            }

            //---------------------------------------------------------------------------
            // Синхронизируем профиль игрока при первом входе
            bool saveResult = false;
            bool saveDone = false;

            ProfileInfo.SaveToServer((Http.Response resp, bool result) =>
            {
                saveDone = true;
                saveResult = result;
            });
            while (!saveDone)
            {
                yield return new WaitForSeconds(0.3f);
            }
            if (!saveResult)
            {
                GameData.CriticalError("Server data error");
                yield break;
            }

            ProfileInfo.launchesCount += 1;
        }
        else
        {
            //currentTimeStamp = ProfileInfo.timeStamp + GetCorrectedTime();
        }
        yield return Load();
    }
    private IEnumerator Load()
    {
        yield return Resources.UnloadUnusedAssets();
        System.GC.Collect();
        yield return new WaitForSeconds(0.3f);

        // Ожидаем ответа о завершении боя на сервере
        while (Http.Manager.BattleServer.IsWaitingForBattleEnd) {
            yield return null;
        }

        CodeStage.AntiCheat.Detectors.SpeedHackDetector.isRunning = false;
        if (string.IsNullOrEmpty (m_loadScene)) {
            yield return loadSceneAssetBundle(GameData.HangarSceneName);
            //SceneManager.LoadSceneAsync (GameData.HangarSceneName);
        }
        else {
            yield return loadSceneAssetBundle(m_loadScene);
            //SceneManager.LoadSceneAsync (m_loadScene);
            m_loadScene = null;
        }
    }

    private IEnumerator loadSceneAssetBundle(string sceneName)
    {
        string path = string.Format("{0}/{1}", GameData.CurInterface.ToString(), sceneName).ToLower ();
        yield return AssetBundleManager.LoadLevelAsync(path, sceneName, false);
        //var path = Application.dataPath + "/AssetBundles/" + sceneName.ToLowerInvariant();
        //if (!AssetBundleManager.getAssetBundle(path, 0))
        //{
        //    yield return StartCoroutine(AssetBundleManager.downloadAssetBundle(path, 0));
        //}
    }


    // Initialize the downloading URL.
    // eg. Development server / iOS ODR / web URL
    void InitializeSourceURL()
    {
        string game = GameData.CurInterface.ToString ().ToLower ();
        string version = GameData.instance.GetBundleVersion ();

        // If ODR is available and enabled, then use it and let Xcode handle download requests.
#if ENABLE_IOS_ON_DEMAND_RESOURCES
        if (UnityEngine.iOS.OnDemandResources.enabled)
        {
            AssetBundleManager.SetSourceAssetBundleURL("odr://");
            return;
        }
#endif
#if UNITY_EDITOR
        // With this code, when in-editor or using a development builds: Always use the AssetBundle Server
        // (This is very dependent on the production workflow of the project.
        //      Another approach would be to make this configurable in the standalone player.)
        //AssetBundles.AssetBundleManager.SetDevelopmentAssetBundleServer();

        AssetBundleManager.SetSourceAssetBundleDirectory("/AssetBundles/"+ AssetBundles.Utility.GetPlatformName());
        Debug.LogFormat("Assets Bundles URL: {0}", AssetBundles.AssetBundleManager.BaseDownloadingURL);
        return;
#elif DEVELOPMENT_BUILD
        AssetBundleManager.SetSourceAssetBundleURL(
    #if UNITY_WEBGL
            string.Format("https://scifi-tanks.com/files/assets/{0}/{1}/", game, version)
    #else
            string.Format("http://scifi-tanks.com/files/assets/{0}/{1}/", game, version)
    #endif
        );
        Debug.LogFormat("Assets Bundles URL: {0}", AssetBundles.AssetBundleManager.BaseDownloadingURL);
        return;
#else
        // Use the following code if AssetBundles are embedded in the project for example via StreamingAssets folder etc:
        //AssetBundleManager.SetSourceAssetBundleURL(Application.dataPath + "/");
        // Or customize the URL based on your deployment or configuration
        AssetBundleManager.SetSourceAssetBundleURL(
            string.Format("https://cdn.extreme-developers.ru/assets/{0}/{1}/", game, version)
        );
        return;
#endif
    }

}
