using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SwapControls : MonoBehaviour
{
    public GameObject[] objectsToSwap;
    public GameObject anchorMiddleCenter;
    private int swapControls;


    void Start()
    {
        swapControls = PlayerPrefs.GetInt("SwapControls", Settings.DEFAULT_SWAP_CONTROLS_VALUE);
        if (swapControls != 0)
        {
            Swap();
        }
    }

    private void Swap()
    {

        foreach (var obj in objectsToSwap)
        {
            var deltaCenter = anchorMiddleCenter.transform.position.x - obj.transform.position.x;
            obj.transform.position += new Vector3(deltaCenter * 2, 0, 0);
        }
    }
}
