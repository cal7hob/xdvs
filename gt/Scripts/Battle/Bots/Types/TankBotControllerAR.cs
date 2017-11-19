using System.Collections;
using UnityEngine;

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
                    oldSpeed: yAxisAcceleration,
                    newSpeed: tankBotAI.YAxisControl,
                    step: yAxisAccelerationStep,
                    inertionRatio: yAxisInertion,
                    xAxis: false);
        }
    }

    public override float XAxisAcceleration
    {
        get
        {
            return xAxisAcceleration
                = Accelerate(
                    oldSpeed: xAxisAcceleration,
                    newSpeed: tankBotAI.XAxisControl,
                    step: xAxisAccelerationStep,
                    inertionRatio: xAxisInertion,
                    xAxis: true);
        }
    }

    public override float XAxisControl { get { return tankBotAI.XAxisControl; } }
    public override float YAxisControl { get { return tankBotAI.YAxisControl; } }
    public override float TurretAxisControl { get { return tankBotAI.TurretAxisControl; } }
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

    protected override void Update()
    {
        if (!PhotonView || !IsAvailable)
            return;

        if (IsMine)
        {
            tankBotAI.MyBotUpdate();
            UpdateEffects();
        }
        else
        {
            tankBotAI.OthersBotUpdate();
        }

        //DrawSkidmarks();
    }

#if UNITY_EDITOR
    void OnDrawGizmos()
    {
        if (!BotDispatcher.Instance || !BotDispatcher.DrawBotPaths || tankBotAI == null || tankBotAI.Path == null || tankBotAI.Path.corners.Length <= 0 || tankBotAI.CurrentWaypoint > tankBotAI.Path.corners.Length - 1)
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

        id = tankControllerAR.id;
        tankGroup = tankControllerAR.tankGroup;
        data = tankControllerAR.data;
        forCam = tankControllerAR.forCam;
        lookPoint = tankControllerAR.lookPoint;
        cameraEndPoint = tankControllerAR.cameraEndPoint;
        shotPrefabPath = tankControllerAR.shotPrefabPath;
        hitPrefabPath = tankControllerAR.hitPrefabPath;
        terrainHitPrefabPath = tankControllerAR.terrainHitPrefabPath;
        explosionPrefabPath = tankControllerAR.explosionPrefabPath;
        shellPrefabPath = tankControllerAR.shellPrefabPath;
        shootEffectPoints = tankControllerAR.shootEffectPoints;
        engineSound = tankControllerAR.engineSound;
        turretRotationSound = tankControllerAR.turretRotationSound;
        shotSound = tankControllerAR.shotSound;
        blowSound = tankControllerAR.blowSound;
        explosionSound = tankControllerAR.explosionSound;
        respawnSound = tankControllerAR.respawnSound;
        maxSpeed = tankControllerAR.maxSpeed;
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

    protected override void OnPhotonInstantiate(PhotonMessageInfo info)
    {
        base.OnPhotonInstantiate(info);

        skidmarkPoints = GetComponentsInChildren<SkidmarksPoint>();

        if (!PhotonNetwork.isMasterClient)
        {
            tankBotAI = new DummyTankBotAI(this);
            tankBotAI.Init((BotDispatcher.BotBehaviours)PhotonView.instantiationData[1]);
        }

        turretController = GetTurret(this, shootAnimation);
    }

    protected override TurretController GetTurret(VehicleController vehicle, Animation shootAnimation)
    {
        return new TurretBotARController(vehicle, shootAnimation);
    }

    public override void EffectItself(VehicleEffect effect, bool inverted = false)
    {
        if (effect.Source == BonusItem.BonusType.Consumable)
            base.EffectItself(effect, inverted);
    }
}
