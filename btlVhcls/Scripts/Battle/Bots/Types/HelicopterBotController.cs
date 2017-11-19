using System.Collections;
using Disconnect;
using UnityEngine;
using Hashtable = ExitGames.Client.Photon.Hashtable;

public class HelicopterBotController : HelicopterController
{
    protected HelicopterBotAI helicopterBotAI;

    public override BotAI BotAI
    {
        get { return helicopterBotAI; }
    }

    public override bool IsRequirePrimaryFire { get { return FireButtonPressed; } }
    public override bool IsRequireSecondaryFire { get { return FireButtonPressed; } }
    public override float XAxisControl { get { return helicopterBotAI.XAxisControl; } }
    public override float YAxisControl { get { return helicopterBotAI.YAxisControl; } }
    public override float XAxisAltControl { get { return helicopterBotAI.XAxisAltControl; } }
    public override float YAxisAltControl { get { return helicopterBotAI.YAxisAltControl; } }
    protected override bool FireButtonPressed { get { return helicopterBotAI.FireButtonPressed; } }

    protected override void OnDestroy()
    {
        PhotonNetwork.RaiseEvent(
            (byte)BattleController.BattleEvent.BotSpawn,
            new Hashtable() { { "botPhotonId", PhotonView.viewID } },
            true,
            new RaiseEventOptions() { CachingOption = EventCaching.RemoveFromRoomCache }
        );
        helicopterBotAI.OnBotDestroy();
        base.OnDestroy();
    }

    protected override void OnVehicleTakesDamage(EventId id, EventInfo ei)
    {
        base.OnVehicleTakesDamage(id, ei);
        helicopterBotAI.OnBotTakesDamage(id, ei);
    }

    public override void OnPhotonInstantiate(PhotonMessageInfo info)
    {
        base.OnPhotonInstantiate(info);

        if (!BattleConnectManager.IsMasterClient)
        {
            helicopterBotAI = new DummyHelicopterBotAI(this);
            helicopterBotAI.Init((BotDispatcher.BotBehaviours)PhotonView.instantiationData[1]);
        }
    }

    public override IEnumerator CheckOutOfMap()
    {
        while (true)
        {
            yield return new WaitForSeconds(checkOutOfMapDelay);

            worldMapCenterDirection = Map.MapCenterPos - transform.position;

            //if (!Map.OutOfMapCol.bounds.Contains(transform.position) && Vector3.Dot(transform.forward, worldMapCenterDirection) < 0)
            //    StartCoroutine(RotateToMapCenter());
        }
    }

    protected override void EffectItself(VehicleEffect effect, bool inverted = false)
    {
        if (effect.Source == BonusItem.BonusType.Consumable)
            base.EffectItself(effect, inverted);
    }

    public override void ReanimateBot()
    {
        helicopterBotAI = new HelicopterBotAI(this);
        helicopterBotAI.Init((BotDispatcher.BotBehaviours)PhotonView.instantiationData[1]);

        StartCoroutine(CheckOutOfMap());

        base.ReanimateBot();
    }

    public override void UpdateBotPrefabs(VehicleController nativeController)
    {
        HelicopterController helicopterBotController = nativeController as HelicopterController;

        if (helicopterBotController == null)
            return;

        data = helicopterBotController.data;
        shotPrefab = helicopterBotController.shotPrefab;
        hitPrefab = helicopterBotController.hitPrefab;
        explosionPrefab = helicopterBotController.explosionPrefab;
        shootEffectPoints = helicopterBotController.shootEffectPoints;
        engineSound = helicopterBotController.engineSound;
        shotSound = helicopterBotController.shotSound;
        blowSound = helicopterBotController.blowSound;
        explosionSound = helicopterBotController.explosionSound;
        respawnSound = helicopterBotController.respawnSound;
        MaxSpeed = helicopterBotController.MaxSpeed;
        centerOfMass = helicopterBotController.centerOfMass;
        continuousFire = helicopterBotController.continuousFire;
        shotCorrection = helicopterBotController.shotCorrection;
        rotationSpeedQualifier = helicopterBotController.rotationSpeedQualifier;

        lookPoint = helicopterBotController.lookPoint;
        forCam = helicopterBotController.forCam;
        shipTransform = helicopterBotController.shipTransform;
        minSpeed = helicopterBotController.minSpeed;
        acceleration = helicopterBotController.acceleration;
        checkOutOfMapDelay = helicopterBotController.checkOutOfMapDelay;
        gunHeatPerShot = helicopterBotController.gunHeatPerShot;
        coolingSpeed = helicopterBotController.coolingSpeed;
        stabilizationSpeed = helicopterBotController.stabilizationSpeed;
        accelerometerDeadZone = helicopterBotController.accelerometerDeadZone;

        shotSounds = helicopterBotController.shotSounds;
        collisionSounds = helicopterBotController.collisionSounds;
        minEnginePitch = helicopterBotController.minEnginePitch;
        minEngineVolume = helicopterBotController.minEngineVolume;
        maxEnginePitch = helicopterBotController.maxEnginePitch;
        maxEngineVolume = helicopterBotController.maxEngineVolume;

        indicatorPoint = helicopterBotController.indicatorPoint;
        rocketLaunchPoints = helicopterBotController.rocketLaunchPoints;
        animationController = helicopterBotController.animationController;
        ircmLaunchPoint = helicopterBotController.ircmLaunchPoint;
        targetPoint = helicopterBotController.targetPoint;

        inclineSmooth = helicopterBotController.inclineSmooth;

        fireEffect = helicopterBotController.fireEffect;
        smokeEffect = helicopterBotController.smokeEffect;

        rocketGuidanceSound = helicopterBotController.rocketGuidanceSound;
        rocketThreatSound = helicopterBotController.rocketThreatSound;
        shootingSound = helicopterBotController.shootingSound;
        shootingEndSound = helicopterBotController.shootingEndSound;
    }

    protected override void Update()
    {
        if (!PhotonView || !IsAvailable)
            return;

        if (PhotonView.isMine)
            helicopterBotAI.MyBotUpdate();
        else
            helicopterBotAI.OthersBotUpdate();
    }

#if UNITY_EDITOR
    protected override void OnDrawGizmos()
    {
        base.OnDrawGizmos();

        if (BotAI == null)
            return;

        Gizmos.color = IsMainsFriend ? Color.green : Color.red;
        Gizmos.DrawIcon(transform.position, gameObject.name, true);
        Gizmos.DrawSphere(transform.position, 3.0f);

        Gizmos.color = Color.cyan;
        Gizmos.DrawIcon(BotAI.PositionToMove, "BotAI.PositionToMove", true);
        Gizmos.DrawSphere(BotAI.PositionToMove, 1.0f);

        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(transform.position, BotAI.PositionToMove);
    }
#endif
}