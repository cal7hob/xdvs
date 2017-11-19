using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public partial class SoldierController
{
    [Header("MobileProperies")]
    //Облегчает попадание с мобилок
    [SerializeField]
    private float capsuleRadiusMobile = 0.6f;
    [SerializeField]
    private float headBoxRaduisMobile = 0.5f;


    void MakeItEasier()
    {
        if (SystemInfo.deviceType != DeviceType.Handheld)
        {
            return;
        }
        bodyCollider.radius = capsuleRadiusMobile;
        headCollider.size = new Vector3(headBoxRaduisMobile, headBoxRaduisMobile, headBoxRaduisMobile);
    }

}
