using UnityEngine;
using System.Collections;
using CodeStage.AntiCheat.ObscuredTypes;
using XDevs.LiteralKeys;

using Hashtable = ExitGames.Client.Photon.Hashtable;

public abstract class FlightController : VehicleController
{
    [Header("Настройки для летающего транспорта")]

    [Header("Ссылки")]
    public Transform shipTransform;

    [Header("Управление")]
    public ObscuredFloat minSpeed;
    public ObscuredFloat acceleration = 5;
    public ObscuredFloat checkOutOfMapDelay = 2;
    public ObscuredFloat gunHeatPerShot = 0.1f;
    public ObscuredFloat coolingSpeed = 0.3f;
    public ObscuredFloat stabilizationSpeed = 60;
    public ObscuredFloat accelerometerDeadZone = 0.15f;

    [Header("Звуки")]
    public AudioClip[] shotSounds;
    public AudioClip[] collisionSounds;
    public float minEnginePitch = 1.0f;
    public float minEngineVolume = 0.5f;
    public float maxEnginePitch = 2.75f;
    public float maxEngineVolume = 1.25f;

    [Header("Всякое")]
    public Vector3 cameraPosition = new Vector3(0.0f, 5.0f, -20.0f);

    protected int currentShotPointIndex;
    protected ObscuredFloat currentSpeed = 10;
    protected ObscuredFloat accelerationDirection;
    protected ObscuredFloat requiredSpeed = 1;

    private static readonly ObscuredFloat ACCELERATION_RATIO = 3.0f;
    private static readonly ObscuredFloat SPEED_RATIO = 2.0f;
    private static readonly ObscuredFloat ODOMETER_RATIO = 0.04f;
    private static readonly ObscuredFloat MAX_SHOOT_ANGLE = 20.0f;
    private static readonly ObscuredFloat CORRECTION_TIME = 0.5f;

    protected bool outOfMapRotated;
    protected Vector3 worldMapCenterDirection;
    private Camera camera2d;

    public virtual float ThrottleLevelInputAxis
    {
        get { return XDevs.Input.GetAxis("Accelerator"); }
    }

    public override Transform Turret
    {
        get { return turret; }
    }

    public override float WeaponReloadingProgress
    {
        get { return weapons[DefaultShellType].HeatingProgress; }
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

    override public float CurrentSpeed {
        get { return currentSpeed; }
    }

    public float TargetSpeed
    {
        get { return requiredSpeed; }
    }

    public float AccelerationProgress
    {
        get { return currentSpeed / MaxSpeed; }
    }

    public float PureAccelerationProgress
    {
        get { return (currentSpeed - minSpeed) / (MaxSpeed - minSpeed); }
    }

    protected override float OdometerRatio
    {
        get { return ODOMETER_RATIO; }
    }

    protected override float SpeedRatio
    {
        get { return SPEED_RATIO; }
    }

    public override float MaxShootAngle
    {
        get { return MAX_SHOOT_ANGLE; }
    }

    protected override bool NeedCorrectAimY
    {
        get { return false; }
    }

    protected override float VertAimCapture
    {
        get { return 17.0f; }
    }

    protected override float HorizAimCapture
    {
        get { return 25.5f; }
    }

    protected override float CorrectionTime
    {
        get { return CORRECTION_TIME; }
    }

    protected AudioClip ShotSound
    {
        get { return shotSounds != null && shotSounds.Length > 0 ? shotSounds.GetRandomItem() : shotSound; }
    }

    /// <summary>
    /// Костыльное свойство, для костыльного урока в боевом туторе.
    /// </summary>
    protected bool IsFireLesson
    {
        get { return ProfileInfo.IsBattleTutorial && BattleTutorial.Instance.CurrentBattleLesson == BattleTutorial.BattleLessons.fire; }
    }

    /* UNITY SECTION */

    public override void OnPhotonInstantiate(PhotonMessageInfo info)
    {
        base.OnPhotonInstantiate(info);

        Dispatcher.Subscribe(EventId.TankRespawned, OnTankRespawned);
        Dispatcher.Subscribe(EventId.StartBurstFire, OnStartBurstFire);
        Dispatcher.Subscribe(EventId.StopBurstFire, OnStopBurstFire);

        if (!PhotonView.isMine || IsBot)
            return;

        Dispatcher.Subscribe(EventId.QualityLevelChanged, OnQualityLevelChanged);

        camera2d = BattleGUI.Instance.GuiCamera;

        CalcParameters();

        rb.maxAngularVelocity = 1;
        rb.maxAngularVelocity = 1;

        StartCoroutine(CheckOutOfMap()); 
    }

    protected override void OnDestroy()
    {
        Dispatcher.Unsubscribe(EventId.TankRespawned, OnTankRespawned);
        Dispatcher.Unsubscribe(EventId.StartBurstFire, OnStartBurstFire);
        Dispatcher.Unsubscribe(EventId.StopBurstFire, OnStopBurstFire);

        base.OnDestroy();
    }

    /* PUBLIC SECTION */
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

        Transform currentShotEffectPoint = shootEffectPoints[currentShotPointIndex];

        currentShotPointIndex = (int)Mathf.Repeat(++currentShotPointIndex, shootEffectPoints.Count);

        Quaternion rotation
            = TargetAimed && IsMain
                ? Quaternion.LookRotation((TargetPosition - currentShotEffectPoint.position).normalized, currentShotEffectPoint.up)
                : currentShotEffectPoint.rotation;

        EffectPoolDispatcher.GetFromPool(
            _effect:        shotPrefab,
            _position:      currentShotEffectPoint.position,
            _rotation:      currentShotEffectPoint.rotation,
            useEffectMover: true,
            moverTarget:    currentShotEffectPoint);

        Shell shell = ShellPoolManager.GetShell(primaryShellInfo.shellPrefabName, currentShotEffectPoint.position, rotation);
        continuousFire = primaryShellInfo.continuousFire;

        shell.OwnerSpeed = Mathf.Abs(CurrentSpeed);

        shell.Activate(this, data.attack, hitMask);

        AudioDispatcher.PlayClipAtPosition(
            clip:       ShotSound,
            position:   currentShotEffectPoint.position,
            channel:    IsMain ? AudioSourceInsert.Channel.SpatialBlendOff : AudioSourceInsert.Channel.Master);

        return true;
    }

    [PunRPC]
    public override void Respawn(Vector3 position, Quaternion rotation, bool restoreLife, bool firstTime)
    {
        base.Respawn(position, rotation, restoreLife, firstTime);
    }

    public override int CalcDamage(int attack, bool critHit = false)
    {
        float result = attack;

        if (data.newbie)
            result /= GameManager.NEWBIE_DAMAGE_RATIO;

        result *= GameData.normDamageRatio;

        return Mathf.CeilToInt(result);
    }

    /* PRIVATE SECTION */
    protected virtual void OnTankRespawned(EventId id, EventInfo ei)
    {
        EventInfo_I info = (EventInfo_I)ei;

        if (info.int1 != PhotonView.ownerId)
            return;

        rb.angularVelocity = Vector3.zero;

        shipTransform.localEulerAngles = Vector3.zero;

        currentSpeed = minSpeed;

        weapons[DefaultShellType].InstantReload();
    }

    protected virtual void OnStartBurstFire(EventId eid, EventInfo ei)
    {
        EventInfo_II info = (EventInfo_II)ei;

        if (info.int1 != data.playerId)
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

    public override void StartBurst()
    {
        Dispatcher.Send(
            id:     EventId.StartBurstFire,
            info:   new EventInfo_II(data.playerId, (int)primaryShellInfo.type),
            target: Dispatcher.EventTargetType.ToAll);
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

        engineAudio.pitch = Mathf.Lerp(minEnginePitch, maxEnginePitch, t);
        engineAudio.volume = Settings.SoundVolume * Mathf.Lerp(minEngineVolume, maxEngineVolume, t);
    }

    public override float XAxisControl
    {
        get
        {
            if (BattleGUI.IsWindowOnScreen || !leftJoystick.IsOn)
            {
                return 0;
            }

            if (ProfileInfo.ControlOption == ControlOption.gyroscope)
                return GetAccelerometerValForScreenDimmension(isHorizontal: true);
            else
                return XDevs.Input.GetAxis("Turn Left/Right");
        }
    }

    public override float YAxisControl
    {
        get
        {
            if (BattleGUI.IsWindowOnScreen || !leftJoystick.IsOn)
            {
                return 0;
            }

            if (ProfileInfo.ControlOption == ControlOption.gyroscope)
                return GetAccelerometerValForScreenDimmension(isHorizontal: false);
            else
                return XDevs.Input.GetAxis("Turn Up/Down")*(ProfileInfo.isInvert ? -1 : 1);
        }
    }

    private void OnQualityLevelChanged(EventId id, EventInfo ei)
    {
        ShellPoolManager.ReloadAllPools();
    }

    public virtual IEnumerator CheckOutOfMap()
    {
        while (transform)
        {
            yield return new WaitForSeconds(checkOutOfMapDelay);

            worldMapCenterDirection = Map.MapCenterPos - transform.position;

            if (!Map.OutOfMapCol.bounds.Contains(transform.position) && Vector3.Dot(transform.forward, worldMapCenterDirection) < 0 && !IsFireLesson)
                StartCoroutine(RotateToMapCenter());

            if (!Map.OutOfMapWarningCol.bounds.Contains(transform.position) && Vector3.Dot(transform.forward, worldMapCenterDirection) < 0)
            {
                if (!outOfMapRotated && !IsFireLesson)
                    Notifier.Instance.ShowOutOfMapNotify();
            }
            else
            {
                Notifier.Instance.StopOutOfMapNotify();
                outOfMapRotated = false;
            }
        }
    }

    protected virtual IEnumerator RotateToMapCenter()
    {
        leftJoystick.IsOn = false;

        while (Vector3.Angle(transform.forward, worldMapCenterDirection) > 25)
        {
            worldMapCenterDirection = Map.MapCenterPos - transform.position;

            rb.rotation
                = Quaternion.RotateTowards(
                    from: rb.rotation,
                    to: Quaternion.LookRotation(worldMapCenterDirection),
                    maxDegreesDelta: stabilizationSpeed * Time.deltaTime);

            yield return null;
        }

        outOfMapRotated = true;
        leftJoystick.IsOn = true;
    }

    protected void MoveStaticGunsight()
    {
        if (BattleGUI.Instance.iGunSight != null)
            BattleGUI.Instance.iGunSight.ShowStaticGunSight(transform.position + transform.forward * 2000f);
        else if (BattleGUI.Instance.StaticGunsight != null)
        {
            Vector3 sightPoint = Camera.main.WorldToViewportPoint(transform.position + transform.forward * 2000f);
            sightPoint = camera2d.ViewportToWorldPoint(sightPoint);
            BattleGUI.Instance.StaticGunsight.transform.position = sightPoint;
        }
    }

    private void CalcParameters()
    {
        acceleration = MaxSpeed / ACCELERATION_RATIO;
    }

    protected virtual float GetAccelerometerValForScreenDimmension(bool isHorizontal)
    {
        float val = NormalizeAccelerometerAngle(isHorizontal);
        return Mathf.Abs(val) < accelerometerDeadZone ? 
            0 :
            val * 
            (isHorizontal ? JoystickManager.Instance.HorizontalGyroQualifier : JoystickManager.Instance.VerticalGyroQualifier) *
			(!isHorizontal && ProfileInfo.isInvert ? -1 : 1);
    }

    /// <summary>
    /// Convert angle such as -90 ...90 degrees to -1 ... 1
    /// </summary>
    /// <returns></returns>
    protected virtual float NormalizeAccelerometerAngle(bool isHorizontal)
    {
        return (float)GetAccelerometerAngleForScreenDimension(isHorizontal) / 90f;
    }

    /// <summary>
    /// return angle from -90 to 90
    /// </summary>
    public static int GetAccelerometerAngleForScreenDimension(bool isHorizontal)
    {
        Quaternion q = Quaternion.FromToRotation(ProfileInfo.initialAcceleration, UnityEngine.Input.acceleration);//3d offset relative saved acceleration state

        int sign = 0;
        float angle = 0;

        if (isHorizontal)
        {
            return (int)(Mathf.Clamp( UnityEngine.Input.acceleration.x - ProfileInfo.initialAcceleration.x, -1, 1) * 90f);//Пока по старинке, т.к. новый код некорректно работает по оси X

            //if (q.eulerAngles.z > 0 && q.eulerAngles.z < 90)
            //{
            //    angle = q.eulerAngles.z;
            //    sign = 1;
            //}
            //else if (q.eulerAngles.z > 270 && q.eulerAngles.z < 360)
            //{
            //    angle = 360 - q.eulerAngles.z;
            //    sign = -1;
            //}
            //else
            //    angle = 0;
        }
        else
        {
            if (q.eulerAngles.x > 0 && q.eulerAngles.x < 90)
            {
                angle = q.eulerAngles.x;
                sign = 1;
            }
            else if (q.eulerAngles.x > 270 && q.eulerAngles.x < 360)
            {
                angle = 360 - q.eulerAngles.x;
                sign = -1;
            }
            else
                angle = 0;
        }

        return sign * (int)angle;
    }
}
