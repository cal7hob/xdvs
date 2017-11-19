using Pool;
using UnityEngine;

public class DustController : MonoBehaviour
{
    public int maxParticleSystems = 1;
    public float angularVelocityThreshold = 0.5f;

    private bool isDusting;
    private ContactPoint[] groundContacts;
    private VehicleController vehicleController;

    void Awake()
    {
        vehicleController = GetComponent<VehicleController>();
        Debug.Log("Dust controller", gameObject);
    }

    void Update()
    {
        if (
            MapParticles.Instance == null ||
            MapParticles.Instance.GroundDust == null ||
            vehicleController == null ||
            !vehicleController.IsAvailable ||
            groundContacts == null)
        {
            return;
        }

        if (Mathf.Abs(vehicleController.LocalAngularVelocity.x) < angularVelocityThreshold &&
            Mathf.Abs(vehicleController.LocalAngularVelocity.z) < angularVelocityThreshold)
        {
            isDusting = false;
            return;
        }

        if (isDusting)
            return;

        for (int i = 0; i < groundContacts.Length; i++)
            if (i < maxParticleSystems)
                PoolManager.GetObject<PoolEffect>(
                    MapParticles.Instance.GroundDust,
                    groundContacts[i].point,
                    Quaternion.identity);

        isDusting = true;
    }

    void OnCollisionEnter(Collision collision)
    {
        groundContacts = collision.contacts;
    }
}
