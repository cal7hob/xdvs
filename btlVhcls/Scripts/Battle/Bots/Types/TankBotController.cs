using Disconnect;
using UnityEngine;
using Hashtable = ExitGames.Client.Photon.Hashtable;

public class TankBotController : TankController
{
    protected TankBotAI tankBotAI;

    public override BotAI BotAI
    {
        get { return tankBotAI; }
    }

    public override float XAxisControl { get { return tankBotAI.XAxisControl; } }
    public override float YAxisControl { get { return tankBotAI.YAxisControl; } }
    protected override float TurretAxisControl { get { return tankBotAI.TurretAxisControl; } }
    protected override bool FireButtonPressed { get { return tankBotAI.FireButtonPressed; } }
    protected override float ZoomRotationSpeed { get { return RotationSpeed; } }

    protected override void OnDestroy()
    {
        PhotonNetwork.RaiseEvent(
            (byte)BattleController.BattleEvent.BotSpawn, 
            new Hashtable() {{"botPhotonId", PhotonView.viewID}}, 
            true, 
            new RaiseEventOptions() { CachingOption = EventCaching.RemoveFromRoomCache}
        );
        tankBotAI.OnBotDestroy();
        base.OnDestroy();
    }

    public override float TurretRotationSpeedQualifier
    {
        get
        {
            return turretRotationSpeedQualifier;
        }
    }

    protected override void Update()
    {
        if (!PhotonView || !IsAvailable)
            return;

        if (PhotonView.isMine)
        {
            tankBotAI.MyBotUpdate();
        }
        else
        {
            tankBotAI.OthersBotUpdate();
        }      
    }

#if UNITY_EDITOR
    override protected void OnDrawGizmos()
    {
        base.OnDrawGizmos ();
        if (!BotDispatcher.DrawBotPaths || tankBotAI == null || tankBotAI.Path == null || tankBotAI.Path.corners.Length <= 0 || tankBotAI.CurrentWaypoint > tankBotAI.Path.corners.Length - 1)
        {
            return;
        }

        for (int i = 0; i < tankBotAI.Path.corners.Length - 1; i++)
        {
            Debug.DrawLine(tankBotAI.Path.corners[i], tankBotAI.Path.corners[i + 1], Color.red);
        }

        Gizmos.color = Color.red;
        Gizmos.DrawSphere(tankBotAI.Path.corners[tankBotAI.CurrentWaypoint], 1);

        Gizmos.color = Color.cyan;
        Gizmos.DrawSphere(tankBotAI.PositionToMove, 2);
    }
#endif

    public override void TurretRotation()
    {
        if (tankBotAI.Target == null)
            return;

        float deltaForRotation = 0;

        var targetDir = (tankBotAI.Target.transform.position - Turret.position).normalized;
        tankBotAI.TurretAxisControl = Mathf.Clamp(Vector3.Dot(targetDir, Turret.right), -1, 1);

        if (!HelpTools.Approximately(TurretAxisControl, 0))
        {
            deltaForRotation = TurretAxisControl;
            TurretCentering = false;
        }
        else if (TurretCentering)
        {
            if (HelpTools.Approximately(Turret.localEulerAngles.y, 0))
            {
                TurretCentering = false;
                return;
            }

            deltaForRotation = Mathf.Clamp(Mathf.DeltaAngle(Turret.localEulerAngles.y, 0), -1, 1);
        }

        if (HelpTools.Approximately(deltaForRotation, 0))
            return;

        float maxTurretRotationAngle = Speed * TurretRotationSpeedQualifier * Time.deltaTime;
        float realRotation = 0f;
        if (BattleSettings.Instance != null)
        {
            realRotation = Mathf.Clamp(
                   value: HelpTools.ApplySensitivity(deltaForRotation, BattleSettings.Instance.TurretRotationSensitivity) * maxTurretRotationAngle,
                   min: -maxTurretRotationAngle,
                   max: maxTurretRotationAngle);
        }
        else
        {
            realRotation = Mathf.Clamp(
                    value: deltaForRotation * maxTurretRotationAngle,
                    min: -maxTurretRotationAngle,
                    max: maxTurretRotationAngle);
        }

        if (TurretCentering && Mathf.Abs(realRotation) > Mathf.Abs(Mathf.DeltaAngle(Turret.localEulerAngles.y, 0)))
            Turret.localEulerAngles = Vector3.zero;
        else
        {
            Turret.Rotate(0, realRotation, 0, Space.Self);
        }
    }

    public override void OnPhotonInstantiate(PhotonMessageInfo info)
    {
        base.OnPhotonInstantiate(info);

        if (!BattleConnectManager.IsMasterClient)
        {
            tankBotAI = new DummyTankBotAI(this);
            tankBotAI.Init((BotDispatcher.BotBehaviours)PhotonView.instantiationData[1]);
        }
    }

    protected override void EffectItself(VehicleEffect effect, bool inverted = false)
    {
        if (effect.Source == BonusItem.BonusType.Consumable)
            base.EffectItself(effect, inverted);
    }

    public override void UpdateBotPrefabs(VehicleController nativeController)
    {
        var tankController = nativeController as TankController;

        if (tankController == null)
        {
            return;
        }

        data = tankController.data;
        forCam = tankController.forCam;
        lookPoint = tankController.lookPoint;
        cameraEndPoint = tankController.cameraEndPoint;
        shotPrefab = tankController.shotPrefab;
        hitPrefab = tankController.hitPrefab;
        tankHitPrefab = tankController.tankHitPrefab;
        terrainHitPrefab = tankController.terrainHitPrefab;
        explosionPrefab = tankController.explosionPrefab;
        shootEffectPoints = tankController.shootEffectPoints;
        engineSound = tankController.engineSound;
        turretRotationSound = tankController.turretRotationSound;
        shotSound = tankController.shotSound;
        blowSound = tankController.blowSound;
        explosionSound = tankController.explosionSound;
        respawnSound = tankController.respawnSound;
        MaxSpeed = tankController.MaxSpeed;
        centerOfMass = tankController.centerOfMass;
        continuousFire = tankController.continuousFire;
        shotCorrection = tankController.shotCorrection;
        turretRotationSpeedQualifier = tankController.turretRotationSpeedQualifier;
        rotationSpeedQualifier = tankController.rotationSpeedQualifier;
    }

    public override void ReanimateBot()
    {
        tankBotAI = new TankBotAI(this);
        tankBotAI.Init((BotDispatcher.BotBehaviours)PhotonView.instantiationData[1]);
        base.ReanimateBot();
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

        RaycastHit hit;

        int damage = 0;

        Vector3 direction = TargetAimed && PhotonView.isMine ? (TargetPosition - ShotPoint.position).normalized : CannonEnd.forward;

        if (Physics.Raycast(ShotPoint.position, direction, out hit, 500, hitMask))
        {
            VehicleController passive = null;
            if (hit.collider.tag == "CritZone")
            {
                passive = hit.collider.transform.GetComponentInParent<VehicleController>();
                critHit = true;

                if(passive.IsBot)
                    passive.BotAI.CurrentBehaviour.OnCritHit(this);
            }
            else if ((hitMask & 1 << hit.collider.gameObject.layer) > 0)
            {
                passive = hit.collider.transform.GetComponentInParent<VehicleController>();
            }


            if (passive && Physics.Raycast(hit.point - direction * 0.1f, -direction, hit.distance, BattleController.HitMask))
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

        PhotonView.RPC("Shoot", PhotonTargets.All, passiveId, hitPoint, damage);

        return true;
    }
}
