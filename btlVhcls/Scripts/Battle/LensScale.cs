using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

[ExecuteInEditMode]
public class LensScale : MonoBehaviour 
{
	public float nearSize = 0.4f;
	public float farSize = 0.085f;
    public float maxDistance = 70.0f;

    private LensFlare lens;
    private float distance;
    private float distanceRatio;
    private float size;

    public Transform CameraTransform
    {
        get
        {
            if (Application.isPlaying)
                return BattleCamera.Instance == null || BattleCamera.Instance.transform == null
                    ? null
                    : BattleCamera.Instance.transform;

            #if UNITY_EDITOR
            return SceneView.lastActiveSceneView == null ? null : SceneView.lastActiveSceneView.camera.transform;
            #else
            return null;
            #endif
        }
    }

    void Awake()
    {
        lens = GetComponent<LensFlare>();
    }

	void Update() 	
	{
        ChangeScale();
	}

    #if UNITY_EDITOR
    void OnRenderObject()
    {
        if (!Application.isPlaying)
            ChangeScale();
    }
    #endif

    private void ChangeScale()
    {
        if (CameraTransform == null)
            return;

        distance = Vector3.Distance(CameraTransform.position, transform.position);

        distanceRatio = distance / maxDistance;
        size = Mathf.Lerp(nearSize, farSize, distanceRatio);

        lens.brightness = size;
    }
}
