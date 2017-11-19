using UnityEngine;

public class Propeller : MonoBehaviour
{
    public bool inverseSpinning;

    private const float SPIN_RATIO = 0.8f;

    public void Rotate(float value)
    {
        transform.RotateAround(
            point:  transform.position,
            axis:   transform.forward,
            angle:  SPIN_RATIO * (inverseSpinning ? -value : value));
    }
}
