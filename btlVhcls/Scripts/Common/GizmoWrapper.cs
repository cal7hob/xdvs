using UnityEngine;

public class GizmoWrapper : MonoBehaviour
{
    private float size;
    private Color color;

    void OnDrawGizmos()
    {
        Gizmos.color = color;
        Gizmos.DrawSphere(transform.position, size);
    }

    public void SetColorAndSize(Color color, float size)
    {
        this.color = color;
        this.size = size;
    }
}