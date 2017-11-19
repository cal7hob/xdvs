using System;
using UnityEngine;
using XD;

public enum WheelActivation
{
    None        = 0,
    All         = 1,
    Partically  = 2,
}

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

    [SerializeField]
    private new Transform   transform = null;
    [SerializeField]
    private new Collider    collider = null;    
    [SerializeField]
    private WheelCollider   wheelCollider = null;
    [SerializeField]
    private Transform       joint = null;
    [SerializeField]
    private float           spinRatio = 300;
    [SerializeField]
    private bool            isGeneral = false;

    [BitMask(typeof(Mode))]
    public Mode             mode = Mode.Driven;    

    private const float     STEERING_RATIO = 35.0f / 90.0f;

    private bool            isDamping = false;
    private bool            isPulling = false;
    private bool            hasCollider = false;
    private bool            hasJoint = false;    

    private float           storedTurnValue = 0;
    private float           dampSpeed = 0;
    private float           pullSpeed = 0;
    private float           dampThreshold = 0;
    
    //private Clamper         randomMultiply = new Clamper(0.8f, 1.2f);
    //private Clamper         randomAngle = new Clamper(1f, 1.2f);

    private Vector3         startPosition;
    private Vector3         currentPosition;
    private Vector3         wheelPosition;
    private Vector3         localEuler;

    private Quaternion      wheelRotation;

    public Collider Collider
    {
        get
        {
            return collider;
        }

        set
        {
            collider = value;
        }
    }

    public bool IsMain
    {
        get
        {
            return isGeneral;
        }

        set
        {
            isGeneral = value;
        }
    }

    public Transform Joint
    {
        get
        {
            return joint;
        }

        set
        {
            joint = value;
        }
    }

    private void Start()
    {
        if (transform == null)
        {
            transform = GetComponent<Transform>();
        }

        startPosition = transform.localPosition;
        //Randomize();
    }

    public void Init(bool isMine, WheelActivation mode)
    {
        if (transform == null)
        {
            transform = GetComponent<Transform>();
        }

        if (wheelCollider == null && Collider != null)
        {
            wheelCollider = Collider.GetComponent<WheelCollider>();
        }

        currentPosition = transform.position;
        hasJoint = Joint != null;

        if (wheelCollider == null)
        {
            return;
        }

        if (Application.isPlaying)
        {
            if (!isMine)
            {

                switch (mode)
                {
                    case WheelActivation.All:
                        break;

                    case WheelActivation.None:
                        wheelCollider.enabled = false;
                        break;

                    case WheelActivation.Partically:
                        if (!isGeneral)
                        {
                            wheelCollider.enabled = false;
                        }
                        break;
                }
            }
        }

        hasCollider = wheelCollider != null && wheelCollider.enabled;
    }

    /*private void Update()
    {
        Damp();
    }*/

    public void Turn(float value)
    {
        if ((mode & Mode.Turning) != Wheel.Mode.Undefined)
        {
            if (hasCollider)
            {
                wheelCollider.steerAngle = value * Mathf.Rad2Deg * STEERING_RATIO;
                return;
            }
        }

        if (HelpTools.Approximately(value, storedTurnValue, 0.01f))
        {
            return;
        }

        storedTurnValue = value;

        if ((mode & Mode.Turning) == Wheel.Mode.Undefined)
        {
            return;
        }

        transform.localRotation = Quaternion.Euler(transform.localEulerAngles.x, 0.0f, 0.0f);

        transform.RotateAround(
            point:  transform.position,
            axis:   transform.parent.up,
            angle:  Mathf.Asin(Mathf.Clamp(value, -1, 1)) * Mathf.Rad2Deg * STEERING_RATIO);
    }

    //public void Randomize()
    //{
    //    randomMultiply.Randomize();
    //    randomAngle.Randomize();
    //}

    public void WheelUpdate(float dampLength, float dampSpeed, float speed)
    {        
        if (hasCollider)
        {
            wheelCollider.GetWorldPose(out wheelPosition, out wheelRotation);
            currentPosition = Vector3.Slerp(currentPosition, wheelPosition, Time.deltaTime * 50);
            transform.position = currentPosition;
            transform.rotation = wheelRotation;
        }
        else
        {
            wheelPosition = startPosition;            
            transform.localPosition = Vector3.Slerp(transform.localPosition, wheelPosition, Time.deltaTime * dampSpeed);
            currentPosition = transform.position;
            localEuler.x += speed * spinRatio * Time.deltaTime;
            transform.localEulerAngles = localEuler;
        }

        if (hasJoint)
        {
            Joint.position = currentPosition;
        }
    }

    //public void Damp(float length, float dampSpeed, float pullSpeed)
    //{
    //    isDamping = true;
    //    dampThreshold = length;
    //    this.dampSpeed = dampSpeed;
    //    this.pullSpeed = pullSpeed;
    //}

    //private void Damp()
    //{
    //    if (isDamping)
    //    {
    //        transform.Translate(Vector3.up * dampThreshold * dampSpeed * Time.deltaTime, transform.parent);
    //        isPulling = !(isDamping = (transform.localPosition.y - startPosition.y) < dampThreshold);
    //
    //    }
    //    else if (isPulling)
    //    {
    //        transform.Translate(Vector3.down * dampThreshold * pullSpeed * Time.deltaTime, transform.parent);
    //        isPulling = transform.localPosition.y > startPosition.y;
    //    }
    //    else
    //    {
    //        transform.localPosition = startPosition;
    //    }
    //}
}
