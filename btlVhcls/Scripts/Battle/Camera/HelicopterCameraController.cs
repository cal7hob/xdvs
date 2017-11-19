using System.Collections;
using UnityEngine;

public class HelicopterCameraController : FlightCameraController
{
    [Header("Вертолётное")]
    public float lookRatio = 0.65f;

    [Header("Движение камеры/Облет препятствий")]
    public float sphereCastRadius = 0.2f;
    public float correctCamDistance = 1;
    public float camTowardsSpeed = 20;
    public float camSpeed = 20;
    public float crashingCamSpeed = 0.1f;
    public LayerMask hitMask;

    private int hitMaskInt;
    private Vector3 correctedHitPoint;
    private Vector3 defaultForCamPos;
    private RaycastHit hitInfo;
    private HelicopterController myHelicopter;

    protected override void SwitchToMyVehicle(EventId eid, EventInfo ei)
    {
        base.SwitchToMyVehicle(eid, ei);

        transform.position = myHelicopter.transform.position;

        camTransform.position = myHelicopter.forCam.position;
        camTransform.LookAt(lookPointTransform.position);

        myHelicopter.forCam.localPosition = defaultForCamPos;

        Camera.main.fieldOfView = respawnedCamFOV;
    }

    protected override void Init(EventId id, EventInfo info)
    {
        base.Init(id, info);

        myHelicopter = (HelicopterController)BattleController.MyVehicle;
        defaultForCamPos = myHelicopter.forCam.localPosition;
        hitMaskInt = hitMask.value;
    }

    protected override void OnShellHit(EventId eid, EventInfo ei)
    {
        EventInfo_U info = (EventInfo_U)ei;

        int victimId = (int)info[0];
        int damage = (int)info[1];
        int ownerId = (int)info[2];
        GunShellInfo.ShellType shellType = (GunShellInfo.ShellType)(int)info[3];
        int hits = (int)info[4];
        Vector3 hitPosition = (Vector3)info[5];

        if (victimId != BattleController.MyPlayerId ||
            !myHelicopter.IsAvailable)
        {
            return;
        }

        VehicleController attacker;

        if (!BattleController.allVehicles.TryGetValue(ownerId, out attacker))
            return;

        DamageShake((attacker.transform.position - myHelicopter.transform.TransformPoint(hitPosition)).normalized);
    }

    protected override IEnumerator DamageShaking(Vector3 shakingDir)
    {
        IsDamageShaking = true;

        float speed = shakingStartSpeed;
        float acceleration = 0;

        while (Mathf.Abs(speed) + Mathf.Abs(acceleration) > 0.5f)
        {
            myHelicopter.forCam.Translate(shakingDir * speed * Time.deltaTime, Space.Self);

            Vector3 delta = defaultForCamPos - myHelicopter.forCam.localPosition;

            acceleration = delta.magnitude * Mathf.Sign(Vector3.Dot(delta, shakingDir)) * accelerationRatio;

            speed = (speed + Time.fixedDeltaTime * acceleration) * vibrationFadeRatio;

            yield return null;
        }

        SwitchOffShaking();
    }

    protected override void SwitchOffShaking()
    {
        myHelicopter.forCam.localPosition = defaultForCamPos;
        IsDamageShaking = false;
    }

    public override void CamRegularMotion()
    {
        var copterPos = myHelicopter.transform.position;
        transform.position = copterPos;

        if (Physics.SphereCast(
            /* origin:      */  copterPos,
            /* radius:      */  sphereCastRadius,
            /* direction:   */  (myHelicopter.forCam.position - copterPos).normalized,
            /* hitInfo:     */  out hitInfo,
            /* maxDistance: */  Vector3.Distance(copterPos, myHelicopter.forCam.position),
            /* layerMask:   */  hitMaskInt))
        {
            MoveAroundBarriers();
        }
        else
        {
            NormalMove();
        }
    }

    private void NormalMove()
    {
        cam.transform.position
            = Vector3.Lerp(
                a:  cam.transform.position,
                b:  myHelicopter.forCam.position,
                t:  Time.deltaTime * (myHelicopter.IsCrashing ? crashingCamSpeed : camSpeed));

        Rotate();

        if (IsDamageShaking)
            return;

        SetAccelerationFOV();
    }

    private void MoveAroundBarriers()
    {
        correctedHitPoint
            = Vector3.MoveTowards(
                current:            hitInfo.point,
                target:             lookPointTransform.position,
                maxDistanceDelta:   correctCamDistance);

        camTransform.position
            = Vector3.MoveTowards(
                current:            camTransform.position,
                target:             correctedHitPoint,
                maxDistanceDelta:   Time.deltaTime * (myHelicopter.IsCrashing ? crashingCamSpeed : camTowardsSpeed));

        Rotate();
    }

    private void Rotate()
    {
        Vector3 forwardIdentityDirection = vehicleInView.transform.forward.GetHorizontalIdentity();
        Vector3 inclineDirection = (lookPointTransform.position - cam.transform.position).normalized * lookRatio;

        Vector3 forwardDirection
            = myHelicopter.IsCrashing
                ? (myHelicopter.transform.position - cam.transform.position).normalized
                : forwardIdentityDirection + inclineDirection;

        cam.transform.rotation
            = Quaternion.Lerp(
                a:  cam.transform.rotation,
                b:  Quaternion.LookRotation(forwardDirection),
                t:  rotationSmooth);
    }
}
