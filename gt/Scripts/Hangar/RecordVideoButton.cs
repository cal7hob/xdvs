using System;
using Http;
using UnityEngine;
using System.Collections;

public class RecordVideoButton : MonoBehaviour
{
    private tk2dUIItem uiItem;

    [SerializeField]
    private VideoRecordMenuBehaviour menu;

    [SerializeField]
    private tk2dSlicedSprite recordIndicator;

    private bool waitForVideo = false;

    private void Awake()
    {
        if (!ReplaySdk.IsRecordingSupported)
        {
            gameObject.SetActive(false);
            ReplaySdk.ReadyForRecording += ReplaySdkOnReadyForRecording;
        }

        uiItem = GetComponent<tk2dUIItem>();

        uiItem.OnClickUIItem += Clicked;

    }

    public void Update()
    {
        bool isRecordActive = ReplaySdk.IsRecording || ReplaySdk.IsBroadcasting;

        recordIndicator.gameObject.SetActive(isRecordActive);

        if (ReplaySdk.IsRecordingAvailable && waitForVideo)
        {
            waitForVideo = false;
            StartCoroutine(WaitPreview());
        }
    }

    IEnumerator WaitPreview()
    {
        yield return new WaitForSeconds(0f);
        ReplaySdk.PreviewRecording();
    }

    private void ReplaySdkOnReadyForRecording(bool b)
    {
        if (b) gameObject.SetActive(true);
    }

    private void OnDestroy()
    {
        uiItem.OnClickUIItem -= Clicked;
    }

    private void Clicked(tk2dUIItem tk2dUiItem)
    {
        if (ReplaySdk.IsRecording)
        {
            ReplaySdk.StopRecording();
            waitForVideo = true;
            //ReplaySdk.HideCameraPreview();
        }
        else if (ReplaySdk.IsBroadcasting)
        {
            ReplaySdk.StopBroadcasting();
            //ReplaySdk.HideCameraPreview();
        }
        else if (ReplaySdk.IsBroadcastingSupported)
        {
            //menu.ShowContextMenu(uiItem, null);
            StartBroadcasting();
        }
        else
        {
            StartRecording();
        }
    }

    private void StartRecording()
    {
        menu.HideContextMenu();
        if (ReplaySdk.IsRecordingAvailable)
        {
            StartCoroutine(DiscardRecordAndStart());
            return;
        }
        ReplaySdk.StartRecording();
        //ReplaySdk.ShowCameraPreviewAt(10, 10);
    }

    IEnumerator DiscardRecordAndStart()
    {
        ReplaySdk.DiscardRecord();
        while (ReplaySdk.IsRecordingAvailable)
            yield return null;
        yield return new WaitForSeconds(1.0f);
        ReplaySdk.StartRecording();
        //ReplaySdk.ShowCameraPreviewAt(10, 10);
    }

    private void StartBroadcasting()
    {
        //ReplaySdk.ShowCameraPreviewAt(10, 10);
        ReplaySdk.StartBroadcasting();
        menu.HideContextMenu();
    }
}
