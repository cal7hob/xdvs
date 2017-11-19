using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using CodeStage.AntiCheat.ObscuredTypes;
using Disconnect;
using Pool;
using XDevs.LiteralKeys;
using Hashtable = ExitGames.Client.Photon.Hashtable;
using Random = UnityEngine.Random;

public struct AimPointInfo
{
    public Vector3 point;
    public RaycastHit hit;
    public VehicleController target;
    public bool critZone;

    public AimPointInfo(Vector3 point, RaycastHit hit)
    {
        this.point = point;
        this.hit = hit;
        target = hit.transform.GetComponentInParent<VehicleController>();
        critZone = hit.collider.tag == "CritZone";
    }

    public AimPointInfo(Vector3 point, VehicleController target)
    {
        this.point = point;
        this.target = target;
        hit = new RaycastHit();
        critZone = false;
    }

    public bool IsEmpty
    {
        get { return target == null; }
    }

}

public abstract class VehicleController : MonoBehaviour
{
    protected const float MAX_SYNC_TIME = 0.2f;
    protected const float DEFAULT_SOUND_DISTANCE = 65.0f;

    [Header("Данные")]
    public ObscuredInt id; // TODO: убрать костыльное поле в TankData после поднятия версии комнаты фотона.
    public ObscuredInt tankGroup;
    public TankData data;

    [Header("Префабы")]
    [AssetPathGetter]
    public string terrainHitPrefabPath;
    [AssetPathGetter]
    public string hitPrefabPath;
    [AssetPathGetter]
    public string explosionPrefabPath;
    [AssetPathGetter]
    public string shotPrefabPath;
    [AssetPathGetter]
    public string shellPrefabPath;

    [Header("Ссылки")]
    public Transform lookPoint;
    public Transform cameraEndPoint;
    public Transform forCam;
    public List<Transform> shootEffectPoints = null;

    [Header("Звуки")]
    public AudioClip engineSound;
    public AudioClip turretRotationSound;
    public AudioClip shotSound;
    public AudioClip blowSound;
    public AudioClip explosionSound;
    public AudioClip respawnSound;

    [Header("Физика")]
    public ObscuredFloat maxSpeed = 5;
    public Vector3 centerOfMass;

    [Header("Прочее")]
    public bool continuousFire;
    public float shotCorrection = 0.3f;
    public float turretRotationSpeedQualifier = 0.03f;
    public float rotationSpeedQualifier = 1.2f;
    public AimingController aimingController;
    public TurretController turretController;
    public Animation shootAnimation;

    #region синхрон по сети
    protected float syncTime;
    protected float rotSyncSpeed;
    protected Vector3 posSyncVelocity = Vector3.zero;
    protected Vector3 correctPosition;
    protected Vector3 correctVelocity;
    protected Quaternion correctRotation;
    #endregion

    protected AudioSource engineAudio;
    protected new Renderer renderer;
    protected GunShellInfo secondaryShellInfo;
    protected int hitMask;
    protected bool isAvailable;
    protected Vector3 storedVehiclePosition;
    protected float correctTurretAngle;
    protected bool settingSpawnPosition = true;
    protected static int terrainLayer;
    protected LayerMask othersLayerMask;
    protected int ownLayer; // Слой своего vehicle
    protected double kickBotAt; // Когда выгнать из комнаты, если бот
    protected bool burst;
    protected Rigidbody rb;
    protected bool isExploded;
    protected ObscuredFloat odometer;
    protected PhotonPlayer player;
    protected BodykitController bodykitController;
    protected Transform shotPoint;
    protected Transform turret;
    protected JoystickController leftJoystick;
    protected JoystickController rightJoystick;


    private VehicleController target;
    private Vector3 targetPosition;

    //private const ShellType DEFAULT_SHELL_TYPE = ShellType.Usual;
    private static readonly ObscuredFloat DEFAULT_ROCKET_FIRE_RATE = 6.0f;

    private readonly Dictionary<int, VehicleEffect> effects = new Dictionary<int, VehicleEffect>(4);
    private readonly List<VehicleEffect> effectsToCancel = new List<VehicleEffect>(3); // Для отмены эффектов. После использования очищать.

    private VehicleInfo vehicleInfo;
    private AimPointInfo aimPointInfo;
    private TankIndicator indicator;
    private PlayerStat statistics;
    private bool visible;
    private bool cheatActivated;
    private bool slowpokeActivated;
    private float inactivityTime;
    private Vector3 indicatorDelta;
    private Transform body;
    private Transform bodyMeshTransform;
    private Renderer[] renderers;
    private Collider[] boundColliders;
    private VehicleMarker vehicleMarker;
    private IEnumerator changingVehicleMaterial;
    private bool existanceSynchronized;
    private Dictionary<int, BattleConsumable> consumables = new Dictionary<int, BattleConsumable>();

    public bool FirePrimaryBtn { get { return XDevs.Input.GetButton("Fire Primary"); } }
    public bool FirePrimaryBtnDown { get { return XDevs.Input.GetButtonDown("Fire Primary"); } }
    public bool DoubleTap { get { return XDevs.Input.GetButton("Double Tap"); } }

    public Transform CameraEndPoint { get { return cameraEndPoint; } }

    #region  Abstract Properties

    protected abstract float OdometerRatio { get; }

    protected abstract float SpeedRatio { get; }

    public abstract float MaxShootAngle { get; }

    protected abstract float CorrectionTime { get; }

    protected abstract bool NeedCorrectAimY { get; }

    #endregion

    #region  Virtual Properties

    protected virtual bool FireButtonPressed
    {
        get
        {
            return continuousFire ?
                FirePrimaryBtn || (ProfileInfo.isFireOnDoubleTap && DoubleTap) :
                FirePrimaryBtnDown || (ProfileInfo.isFireOnDoubleTap && DoubleTap);
        }
    }

    public bool TargetAimed { get { return aimingController != null && aimingController.Target != null; } }

    public VehicleController Target
    {
        get { return aimingController == null ? target : aimingController.Target; }
        protected set { target = value; }
    }

    public Vector3 TargetPosition
    {
        get
        {
            return TargetAimed ? aimingController.Target.transform.position : targetPosition;
        }
        protected set { targetPosition = value; }
    }

    public Vector3 ViewPoint //позиция в которую должны смотреть камера и пушка
    {
        get
        {
            return TargetPosition + Target.transform.up * 0.5f;
        }
    }

    public CruiseControl.CruiseControlState CruiseControlState { get; protected set; }

    public Quaternion CorrectRotation { get { return correctRotation; } }

    public float CorrectTurretAngle { get { return correctTurretAngle; } }

    public bool SettingSpawnPosition { get { return settingSpawnPosition; } }

    public virtual float TurretAxisControl
    {
        get
        {
            return !BattleGUI.IsWindowOnScreen ? XDevs.Input.GetAxis("Turret Rotation") : 0;
        }
    }

    public virtual float AutoTurretAxisControl
    {
        get
        {
            return !BattleGUI.IsWindowOnScreen ? XDevs.Input.GetAxis("Auto Turret Rotation") : 0;
        }
    }

    public virtual float XAxisControl
    {
        get
        {
            return XDevs.Input.GetAxis("Turn Left/Right");
        }
    }

    public virtual float YAxisControl
    {
        get
        {
            return CruiseControlState.YAxisControl();
        }
    }

    protected virtual float XAxisAltControl
    {
        get
        {
            return XDevs.Input.GetAxis("Strafe Left/Right");
        }
    }

    protected virtual float YAxisAltControl
    {
        get
        {
            return XDevs.Input.GetAxis("Turn Up/Down");
        }
    }

    public virtual bool IsRequirePrimaryFire
    {
        get
        {
            return PrimaryFireIsOn && FireButtonPressed && (!BattleGUI.IsWindowOnScreen || IsBot);
        }
    }

    public virtual bool IsRequireSecondaryFire
    {
        get { return false && SecondaryFireIsOn; }
    }

    public virtual bool IsCrashing
    {
        get { return false; }
        set { }
    }

    public virtual float WeaponReloadingProgress
    {
        get
        {
            return turretController.WeaponReloadingProgress;
        }
    }

    public virtual Transform Turret
    {
        get { return null; }
        set { turret = value; }
    }

    public virtual Transform ShotPoint
    {
        get { return null; }
        set { shotPoint = value; }
    }

    public virtual Transform CannonEnd
    {
        get { return null; }
    }

    public virtual Vector3 TargetPoint
    {
        get { return transform.position; }
    }

    public virtual Vector3 Velocity
    {
        get
        {
            return IsMine ? rb.velocity : posSyncVelocity;
        }
    }

    public virtual Vector3 AngularVelocity
    {
        get { return IsMine ? rb.angularVelocity : Vector3.zero; }
    }

    public virtual Renderer Renderer
    {
        get { return renderer ?? (renderer = GetComponent<Renderer>()); }
    }

    public virtual float YAxisAcceleration { get { return 0; } }

    public virtual float XAxisAcceleration { get { return 0; } }

    protected virtual Vector3 IndicatorDeltaOffset
    {
        get { return Vector3.zero; }
    }

    protected virtual Transform IndicatorPoint
    {
        get { return null; }
    }

    protected virtual Transform CritZones
    {
        get { return null; }
    }

    protected virtual Transform Bumper
    {
        get { return null; }
    }
    public virtual float VertAimCapture
    {
        get { return 20.0f; }
    }

    public virtual float HorizAimCapture
    {
        get { return 0.1f; }
    }

    public virtual float MaxAimDistance
    {
        get { return 500.0f; }
    }

    public virtual BotAI BotAI { get { return null; } }

    public virtual Transform Body { get { return body = body ?? transform.Find("Body") ?? transform.Find("Mesh"); } }

    public virtual Transform BodyMeshTransform { get { return bodyMeshTransform = bodyMeshTransform ?? transform.FindInHierarhy("Mesh_Body") ?? Body; } }

    public virtual Transform ForCam { get { return forCam = forCam ?? turret.Find("ForCamera"); } }


    #endregion

    #region UsualProperties

    protected GunShellInfo primaryShellInfo;
    public GunShellInfo PrimaryShellInfo { get { return primaryShellInfo; } }

    public Vector3 CorrectPosition { get { return correctPosition; } }

    public int OthersLayerMask { get { return othersLayerMask; } }

    public int OwnLayer { get { return ownLayer; } }

    public double KickBotAt { get { return kickBotAt; } }

    public bool Burst { get { return burst; } }

    public Rigidbody Rb { get { return rb; } }

    public bool IsExploded { get { return isExploded; } }

    public float Odometer { get { return odometer; } }

    public PhotonPlayer Player { get { return player; } }

    public PlayerStat Statistics { get { return statistics; } }

    public BodykitController BodykitController { get { return bodykitController; } }

    public Mesh BodyMesh { get; private set; }

    public Bounds BodyMeshBounds { get; private set; }

    public int EnemyLayerMask { get; private set; }
    public bool TurretCentering { get; set; }

    public Vector3 CameraTranslationAxis { get; protected set; }

    public UnityEngine.AI.NavMeshObstacle SelfObstacle { get; protected set; }

    public PhotonView PhotonView { get; protected set; }

    private Dictionary<StatisticKey, string> statisticKeys = new Dictionary<StatisticKey, string>();
    public string GetStringKey(StatisticKey type)
    {
        string res;
        if (!statisticKeys.TryGetValue(type, out res))
        {
            res = StatisticType.GetKey(type);
            if (isBot)
            {
                res = "bt" + res + data.playerId;
            }
            statisticKeys.Add(type, res);
        }
        return res;
    }

    public string this[StatisticKey type]
    {
        get { return GetStringKey(type); }

    }

    public void SetCustomProperties(StatisticKey type, object param)
    {
        if (isBot)
        {
            PhotonNetwork.room.SetCustomProperties(new Hashtable { { this[type], param } });
        }
        else
        {
            PhotonNetwork.player.SetCustomProperties(new Hashtable { { this[type], param } });
        }
    }

    private bool isBotAlreadyChecked = false;
    private bool isBot = false;
    public bool IsBot
    {
        get
        {
            if (isBotAlreadyChecked)
            {
                return isBot;
            }
            else
            {
                isBotAlreadyChecked = true;
                isBot = PhotonView.isSceneView;
                return isBot;
            }
        }
    }

    public bool IsSeenByCamera
    {
        get { return renderers[0].isVisible; }
    }

    public bool IsVisible
    {
        get { return visible; }
        set
        {
            if (visible == value)
            {
                return;
            }

            visible = value;

            foreach (Renderer rend in renderers)
            {
                if (rend != null)
                {
                    rend.enabled = visible;
                }
            }
        }
    }

    public bool IsInParallelWorld
    {
        get
        {
            return gameObject.layer == StaticContainer.ParallelWorldLayer;
        }

        set
        {
            int layer = value ? StaticContainer.ParallelWorldLayer : ownLayer;
            Transform[] children = GetComponentsInChildren<Transform>(true);
            foreach (Transform child in children)
            {
                if (child != Bumper)
                {
                    child.gameObject.layer = layer;
                }
            }

            if (Bumper)
            {
                Bumper.gameObject.layer = LayerMask.NameToLayer(Layer.Items[isAvailable ? Layer.Key.TankBumper : Layer.Key.ParallelWorld]);
            }
        }
    }

    public int VehicleGroup
    {
        get { return vehicleInfo != null ? (int)vehicleInfo.vehicleGroup : (int)tankGroup; }
    }

    public bool IsAvailable
    {
        get { return isAvailable; }
        set
        {
            isAvailable = value;
            IsVisible = value;
            ApplyAvailability();
        }
    }

    public Vector3 IndicatorPointPosition
    {
        get { return transform.position + indicatorDelta; }
    }

    public Vector3 LocalVelocity
    {
        get { return transform.InverseTransformDirection(Velocity); }
    }

    public Vector3 LocalAngularVelocity
    {
        get { return transform.InverseTransformDirection(AngularVelocity); }
    }
    #endregion

    public Dictionary<int, VehicleEffect> Effects
    {
        get { return effects; }
    }

    public bool IsMain
    {
        get { return ((PhotonNetwork.connected && IsMine) || (data.playerId == BattleController.MyPlayerId)) && !IsBot; }
    }

    public int OwnerId
    {
        get { return IsBot ? PhotonNetwork.masterClient.ID : data.playerId; }
    }

    public bool IsMainsFriend
    {
        get
        {
            if (IsMain)
            {
                return false;
            }

            if (!BattleController.MyVehicle)
            {
                return false;
            }

            return StaticContainer.AreFriends(BattleController.MyVehicle, this);
        }
    }

    public int ExperienceBonus
    {
        get { return MiscTools.Round((int)(data.maxArmor / 10.5), 5); }
    }

    public int Armor
    {
        get { return data.armor; }
        set { data.armor = Mathf.Clamp(value, 0, data.maxArmor); }
    }

    public int MaxArmor
    {
        get { return data.maxArmor; }
        set
        {
            data.maxArmor = Mathf.Abs(value);

            if (data.armor > data.maxArmor)
            {
                data.armor = data.maxArmor;
            }
        }
    }

    public int Regeneration
    {
        get { return data.regeneration; }
        set
        {
            data.regeneration = value;
            if (!IsMine)
            {
                return;
            }

            StopCoroutine("Regenerate");
            if (value != 0)
            {
                StartCoroutine("Regenerate");
            }
        }
    }

    public int Shield
    {
        get { return data.shield; }
        set { data.shield = value; }
    }

    public float TakenDamageRatio
    {
        get { return data.takenDamageRatio; }
        set { data.takenDamageRatio = value; }
    }

    public int Attack
    {
        get { return data.attack; }
        set { data.attack = value; }
    }

    public float Speed
    {
        get { return data.speed; }
        set
        {
            data.speed = Mathf.Abs(value);
            CalculateMaxSpeed();
        }
    }

    public float ROF
    {
        get { return data.rof; }
        set { data.rof = Mathf.Clamp(value, 0, float.MaxValue); }
    }

    public int RocketAttack
    {
        get { return data.rocketAttack; }
        set { data.rocketAttack = value; }
    }

    public float IRCMROF
    {
        get { return data.ircmRof; }
        set { data.ircmRof = Mathf.Clamp(value, 0, float.MaxValue); }
    }

    public float CurrentSpeed { get { return LocalVelocity.z; } }

    public float CurrentSpeedRatio { get { return LocalVelocity.z / maxSpeed; } }

    public bool IsMine { get { return PhotonView.isMine; } }


    public bool DoubleExperience { get; private set; }

    public BoundsVertZone CritZonePlace { get; private set; }

    public bool PrimaryFireIsOn { get; set; }
    public bool SecondaryFireIsOn { get; set; }

    public int HitMask { get { return hitMask; } }

    public BattleConsumable GetConsumable(int consumableId)
    {
        BattleConsumable consumable;
        consumables.TryGetValue(consumableId, out consumable);
        return consumable;
    }

    public void SetContiniousFire()
    {
        this.continuousFire = PrimaryShellInfo.continuousFire;
    }
    public void SetContiniousFire(bool continiousFire)
    {
        this.continuousFire = continiousFire;
    }

    public void ActivateShell(Shell shell)
    {
        shell.Activate(
        owner: this,
        damage: data.attack,
        hitMask: hitMask);
    }
    public void ActivateShell(Shell shell, int victimId)
    {
        shell.Activate(
        owner: this,
        damage: data.attack,
        victimId: victimId,
        hitMask: hitMask);
    }
    public void ActivateShell(Shell shell, int victimId, ShellType shellType)
    {
        shell.Activate(
        owner: this,
        damage: data.rocketAttack,
        hitMask: hitMask,
        victimId: victimId,
        shellType: shellType);
    }

    public float GetParameterForCalc(VehicleEffect.ParameterType parameter)
    {
        switch (parameter)
        {
            case VehicleEffect.ParameterType.Armor:
                return data.maxArmor;
            case VehicleEffect.ParameterType.Attack:
                return data.attack;
            case VehicleEffect.ParameterType.IRCMRoF:
                return data.ircmRof;
            case VehicleEffect.ParameterType.MaxArmor:
                return data.maxArmor;
            case VehicleEffect.ParameterType.Regeneration:
                return data.regeneration;
            case VehicleEffect.ParameterType.RoF:
                return data.rof;
            case VehicleEffect.ParameterType.Shield:
                return data.shield;
            case VehicleEffect.ParameterType.RocketAttack:
                return data.rocketAttack;
            case VehicleEffect.ParameterType.Speed:
                return data.speed;
            case VehicleEffect.ParameterType.TakenDamageRatio:
                return data.takenDamageRatio;
            default:
                return 0f;
        }
    }

    public void SetParameter(VehicleEffect.ParameterType parameter, float value)
    {
        switch (parameter)
        {
            case VehicleEffect.ParameterType.Armor:
                Armor = Mathf.RoundToInt(value);
                break;
            case VehicleEffect.ParameterType.Attack:
                Attack = Mathf.RoundToInt(value);
                break;
            case VehicleEffect.ParameterType.IRCMRoF:
                IRCMROF = value;
                break;
            case VehicleEffect.ParameterType.MaxArmor:
                MaxArmor = Mathf.RoundToInt(value);
                break;
            case VehicleEffect.ParameterType.Regeneration:
                Regeneration = Mathf.RoundToInt(value);
                break;
            case VehicleEffect.ParameterType.RoF:
                ROF = value;
                break;
            case VehicleEffect.ParameterType.Shield:
                Shield = Mathf.RoundToInt(value);
                break;
            case VehicleEffect.ParameterType.RocketAttack:
                RocketAttack = Mathf.RoundToInt(value);
                break;
            case VehicleEffect.ParameterType.Speed:
                Speed = value;
                break;
            case VehicleEffect.ParameterType.TakenDamageRatio:
                TakenDamageRatio = value;
                break;
        }
    }

    protected virtual TurretController GetTurret(VehicleController vehicle, Animation shootAnimation)
    {
        return null;
    }

    protected virtual void OnPhotonInstantiate(PhotonMessageInfo info)
    {
        SetNavMeshObstacle();
        BodyMesh = BodyMeshTransform.gameObject.GetComponentInChildren<MeshFilter>(true).sharedMesh;
        BodyMeshBounds = BodyMesh.bounds;
        vehicleMarker = GetComponentInChildren<VehicleMarker>();

        object lastOdometer;
        if (BattleConnectManager.Instance.GetStoredValue("MyOdometer", out lastOdometer))
        {
            odometer = (ObscuredFloat)lastOdometer;
        }

        vehicleInfo = VehiclePool.Instance.GetItemById(id); // TODO: убрать после переноса id танка в TankData.

        primaryShellInfo = GunShellInfo.UsualShell;
        secondaryShellInfo = GunShellInfo.UsualShell;

        changingVehicleMaterial = QualityManager.Instance.ChangeObjectMaterials(gameObject);
        QualityManager.Instance.StartCoroutine(changingVehicleMaterial);

        rb = GetComponent<Rigidbody>();
        PhotonView = info.photonView ?? GetComponent<PhotonView>();
        player = PhotonView.owner;

        aimingController = new AimingController(this);
        turretController = GetTurret(this, shootAnimation);

        DoubleExperience = !IsBot && ProfileInfo.doubleExpVehicles.Contains(ProfileInfo.currentVehicle);

        if (!BattleController.vehicleData.TryGetValue(PhotonView.ownerId, out data))
        {
            data = (TankData)PhotonView.instantiationData[0];
            data.playerId = IsBot ? data.playerId : PhotonView.ownerId;
        }

        if (!IsMine && data.profileId == ProfileInfo.profileId && !Debug.isDebugBuild)
        {
            var query = new Dictionary<string, string>
            {
                { "tankId", ProfileInfo.currentVehicle.ToString() },
            };

            Http.Manager.ReportStats(
                location: "battle",
                action: "playerWithSameIdAlreadyConnected",
                query: query);

            BattleConnectManager.Instance.ForcedDisconnect();
            return;
        }

        //SetPropertyKeys();

        correctPosition = transform.position;
        correctRotation = transform.rotation;

        if (Bumper)
        {
            Bumper.gameObject.layer = LayerMask.NameToLayer(Layer.Items[Layer.Key.TankBumper]);
        }

        Subscriptions();

        if (IsMain)
        {
            Dispatcher.Subscribe(EventId.BeforeReconnecting, OnReconnect);
        }

        indicatorDelta = IndicatorPoint ? IndicatorPoint.localPosition : IndicatorDeltaOffset;
        // turretController.WeaponsInit();

        CalculateMaxSpeed();
        if (IsMain && !BattleConnectManager.Instance.FirstConnect)
        {
            statistics = BattleConnectManager.Instance.MyLastPlayerStat;
            statistics.teamId = data.teamId;
            statistics.playerId = PhotonNetwork.player.ID;
        }
        else
        {
            statistics = new PlayerStat(
                    playerId: data.playerId,
                    teamId: data.teamId,
                    playerLevel: data.playerLevel,
                    playerName: data.playerName,
                    countryCode: data.hideMyFlag ? string.Empty : (string)data.country,
                    vip: data.vip,
                    profileId: data.profileId,
                    clanName: data.clanName);
        }

        BattleController.FillDictionaries(this, statistics);

        rb.isKinematic = !IsMine;
        rb.collisionDetectionMode = IsMine ? CollisionDetectionMode.ContinuousDynamic : CollisionDetectionMode.Discrete;

        if (IsMain)
        {
            tag = Tag.Items[Tag.Key.Player];
            rb.centerOfMass = centerOfMass;
            transform.position = SpawnPoints.instance.GetCorrectPosition(this);
            storedVehiclePosition = transform.position;
            if (BattleConnectManager.Instance.FirstConnect)
            {
                Armor = MaxArmor;
            }

            if (cameraEndPoint)
            {
                CameraTranslationAxis = (cameraEndPoint.localPosition - forCam.localPosition).normalized;
            }

            InitConsumables(BattleController.battleInventory);
        }
        else
        {
            indicator = TankIndicators.AddIndicator(this);
            indicator.RedrawHealthBar(data.maxArmor);
            indicator.RedrawHealthBar(data.maxArmor);
            indicator.RedrawHealthBar(data.maxArmor);
            Respawn(transform.position, transform.rotation, true, true);
        }

        if (!ShotPoint)
        {
            DT.LogWarning(gameObject, "Shot point is null!");
        }

        renderers = GetComponentsInChildren<Renderer>(true);
        CollectBoundColliders();
        visible = renderers[0].enabled;
        bodykitController = GetComponent<BodykitController>();

        if (data.patternId != 0)
        {
            bodykitController.DrawCamouflage(PatternPool.Instance.GetItemById(data.patternId), vehicleInfo.id);
        }

        bodykitController.DrawDecal((data.decalId <= 0) ? null : DecalPool.Instance.GetItemById(data.decalId));

        SetEngineAudio();
        turretController.SetTurretAudio();

        bodykitController.SetShadowPlane();

        if (IsBot)
        {
            kickBotAt = (double)PhotonView.instantiationData[2];
            if (IsMine)
            {
                SetStartBotProperties();
            }
            else
            {
                CheckAllBotStats();
                TryGetBotParams();
            }
        }
        else
        {
            FillVehicleData();
        }

        PrimaryFireIsOn = true;
        SecondaryFireIsOn = true;

        RegisterForPhoton();
        if (IsMine && IsBot)
        {
            ReanimateBot();
            BotDispatcher.Instance.RegisterBotAI(BotAI);
        }

        SetMembership();

        if (!BattleController.MyVehicle && !IsMain)
        {
            Dispatcher.Subscribe(EventId.MainTankAppeared, OnMainVehicleAppeared);
        }

        if (JoystickManager.Instance.joysticks.Length > (int)JoystickManager.Joystics.left)
        {
            leftJoystick = JoystickManager.Instance.joysticks[(int)JoystickManager.Joystics.left];
        }

        if (JoystickManager.Instance.joysticks.Length > (int)JoystickManager.Joystics.right)
        {
            rightJoystick = JoystickManager.Instance.joysticks[(int)JoystickManager.Joystics.right];
        }

        if (StaticContainer.IsFriendOfMain(this))
        {
            Dispatcher.Send(EventId.ShowEnemy, new EventInfo_I(data.playerId));
        }

        Dispatcher.Send(EventId.TankJoinedBattle, new EventInfo_I(data.playerId));
    }

    public virtual void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        if (stream.isWriting)
        {
            if (IsExploded)
            {
                return;
            }

            stream.SendNext(transform.position);
            stream.SendNext(transform.rotation);

            if (Turret)
            {
                stream.SendNext(Turret.localEulerAngles.y);
            }
        }
        else
        {
            MarkActivity();
            int itemCount = stream.Count - stream.currentItem;

            correctPosition = (Vector3)stream.ReceiveNext();
            correctRotation = (Quaternion)stream.ReceiveNext();
            if (itemCount > 2)
            {
                correctTurretAngle = (float)stream.ReceiveNext();
            }

            if (settingSpawnPosition)
            {
                transform.position = correctPosition;
                transform.rotation = correctRotation;

                if (Turret)
                {
                    Turret.localEulerAngles = new Vector3(0, correctTurretAngle, 0);
                }

                settingSpawnPosition = false;
            }

            syncTime = Mathf.Min(PhotonNetwork.GetPing() * 0.001f, MAX_SYNC_TIME);
            rotSyncSpeed = Quaternion.Angle(transform.rotation, correctRotation) / syncTime;
        }
    }

    protected virtual void FixedUpdate() { }

    protected virtual void Update()
    {
        if (!PhotonView)
        {
            return;
        }

        if (!Debug.isDebugBuild)
        {
            inactivityTime += Time.deltaTime;

            if (inactivityTime > GameData.maxInactivityTime)
            {
                if (IsMine)
                {
                    BattleController.EndBattle(BattleController.EndBattleCause.Inactivity);
                    return;
                }

                if (PhotonNetwork.isMasterClient)
                {
                    PhotonNetwork.CloseConnection(PhotonView.owner);
                }
            }
        }

        if (IsMine && isAvailable)
        {
            aimingController.Aiming();
            UpdateEffects();
        }
        else
        {
            MoveClone();
        }

        if (!isAvailable)
        {
            return;
        }
        turretController.FullRealoadingUpdate();

        if (IsMain)
        {
            if (XDevs.Input.GetButtonDown("Center Turret") || (!ProfileInfo.isFireOnDoubleTap && XDevs.Input.GetButtonDown("Double Tap")))
            {
                turretController.TurretCentering = true;
            }

            turretController.TurretRotation();

            if (IsRequirePrimaryFire && turretController.PrimaryFire())
            {
                if (!burst)
                {
                    StartBurst();
                }
            }
            else
            {
                if (burst)
                {
                    StopBurst();
                }
            }

#if UNITY_EDITOR || UNITY_STANDALONE_WIN

            if (Input.GetButtonDown("ForceRespawn"))
            {
                MakeRespawn(forced: true, restoreLife: false, firstTime: false);
            }

            if (Input.GetKeyDown(KeyCode.B))
            {
                BattleController.EndBattle(BattleController.EndBattleCause.Timeouted);
            }
            //if (Input.GetKeyDown(KeyCode.Alpha1))
            //{
            //    TestConsumable(0);
            //}

            //if (Input.GetKeyDown(KeyCode.Alpha2))
            //{
            //    TestConsumable(1);
            //}

            //if (Input.GetKeyDown(KeyCode.Alpha3))
            //{
            //    TestConsumable(2);
            //}
#endif
        }
    }

    protected virtual IEnumerator ParametersRegistration()
    {
        yield break;
    }

    protected virtual void OnDestroy()
    {
        if (data.playerId == BattleController.MyPlayerId)
        {
            BattleConnectManager.Instance.StoreValue("MyOdometer", odometer);
        }
        Dispatcher.Send(EventId.HideEnemy, new EventInfo_I(data.playerId));

        BattleConnectManager.RemovePhotonMessageTarget(gameObject);

        TankIndicators.RemoveIndicator(data.playerId);
        Unsubscriptions();
        StopCoroutine(changingVehicleMaterial);

        // Чтобы результаты бота сохранились
        if (IsBot)
        {
            Dispatcher.Send(EventId.OffLayerInTeamMask, new EventInfo_II(data.teamId, ownLayer));
            Dispatcher.Send(EventId.TankOutOfTime, new EventInfo_I(data.playerId));
        }

        turretController.OnDestroy();

        Dispatcher.Send(EventId.TankLeftTheGame, new EventInfo_I(data.playerId));

    }

    /* PUBLIC SECTION */

    public virtual void AnimateClone() { }
    public virtual void StoreCloneRotation() { }

    public abstract void MovePlayer();

    public void SyncExistance(bool newExistance)
    {
        if (existanceSynchronized || IsMine)
        {
            return;
        }

        IsAvailable = newExistance;
        existanceSynchronized = true;
    }

    public virtual void Respawn(Vector3 position, Quaternion rotation, bool restoreLife, bool firstTime)
    {
        if (restoreLife)
        {
            Armor = data.maxArmor;
        }

        Dispatcher.Send(EventId.TankHealthChanged, new EventInfo_II(data.playerId, Armor));

        settingSpawnPosition = true;
        if (!firstTime)
        {
            IsAvailable = true;
        }

        correctPosition = transform.position = position;
        correctRotation = transform.rotation = rotation;
        turretController.ResetLocalRotation();

        correctVelocity = Vector3.zero;

        storedVehiclePosition = transform.position;

        Dispatcher.Send(EventId.TankRespawned, new EventInfo_I(data.playerId));

        if (StaticContainer.IsFriendOfMain(this))
        {
            Dispatcher.Send(EventId.ShowEnemy, new EventInfo_I(data.playerId));
        }
        else
        {
            Dispatcher.Send(EventId.HideEnemy, new EventInfo_I(data.playerId));
        }

        if (IsMain)
        {
            RemoveAllItems();
            turretController.ResetAimingState();

            Dispatcher.Send(EventId.MyTankRespawned, new EventInfo_SimpleEvent());
            //Dispatcher.Send(EventId.ShowEnemy, new EventInfo_I(data.playerId));
            if (respawnSound)
            {
                AudioDispatcher.PlayClipAtPosition(
                    /* clip:     */ respawnSound,
                    /* position: */ transform.position,
                    /* parent:   */ transform);
            }
        }
        /* else
         {
             indicator.Hidden = !isAvailable;
         }*/
    }

    public virtual void Explode()
    {
        if (!isAvailable)
        {
            return;
        }

        var explosionEffect = PoolManager.GetObject<ParticleEffect>(explosionPrefabPath);
        explosionEffect.transform.position = transform.position;
        explosionEffect.transform.rotation = Quaternion.LookRotation(
            forward: (Camera.main.transform.position - transform.position).normalized,
            upwards: Vector3.up);

        PlayExplosionSound();

        IsAvailable = false;

        if (!IsMine)
        {
            return;
        }

        CancelAllEffects();

        foreach (var weapon in turretController.weapons.Values)
        {
            weapon.InstantReload();
        }
    }

    public virtual float GetHeating(ShellType shellType)
    {
        switch (shellType)
        {
            case ShellType.Usual:
                return 0.0f;

            default:
                return 0.0f;
        }
    }

    public virtual float GetCooling(ShellType shellType)
    {
        switch (shellType)
        {
            case ShellType.Usual:
                return 0.0f;

            default:
                return 0.0f;
        }
    }

    public float GetROF(ShellType shellType)
    {
        switch (shellType)
        {
            case ShellType.Usual:
                return ROF;

            case ShellType.Missile_SACLOS:
                return DEFAULT_ROCKET_FIRE_RATE;

            case ShellType.IRCM:
                return IRCMROF;

            default:
                throw new ArgumentOutOfRangeException("shellType", shellType, null);
        }
    }

    public void SetMarkedStatus(bool marked)
    {
        if (vehicleMarker != null)
        {
            vehicleMarker.SetMarkedStatus(marked);
        }
    }

    public virtual void MakeRespawn(bool forced, bool restoreLife, bool firstTime)
    {
        Transform spawnPoint = SpawnPoints.instance.GetRandomPoint(this, data.teamId, forced);
        PhotonView.RPC("Respawn", PhotonTargets.All, spawnPoint.position, spawnPoint.rotation, restoreLife, firstTime);
    }

    ///<summary>
    /// Fixates addition effect in tank.
    /// Returns true if new effect was created (not permanent), false otherwise (refreshing existed effect or add not cancellable effect)
    ///</summary>
    public bool FixateEffect(VehicleEffect effect)
    {
        List<VehicleEffect> sameCellEffects = new List<VehicleEffect>(3);
        HashSet<int> uiIdsToCancel = new HashSet<int>();

        foreach (VehicleEffect eff in Effects.Values)
        {
            if (eff.UI_Id == effect.UI_Id)
            {
                sameCellEffects.Add(eff);
                continue;
            }

            if (eff.Type == effect.Type)
            {
                uiIdsToCancel.Add(eff.UI_Id);
            }
        }

        // Отключить все эффекты тех расходок, в которых есть эффект того же параметра, что у фиксируемого
        foreach (var uiId in uiIdsToCancel)
        {
            CancelAllEffectsForUI_ID(uiId);
            IEPanel.Instance.RemoveCell(uiId);
        }

        // Если уже есть эффекты под этот UI_ID, просто обновить их время в GUI
        if (sameCellEffects.Count != 0)
        {
            bool effectAlreadyPresents = false;
            foreach (var eff in sameCellEffects)
            {
                eff.SetEndTime(effect.EndTime);
                if (eff.Type == effect.Type)
                {
                    effectAlreadyPresents = true;
                }
            }
            if (!effectAlreadyPresents)
            {
                EffectItself(effect);
            }

            return false;
        }

        EffectItself(effect);
        return effect.MustBeReturned;
    }

    public void TakeEffectAway(int id)
    {
        VehicleEffect effect;
        if (!Effects.TryGetValue(id, out effect))
        {
            return;
        }

        EffectItself(effect, true);
    }

    public void ApplyEffect(VehicleEffect effect)
    {
        if (!IsMine)
        {
            return;
        }

        effect = new VehicleEffect(
                id: VehicleEffect.GetNewId(),
                efType: effect.Type,
                modType: effect.ModType,
                modValue: effect.ModValue,
                _duration: effect.Duration,
                _startTime: PhotonNetwork.time,
                bonusType: effect.Source,
                _icon: effect.Icon,
                consumableId: effect.ConsumableId);

        Dispatcher.Send(EventId.TankEffectApply, new EventInfo_IE(data.playerId, effect), Dispatcher.EventTargetType.ToAll);
    }

    public Bounds GetEntireAimBounds()
    {
        Bounds totalBounds = new Bounds(transform.position, Vector3.zero);
        foreach (var col in boundColliders)
        {
            totalBounds = MiscTools.SumBounds(totalBounds, col.bounds);
        }

        return totalBounds;
    }


    public bool UseItem(string itemName)
    {
        /*        if (string.IsNullOrEmpty(itemName) || !items.ContainsKey(itemName))
                    return false;

                int count = --items[itemName];

                switch (itemName)
                {
                    case "boost":
                        ApplyEffect(new VehicleEffect(-1, VehicleEffect.ParameterType.Speed, VehicleEffect.ModifierType.Product,
                            1.3f, 30, PhotonNetwork.time, BonusItem.BonusType.Boost, IECell.IEIcon.Boost));
                        break;
                    case "landmine":
                        Landmine landMine = PhotonNetwork.Instantiate("Landmine", transform.position, Quaternion.identity, 0).GetComponent<Landmine>();
                        break;
                    case "missile":
                        break;
                }

                var cell = IEPanel.Instance.GetCell(itemName);

                cell.Count = count;

                if (count == 0)
                {
                    items.Remove(itemName);
                    IEPanel.Instance.RemoveCell(itemName);
                }*/

        return true;
    }

    public void SetCruiseControlState(CruiseControl.CruiseControlState state)
    {
        CruiseControlState = state;
    }

#if UNITY_EDITOR
    private int maxArmorBeforeCheat;

    public void Cheat()
    {
        if (!Debug.isDebugBuild)
        {
            return;
        }

        cheatActivated = !cheatActivated;

        if (cheatActivated)
        {
            Speed *= 2f;
            ROF *= 17;
            maxArmorBeforeCheat = MaxArmor;
            Armor = MaxArmor = 200000;
            Dispatcher.Send(EventId.TankHealthChanged, new EventInfo_II(data.playerId, Armor));
            Debug.Log("*** Cheat activated ***");
        }
        else
        {
            Speed /= 2f;
            ROF /= 17;
            MaxArmor = maxArmorBeforeCheat;
            Armor = (int)(MaxArmor * 0.9);
            Dispatcher.Send(EventId.TankHealthChanged, new EventInfo_II(data.playerId, Armor));
            Debug.Log("*** Cheat disactivated ***");
        }
    }

    public void ActivateSlowpoke()
    {
        if (!Debug.isDebugBuild)
        {
            return;
        }

        slowpokeActivated = !slowpokeActivated;

        if (slowpokeActivated)
        {
            ROF /= 5;
            Debug.Log("*** Slowpoke mode activated ***");
        }
        else
        {
            ROF *= 5;
            Debug.Log("*** Slowpoke mode disactivated ***");
        }
    }
#endif

    public virtual int CalcDamage(int attack, bool critHit = false)
    {
        float result = attack;

        if (vehicleInfo != null && vehicleInfo.isIgnoringCritHits)
        {
            critHit = false;
        }

        if (data.newbie)
        {
            result *= GameManager.NEWBIE_DAMAGE_RATIO;
        }

        result *= Random.Range(GameManager.RANDOM_DAMAGE_RATIO_LOWER_BOUND, GameManager.RANDOM_DAMAGE_RATIO_UPPER_BOUND);

        result *= critHit ? GameData.critDamageRatio : GameData.normDamageRatio;

        return (int)result;
    }

    public Weapon GetWeapon(ShellType shellType)
    {
        return turretController.weapons[shellType];
    }

    private void SetNavMeshObstacle()
    {
        SelfObstacle = GetComponent<UnityEngine.AI.NavMeshObstacle>();

        if (SelfObstacle != null)
        {
            var col = Bumper.GetComponent<BoxCollider>();
            SelfObstacle.size = col.size;
            SelfObstacle.center = transform.InverseTransformPoint(Bumper.position);
        }
    }

    private void SetNewAimPointInfo(AimPointInfo pointInfo)
    {
        if (aimPointInfo.target != null && !IsBot)
        {
            aimPointInfo.target.SetMarkedStatus(false);
        }

        aimPointInfo = pointInfo;
        if (aimPointInfo.target != null && !IsBot)
        {
            aimPointInfo.target.SetMarkedStatus(true);
        }
    }

    private bool CheckHitPoint(RaycastHit hit, out AimPointInfo aimPoint)
    {
        Vector3 checkingPoint = hit.point;

        RaycastHit castHit;
        if (!Physics.Raycast(ShotPoint.position, (checkingPoint - ShotPoint.position).normalized, out castHit, hit.distance + 0.5f, hitMask))
        {
            aimPoint = new AimPointInfo(hit.point, hit);
            return true;
        }

        if (castHit.collider == hit.collider)
        {
            aimPoint = new AimPointInfo(castHit.point, castHit);
            return true;
        }

        if (IsBot)
        {
            // Упрощенная проверка для бота
            aimPoint = new AimPointInfo();
            return false;
        }

        checkingPoint.y = hit.collider.bounds.max.y - 0.05f;
        if (Physics.Raycast(ShotPoint.position, (checkingPoint - ShotPoint.position).normalized, out castHit, hit.distance + 10f, hitMask)
            && castHit.collider != hit.collider)
        {
            checkingPoint.y = hit.collider.bounds.min.y + 0.05f;
            if (Physics.Raycast(ShotPoint.position, (checkingPoint - ShotPoint.position).normalized, out castHit, hit.distance + 10f, hitMask)
                && castHit.collider != hit.collider)
            {
                aimPoint = new AimPointInfo();
                return false;
            }
        }

        checkingPoint = hit.point;
        aimPoint = new AimPointInfo(checkingPoint, hit);
        return true;
    }

    private void Subscriptions()
    {
        Dispatcher.Subscribe(EventId.TargetAimed, OnTargetAimed);
        Dispatcher.Subscribe(EventId.TankTakesDamage, OnVehicleTakesDamage, 2);
        Dispatcher.Subscribe(EventId.NowImMaster, OnIamMaster);
        Dispatcher.Subscribe(EventId.OffLayerInTeamMask, OnOffLayerInTeamMask);
        Dispatcher.Subscribe(EventId.ShellHit, OnShellHit);
        Dispatcher.Subscribe(EventId.BattleSettingsSubmited, OnBattleSettingsSubmited);

        if (IsMain)
        {
            Dispatcher.Subscribe(EventId.TankKilled, OnTankKilledForMain);
            Dispatcher.Subscribe(EventId.TankLeftTheGame, OnVehLeavesForMain);
        }

        if (IsBot)
        {
            Dispatcher.Subscribe(EventId.PhotonRoomCustomPropertiesChanged, RoomPropsChanged);
        }
    }

    private void Unsubscriptions()
    {
        Dispatcher.Unsubscribe(EventId.MainTankAppeared, OnMainVehicleAppeared);
        Dispatcher.Unsubscribe(EventId.TargetAimed, OnTargetAimed);
        Dispatcher.Unsubscribe(EventId.TankTakesDamage, OnVehicleTakesDamage);
        Dispatcher.Unsubscribe(EventId.BeforeReconnecting, OnReconnect);
        Dispatcher.Unsubscribe(EventId.NowImMaster, OnIamMaster);
        Dispatcher.Unsubscribe(EventId.PhotonRoomCustomPropertiesChanged, RoomPropsChanged);
        Dispatcher.Unsubscribe(EventId.NewLayerInTeamMask, OnNewLayerInTeamMask);
        Dispatcher.Unsubscribe(EventId.OffLayerInTeamMask, OnOffLayerInTeamMask);
        Dispatcher.Unsubscribe(EventId.ShellHit, OnShellHit);
        Dispatcher.Unsubscribe(EventId.TankKilled, OnTankKilledForMain);
        Dispatcher.Unsubscribe(EventId.TankLeftTheGame, OnVehLeavesForMain);
        Dispatcher.Unsubscribe(EventId.BattleSettingsSubmited, OnBattleSettingsSubmited);
    }

    private void TryGetBotParams()
    {
        // object botHealthObj;
        // object botExistanceObj;
        SetParamToBC(StatisticKey.Health);
        SetParamToBC(StatisticKey.Existance);
        /* if (PhotonNetwork.room.customProperties.TryGetValue(StatisticKey.Health, out botHealthObj) && botHealthObj != null)
         {
             BattleController.Instance.OnPlayerPropertiesChanged(data.playerId,
                 new Hashtable {{"hl", (int) botHealthObj}});
         }
         if (PhotonNetwork.room.customProperties.TryGetValue(StatisticKey.Existance, out botExistanceObj) && botExistanceObj != null)
         {
             BattleController.Instance.OnPlayerPropertiesChanged(data.playerId,
                 new Hashtable { { "ex", (bool) botExistanceObj } });
         }*/
    }
    private void SetParamToBC(StatisticKey key)
    {
        object meaning;
        if (PhotonNetwork.room.customProperties.TryGetValue(key, out meaning) && meaning != null)
        {
            BattleController.Instance.OnPlayerPropertiesChanged(data.playerId,
                new Hashtable { { StatisticType.GetKey(key), (int)meaning } });// this[key]
        }

    }
    private void OnIamMaster(EventId id, EventInfo ei)
    {
        if (IsBot)
        {
            rb.isKinematic = false;
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            ReanimateBot();
            BotDispatcher.Instance.RegisterBotAI(BotAI);
        }
    }

    private void RegisterForPhoton()
    {
        BattleConnectManager.AddPhotonMessageTarget(gameObject);
    }

    public virtual void ReanimateBot()
    {
        Rb.mass *= 100;
        Dispatcher.Subscribe(EventId.TankKilled, BotAI.OnBotBehaviourVehicleKilled);
        Dispatcher.Subscribe(EventId.TankTakesDamage, BotAI.OnBotBehaviourTakesDamage);
        Dispatcher.Subscribe(EventId.TankLeftTheGame, BotAI.OnBotBehaviourVehicleLeft);
        Dispatcher.Subscribe(EventId.BonusDestroyed, BotAI.OnBotBehaviourBonusDestroyed);
        Dispatcher.Subscribe(EventId.TankRespawned, BotAI.OnBotBehaviourTankRespawned);

        StartCoroutine(BotAI.DelayAndStartBot());

        BotAI.StartBotAICoroutines();
    }

    public abstract void UpdateBotPrefabs(VehicleController nativeController);

    public virtual void BoundPointsToList(List<Vector3> points) { }

    protected virtual void OnTargetAimed(EventId id, EventInfo ei) { } // TODO: Demetri Попробовать отрефакторить этот механизм

    protected virtual void OnVehicleTakesDamage(EventId id, EventInfo ei)
    {
        EventInfo_U info = (EventInfo_U)ei;

        int victimId = (int)info[0];
        if (victimId != data.playerId)
        {
            return;
        }

        int damage = (int)info[1];
        int attackerId = (int)info[2];

        if (!IsMine)
        {
            return;
        }

        Armor -= damage;
        SetCustomProperties(StatisticKey.Health, Armor);

        if (IsAvailable && Armor <= 0)
        {
            Dispatcher.Send(EventId.TankKilledInfo, new EventInfo_II(data.playerId, (int)info[3]), Dispatcher.EventTargetType.ToAll);
            Dispatcher.Send(EventId.HideEnemy, new EventInfo_I(data.playerId), Dispatcher.EventTargetType.ToAll);
            Dispatcher.Send(
                id: EventId.TankKilled,
                info: new EventInfo_II(data.playerId, attackerId),
                target: Dispatcher.EventTargetType.ToAll);
        }
    }

    protected virtual void SetEngineNoise(float t)
    {
        if (IsBot || !engineAudio)
        {
            return;
        }

        engineAudio.pitch = Mathf.Lerp(SoundControllerBase.DYNAMIC_ENGINE_PITCH_MIN, SoundControllerBase.DYNAMIC_ENGINE_PITCH_MAX, t);
        engineAudio.volume = Settings.SoundVolume * Mathf.Lerp(SoundControllerBase.DYNAMIC_ENGINE_VOLUME_MIN, SoundControllerBase.DYNAMIC_ENGINE_VOLUME_MAX, t);
    }

    protected virtual void PlayExplosionSound()
    {
        AudioDispatcher.PlayClipAtPosition(
            /* clip:     */ explosionSound,
            /* position: */ IsMain ? BattleCamera.Instance.transform.position : transform.position,
            /* parent:   */ IsMain ? BattleCamera.Instance.transform : transform);
    }

    public virtual void StartBurst() { }

    public virtual void StopBurst() { }

    protected virtual void ApplyAvailability()
    {
        IsInParallelWorld = !isAvailable;
        isExploded = !isAvailable;

        if (SelfObstacle != null)
        {
            SelfObstacle.enabled = isAvailable;
        }

        rb.isKinematic = !IsMine || !isAvailable;

        Dispatcher.Send(EventId.TankAvailabilityChanged, new EventInfo_II(data.playerId, isAvailable ? 1 : 0));

        if (engineAudio != null)
        {
            if (isAvailable)
            {
                if (!engineAudio.isPlaying)
                {
                    engineAudio.Play();
                }
            }
            else
            {
                if (engineAudio.isPlaying)
                {
                    engineAudio.Stop();
                }
            }
        }
        turretController.StopTurretAudio();

        System.Object res;

        if (IsMine)
        {
            if (!IsBot)
            {
                if (player != null && player.isLocal && (!player.customProperties.TryGetValue("ex", out res) || (res != null && (bool)res != IsAvailable)))
                {
                    player.SetCustomProperties(new Hashtable { { "ex", IsAvailable } });
                }
            }
            else
            {
                if (PhotonNetwork.inRoom &&
                   (!PhotonNetwork.room.customProperties.TryGetValue(this[StatisticKey.Existance], out res) ||
                   (res != null && (bool)res != IsAvailable)))
                {
                    PhotonNetwork.room.SetCustomProperties(new Hashtable { { this[StatisticKey.Existance], IsAvailable } });
                }
            }
        }
        /*
        if (indicator != null)
        {
            indicator.Hidden = !IsAvailable;
        }*/
    }


    protected virtual void SetEngineAudio()
    {
        //if (!IsMain || GameData.IsGame(Game.Armada) || GameData.IsGame(Game.SpaceJet))

        if (!IsMain || GameData.IsGame(Game.WWT2))
        {
            return;
        }

        engineAudio = gameObject.AddComponent<AudioSource>();

        engineAudio.clip = engineSound;
        engineAudio.loop = true;
        engineAudio.rolloffMode = AudioRolloffMode.Linear;
        engineAudio.maxDistance = 65;
        engineAudio.dopplerLevel = 0;
        engineAudio.volume = Settings.SoundVolume * SoundControllerBase.DYNAMIC_ENGINE_VOLUME_MIN;
        engineAudio.pitch = SoundControllerBase.DYNAMIC_ENGINE_PITCH_MIN;
    }

    protected void StoreVehiclePosition()
    {
        if (transform.position != storedVehiclePosition)
        {
            odometer += Vector3.Distance(rb.position, storedVehiclePosition) * OdometerRatio;
        }

        storedVehiclePosition = transform.position;
    }


    public void MarkActivity()
    {
        inactivityTime = 0;
    }

    protected void CancelAllEffects()
    {
        foreach (VehicleEffect effect in Effects.Values)
        {
            effectsToCancel.Add(effect);
        }

        foreach (var effect in effectsToCancel)
        {
            Dispatcher.Send(EventId.TankEffectCancelled, new EventInfo_II(data.playerId, effect.Id), Dispatcher.EventTargetType.ToAll);
        }
        effectsToCancel.Clear();
    }

    /// <summary>
    /// Отменить все эффекты связанные с опред. UI_ID
    /// </summary>
    /// <param name="uiId"></param>
    protected void CancelAllEffectsForUI_ID(int uiId)
    {
        foreach (VehicleEffect effect in Effects.Values)
        {
            if (effect.UI_Id == uiId)
            {
                effectsToCancel.Add(effect);
            }
        }

        foreach (var effect in effectsToCancel)
        {
            Dispatcher.Send(EventId.TankEffectCancelled, new EventInfo_II(data.playerId, effect.Id),
                Dispatcher.EventTargetType.ToAll);
        }

        effectsToCancel.Clear();
    }

    protected void RemoveAllItems()
    {
        // items.Clear();
        IEPanel.Instance.RemoveAllItemsCells();
    }

    private void OnMainVehicleAppeared(EventId id, EventInfo ei)
    {
        SetMembership();
    }

    private void SetMembership()
    {
        if (IsMain)
        {
            ownLayer = LayerMask.NameToLayer("Player");
        }
        else
        {
            if (IsBot)
            {
                ownLayer = BotDispatcher.GetNewBotLayer();
                if (ownLayer == 0)
                {
                    BotDispatcher.Instance.RemoveBot(this);
                    return;
                }
            }
            else
            {
                ownLayer = LayerMask.NameToLayer(IsMainsFriend ? "Friend" : "Enemy");
            }
        }

        if (!IsMine)
        {
            System.Object res;

            Hashtable properties = (!IsBot) ? player.customProperties : PhotonNetwork.room.customProperties;
            if (properties.TryGetValue(this[StatisticKey.Existance], out res) && res != null)
            {
                IsAvailable = (bool)res;
                existanceSynchronized = true;
            }
            else
            {
                IsAvailable = false;
            }
        }
        else
        {
            IsAvailable = true;
        }

        Dispatcher.Subscribe(EventId.NewLayerInTeamMask, OnNewLayerInTeamMask);
        if (IsBot)
        {
            Dispatcher.Send(EventId.NewLayerInTeamMask, new EventInfo_II(data.teamId, ownLayer));
        }
        if (CritZones)
        {
            CritZones.tag = Tag.Items[Tag.Key.CritZone];
        }

        if (GameData.Mode == GameData.GameMode.Team)
        {
            Dispatcher.Send(EventId.TeamChange, new EventInfo_SimpleEvent());
        }

        if (!IsMain)
        {
            tag = Tag.Items[IsMainsFriend ? Tag.Key.Friend : Tag.Key.Enemy];
        }
        StartCoroutine(ResetMasks_Coroutine());
    }

    public virtual void EffectItself(VehicleEffect effect, bool inverted = false)
    {
        if (effect.MustBeReturned)
        {
            if (inverted)
            {
                Effects.Remove(effect.Id);
            }
            else
            {
                Effects.Add(effect.Id, effect);
            }
        }

        effect.ApplyToVehicle(this, inverted);
    }

    protected VehicleEffect FindRelatedEffect(VehicleEffect effect)
    {
        foreach (VehicleEffect eff in Effects.Values)
        {
            if (eff.UI_Id == effect.UI_Id && eff.Type == effect.Type)
            {
                return effect;
            }
        }

        return null;
    }

    private void FillVehicleData()
    {
        object[] playerAndProps = new object[2];

        playerAndProps[0] = player;
        playerAndProps[1] = player.customProperties;

        BattleController.Instance.OnPlayerPropertiesChanged(player.ID, player.customProperties);
    }

    public void MoveClone()
    {
        if (isExploded)
        {
            transform.position = correctPosition;
            transform.rotation = correctRotation;

            if (Turret)
            {
                Turret.localEulerAngles = Vector3.zero;
            }

            return;
        }

        transform.position = Vector3.SmoothDamp(transform.position, correctPosition, ref posSyncVelocity, syncTime);
        transform.rotation = Quaternion.RotateTowards(transform.rotation, correctRotation, rotSyncSpeed * Time.deltaTime);

        if (Turret)
        {
            Turret.localEulerAngles = new Vector3(0, Mathf.MoveTowardsAngle(Turret.localEulerAngles.y, correctTurretAngle, Speed * turretRotationSpeedQualifier * Time.deltaTime), 0);
        }
    }

    private void CalculateMaxSpeed()
    {
        maxSpeed = data.speed / SpeedRatio;
    }

    public virtual void TakeExperienceBonus(int amount)
    {
        if (IsMine)
        {
            ScoreCounter.ScoreInto(this, amount);
            if (IsMain)
            {
                Dispatcher.Send(EventId.ExperienceAcquired, new EventInfo_I(amount));
            }
        }
    }

    public virtual void TakeGoldBonus(int amount)
    {
        if (IsMain)
        {
            Dispatcher.Send(EventId.GoldAcquired, new EventInfo_I(amount));
        }
    }

    public virtual void TakeSilverBonus(int amount)
    {
        if (IsMain)
        {
            Dispatcher.Send(EventId.SilverAcquired, new EventInfo_I(amount));
        }
    }

    public virtual void TakeHealthBonus()
    {
        if (!IsMine)
        {
            return;
        }
        Armor = MaxArmor;
        SetCustomProperties(StatisticKey.Health, Armor);
    }

    public void TakeFuelBonus()
    {
        Dispatcher.Send(EventId.FuelAcquired, new EventInfo_I(1));
    }

    public void TakeGoldRushBonus()
    {
        if (!GameData.IsGoldRushEnabled)
        {
            return;
        }

        if (BattleController.TimeRemaining < GameData.GoldRushMinTime)
        {
            BattleController.Instance.ProlongGameForFree(GameData.GoldRushMinTime - BattleController.TimeRemaining);
            TopPanelValues.ShowCriticalTime(false);
        }

        GoldRush.AwardPermission = true;
        PhotonNetwork.room.SetCustomProperties(new Hashtable { { "goldLeader", data.playerId } });
        MakeRespawn(false, false, false);
    } // TODO: Demetri Если включат зотолую лихорадку, то проверить, как работает для ботов


    /// <summary>
    /// Использовать расходку из имеющихся у данного транспорта
    /// </summary>
    /// <param name="consumableId">Id расходки</param>
    /// <returns>true, если расходка использована</returns>
    public bool UseConsumable(int consumableId)
    {
        BattleConsumable consumable;

        return consumables.TryGetValue(consumableId, out consumable) && consumable.Use();
    }

    protected void UpdateEffects()
    {
        foreach (var effect in Effects.Values)
        {
            if (PhotonNetwork.time >= effect.EndTime)
            {
                effectsToCancel.Add(effect);
            }
        }

        if (effectsToCancel.Count == 0)
        {
            return;
        }

        foreach (var effect in effectsToCancel)
        {
            Dispatcher.Send(EventId.TankEffectCancelled, new EventInfo_II(data.playerId, effect.Id), Dispatcher.EventTargetType.ToAll);
        }
        effectsToCancel.Clear();
    }

    private void OnReconnect(EventId eid, EventInfo ei)
    {
        CancelAllEffects();
    }

    private void CheckAllBotStats()
    {
        if (PhotonNetwork.room.customProperties == null)
        {
            return;
        }

        CheckBotStat(this[StatisticKey.Score], out statistics.score);
        CheckBotStat(this[StatisticKey.Deaths], out statistics.deaths);
        CheckBotStat(this[StatisticKey.Kills], out statistics.kills);
    }

    private void CheckBotStat(string propertyKey, out int statField)
    {
        Hashtable properties = PhotonNetwork.room.customProperties;
        object value;
        /*string propertyKey = StringKeyPack.GetKey(type)+id;
        if (IsBot) 
        {
            propertyKey = "bt" + propertyKey;
        }*/
        statField = properties.TryGetValue(propertyKey, out value) && value != null ? (int)value : 0;
    }

    private void ResetMasks()
    {
        if (IsMain)
        {
            EnemyLayerMask = StaticContainer.EnemyLayerMask;
        }
        else if (GameData.IsTeamMode)
        {
            EnemyLayerMask = IsMainsFriend ? StaticContainer.EnemyLayerMask : MiscTools.GetLayerMask("Player", "Friend");
        }
        else
        {
            EnemyLayerMask = MiscTools.GetLayerMask("Player", "Friend", "Enemy");
        }

        EnemyLayerMask |= (GameData.Mode == GameData.GameMode.Team)
                ? BotDispatcher.GetBotsLayerMaskForTeam(1 - data.teamId)
                : MiscTools.ExcludeLayerFromMask(BotDispatcher.BotsCommonMask, ownLayer); //Exclude на тот случай, если сам - бот.
        if (IsMain)
        {
            hitMask = MiscTools.ExcludeLayersFromMask(BattleController.HitMask, "Player");
        }
        else
        {
            hitMask = IsBot ? MiscTools.ExcludeLayerFromMask(BattleController.HitMask, ownLayer) : BattleController.HitMask;
        }

        othersLayerMask = MiscTools.GetLayerMask(Layer.Key.Enemy, Layer.Key.Friend, Layer.Key.Player) | BotDispatcher.BotsCommonMask;
        othersLayerMask = MiscTools.ExcludeLayerFromMask(othersLayerMask, ownLayer);
        aimingController.SetMask(hitMask);
    }

    private void RoomPropsChanged(EventId id, EventInfo ei)
    {
        CheckAllBotStats();
        StartCoroutine(ForgetPropsChanged());
    }

    private IEnumerator ForgetPropsChanged()
    {
        yield return null;
        Dispatcher.Unsubscribe(EventId.PhotonRoomCustomPropertiesChanged, RoomPropsChanged);
    }

    private void SetStartBotProperties()
    {
        Hashtable properties = new Hashtable
            {
                { this[StatisticKey.Health], MaxArmor },
                { this[StatisticKey.Score], 0 },
                { this[StatisticKey.Kills], 0 },
                { this[StatisticKey.Deaths], 0 }
            };
        PhotonNetwork.room.SetCustomProperties(properties);
    }

    private bool GetLayerOnChange(EventId id, EventInfo ei, ref int layer)
    {
        if (GameData.Mode == GameData.GameMode.Deathmatch)
        {
            return false;
        }

        EventInfo_II info = ei as EventInfo_II;
        int teamId = info.int1;
        layer = info.int2;

        if (teamId == data.teamId)
        {
            return false;
        }
        return true;
    }

    private void OnNewLayerInTeamMask(EventId id, EventInfo ei)
    {
        int layer = -1;
        if (GetLayerOnChange(id, ei, ref layer))
        {
            EnemyLayerMask = EnemyLayerMask | (1 << layer);
        }

    }

    private void OnOffLayerInTeamMask(EventId id, EventInfo ei)
    {
        int layer = -1;
        if (GetLayerOnChange(id, ei, ref layer))
        {
            EnemyLayerMask = EnemyLayerMask & ~(1 << layer);
        }
    }

    private IEnumerator ResetMasks_Coroutine()
    {
        yield return null;
        ResetMasks();
    }
    private IEnumerator Regenerate()
    {
        if (!IsMine)
        {
            yield break;
        }

        WaitForSeconds oneSecondWaiting = new WaitForSeconds(1f);
        while (Regeneration != 0)
        {
            {
                if (Armor != MaxArmor)
                {
                    Dispatcher.Send(EventId.TankTakesDamage,
                        new EventInfo_U(data.playerId, -Regeneration, 0, ShellType.Usual, Vector3.zero));
                }
                yield return oneSecondWaiting;
            }
        }
    }

    private void OnShellHit(EventId eid, EventInfo ei)
    {
        EventInfo_IIIIV info = (EventInfo_IIIIV)ei;

        if (info.int1 != data.playerId)
        {
            return;
        }

        Vector3 position = transform.TransformPoint(info.vector);

        var shellType = (ShellType)info.int4;

        if (shellType == ShellType.Usual)
        {
            var shell = PoolManager.GetObject<Shell>(shellPrefabPath);
            shell.transform.position = Vector3.zero;
            shell.transform.rotation = Quaternion.identity;

            shell.Explosion(position: position, hitsVehicle: true);
        }

        int damage = info.int2;

        Dispatcher.Send(
            id: EventId.TankTakesDamage,
            info: new EventInfo_U(
                        info.int1,
                        damage,
                        info.int3,
                        info.int4,
                        position));

        if (!IsMine)
        {
            return;
        }
        SetCustomProperties(StatisticKey.Health, Armor);
    }

    private void OnTankKilledForMain(EventId eid, EventInfo ei)
    {
        EventInfo_II info = (EventInfo_II)ei;
        if (info.int1 == data.playerId)
        {
            ResetGunsight();
        }
    }

    private void OnVehLeavesForMain(EventId eid, EventInfo ei)
    {
        EventInfo_I info = (EventInfo_I)ei;
        if (aimingController.Target != null && info.int1 == aimingController.Target.data.playerId)
        {
            ResetGunsight();
        }
    }

    private void OnBattleSettingsSubmited(EventId eid, EventInfo ei)
    {
        if (IsMain)
        {
            turretController.SetAudioVolume(Settings.SoundVolume * SoundControllerBase.TURRET_ROTATION_VOLUME);
        }
    }

    private void CollectBoundColliders()
    {
        List<Collider> boundColliderList = new List<Collider>(8);
        if (Body != null)
        {
            boundColliderList.AddRange(Body.GetComponentsInChildren<Collider>());
        }
        if (Turret != null)
        {
            boundColliderList.AddRange(Turret.GetComponentsInChildren<Collider>());
        }

        boundColliders = boundColliderList.ToArray();

        CritZonePlace = BoundsVertZone.Bottom;
        foreach (Collider col in boundColliders)
        {
            if (col.gameObject.tag == "CritZone")
            {
                CritZonePlace = DetermineColliderPlace(col);
                return;
            }
        }
    }

    private BoundsVertZone DetermineColliderPlace(Collider collider)
    {
        Bounds entireBounds = GetEntireAimBounds();
        Bounds colBounds = collider.bounds;
        if (colBounds.min.y > entireBounds.center.y)
        {
            return BoundsVertZone.Top;
        }

        if (colBounds.max.y < entireBounds.center.y)
        {
            return BoundsVertZone.Bottom;
        }
        return BoundsVertZone.Center;
    }

    private void InitConsumables(Dictionary<int, ObscuredInt> consumableDict)
    {
        foreach (var consPair in consumableDict)
        {
            consumables[consPair.Key] = new BattleConsumable(this, GameData.consumableInfos[consPair.Key]);
        }
    }

    public void ResetGunsight()
    {
        if (Target == null)
        {
            return;
        }

        if (!IsBot)
        {
            BattleGUI.HideGunSight();
            Target.SetMarkedStatus(false);
        }

        int lastTargetId = Target.data.playerId;
        aimingController.ResetTarget();
        Dispatcher.Send(EventId.TargetAimed, new EventInfo_IIB(data.playerId, lastTargetId, false));
    }

#if UNITY_EDITOR
    private void TestConsumable(int index)
    {
        Dictionary<int, BattleConsumable>.Enumerator enumer = consumables.GetEnumerator();
        for (int i = 0; i <= index; i++)
        {
            if (!enumer.MoveNext())
            {
                return;
            }
        }

        BattleConsumable consumable = enumer.Current.Value;
        if (consumable != null)
        {
            consumable.Use();
        }
    }
#endif
}
