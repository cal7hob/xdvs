using System;

internal abstract class ReplayBase
{
    public event Action<bool> ReadyForRecording;

    public event Action<bool> ReadyForBroadcasting;
    
    public abstract bool IsBroadcasting { get; }
    public abstract bool IsRecordingAvailable { get; }
    public abstract bool IsRecording { get; }
    public abstract bool IsRecordingSupported { get; }
    public abstract bool IsBroadcastingSupported { get; }
    public abstract bool IsCameraEnabled { get; }

    public abstract void StartRecording();

    public abstract void StopRecording();

    public abstract void StartBroadcasting();

    public abstract void StopBroadcasting();

    public abstract void PreviewRecording();

    protected virtual void RaiseReadyForRecording(bool obj)
    {
        var handler = ReadyForRecording;
        if (handler != null) handler(obj);
    }

    protected virtual void RaiseReadyForBroadcasting(bool obj)
    {
        var handler = ReadyForBroadcasting;
        if (handler != null) handler(obj);
    }

    public abstract void ShowCameraPreviewAt(float posX, float posY);
    public abstract void HideCameraPreview();
    public abstract void DiscardRecord();
}