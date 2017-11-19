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

        if (string.IsNullOrEmpty(MapParticles.Instance.GroundDustPrefabPath) || vehicleController == null)
        {
            Destroy(this);
        }
    }

    void Update()
    {
        if (!vehicleController.IsAvailable ||
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
        {
            return;
        }

        for (int i = 0; i < groundContacts.Length; i++)
        {
            if (i < maxParticleSystems)
            {
                var dustEffect = PoolManager.GetObject<ParticleEffect>(MapParticles.Instance.GroundDustPrefabPath);
                dustEffect.transform.position = groundContacts[i].point;
                dustEffect.transform.rotation = Quaternion.identity;
            }
		}

        isDusting = true;
    }

    void OnCollisionEnter(Collision collision)
    {
        groundContacts = collision.contacts;
    }
}
