using System;
using System.Collections.Generic;
using UnityEngine;
using XD;
using XDevs.LiteralKeys;

using Hashtable = ExitGames.Client.Photon.Hashtable;

public class TankController : VehicleController
{
    [Header("Настройки для всех танков")]

    [Header("Звуки")]
    public AudioClip[]  collisionSounds = null;

    protected const float MOVEMENT_SPEED_THRESHOLD = 0.01f;

    protected bool      onGround = false;

    protected float     currentAcceleration = 0;
    protected float     curMaxRotationSpeed = 0;

    [SerializeField]
    protected Vector3   requiredLocalVelocity = new Vector3();
    protected Vector3   requiredLocalAngularVelocity = new Vector3();
    
    private const float CORRECTION_TIME = 0.5f;
    private const float ODOMETER_RATIO = 1.5f;    

    private int         terrainContactCount = 0;  

    protected override float OdometerRatio
    {
        get 
        { 
            return ODOMETER_RATIO; 
        }
    }

    protected override float SpeedRatio
    {
        get
        {
            return moveSpeedKoefficient;
        }
    }

    protected override float CorrectionTime
    {
        get 
        {  
            return CORRECTION_TIME; 
        }
    }

    protected virtual AudioClip CollisionSound
    {
        get 
        { 
            return collisionSounds == null || collisionSounds.Length == 0 ? null : collisionSounds.GetRandomItem(); 
        }
    }

    /* UNITY SECTION */
    //protected override void PhysicsUpdate()
    //{
    //    base.PhysicsUpdate();
    //    MovePlayer();
    //}
    
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
            {
                AudioDispatcher.PlayClipAtPosition(
                    clip: collisionSound,
                    position: transform.position,
                    volume: SoundControllerTankAR.COLLISION_VOLUME);
            }
        }
    }

    protected virtual void OnCollisionExit(Collision collision)
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
    
    /* PUBLIC SECTION */

    [PunRPC]
    public void Shoot(int victimId, Vector3 position, int damage)
    {
        if (!isAvailable)
        {
            return;
        }

        Vector3 hitPoint;

        bool tankHit = false;

        if (victimId != 0 && StaticContainer.BattleController.Units.ContainsKey(victimId))
        {
            Transform passiveTransform = StaticContainer.BattleController.Units[victimId].transform;
            hitPoint = passiveTransform.TransformPoint(position);
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

        if (PhotonView.isMine)
        {
            hitPoint = Vector3.MoveTowards(hitPoint, CannonEnd.position, shotCorrection);
        }

        if (victimId == 0 || !StaticContainer.BattleController.Units.ContainsKey(victimId))
        {
            return;
        }

        VehicleController victim = StaticContainer.BattleController.Units[victimId];

        // NO FRIENDLY FIRE!!!
        if (!victim.IsAvailable || AreFriends(this, victim))
        {
            return;
        }

        victim.HPSystem.ChangeHitPoints(damage, OwnerID, false);
        Dispatcher.Send(EventId.TankTakesDamage, new EventInfo_U(victimId, damage, data.playerId, (int)DefaultShellType, hitPoint));

        if (victim.PhotonView.isMine) // Если попали в мой танк.
        {
            if (!victim.IsBot)
            {
                PhotonNetwork.player.SetCustomProperties(new Hashtable {{"hl", victim.HPSystem.Armor}});
            }
            else
            {
                PhotonNetwork.room.SetCustomProperties(new Hashtable {{victim.KeyForBotHealth, victim.HPSystem.Armor}});
            }
        }
    }

    [PunRPC]
    public override void Respawn(Vector3 position, Quaternion rotation, bool restoreLife, bool firstTime)
    {
        base.Respawn(position, rotation, restoreLife, firstTime);

        if (IsMine)
        {
            CameraToHome();
        }
    }

    public override void UpdateBotAssets(VehicleController nativeController)
    {
      
    }

    protected override void OnPhotonInstantiate(PhotonMessageInfo info)
    {
        base.OnPhotonInstantiate(info);
        rigidbody.interpolation = RigidbodyInterpolation.Interpolate;

        if (terrainLayer == 0)
        {
            terrainLayer = LayerMask.NameToLayer(Layer.Items[Layer.Key.Terrain]);
        }
        
        if (PhotonView.isMine && !IsBot)
		{			
			CameraToHome();
		}       
	}

    public override bool PrimaryFire(Quaternion rotation)
    {
        Weapon weapon = null;
        if (!ShotPrepare(DefaultShellType, ref weapon, true))
        {
            return false;
        }

        int passiveId = 0;
        Vector3 hitPoint = Vector3.zero;
        RaycastHit hit;
        int damage = 0;
        Vector3 direction = TargetAimed ? (AimPoint.point - ShotPoint.position).normalized : CannonEnd.forward;

        if (Physics.Raycast(ShotPoint.position, direction, out hit, 500, hitMask))
        {
            VehicleController passive = null;
            if (hit.collider.CompareTag("CritZone"))
            {
                passive = hit.collider.transform.GetComponentInParent<VehicleController>();

                if (passive.BotAI != null)
                {
                    passive.BotAI.CurrentBehaviour.OnCritHit(this);
                }
            }
            else if ((hitMask & 1 << hit.collider.gameObject.layer) > 0)
            {
                passive = hit.collider.transform.GetComponentInParent<VehicleController>();
            }

            if (passive && Physics.Raycast(hit.point - direction * 0.1f, -direction, hit.distance, StaticContainer.BattleController.HitMask))
            {
                hitPoint = passive.transform.InverseTransformPoint(hit.point);
                passiveId = passive.data.playerId;
                damage = passive.CalcDamage((int)Damage, hit.collider);
            }
            else
            {
                hitPoint = hit.point;
            }
        }

        PhotonView.RPC("Shoot", PhotonTargets.All, passiveId, hitPoint, damage);

        if (IsMine)
        {
            Dispatcher.Send(EventId.MyTankShots, new EventInfo_I((int) DefaultShellType));
        }

        return true;
    }

    /* PRIVATE SECTION */
    
    private void CameraToHome()
    {
        //Camera.main.transform.position = forCam.position;
        //Camera.main.transform.rotation = forCam.rotation;
    }

    private void DrawShoot()
    {       
    }

    public override void MovePlayer()
    {
    }
}