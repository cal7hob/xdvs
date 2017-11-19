using UnityEngine;
using System.Collections;
using UnityEngine.SceneManagement;

public class Loading : MonoBehaviour
{
    public VKAuth           vkAuth = null;
    public GameObject       appsFlyerPrefab = null;

    private const string    ADVICE_RESOURCES_PATH = "Other";

    private static string   m_loadScene = "";

    public static string PreviousScene
    {
        get;
        private set;
    }

    public static void LoadScene(string scene)
    {
        PreviousScene = m_loadScene = scene;
        GoToLoadingScene();        
    }

    public static void GoToLoadingScene()
    {
        SceneManager.LoadScene("scn_clear");
    }

    private void Start()
    {
		#if DEBUG_FOR_WEB_GL
		Debug.LogError("TF: Loading.Start 1");
		#endif
        GameData.Init();

        if (AdviceManager.Instance == null)
        {
            string path = string.Format("{0}/{1}/Advices{2}", XD.StaticContainer.GameManager.CurrentResourcesFolder, ADVICE_RESOURCES_PATH, GameData.InterfaceShortName.ToUpper());
            Instantiate(Resources.Load(path));
        }

#if (UNITY_ANDROID || UNITY_IPHONE || UNITY_WSA) && !UNITY_EDITOR
         /*if (AppsFlyerImpl.Instance == null)
            Instantiate(appsFlyerPrefab);*/
#endif
        StartCoroutine(StartAndLoading());
    }

    private IEnumerator StartAndLoading()
    {
        //Debug.Log("Start And Loading!", this);
        yield return null;
        yield return null;

        if (HangarController.FirstEnter)
        {
            while (!GameData.ServerDataReceived)
            {
                //Debug.Log("while (!GameData.ServerDataReceived) - waiting!", this);
                yield return null;
            }

            if (ProfileInfo.ImportantUpdate)
            {
                StartCoroutine(Load());
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
                //Debug.Log("while (!saveDone) - waiting!", this);
                yield return new WaitForSeconds(0.3f);
            }

            if (!saveResult)
            {
                Debug.Log("!saveResult - break!", this);
                GameData.CriticalError("UI_MB_ServerDataError");
                yield break;
            }

            ProfileInfo.launchesCount += 1;
        }
        else
        {
            //currentTimeStamp = ProfileInfo.timeStamp + GetCorrectedTime();
        }

        StartCoroutine(Load());
    }

    private IEnumerator Load()
    {
        yield return Resources.UnloadUnusedAssets();
        System.GC.Collect();
        yield return new WaitForSeconds(0.3f);

        // Ожидаем ответа о завершении боя на сервере
        while (Http.Manager.Instance().battleServer.IsWaitingForBattleEnd)
        {
            yield return null;
        }

        CodeStage.AntiCheat.Detectors.SpeedHackDetector.isRunning = false;
        if (string.IsNullOrEmpty(m_loadScene))
        {
            SceneManager.LoadScene(GameData.HangarSceneName);
            Dispatcher.Send(EventId.AfterHangarInit, null);
        }
        else
        {
            SceneManager.LoadScene(m_loadScene);
            m_loadScene = null;
        }
    }
}
