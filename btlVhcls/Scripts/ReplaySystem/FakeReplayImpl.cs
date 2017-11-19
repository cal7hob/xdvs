class FakeReplayImpl : ReplayBase
{
    public override bool IsBroadcasting
    {
        get { return false; }
    }

    public override bool IsRecordingAvailable
    {
        get { return false; }
    }

    public override bool IsRecording
    {
        get { return false; }
    }

    public override bool IsRecordingSupported
    {
        get { return false; }
    }

    public override bool IsBroadcastingSupported
    {
        get { return false; }
    }

    public override bool IsCameraEnabled
    {
        get { return false; }
    }

    public override void StartRecording()
    {
    }

    public override void StopRecording()
    {
    }

    public override void StartBroadcasting()
    {
    }

    public override void StopBroadcasting()
    {
    }

    public override void PreviewRecording()
    {
    }

    public override void ShowCameraPreviewAt(float posX, float posY)
    {
        
    }

    public override void HideCameraPreview()
    {
        
    }

    public override void DiscardRecord()
    {
        
    }
}