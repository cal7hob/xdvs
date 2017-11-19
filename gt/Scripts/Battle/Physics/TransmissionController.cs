using UnityEngine;

public class TransmissionController : MonoBehaviour
{
    [Header("Разное")]
    public bool isInversed;

    protected float currentTurnValue;
    protected float requiredTurnValue;

    private const float SPIN_RATIO = 2.8f;

    private bool canMove;
    private float currentDrivingSpinSpeed;
    private float currentDrivenSpinSpeed;
    private Wheel[] wheels;

    protected bool is4WD;

    protected virtual float SpinRatio
    {
        get { return SPIN_RATIO; }
    }

    protected virtual void Awake()
    {
        wheels = GetComponentsInChildren<Wheel>();
        is4WD = CheckFor4WD();
    }

    public virtual void Receive()
    {
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
                    angle:  value * SpinRatio * (isInversed ? -1 : 1));
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
