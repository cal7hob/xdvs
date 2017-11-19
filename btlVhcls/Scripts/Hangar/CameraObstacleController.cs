using System.Collections.Generic;
using UnityEngine;

public class CameraObstacleController : MonoBehaviour
{
    public float radius;
    public string[] hideOnlyNameContains;
    private readonly HashSet<Collider> storedColliders = new HashSet<Collider>(); 

    void Update()
    {
        HideObjects();
    }

#if UNITY_EDITOR
    void OnDrawGizmos()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawSphere(transform.position, radius);
    }
#endif

    private void HideObjects()
    {
        Collider[] colliders = Physics.OverlapSphere(transform.position, radius);

        storedColliders.ExceptWith(colliders);

        SetRenderersEnabled(colliders, false);
        SetRenderersEnabled(storedColliders, true);

        storedColliders.UnionWith(colliders);

        if (colliders.Length == 0)
            storedColliders.Clear();
    }

    private void SetRenderersEnabled(IEnumerable<Collider> colliders, bool enabled)
    {
        foreach (Collider collider in colliders)
        {
            if (CheckExcluded(collider))
                continue;

            foreach (MeshRenderer renderer in collider.GetComponentsInChildren<MeshRenderer>())
            {
                if (renderer.enabled != enabled)
                    renderer.enabled = enabled;
            }
        }
    }

    private bool CheckExcluded(Collider collider)
    {
        if (hideOnlyNameContains.Length == 0)
            return false;

        foreach (string pattern in hideOnlyNameContains)
        {
            if (collider == null)
                continue;

            if (collider.gameObject.name.Contains(pattern))
                return false;
        }

        return true;
    }
}
