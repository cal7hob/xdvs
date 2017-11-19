using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ForceReload : MonoBehaviour
{
    public GameObject wrapper;
    void Awake()
    {
        Dispatcher.Subscribe(EventId.MainTankAppeared, OnMainTankAppearedHandler);
    }
    void OnDestroy()
    {
        Dispatcher.Unsubscribe(EventId.MainTankAppeared, OnMainTankAppearedHandler);
    }

    void OnMainTankAppearedHandler(EventId _id, EventInfo _info)
    {
#if UNITY_EDITOR
        wrapper.SetActive(true);
#endif
        if (SystemInfo.deviceType == DeviceType.Handheld)
        {
            wrapper.SetActive(true);
        }
    }

    void Update()
    {
        if (XDevs.Input.GetButtonDown("ForceReload"))
        {
            Reload();
        }
    }

    public void OnClick(tk2dUIItem _btn)
    {
        Reload();
    }


    void Reload()
    {
        Dispatcher.Send(EventId.ForceReload, null);
    }

}
