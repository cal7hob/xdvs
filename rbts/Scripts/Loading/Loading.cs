using UnityEngine;
using System.Collections;
using UnityEngine.SceneManagement;

public class Loading : MonoBehaviour
{
	public VKAuth vkAuth;
	public GameObject musicBoxPrefab;
    public GameObject appsFlyerPrefab;

    private const string ADVICE_RESOURCES_PATH = "Other";

    private static string m_loadScene;

    public static string PreviousScene { get; private set; }

    public static void loadScene (string scene)
    {
        PreviousScene = m_loadScene = scene;
        gotoLoadingScene ();
    }

    public static void gotoLoadingScene ()
    {
        SceneManager.LoadScene ("LoadingScene");
    }

	private void Start()
	{       
		GameData.Init();
        if (XdevsSplashScreen.Instance.waitingIndicator == null)
        {
            GameObject go = Instantiate(Resources.Load(string.Format("{0}/{1}/WaitingIndicator{2}", GameManager.CurrentResourcesFolder, ADVICE_RESOURCES_PATH, GameData.InterfaceShortName.ToUpper()))) as GameObject;
            if(go)
            {
                go.transform.parent = XdevsSplashScreen.Instance.waitingIndicatorParent;
                go.transform.localPosition = Vector3.zero;
                XdevsSplashScreen.Instance.waitingIndicator = go.GetComponent<WaitingIndicatorBase>();
            }
        }

        XdevsSplashScreen.SetActive(true,HangarController.FirstEnter ? false : true);// Set MobileSplash texture
        XdevsSplashScreen.SetActiveWaitingIndicator(true);

		if (MusicBox.Instance == null)
			Instantiate(musicBoxPrefab);
        //DT3.Log(string.Format("{0}/{1}/Advices{2}", GameManager.CurrentResourcesFolder, ADVICE_RESOURCES_PATH, GameData.InterfaceShortName.ToUpper()));
        if (AdviceManager.Instance == null)
            Instantiate(Resources.Load(string.Format("{0}/{1}/Advices{2}", GameManager.CurrentResourcesFolder,ADVICE_RESOURCES_PATH,GameData.InterfaceShortName.ToUpper())));

#if (UNITY_ANDROID || UNITY_IOS || UNITY_WSA) && !UNITY_EDITOR
        if (AppsFlyerImpl.Instance == null)
            Instantiate(appsFlyerPrefab);
#endif

        StartCoroutine(StartAndLoading());
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
#if UNITY_WEBGL
            yield return loadSceneAssetBundle(GameData.HangarSceneName);
#endif
            SceneManager.LoadScene (GameData.HangarSceneName);
        }
        else {
#if UNITY_WEBGL
            yield return loadSceneAssetBundle(m_loadScene);
#endif
            SceneManager.LoadScene(m_loadScene);
            m_loadScene = null;
        }
	}

    private IEnumerator loadSceneAssetBundle(string sceneName)
    {
        var path = Application.dataPath + "/AssetBundles/" + sceneName.ToLowerInvariant();
        if (!AssetBundleManager.getAssetBundle(path, 0))
        {
            yield return StartCoroutine(AssetBundleManager.downloadAssetBundle(path, 0));
        }
    }
}
