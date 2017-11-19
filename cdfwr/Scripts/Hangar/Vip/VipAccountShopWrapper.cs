using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VipAccountShopWrapper : MonoBehaviour {

    void Start()
    {
        Dispatcher.Send(EventId.VipShopOpen, null);
    }
}
