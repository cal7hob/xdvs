using UnityEngine;

/// <summary>
/// Костыль, который навешивает слетающую ссылку MeshCollider.mesh для объектов tk2d.
/// </summary>
public class tk2dMeshColliderCrutch : MonoBehaviour
{
    void Awake() { GetComponent<MeshCollider>().sharedMesh = GetComponent<MeshFilter>().sharedMesh; }
}
