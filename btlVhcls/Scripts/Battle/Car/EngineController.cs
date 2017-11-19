using UnityEngine;

public class EngineController : MonoBehaviour
{
    [Tooltip("Первичный коэффициент разгона (чем меньше, тем плавнее). Вторичный: CarController.accelerationRatio.")]
    public float accelerationRatio = 0.0125f;

    public float idleSpeedMultiplier = 8.0f;

    public bool IsAccelerating
    {
        get { return !HelpTools.Approximately(RequiredTorque, 0); }
    }

    public bool IsChangingDirection
    {
        get { return (RequiredTorque > 0 ^ Torque > 0) && IsAccelerating; }
    }

    public float AccelerationProgress
    {
        get { return !IsAccelerating ? 0 : Mathf.Clamp01(Torque / RequiredTorque); }
    }

    public float RequiredTorque
    {
        get; private set;
    }

    public float Torque
    {
        get; private set;
    }

    public float Acceleration
    {
        set
        {
            RequiredTorque = value;

            Torque
                = Mathf.MoveTowards(
                    current:    Torque,
                    target:     RequiredTorque,
                    maxDelta:   IsAccelerating ? accelerationRatio : (accelerationRatio / idleSpeedMultiplier));
        }
    }

    public void Stop()
    {
        Acceleration = 0;
        Torque = 0;
    }
}
