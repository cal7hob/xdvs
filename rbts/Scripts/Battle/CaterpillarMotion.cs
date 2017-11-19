using System;
using UnityEngine;
using System.Collections.Generic;

public class CaterpillarMotion : MonoBehaviour
{
    private const float ZOOM_VISIBLE_SQR_DISTANCE = 160000f; // 400^2
    private const float VISIBLE_SQR_DISTANCE = 10000f; // 100^2

    public GameObject caterpillarLeft;
    public GameObject caterpillarRight;
    public GameObject caterpillarLeft2;
    public GameObject caterpillarRight2;

    public GameObject wheelsLeft;
    public GameObject wheelsRight;
	public bool perpendicular;
    public float trackSpinSpeed = 0.4f;
    public float turningGain = 4.0f;
    public float wheelSpinSpeed = 80.0f;

    private VehicleController vehicleController;
    private Renderer visibilityChecker;
    private bool hasLodGroup;
    private Renderer[] caterpillarLeftRenderers;
    private Renderer[] caterpillarRightRenderers;
    private Transform[] cachedWheelsLeft;
    private Transform[] cachedWheelsRight;
    private bool optimizationEnabled;
    
    void OnPhotonInstantiate()
    {
        vehicleController = GetComponent<VehicleController>();
        CacheMaterials();
        CacheWheels();
        EngageOptimization();

        Messenger.Subscribe(EventId.QualitySettingsChanged, OnQualitySettingsChanged);
    }

    void OnDestroy()
    {
        DestroyMaterials(caterpillarLeftRenderers);
        DestroyMaterials(caterpillarRightRenderers);
        
        Messenger.Unsubscribe(EventId.QualitySettingsChanged, OnQualitySettingsChanged);
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.T))
            CacheMaterials();

        if (optimizationEnabled)
        {
            if (visibilityChecker != null && !visibilityChecker.isVisible)
                return;

            if (!hasLodGroup && Vector3.SqrMagnitude(transform.position - Camera.main.transform.position) >
                (BattleCamera.Instance.IsZoomed
                    ? ZOOM_VISIBLE_SQR_DISTANCE
                    : VISIBLE_SQR_DISTANCE))
                return;
        }

        float leftTrackOffset = 0;
        float rightTrackOffset = 0;

        float speed = vehicleController.LocalVelocity.z;
        float angularSpeed = vehicleController.LocalAngularVelocity.y;

        bool idle = HelpTools.Approximately(speed, 0, 0.4f);

        if (!idle)
        {
            leftTrackOffset += speed;
            rightTrackOffset += speed;
        }

        if (!HelpTools.Approximately(angularSpeed, 0))
        {
            leftTrackOffset += angularSpeed * (turningGain * (angularSpeed < 0 && !idle ? 0 : 1));
            rightTrackOffset -= angularSpeed * (turningGain * (angularSpeed > 0 && !idle ? 0 : 1));
        }

        TracksControl(caterpillarLeftRenderers, leftTrackOffset);
        TracksControl(caterpillarRightRenderers, rightTrackOffset);

        WheelsControl(cachedWheelsLeft, leftTrackOffset);
        WheelsControl(cachedWheelsRight, rightTrackOffset);
    }

    private void EngageOptimization()
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

    private Transform[] GetWheelTransforms(GameObject parentGO)
    {
        if (parentGO == null)
            return null;

        Transform parentTransform = parentGO.transform;

        List<Transform> result = new List<Transform>(parentTransform.childCount);

        foreach (Transform child in parentTransform)
        {
            result.Add(child);
        }

        return result.ToArray();
    }

    private void OnQualitySettingsChanged(EventId id, EventInfo ei)
    {
        CacheMaterials();
    }

    private void CacheWheels()
    {
        cachedWheelsLeft = GetWheelTransforms(wheelsLeft);
        cachedWheelsRight = GetWheelTransforms(wheelsRight);
    }

    private void CacheMaterials()
    {
        caterpillarLeftRenderers = CollectRenderers(caterpillarLeft, caterpillarLeft2);
        caterpillarRightRenderers = CollectRenderers(caterpillarRight, caterpillarRight2);
    }

    private Renderer[] CollectRenderers(params GameObject[] gameObjects)
    {
        List<Renderer> trackRenderers = new List<Renderer>(2);
        foreach (var go in gameObjects)
        {
            if (go != null)
            {
                Renderer rend = go.GetComponentInChildren<Renderer>(true);
                trackRenderers.Add(rend);
            }
        }

        return trackRenderers.Count == 0 ? null : trackRenderers.ToArray();
    }

    private void TracksControl(Renderer[] trackRenderers, float offset)
    {
        if (trackRenderers == null)
            return;

        Vector2 direction = perpendicular ? Vector2.right : Vector2.up;
        foreach (Renderer trackMaterial in trackRenderers)
        {
            trackMaterial.material.mainTextureOffset += direction * offset * trackSpinSpeed * Time.deltaTime;
        }
    }

    private void WheelsControl(Transform[] wheels, float offset)
    {
        if (wheels == null)
            return;

        foreach (Transform wheel in wheels)
            wheel.Rotate(offset * wheelSpinSpeed * Time.deltaTime, 0, 0, Space.Self);
    }

    private void DestroyMaterials(Renderer[] renderers)
    {
        if (renderers == null)
            return;

        foreach (var rend in renderers)
        {
            Destroy(rend.material);
        }
    }
}
