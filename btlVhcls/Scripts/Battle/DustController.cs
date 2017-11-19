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
    }

    void Update()
    {
        if (MapParticles.Instance.GroundDust == null ||
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
                EffectPoolDispatcher.GetFromPool(
                    _effect:    MapParticles.Instance.GroundDust,
                    _position:  groundContacts[i].point,
                    _rotation:  Quaternion.identity);

        isDusting = true;
    }

    void OnCollisionEnter(Collision collision)
    {
        groundContacts = collision.contacts;
    }
}
