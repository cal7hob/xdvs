using UnityEngine;

public class Propeller : MonoBehaviour
{
    public bool inverseSpinning;
    public bool idleSpinning;
    public float idleSpeed = 800.0f;
    public Vector3 axis = Vector3.forward;

    private const float SPIN_RATIO = 0.8f;

    void Update()
    {
        if (idleSpinning)
            Rotate(idleSpeed * Time.deltaTime);
    }

    public void Rotate(float value)
    {
        transform.RotateAround(
            point:  transform.position,
            axis:   transform.TransformVector(axis),
            angle:  SPIN_RATIO * (inverseSpinning ? -value : value));
    }
}
