using UnityEngine;

public class TankUnderground : MonoBehaviour
{
	void OnTriggerEnter(Collider tankCollider)
	{
	    VehicleController vehicle = tankCollider.GetComponentInParent<VehicleController>();
        if (vehicle != null && vehicle.PhotonView.isMine)
            vehicle.MakeRespawn(true, false, false);
	}
}
