using UnityEngine;

public class AircraftAnimationController : MonoBehaviour
{
    public Aileron[] ailerons;
    public Propeller[] propellers;

    private const float STEERING_SMOOTHNESS_RATIO = 0.125f;

    private float requiredVerticalTurnValue;
    private float requiredHorizontalTurnValue;
    private float currentVerticalTurnValue;
    private float currentHorizontalTurnValue;
    private AircraftController aircraftController;

    void Start() { aircraftController = GetComponent<AircraftController>(); }

    public void Receive()
    {
        SpinPropeller(aircraftController.IsMine ? aircraftController.TargetSpeed : aircraftController.CurrentSpeed);

        if (!aircraftController.IsMine)
            return;

        requiredVerticalTurnValue = aircraftController.YAxisControl;

        currentVerticalTurnValue
            = Mathf.Lerp(
                a:  currentVerticalTurnValue,
                b:  requiredVerticalTurnValue,
                t:  STEERING_SMOOTHNESS_RATIO);

        requiredHorizontalTurnValue = aircraftController.XAxisControl;

        currentHorizontalTurnValue
            = Mathf.Lerp(
                a:  currentHorizontalTurnValue,
                b:  requiredHorizontalTurnValue,
                t:  STEERING_SMOOTHNESS_RATIO);

        TurnAilerons(steering: Aileron.Steering.Horizontal, value: currentHorizontalTurnValue);
        TurnAilerons(steering: Aileron.Steering.Vertical, value: currentVerticalTurnValue);
    }

    private void TurnAilerons(Aileron.Steering steering, float value)
    {
        foreach (Aileron aileron in ailerons)
            aileron.Turn(steering, value);
    }

    public void SpinPropeller(float value)
    {
        if (HelpTools.Approximately(value, 0))
            return;

        foreach (Propeller propeller in propellers)
            propeller.Rotate(value);
    }
}
