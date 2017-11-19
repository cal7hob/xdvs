using System.Collections;
using Disconnect;
using UnityEngine;
using Hashtable = ExitGames.Client.Photon.Hashtable;

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
        PhotonNetwork.RaiseEvent(
            (byte)BattleController.BattleEvent.BotSpawn,
            new Hashtable() { { "botPhotonId", PhotonView.viewID } },
            true,
            new RaiseEventOptions() { CachingOption = EventCaching.RemoveFromRoomCache }
        );
        spaceshipBotAI.OnBotDestroy();
        base.OnDestroy();
    }

    protected override void Update()
    {
        if (!PhotonView || !IsAvailable)
            return;

        if (PhotonView.isMine)
        {
            spaceshipBotAI.MyBotUpdate();
            SetMyShipSpeed(spaceshipBotAI.ThrottleLevelInputLevelInputAxis);
        }
        else
        {
            spaceshipBotAI.OthersBotUpdate();
            SetOthersShipsSpeed();
        }

        if (!HelpTools.Approximately(accelerationDirection, 0))
            DrawEngineJets();
    }

#if UNITY_EDITOR
    protected override void OnDrawGizmos()
    {
        base.OnDrawGizmos();

        if (BotAI == null)
            return;

        Gizmos.color = Color.red;
        Gizmos.DrawSphere(BotAI.PositionToMove, 4);
    }
#endif

    public override void OnPhotonInstantiate(PhotonMessageInfo info)
    {
        base.OnPhotonInstantiate(info);

        if (!BattleConnectManager.IsMasterClient)
        {
            spaceshipBotAI = new DummySpaceshipBotAI(this);
            spaceshipBotAI.Init((BotDispatcher.BotBehaviours)PhotonView.instantiationData[1]);
        }
    }

    protected override void OnVehicleTakesDamage(EventId id, EventInfo ei)
    {
        base.OnVehicleTakesDamage(id, ei);
        spaceshipBotAI.OnBotTakesDamage(id, ei);
    }

    public override IEnumerator CheckOutOfMap()
    {
        while (true)
        {
            yield return new WaitForSeconds(checkOutOfMapDelay);          
            worldMapCenterDirection = Map.MapCenterPos - transform.position;

            if (!Map.OutOfMapCol.bounds.Contains(transform.position) && Vector3.Dot(transform.forward, worldMapCenterDirection) < 0)
                BotAI.PositionToMove = Map.MapCenterPos;
        }
    }

    protected override void EffectItself(VehicleEffect effect, bool inverted = false)
    {
        if (effect.Source == BonusItem.BonusType.Consumable)
            base.EffectItself(effect, inverted);
    }

    public override void UpdateBotPrefabs(VehicleController nativeController)
    {
        var spaceshipController = nativeController as SpaceshipController;

        if (spaceshipController == null)
        {
            return;
        }

        data = spaceshipController.data;
        shotPrefab = spaceshipController.shotPrefab;
        hitPrefab = spaceshipController.hitPrefab;
        explosionPrefab = spaceshipController.explosionPrefab;
        shootEffectPoints = spaceshipController.shootEffectPoints;
        engineSound = spaceshipController.engineSound;
        shotSound = spaceshipController.shotSound;
        blowSound = spaceshipController.blowSound;
        explosionSound = spaceshipController.explosionSound;
        respawnSound = spaceshipController.respawnSound;
        MaxSpeed = spaceshipController.MaxSpeed;
        centerOfMass = spaceshipController.centerOfMass;
        continuousFire = spaceshipController.continuousFire;
        shotCorrection = spaceshipController.shotCorrection;
        rotationSpeedQualifier = spaceshipController.rotationSpeedQualifier;

        lookPoint = spaceshipController.lookPoint;
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
        sparksEffectPrefab = spaceshipController.sparksEffectPrefab;
    }

    public override void ReanimateBot()
    {
        spaceshipBotAI = new SpaceshipBotAI(this);
        spaceshipBotAI.Init((BotDispatcher.BotBehaviours)PhotonView.instantiationData[1]);
        StartCoroutine(CheckOutOfMap());

        base.ReanimateBot();
    }
}
