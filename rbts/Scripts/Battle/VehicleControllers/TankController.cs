using System;
using UnityEngine;
using System.Collections.Generic;
using XDevs.LiteralKeys;

using Hashtable = ExitGames.Client.Photon.Hashtable;

public class TankController : VehicleController
{
    [Header("Настройки для всех танков")]

    protected const float MOVEMENT_SPEED_THRESHOLD = 0.01f;

    protected float curMaxSpeed;
    protected float curMaxRotationSpeed;
    protected Vector3 requiredLocalVelocity;
    protected Vector3 requiredLocalAngularVelocity;
    protected Transform indicatorPoint;
    
    private const float CORRECTION_TIME = 0.5f;
    private const float ODOMETER_RATIO = 1.5f;
    private const float IT_SPEED_RATIO = 10;
    private const float FT_SPEED_RATIO = 8;
    private const float TW_SPEED_RATIO = 12;
    private const float AR_SPEED_RATIO = 10;
    private const float MAX_SHOOT_ANGLE = 30f;

    protected override float OdometerRatio
    {
        get { return ODOMETER_RATIO; }
    }

    protected override float SpeedRatio
    {
        get
        {
            switch (GameData.CurInterface)
            {
                case Interface.IronTanks:
                    return IT_SPEED_RATIO;
                case Interface.FutureTanks:
                    return FT_SPEED_RATIO;
                case Interface.ToonWars:
                    return TW_SPEED_RATIO;
                case Interface.Armada:
                    return AR_SPEED_RATIO;
                case Interface.FTRobotsInvasion:
                    return FT_SPEED_RATIO;
                default:
                    Debug.LogError(GameData.CurInterface + " case is not defined in TankController.SpeedRatio! FT's one returned.");
                    return FT_SPEED_RATIO;
            }
        }
    }

    protected override float MaxShootAngle
    {
        get { return MAX_SHOOT_ANGLE; }
    }

    protected override bool NeedCorrectAimY
    {
        get { return true; }
    }

    protected override float CorrectionTime
    {
        get {  return CORRECTION_TIME; }
    }

    protected override Transform IndicatorPoint
    {
        get
        {
            if (indicatorPoint == null)
            {
                indicatorPoint = transform.FindInHierarchy("IndicatorPoint");
                if (indicatorPoint == null)
                {
                    Debug.LogErrorFormat(gameObject, "There is no IndicatorPoint in '{0}'", name);
                }
            }
            return indicatorPoint;
        }
    }

    protected float RotationSpeed
    {
        get
        {
            return Speed * rotationSpeedQualifier * XAxisControl;
        }
    }

    protected virtual float ZoomRotationSpeed { get { return RotationSpeed * 0.5f; } }

    /* PUBLIC SECTION */

    protected override void MovePlayer()
    {
        if (Stunned)
        {
            return;
        }

        if (!PhotonView.isMine || !IsAvailable || !leftJoystick.IsOn)
            return;

#if UNITY_EDITOR
        rb.centerOfMass = centerOfMass; //sega
#endif  //Demetri

        curMaxSpeed = maxSpeed * YAxisControl;

        if (Mathf.Abs(curMaxSpeed) > MOVEMENT_SPEED_THRESHOLD)
            MarkActivity();

        SetEngineNoise(Mathf.Abs(curMaxSpeed / maxSpeed) + Mathf.Abs(curMaxRotationSpeed / Speed) / 2);

        curMaxRotationSpeed = BattleCamera.Instance.IsZoomed ? ZoomRotationSpeed : RotationSpeed;

        if (Mathf.Abs(curMaxRotationSpeed) > MOVEMENT_SPEED_THRESHOLD)
            MarkActivity();

        if (OnGround)
        {
            requiredLocalVelocity = LocalVelocity;

            requiredLocalVelocity.z = Mathf.Abs(curMaxSpeed) > 0.05f ? curMaxSpeed : 0;
            // Исключение заноса.
            if (Vector3.Dot(transform.forward, rb.velocity.normalized) < 0.996f)
                requiredLocalVelocity.x = 0;

            rb.velocity = transform.TransformDirection(requiredLocalVelocity);

            //bool moveBackward = rb.velocity.sqrMagnitude > 0.01f && Vector3.Dot(rb.velocity, transform.forward) < 0;
            bool moveBackward = curMaxSpeed < 0;
            if (Mathf.Abs(curMaxRotationSpeed) > 0.1f)
            {
                requiredLocalAngularVelocity = LocalAngularVelocity;
                requiredLocalAngularVelocity.y = (moveBackward && ProfileInfo.isInvert ? -1 : 1) * curMaxRotationSpeed * 0.03f;
                requiredLocalAngularVelocity = transform.TransformDirection(requiredLocalAngularVelocity);

                rb.angularVelocity = requiredLocalAngularVelocity;
            }
        }
        else
        {
            rb.AddForce((Vector3.down - transform.up) * 45, ForceMode.Acceleration);
        }

        StoreVehiclePosition();
    }

    [PunRPC]
    public override void Respawn(Vector3 position, Quaternion rotation, bool restoreLife, bool firstTime)
    {
        base.Respawn(position, rotation, restoreLife, firstTime);
    }

    public override VehicleInfo.VehicleType VehicleType
    {
        get { return VehicleInfo.VehicleType.Tank; }
    }
}