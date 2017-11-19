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
    [SerializeField] private List<tk2dCameraAnchor> anchors = new List<tk2dCameraAnchor>();

    public GameObject Wrapper { get { return wrapper; } }
    public List<tk2dCameraAnchor> Anchors { get { return anchors; } } 
}
