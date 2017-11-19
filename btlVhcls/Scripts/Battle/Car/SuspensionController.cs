using System;
using UnityEngine;

public class SuspensionController : MonoBehaviour
{
    [Header("Наклон кузова")]
    public float suspensionAngle = 4.0f;
    public float suspensionSpeed = 3.5f;
    public float angularVelocityThreshold = 0.5f;

    [Header("Подвеска колёс")]
    public float dampLength = 0.04f;
    public float dampSpeed = 8.0f;
    public float pullSpeed = 4.0f;

    private const string COLLIDER_PREFIX = "Wheel";

    private VehicleController vehicleController;
    private Quaternion requiredBodyRotation;
    private Transform body;
    private Transform suspension;
    private TankAnimationController animationController;
    private Wheel[] wheels;
    private Wheel[] sortedWheels;
    private Wheel closestWheel;

    void Awake()
    {
        suspension = transform.Find("Suspension");
        body = transform.Find("Body");
        vehicleController = GetComponent<VehicleController>();
        animationController = GetComponent<TankAnimationController>();
        wheels = GetComponentsInChildren<Wheel>();
    }

    void Update()
    {
        RotateBody();
    }

    public bool CheckGroundContacts(Collision collision)
    {
        bool anyWheelContact = false;

        ContactPoint contact;

        for (int i = 0; i < collision.contacts.Length; i++)
        {
            contact = collision.contacts[i];

            bool wheelContact = contact.thisCollider.name.Contains(COLLIDER_PREFIX);

            if (!wheelContact)
                continue;

            anyWheelContact = true;

            sortedWheels = wheels;

            Vector3 contactPoint = contact.point;

            Array.Sort(
                sortedWheels,
                (first, second) =>
                    Vector3.Distance(first.transform.position, contactPoint)
                        .CompareTo(Vector3.Distance(second.transform.position, contactPoint)));

            closestWheel = sortedWheels[0];

            closestWheel.Damp(
                length:     dampLength,
                dampSpeed:  dampSpeed,
                pullSpeed:  pullSpeed);
        }

        return anyWheelContact;
    }

    private void RotateBody()
    {
        if (animationController != null && animationController.IsPlaying)
            return;

        requiredBodyRotation
            = vehicleController.LocalAngularVelocity.x < angularVelocityThreshold &&
              vehicleController.LocalAngularVelocity.z < angularVelocityThreshold
                ? Quaternion.Euler(
                    x:  suspension.eulerAngles.x,
                    y:  suspension.eulerAngles.y,
                    z:  suspension.eulerAngles.z)
                : Quaternion.Euler(
                    x:  suspension.eulerAngles.x + (suspensionAngle * -Mathf.Sign(suspension.eulerAngles.x)),
                    y:  suspension.eulerAngles.y + (suspensionAngle * -Mathf.Sign(suspension.eulerAngles.y)),
                    z:  suspension.eulerAngles.z + (suspensionAngle * -Mathf.Sign(suspension.eulerAngles.z)));

        body.rotation
            = Quaternion.Lerp(
                a:  body.rotation,
                b:  requiredBodyRotation,
                t:  suspensionSpeed * Time.deltaTime);
    }
}
