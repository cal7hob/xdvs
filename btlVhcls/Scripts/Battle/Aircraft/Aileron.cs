using UnityEngine;

public class Aileron : MonoBehaviour
{
    public enum Steering
    {
        Horizontal,
        Vertical
    }

    public bool reverseTurning;
    public bool turnOverY;
    public Steering steering = Steering.Horizontal;

    private const float STEERING_RATIO = 35.0f / 90.0f;

    private float storedTurningValue;
    private Quaternion defaultLocalRotation;

    void Start() { defaultLocalRotation = transform.localRotation; }

    public void Turn(Aileron.Steering steering, float value)
    {
        value = reverseTurning ? -value : value;

        if (this.steering != steering || HelpTools.Approximately(a: value, b: storedTurningValue, tolerance: 0.01f))
            return;

        storedTurningValue = value;

        transform.localRotation = defaultLocalRotation;

        transform.RotateAround(
            point:  transform.position,
            axis:   turnOverY ? transform.up : transform.right,
            angle:  Mathf.Asin(value) * Mathf.Rad2Deg * STEERING_RATIO);
    }
}
