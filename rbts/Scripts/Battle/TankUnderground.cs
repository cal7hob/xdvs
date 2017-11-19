using UnityEngine;

public class TankUnderground : MonoBehaviour
{
	void OnTriggerEnter(Collider collider)
	{
	    if (collider.isTrigger)
	        return;

        VehicleController vehicle = collider.GetComponentInParent<VehicleController>();
        if (vehicle != null && vehicle.PhotonView.isMine)
            vehicle.MakeRespawn(true, false, false);
	}
}
