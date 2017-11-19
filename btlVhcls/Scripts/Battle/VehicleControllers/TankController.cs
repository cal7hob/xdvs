using System;
using UnityEngine;
using System.Collections.Generic;
using XDevs.LiteralKeys;

using Hashtable = ExitGames.Client.Photon.Hashtable;

public class TankController : VehicleController
{
    [Header("Настройки для всех танков")]

    [Header("Префабы для Shoot")]
    public GameObject tankHitPrefab;
    public GameObject terrainHitPrefab;

    [Header("Звуки")]
    public AudioClip[] collisionSounds;

    protected const float MOVEMENT_SPEED_THRESHOLD = 0.01f;
    protected const float SKIDMARK_SPEED_THRESHOLD = 0.5f;

    protected bool onGround;
    protected float curMaxSpeed;
    protected float curMaxRotationSpeed;
    protected Vector3 requiredLocalVelocity;
    protected Vector3 requiredLocalAngularVelocity;
    protected Transform cannonEnd;
    protected Transform indicatorPoint;
    protected Transform critZones;
    protected Transform bumper;

    private const float CORRECTION_TIME = 0.5f;
    private const float ODOMETER_RATIO = 1.5f;
    private const float IT_SPEED_RATIO = 10;
    private const float FT_SPEED_RATIO = 8;
    private const float TW_SPEED_RATIO = 12;
    private const float AR_SPEED_RATIO = 10;
    private const float MF_SPEED_RATIO = 2.0f;
    private const float MAX_SHOOT_ANGLE = 30f;

    private int terrainContactCount;
    private List<Vector3> bodyBoundPoints;

    public override Transform Turret
    {
        get { return turret = turret ?? transform.Find("Turret"); }
        set { turret = value; }
    }

    public override Transform ShotPoint
    {
        get { return shotPoint = shotPoint ?? (transform.Find("Turret/ShotPoint") ?? transform.Find("ShotPoint")); }
        set { shotPoint = value; }
    }

    public override Transform CannonEnd
    {
        get { return cannonEnd = cannonEnd ?? (transform.Find("Turret/CannonEnd") ?? transform.Find("CannonEnd")); }
    }

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
                case Interface.MetalForce:
                    return MF_SPEED_RATIO;
                default:
                    throw new Exception(GameData.CurInterface + " case is not defined in TankController.SpeedRatio!");
            }
        }
    }

    public override float MaxShootAngle
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
        get { return indicatorPoint = indicatorPoint ?? transform.Find("IndicatorPoint"); }
    }

    protected override Transform CritZones
    {
        get { return critZones = critZones ?? transform.Find("CritZones"); }
    }

    protected override Transform Bumper
    {
        get { return bumper = bumper ?? transform.Find("Body/Bumper"); }
    }

    protected virtual AudioClip CollisionSound
    {
        get { return collisionSounds == null || collisionSounds.Length == 0 ? null : collisionSounds.GetRandomItem(); }
    }

    virtual protected float RotationSpeed
    {
        get { return data.speed*rotationSpeedQualifier*XAxisControl; }
    }

    protected virtual float ZoomRotationSpeed { get { return RotationSpeed*0.5f; } }

    /* UNITY SECTION */
    protected override void FixedUpdate()
    {
        MovePlayer();
    }

    /* PUBLIC SECTION */

    public override void MovePlayer()
    {
        if (!PhotonView.isMine || !IsAvailable || !leftJoystick.IsOn)
            return;
         
        rb.centerOfMass = centerOfMass; //sega

        curMaxSpeed = MaxSpeed * YAxisControl;

        if (Mathf.Abs(curMaxSpeed) > MOVEMENT_SPEED_THRESHOLD)
            MarkActivity();

        SetEngineNoise(Mathf.Abs(curMaxSpeed / MaxSpeed) + Mathf.Abs(curMaxRotationSpeed / data.speed) / 2);

        curMaxRotationSpeed = BattleCamera.Instance.IsZoomed ? ZoomRotationSpeed : RotationSpeed;

        if (Mathf.Abs(curMaxRotationSpeed) > MOVEMENT_SPEED_THRESHOLD)
            MarkActivity();

        if (onGround)
        {
            requiredLocalVelocity = LocalVelocity;

            requiredLocalVelocity.z = Mathf.Abs(curMaxSpeed) > 0.05f ? curMaxSpeed : 0;
            // Исключение заноса.
            if (Vector3.Angle(transform.forward, rb.velocity) > 5.0f)
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

    protected virtual void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.layer == terrainLayer)
        {
            terrainContactCount++;
            onGround = true;
        }

        PlayCollisionSound(collision);
    }

    private void PlayCollisionSound(Collision collision)
    {
        if (collision.relativeVelocity.magnitude > 6.0f &&
            MiscTools.CheckIfLayerInMask(othersLayerMask, collision.gameObject.layer))
        {
            AudioClip collisionSound = CollisionSound;

            if (collisionSound != null)
                AudioDispatcher.PlayClipAtPosition(
                    clip:       collisionSound,
                    position:   transform.position,
                    volume:     SoundControllerBase.COLLISION_VOLUME);
        }
    }

    void OnCollisionExit(Collision collision)
    {
        if (collision.gameObject.layer != terrainLayer)
            return;

        terrainContactCount--;

        if (terrainContactCount == 0)
            onGround = false;
    }

    /* PUBLIC SECTION */
    [PunRPC]
    public void Shoot(int victimId, Vector3 position, int damage)
    {
        if (!isAvailable)
            return;

        if (IsVisible)
            DrawShoot();

        AudioDispatcher.PlayClipAtPosition(shotSound, CannonEnd.position);

        Vector3 hitPoint;

        bool tankHit = false;

        if (victimId != 0 && BattleController.allVehicles.ContainsKey(victimId))
        {
            Transform passiveTransform = BattleController.allVehicles[victimId].transform;
            hitPoint = passiveTransform.TransformPoint(position);
            tankHit = true;
        }
        else
        {
            Dispatcher.Send(EventId.TankShotMissed, new EventInfo_II(data.playerId, (int)GunShellInfo.ShellType.Usual));

            RaycastHit hit;

            if (!Physics.Raycast(ShotPoint.position, CannonEnd.forward, out hit, MaxAimDistance, hitMask))
                return;

            hitPoint = hit.point;
        }

        if (PhotonView.isMine)
            hitPoint = Vector3.MoveTowards(hitPoint, CannonEnd.position, shotCorrection);

        if (!tankHitPrefab || !terrainHitPrefab)
            EffectPoolDispatcher.GetFromPool(hitPrefab, hitPoint, Quaternion.identity);
        else
            EffectPoolDispatcher.GetFromPool(tankHit ? tankHitPrefab : terrainHitPrefab, hitPoint, Quaternion.identity);

        if (victimId == 0 || !BattleController.allVehicles.ContainsKey(victimId))
            return;

        VehicleController victim = BattleController.allVehicles[victimId];

        AudioDispatcher.PlayClipAtPosition(victim.blowSound, victim.transform.TransformPoint(position));

        // NO FRIENDLY FIRE!!!
        if (!victim.IsAvailable || AreFriends(this, victim))
            return;

        Dispatcher.Send(
            id:     EventId.TankTakesDamage,
            info:   new EventInfo_U(
                        /* victimId */      victimId,
                        /* damage */        damage,
                        /* attackerId */    data.playerId,
                        /* shellType */     (int)DefaultShellType,
                        /* hits */          1,
                        /* hitPosition */   hitPoint));

        if (victim.PhotonView.isMine) // Если попали в локальный танк (мой или бот, если мастер)
        {
            if (!victim.IsBot)
                PhotonNetwork.player.SetCustomProperties(new Hashtable { { "hl", victim.Armor } });
            else
                PhotonNetwork.room.SetCustomProperties(new Hashtable { { victim.KeyForHealth, victim.Armor } });
        }
    }

    [PunRPC]
    public override void Respawn(Vector3 position, Quaternion rotation, bool restoreLife, bool firstTime)
    {
        base.Respawn(position, rotation, restoreLife, firstTime);
    }

    public override void UpdateBotPrefabs(VehicleController nativeController)
    {
      
    }

    public override void OnPhotonInstantiate(PhotonMessageInfo info)
    {
        base.OnPhotonInstantiate(info);

        rb.interpolation = RigidbodyInterpolation.Interpolate;

        if (terrainLayer == 0)
            terrainLayer = LayerMask.NameToLayer(Layer.Items[Layer.Key.Terrain]);

        if (!GameData.IsGame(Game.IronTanks))
            shootAnimation = gameObject.GetComponent<Animation>();

        CollectBodyBoundPoints();
    }

    public override bool PrimaryFire()
    {
        MarkActivity();

        if (!weapons[DefaultShellType].IsReady)
        {
            return false;
        }

        weapons[DefaultShellType].RegisterShot();

        BattleGUI.FireButtons[DefaultShellType].SimulateReloading();

        if (shootAnimation)
            shootAnimation.Play();
        
        int passiveId = 0;
        bool critHit = false;
        Vector3 hitPoint = Vector3.zero;
        int damage = 0;

        if (aimingController == null || aimingController.Target == null)
        {
            Vector3 direction = TargetAimed ? (TargetPosition - ShotPoint.position).normalized : CannonEnd.forward;
            RaycastHit hit;

            if (Physics.Raycast(ShotPoint.position, direction, out hit, MaxAimDistance, hitMask))
            {
                VehicleController passive = null;
                if (hit.collider.tag == "CritZone")
                {
                    passive = hit.collider.transform.GetComponentInParent<VehicleController>();
                    critHit = true;
                }
                else if ((hitMask & 1 << hit.collider.gameObject.layer) > 0)
                {
                    passive = hit.collider.transform.GetComponentInParent<VehicleController>();
                }

                if (passive &&
                    Physics.Raycast(hit.point - direction * 0.1f, -direction, hit.distance, BattleController.HitMask))
                {
                    hitPoint = passive.transform.InverseTransformPoint(hit.point);
                    passiveId = passive.data.playerId;
                    damage = passive.CalcDamage(data.attack, critHit);
                }
                else
                {
                    hitPoint = hit.point;
                }
            }
        }
        else
        {
            passiveId = aimingController.Target.data.playerId;
            damage = aimingController.Target.CalcDamage(data.attack, aimingController.CritZoneAimed);
            hitPoint = aimingController.Target.transform.InverseTransformPoint(aimingController.TargetPosition);
        }

        PhotonView.RPC("Shoot", PhotonTargets.All, passiveId, hitPoint, damage);

        if (IsMain)
            Dispatcher.Send(EventId.MyTankShots, new EventInfo_I((int)DefaultShellType));

        return true;
    }

    public override void BoundPointsToList(List<Vector3> points)
    {
        points.Clear();
        Transform body = Body;
        for (int i = 0; i < bodyBoundPoints.Count; i++)
        {
            points.Add(body.TransformPoint(bodyBoundPoints[i]));
        }
    }


    private void DrawShoot()
    {
        if (shootEffectPoints == null || shootEffectPoints.Count == 0)
        {
            EffectPoolDispatcher.GetFromPool(
                _effect:        shotPrefab,
                _position:      CannonEnd.position,
                _rotation:      CannonEnd.rotation,
                useEffectMover: !GameData.IsGame(Game.FutureTanks), // Не двигать эффект выстрела для FT, он там выглядит странно.
                moverTarget:    CannonEnd);
        }
        else
        {
            foreach (Transform point in shootEffectPoints)
                EffectPoolDispatcher.GetFromPool(
                    _effect:        shotPrefab,
                    _position:      point.position,
                    _rotation:      point.rotation,
                    useEffectMover: true,
                    moverTarget:    point);
        }
    }

    private void CollectBodyBoundPoints()
    {
        Bounds bodyBounds = BodyMesh.bounds;
        Vector3 center = bodyBounds.center;
        Vector3 min = bodyBounds.min;
        Vector3 max = bodyBounds.max;

        bodyBoundPoints = new List<Vector3>(8);
        bodyBoundPoints.Add(new Vector3(min.x, center.y, min.z));
        bodyBoundPoints.Add(new Vector3(min.x, center.y, max.z));
        bodyBoundPoints.Add(new Vector3(max.x, center.y, max.z));
        bodyBoundPoints.Add(new Vector3(max.x, center.y, min.z));
        bodyBoundPoints.Add(new Vector3(min.x, center.y, center.z));
        bodyBoundPoints.Add(new Vector3(max.x, center.y, center.z));
        bodyBoundPoints.Add(new Vector3(center.x, center.y, min.z));
        bodyBoundPoints.Add(new Vector3(center.x, center.y, max.z));
    }
}