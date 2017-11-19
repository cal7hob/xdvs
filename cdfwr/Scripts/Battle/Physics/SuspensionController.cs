using System.Collections;
using UnityEngine;

public class SuspensionController : MonoBehaviour
{    
  //  [SerializeField]private Transform body;
    [SerializeField]private Transform suspensionCenter;
    [SerializeField]private Transform suspension;
    [SerializeField]private VehicleController vehicleController;
    [SerializeField]private TankAnimationController animationController;

    [Header("Наклон кузова")]
    public float suspensionAngle = 4.0f;
    public float suspensionSpeed = 3.5f;
    public float angularVelocityThreshold = 0.5f;

    [Header("Отдача")]
    [SerializeField] private float recoilTime;
    [SerializeField] private float recoilMaxAngle;

    [Header("Подвеска колёс")]
    public float dampLength = 0.04f;
    public float dampSpeed = 8.0f;
    public float pullSpeed = 4.0f;
    public float accelerationToAngleRatio = 0.08f;
    public float accerationTimeRatio = 3.5f;
    public float maxAcceleration = 15;

    
    private Quaternion requiredBodyRotation;

    
    private Wheel[] wheels;
    private float recoilProgress = 1;
    private Quaternion recoilInitRotation;
    private Quaternion recoilTargetRotation;
    private Quaternion recoilCurrentRotation;
    private Quaternion startStopCurrentBodyRotation;

    private float prevSpeed;
    private float targetAngle;

    void Awake()
    {    
        if (suspension == null)
        {
            suspension = transform.Find("Suspension");
        }

        if (suspensionCenter == null)
        {
            suspensionCenter = suspension.Find("SuspensionCenter");
        }

        if (vehicleController == null)
        {
            vehicleController = GetComponent<VehicleController>();
        }
        if (animationController == null)
        {
            animationController = GetComponent<TankAnimationController>();
        }

        wheels = GetComponentsInChildren<Wheel>();

        Dispatcher.Subscribe(EventId.StartBurstFire, Recoil);
    }

    void OnDestroy()
    {
        Dispatcher.Unsubscribe(EventId.StartBurstFire, Recoil);
    }

    void Update()
    {
        RotateBody();
    }

    public void Recoil(EventId id, EventInfo info)//отдача
    {
        var info_ii = info as EventInfo_II;

        if (info_ii == null)
        {
            return;
        }

       int playerId = info_ii.int1;

        if (playerId == vehicleController.data.playerId)
        {
            recoilProgress = 0f;
            recoilInitRotation = vehicleController.Body.localRotation;
            recoilTargetRotation = Quaternion.AngleAxis(-recoilMaxAngle, vehicleController.Body.InverseTransformDirection(vehicleController.Turret.right)) * recoilInitRotation;
        }
    }

    public bool CheckGroundContacts(Collision collision)
    {
        foreach (ContactPoint contact in collision.contacts)
        {
            if (wheels == null) 
            {
                return false;
            }
            foreach (Wheel wheel in wheels)
            {
                if (contact.thisCollider == wheel.collider)
                {
                    wheel.Damp(
                        length: dampLength,
                        dampSpeed: dampSpeed,
                        pullSpeed: pullSpeed);
                    return true;
                }
            }
        }

        return false;
    }

    private void RotateBody()
    {
        if (animationController != null && animationController.IsPlaying)
        {
            return;
        }

        requiredBodyRotation
            = vehicleController.LocalAngularVelocity.x < angularVelocityThreshold &&
              vehicleController.LocalAngularVelocity.z < angularVelocityThreshold
                ? Quaternion.Euler(
                    x: suspension.eulerAngles.x,
                    y: suspension.eulerAngles.y,
                    z: suspension.eulerAngles.z)
                : Quaternion.Euler(
                    x: suspension.eulerAngles.x + (suspensionAngle * -Mathf.Sign(suspension.eulerAngles.x)),
                    y: suspension.eulerAngles.y + (suspensionAngle * -Mathf.Sign(suspension.eulerAngles.y)),
                    z: suspension.eulerAngles.z + (suspensionAngle * -Mathf.Sign(suspension.eulerAngles.z)));

        vehicleController.Body.rotation = Quaternion.Lerp(
                a: vehicleController.Body.rotation,
                b: requiredBodyRotation,
                t: suspensionSpeed * Time.deltaTime);

        vehicleController.Body.localRotation = GetRocoilRotation() * GetStartStopRotation();
    }

    private Quaternion GetRocoilRotation()
    {
        if (recoilProgress < 1)
        {
            recoilProgress += Time.deltaTime / recoilTime;
            return Quaternion.Lerp(recoilInitRotation, recoilTargetRotation, Mathf.Sin(recoilProgress * Mathf.PI * 0.5f));
        }

        return vehicleController.Body.localRotation;
    }

    private float currentSpeed;
    private float acceleration;
    private Quaternion GetStartStopRotation()
    {
        currentSpeed = Vector3.Project(vehicleController.Rb.velocity, transform.forward).magnitude;
        acceleration = Mathf.Clamp((currentSpeed - prevSpeed) / Time.deltaTime, -maxAcceleration, maxAcceleration);
        prevSpeed = currentSpeed;
        targetAngle = Mathf.Clamp(Mathf.Lerp(targetAngle, acceleration * (Vector3.Dot(transform.forward, vehicleController.Rb.velocity) > 0 ? -1 : 1) * accelerationToAngleRatio, Mathf.Sin(accerationTimeRatio * Time.deltaTime * Mathf.PI * 0.5f)), -suspensionAngle, suspensionAngle);

        return Quaternion.AngleAxis(targetAngle, vehicleController.Body.InverseTransformDirection(suspensionCenter.right));
    }
}
