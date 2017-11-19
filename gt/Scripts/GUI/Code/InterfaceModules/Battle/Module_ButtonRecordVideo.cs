using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Module_ButtonRecordVideo : InterfaceModuleBase
{

    [SerializeField]
    private ActivatedUpDownButton btn;

    [SerializeField]
    private GameObject menu;

    [SerializeField]
    private tk2dSlicedSprite recordIndicator;

    [SerializeField]
    private Camera guiCamera;

    [SerializeField]
    protected Renderer sprBgRenderer;

    private Rect contextMenuRect;
    private bool waitForVideo;

    protected override void Awake()
    {
        base.Awake();

        Dispatcher.Subscribe(EventId.OnStatTableChangeVisibility, OnStatTableChangeVisibility);
        Dispatcher.Subscribe(EventId.MainTankAppeared, OnMainTankAppeared);

        btn.uiItem.OnClickUIItem += Clicked;
        if (Application.platform == RuntimePlatform.Android && ReplaySdk.IsRecording)
        {
            btn.Activated = false;
        }
            
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

        HideOnTouchOutsideBounds();
    }

    IEnumerator WaitPreview()
    {
        yield return new WaitForSeconds(1.0f);
        ReplaySdk.PreviewRecording();
    }

    private void HideOnTouchOutsideBounds()
    {
        if (!menu.activeSelf) return;

#if UNITY_EDITOR
        if (!Input.GetMouseButtonDown(0)) return;
        if (contextMenuRect.Contains(Input.mousePosition)) return;
#else
        if (Input.touchCount <= 0) return;
        if (contextMenuRect.Contains(Input.GetTouch(0).position)) return;
#endif
        menu.SetActive(false);
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
        btn.uiItem.OnClickUIItem -= Clicked;
        Dispatcher.Unsubscribe(EventId.OnStatTableChangeVisibility, OnStatTableChangeVisibility);
        Dispatcher.Unsubscribe(EventId.MainTankAppeared, OnMainTankAppeared);
    }

    private void OnMainTankAppeared(EventId id, EventInfo info)
    {
        bool active = (Application.platform == RuntimePlatform.Android && ReplaySdk.IsRecordingSupported) ||
                      (Application.platform == RuntimePlatform.IPhonePlayer && ReplaySdk.IsBroadcasting);
        SetActive(active);
    }

    private void OnStatTableChangeVisibility(EventId id, EventInfo info)
    {
        bool active = (Application.platform == RuntimePlatform.Android && ReplaySdk.IsRecordingSupported) ||
                      (Application.platform == RuntimePlatform.IPhonePlayer && ReplaySdk.IsBroadcasting);
        SetActive(!((EventInfo_B)info).bool1 && active);
    }

    private void Clicked(tk2dUIItem tk2dUiItem)
    {
        if (ReplaySdk.IsRecording)
        {
            ReplaySdk.StopRecording();
            waitForVideo = true;
        }
        else if (ReplaySdk.IsBroadcasting)
        {
            Debug.Log("ReplaySdk.IsBroadcasting");
            SetActive(false);
            ReplaySdk.StopBroadcasting();
            //ReplaySdk.HideCameraPreview();
        }
        else if (ReplaySdk.IsBroadcastingSupported)
        {
            //ShowMenu();
            //StartBroadcasting();
        }
        else
        {
            StartRecording();
        }
        if (Application.platform == RuntimePlatform.IPhonePlayer)
            SetActive(false);
    }

    private void ShowMenu()
    {
        menu.SetActive(true);
        var contextMenuBounds = sprBgRenderer.bounds;
        contextMenuRect = new Rect()
        {
            xMin = guiCamera.WorldToScreenPoint(contextMenuBounds.min).x,
            xMax = guiCamera.WorldToScreenPoint(contextMenuBounds.max).x,
            yMin = guiCamera.WorldToScreenPoint(contextMenuBounds.min).y,
            yMax = guiCamera.WorldToScreenPoint(contextMenuBounds.max).y,
        };
    }

    private void StartRecording()
    {
        if (ReplaySdk.IsRecordingAvailable)
        {
            ReplaySdk.DiscardRecord();
        }
        //ReplaySdk.ShowCameraPreviewAt(10, 10);
        ReplaySdk.StartRecording();
        btn.Activated = false;
    }

    private void StartBroadcasting()
    {
        //ReplaySdk.ShowCameraPreviewAt(10, 10);
        ReplaySdk.StartBroadcasting();
        menu.SetActive(false);
    }
}
