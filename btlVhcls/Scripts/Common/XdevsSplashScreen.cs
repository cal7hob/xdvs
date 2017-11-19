using UnityEngine;
using System.Collections.Generic;

public class XdevsSplashScreen : MonoBehaviour 
{
    [SerializeField] private GameObject wrapper;
    [SerializeField] private tk2dTextMesh lblLoading;
    [SerializeField] private tk2dTextMesh lblSplashAdvice;
    [SerializeField] private Texture2D defaultSplash;
    public Transform waitingIndicatorParent;
    public ProgressBar downloadBundlesProgressBar;
    public tk2dTextMesh downloadBundlesProgressText;

    [SerializeField] private Color downloadBundlesTextColor = Color.white;
    [SerializeField] private Color downloadBundlesValueColor = Color.white;

    public WaitingIndicatorBase WaitingIndicator { get; private set; }
    public GameObject Wrapper { get { return wrapper; } }
    public Camera GuiCamera { get { return GetComponent<Camera>(); } }

    #region For Bundles Loading
    private const float BYTES_IN_MB = 1048576;
    public static float LoadedBundlesFullSize { get { return LoadedBundlesFullSizeInBytes / BYTES_IN_MB; } }// in MB
    public static float LoadedBundlesDownloadedSize { get { return LoadedBundlesDownloadedSizeInBytes / BYTES_IN_MB; } }// in MB

    public static bool IsBundlesLoaded { get { return Instance.m_isBundlesLoaded; } set { Instance.m_isBundlesLoaded = value; } }
    public static long LoadedBundlesFullSizeInBytes {// in bytes
        get { return Instance.m_budlesFullBytes; }
        set { Instance.m_budlesFullBytes = value; }
    }
    public static long LoadedBundlesDownloadedSizeInBytes {// in bytes
        get { return Instance.m_budlesDownlodedBytes; }
        set { Instance.m_budlesDownlodedBytes = value; }
    }

    bool m_isBundlesLoaded = true;
    long m_budlesFullBytes = 0L;
    long m_budlesDownlodedBytes = 0L;
    #endregion

    private const string RESOURCES_PATH = "Other";

    public static XdevsSplashScreen Instance { get; private set; }
    
    private void Awake()
    {
        if(Instance != null)
        {
            Destroy(gameObject);
            return;
        }
        Debug.LogFormat("[XdevsSplashScreen] Awake");
        Instance = this;
        DontDestroyOnLoad(gameObject);

        transform.localPosition = Vector3.zero;
        gameObject.name = gameObject.name.Replace("(Clone)", "");

        if (downloadBundlesProgressBar)
            downloadBundlesProgressBar.gameObject.SetActive(!IsBundlesLoaded);
        if (downloadBundlesProgressText)
            downloadBundlesProgressText.text = "";

        SetLabelsVisibility(false);

        if (!WaitingIndicator)
        {
            Debug.LogFormat("[XdevsSplashScreen] Loading waiting indicator...");
            GameObject go = Instantiate(Resources.Load(string.Format("{0}/{1}/WaitingIndicator{2}", GameManager.CurrentResourcesFolder, RESOURCES_PATH, GameData.InterfaceShortName.ToUpper()))) as GameObject;
            if (go)
            {
                Debug.LogFormat("[XdevsSplashScreen] Load waiting indicator success!");
                go.transform.SetParent(waitingIndicatorParent);
                go.transform.localPosition = Vector3.zero;
                WaitingIndicator = go.GetComponent<WaitingIndicatorBase>();
            }
        }
    }

    private void OnDestroy()
    {
        Debug.LogFormat("[XdevsSplashScreen] Destroy");
        Instance = null;//поидее не нужно, т.к. уничтожение происходит только при выходе из приложения
    }

    public static void SetActive(bool en, bool showLabels = false)
    {
        if(Instance == null)
        {
            DT.LogError("SplashScreen.Instance == null!");
            return;
        }
        if (en && IsShowed)//Повторно не включаем, чтобы не регенерировать совет.
            return;
        Instance.wrapper.SetActive(en);
        SetLabelsVisibility(showLabels);
    }

    public static bool IsShowed
    {
        get
        {
            if (Instance == null)
                return false;
            return Instance.wrapper.activeSelf;
        }
    }

    public static void SetActiveWaitingIndicator(bool en,Transform parent = null, Vector3? defaultPosition = null)
    {
        //DT.Log("SetActiveWaitingIndicator({0}). Parent = {1}",en, parent == null ? "Default" : parent.name);
        if (Instance == null)
        {
            DT.LogError("SetActiveWaitingIndicator(). Instance == null!!!");
            return;
        }
            
        if (Instance.WaitingIndicator == null)//if waiting indicator was forgotten on other scene in some transform...
        {
            DT.LogError("SetActiveWaitingIndicator(). Instance.waitingIndicator == null!!!");
            return;
        }
        if(en)
        {
            Instance.WaitingIndicator.Show(parent, defaultPosition);
        }
        else
        {
            Instance.WaitingIndicator.Hide();
        }
    }

    public static void SetLabelsVisibility(bool en)
    {
        if (Instance == null)
            return;
        
        if (en && ( !Instance.downloadBundlesProgressBar || !Instance.downloadBundlesProgressBar.gameObject.activeSelf))
        {
            Instance.lblLoading.text = Localizer.GetText("lblDownloading");
            Instance.lblSplashAdvice.text = AdviceManager.GetRandomAdvice();
        } 
        else
        {
            Instance.lblLoading.text = "";
            Instance.lblSplashAdvice.text = "";
        }
    }


    string m_progressString;
    private void Update()
    {
        if (!IsShowed)
            return;

        if (!IsBundlesLoaded && downloadBundlesProgressBar)
        {
            if(Mathf.Approximately(LoadedBundlesFullSize, 0))//division by zero protection
            {
                downloadBundlesProgressBar.gameObject.SetActive(false);
                return;
            }

            if (!downloadBundlesProgressBar.gameObject.activeSelf)
                downloadBundlesProgressBar.gameObject.SetActive(true);

            float progress = LoadedBundlesDownloadedSize / LoadedBundlesFullSize;
            downloadBundlesProgressBar.Percentage = progress;
            
            if (downloadBundlesProgressText)
            {
                if (string.IsNullOrEmpty (m_progressString)) {
                    m_progressString = "{0}{1}{2}% ({3}MiB / {4}MiB)";
                    if (Localizer.Loaded) {
                        m_progressString = Localizer.GetText("lblDownloadBundlesText", "{0}", "{1}", "{2}", "{3}", "{4}");
                    }
                }
                downloadBundlesProgressText.text = string.Format (m_progressString,
                   downloadBundlesTextColor.To2DToolKitColorFormatString(),
                   downloadBundlesValueColor.To2DToolKitColorFormatString(),
                   string.Format("{0}", (int)Mathf.Floor(Mathf.Clamp(progress * 100f, 0, 100))),
                   string.Format("{0:0.00}", LoadedBundlesDownloadedSize),
                   string.Format("{0:0.00}", LoadedBundlesFullSize));
            }

            if (Mathf.Approximately(progress, 1))
                downloadBundlesProgressBar.gameObject.SetActive(false);
        }
    }
}
