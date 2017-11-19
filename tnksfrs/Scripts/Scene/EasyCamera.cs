using UnityEngine;

public class EasyCamera : MonoBehaviour
{
    new private Camera camera;

    public void Set(Camera sourceCamera)
    {
        camera = GetComponent<Camera>();

        if (camera == null)
        {
            Debug.LogError(name + " doesn't contain Camera component!");
            return;
        }

        camera.transform.position = sourceCamera.transform.position;
        camera.transform.rotation = sourceCamera.transform.rotation;
        camera.fieldOfView = sourceCamera.fieldOfView;
        camera.orthographic = sourceCamera.orthographic;
    }
}
