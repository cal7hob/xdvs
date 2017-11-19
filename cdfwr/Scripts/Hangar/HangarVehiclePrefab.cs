using UnityEngine;

public class HangarVehiclePrefab : MonoBehaviour
{
    [SerializeField] private MeshFilter longestMesh;

    public MeshFilter LongestMesh { get { return longestMesh; } }
}
