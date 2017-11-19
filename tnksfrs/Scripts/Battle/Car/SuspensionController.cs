using UnityEngine;
using XD;

public class SuspensionController : MonoBehaviour
{
    [Header("Подвеска колёс")]
    public float                    dampValue = 0.06f;
    public float                    dampSpeed = 8.0f;
    public float                    pullSpeed = 4.0f;

    [SerializeField]
    private VehicleController       vehicleController = null;
    [SerializeField]
    private Transform               suspension = null;
    [SerializeField]
    private Wheel[]                 wheels = null;
    [SerializeField]
    private bool                    inited = false;    

    private Quaternion              requiredBodyRotation = new Quaternion();    
    private TankAnimationController animationController = null;
    private Transform               body = null;

    public VehicleController VehicleContoller
    {
        set
        {
            vehicleController = value;
        }
    }

    private Transform Body
    {
        get
        {
            body = body ?? transform.Find("Body");
            return body;
        }
    }

    public Transform Suspension
    {
        get
        {
            if (suspension == null)
            {
                suspension = transform.Find("Suspension");

                if (suspension == null)
                {
                    Suspension component = GetComponentInChildren<Suspension>();
                    if (component != null)
                    {
                        suspension = component.transform;
                    }
                }
            }            

            return suspension;
        }

        set
        {
            suspension = value;
        }
    }

    public void ResetToDefaults()
    {
        dampValue = 0.065f;
        dampSpeed = 8f;
        pullSpeed = 4f;
    }

    public void Init()
    {
        vehicleController = GetComponent<VehicleController>();
        animationController = GetComponent<TankAnimationController>();
        wheels = GetComponentsInChildren<Wheel>();
        inited = true;
    }

    private void Awake()
    {
        if (!inited)
        {
            Init();
        }
    }

    public void NormalUpdate()
    {
        AnimateWheels(dampValue, dampSpeed);
    }

    public virtual void InitComponents(bool isMine)
    {
        if (wheels == null || wheels.Length == 0)
        {
            wheels = GetComponentsInChildren<Wheel>();
        }

        bool isMobile = false;
        WheelActivation activation = WheelActivation.All;

        if (Application.isPlaying)
        {
            isMobile = StaticType.Input.Instance<IInput>().IsMobile;
            if (isMobile)
            {
                switch (StaticType.Options.Instance<IOptions>().GraphicsQuality)
                {
                    case 0:
                        activation = WheelActivation.None;
                        break;

                    case 1:
                        activation = WheelActivation.Partically;
                        break;
                }
            }
        }

        for (int i = 0; i < wheels.Length; i++)
        {
            wheels[i].Init(isMine, activation);
        }
    }

    //private void OnEnable()
    //{
    //    foreach (Wheel wheel in wheels)
    //    {
    //        wheel.Randomize();
    //    }
    //}

    private void AnimateWheels(float dampLength, float dampSpeed)
    {
        float magnitude = Mathf.Clamp01(vehicleController.Velocity.magnitude);
        //float yAxisAcceleration = vehicleController.YAxisAcceleration;
        float yAxisAcceleration = 1;
        float dampLengthResult = dampLength * magnitude;
        float speed = magnitude * yAxisAcceleration;

        for (int i = 0; i < wheels.Length; i++)
        {
            wheels[i].WheelUpdate(dampLengthResult, dampSpeed, speed);
        }
    }

    public bool CheckGroundContacts(Collision collision)
    {
        bool anyWheelContact = false;

        foreach (ContactPoint contact in collision.contacts)
        {
            foreach (Wheel wheel in wheels)
            {
                if (contact.thisCollider == wheel.Collider)
                {
                    anyWheelContact = true;

                    /*wheel.Damp(
                        length:     dampLength,
                        dampSpeed:  dampSpeed,
                        pullSpeed:  pullSpeed);*/
                }
            }
        }
        return anyWheelContact;
    }

    //private void RotateBody()
    //{
    //    if (animationController != null && animationController.IsPlaying)
    //        return;
    //
    //    requiredBodyRotation
    //        = vehicleController.LocalAngularVelocity.x < angularVelocityThreshold &&
    //          vehicleController.LocalAngularVelocity.z < angularVelocityThreshold
    //            ? Quaternion.Euler(
    //                x: Suspension.transform.eulerAngles.x,
    //                y: Suspension.transform.eulerAngles.y,
    //                z: Suspension.transform.eulerAngles.z)
    //            : Quaternion.Euler(
    //                x: Suspension.transform.eulerAngles.x + (suspensionAngle * -Mathf.Sign(Suspension.transform.eulerAngles.x)),
    //                y: Suspension.transform.eulerAngles.y + (suspensionAngle * -Mathf.Sign(Suspension.transform.eulerAngles.y)),
    //                z: Suspension.transform.eulerAngles.z + (suspensionAngle * -Mathf.Sign(Suspension.transform.eulerAngles.z)));
    //
    //    Body.rotation
    //        = Quaternion.Lerp(
    //            a:  Body.rotation,
    //            b:  requiredBodyRotation,
    //            t:  suspensionSpeed * Time.deltaTime);
    //}
}
