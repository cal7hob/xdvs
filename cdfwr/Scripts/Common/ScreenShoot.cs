using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ScreenShoot :MonoBehaviour
{
    [Header("Основное")] public new string name = "dffsrdgg";
    public int scale = 1;
    public KeyCode instantShotKey = KeyCode.F12;
    public bool screenOnStart;
    public bool hideGUI;

    [Header("Скрин по таймеру")] public bool useTimer;
    public float timerSeconds = 1.0f;
    public KeyCode timerShotKey = KeyCode.F11;

    [Header("Замедление игры / пауза")] public KeyCode slowmoKey = KeyCode.P;
    public float slowmoRatio;

    private static ScreenShoot instance;

    private readonly List<Camera> guiCameras = new List<Camera>();

    private int index;
    private float shootTime;
    

void Awake()
    {
        Start();
        //if (instance != null && instance != this)
        //    Destroy(gameObject);

        //instance = this;

        //DontDestroyOnLoad(gameObject);
    }

    void Start()
    {
        if (screenOnStart)
            StartCoroutine(GettingScreenshot());
    }

    public void GetScreenShot()
    {
        StartCoroutine(GettingScreenshot());
    }
    void OnLevelWasLoaded()
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

        Application.CaptureScreenshot(filename, scale);

        if (hideGUI)
        {
            foreach (Camera guiCamera in guiCameras)
                if (guiCamera != null)
                    guiCamera.enabled = true;
        }

        Debug.LogFormat("{0} taken!", filename);
    }
}
