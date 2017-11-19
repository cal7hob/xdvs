using UnityEngine;
using System;
using System.Collections;

public class ScreenExtensions : MonoBehaviour
{
    public enum AspectRatio
    {
        SixteenToNine,
        FourToThree,
        SixteenToTen
    }

    enum State
    {
        Fullscreen,
        Window
    }

    private int width = 0;
    private int height = 0;
    private State state = State.Window;
    private IEnumerator resolutionChangingRoutine;

    public static AspectRatio Aspect
    {
        get
        {
            if (HelpTools.Approximately(Camera.main.aspect, 1.73f, 0.01f))
                return AspectRatio.SixteenToNine;

            if (HelpTools.Approximately(Camera.main.aspect, 1.33f, 0.01f))
                return AspectRatio.FourToThree;

            if (HelpTools.Approximately(Camera.main.aspect, 1.6f, 0.01f))
                return AspectRatio.SixteenToTen;

            return AspectRatio.SixteenToNine;
        }
    }

    void Awake()
    {
        width = Screen.width;
        height = Screen.height;
        state = State.Window;
    }
	
	void Update()
    {
	    if (Screen.width != width || Screen.height != height)
        {
            width = Screen.width;
            height = Screen.height;

            if (resolutionChangingRoutine != null)
                StopCoroutine(resolutionChangingRoutine);

            resolutionChangingRoutine = ChangeResolution();

            StartCoroutine(resolutionChangingRoutine);
        }
        if (Screen.fullScreen && state == State.Window)
        {
            state = State.Fullscreen;
#if UNITY_STANDALONE_OSX || UNITY_STANDALONE_WINDOWS || UNITY_WSA
            Screen.SetResolution(Screen.currentResolution.width, Screen.currentResolution.height, true);//Если заметили выход из полноэкранного режима - принудительно присваиваем нужное разрешение
#endif
            Dispatcher.Send(EventId.WindowModeChanged, null);
        }
        else if (!Screen.fullScreen && state == State.Fullscreen)
        {
            state = State.Window;
#if UNITY_STANDALONE_OSX || UNITY_STANDALONE_WINDOWS || UNITY_WSA
            Screen.SetResolution(800, 600, false);//Если заметили выход из полноэкранного режима - принудительно присваиваем нужное разрешение
#endif
            Dispatcher.Send(EventId.WindowModeChanged, null);
        }
    }

    private IEnumerator ChangeResolution()
    {
        // Пропускаем 2 кадра (один по документации юнити, второй – чтобы тулкитовская камера отдуплила смену разрешения).
        yield return null; 
        yield return null;

        Dispatcher.Send(EventId.ResolutionChanged, null);
    }
}
