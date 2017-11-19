using UnityEngine;

public class ShadowPlane : MonoBehaviour
{
    public float correctHeight = 0.05f;
    public LayerMask ShadowLayer = 0;

    private const float MAX_ANGLE = 5.0f;

    private Vector3 start = new Vector3(0, 0, 0);
    private Vector3 end = new Vector3(0, 0, 0);

    void Update()
    {
        start.Set(transform.parent.position.x, transform.parent.position.y + 1, transform.parent.position.z);
        end.Set(transform.parent.position.x, transform.parent.position.y - 10, transform.parent.position.z);

        RaycastHit hit;

        if (Physics.Linecast(start, end, out hit, ShadowLayer.value))
        {
            Quaternion previousRotation = transform.rotation;

            transform.position = new Vector3(hit.point.x, hit.point.y + correctHeight, hit.point.z);
            transform.rotation = Quaternion.FromToRotation(Vector3.up, hit.normal);

            transform.Rotate(Vector3.up, transform.parent.eulerAngles.y);

            float angle = Vector3.Angle(transform.parent.up, transform.up);

            if (angle > MAX_ANGLE)
                transform.rotation = previousRotation;
        }
    }
}