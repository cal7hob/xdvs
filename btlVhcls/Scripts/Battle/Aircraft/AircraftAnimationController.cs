using UnityEngine;

public class AircraftAnimationController : MonoBehaviour
{
    private const float SHAKING_RATIO = 0.000125f;
    private const float STEERING_SMOOTHNESS_RATIO = 0.125f;
    
    private float requiredVerticalTurnValue;
    private float requiredHorizontalTurnValue;
    private float currentVerticalTurnValue;
    private float currentHorizontalTurnValue;
    private Vector3 initialBodyLocalPosition;
    private AircraftController aircraftController;
    private Aileron[] ailerons;
    private Propeller[] propellers;

    void Start()
    {
        aircraftController = GetComponent<AircraftController>();
        ailerons = GetComponentsInChildren<Aileron>();
        propellers = GetComponentsInChildren<Propeller>();

        initialBodyLocalPosition = aircraftController.Body.localPosition;
    }

    public void Receive()
    {
        if (aircraftController == null)
            return;

        if (!aircraftController.IsAvailable)
        {
            RestoreValues();
            return;
        }

        SpinPropeller(aircraftController.IsMain ? aircraftController.TargetSpeed : aircraftController.CurrentSpeed);

        if (aircraftController.IsMain)
        {
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

            TurnAilerons(Aileron.Steering.Horizontal, currentHorizontalTurnValue);
            TurnAilerons(Aileron.Steering.Vertical, currentVerticalTurnValue);

            aircraftController.Body.localPosition = initialBodyLocalPosition + Random.onUnitSphere * aircraftController.CurrentSpeed * SHAKING_RATIO;
        }
    }

    public void SpinPropeller(float value)
    {
        if (HelpTools.Approximately(value, 0))
            return;

        foreach (Propeller propeller in propellers)
            propeller.Rotate(value);
    }

    private void TurnAilerons(Aileron.Steering steering, float value)
    {
        foreach (Aileron aileron in ailerons)
            aileron.Turn(steering, value);
    }

    private void RestoreValues()
    {
        aircraftController.Body.localPosition = initialBodyLocalPosition;
    }
}
