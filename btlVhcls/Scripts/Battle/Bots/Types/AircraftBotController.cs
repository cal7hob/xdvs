using System.Collections;
using Disconnect;
using UnityEngine;
using Hashtable = ExitGames.Client.Photon.Hashtable;

public class AircraftBotController : AircraftController
{
    protected AircraftBotAI aircraftBotAI;

    private const float MISSILE_AIMING_DURATION_MULTIPLIER = 3.0f;
    private const float STABILIZATION_INPUT_THRESHOLD = 0.2f;

    public override BotAI BotAI { get { return aircraftBotAI; } }
    public override float XAxisControl { get { return aircraftBotAI.XAxisControl; } }
    public override float YAxisControl { get { return aircraftBotAI.YAxisControl; } }
    public override float ThrottleLevelInputAxis { get { return aircraftBotAI.ThrottleLevelInputLevelInputAxis; } }
    public override bool IsRequirePrimaryFire { get { return FireButtonPressed; } }

    protected override bool FireButtonPressed { get { return aircraftBotAI.FireButtonPressed; } }
    protected override float MissileAimingDuration { get { return GameManager.MissileAimingDuration * MISSILE_AIMING_DURATION_MULTIPLIER; } }
    protected override float StabilizationInputThreshold { get { return STABILIZATION_INPUT_THRESHOLD; } }

    protected override void OnDestroy()
    {
        PhotonNetwork.RaiseEvent(
            (byte)BattleController.BattleEvent.BotSpawn,
            new Hashtable() { { "botPhotonId", PhotonView.viewID } },
            true,
            new RaiseEventOptions() { CachingOption = EventCaching.RemoveFromRoomCache }
        );
        aircraftBotAI.OnBotDestroy();
        base.OnDestroy();
    }

    protected override void Update()
    {
        if (!PhotonView || !IsAvailable)
            return;

        if (PhotonView.isMine)
        {
            aircraftBotAI.MyBotUpdate();
            SetMyAircraftSpeed(aircraftBotAI.ThrottleLevelInputLevelInputAxis);
        }
        else
        {
            aircraftBotAI.OthersBotUpdate();
            SetOthersAircraftSpeed();
        }
    }

#if UNITY_EDITOR
    protected override void OnDrawGizmos()
    {
        base.OnDrawGizmos();

        if (!BotDispatcher.DrawBotPaths || BotAI == null)
            return;

        Gizmos.color = IsMainsFriend ? Color.green : Color.red;
        Gizmos.DrawSphere(transform.position, 7.0f);

        Gizmos.color = Color.cyan;
        Gizmos.DrawSphere(BotAI.PositionToMove, 12.5f);

        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(transform.position, BotAI.PositionToMove);
    }
#endif

    public override void OnPhotonInstantiate(PhotonMessageInfo info)
    {
        base.OnPhotonInstantiate(info);

        if (!BattleConnectManager.IsMasterClient)
        {
            aircraftBotAI = new DummyAircraftBotAi(this);
            aircraftBotAI.Init((BotDispatcher.BotBehaviours)PhotonView.instantiationData[1]);
        }
    }

    protected override void OnVehicleTakesDamage(EventId id, EventInfo ei)
    {
        base.OnVehicleTakesDamage(id, ei);
        aircraftBotAI.OnBotTakesDamage(id, ei);
    }

    public override IEnumerator CheckOutOfMap()
    {
        while (true)
        {
            yield return new WaitForSeconds(checkOutOfMapDelay);

            worldMapCenterDirection = Map.MapCenterPos - transform.position;

            if (!Map.OutOfMapCol.bounds.Contains(transform.position) && Vector3.Dot(transform.forward, worldMapCenterDirection) < 0 && !IsFireLesson)
                StartCoroutine(RotateToMapCenter());
        }
    }

    protected override void EffectItself(VehicleEffect effect, bool inverted = false)
    {
        if (effect.Source == BonusItem.BonusType.Consumable)
            base.EffectItself(effect, inverted);
    }

    public override void ReanimateBot()
    {
        aircraftBotAI = new AircraftBotAI(this);
        aircraftBotAI.Init((BotDispatcher.BotBehaviours)PhotonView.instantiationData[1]);

        StartCoroutine(CheckOutOfMap());

        base.ReanimateBot();
    }

    public override void UpdateBotPrefabs(VehicleController nativeController)
    {
        AircraftController aircraftBotController = nativeController as AircraftController;

        if (aircraftBotController == null)
            return;

        data = aircraftBotController.data;
        shotPrefab = aircraftBotController.shotPrefab;
        hitPrefab = aircraftBotController.hitPrefab;
        explosionPrefab = aircraftBotController.explosionPrefab;
        shootEffectPoints = aircraftBotController.shootEffectPoints;
        engineSound = aircraftBotController.engineSound;
        shotSound = aircraftBotController.shotSound;
        blowSound = aircraftBotController.blowSound;
        explosionSound = aircraftBotController.explosionSound;
        respawnSound = aircraftBotController.respawnSound;
        MaxSpeed = aircraftBotController.MaxSpeed;
        centerOfMass = aircraftBotController.centerOfMass;
        continuousFire = aircraftBotController.continuousFire;
        shotCorrection = aircraftBotController.shotCorrection;
        rotationSpeedQualifier = aircraftBotController.rotationSpeedQualifier;

        lookPoint = aircraftBotController.lookPoint;
        shipTransform = aircraftBotController.shipTransform;
        minSpeed = aircraftBotController.minSpeed;
        acceleration = aircraftBotController.acceleration;
        checkOutOfMapDelay = aircraftBotController.checkOutOfMapDelay;
        gunHeatPerShot = aircraftBotController.gunHeatPerShot;
        coolingSpeed = aircraftBotController.coolingSpeed;
        stabilizationSpeed = aircraftBotController.stabilizationSpeed;
        accelerometerDeadZone = aircraftBotController.accelerometerDeadZone;

        shotSounds = aircraftBotController.shotSounds;
        collisionSounds = aircraftBotController.collisionSounds;
        minEnginePitch = aircraftBotController.minEnginePitch;
        minEngineVolume = aircraftBotController.minEngineVolume;
        maxEnginePitch = aircraftBotController.maxEnginePitch;
        maxEngineVolume = aircraftBotController.maxEngineVolume;

        indicatorPoint = aircraftBotController.indicatorPoint;
        rocketLaunchPoints = aircraftBotController.rocketLaunchPoints;
        animationController = aircraftBotController.animationController;

        maxInclineAngle = aircraftBotController.maxInclineAngle;
        inclineSmooth = aircraftBotController.inclineSmooth;
        verticalTorqueAcceleration = aircraftBotController.verticalTorqueAcceleration;
        horizontalTorqueAcceleration = aircraftBotController.horizontalTorqueAcceleration;
        torqueBrake = aircraftBotController.torqueBrake;
        stabilizationDelay = aircraftBotController.stabilizationDelay;
        minUnstabilizedAngle = aircraftBotController.minUnstabilizedAngle;

        rocketGuidanceSound = aircraftBotController.rocketGuidanceSound;
    } 

    protected void SetMyAircraftSpeed(float throttleVal)
    {
        requiredSpeed = Mathf.Lerp(minSpeed, MaxSpeed, throttleVal);

        accelerationDirection
            = HelpTools.Approximately(currentSpeed, requiredSpeed)
                ? 0
                : Mathf.Sign(requiredSpeed - currentSpeed);

        currentSpeed = Mathf.MoveTowards(currentSpeed, requiredSpeed, acceleration * Time.deltaTime);
    }

    protected void SetOthersAircraftSpeed()
    {
        float correctSpeed = correctVelocity.magnitude;

        if (!Mathf.Approximately(currentSpeed, correctSpeed))
        {
            currentSpeed = Mathf.MoveTowards(currentSpeed, correctVelocity.magnitude, acceleration * Time.deltaTime);
            accelerationDirection = 1f;
        }
        else
        {
            accelerationDirection = 0f;
        }
    }
}
