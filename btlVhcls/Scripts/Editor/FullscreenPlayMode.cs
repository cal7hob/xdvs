using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;

/// <summary>
/// При запуске игры в редакторе, разворачивает вкладку Game на весь экран.
/// </summary>
[InitializeOnLoad]
public class FullscreenPlayMode : MonoBehaviour
{
    private const int TAB_HEIGHT = 22;

    static FullscreenPlayMode()
    {
        // Раскомментить, чтобы всё работало:
        //EditorApplication.playmodeStateChanged -= CheckPlayModeState;
        //EditorApplication.playmodeStateChanged += CheckPlayModeState;
    }

    private static void CheckPlayModeState()
    {
        if (EditorApplication.isPlaying)
        {
            FullScreenGameWindow();
        }
        else
        {
            // Раскомментить, если нужно, чтобы вкладка Game закрывалась после стопа игры.
            //CloseGameWindow();
        }
    }

    private static EditorWindow GetMainGameView()
    {
        EditorApplication.ExecuteMenuItem("Window/Game");

        Type type = Type.GetType("UnityEditor.GameView,UnityEditor");

        MethodInfo getMainGameView = type.GetMethod("GetMainGameView", BindingFlags.NonPublic | BindingFlags.Static);

        object result = getMainGameView.Invoke(null, null);

        return (EditorWindow)result;
    }

    private static void FullScreenGameWindow()
    {
        EditorWindow gameView = GetMainGameView();

        gameView.titleContent = new GUIContent("Game (Stereo)");

        Rect newPos
            = new Rect(
                x:      1920, // Отступ. Если нужно развернуть на первом мониторе – 0, если на втором – 1920 (если разрешение первого монитора 1920).
                y:      0 - TAB_HEIGHT,
                width:  Screen.currentResolution.width,
                height: Screen.currentResolution.height + TAB_HEIGHT);

        gameView.position = newPos;
        gameView.minSize = new Vector2(Screen.currentResolution.width, Screen.currentResolution.height + TAB_HEIGHT);
        gameView.maxSize = gameView.minSize;
        gameView.position = newPos;

    }

    private static void CloseGameWindow()
    {
        EditorWindow gameView = GetMainGameView();
        gameView.Close();
    }
}