public abstract class InputControllerType
{
    protected BattleCamera camera;

    public abstract float XAxis { get; } 
    public abstract float YAxis { get; } 
    public abstract float SensitivityQualifier { get; }

    protected InputControllerType(BattleCamera camera)
    {
        this.camera = camera;
    }

    public abstract void RegularSpecificCameraMotion();
    public abstract void ZoomSpecificCameraMotion();
}
