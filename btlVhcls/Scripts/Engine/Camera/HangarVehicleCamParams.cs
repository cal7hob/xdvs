using UnityEngine;

public class HangarVehicleCamParams : MonoBehaviour
{
    [SerializeField] private float minDistanceToVehicle;

    public float MinDistanceToVehicle { get { return minDistanceToVehicle; } }
    
    public CameraLookPoints CameraLookPoints
    {
        get
        {
            Transform camLookPoints = transform.Find(CamLookTransform.CAM_LOOK_POINTS_PATH); // Find(), потому что каждый раз подсовывается новая модель.
            return camLookPoints ? camLookPoints.GetComponent<CameraLookPoints>() : null; 
        } 
    }
}
