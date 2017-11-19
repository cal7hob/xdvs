using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TankBotController : TankController
{
    protected TankBotAI tankBotAI;

    public override BotAI BotAI
    {
        get { return tankBotAI; }
    }

    public override float XAxisControl { get { return tankBotAI.XAxisControl; } }
    public override float YAxisControl { get { return tankBotAI.YAxisControl; } }
    public override float TurretAxisControl { get { return tankBotAI.TurretAxisControl; } }
    protected override bool FireButtonPressed { get { return tankBotAI.FireButtonPressed; } }
    protected override float ZoomRotationSpeed { get { return RotationSpeed; } }

    protected override void OnDestroy()
    {
        tankBotAI.OnBotDestroy();
        base.OnDestroy();
    }

    protected override void Update()
    {
        if (!PhotonView || !IsAvailable)
            return;

        if (IsMine)
        {
            tankBotAI.MyBotUpdate();
        }
        else
        {
            tankBotAI.OthersBotUpdate();
        }
    }

#if UNITY_EDITOR
    void OnDrawGizmos()
    {
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

    protected override void OnPhotonInstantiate(PhotonMessageInfo info)
    {
        base.OnPhotonInstantiate(info);

        if (!PhotonNetwork.isMasterClient)
        {
            tankBotAI = new DummyTankBotAI(this);
            tankBotAI.Init((BotDispatcher.BotBehaviours)PhotonView.instantiationData[1]);
        }
    }

    public override void EffectItself(VehicleEffect effect, bool inverted = false)
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

        id = tankController.id;
        tankGroup = tankController.tankGroup;
        data = tankController.data;
        forCam = tankController.forCam;
        lookPoint = tankController.lookPoint;
        cameraEndPoint = tankController.cameraEndPoint;
        shotPrefabPath = tankController.shotPrefabPath;
        hitPrefabPath = tankController.hitPrefabPath;
        terrainHitPrefabPath = tankController.terrainHitPrefabPath;
        explosionPrefabPath = tankController.explosionPrefabPath;
        shellPrefabPath = tankController.shellPrefabPath;
        shootEffectPoints = tankController.shootEffectPoints;
        engineSound = tankController.engineSound;
        turretRotationSound = tankController.turretRotationSound;
        shotSound = tankController.shotSound;
        blowSound = tankController.blowSound;
        explosionSound = tankController.explosionSound;
        respawnSound = tankController.respawnSound;
        maxSpeed = tankController.maxSpeed;
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
}
