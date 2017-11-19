using System;
using UnityEditor;

[InitializeOnLoad]
public class EditorPlayMode
{
    private static PlayModeState _currentState = PlayModeState.Stopped;
    public static event Action<PlayModeState, PlayModeState> PlayModeChanged;

    static EditorPlayMode() { EditorApplication.playmodeStateChanged = OnUnityPlayModeChanged; }
    public static void Play() { EditorApplication.isPlaying = true; }
    public static void Pause() { EditorApplication.isPaused = true; }
    public static void Stop() { EditorApplication.isPlaying = false; }
    private static void OnPlayModeChanged(PlayModeState currentState, PlayModeState changedState) { if (PlayModeChanged != null) PlayModeChanged(currentState, changedState); }

    private static void OnUnityPlayModeChanged()
    {
        PlayModeState changedState = PlayModeState.Stopped;
        switch (_currentState)
        {
            case PlayModeState.Stopped:
                if (EditorApplication.isPlayingOrWillChangePlaymode) changedState = PlayModeState.Playing;
                break;
            case PlayModeState.Playing:
                if (EditorApplication.isPaused) changedState = PlayModeState.Paused;
                else changedState = PlayModeState.Stopped;
                break;
            case PlayModeState.Paused:
                if (EditorApplication.isPlayingOrWillChangePlaymode) changedState = PlayModeState.Playing;
                else changedState = PlayModeState.Stopped;
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }

        OnPlayModeChanged(_currentState, changedState);
        _currentState = changedState;
    }
}