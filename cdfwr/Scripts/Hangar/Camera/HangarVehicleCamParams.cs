using UnityEngine;
using System.Collections;

public class HangarVehicleCamParams : MonoBehaviour
{
    public float MinDistanceToVehicle { get; set; }
    
    public CameraLookPoints CameraLookPoints
    {
        get
        {
            var camLookPoints = transform.Find("CamLookPoints"); // Find, ������ ��� ������ ��� ������������� ����� ������
            return camLookPoints ? camLookPoints.GetComponent<CameraLookPoints>() : null; 
        } 
    }
}
