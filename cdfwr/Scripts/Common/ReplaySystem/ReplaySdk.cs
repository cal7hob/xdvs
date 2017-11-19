using System;
using UnityEngine;

public class ReplaySdk
{
    public static event Action<bool> ReadyForRecording;

    public static event Action<bool> ReadyForBroadcasting;

    private static ReplaySdk instance;
    private ReplayBase impl;
    private bool cameraShown;

    private static ReplaySdk Instance
    {
        get
        {
            if (instance == null)
            {
                instance = new ReplaySdk();
            }
            return instance;
        }
    }

    public static bool IsBroadcasting
    {
        get { return Instance.impl.IsBroadcasting; }
    }
    public static bool IsRecordingAvailable
    {
        get { return Instance.impl.IsRecordingAvailable; }
    }

    public static bool IsRecording
    {
        get { return Instance.impl.IsRecording; }
    }

    public static bool IsBroadcastingSupported
    {
        get { return Instance.impl.IsBroadcastingSupported; }
    }

    public static bool IsRecordingSupported
    {
        get { return Instance.impl.IsRecordingSupported; }
    }

    public static bool IsCameraShown
    {
        get { return Instance.cameraShown; }
    }

    private ReplaySdk()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        impl = new ReplayAndroid();
#elif UNITY_IOS && !UNITY_EDITOR
        impl = new ReplayIos();
#else
        impl = new FakeReplayImpl();
#endif
        ;
        impl.ReadyForRecording += OnReadyForRecording;
        impl.ReadyForBroadcasting += OnReadyForBroadcasting;
    }

    private void OnReadyForBroadcasting(bool enabled)
    {
        if (ReadyForBroadcasting != null)
            ReadyForBroadcasting(enabled);
    }

    private void OnReadyForRecording(bool enabled)
    {
        if (ReadyForRecording != null)
            ReadyForRecording(enabled);
    }

    public static void StartRecording()
    {
        Instance.impl.StartRecording();
    }

    public static void StopRecording()
    {
        Instance.impl.StopRecording();
    }

    public static void StopBroadcasting()
    {
        Instance.impl.StopBroadcasting();
    }

    public static void StartBroadcasting()
    {
        Instance.impl.StartBroadcasting();
    }

    public static void PreviewRecording()
    {
        Instance.impl.PreviewRecording();
    }

    public static void ShowCameraPreviewAt(float posX, float posY)
    {
        Instance.impl.ShowCameraPreviewAt(posX, posY);
        Instance.cameraShown = true;
    }

    public static void HideCameraPreview()
    {
        Instance.impl.HideCameraPreview();
        Instance.cameraShown = false;
    }

    public static bool IsCameraEnabled()
    {
        return Instance.impl.IsCameraEnabled;
    }

    public static void DiscardRecord()
    {
        Instance.impl.DiscardRecord();
    }
}