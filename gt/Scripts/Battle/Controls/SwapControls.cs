using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SwapControls : MonoBehaviour
{
    public GameObject[] objectsToSwap;
    public GameObject[] objectsToRotate;
    public GameObject anchorMiddleCenter;
    private int swapControls;


    void Start()
    {
        swapControls = PlayerPrefs.GetInt("SwapControls", 0);
        if (swapControls != 0)
        {
            Swap();
            Rotate();
        }
    }

    private void Swap()
    {

        foreach (var obj in objectsToSwap)
        {
            if (obj == null)
            {
                continue;
            }
            var deltaCenter = anchorMiddleCenter.transform.position.x - obj.transform.position.x;
            obj.transform.position += new Vector3(deltaCenter * 2, 0, 0);
        }
    }

    private void Rotate()
    {
        foreach (var obj in objectsToRotate)
        {
            if (obj == null)
            {
                continue;
            }
            obj.transform.Rotate(new Vector3(0,180,0));
        }
    }
}
