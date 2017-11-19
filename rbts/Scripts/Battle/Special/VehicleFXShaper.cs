using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VehicleFXShaper : MonoBehaviour
{
    private new Collider collider;

    void Awake()
    {
        collider = GetComponent<Collider>();
        if (collider == null)
        {
            Debug.LogError("No collider attached to VehicleFXBox", gameObject);
            Destroy(this);
        }
    }

    public void ConfigurateParticleSystem(ParticleSystem ps)
    {
        ps.transform.SetParent(transform);

        ParticleSystem.MainModule mainModule = ps.main;
        ParticleSystem.ShapeModule shapeModule = ps.shape;

        if (TryBoxCollider(mainModule, shapeModule, ps.transform))
        {
            return;
        }

        if (TrySphereCollider(mainModule, shapeModule, ps.transform))
        {
            return;
        }
    }

    private bool TryBoxCollider(ParticleSystem.MainModule mainModule, ParticleSystem.ShapeModule shapeModule, Transform fxTransform)
    {
        BoxCollider boxCollider = collider as BoxCollider;
        if (boxCollider == null)
        {
            return false;
        }

        shapeModule.shapeType = ParticleSystemShapeType.Box;
        shapeModule.scale = boxCollider.size;
        mainModule.simulationSpace = ParticleSystemSimulationSpace.Local;
        fxTransform.localPosition = boxCollider.center;

        return true;
    }

    private bool TrySphereCollider(ParticleSystem.MainModule mainModule, ParticleSystem.ShapeModule shapeModule, Transform fxTransform)
    {
        SphereCollider sphereCollider = collider as SphereCollider;
        if (sphereCollider == null)
        {
            return false;
        }

        shapeModule.shapeType = ParticleSystemShapeType.Sphere;
        shapeModule.radius = sphereCollider.radius;
        mainModule.simulationSpace = ParticleSystemSimulationSpace.Local;
        fxTransform.localPosition = sphereCollider.center;

        return true;
    }
}
