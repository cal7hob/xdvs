using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class TutorialHolder : MonoBehaviour
{
    public enum CamAnchors
    {
        lowerLeft,
        middleLeft,
        upperLeft
    }


    [SerializeField] private GameObject wrapper;

    public GameObject Wrapper { get { return wrapper; } }
}
