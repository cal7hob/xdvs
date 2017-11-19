using UnityEngine;
using System.Collections;

public class Throttle : MonoBehaviour
{
    public static float AccelerationValue { get; private set; }

    public void OnLowerThrottle()
    {
        AccelerationValue = -1;
    }

    public void OnHigherThrottle() 
    {
        AccelerationValue = 1;
    }

    public void OnThrottleBtnRelease()
    {
        AccelerationValue = 0;
    }
}
