using System;
using UnityEngine;

public class Wheel : MonoBehaviour
{
    [Flags]
    public enum Mode
    {
        Undefined   = 0,
        Driven      = 1 << 0,
        Driving     = 1 << 1,
        Turning     = 1 << 2,
        All         = Driven | Driving
    }

    public new Collider collider;

    [BitMask(typeof(Mode))]
    public Mode mode = Mode.Driven;

    private const float STEERING_RATIO = 35.0f / 90.0f;

    private bool isDamping;
    private bool isPulling;
    private float storedTurnValue;
    private float dampSpeed;
    private float pullSpeed;
    private float dampThreshold;
    private Vector3 startPosition;

    void Start()
    {
        startPosition = transform.localPosition;
    }

    void Update()
    {
        Damp();
    }

    public void Turn(float value)
    {
        if (HelpTools.Approximately(a: value, b: storedTurnValue, tolerance: 0.01f))
            return;

        storedTurnValue = value;

        if ((mode & Mode.Turning) == Wheel.Mode.Undefined)
            return;

        transform.localRotation
            = Quaternion.Euler(
                x:  transform.localEulerAngles.x,
                y:  0.0f,
                z:  0.0f);

        transform.RotateAround(
            point:  transform.position,
            axis:   transform.parent.up,
            angle:  Mathf.Asin(Mathf.Clamp(value, -1, 1)) * Mathf.Rad2Deg * STEERING_RATIO);
    }

    public void Damp(float length, float dampSpeed, float pullSpeed)
    {
        isDamping = true;
        dampThreshold = length;
        this.dampSpeed = dampSpeed;
        this.pullSpeed = pullSpeed;
    }

    private void Damp()
    {
        if (isDamping)
        {
            transform.Translate(Vector3.up * dampThreshold * dampSpeed * Time.deltaTime, transform.parent);
            isPulling = !(isDamping = (transform.localPosition.y - startPosition.y) < dampThreshold);

        }
        else if (isPulling)
        {
            transform.Translate(Vector3.down * dampThreshold * pullSpeed * Time.deltaTime, transform.parent);
            isPulling = transform.localPosition.y > startPosition.y;
        }
        else
        {
            transform.localPosition = startPosition;
        }
    }
}
