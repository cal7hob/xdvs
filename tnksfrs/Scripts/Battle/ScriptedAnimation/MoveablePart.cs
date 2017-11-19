using UnityEngine;
using XD;

public class MoveablePart : MonoBehaviour
{
    public float speed;
    public float parentAccelerationThreshold;

    [HideInInspector]
    public Vector3 origin;

    [HideInInspector]
    public Vector3 destination;

    [HideInInspector]
    public Quaternion startRotation;

    [HideInInspector]
    public Quaternion targetRotation;

    private SpaceshipController flightController;

    void Awake()
    {
        flightController = GetComponentInParent<SpaceshipController>();
    }

    void Update()
    {
        transform.localPosition
            = Vector3.Lerp(
                a:  transform.localPosition,
                b:  flightController.CurrentSpeed / flightController.Settings[Setting.MovingSpeed].Max > parentAccelerationThreshold ? destination : origin,
                t:  (speed / Time.fixedDeltaTime ) * Time.deltaTime);

        transform.localRotation
            = Quaternion.Lerp(
                a:  transform.localRotation,
                b:  flightController.CurrentSpeed / flightController.Settings[Setting.MovingSpeed].Max > parentAccelerationThreshold ? targetRotation : startRotation,
                t:  speed / Time.fixedDeltaTime * Time.deltaTime);
    }
}
