using UnityEngine;

public class ZeroTransformForCameras : MonoBehaviour
{
    private void Awake()
    {
        transform.localPosition = Vector3.zero;
    }
}
