using System;
using System.Collections.Generic;
using UnityEngine;
using System.Collections;

public class SpriteHelper : MonoBehaviour
{

    public List<GameObject> ladies;

    void Start()
    {
        if ((int) Math.Ceiling(Camera.main.aspect*100) == 125)
        {
            foreach (var lady in ladies)
            {
                lady.transform.localPosition += Vector3.down*50;
            }
        }
    }
}
