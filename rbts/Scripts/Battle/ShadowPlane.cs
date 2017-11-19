using UnityEngine;

public class ShadowPlane : MonoBehaviour
{
    private const float ZOOM_VISIBLE_SQR_DISTANCE = 160000f; // 400^2
    private const float VISIBLE_SQR_DISTANCE = 10000f; // 100^2

    public float correctHeight = 0.05f;
    public LayerMask ShadowLayer = 0;

    private const float MAX_ANGLE = 5.0f;

    private Renderer visibilityChecker;
    private bool hasLodGroup;
    private bool optimizationEnabled;
    private Renderer shadowRenderer;
    private Transform parent;

    private Vector3 start = new Vector3(0, 0, 0);
    private Vector3 end = new Vector3(0, 0, 0);

    void Start()
    {
        shadowRenderer = GetComponentInChildren<Renderer>(true);
        parent = transform.parent;
    }

    void Update()
    {
        if (optimizationEnabled)
        {
            if (visibilityChecker != null && !visibilityChecker.isVisible)
                return;

            if (!hasLodGroup && Vector3.SqrMagnitude(transform.position - Camera.main.transform.position) >
                (BattleCamera.Instance.IsZoomed
                    ? ZOOM_VISIBLE_SQR_DISTANCE
                    : VISIBLE_SQR_DISTANCE))
            {
                SwitchShadowRenderer(false);
                return;
            }

            SwitchShadowRenderer(true);
        }

        start.Set(parent.position.x, parent.position.y + 1, parent.position.z);
        end.Set(parent.position.x, parent.position.y - 10, parent.position.z);

        RaycastHit hit;

        if (Physics.Linecast(start, end, out hit, ShadowLayer.value))
        {
            Quaternion previousRotation = transform.rotation;

            transform.position = new Vector3(hit.point.x, hit.point.y + correctHeight, hit.point.z);
            transform.rotation = Quaternion.FromToRotation(Vector3.up, hit.normal);

            transform.Rotate(Vector3.up, parent.eulerAngles.y);

            float angle = Vector3.Angle(parent.up, transform.up);

            if (angle > MAX_ANGLE)
                transform.rotation = previousRotation;
        }
    }

    public void EngageOptimization()
    {
        optimizationEnabled = true;
        LODGroup lodGroup = GetComponentInChildren<LODGroup>();
        if (lodGroup == null)
        {
            visibilityChecker = GetComponentInChildren<Renderer>();
        }
        else
        {
            visibilityChecker = lodGroup.GetLODs()[0].renderers[0];
            hasLodGroup = true;
        }

        
    }

    private void SwitchShadowRenderer(bool state)
    {
        if (shadowRenderer.enabled != state)
            shadowRenderer.enabled = state;
    }
}