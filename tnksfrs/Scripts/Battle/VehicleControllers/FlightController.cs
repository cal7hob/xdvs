using UnityEngine;
using System.Collections;
using CodeStage.AntiCheat.ObscuredTypes;
using XD;
using XDevs.LiteralKeys;

using Hashtable = ExitGames.Client.Photon.Hashtable;

public abstract class FlightController : VehicleController
{
    [Header("Настройки для летающего транспорта")]

    [Header("Ссылки")]
    public Transform shipTransform;
    public Transform viewPoint;

    [Header("Управление")]
    public ObscuredFloat minSpeed;
    public ObscuredFloat acceleration = 5;
    public ObscuredFloat checkOutOfMapDelay = 2;
    public ObscuredFloat gunHeatPerShot = 0.1f;
    public ObscuredFloat coolingSpeed = 0.3f;
    public ObscuredFloat stabilizationSpeed = 60;
    public ObscuredFloat accelerometerDeadZone = 0.15f;

    [Header("Звуки")]    
    public AudioClip[] collisionSounds;
    public float minEnginePitch = 1.0f;
    public float minEngineVolume = 0.5f;
    public float maxEnginePitch = 2.75f;
    public float maxEngineVolume = 1.25f;

    protected int currentShotPointIndex;
    protected ObscuredFloat currentSpeed = 10;
    protected ObscuredFloat accelerationDirection;
    protected ObscuredFloat requiredSpeed = 1;

    private static readonly ObscuredFloat ACCELERATION_RATIO = 3.0f;
    private static readonly ObscuredFloat SPEED_RATIO = 2.0f;
    private static readonly ObscuredFloat ODOMETER_RATIO = 0.04f;
    private static readonly ObscuredFloat CORRECTION_TIME = 0.5f;

    protected bool outOfMapRotated;
    protected Vector3 worldMapCenterDirection;
    protected Vector3 mapCenterPos;
    protected Collider outOfMapWarningCol;
    protected Collider outOfMapCol;
    private Camera camera2d;

    #if UNITY_EDITOR
    private string previousEngineSoundName;
    #endif

    public virtual float ThrottleLevelInputAxis
    {
        get 
        { 
            return XDevs.Input.GetAxis("Accelerator"); 
        }
    }

   /* public override Transform Turret
    {
        get 
        { 
            return turret; 
        }
    }*/

    public override float WeaponReloadingProgress
    {
        get 
        { 
            return weapons[DefaultShellType].HeatingProgress; 
        }
    }

    public override float GetHeating(GunShellInfo.ShellType shellType)
    {
        switch (shellType)
        {
            case GunShellInfo.ShellType.Usual:
                return gunHeatPerShot;

            default:
                return 0.0f;
        }
    }

    public override float GetCooling(GunShellInfo.ShellType shellType)
    {
        switch (shellType)
        {
            case GunShellInfo.ShellType.Usual:
                return coolingSpeed;

            default:
                return 0.0f;
        }
    }

    public float CurrentSpeed
    {
        get 
        { 
            return currentSpeed; 
        }
    }

    public float TargetSpeed
    {
        get 
        { 
            return requiredSpeed; 
        }
    }

    public float AccelerationProgress
    {
        get 
        { 
            return currentSpeed / Settings[Setting.MovingSpeed].Max; 
        }
    }

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
            return SPEED_RATIO; 
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

    protected override void OnPhotonInstantiate(PhotonMessageInfo info)
    {
        base.OnPhotonInstantiate(info);

        Dispatcher.Subscribe(EventId.TankRespawned, OnTankRespawned);
        Dispatcher.Subscribe(EventId.StartBurstFire, OnStartBurstFire);
        Dispatcher.Subscribe(EventId.StopBurstFire, OnStopBurstFire);
        Dispatcher.Subscribe(EventId.ShellHit, OnShellHit, 2);

        if (!PhotonView.isMine || IsBot)
        {
            return;
        }

        Dispatcher.Subscribe(EventId.QualityLevelChanged, OnQualityLevelChanged);

       // camera2d = BattleGUI.Instance.GuiCamera;
        CalcParameters();

        rigidbody.maxAngularVelocity = 1;
        rigidbody.maxAngularVelocity = 1;
        
        var warnColObj = GameObject.FindWithTag(Tag.Items[Tag.Key.OutOfMapWarningCollider]);
        var outMapColObj = GameObject.FindWithTag(Tag.Items[Tag.Key.OutOfMapCollider]);

        outOfMapWarningCol = warnColObj ? warnColObj.GetComponent<Collider>(): null;
        outOfMapCol = outMapColObj ? outMapColObj.GetComponent<Collider>() : null;

        mapCenterPos = outMapColObj ? outMapColObj.transform.position : Vector3.zero;

        StartCoroutine(CheckOutOfMap()); 
    }

    protected override void OnDestroy()
    {
        Dispatcher.Unsubscribe(EventId.TankRespawned, OnTankRespawned);
        Dispatcher.Unsubscribe(EventId.StartBurstFire, OnStartBurstFire);
        Dispatcher.Unsubscribe(EventId.StopBurstFire, OnStopBurstFire);
        Dispatcher.Unsubscribe(EventId.ShellHit, OnShellHit);

        base.OnDestroy();
    }

    protected virtual void OnCollisionEnter(Collision collision)
    {
        PlayCollisionSound(collision);
    }

    private void PlayCollisionSound(Collision collision)
    {
        if (collision.relativeVelocity.magnitude > 6.0f && MiscTools.CheckIfLayerInMask(othersLayerMask, collision.gameObject.layer))
        {
            if (CollisionSound != null)
            {
                AudioDispatcher.PlayClipAtPosition(clip: CollisionSound, position: transform.position, volume: SoundControllerTankAR.COLLISION_VOLUME);
            }
        }
    }

    /* PUBLIC SECTION */
    public override bool PrimaryFire(Quaternion rotation)
    {       
        return true;
    }

    [PunRPC]
    public override void Respawn(Vector3 position, Quaternion rotation, bool restoreLife, bool firstTime)
    {
        base.Respawn(position, rotation, restoreLife, firstTime);
    }

    public override int CalcDamage(int attack, Collider collider)
    {
        float result = attack;
        /*
        if (data.newbie)
            result *= XD.Constants.NEWBIE_DAMAGE_RATIO;

        result *= GameData.normDamageRatio;
        */
        return Mathf.CeilToInt(result);
    }

    /* PRIVATE SECTION */
    protected virtual void OnTankRespawned(EventId id, EventInfo ei)
    {
        EventInfo_I info = (EventInfo_I)ei;

        if (info.int1 != PhotonView.ownerId)
            return;

        rigidbody.angularVelocity = Vector3.zero;

        shipTransform.localEulerAngles = Vector3.zero;

        currentSpeed = minSpeed;

        weapons[DefaultShellType].InstantReload();
    }

    protected virtual void OnStartBurstFire(EventId eid, EventInfo ei)
    {
        EventInfo_IIV info = (EventInfo_IIV)ei;

        if (info.int1 != PhotonView.ownerId)
            return;

        burst = true;

        if ((int)primaryShellInfo.type != info.int2 && PhotonView.isMine)
            primaryShellInfo = GunShellInfo.GetShellInfoForType((GunShellInfo.ShellType)info.int2);
    }

    protected virtual void OnStopBurstFire(EventId eid, EventInfo ei)
    {
        EventInfo_II info = (EventInfo_II)ei;

        if (info.int1 != PhotonView.ownerId)
            return;

        burst = false;
    }

    protected virtual void OnShellHit(EventId eid, EventInfo ei)
    {
        EventInfo_IIIIV info = (EventInfo_IIIIV)ei;

        if (info.int1 != data.playerId)
            return;

        var shellType = (GunShellInfo.ShellType)info.int4;
        Vector3 position = transform.TransformPoint(info.vector);

        if (shellType == GunShellInfo.ShellType.Usual && // Костыль для отрисовки попадания, т.к. другие клиенты не учитывают наведение.
            !GameData.IsGame(Game.BattleOfHelicopters))  // В вертолётах наведение учитывается.
        {
            Shell shell
                = ShellPoolManager.GetShell(
                    shellName:  GunShellInfo.GetShellInfoForType(shellType).shellPrefabName,
                    position:   Vector3.zero,
                    rotation:   Quaternion.identity);

            shell.Explosion(position: position, hitsVehicle: true);
        }

        int damage = info.int2;
        HPSystem.ChangeHitPoints(damage, info.int3, false);

        Dispatcher.Send(
            id:     EventId.TankTakesDamage,
            info:   new EventInfo_U(
                        info.int1,
                        damage,
                        info.int3,
                        info.int4,
                        position));

        if (IsBot)
            return;

        Hashtable props = new Hashtable { { "hl", HPSystem.Armor } };

        Player.SetCustomProperties(props);
    }

    public override void StartBurst()
    {
        Dispatcher.Send(EventId.StartBurstFire, new EventInfo_IIV(data.playerId, (int)primaryShellInfo.type, ShotPoint.forward), Dispatcher.EventTargetType.ToAll);
    }

    public override void StopBurst()
    {
        Dispatcher.Send(
            id:     EventId.StopBurstFire,
            info:   new EventInfo_II(data.playerId, (int)primaryShellInfo.type),
            target: Dispatcher.EventTargetType.ToAll);
    }

    protected override void SetEngineNoise(float t)
    {
		if (engineAudio == null)
			return;
		
        #if UNITY_EDITOR

        if (previousEngineSoundName != null)
        {
            foreach (AudioSource audioSource in gameObject.GetComponents<AudioSource>())
                Destroy(audioSource);

            SetEngineAudio();
        }        

        #endif

        engineAudio.pitch = Mathf.Lerp(minEnginePitch, maxEnginePitch, t);
        engineAudio.volume = global::Settings.SoundVolume * Mathf.Lerp(minEngineVolume, maxEngineVolume, t);
    }

    public override float XAxisControl
    {
        get
        {         
            float horizontalAccel;

            if (ProfileInfo.ControlOption == ControlOption.gyroscope)
            {
                horizontalAccel = Input.acceleration.x * JoystickManager.Instance.HorizontalGyroQualifier;

                if (horizontalAccel >= -accelerometerDeadZone && horizontalAccel <= accelerometerDeadZone)
                    horizontalAccel = 0;   
            }
            else
            { 
                horizontalAccel = XDevs.Input.GetAxis("Turn Left/Right");
            }

            return horizontalAccel;
        }
    }

    public override float YAxisControl
    {
        get
        {          
            float verticalAccel;

            if (ProfileInfo.ControlOption == ControlOption.gyroscope)
            {
                verticalAccel
                    = (Input.acceleration.y - global::Settings.InitialAcceleration.y)
                      *(ProfileInfo.isInvert ? -1 : 1)
                      *JoystickManager.Instance.VerticalGyroQualifier;

                if (verticalAccel >= -accelerometerDeadZone && verticalAccel <= accelerometerDeadZone)
                    verticalAccel = 0;
            }
            else
            {
                verticalAccel = XDevs.Input.GetAxis("Turn Up/Down")*(ProfileInfo.isInvert ? -1 : 1);
            }

            return verticalAccel;
        }
    }

    private void OnQualityLevelChanged(EventId id, EventInfo ei)
    {
        ShellPoolManager.ClearAllPools();
    }

    protected virtual IEnumerator CheckOutOfMap()
    {
        while (transform)
        {
            yield return new WaitForSeconds(checkOutOfMapDelay);

            worldMapCenterDirection = mapCenterPos - transform.position;

            if (!outOfMapCol.bounds.Contains(transform.position) && Vector3.Dot(transform.forward, worldMapCenterDirection) < 0)
                StartCoroutine(RotateToMapCenter());

            if (!outOfMapWarningCol.bounds.Contains(transform.position) && Vector3.Dot(transform.forward, worldMapCenterDirection) < 0)
            {
                if (!outOfMapRotated)
                {
                    //TODO: на наш механизм
                    //Notifier.Instance.ShowOutOfMapNotify();
                }
            }
            else
            {
                //TODO: на наш механизм
                //Notifier.Instance.StopOutOfMapNotify();
                outOfMapRotated = false;
            }
        }

        yield return null;
    }

    protected virtual IEnumerator RotateToMapCenter()
    {
        while (Vector3.Angle(transform.forward, worldMapCenterDirection) > 25)
        {
            worldMapCenterDirection = mapCenterPos - transform.position;

            rigidbody.rotation
                = Quaternion.RotateTowards(
                    from:               rigidbody.rotation,
                    to:                 Quaternion.LookRotation(worldMapCenterDirection),
                    maxDegreesDelta:    stabilizationSpeed * Time.deltaTime);

            yield return null;
        }

        outOfMapRotated = true;
    }

    protected void MoveStaticGunsight()
    {
      /*  if (BattleGUI.Instance.StaticGunsight == null)
            return;*/

        Vector3 sightPoint = Camera.main.WorldToViewportPoint(transform.position + transform.forward * 2000f);

        sightPoint = camera2d.ViewportToWorldPoint(sightPoint);

      //  BattleGUI.Instance.StaticGunsight.transform.position = sightPoint;
    }

    private void CalcParameters()
    {
        acceleration = Settings[Setting.MovingSpeed].Max / ACCELERATION_RATIO;
    }
}
