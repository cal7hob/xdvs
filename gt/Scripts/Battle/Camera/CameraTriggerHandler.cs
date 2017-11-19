using UnityEngine;

public class CameraTriggerHandler : MonoBehaviour
{
    [SerializeField] private BattleCamera battleCamera;

    void OnTriggerEnter(Collider collider)
    {
        var vehicleInView = battleCamera.VehicleInView;

        if (collider.tag != "CritZone" && collider.transform.IsChildOf(vehicleInView.transform))
        {
            vehicleInView.IsVisible = false;
        }
    }

    void OnTriggerExit(Collider collider)
    {
        var vehicleInView = battleCamera.VehicleInView;

        if (!battleCamera.IsZoomed && collider.tag != "CritZone" && collider.transform.IsChildOf(vehicleInView.transform))
        {
            vehicleInView.IsVisible = true;
        }
    }

}
