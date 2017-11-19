using System;
using UnityEngine;

internal class ReplayAndroid : ReplayBase
{
    private bool isRecordingAvailable = false;

    public ReplayAndroid()
    {
        Everyplay.ReadyForRecording += EveryplayOnReadyForRecording;
    }

    private void EveryplayOnReadyForRecording(bool enabled)
    {
        RaiseReadyForRecording(enabled);
    }
    
    public override bool IsBroadcasting
    {
        get { return false; }
    }

    public override bool IsRecordingAvailable
    {
        get { return isRecordingAvailable; }
    }

    public override bool IsRecording
    {
        get { return Everyplay.IsRecording(); }
    }

    public override bool IsRecordingSupported
    {
        get { return Everyplay.IsRecordingSupported(); }
    }

    public override bool IsBroadcastingSupported
    {
        get { return false; }
    }

    public override bool IsCameraEnabled
    {
        get { return Everyplay.FaceCamIsRecordingPermissionGranted(); }
    }

    public override void StartRecording()
    {
        Everyplay.StartRecording();
    }

    public override void StopRecording()
    {
        Everyplay.StopRecording();
        Everyplay.SetMetadata("", "");
        isRecordingAvailable = true;
    }

    public override void PreviewRecording()
    {
        Everyplay.ShowSharingModal();
        isRecordingAvailable = false;
    }

    public override void ShowCameraPreviewAt(float posX, float posY)
    {
        Everyplay.FaceCamSetPreviewOrigin(Everyplay.FaceCamPreviewOrigin.TopLeft);
        Everyplay.FaceCamSetPreviewScaleRetina(true);
        Everyplay.FaceCamSetPreviewBorderWidth(2);
        Everyplay.FaceCamSetPreviewVisible(true);

        Everyplay.FaceCamSetPreviewSideWidth((int) (Screen.width * 0.15f));
        Everyplay.FaceCamSetPreviewPositionX((int) posX);
        Everyplay.FaceCamSetPreviewPositionY((int) posY);

        if (!Everyplay.FaceCamIsRecordingPermissionGranted())
        {
            Everyplay.FaceCamRecordingPermission += EveryplayOnFaceCamRecordingPermission;
            Everyplay.FaceCamRequestRecordingPermission();
        }
        else
        {
            Everyplay.FaceCamStartSession();
        }
    }

    private void EveryplayOnFaceCamRecordingPermission(bool granted)
    {
        if (granted)
            Everyplay.FaceCamStartSession();
    }

    public override void HideCameraPreview()
    {
        Everyplay.FaceCamStopSession();
        Everyplay.SetMetadata("","");
    }

    public override void DiscardRecord()
    {
        isRecordingAvailable = false;
    }

    public override void StartBroadcasting()
    {
    }

    public override void StopBroadcasting()
    {
    }

}