using UnityEngine;

public class Rotor : MonoBehaviour
{
    public enum TurnAxis { X, Y, Z }

    public bool inverseSpinning;
    public bool idleSpinning;
    public TurnAxis turnOver;
    public float spinRatio = 1.0f;

    private const float MIN_SPIN_SPEED = 400.0f * 360.0f * 0.004f;
    private const float MAX_SPIN_SPEED = 700.0f * 360.0f * 0.004f;
    private const float IDLE_SPIN_RATIO = 0.25f;

    void Update()
    {
        if (idleSpinning)
            Rotate();
    }

    public void Rotate()
    {
        transform.RotateAround(
            point:  transform.position,
            axis:   GetAxisVector(turnOver),
            angle:  MIN_SPIN_SPEED * IDLE_SPIN_RATIO * spinRatio * (inverseSpinning ? -1 : 1) * Time.deltaTime);
    }

    public void Rotate(float value)
    {
        float spinningAngle
            = Mathf.Lerp(
                a:  MIN_SPIN_SPEED,
                b:  MAX_SPIN_SPEED,
                t:  value);

        if (inverseSpinning)
            spinningAngle *= -1;

        transform.RotateAround(
            point:  transform.position,
            axis:   GetAxisVector(turnOver),
            angle:  spinningAngle * spinRatio * Time.deltaTime);
    }

    private Vector3 GetAxisVector(TurnAxis axis)
    {
        return axis == TurnAxis.Y
                ? transform.up
                : axis == TurnAxis.Z
                    ? transform.forward
                    : transform.right;
    }
}
