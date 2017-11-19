using UnityEngine;
using XD;

public class CaterpillarMotion : MonoBehaviour
{
    public float                moveGain = -0.015f;
    public float                rotationFactor = 0.18f;
    public float                spinGain = -2700.0f;

    public GameObject           caterpillarLeft = null;
    public GameObject           caterpillarRight = null;

    [SerializeField]
    private Material            caterpillarLeftMaterial = null;
    [SerializeField]
    private Material            caterpillarRightMaterial = null;
    [SerializeField]
    private Renderer            caterpillarLeftRenderer = null;
    [SerializeField]
    private Renderer            caterpillarRightRenderer = null;
    [SerializeField]
    private VehicleController   unit = null;
    [SerializeField]
    private MaterialsContainer  container = null;

    public GameObject           wheelsLeft = null;
    public GameObject           wheelsRight = null;
	public bool                 perpendicular = false;

    private float               offsetLeft = 0;
    private float               offsetRight = 0;
    private float               deltaOffset = 0;
	private Vector2             leftTexOffset = new Vector2();
	private Vector2             rightTexOffset = new Vector2();	

    private MaterialsContainer Container
    {
        get
        {
            if (container == null)
            {
                container = GetComponent<MaterialsContainer>();

                if (container == null)
                {
                    container = gameObject.AddComponent<MaterialsContainer>();
                }
            }

            return container;
        }
    }

    private void Awake()
    {
        InitComponents();

        Dispatcher.Subscribe(EventId.QualitySettingsChanged, OnQualitySettingsChanged);
    }

    private void OnDestroy()
    {
        Dispatcher.Unsubscribe(EventId.QualitySettingsChanged, OnQualitySettingsChanged);
    }

    private void Update()
    {
        TracksControl();
    }

    private void OnQualitySettingsChanged(EventId id, EventInfo ei)
    {
        Debug.LogWarning(name + " OnQualitySettingsChanged!", this);
        CacheMaterials();
    }

    public void InitComponents()
    {
        if (Container == null)
        {
            return;
        }

        if (unit == null)
        {
            unit = GetComponent<VehicleController>();
        }

        if (caterpillarLeftRenderer == null)
        {
            caterpillarLeftRenderer = (caterpillarLeft == null ? null : caterpillarLeft.GetComponent<Renderer>());
        }

        if (caterpillarRightRenderer == null)
        {
            caterpillarRightRenderer = (caterpillarRight == null ? null : caterpillarRight.GetComponent<Renderer>());
        }

        CacheMaterials();
    }

    private void CacheMaterials()
    {
        if (!Application.isPlaying)
        {
            return;
        }

        if (caterpillarLeftMaterial == null && caterpillarLeftRenderer != null)
        {
            Container.Materials.TryGetValue(caterpillarLeftRenderer, out caterpillarLeftMaterial);
        }

        if (caterpillarRightMaterial == null && caterpillarRightRenderer != null)
        {
            Container.Materials.TryGetValue(caterpillarRightRenderer, out caterpillarRightMaterial);
        }        
    }

    private void TracksControl()
    {
        deltaOffset = moveGain * unit.Velocity.magnitude * Time.deltaTime;

        if (transform.InverseTransformDirection(unit.Velocity).z > 0)
        {
            offsetLeft += deltaOffset;
            offsetRight += deltaOffset;

            WheelsControl(wheelsLeft, deltaOffset);
            WheelsControl(wheelsRight, deltaOffset);
        }
        else
        {
            offsetLeft -= deltaOffset;
            offsetRight -= deltaOffset;

            WheelsControl(wheelsLeft, -deltaOffset);
            WheelsControl(wheelsRight, -deltaOffset);
        }

        deltaOffset = rotationFactor * unit.AngularVelocity.y * Time.deltaTime;

        if (unit.AngularVelocity.y > 0)
        {
            offsetLeft += deltaOffset;
            offsetRight -= deltaOffset;

            WheelsControl(wheelsLeft, deltaOffset);
            WheelsControl(wheelsRight, -deltaOffset);
        }
        else
        {
            offsetLeft += deltaOffset;
            offsetRight -= deltaOffset;

            WheelsControl(wheelsLeft, deltaOffset);
            WheelsControl(wheelsRight, -deltaOffset);
        }

		if (perpendicular)
		{
			leftTexOffset.Set(offsetLeft, 0);
			rightTexOffset.Set(offsetRight, 0);
		}
		else
		{
			leftTexOffset.Set(0, offsetLeft);
			rightTexOffset.Set(0, offsetRight);
		}

        if (caterpillarLeftMaterial != null)
        {
            caterpillarLeftMaterial.mainTextureOffset = leftTexOffset;
        }

        if (caterpillarRightMaterial != null)
        {
            caterpillarRightMaterial.mainTextureOffset = rightTexOffset;
        }
    }

    private void WheelsControl(GameObject wheels, float offset)
    {
        if (GameData.IsGame(Game.IronTanks | Game.ToonWars | Game.Armada2))
        {
            return;
        }

        foreach (Transform wheel in wheels.transform)
        {
            wheel.Rotate(offset * spinGain, 0, 0, Space.Self);
        }
    }
}
