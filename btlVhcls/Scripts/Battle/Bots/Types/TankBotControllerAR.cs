using Disconnect;
using UnityEngine;
using Hashtable = ExitGames.Client.Photon.Hashtable;

public class TankBotControllerAR : TankControllerAR
{
    protected TankBotAI tankBotAI;

    public override BotAI BotAI
    {
        get { return tankBotAI; }
    }

    public override float YAxisAcceleration
    {
        get
        {
            return yAxisAcceleration
                = Accelerate(
                    oldSpeed:       yAxisAcceleration,
                    newSpeed:       tankBotAI.YAxisControl,
                    step:           yAxisAccelerationStep,
                    inertionRatio:  yAxisInertion,
                    xAxis:          false);
        }
    }

    public override float XAxisAcceleration
    {
        get
        {
            return xAxisAcceleration
                = Accelerate(
                    oldSpeed:       xAxisAcceleration,
                    newSpeed:       tankBotAI.XAxisControl,
                    step:           xAxisAccelerationStep,
                    inertionRatio:  xAxisInertion,
                    xAxis:          true);
        }
    }

    public override float TurretRotationSpeedQualifier
    {
        get
        {
            return turretRotationSpeedQualifier;
        }
    }

    public override float XAxisControl { get { return tankBotAI.XAxisControl; } }
    public override float YAxisControl { get { return tankBotAI.YAxisControl; } }
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
    }

    [PunRPC]
    public override void Respawn(Vector3 position, Quaternion rotation, bool restoreLife, bool firstTime)
    {
        base.Respawn(position, rotation, restoreLife, firstTime);

        yAxisAcceleration = 0;
        xAxisAcceleration = 0;
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

    protected override void Update()
    {
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
    }

#if UNITY_EDITOR
    override protected void OnDrawGizmos()
    {
        base.OnDrawGizmos ();

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
        suspensionController.CheckGroundContacts(collision);
    }

    public override void UpdateBotPrefabs(VehicleController nativeController)
    {
        var tankControllerAR = nativeController as TankControllerAR;

        if (tankControllerAR == null)
        {
            return;
        }

        data = tankControllerAR.data;
        forCam = tankControllerAR.forCam;
        lookPoint = tankControllerAR.lookPoint;
        cameraEndPoint = tankControllerAR.cameraEndPoint;
        shotPrefab = tankControllerAR.shotPrefab;
        hitPrefab = tankControllerAR.hitPrefab;
        tankHitPrefab = tankControllerAR.tankHitPrefab;
        terrainHitPrefab = tankControllerAR.terrainHitPrefab;
        explosionPrefab = tankControllerAR.explosionPrefab;
        shootEffectPoints = tankControllerAR.shootEffectPoints;
        engineSound = tankControllerAR.engineSound;
        turretRotationSound = tankControllerAR.turretRotationSound;
        shotSound = tankControllerAR.shotSound;
        blowSound = tankControllerAR.blowSound;
        explosionSound = tankControllerAR.explosionSound;
        respawnSound = tankControllerAR.respawnSound;
        MaxSpeed = tankControllerAR.MaxSpeed;
        centerOfMass = tankControllerAR.centerOfMass;
        continuousFire = tankControllerAR.continuousFire;
        shotCorrection = tankControllerAR.shotCorrection;
        turretRotationSpeedQualifier = tankControllerAR.turretRotationSpeedQualifier;
        rotationSpeedQualifier = tankControllerAR.rotationSpeedQualifier;

        idleSound = tankControllerAR.idleSound;
        trackSound = tankControllerAR.trackSound;
        collisionSound = tankControllerAR.collisionSound;
        rotationSound = tankControllerAR.rotationSound;
        accelerationSound = tankControllerAR.accelerationSound;
        reloadingSound = tankControllerAR.reloadingSound;
        reverseSound = tankControllerAR.reverseSound;
        xAxisAccelerationStep = tankControllerAR.xAxisAccelerationStep;
        yAxisAccelerationStep = tankControllerAR.yAxisAccelerationStep;
        xAxisInertion = tankControllerAR.xAxisInertion;
        yAxisInertion = tankControllerAR.yAxisInertion;

        var soundControllerAR = GetComponent<SoundControllerTankAR>();
        DestroyImmediate(soundControllerAR, true);
    }

    public override void ReanimateBot()
    {
        tankBotAI = new TankBotAI(this);
        tankBotAI.Init((BotDispatcher.BotBehaviours)PhotonView.instantiationData[1]);
        base.ReanimateBot();
    }

    public override void OnPhotonInstantiate(PhotonMessageInfo info)
    {
        base.OnPhotonInstantiate(info);

        skidmarkPoints = GetComponentsInChildren<SkidmarksPoint>();

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
