using UnityEngine;
using XD;

public class SpaceshipBotController : SpaceshipController
{

    protected SpaceshipBotAI spaceshipBotAI;

    public override BotAI BotAI
    {
        get { return spaceshipBotAI; }
    }

    public override float XAxisControl { get { return spaceshipBotAI.XAxisControl; } }
    public override float YAxisControl { get { return spaceshipBotAI.YAxisControl; } }
    protected override bool FireButtonPressed { get { return spaceshipBotAI.FireButtonPressed; } }
    public override float ThrottleLevelInputAxis { get { return spaceshipBotAI.ThrottleLevelInputLevelInputAxis; } }

    protected override void OnDestroy()
    {
        spaceshipBotAI.OnBotDestroy();
        base.OnDestroy();
    }

    protected override void OnVehicleTakesDamage(EventId id, EventInfo ei)
    {
        spaceshipBotAI.OnBotTakesDamage(id, ei);
    }

    protected override void OnTargetAimed(EventId id, EventInfo ei)
    {
        spaceshipBotAI.OnBotAimed(id, ei);
    }
    
    protected override void OnPhotonInstantiate(PhotonMessageInfo info)
    {
        base.OnPhotonInstantiate(info);

        if (!PhotonNetwork.isMasterClient)
        {
            spaceshipBotAI = new DummySpaceshipBotAI(this);
            spaceshipBotAI.Init((BotDispatcher.BotBehaviours)PhotonView.instantiationData[1]);
            return;
        }

        spaceshipBotAI.OnBotPhotonInstantiate();
    }
    
    public override void UpdateBotAssets(VehicleController nativeController)
    {
        var spaceshipController = nativeController as SpaceshipController;

        if (spaceshipController == null)
        {
            return;
        }

        data = spaceshipController.data;
        Settings[Setting.MovingSpeed].Max = spaceshipController.Settings[Setting.MovingSpeed].Max;
        CenterOfMass = spaceshipController.CenterOfMass;
        continuousFire = spaceshipController.continuousFire;
        shotCorrection = spaceshipController.shotCorrection;
        rotationSpeedQualifier = spaceshipController.rotationSpeedQualifier;

        shipTransform = spaceshipController.shipTransform;
        minSpeed = spaceshipController.minSpeed;
        acceleration = spaceshipController.acceleration;
        checkOutOfMapDelay = spaceshipController.checkOutOfMapDelay;
        gunHeatPerShot = spaceshipController.gunHeatPerShot;
        coolingSpeed = spaceshipController.coolingSpeed;
        stabilizationSpeed = spaceshipController.stabilizationSpeed;
        accelerometerDeadZone = spaceshipController.accelerometerDeadZone;

        minEnginePitch = spaceshipController.minEnginePitch;
        minEngineVolume = spaceshipController.minEngineVolume;
        maxEnginePitch = spaceshipController.maxEnginePitch;
        maxEngineVolume = spaceshipController.maxEngineVolume;

        maxInclineAngle = spaceshipController.maxInclineAngle;
        inclineSmooth = spaceshipController.inclineSmooth;
        torqueAcceleration = spaceshipController.torqueAcceleration;
        torqueBrake = spaceshipController.torqueBrake;

        primaryEffects = spaceshipController.primaryEffects;
        trailEffects = spaceshipController.trailEffects;

        speedEffect = spaceshipController.speedEffect;
        spawnLens = spaceshipController.spawnLens;
        spawnEffect = spaceshipController.spawnEffect;
    }

    public override void ReanimateBot()
    {
        spaceshipBotAI = new SpaceshipBotAI(this);
        spaceshipBotAI.Init((BotDispatcher.BotBehaviours)PhotonView.instantiationData[1]);
        base.ReanimateBot();
    }

    public override void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        spaceshipBotAI.OnBotPhotonSerializeView(stream, info);
    }

    protected override void NormalUpdate()
    {
        
    }
}
