using System;
using UnityEngine;
#if UNITY_IOS
using UnityEngine.iOS;
using UnityEngine.Apple.ReplayKit;

internal class ReplayIos : ReplayBase
{
    public ReplayIos()
    {
    }

    private void EveryplayOnReadyForRecording(bool enabled)
    {
        RaiseReadyForRecording(enabled);
    }
    
    public override bool IsBroadcasting
    {
        get { return ReplayKit.isBroadcasting; }
    }

    public override bool IsRecordingAvailable
    {
        get { return ReplayKit.recordingAvailable; }
    }

    public override bool IsRecording
    {
        get { return ReplayKit.isRecording; }
    }

    public override bool IsRecordingSupported
    {
        get { return ReplayKit.APIAvailable; }
    }

    public override bool IsBroadcastingSupported
    {
        get { return ReplayKit.broadcastingAPIAvailable; }
    }

    public override bool IsCameraEnabled
    {
        get { return ReplayKit.cameraEnabled; }
    }

    public override void StartRecording()
    {
        ReplayKit.StartRecording(true);
    }

    public override void StopRecording()
    {
        ReplayKit.StopRecording();
    }

    public override void PreviewRecording()
    {
        ReplayKit.Preview();
    }

    public override void ShowCameraPreviewAt(float posX, float posY)
    {
        //ReplayKit.ShowCameraPreviewAt(posX, posY);
    }

    public override void HideCameraPreview()
    {
        //sReplayKit.HideCameraPreview();
    }

    public override void DiscardRecord()
    {
        ReplayKit.Discard();
    }

    public override void StartBroadcasting()
    {
        ReplayKit.StartBroadcasting((success, error) => Debug.Log(string.Format("Start : {0}, error : `{1}`", success, error)), true);
    }

    public override void StopBroadcasting()
    {
        ReplayKit.StopBroadcasting();
    }

}
#endif