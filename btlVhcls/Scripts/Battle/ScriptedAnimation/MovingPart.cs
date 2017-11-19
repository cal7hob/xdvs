using UnityEngine;

public class MovingPart : MonoBehaviour
{
    public Transform mesh;
    public Transform from;
    public Transform to;
    public float speed;
    public float parentAccelerationThreshold;

    private SpaceshipController flightController;

    void Awake()
    {
        flightController = GetComponentInParent<SpaceshipController>();
    }

    void Update()
    {
        mesh.localPosition
            = Vector3.Lerp(
                a:  mesh.localPosition,
                b:  (flightController.CurrentSpeed / flightController.MaxSpeed) > parentAccelerationThreshold ? to.localPosition : from.localPosition,
                t:  (speed / Time.fixedDeltaTime ) * Time.deltaTime);

        mesh.localRotation
            = Quaternion.Lerp(
                a:  mesh.localRotation,
                b:  (flightController.CurrentSpeed / flightController.MaxSpeed) > parentAccelerationThreshold ? to.localRotation : from.localRotation,
                t:  (speed / Time.fixedDeltaTime) * Time.deltaTime);
    }
}
