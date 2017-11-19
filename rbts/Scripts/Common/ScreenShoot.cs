using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class ScreenShoot : MonoBehaviour
{
    [Header("Основное")]
    public new string name = "Screenshot";
    public int scale = 1;
    public KeyCode instantShotKey = KeyCode.F12;
    public bool screenOnStart;
    public bool hideGUI;

    [Header("Скрин по таймеру")]
    public bool useTimer;
    public float timerSeconds = 1.0f;
    public KeyCode timerShotKey = KeyCode.F11;

    [Header("Замедление игры / пауза")]
    public KeyCode slowmoKey = KeyCode.P;
    public float slowmoRatio;

    private static ScreenShoot instance;

    private readonly List<Camera> guiCameras = new List<Camera>();

    private int index;
    private float shootTime;
    
    void Awake()
    {
        if (instance != null && instance != this)
            Destroy(gameObject);

        instance = this;

        DontDestroyOnLoad(gameObject);
    }

    void Start()
    {
        SceneManager.sceneLoaded += sceneLoaded;
        if (screenOnStart)
            StartCoroutine(GettingScreenshot());
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= sceneLoaded;
    }

    private void sceneLoaded(Scene scene, LoadSceneMode mode)
    {
        tk2dCamera[] tk2dCameras = FindObjectsOfType<tk2dCamera>();

        guiCameras.Clear();

        foreach (tk2dCamera tk2dCamera in tk2dCameras)
            if (tk2dCamera != null)
                guiCameras.Add(tk2dCamera.gameObject.GetComponent<Camera>());
    }

    void Update()
    {
        if (Input.GetKeyDown(slowmoKey))
            Time.timeScale = !HelpTools.Approximately(Time.timeScale, slowmoRatio) ? slowmoRatio : 1;

        if (Input.GetKeyUp(instantShotKey))
            StartCoroutine(GettingScreenshot());

        if (Input.GetKeyUp(timerShotKey))
        {
            useTimer = !useTimer;
            shootTime = Time.time + timerSeconds;
        }

        if (useTimer && shootTime < Time.time)
        {
            shootTime = Time.time + timerSeconds;
            StartCoroutine(GettingScreenshot());
        }
    }

    private IEnumerator GettingScreenshot()
    {
        index++;

        string filename = string.Format("{0}_{1}.png", name, index);

        if (hideGUI)
        {
            foreach (Camera guiCamera in guiCameras)
                if (guiCamera != null)
                    guiCamera.enabled = false;

            yield return new WaitForEndOfFrame();
        }

        ScreenCapture.CaptureScreenshot(filename, scale);

        if (hideGUI)
        {
            foreach (Camera guiCamera in guiCameras)
                if (guiCamera != null)
                    guiCamera.enabled = true;
        }

        Debug.LogFormat("{0} taken!", filename);
    }
}
