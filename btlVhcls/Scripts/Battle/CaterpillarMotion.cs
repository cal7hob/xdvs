using UnityEngine;

public class CaterpillarMotion : MonoBehaviour
{
    public GameObject caterpillarLeft;
    public GameObject caterpillarRight;
    public GameObject caterpillarLeftLOD;
    public GameObject caterpillarRightLOD;
    public GameObject wheelsLeft;
    public GameObject wheelsRight;
	public bool perpendicular;
    public float trackSpinSpeed = 0.4f;
    public float turningGain = 4.0f;
    public float wheelSpinSpeed = 80.0f;

    private VehicleController vehicleController;
    private Material caterpillarLeftMaterial;
    private Material caterpillarRightMaterial;
    private Material caterpillarLeftMaterialLOD;
    private Material caterpillarRightMaterialLOD;

    void Awake()
    {
        vehicleController = GetComponent<VehicleController>();

        CacheMaterials();

        Dispatcher.Subscribe(EventId.QualitySettingsChanged, OnQualitySettingsChanged);
    }

    void OnDestroy()
    {
        Dispatcher.Unsubscribe(EventId.QualitySettingsChanged, OnQualitySettingsChanged);
    }

    void Update()
    {
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

        TracksControl(caterpillarLeftMaterial, leftTrackOffset);
        TracksControl(caterpillarRightMaterial, rightTrackOffset);
        TracksControl(caterpillarLeftMaterialLOD, leftTrackOffset);
        TracksControl(caterpillarRightMaterialLOD, rightTrackOffset);

        WheelsControl(wheelsLeft, leftTrackOffset);
        WheelsControl(wheelsRight, rightTrackOffset);
    }

    private void OnQualitySettingsChanged(EventId id, EventInfo ei)
    {
        CacheMaterials();
    }

    private void CacheMaterials()
    {
        if (caterpillarLeft != null)
            caterpillarLeftMaterial = caterpillarLeft.GetComponent<Renderer>().material;

        if (caterpillarRight != null)
            caterpillarRightMaterial = caterpillarRight.GetComponent<Renderer>().material;

        if (caterpillarLeftLOD != null)
            caterpillarLeftMaterialLOD = caterpillarLeftLOD.GetComponent<Renderer>().material;

        if (caterpillarRightLOD != null)
            caterpillarRightMaterialLOD = caterpillarRightLOD.GetComponent<Renderer>().material;
    }

    private void TracksControl(Material track, float offset)
    {
        if (track == null)
            return;

        Vector2 direction = perpendicular ? Vector2.right : Vector2.up;
        track.mainTextureOffset += direction * offset * trackSpinSpeed * Time.deltaTime;
    }

    private void WheelsControl(GameObject wheels, float offset)
    {
        if (wheels == null)
            return;

        foreach (Transform wheel in wheels.transform)
            wheel.Rotate(offset * wheelSpinSpeed * Time.deltaTime, 0, 0, Space.Self);
    }
}
