using UnityEngine;

public class CaterpillarMotion : MonoBehaviour
{

    [SerializeField]private VehicleController vehicleController;
    [SerializeField]private GameObject caterpillarLeft;
    [SerializeField]private GameObject caterpillarRight;
    [SerializeField]private GameObject caterpillarLeftLOD;
    [SerializeField]private GameObject caterpillarRightLOD;
    [SerializeField]private GameObject wheelsLeft;
    [SerializeField]private GameObject wheelsRight;

	public bool perpendicular;
    public float trackSpinSpeed = 0.4f;
    public float turningGain = 4.0f;
    public float wheelSpinSpeed = 80.0f;

    private Material caterpillarLeftMaterial;
    private Material caterpillarRightMaterial;
    private Material caterpillarLeftMaterialLOD;
    private Material caterpillarRightMaterialLOD;

    void Awake()
    {
        if (vehicleController == null)
        {
            vehicleController = GetComponent<VehicleController>();
        }

        CacheMaterials();
        //Dispatcher.Subscribe(EventId.QualitySettingsChanged, OnQualitySettingsChanged);
    }

  /*  void OnDestroy()
    {
        Dispatcher.Unsubscribe(EventId.QualitySettingsChanged, OnQualitySettingsChanged);
    }*/

    private float leftTrackOffset;
    private float rightTrackOffset;
    private float speed;
    private float angularSpeed;
    private bool idle;

    void Update()
    {
        leftTrackOffset = 0;
        rightTrackOffset = 0;

        speed = vehicleController.LocalVelocity.z;
        angularSpeed = vehicleController.LocalAngularVelocity.y;

        //---Вращение гусениц для движения---
        idle = HelpTools.Approximately(speed, 0, 0.4f);

        if (!idle)
        {
            leftTrackOffset += speed;
            rightTrackOffset += speed;
        }

        //---Для поворота---
        if (!HelpTools.Approximately(angularSpeed, 0))
        {
            if (idle || angularSpeed > 0)
            {
                leftTrackOffset += angularSpeed * turningGain;
            }
            if (idle || angularSpeed < 0)
            {
                rightTrackOffset -= angularSpeed * turningGain;
            }
        }

        TracksControl(caterpillarLeftMaterial, leftTrackOffset);
        TracksControl(caterpillarRightMaterial, rightTrackOffset);
   //     TracksControl(caterpillarLeftMaterialLOD, leftTrackOffset);
   //     TracksControl(caterpillarRightMaterialLOD, rightTrackOffset);

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
        {
            caterpillarLeftMaterial = caterpillarLeft.GetComponent<Renderer>().material;
        }

        if (caterpillarRight != null)
        {
            caterpillarRightMaterial = caterpillarRight.GetComponent<Renderer>().material;
        }
        /*
        if (caterpillarLeftLOD != null)
        {
            caterpillarLeftMaterialLOD = caterpillarLeftLOD.GetComponent<Renderer>().material;
        }

        if (caterpillarRightLOD != null)
        {
            caterpillarRightMaterialLOD = caterpillarRightLOD.GetComponent<Renderer>().material;
        }*/
    }

    private void TracksControl(Material track, float offset)
    {
        if (track == null)
        {
            return;
        }
        track.mainTextureOffset += (perpendicular ? Vector2.right : Vector2.up) * offset * trackSpinSpeed * Time.deltaTime;
    }

    private void WheelsControl(GameObject wheels, float offset)
    {
        if (wheels == null)
        {
            return;
        }

        foreach (Transform wheel in wheels.transform)
        {
            wheel.Rotate(offset * wheelSpinSpeed * Time.deltaTime, 0, 0, Space.Self);
        }
    }
}
