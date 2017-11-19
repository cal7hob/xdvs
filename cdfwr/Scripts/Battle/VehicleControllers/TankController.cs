using System;
using System.Collections.Generic;
using Pool;
using UnityEngine;
using XDevs.LiteralKeys;

using Hashtable = ExitGames.Client.Photon.Hashtable;

public class TankController : VehicleController
{
    [Header("Настройки для всех танков")]
    [Header("Звуки")]
    public AudioClip[] collisionSounds;

    protected bool onGround;

    protected Vector3 requiredLocalVelocity;
    protected Vector3 requiredLocalAngularVelocity;

    protected Transform indicatorPoint;
    protected Transform critZones;



    private const float ODOMETER_RATIO = 1.5f;

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


    public override Vector3 ViewPoint //позиция в которую должны смотреть камера и пушка
    {
        get
        {
            return TargetPosition + Target.transform.up * 0.5f;
        }
        protected set { }
    }

    [HideInInspector]
    public Transform cannonEnd;

    public override Transform CannonEnd
    {
        get { return cannonEnd = cannonEnd ?? (transform.Find("Turret/CannonEnd") ?? transform.Find("CannonEnd")); }
    }

    protected float OdometerRatio
    {
        get { return ODOMETER_RATIO; }
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

    protected override float RotationSpeed
    {
        get { return data.speed*rotationSpeedQualifier*XAxisControl; }
    }

    protected override float ZoomRotationSpeed { get { return RotationSpeed*0.5f; } }

    protected void StoreVehiclePosition()
    {
        if (transform.position != storedVehiclePosition)
        {
            odometer += Vector3.Distance(rb.position, storedVehiclePosition) * OdometerRatio;
        }

        storedVehiclePosition = transform.position;
    }


    /* UNITY SECTION */
    protected override void FixedUpdate()
    {
        MovePlayer();
    }

    /* PUBLIC SECTION */

    public override void MovePlayer()
    {
        if (!IsMine || !IsAvailable || !leftJoystick.IsOn)
        {
            return;
        }
         
        rb.centerOfMass = centerOfMass; //sega

        curMaxSpeed = maxSpeed * YAxisControl;

        if (Mathf.Abs(curMaxSpeed) > MOVEMENT_SPEED_THRESHOLD)
        {
            MarkActivity();
        }

        SetEngineNoise(Mathf.Abs(curMaxSpeed / maxSpeed) + Mathf.Abs(curMaxRotationSpeed / data.speed) / 2);

        curMaxRotationSpeed = BattleCamera.Instance.IsZoomed ? ZoomRotationSpeed : RotationSpeed;

        if (Mathf.Abs(curMaxRotationSpeed) > MOVEMENT_SPEED_THRESHOLD)
        {
            MarkActivity();
        }

        if (onGround)
        {
            requiredLocalVelocity = LocalVelocity;

            requiredLocalVelocity.z = Mathf.Abs(curMaxSpeed) > 0.05f ? curMaxSpeed : 0;
            // Исключение заноса.
            if (Vector3.Angle(transform.forward, rb.velocity) > 5.0f)
            {
                requiredLocalVelocity.x = 0;
            }

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

    public override void MoveClone()
    {
        if (!isAvailable)
        {
            transform.position = correctPosition;
            transform.rotation = correctRotation;
            return;
        }

        transform.position = Vector3.SmoothDamp(transform.position, correctPosition, ref posSyncVelocity, syncTime);
        transform.rotation = Quaternion.RotateTowards(transform.rotation, correctRotation, rotSyncSpeed * Time.deltaTime);

        if (Turret)
        {
            Turret.localEulerAngles = new Vector3(0, Mathf.MoveTowardsAngle(Turret.localEulerAngles.y, correctTurretAngle, Speed * turretRotationSpeedQualifier * Time.deltaTime), 0);
        }
    }

    protected virtual void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.layer == terrainLayer)
        {
            terrainContactCount++;
            onGround = true;
        }
        /*
        if (IsBot) 
        {
            return;
        }
        */
        if (collision.collider is SphereCollider) //чтобы не срабатывало на колеса
        {
            return;
        }

        PlayCollisionSound(collision);
    }

    private float minHitSpeed = 6.0f;
    private float delta = 0.3f;
    private float lastCollisionMoment = 0;
    private GameObject hittedObj = null;

    private void PlayCollisionSound(Collision collision)
    {
        if (collision.relativeVelocity.magnitude > minHitSpeed && MiscTools.CheckIfLayerInMask(othersLayerMask, collision.gameObject.layer))
        {
            if (collision.transform.gameObject != hittedObj)
            {
                hittedObj = collision.transform.gameObject;
            }
            else 
            {
                if (Time.time - lastCollisionMoment < delta) //чтобы при постоянном столкновении с одним и тем же объектом звук не воспроизводился много раз подряд
                {
                    return;
                }
                else 
                {
                    lastCollisionMoment = Time.time;
                }
            }
            AudioClip collisionSound = CollisionSound;

            if (collisionSound != null)
            {
                AudioDispatcher.PlayClipAtPosition(
                    clip: collisionSound,
                    position: transform.position,
                    volume: SoundControllerBase.COLLISION_VOLUME);
            }
        }
    }

    void OnCollisionExit(Collision collision)
    {
        if (collision.gameObject.layer != terrainLayer)
        {
            return;
        }

        terrainContactCount--;

        if (terrainContactCount == 0)
        {
            onGround = false;
        }
    }

    #if UNITY_EDITOR
    void OnDrawGizmos()
    {
        Gizmos.color = Color.magenta;
        Gizmos.DrawSphere(center: transform.TransformPoint(centerOfMass), radius: 0.1f);
    }
    #endif

    /* PUBLIC SECTION */
    [PunRPC]
    public override void Shoot(int victimId, int attackerId, Vector3 hitPosition, Vector3 normal, int damage, bool hasHit)
    {
        if (!isAvailable)
        {
            return;
        }

        if (IsVisible)
        {
            DrawShot();
        }

        AudioDispatcher.PlayClipAtPosition(shotSound, CannonEnd.position);

        Vector3 hitPoint;
        bool tankHit = false;

        if (victimId != 0 && BattleController.allVehicles.ContainsKey(victimId))
        {
            Transform passiveTransform = BattleController.allVehicles[victimId].transform;
            hitPoint = passiveTransform.TransformPoint(hitPosition);
            tankHit = true;
        }
        else
        {
            Dispatcher.Send(EventId.TankShotMissed, new EventInfo_I(data.playerId));

            RaycastHit hit;

            if (!Physics.Raycast(ShotPoint.position, CannonEnd.forward, out hit, 500, hitMask))
            {
                return;
            }

            hitPoint = hit.point;
        }

        if (IsMine)
        {
            hitPoint = Vector3.MoveTowards(hitPoint, CannonEnd.position, shotCorrection);
        }

        var effect = PoolManager.GetObject<ParticleEffect>(tankHit ? hitPrefabPath : terrainHitPrefabPath);
        effect.transform.position = hitPoint;
        effect.transform.rotation = Quaternion.identity;

        if (victimId == 0 || !BattleController.allVehicles.ContainsKey(victimId))
        {
            return;
        }

        VehicleController victim = BattleController.allVehicles[victimId];

        AudioDispatcher.PlayClipAtPosition(victim.blowSound, victim.transform.TransformPoint(hitPosition));

        // NO FRIENDLY FIRE!!!
        if (!victim.IsAvailable || StaticContainer.AreFriends(this, victim))
        {
            return;
        }

        victim.Armor -= damage;
        
        Dispatcher.Send(EventId.TankTakesDamage, new EventInfo_U(victimId, damage, data.playerId, (int)StaticContainer.DefaultShellType, hitPoint),Dispatcher.EventTargetType.ToAll);

        if (victim.IsMine) // Если попали в локальный танк (мой или бот, если мастер)
        {
			victim.SetCustomProperties(StatisticKey.Health, victim.Armor);
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
    
    protected override void OnPhotonInstantiate(PhotonMessageInfo info)
    {
        base.OnPhotonInstantiate(info);

        rb.interpolation = RigidbodyInterpolation.Interpolate;

        if (terrainLayer == 0)
        {
            terrainLayer = LayerMask.NameToLayer(Layer.Items[Layer.Key.Terrain]);
        }

	    turretController.SetAnimation(gameObject.GetComponent<Animation>());	        
        CollectBodyBoundPoints();        
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

    /* PRIVATE SECTION */

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

    private void DrawShot()
    {
        Debug.Log("Draw shot");

        if (shootEffectPoints == null || shootEffectPoints.Count == 0)
        {
            var shotEffect = PoolManager.GetObject<ParticleEffect>(shotPrefabPath);
            shotEffect.transform.position = CannonEnd.transform.position;
            shotEffect.transform.forward = CannonEnd.transform.forward;
        }
        else
        {
            foreach (Transform point in shootEffectPoints)
            {
                var shotEffect = PoolManager.GetObject<ParticleEffect>(shotPrefabPath);
                shotEffect.transform.position = point.position;
                shotEffect.transform.forward = point.forward;
            }
        }
    }
}