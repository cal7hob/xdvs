using Disconnect;
using UnityEngine;
using Hashtable = ExitGames.Client.Photon.Hashtable;

public class TankBotControllerMF : TankControllerMF
{
    protected TankBotAI tankBotAI;
    bool m_lastIsMine = false;

    public override BotAI BotAI
    {
        get { return tankBotAI; }
    }

    public override float TurretRotationSpeedQualifier
    {
        get
        {
            return turretRotationSpeedQualifier;
        }
    }

    public override float XAxisControl { get { return xAxisControl; } }
    public override float YAxisControl { get { return yAxisControl; } }
    protected override float TurretAxisControl { get { return tankBotAI.TurretAxisControl; } }
    protected override bool FireButtonPressed { get { return tankBotAI.FireButtonPressed; } }

    protected override float ZoomRotationSpeed
    {
        get { return RotationSpeed; }
    }

    protected override AudioClip CollisionSound
    {
        get { return collisionSound; }
    }

    protected override void OnDestroy()
    {
        PhotonNetwork.RaiseEvent(
            (byte)BattleController.BattleEvent.BotSpawn,
            new Hashtable() { { "botPhotonId", PhotonView.viewID } },
            true,
            new RaiseEventOptions() { CachingOption = EventCaching.RemoveFromRoomCache }
        );
        tankBotAI.OnBotDestroy();
        base.OnDestroy();
        UberDebug.LogChannel("TankBotControllerMF", "OnDestroy {0}", data.playerId);
    }

    [PunRPC]
    public override void Respawn(Vector3 position, Quaternion rotation, bool restoreLife, bool firstTime)
    {
        base.Respawn(position, rotation, restoreLife, firstTime);
    }

    public override bool PrimaryFire()
    {
        MarkActivity();

        if (!weapons[DefaultShellType].IsReady)
            return false;

        if (PhotonView.isMine)
            BattleGUI.FireButtons[DefaultShellType].SimulateReloading();

        weapons[DefaultShellType].RegisterShot();

        if (shootAnimation)
            shootAnimation.Play();

        EffectPoolDispatcher.GetFromPool(
            _effect:        shotPrefab,
            _position:      CannonEnd.position,
            _rotation:      CannonEnd.rotation,
            useEffectMover: true,
            moverTarget:    CannonEnd);

        Shell shell
            = ShellPoolManager.GetShell(
                shellName:  primaryShellInfo.shellPrefabName,
                position:   shotPoint.position,
                rotation:   TargetAimed && PhotonView.isMine
                                ? Quaternion.LookRotation((TargetPosition - shotPoint.position).normalized, shotPoint.up)
                                : shotPoint.rotation);

        shell.OwnerSpeed = Mathf.Abs(curMaxSpeed);

        continuousFire = false;

        shell.Activate(this, data.attack, hitMask);

        AudioDispatcher.PlayClipAtPosition(shotSound, shotPoint.position, SoundControllerBase.SHOT_VOLUME);

        return true;
    }

    float xAxisControl = 0f;
    float yAxisControl = 0f;
    protected override void Update()
    {
        xAxisControl = Mathf.MoveTowards (xAxisControl, tankBotAI.XAxisControl, .5f * Time.deltaTime);
        yAxisControl = Mathf.MoveTowards (yAxisControl, tankBotAI.YAxisControl, 1f * Time.deltaTime);
        base.Update ();

        if (!PhotonView || !IsAvailable)
            return;

        if (PhotonView.isMine)
        {
            tankBotAI.MyBotUpdate();
            UpdateEffects();
        }
        else
        {
            tankBotAI.OthersBotUpdate();
        }

        if (m_lastIsMine != PhotonView.isMine) {
            m_lastIsMine = PhotonView.isMine;
            OwnershipChanged ();
        }
    }

    void OwnershipChanged () {
        foreach (AxleInfo axleInfo in axleInfos) {
            axleInfo.leftWheel.OnNowImMaster ();
            axleInfo.rightWheel.OnNowImMaster ();
        }
    }

#if UNITY_EDITOR
    override protected void OnDrawGizmos()
    {
        if (BotDispatcher.Instance == null || !BotDispatcher.DrawBotPaths || tankBotAI == null || tankBotAI.Path == null || tankBotAI.Path.corners.Length <= 0 || tankBotAI.CurrentWaypoint > tankBotAI.Path.corners.Length - 1)
        {
            return;
        }

        for (int i = 0; i < tankBotAI.Path.corners.Length - 1; i++)
        {
            Debug.DrawLine(tankBotAI.Path.corners[i], tankBotAI.Path.corners[i + 1], Color.red);
        }

        Gizmos.color = Color.red;
        Gizmos.DrawSphere(tankBotAI.Path.corners[tankBotAI.CurrentWaypoint], 1);

        Gizmos.color = Color.yellow;
        Gizmos.DrawSphere(tankBotAI.PositionToMove, 1);
    }
#endif

    protected override void OnCollisionEnter(Collision collision)
    {
        base.OnCollisionEnter(collision);
        //suspensionController.CheckGroundContacts(collision);
    }

    public override void UpdateBotPrefabs(VehicleController nativeController)
    {
        var tankControllerMF = nativeController as TankControllerMF;

        if (tankControllerMF == null)
        {
            return;
        }

        data = tankControllerMF.data;
        forCam = tankControllerMF.forCam;
        lookPoint = tankControllerMF.lookPoint;
        cameraEndPoint = tankControllerMF.cameraEndPoint;
        shotPrefab = tankControllerMF.shotPrefab;
        hitPrefab = tankControllerMF.hitPrefab;
        tankHitPrefab = tankControllerMF.tankHitPrefab;
        terrainHitPrefab = tankControllerMF.terrainHitPrefab;
        explosionPrefab = tankControllerMF.explosionPrefab;
        shootEffectPoints = tankControllerMF.shootEffectPoints;
        engineSound = tankControllerMF.engineSound;
        turretRotationSound = tankControllerMF.turretRotationSound;
        shotSound = tankControllerMF.shotSound;
        blowSound = tankControllerMF.blowSound;
        explosionSound = tankControllerMF.explosionSound;
        respawnSound = tankControllerMF.respawnSound;
        MaxSpeed = tankControllerMF.MaxSpeed;
        centerOfMass = tankControllerMF.centerOfMass;
        continuousFire = tankControllerMF.continuousFire;
        shotCorrection = tankControllerMF.shotCorrection;
        turretRotationSpeedQualifier = tankControllerMF.turretRotationSpeedQualifier;
        rotationSpeedQualifier = tankControllerMF.rotationSpeedQualifier;

        //idleSound = tankControllerMF.idleSound;
        //trackSound = tankControllerMF.trackSound;
        collisionSound = tankControllerMF.collisionSound;
        //rotationSound = tankControllerMF.rotationSound;
        //accelerationSound = tankControllerMF.accelerationSound;
        reloadingSound = tankControllerMF.reloadingSound;
        //reverseSound = tankControllerMF.reverseSound;

        axleInfos = tankControllerMF.axleInfos;
        brakingPower = tankControllerMF.brakingPower;
        steeringSensitivity = tankControllerMF.steeringSensitivity;
        suspSpring = tankControllerMF.suspSpring;
        suspDamping = tankControllerMF.suspDamping;
        suspTarget = tankControllerMF.suspTarget;
        downForce = tankControllerMF.downForce;
        stopSpeed = tankControllerMF.stopSpeed;
        speed = tankControllerMF.speed;
        speedKmH = tankControllerMF.speedKmH;
        stability = tankControllerMF.stability;
        stabilitySpeed = tankControllerMF.stabilitySpeed;
        maxDrag = tankControllerMF.maxDrag;

        Rigidbody thisRigidbody = GetComponent<Rigidbody>();
        Rigidbody sourceRigidbody = tankControllerMF.GetComponent<Rigidbody>();

        thisRigidbody.mass = sourceRigidbody.mass;
        thisRigidbody.drag = sourceRigidbody.drag;
        thisRigidbody.angularDrag = sourceRigidbody.angularDrag;
        thisRigidbody.interpolation = sourceRigidbody.interpolation;
        thisRigidbody.collisionDetectionMode = sourceRigidbody.collisionDetectionMode;
    }

    public override void ReanimateBot()
    {
        tankBotAI = new TankBotAiMF(this);
        tankBotAI.Init((BotDispatcher.BotBehaviours)PhotonView.instantiationData[1]);
        var mass = Rb.mass;
        base.ReanimateBot();
        Rb.mass = mass;
    }

    public override void OnPhotonInstantiate(PhotonMessageInfo info)
    {
        base.OnPhotonInstantiate(info);
        UberDebug.LogChannel("TankBotControllerMF", "OnPhotonInstantiate {0}", data.playerId);
        m_lastIsMine = PhotonView.isMine;

        rb.mass *= 2.5f;
        engineTorque *= 1.5f;
        rotationSpeedQualifier *= 1.5f;
        stabilitySpeed = 1200f;

        //skidmarkPoints = GetComponentsInChildren<SkidmarksPoint>();

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

    protected override void OnVehicleTakesDamage(EventId id, EventInfo ei)
    {
        base.OnVehicleTakesDamage(id, ei);
        tankBotAI.OnBotTakesDamage(id, ei);
    }

    public override void TurretRotation()
    {
        if (tankBotAI.Target == null)
            return;

        float deltaForRotation = lastTouchTurretRotation;

        lastTurretLocalRotationY = Turret.localEulerAngles.y;

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
}
