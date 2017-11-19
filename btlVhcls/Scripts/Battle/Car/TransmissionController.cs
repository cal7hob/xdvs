using UnityEngine;

public class TransmissionController : MonoBehaviour
{
    [Header("Разное")]
    public bool inverse;

    protected float currentTurnValue;
    protected float requiredTurnValue;

    private const float SPIN_RATIO = 2.8f;
    private const float STEERING_SMOOTHNESS_RATIO = 0.125f;

    private bool isCanMove;
    private float currentDrivingSpinSpeed;
    private float currentDrivenSpinSpeed;
    private CarController carController;
    private Wheel[] wheels;

    protected bool is4WD;

    protected virtual float SpinRatio
    {
        get { return SPIN_RATIO; }
    }

    protected virtual void Awake()
    {
        carController = GetComponent<CarController>();
        wheels = GetComponentsInChildren<Wheel>();
        is4WD = CheckFor4WD();
    }

    public virtual void Receive()
    {
        isCanMove
            = carController.IsOnGround &&
              !HelpTools.Approximately(
                  a:          carController.HorizontalTransformVelocity,
                  b:          0,
                  tolerance:  CarController.ACTIVITY_THRESHOLD);

        currentDrivingSpinSpeed = carController.IsBraking ? 0 : carController.engineController.Torque;

        Spin(
            drive:  is4WD && (carController.IsHandBraking || carController.IsTankTurning) ? Wheel.Mode.Turning : Wheel.Mode.Driving,
            value:  currentDrivingSpinSpeed);

        requiredTurnValue = carController.JoystickXAxis;

        currentTurnValue
            = Mathf.Lerp(
                a:  currentTurnValue,
                b:  requiredTurnValue,
                t:  STEERING_SMOOTHNESS_RATIO);

        Turn(currentTurnValue);

        if (is4WD)
            return;

        currentDrivenSpinSpeed
            = isCanMove &&
              !carController.IsHandBraking &&
              !carController.IsBraking &&
              !carController.IsTankTurning
                ? carController.CurrentSpeed
                : 0;

        Spin(drive: Wheel.Mode.Driven, value: currentDrivenSpinSpeed);
    }

    public void Spin(Wheel.Mode drive, float value)
    {
        if (HelpTools.Approximately(value, 0))
            return;

        foreach (Wheel wheel in wheels)
            if ((wheel.mode & drive) != Wheel.Mode.Undefined)
                wheel.transform.RotateAround(
                    point:  wheel.transform.position,
                    axis:   wheel.transform.right,
                    angle:  value * SpinRatio * (inverse ? -1 : 1));
    }

    public void Turn(float value)
    {
        foreach (Wheel wheel in wheels)
            wheel.Turn(value);
    }

    private bool CheckFor4WD()
    {
        foreach (Wheel wheel in wheels)
            if ((wheel.mode & Wheel.Mode.Driving) == Wheel.Mode.Undefined)
                return false;

        return true;
    }
}
