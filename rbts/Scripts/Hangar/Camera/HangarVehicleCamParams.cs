using UnityEngine;
using System.Collections;

public class HangarVehicleCamParams : MonoBehaviour
{
    [SerializeField] private float minDistanceToVehicle;
    [SerializeField] private float height = 0;
    public float MinDistanceToVehicle { get { return minDistanceToVehicle; } }
    public float Height { get { return height; } }

    public CameraLookPoints CameraLookPoints
    {
        get
        {
            var camLookPoints = transform.Find("CamLookPoints"); // Find, ������ ��� ������ ��� ������������� ����� ������

            return camLookPoints ? camLookPoints.GetComponent<CameraLookPoints>() : null; 
        } 
    }
}
