using UnityEngine;
using XD;

public class TransmissionControllerAR : TransmissionController
{
    private const float SPIN_RATIO = 8.0f;
    private const float STEERING_SMOOTHNESS_RATIO = 0.5f;

    private IVehicleLogic tankController;

    protected override float SpinRatio
    {
        get
        {
            return SPIN_RATIO;
        }
    }

    protected override void Awake()
    {
        tankController = GetComponent<TankControllerAR>() ?? (IVehicleLogic)GetComponent<TankBotControllerAR>();
        base.Awake();
    }

    public override void Receive()
    {
        float spinValue = tankController.YAxisAcceleration;
        requiredTurnValue = tankController.XAxisAcceleration;

        bool isTurningInPlace = !HelpTools.Approximately(requiredTurnValue, 0) && HelpTools.Approximately(spinValue, 0);

        //Spin(
        //    drive: Wheel.Mode.All,
        //    value: isTurningInPlace ? requiredTurnValue : spinValue);

        currentTurnValue
            = Mathf.Lerp(
                a: currentTurnValue,
                b: requiredTurnValue,
                t: STEERING_SMOOTHNESS_RATIO);

        Turn(currentTurnValue);
    }
}
