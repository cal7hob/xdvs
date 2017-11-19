public enum EngineState
{
    None,
    Idle,
    ForwardAcceleration,
    BackwardAcceleration,
    Movement,
    ReverseMovement,
    ForwardBrake,
    BackwardBrake
}

public enum VehicleRotationState
{
    None,
    Idle,
    Left,
    Right
}

public class VehicleStateDispatcher
{
    private const float DEFAULT_THRESHOLD = 0.01f;

    private float lastSpeed;
    private float lastRotationSpeed;
    private VehicleController vehicleController;
    private EngineState lastEngineState;
    private VehicleRotationState lastRotationState;

    public void Init(VehicleController vehicleController)
    {
        this.vehicleController = vehicleController;

        lastEngineState = EngineState.None;
        lastRotationState = VehicleRotationState.None;
    }

    public void RegisterMovement(float speed)
    {
        RegisterMovement(lastSpeed, speed, DEFAULT_THRESHOLD);
        lastSpeed = speed;
    }

    public void RegisterMovement(float oldSpeed, float newSpeed, float step)
    {
        bool isIdling = HelpTools.Approximately(oldSpeed, 0) && HelpTools.Approximately(newSpeed, 0);
        bool isForwardAccelerating = newSpeed > oldSpeed && newSpeed > 0 && oldSpeed >= 0;
        bool isBackwardAccelerating = newSpeed < oldSpeed && newSpeed < 0 && oldSpeed <= 0;
        bool isForwardDecelerating = newSpeed < oldSpeed && newSpeed >= 0 && oldSpeed > 0;
        bool isBackwardDecelerating = newSpeed > oldSpeed && newSpeed <= 0 && oldSpeed < 0;
        bool isSavingSpeed = HelpTools.Approximately(newSpeed, oldSpeed, step);

        if (isIdling)
            SetEngineState(EngineState.Idle);

        if (!isSavingSpeed)
        {
            if (isForwardAccelerating)
                SetEngineState(EngineState.ForwardAcceleration);

            if (isBackwardAccelerating)
                SetEngineState(EngineState.BackwardAcceleration);

            if (isForwardDecelerating)
                SetEngineState(EngineState.ForwardBrake);

            if (isBackwardDecelerating)
                SetEngineState(EngineState.BackwardBrake);
        }
        else if (!isIdling)
        {
            if (isForwardAccelerating)
                SetEngineState(EngineState.Movement);

            if (isBackwardAccelerating)
                SetEngineState(EngineState.ReverseMovement);
        }
    }

    public void RegisterRotation(float speed)
    {
        RegisterRotation(lastRotationSpeed, speed);
        lastRotationSpeed = speed;
    }

    public void RegisterRotation(float oldSpeed, float newSpeed)
    {
        bool isIdling = HelpTools.Approximately(oldSpeed, 0) && HelpTools.Approximately(newSpeed, 0);
        bool isRightAccelerating = newSpeed > oldSpeed && newSpeed > 0 && oldSpeed >= 0;
        bool isLeftAccelerating = newSpeed < oldSpeed && newSpeed < 0 && oldSpeed <= 0;

        if (isIdling)
            SetRotationState(VehicleRotationState.Idle);

        if (isRightAccelerating)
            SetRotationState(VehicleRotationState.Right);

        if(isLeftAccelerating)
            SetRotationState(VehicleRotationState.Left);
    }

    private void SetEngineState(EngineState engineState)
    {
        if (engineState != lastEngineState)
            Dispatcher.Send(EventId.EngineStateChanged, new EventInfo_II(vehicleController.data.playerId, (int)engineState));

        lastEngineState = engineState;
    }

    private void SetRotationState(VehicleRotationState rotationState)
    {
        if (rotationState != lastRotationState)
            Dispatcher.Send(EventId.VehicleRotationStateChanged, new EventInfo_II(vehicleController.data.playerId, (int)rotationState));

        lastRotationState = rotationState;
    }
}
