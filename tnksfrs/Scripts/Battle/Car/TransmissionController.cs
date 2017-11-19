using UnityEngine;
using XD;

public class TransmissionController : MonoBehaviour
{
    [Header("Разное")]
    public bool             inverse = false;
    [SerializeField]
    private Wheel[]         wheels = null;

    protected float         currentTurnValue = 0;
    protected float         requiredTurnValue = 0;

    private const float     SPIN_RATIO = 2.8f;
    private const float     STEERING_SMOOTHNESS_RATIO = 0.125f;

    private bool            isCanMove = false;

    private float           currentDrivingSpinSpeed = 0;
    private float           currentDrivenSpinSpeed = 0;

    private CarController   carController = null;
    

    protected bool          is4WD = false;

    protected virtual float SpinRatio
    {
        get
        {
            return SPIN_RATIO;
        }
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
                  a: carController.HorizontalTransformVelocity,
                  b: 0,
                  tolerance: CarController.ACTIVITY_THRESHOLD);

        currentDrivingSpinSpeed = carController.IsBraking ? 0 : carController.engineController.Torque;

        Spin(
            drive: is4WD && (carController.IsHandBraking || carController.IsTankTurning) ? Wheel.Mode.Turning : Wheel.Mode.Driving,
            value: currentDrivingSpinSpeed);

        requiredTurnValue = carController.JoystickXAxis;

        currentTurnValue
            = Mathf.Lerp(
                a: currentTurnValue,
                b: requiredTurnValue,
                t: STEERING_SMOOTHNESS_RATIO);

        Turn(currentTurnValue);

        if (is4WD)
        {
            return;
        }

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
        {
            return;
        }

        foreach (Wheel wheel in wheels)
        {
            wheel.WheelUpdate(0, 0, value);
        }

        //foreach (Wheel wheel in wheels)
        //{
        //    if ((wheel.mode & drive) != Wheel.Mode.Undefined)
        //    {
        //        wheel.transform.RotateAround(
        //            point: wheel.transform.position,
        //            axis: wheel.transform.right,
        //            angle: value * SpinRatio * (inverse ? -1 : 1));
        //    }
        //}
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

    public void Turn(float value)
    {
        foreach (Wheel wheel in wheels)
        {
            wheel.Turn(value);
        }
    }

    private bool CheckFor4WD()
    {
        foreach (Wheel wheel in wheels)
        {
            if ((wheel.mode & Wheel.Mode.Driving) == Wheel.Mode.Undefined)
            {
                return false;
            }
        }

        return true;
    }
}