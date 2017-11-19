using System;
using UnityEngine;

public class WheelMF : MonoBehaviour
{
    // Define the variables used in the script, the Corresponding collider is the wheel collider at the position of
    // the visible wheel, the slip prefab is the prefab instantiated when the wheels slide, the rotation value is the
    // value used to rotate the wheel around it's axle.
    public WheelCollider col;
    public Transform wheel;

    [HideInInspector]
    public float maxVisualAngle = 50f;
    [HideInInspector]
    public bool limitVisualAngle = false;
    [HideInInspector]
    public bool isSteering = false;

    private TankControllerMF tc;
    private float rotationValue = 0.0f;
    private float steer = 0;

    private void Start () {
        tc = transform.root.GetComponentInParent<TankControllerMF> ();

        col.gameObject.SetActive (tc.PhotonView.isMine);
    }

    Vector3 position;
    #pragma warning disable 414
    Quaternion rotation;
    #pragma warning restore
    void Update () {
        if (tc.PhotonView.isMine) {
            col.GetWorldPose (out position, out rotation);

            wheel.position = position;
            //wheel.rotation = rotation;
        }
        else {
            // define a hit point for the raycast collision
            RaycastHit hit;
            // Find the collider's center point, you need to do this because the center of the collider might not actually be
            // the real position if the transform's off.
            Vector3 colliderCenterPoint = col.transform.TransformPoint( col.center );

            // now cast a ray out from the wheel collider's center the distance of the suspension, if it hit something, then use the "hit"
            // variable's data to find where the wheel hit, if it didn't, then se tthe wheel to be fully extended along the suspension.
            if (Physics.Raycast (colliderCenterPoint, -col.transform.up, out hit, col.suspensionDistance + col.radius)) {
                wheel.position = hit.point + (col.transform.up * col.radius);
            }
            else {
                wheel.position = colliderCenterPoint - (col.transform.up * col.suspensionDistance);
            }
        }

        if (isSteering) {
            steer = tc.PhotonView.isMine ? col.steerAngle : tc.cloneSteer;

            if (limitVisualAngle && Mathf.Abs (steer) > maxVisualAngle) {
                steer = Mathf.Sign (steer) * maxVisualAngle;
            }
        }

        wheel.localRotation = Quaternion.Euler (rotationValue, steer, 0);

        // increase the rotation value by the rotation speed (in degrees per second)
        rotationValue += SpeedToRpm (tc.speed, col.radius) * (360f / 60f) * Time.deltaTime;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="speed">Speed in meters per second</param>
    /// <param name="radius">Wheel radius</param>
    /// <returns>RPM - rounds per minute</returns>
    /// 
    float SpeedToRpm (float speed, float radius) {
        return speed / (radius * 0.10472f);
    }

    public void OnNowImMaster () {
        col.gameObject.SetActive (tc.PhotonView.isMine);
    }
}
