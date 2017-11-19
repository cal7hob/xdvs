using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using CodeStage.AntiCheat.ObscuredTypes;
using Disconnect;
using XDevs.LiteralKeys;

using Hashtable = ExitGames.Client.Photon.Hashtable;
using Random = UnityEngine.Random;

public abstract class VehicleController : MonoBehaviour, IPunObservable
{
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

    [Header("Данные (для чтения)")]
    public ObscuredInt id;
    public TankData data;

    [Header("Префабы")]
    public GameObject shotPrefab;
    public GameObject hitPrefab;
    public GameObject explosionPrefab;

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
    [SerializeField]
    protected ObscuredFloat maxSpeed = 5;
    public virtual ObscuredFloat MaxSpeed { get { return maxSpeed; } set { maxSpeed = value; } }
    public Vector3 centerOfMass;

    [Header("Прочее")]
    public bool continuousFire;
    public float shotCorrection = 0.3f;
    public float turretRotationSpeedQualifier = 0.03f; 
    public float rotationSpeedQualifier = 1.2f;

    public Dictionary<GunShellInfo.ShellType, Weapon> weapons;

    public float TurretRotationZoomSpeedQualifier
    {
        get
        {
            return TargetAimed ? Mathf.Clamp(BattleCamera.Instance.TurretIndicationZoomSqrDist / Vector3.SqrMagnitude(TargetPosition - transform.position), 0.2f, 1) * turretRotationSpeedQualifier : turretRotationSpeedQualifier * 0.5f;
        }
    }

    public virtual float TurretRotationSpeedQualifier
    {
        get
        {
            return BattleCamera.Instance.IsZoomed ? TurretRotationZoomSpeedQualifier : turretRotationSpeedQualifier;
        }
    }

    private Vector3 testPoint;

    protected const float DEFAULT_SOUND_DISTANCE = 65.0f;

    protected readonly List<AimPointInfo> aimPoints = new List<AimPointInfo>(50);
    protected readonly Dictionary<string, int> items = new Dictionary<string, int>(2);

    protected bool isAvailable;
    protected bool isExploded;
    protected bool burst;
    protected bool settingSpawnPosition = true;
    protected float currentCorrection;
    protected ObscuredFloat odometer;
    protected Vector3 correctPosition;
    protected Vector3 correctVelocity;
    protected Vector3 storedVehiclePosition;
    protected Quaternion correctRotation;
    protected Rigidbody rb;
    protected Animation shootAnimation;
    protected AudioSource turretAudio;
    protected AudioSource engineAudio;
    protected new Renderer renderer;
    protected PhotonPlayer player;
    protected AimPointInfo aimPointInfo;
    protected AimPointInfo lastAimPointInfo;
    protected GunShellInfo primaryShellInfo;
    protected GunShellInfo secondaryShellInfo;
    protected float correctTurretAngle;
    protected double kickBotAt; // Когда выгнать из комнаты, если бот
    protected int hitMask;
    protected int ownLayer; // Слой своего vehicle
    protected Transform shotPoint;
    protected Transform turret;
    protected float lastTouchTurretRotation = 0f;
    protected float lastTouchTurretRotationSpeed = 0f;
    protected TankIndicator indicator;
    protected LayerMask othersLayerMask;
    protected JoystickController leftJoystick;
    protected JoystickController rightJoystick;
    protected static int terrainLayer;
    protected float lastTurretLocalRotationY;
    protected bool isTurretIdleFrameBefore;
    protected int turretTouchId = -1;
    protected Vector2 turretTouchCenter = Vector2.zero;
    protected AimingController aimingController;

    private const GunShellInfo.ShellType DEFAULT_SHELL_TYPE = GunShellInfo.ShellType.Usual;
    private static readonly ObscuredFloat DEFAULT_ROCKET_FIRE_RATE = 6.0f;
    
    private readonly RaycastHit[] aimingHits = new RaycastHit[32];
    private readonly Dictionary<int, VehicleEffect> effects = new Dictionary<int, VehicleEffect>(4);
    private readonly List<VehicleEffect> effectsToCancel = new List<VehicleEffect>(3); // Для отмены эффектов. После использования очищать.
    
    private bool visible;
    private bool cheatActivated;
    private bool slowpokeActivated;
    private float inactivityTime;
    private Vector3 indicatorDelta;
    private Transform body;
    protected Transform bodyMeshTransform;
    private PlayerStat statistics;
    private BodykitController bodykitController;
    private Renderer[] renderers;
    private Collider[] boundColliders;
    private int maxArmorBeforeCheat;
    private VehicleMarker vehicleMarker;
    private IEnumerator changingVehicleMaterial;
    private bool existanceSynchronized;

    public Collider[] BoundColliders { get { return boundColliders; } }

    public float MaxShootAngleCos { get; private set; } // Косинус максимального угла прицеливания

    private Dictionary<int, BattleConsumable> consumables = new Dictionary<int, BattleConsumable>();

    public BattleConsumable GetConsumable(int consumableId)
    {
        BattleConsumable consumable;
        consumables.TryGetValue(consumableId, out consumable);

        return consumable;
    }

    public UnityEngine.AI.NavMeshObstacle SelfObstacle { get; protected set; }

    public bool TurretCentering { get; set; }

    public float LastTurretLocalRotationY { get { return lastTurretLocalRotationY; } }

    public bool IsTurretIdleFrameBefore { get { return isTurretIdleFrameBefore; } }

    public Vector3 CameraTranslationAxis { get; protected set; }

    public Vector3 CorrectPosition { get { return correctPosition; } }

    public Vector3 CorrectVelocity { get { return correctVelocity; } }

    public Quaternion CorrectRotation { get { return correctRotation; } }

    public float CorrectTurretAngle { get { return correctTurretAngle; } }

    public float CurrentCorrection { get { return currentCorrection; } }

    public bool SettingSpawnPosition { get { return settingSpawnPosition; } }

    public int TerrainLayer { get { return terrainLayer; } }

    public int OthersLayerMask { get { return othersLayerMask; } }

    public int OwnLayer { get { return ownLayer; } }

    public double KickBotAt { get { return kickBotAt; } }

    public bool Burst { get { return burst; } }

    public string KeyForHealth { get; private set; }

    public string KeyForScore{get; private set; }

    public string KeyForKills { get; private set; }

    public string KeyForDeaths { get; private set; }

    public string KeyForExistance { get; private set; }

    public string KeyForAttack { get; private set; }

    public string KeyForRoF { get; private set; }

    public string KeyForSpeed { get; private set; }

    public string KeyForMaxArmor { get; private set; }

    public string KeyForRegen { get; private set; }

    public string KeyForShield { get; private set; }

    public string KeyForDamageRatio { get; private set; }

    public PhotonView PhotonView { get; protected set; }

    public AimPointInfo AimPoint
    {
        get { return aimPointInfo; }
        set { aimPointInfo = value; }
    }

    public Rigidbody Rb { get { return rb; } }

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

    public ConsumableInfo SuperWeaponInfo { get; private set; }

    public BattleConsumable SuperWeaponBC { get { return SuperWeaponInfo != null ? GetConsumable (SuperWeaponInfo.id) : null; } }

    public Mesh BodyMesh { get; protected set; }

    public Bounds BodyMeshBounds { get; private set; }

    public virtual bool IsCrashing
    {
        get { return false; }
        set { }
    }

    public virtual float WeaponReloadingProgress
    {
        get
        {
            return weapons[DEFAULT_SHELL_TYPE].ReloadingProgress;
        }
    }

    public virtual GunShellInfo.ShellType DefaultShellType
    {
        get { return DEFAULT_SHELL_TYPE; }
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
            return PhotonView.isMine ? rb.velocity : correctVelocity;
        }
    }

    public virtual Vector3 AngularVelocity
    {
        get { return PhotonView.isMine ? rb.angularVelocity : Vector3.zero; }
    }

    public virtual Renderer Renderer
    {
        get { return renderer ?? (renderer = GetComponent<Renderer>()); }
    }

    public bool IsBot
    {
        get { return PhotonView.isSceneView; }
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
                return;

            visible = value;

            foreach (Renderer rend in renderers)
                rend.enabled = visible;
        }
    }

    public bool IsInParallelWorld
    {
        get { return gameObject.layer == LayerMask.NameToLayer(Layer.Items[Layer.Key.ParallelWorld]); }

        set
        {
            int layer = value ? LayerMask.NameToLayer("ParallelWorld") : ownLayer;
            Transform[] children = GetComponentsInChildren<Transform>(true);
            foreach (Transform child in children)
            {
                if (child != Bumper)
                    child.gameObject.layer = layer;
            }

            if (Bumper)
                Bumper.gameObject.layer =
                    LayerMask.NameToLayer(Layer.Items[isAvailable ? Layer.Key.TankBumper : Layer.Key.ParallelWorld]);

            if (CritZones && GameData.IsGame (Game.MetalForce)) {
                CritZones.gameObject.layer =
                    LayerMask.NameToLayer (Layer.Items[isAvailable ? Layer.Key.CritZone : Layer.Key.ParallelWorld]);
            }
        }
    }

    public VehicleInfo VehicleInfo
    {
        get { return VehiclePool.Instance.GetItemById(id); }
    }

    public int VehicleId
    {
        get { return VehicleInfo.id; }
    }

    public int VehicleGroup
    {
        get { return VehicleInfo.vehicleGroup; }
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

    public bool IsExploded
    {
        get { return isExploded; }
    }

    public bool IsMain
    {
        get { return ((PhotonNetwork.connected && PhotonView.isMine) || (data.playerId == BattleController.MyPlayerId)) && !IsBot; }
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
                return false;

            if (!BattleController.MyVehicle)
                return false;

            return AreFriends(BattleController.MyVehicle, this);
        }
    }

    public int ExperienceBonus
    {
        get { return MiscTools.Round((int) (data.maxArmor/10.5), 5); }
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
                data.armor = data.maxArmor;
        }
    }

    public int Regeneration
    {
        get { return data.regeneration; }
        set
        {
            data.regeneration = value;
            if (!PhotonView.isMine)
                return;
            
            StopCoroutine("Regenerate");
            if (value != 0)
                StartCoroutine("Regenerate");
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

    public float Odometer
    {
        get { return odometer; }
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

    virtual public float CurrentSpeed {
        get {
            return LocalVelocity.z;
        }
    }

    public float CurrentSpeedRatio {
        get {
            return LocalVelocity.z / MaxSpeed;
        }
    }

    public Vector3 LocalVelocity
    {
        get { return transform.InverseTransformDirection(Velocity); }
    }

    public Vector3 LocalAngularVelocity
    {
        get { return transform.InverseTransformDirection(AngularVelocity); }
    }

    public Vector3 IndicatorPointPosition
    {
        get { return transform.position + indicatorDelta; }
    }

    public PhotonPlayer Player
    {
        get { return player; }
    }

    public PlayerStat Statistics
    {
        get { return statistics; }
    }

    public BodykitController BodykitController
    {
        get { return bodykitController; }
    }

    public Dictionary<int, VehicleEffect> Effects
    {
        get { return effects; }
    }

    public bool TargetAimed { get { return Target != null; } }

    private VehicleController target;
    public VehicleController Target
    {
        get { return aimingController == null ? target : aimingController.Target; }
        protected set { target = value; }
    }

    public int TargetId
    {
        get { return TargetAimed ? Target.data.playerId : BattleController.DEFAULT_TARGET_ID; }
    }

    private Vector3 targetPosition;

    public Vector3 TargetPosition
    {
        get { return aimingController == null ? targetPosition : aimingController.TargetPosition; }
        protected set { targetPosition = value; }
    }

    public Vector3 AimPointLocalToTarget
    {
        get { return Target.transform.InverseTransformPoint(TargetPosition); }
    }

    public int EnemyLayerMask
    {
        private set
        {
            enemyLayerMask = value;
        }
        get
        {
            return enemyLayerMask;
        }
    }
    protected int enemyLayerMask;

    public virtual BotAI BotAI { get { return null; } }

    public virtual Transform Body { get { return body = body ?? transform.Find("Body") ?? transform.Find("Mesh"); } }

    public virtual Transform BodyMeshTransform { get { return bodyMeshTransform = bodyMeshTransform ?? transform.Find("Mesh_Body") ?? Body; } }

    public virtual Transform ForCam { get { return forCam = forCam ?? turret.Find("ForCamera"); } }

    public bool DoubleExperience { get { return IsMain && ProfileInfo.doubleExpVehicles.Contains(ProfileInfo.currentVehicle); } }

    public BoundsVertZone CritZonePlace { get; private set; }

    protected abstract float OdometerRatio { get; }

    protected abstract float SpeedRatio { get; }

    public abstract float MaxShootAngle { get; }

    protected abstract float CorrectionTime { get; }

    protected abstract bool NeedCorrectAimY { get; }

    protected virtual bool FireButtonPressed
    {
        get
        {
            return continuousFire ? 
                XDevs.Input.GetButton("Fire Primary") || (ProfileInfo.isFireOnDoubleTap && XDevs.Input.GetButton("Double Tap")) : 
                XDevs.Input.GetButtonDown("Fire Primary") || (ProfileInfo.isFireOnDoubleTap && XDevs.Input.GetButtonDown("Double Tap"));
        }
    }
    
    protected virtual float TurretAxisControl
    {
        get
        {
            return !BattleGUI.IsWindowOnScreen ? XDevs.Input.GetAxis("Turret Rotation") : 0;
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
            return XDevs.Input.GetAxis("Move Forward/Backward");
        }
    }

    public virtual float XAxisAltControl
    {
        get
        {
            return XDevs.Input.GetAxis("Strafe Left/Right");
        }
    }

    public virtual float YAxisAltControl
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
        get { return false; }
    }

    public bool PrimaryFireIsOn { get; set; }
    public bool SecondaryFireIsOn { get; set; }

    public int HitMask { get { return hitMask; } }

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

    protected virtual float VertAimCapture
    {
        get { return 20.0f; }
    }

    protected virtual float HorizAimCapture
    {
        get { return 0.1f; }
    }

    public virtual float MaxAimDistance
    {
        get { return 500.0f; }
    }

    #region Smooth network move
    public double interpolationBackTime = 0.2;

    protected internal struct State {
        internal double timestamp;
        internal Vector3 pos;
        internal Quaternion rot;
        internal Vector3 vel;
        internal float tRot;
        internal float steering;
    }

    // We store twenty states with "playback" information
    protected State[] m_BufferedState = new State[20];
    // Keep track of what slots are used
    protected int m_TimestampCount;
    #endregion

    /* UNITY and EVENT SECTION */

    public virtual void OnPhotonInstantiate(PhotonMessageInfo info)
    {
        if (!GameData.IsGame(Game.BattleOfHelicopters | Game.BattleOfWarplanes | Game.WingsOfWar | Game.SpaceJet))
        {
            SetNavMeshObstacle();
        }

        BodyMesh = BodyMeshTransform.gameObject.GetComponentInChildren<MeshFilter>(true).sharedMesh;
        BodyMeshBounds = BodyMesh.bounds;
        vehicleMarker = GetComponentInChildren<VehicleMarker>();
        Target = null;

        object lastOdometer;
        if (BattleConnectManager.Instance.GetStoredValue("MyOdometer", out lastOdometer))
            odometer = (ObscuredFloat) lastOdometer;

        MaxShootAngleCos = Mathf.Cos(Mathf.Deg2Rad * MaxShootAngle);

        primaryShellInfo = GunShellInfo.UsualShell;
        secondaryShellInfo = GunShellInfo.UsualShell;

        changingVehicleMaterial = QualityManager.Instance.ObjectMaterialsChanging(gameObject);
        QualityManager.Instance.StartCoroutine(changingVehicleMaterial);

        rb = GetComponent<Rigidbody>();
        PhotonView = info.photonView ?? GetComponent<PhotonView>();
        player = PhotonView.owner;

        if (BattleController.vehicleData.ContainsKey(PhotonView.ownerId))
        {
            data = BattleController.vehicleData[PhotonView.ownerId];
        }
        else
        {
            data = (TankData)PhotonView.instantiationData[0];
        }

        data.playerId = IsBot ? data.playerId : PhotonView.ownerId;

        int dataIndexOffset = 0;

        if (IsBot)
            dataIndexOffset = 2;

        if (PhotonView.instantiationData.Length > 1 + dataIndexOffset)
            SuperWeaponInfo = ConsumableInfo.GetInfo((int)PhotonView.instantiationData[1 + dataIndexOffset]);

        if (PhotonView.instantiationData.Length > 2 + dataIndexOffset)
            id = (int)PhotonView.instantiationData[2 + dataIndexOffset];

        if (!PhotonView.isMine && data.profileId == ProfileInfo.profileId && !Debug.isDebugBuild)
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

        SetPropertyKeys();

        correctPosition = transform.position;
        correctRotation = transform.rotation;

        if (Bumper)
            Bumper.gameObject.layer = LayerMask.NameToLayer(Layer.Items[Layer.Key.TankBumper]);

        Subscriptions();
        
        if (IsMain)
            Dispatcher.Subscribe(EventId.BeforeReconnecting, OnReconnect);

        indicatorDelta = IndicatorPoint ? IndicatorPoint.localPosition : IndicatorDeltaOffset;
        weapons = new Dictionary<GunShellInfo.ShellType, Weapon>
        {
            { GunShellInfo.ShellType.Usual,             new Weapon(this, GunShellInfo.ShellType.Usual) },
            { GunShellInfo.ShellType.Missile_SACLOS,    new Weapon(this, GunShellInfo.ShellType.Missile_SACLOS) },
            { GunShellInfo.ShellType.IRCM,              new Weapon(this, GunShellInfo.ShellType.IRCM) }
        };

        foreach (var weapon in weapons)
            weapon.Value.InstantReload();

        CalculateMaxSpeed();
        if (IsMain && !BattleConnectManager.Instance.FirstConnect)
        {
            statistics = BattleConnectManager.Instance.MyLastPlayerStat;
            statistics.teamId = data.teamId;
            statistics.playerId = PhotonNetwork.player.ID;
        }
        else
            statistics
                = new PlayerStat(
                    playerId: data.playerId,
                    teamId: data.teamId,
                    playerLevel: data.playerLevel,
                    playerName: data.playerName,
                    countryCode: data.hideMyFlag ? string.Empty : (string) data.country,
                    vip: data.vip,
                    profileId: data.profileId,
                    clanName: data.clanName);

        if (!BattleController.allVehicles.ContainsKey(data.playerId))
            BattleController.allVehicles.Add(data.playerId, this);
        
        if(!BattleController.vehicleData.ContainsKey(data.playerId))
            BattleController.vehicleData.Add(data.playerId, data);

        if(!BattleController.GameStat.ContainsKey(data.playerId))
            BattleController.GameStat.Add(data.playerId, statistics);

        rb.isKinematic = !PhotonView.isMine;
        rb.collisionDetectionMode = PhotonView.isMine ? CollisionDetectionMode.ContinuousDynamic : CollisionDetectionMode.Discrete;
        
        if (IsMain)
        {
            tag = Tag.Items[Tag.Key.Player];
            rb.centerOfMass = centerOfMass;
            transform.position = SpawnPoints.instance.GetCorrectPosition(this);
            storedVehiclePosition = transform.position;
            if (BattleConnectManager.Instance.FirstConnect)
                Armor = MaxArmor;

            if (cameraEndPoint)
                CameraTranslationAxis = (cameraEndPoint.localPosition - forCam.localPosition).normalized;

            InitConsumables(BattleController.battleInventory);
        }
        else
        {
            indicator = TankIndicators.AddIndicator(this, false);
            indicator.RedrawHealthBar(data.maxArmor);
            indicator.RedrawHealthBar(data.maxArmor);
            indicator.RedrawHealthBar(data.maxArmor);
            Respawn(transform.position, transform.rotation, true, true);
        }

        if (!ShotPoint)
            DT.LogWarning(gameObject, "Shot point is null!");

        renderers = GetComponentsInChildren<Renderer>(true);
        CollectBoundColliders();
        visible = renderers[0].enabled;
        bodykitController = GetComponent<BodykitController>();

        if (data.patternId != 0)
            bodykitController.DrawCamouflage(PatternPool.Instance.GetItemById(data.patternId), VehicleId);

        if (data.decalId != 0)
            bodykitController.DrawDecal(DecalPool.Instance.GetItemById(data.decalId));

        SetEngineAudio();
        SetTurretAudio();

        bodykitController.SetShadowPlane();

        if (IsBot)
        {
            kickBotAt = (double) PhotonView.instantiationData[2];
            if (PhotonView.isMine)
                SetStartBotProperties();
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
        if (PhotonView.isMine && IsBot)
        {
            ReanimateBot();
            BotDispatcher.Instance.RegisterBotAI(BotAI);
        }

        SetMembership();
        if (!BattleController.MyVehicle && !IsMain)
            Dispatcher.Subscribe(EventId.MainTankAppeared, OnMainVehicleAppeared);

        if (JoystickManager.Instance.joysticks.Length > (int)JoystickManager.Joystics.left)
            leftJoystick = JoystickManager.Instance.joysticks[(int)JoystickManager.Joystics.left];

        if (JoystickManager.Instance.joysticks.Length > (int)JoystickManager.Joystics.right)
            rightJoystick = JoystickManager.Instance.joysticks[(int)JoystickManager.Joystics.right];

        Dispatcher.Send(EventId.TankJoinedBattle, new EventInfo_I(data.playerId));
    }

    public virtual void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        if (isExploded)
            return;

        if (stream.isWriting)
        {
            if (!PhotonView.isMine)
                return;

            stream.SendNext(transform.position);
            stream.SendNext(transform.rotation);
            stream.SendNext(rb.velocity);

            if (Turret)
                stream.SendNext(Turret.localEulerAngles.y);
        }
        else
        {
            if (info.photonView.isMine)
                return;

            MarkActivity();

            int streamCount = stream.Count - stream.currentItem;

            if (streamCount > 0)
                correctPosition = (Vector3)stream.ReceiveNext();

            if (streamCount > 1)
                correctRotation = (Quaternion)stream.ReceiveNext();

            if (streamCount > 2)
                correctVelocity = (Vector3)stream.ReceiveNext();

            if (streamCount > 3)
                correctTurretAngle = (float)stream.ReceiveNext();

            #region Smooth network move 
            // Shift buffer contents, oldest data erased, 18 becomes 19, ... , 0 becomes 1
            for (int i = m_BufferedState.Length - 1; i >= 1; i--) {
                m_BufferedState[i] = m_BufferedState[i - 1];
            }

            // Save currect received state as 0 in the buffer, safe to overwrite after shifting
            State state;
            state.timestamp = info.timestamp;
            state.pos = correctPosition;
            state.rot = correctRotation;
            state.vel = correctVelocity;
            state.tRot = correctTurretAngle;
            state.steering = 0f;
            m_BufferedState[0] = state;

            // Increment state count but never exceed buffer size
            m_TimestampCount = Mathf.Min (m_TimestampCount + 1, m_BufferedState.Length);

            // Check integrity, lowest numbered state in the buffer is newest and so on
            for (int i = 0; i < m_TimestampCount - 1; i++) {
                if (m_BufferedState[i].timestamp < m_BufferedState[i + 1].timestamp)
                    Debug.Log ("State inconsistent");
            }

            //Debug.Log("stamp: " + info.timestamp + "my time: " + PhotonNetwork.time + "delta: " + (PhotonNetwork.time - info.timestamp));
            #endregion

            if (settingSpawnPosition)
            {
                transform.position = correctPosition;
                transform.rotation = correctRotation;

                if (Turret)
                    Turret.localEulerAngles = new Vector3(0, correctTurretAngle, 0);

                settingSpawnPosition = false;
            }

            currentCorrection = 0;
        }
    }

    protected virtual void FixedUpdate() { }

    protected virtual void Update()
    {
        if (!PhotonView)
            return;

        if (!Debug.isDebugBuild)
        {
            inactivityTime += Time.deltaTime;

            if (inactivityTime > GameData.maxInactivityTime)
            {
                if (PhotonView.isMine)
                {
                    BattleController.EndBattle(BattleController.EndBattleCause.Inactivity);
                    return;
                }

                if (BattleConnectManager.IsMasterClient)
                    PhotonNetwork.CloseConnection(PhotonView.owner);
            }
        }

        if (PhotonView.isMine && isAvailable)
        {
            Aiming();
            UpdateEffects();
        }
        else if (!PhotonView.isMine)
        {
            MoveClone();
        }

        if (!isAvailable)
            return;

        foreach (var weapon in weapons.Values)
            weapon.UpdateReloadingProgress();

        if (IsMain)
        {
            if (XDevs.Input.GetButtonDown("Center Turret") || (!ProfileInfo.isFireOnDoubleTap && XDevs.Input.GetButtonDown("Double Tap"))) {
                TurretCentering = true;
                if (aimingController != null) {
                    aimingController.ResetAutoaim ();
                }
            }

            TurretRotation();

            if (IsRequirePrimaryFire && PrimaryFire())
            {
                if (!burst)
                    StartBurst();
            }
            else
            {
                if (burst)
                    StopBurst();
            }

            if (XDevs.Input.GetButtonDown ("Use Consumable 1")) {
                UseConsumableByIndex (0);
            }
            if (XDevs.Input.GetButtonDown ("Use Consumable 2")) {
                UseConsumableByIndex (1);
            }
            if (XDevs.Input.GetButtonDown ("Use Consumable 3")) {
                UseConsumableByIndex (2);
            }

            if (XDevs.Input.GetButtonDown ("Use Super Weapon")) {
                UseSuperWeaponByIndex (0);
            }

#if UNITY_EDITOR
            if (Input.GetButtonDown("ForceRespawn"))
                MakeRespawn(forced: true, restoreLife: false, firstTime: false);

            if (Input.GetKeyDown(KeyCode.B))
                BattleController.EndBattle(BattleController.EndBattleCause.Timeouted);
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
            
        BattleConnectManager.RemovePhotonMessageTarget(gameObject);
        TankIndicators.RemoveIndicator(this);
        Unsubscriptions();

        if (shootEffectPoints != null)
        {
            foreach (Transform point in shootEffectPoints)
                foreach (Transform effect in point)
                    effect.SetParent(EffectPoolDispatcher.GetEffectsParent());
        }

        // Чтобы результаты бота сохранились
        if (IsBot)
        {
            Dispatcher.Send(EventId.OffLayerInTeamMask, new EventInfo_II(data.teamId, ownLayer));
            Dispatcher.Send(EventId.TankOutOfTime, new EventInfo_I(data.playerId));
        }

        if (aimingController != null)
        {
            aimingController = null;
        }

        Dispatcher.Send(EventId.TankLeftTheGame, new EventInfo_I(data.playerId));
        StopCoroutine (changingVehicleMaterial);
    }

    /* PUBLIC SECTION */

    public virtual void AnimateClone() { }
    public virtual void StoreCloneRotation() { }

    /// <summary>
    /// Друзья ли владельцы Vehicl'ов в данном бою.
    /// Внимание! Если транспорт сравнивается сам с собой, возвращается FALSE.
    /// </summary>
    /// <returns></returns>
    public static bool AreFriends(VehicleController player1, VehicleController player2)
    {
        if (player1 == null || player2 == null || player1 == player2)
            return false;

        switch (GameData.Mode)
        {
            case GameData.GameMode.Deathmatch:
                return !string.IsNullOrEmpty(player1.data.clanName) && player1.data.clanName == player2.data.clanName;
            case GameData.GameMode.Team:
                return player1.data.teamId == player2.data.teamId;
            default:
                return false;
        }
    }

    public static bool TryGetHitTarget(RaycastHit hit, out VehicleController vehicle)
    {
        vehicle = hit.transform.GetComponentInParent<VehicleController>();
        return vehicle != null;
    }

    public static bool AreClanmates(VehicleController player1, VehicleController player2)
    {
        if (player1 == null || player2 == null || player1 == player2)
            return false;

        string clan1 = player1.data.clanName;
        string clan2 = player2.data.clanName;

        return !string.IsNullOrEmpty(clan1) && clan1 == clan2;
    }

    public abstract void MovePlayer();

    public abstract bool PrimaryFire();

    public virtual void Aiming()
    {
        if (aimingController != null)
        {
            aimingController.Aiming();
            return;
        }
        
        // Найти точки, куда можно целиться в данный момент
        //Vector3 direction = Turret ? Turret.forward : ShotPoint.forward;

        int hitCount = Physics.CapsuleCastNonAlloc(
            ShotPoint.position + ShotPoint.up * VertAimCapture,
            ShotPoint.position - ShotPoint.up * VertAimCapture,
            HorizAimCapture,
            ShotPoint.forward,
            aimingHits,
            MaxAimDistance,
            EnemyLayerMask,
            QueryTriggerInteraction.Ignore);

        //Выход, если точек нет
        if (hitCount == 0)
        {
            ResetGunsight();
            return;
        }

        #region Отбираем в aimPoints точки выстрела, входящие в макс. угол стрельбы, а также проверяем критзону (она в приоритете) и прошлый коллайдер

        bool aimPointAlreadyFound = false;
        AimPointInfo selectedPointInfo = new AimPointInfo();
        aimPoints.Clear();

        for (int i = 0; i < hitCount; i++)
        {
            RaycastHit hit = aimingHits[i];
            AimPointInfo aimPoint;
            Vector3 hitDirection = (hit.point - ShotPoint.position).normalized;
            if (Vector3.Dot(ShotPoint.forward, hitDirection) < MaxShootAngleCos)
            {
                continue;
            }

            try
            {
                if (CheckHitPoint(hit, out aimPoint))
                {
                    if (aimPoint.critZone)
                    {
                        aimPointAlreadyFound = true;
                        selectedPointInfo = aimPoint;
                        break;
                    }

                    if (aimPointInfo.target != null && (aimPoint.hit.collider == aimPointInfo.hit.collider))
                    {
                        selectedPointInfo = aimPoint;
                        aimPointAlreadyFound = true;
                        continue;
                    }

                    if (!aimPointAlreadyFound)
                        aimPoints.Add(aimPoint);
                }
            }
            catch (Exception e)
            {
                Debug.LogErrorFormat("Exception in gunsight({0})", e.Message);
                ResetGunsight();
                return;
            }
        }

        #endregion

        if (!aimPointAlreadyFound)
        {
            if (aimPoints.Count == 0)
            {
                ResetGunsight();
                return;
            }

            float minDistance = MaxAimDistance;
            selectedPointInfo = aimPoints[0];
            for (int i = 0; i < aimPoints.Count; i++)
            {
                AimPointInfo pointInfo = aimPoints[i];
                if (pointInfo.hit.distance < minDistance)
                {
                    minDistance = pointInfo.hit.distance;
                    selectedPointInfo = pointInfo;
                }
            }
        }
        
        SetNewAimPointInfo(selectedPointInfo);
        if (aimPointInfo.target == null)
            return;

        // Далее: в прицеле кто-то есть

        if (!IsBot)
        {
            Vector3 pointForGunsight = aimPointInfo.point;
            pointForGunsight.y = aimPointInfo.hit.collider.bounds.center.y;
            BattleGUI.ShowGunSightForWorld(pointForGunsight, aimPointInfo.hit.distance);
        }

        TargetPosition = aimPointInfo.point;
        if (Target == aimPointInfo.target)
            return;

        ResetGunsight();
        Target = aimPointInfo.target;

        if (!IsBot)
        {
            Target.SetMarkedStatus(true);
        }

        Dispatcher.Send(EventId.TargetAimed, new EventInfo_IIB(data.playerId, aimPointInfo.target.data.playerId, true));
    }

    public virtual void SecondaryFire(GunShellInfo.ShellType shellType, int targetId, Vector3 aimPointLocalToTarget)
    {
        throw new NotImplementedException();
    }

    public void SyncExistance(bool newExistance)
    {
        if (existanceSynchronized || PhotonView.isMine)
            return;

        IsAvailable = newExistance;
        existanceSynchronized = true;
    }

    public virtual void Respawn(Vector3 position, Quaternion rotation, bool restoreLife, bool firstTime)
    {
        if (restoreLife)
            Armor = data.maxArmor;

        Dispatcher.Send(EventId.TankHealthChanged, new EventInfo_II(data.playerId, Armor));

        settingSpawnPosition = true;
        correctPosition = transform.position = position;
        correctRotation = transform.rotation = rotation;

        if (Turret)
            Turret.localRotation = Quaternion.identity;

        lastTurretLocalRotationY = 0;

        correctVelocity = Vector3.zero;

        storedVehiclePosition = transform.position;

        if (!firstTime)
            IsAvailable = true;

        Dispatcher.Send(EventId.TankRespawned, new EventInfo_I(data.playerId));

        if (IsMain)
        {
            RemoveAllItems();

            Dispatcher.Send(EventId.MyTankRespawned, new EventInfo_SimpleEvent());

            if (respawnSound)
                AudioDispatcher.PlayClipAtPosition(
                    /* clip:     */ respawnSound,
                    /* position: */ transform.position,
                    /* parent:   */ transform);
        }
        else
        {
            indicator.Hidden = !isAvailable;
        }
    }

    public virtual void Explode()
    {
        if (!isAvailable)
            return;

        if (Camera.main != null)
        {
            EffectPoolDispatcher.GetFromPool(
                _effect:    explosionPrefab,
                _position:  transform.position,
                _rotation:  Quaternion.LookRotation(
                                forward:    (Camera.main.transform.position - transform.position).normalized,
                                upwards:    Vector3.up));

            PlayExplosionSound();
        }

        IsAvailable = false;

        if (!PhotonView.isMine)
            return;

        CancelAllEffects();

        foreach (var weapon in weapons)
            weapon.Value.InstantReload();
    }

    public virtual float GetHeating(GunShellInfo.ShellType shellType)
    {
        switch (shellType)
        {
            case GunShellInfo.ShellType.Usual:
                return 0.0f;

            default:
                return 0.0f;
        }
    }

    public virtual float GetCooling(GunShellInfo.ShellType shellType)
    {
        switch (shellType)
        {
            case GunShellInfo.ShellType.Usual:
                return 0.0f;

            default:
                return 0.0f;
        }
    }

    public float GetROF(GunShellInfo.ShellType shellType)
    {
        switch (shellType)
        {
            case GunShellInfo.ShellType.Usual:
                return ROF;

            case GunShellInfo.ShellType.Missile_SACLOS:
                return DEFAULT_ROCKET_FIRE_RATE;

            case GunShellInfo.ShellType.IRCM:
                return IRCMROF;

            default:
                throw new ArgumentOutOfRangeException("shellType", shellType, null);
        }
    }

    public virtual Transform GetShotPoint(BattleItem weapon)
    {
        // TODO: оверрайд для супероружия.
        return ShotPoint;
    }

    public void SetMarkedStatus(bool marked)
    {
        if (vehicleMarker != null)
            vehicleMarker.SetMarkedStatus(marked);
    }

    public virtual void MakeRespawn(bool forced, bool restoreLife, bool firstTime)
    {
        Transform spawnPoint = SpawnPoints.instance.GetRandomPoint(this, data.teamId, forced);
        MakeRespawn(spawnPoint.position, spawnPoint.rotation, restoreLife, firstTime);
    }

    public virtual void MakeRespawn(Vector3 position, Quaternion rotation, bool restoreLife, bool firstTime)
    {
        PhotonView.RPC("Respawn", PhotonTargets.All, position, rotation, restoreLife, firstTime);
    }

    public void TakeBonus(BonusItem.BonusType bonusType, int amount)
    {
        if (!PhotonView.isMine)
            return;

        switch (bonusType)
        {
            case BonusItem.BonusType.Experience:
                TakeExperienceBonus(amount);
                break;
            case BonusItem.BonusType.Gold:
                TakeGoldBonus(amount);
                break;
            case BonusItem.BonusType.Silver:
                TakeSilverBonus(amount);
                break;
            case BonusItem.BonusType.Health:
                TakeHealthBonus();
                break;
            case BonusItem.BonusType.Fuel:
                TakeFuelBonus();
                break;
            case BonusItem.BonusType.GoldRush:
                amount = GoldRush.TotalStake;
                TakeGoldRushBonus();
                break;
        }

        if (IsBot)
            return;

#region Google Analytics: picking up booster

        GoogleAnalyticsWrapper.LogEvent(
            new CustomEventHitBuilder()
                .SetParameter(GAEvent.Category.PickedUpBonus)
                .SetParameter<GAEvent.Action>()
                .SetSubject(GAEvent.Subject.MapName, GameManager.CurrentMap)
                .SetParameter<GAEvent.Label>()
                .SetSubject(GAEvent.Subject.BonusType, bonusType)
                .SetValue(ProfileInfo.Level));

        GoogleAnalyticsWrapper.LogEvent(
            new CustomEventHitBuilder()
                .SetParameter(GAEvent.Category.PickedUpBonus)
                .SetParameter<GAEvent.Action>()
                .SetSubject(GAEvent.Subject.MapName, GameManager.CurrentMap)
                .SetParameter<GAEvent.Label>()
                .SetSubject(GAEvent.Subject.VehicleID, ProfileInfo.currentVehicle)
                .SetValue(ProfileInfo.Level));

#endregion

        Notifier.ShowBonus(bonusType, amount);
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
                    effectAlreadyPresents = true;
            }
            if (!effectAlreadyPresents)
                EffectItself(effect);

            return false;
        }

        EffectItself(effect);
        return effect.MustBeReturned;
    }

    public void TakeEffectAway(int id)
    {
        VehicleEffect effect;
        if (!Effects.TryGetValue(id, out effect))
            return;
        
        EffectItself(effect, true);
    }

    public void ApplyEffect(VehicleEffect effect)
    {
        if (!PhotonView.isMine)
            return;

        effect
            = new VehicleEffect(
                id:        VehicleEffect.GetNewId(),
                efType:     effect.Type,
                modType:    effect.ModType,
                modValue:   effect.ModValue,
                _duration:  effect.Duration,
                _startTime: PhotonNetwork.time,
                bonusType:  effect.Source,
                _icon:      effect.Icon,
                consumableId: effect.ConsumableId);

        Dispatcher.Send(EventId.TankEffectApply, new EventInfo_IE(data.playerId, effect), Dispatcher.EventTargetType.ToAll);
    }

    public Bounds GetEntireAimBounds()
    {
        Bounds totalBounds = boundColliders[0].bounds;
        for (int i=1; i<boundColliders.Length; i++) {
            totalBounds.Encapsulate (boundColliders[i].bounds);
            //totalBounds = MiscTools.SumBounds(totalBounds, col.bounds);
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

#if UNITY_EDITOR
    public virtual void Cheat()
    {
        if (!Debug.isDebugBuild)
            return;

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
            return;

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

        if (VehicleInfo != null && VehicleInfo.isIgnoringCritHits)
            critHit = false;

        if (data.newbie)
            result /= GameManager.NEWBIE_DAMAGE_RATIO;

        result *= Random.Range(GameManager.RANDOM_DAMAGE_RATIO_LOWER_BOUND, GameManager.RANDOM_DAMAGE_RATIO_UPPER_BOUND);

        result *= critHit ? GameData.critDamageRatio : GameData.normDamageRatio;

        return (int)result;
    }

    public Weapon GetWeapon(GunShellInfo.ShellType shellType)
    {
        return weapons[shellType];
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
            aimPointInfo.target.SetMarkedStatus(false);
        aimPointInfo = pointInfo;
        if (aimPointInfo.target != null && !IsBot)
            aimPointInfo.target.SetMarkedStatus(true);
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
        Dispatcher.Subscribe(EventId.StartTurretRotation, OnStartTurretRotation);
        Dispatcher.Subscribe(EventId.StopTurretRotation, OnStopTurretRotation);

        if (IsMain)
        {
            Dispatcher.Subscribe(EventId.TankKilled, OnTankKilledForMain);
            Dispatcher.Subscribe(EventId.TankLeftTheGame, OnVehLeavesForMain);
        }

        if (IsBot)
            Dispatcher.Subscribe(EventId.PhotonRoomCustomPropertiesChanged, RoomPropsChanged);
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
        Dispatcher.Unsubscribe(EventId.StartTurretRotation, OnStartTurretRotation);
        Dispatcher.Unsubscribe(EventId.StopTurretRotation, OnStopTurretRotation);
    }

    private void TryGetBotParams()
    {
        object botHealthObj;
        object botExistanceObj;

        if (PhotonNetwork.room.CustomProperties.TryGetValue(KeyForHealth, out botHealthObj) && botHealthObj != null)
        {
            BattleController.Instance.OnPlayerPropertiesChanged(data.playerId,
                new Hashtable {{"hl", (int) botHealthObj}});
        }
        if (PhotonNetwork.room.CustomProperties.TryGetValue(KeyForExistance, out botExistanceObj) && botExistanceObj != null)
        {
            BattleController.Instance.OnPlayerPropertiesChanged(data.playerId,
                new Hashtable { { "ex", (bool) botExistanceObj } });
        }
    }

    protected virtual void OnIamMaster(EventId id, EventInfo ei)
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
        Dispatcher.Subscribe(EventId.TankLeftTheGame, BotAI.OnBotBehaviourVehicleLeft, 4);
        Dispatcher.Subscribe(EventId.BonusDestroyed, BotAI.OnBotBehaviourBonusDestroyed, 4);

        StartCoroutine(BotAI.DelayAndStartBot());

        BotAI.StartBotAICoroutines();
    }

    public abstract void UpdateBotPrefabs(VehicleController nativeController);

    public virtual void BoundPointsToList(List<Vector3> points) { }

    protected virtual void OnTargetAimed(EventId id, EventInfo ei){} // TODO: Demetri Попробовать отрефакторить этот механизм

    protected virtual void OnVehicleTakesDamage(EventId id, EventInfo ei)
    {
        EventInfo_U info = (EventInfo_U)ei;

        int victimId = (int)info[0];

        if (victimId != data.playerId)
            return;

        int damage = (int)info[1];
        int attackerId = (int)info[2];
        GunShellInfo.ShellType shellType = (GunShellInfo.ShellType)(int)info[3];
        int hits = (int)info[4];
        Vector3 hitPosition = (Vector3)info[5];

        if (!PhotonView.isMine)
            return;

        float takenDamage = damage * TakenDamageRatio;

        Armor -= (int)takenDamage;

        if (takenDamage > 0)
        {
            Dispatcher.Send(
                id:     EventId.TankDamageApplied,
                info:   new EventInfo_U(
                            victimId,
                            (int)takenDamage,
                            attackerId,
                            shellType,
                            hitPosition),
                target: Dispatcher.EventTargetType.ToAll);
        }

        if (!IsBot)
            PhotonNetwork.player.SetCustomProperties(new Hashtable { { "hl", Armor } });
        else
            PhotonNetwork.room.SetCustomProperties(new Hashtable { { KeyForHealth, Armor } });

        if (IsAvailable && Armor <= 0)
        {
            Dispatcher.Send(
                id:     EventId.TankKilled,
                info:   new EventInfo_III(data.playerId, attackerId, (int)shellType),
                target: Dispatcher.EventTargetType.ToAll);
        }

        if (!IsBot)
        {
            switch (shellType)
            {
                case GunShellInfo.ShellType.MachineGun:
                    BattleStatisticsManager.BattleStats["TakenDamage_MachineGun"] += damage;
                    BattleStatisticsManager.BattleStats["TakenHits_MachineGun"]++;
                    break;

                case GunShellInfo.ShellType.Grenade_AGS:
                    BattleStatisticsManager.BattleStats["TakenDamage_AGS"] += damage;
                    BattleStatisticsManager.BattleStats["TakenHits_AGS"]++;
                    break;

                case GunShellInfo.ShellType.Missile_ATGW:
                    BattleStatisticsManager.BattleStats["TakenDamage_ATGW"] += damage;
                    BattleStatisticsManager.BattleStats["TakenHits_ATGW"]++;
                    break;

                case GunShellInfo.ShellType.Missile_SACLOS:
                    BattleStatisticsManager.BattleStats["TakenDamage_SACLOS"] += damage;
                    BattleStatisticsManager.BattleStats["TakenHits_SACLOS"]++;
                    break;

                case GunShellInfo.ShellType.Usual:
                    BattleStatisticsManager.BattleStats["TakenDamage"] += damage;
                    BattleStatisticsManager.BattleStats["TakenHits"]++;
                    break;
            }
        }
    }

    protected virtual void SetEngineNoise(float t)
    {
        if (IsBot || !engineAudio)
            return;

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
            SelfObstacle.enabled = isAvailable;

        rb.isKinematic = !PhotonView.isMine || !isAvailable;

        Dispatcher.Send(EventId.TankAvailabilityChanged, new EventInfo_II(data.playerId, isAvailable ? 1 : 0));

        if (engineAudio != null)
        {
            if (isAvailable)
            {
                if (!engineAudio.isPlaying)
                    engineAudio.Play();
            }
            else
            {
                if (engineAudio.isPlaying)
                    engineAudio.Stop();
            }
        }

        if (turretAudio != null && !IsAvailable && turretAudio.isPlaying)
            turretAudio.Stop();

        if (PhotonView.isMine)
        {
            if (!IsBot)
            {
                if (player != null && player.IsLocal && (!player.CustomProperties.ContainsKey("ex") ||
                                                         (player.CustomProperties["ex"] != null &&
                                                          (bool) player.CustomProperties["ex"] != IsAvailable)))
                {
                    player.SetCustomProperties(new Hashtable {{"ex", IsAvailable}});
                }
            }
            else
            {
                if (PhotonNetwork.inRoom &&
                    (!PhotonNetwork.room.CustomProperties.ContainsKey(KeyForExistance) ||
                     (PhotonNetwork.room.CustomProperties[KeyForExistance] != null &&
                      (bool) PhotonNetwork.room.CustomProperties[KeyForExistance] != IsAvailable)))
                {
                    PhotonNetwork.room.SetCustomProperties(new Hashtable {{KeyForExistance, IsAvailable}});
                }
            }
        }
        if (indicator != null)
            indicator.Hidden = !IsAvailable;
    }

    protected virtual void SetEngineAudio()
    {
        if (!IsMain || GameData.IsGame(Game.Armada | Game.SpaceJet | Game.MetalForce))
            return;

        engineAudio = gameObject.AddComponent<AudioSource>();

        engineAudio.clip = engineSound;
        engineAudio.loop = true;
        engineAudio.rolloffMode = AudioRolloffMode.Linear;
        engineAudio.maxDistance = 65;
        engineAudio.dopplerLevel = 0;
        engineAudio.volume = Settings.SoundVolume * SoundControllerBase.DYNAMIC_ENGINE_VOLUME_MIN;
        engineAudio.pitch = SoundControllerBase.DYNAMIC_ENGINE_PITCH_MIN;
    }

    protected virtual void SetTurretAudio()
    {
        if (!IsMain || turretRotationSound == null || GameData.IsGame(Game.Armada | Game.SpaceJet | Game.MetalForce))
            return;

        turretAudio = gameObject.AddComponent<AudioSource>();

        turretAudio.clip = turretRotationSound;
        turretAudio.loop = true;
        turretAudio.rolloffMode = AudioRolloffMode.Linear;
        turretAudio.volume = Settings.SoundVolume * SoundControllerBase.TURRET_ROTATION_VOLUME;
        turretAudio.maxDistance = 25;
        turretAudio.dopplerLevel = 0;
    }

    protected void StoreVehiclePosition()
    {
        if (transform.position != storedVehiclePosition)
            odometer += Vector3.Distance(rb.position, storedVehiclePosition) * OdometerRatio;

        storedVehiclePosition = transform.position;
    }

    public virtual void ResetGunsight()
    {
        if (Target == null)
            return;

        if (!IsBot)
        {
            BattleGUI.HideGunSight();
            Target.SetMarkedStatus(false);
        }

        int lastTargetId = Target.data.playerId;
        Target = null;

        Dispatcher.Send(EventId.TargetAimed, new EventInfo_IIB(data.playerId, lastTargetId, false));
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
            Dispatcher.Send(EventId.TankEffectCancelled, new EventInfo_II(data.playerId, effect.Id), Dispatcher.EventTargetType.ToAll);

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
                effectsToCancel.Add(effect);
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
        items.Clear();
        IEPanel.Instance.RemoveAllItemsCells();
    }

    private void OnMainVehicleAppeared(EventId id, EventInfo ei)
    {
        SetMembership();
    }

    private void SetMembership()
    {
        if (IsMain)
            ownLayer = LayerMask.NameToLayer("Player");
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
                ownLayer = LayerMask.NameToLayer(IsMainsFriend ? "Friend" : "Enemy");
        }

        if (!PhotonView.isMine)
        {
            if (!IsBot)
            {
                // Игрок по сети
                Hashtable playerProperties = player.CustomProperties;
                if (playerProperties.ContainsKey("ex") && playerProperties["ex"] != null)
                {
                    IsAvailable = (bool) playerProperties["ex"];
                    existanceSynchronized = true;
                }
                else
                    IsAvailable = false;
            }
            else
            { //Бот по сети
                Hashtable roomProperties = PhotonNetwork.room.CustomProperties;
                if (roomProperties.ContainsKey(KeyForExistance) && roomProperties[KeyForExistance] != null)
                {
                    IsAvailable = (bool) roomProperties[KeyForExistance];
                    existanceSynchronized = true;
                }
                else
                    IsAvailable = false;
            }
        }
        else
            IsAvailable = true;
        
        Dispatcher.Subscribe(EventId.NewLayerInTeamMask, OnNewLayerInTeamMask);
        if (IsBot)
            Dispatcher.Send(EventId.NewLayerInTeamMask, new EventInfo_II(data.teamId, ownLayer));
        if (CritZones) {
            CritZones.tag = Tag.Items[Tag.Key.CritZone];
            if (GameData.IsGame (Game.MetalForce)) {
                CritZones.gameObject.layer = LayerMask.NameToLayer ("CritZone");
            }
        }

        if (GameData.Mode == GameData.GameMode.Team)
            Dispatcher.Send(EventId.TeamChange, new EventInfo_SimpleEvent());

        if (!IsMain)
        {
            tag = Tag.Items[IsMainsFriend ? Tag.Key.Friend : Tag.Key.Enemy];
        }
        UberDebug.LogChannel("VehicleController", "Start ResetMasks Coroutine for {0}", data.playerId);
        StartCoroutine(ResetMasks_Coroutine());
    }

    protected virtual void EffectItself(VehicleEffect effect, bool inverted = false)
    {
        if (effect.MustBeReturned)
        {
            if (inverted)
                Effects.Remove(effect.Id);
            else
                Effects.Add(effect.Id, effect);
        }

        effect.ApplyToVehicle(this, inverted);
    }

    protected VehicleEffect FindRelatedEffect(VehicleEffect effect)
    {
        foreach (VehicleEffect eff in Effects.Values)
        {
            if (eff.UI_Id == effect.UI_Id && eff.Type == effect.Type)
                return effect;
        }

        return null;
    }

    private void FillVehicleData()
    {
        object[] playerAndProps = new object[2];

        playerAndProps[0] = player;
        playerAndProps[1] = player.CustomProperties;

        BattleController.Instance.OnPlayerPropertiesChanged(player.ID, player.CustomProperties);
    }

    public virtual void MoveClone()
    {
        if (isExploded)
        {
            transform.position = correctPosition;
            transform.rotation = correctRotation;

            if (Turret)
                Turret.localEulerAngles = Vector3.zero;

            return;
        }
        #region Smooth network move
        double currentTime = PhotonNetwork.time;
        double interpolationTime = currentTime - interpolationBackTime;
        // We have a window of interpolationBackTime where we basically play 
        // By having interpolationBackTime the average ping, you will usually use interpolation.
        // And only if no more data arrives we will use extrapolation

        // Use interpolation
        // Check if latest state exceeds interpolation time, if this is the case then
        // it is too old and extrapolation should be used
        if (m_BufferedState[0].timestamp > interpolationTime) {
            for (int i = 0; i < m_TimestampCount; i++) {
                // Find the state which matches the interpolation time (time+0.1) or use last state
                if (m_BufferedState[i].timestamp <= interpolationTime || i == m_TimestampCount - 1) {
                    // The state one slot newer (<100ms) than the best playback state
                    State rhs = m_BufferedState[Mathf.Max(i-1, 0)];
                    // The best playback state (closest to 100 ms old (default time))
                    State lhs = m_BufferedState[i];

                    // Use the time between the two slots to determine if interpolation is necessary
                    double length = rhs.timestamp - lhs.timestamp;
                    float t = 0.0F;
                    // As the time difference gets closer to 100 ms t gets closer to 1 in 
                    // which case rhs is only used
                    if (length > 0.0001)
                        t = (float)((interpolationTime - lhs.timestamp) / length);

                    // if t=0 => lhs is used directly
                    transform.position = Vector3.Lerp (lhs.pos, rhs.pos, t);
                    transform.rotation = Quaternion.Slerp (lhs.rot, rhs.rot, t);
                    rb.velocity = Vector3.Lerp (lhs.vel, rhs.vel, t);
                    if (Turret)
                        Turret.localEulerAngles = new Vector3 (0, Mathf.MoveTowardsAngle (lhs.tRot, rhs.tRot, t), 0);
                    return;
                }
            }
        }
        // Use extrapolation. Here we do something really simple and just repeat the last
        // received state. You can do clever stuff with predicting what should happen.
        else {
            State latest = m_BufferedState[0];

            transform.position = latest.pos;
            transform.rotation = latest.rot;
            rb.velocity = latest.vel;
            if (Turret)
                Turret.localEulerAngles = new Vector3 (0, latest.tRot, 0);
        }
        #endregion

        //if (transform.position == correctPosition && transform.rotation == correctRotation)
        //    transform.Translate (correctVelocity * Time.deltaTime);
        //else {
        //    currentCorrection = Mathf.Clamp01 (currentCorrection + Time.deltaTime / 0.2f);

        //    transform.position = Vector3.Lerp (transform.position, correctPosition, currentCorrection);
        //    transform.rotation = Quaternion.Lerp (transform.rotation, correctRotation, currentCorrection);
        //}

        //if (Turret)
        //    Turret.localEulerAngles = new Vector3(0, Mathf.MoveTowardsAngle(Turret.localEulerAngles.y, correctTurretAngle, Speed * TurretRotationSpeedQualifier * Time.deltaTime), 0);
    }

    private void CalculateMaxSpeed()
    {
        MaxSpeed = data.speed / SpeedRatio;
    }

    protected virtual void TakeExperienceBonus(int amount)
    {
        if (PhotonView.isMine)
        {
            ScoreCounter.ScoreInto(this, amount);
            if (IsMain)
                Dispatcher.Send(EventId.ExperienceAcquired, new EventInfo_I(amount));
        }
    }

    protected virtual void TakeGoldBonus(int amount)
    {
        if (IsMain) {
            ProfileInfo.ReplenishBalance(ProfileInfo.Price.Gold(amount));
            Dispatcher.Send(EventId.GoldAcquired, new EventInfo_I(amount));
        }
    }

    protected virtual void TakeSilverBonus(int amount)
    {
        if (IsMain) {
            ProfileInfo.ReplenishBalance(ProfileInfo.Price.Silver(amount));
            Dispatcher.Send(EventId.SilverAcquired, new EventInfo_I(amount));
        }
    }

    protected virtual void TakeHealthBonus()
    {
        if (!PhotonView.isMine)
            return;

        Armor = MaxArmor;
        if (!IsBot)
            player.SetCustomProperties(new Hashtable {{"hl", Armor}});
        else
        {
            Hashtable properties = new Hashtable {{KeyForHealth, Armor}};
            PhotonNetwork.room.SetCustomProperties(properties);
        }
    }

    protected virtual void TakeFuelBonus()
    {
        if (IsMain)
            Dispatcher.Send(EventId.FuelAcquired, new EventInfo_I(1));
    }

    protected virtual void TakeGoldRushBonus()
    {
        if (!PhotonView.isMine || !GameData.IsGoldRushEnabled)
            return;

        if (BattleController.TimeRemaining < GameData.GoldRushMinTime)
        {
            BattleController.Instance.ProlongGameForFree(GameData.GoldRushMinTime - BattleController.TimeRemaining);
            TopPanelValues.ShowCriticalTime(false);
        }

        GoldRush.AwardPermission = true;

        Hashtable properties = new Hashtable();

        properties.Add("goldLeader", data.playerId);

        PhotonNetwork.room.SetCustomProperties(properties);

        MakeRespawn(false, false, false);
    } // TODO: Demetri Если включат зотолую лихорадку, то проверить, как работает для ботов

    public virtual void TurretRotation()
    {
        if (!Turret)
            return;

        float deltaForRotation = 0;

        float currentTurretRotationY = Turret.localEulerAngles.y;
        bool turretIdle = HelpTools.Approximately(lastTurretLocalRotationY, currentTurretRotationY);

        if (turretIdle && !isTurretIdleFrameBefore)
            Dispatcher.Send(EventId.StopTurretRotation, new EventInfo_I(data.playerId));
        else if (!turretIdle && isTurretIdleFrameBefore)
            Dispatcher.Send(EventId.StartTurretRotation, new EventInfo_I(data.playerId));

        isTurretIdleFrameBefore = turretIdle;

        lastTurretLocalRotationY = Turret.localEulerAngles.y;

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
        if (BattleSettings.Instance != null) {
            realRotation = Mathf.Clamp (
                   value: HelpTools.ApplySensitivity (deltaForRotation, BattleSettings.Instance.TurretRotationSensitivity) * maxTurretRotationAngle,
                   min: -maxTurretRotationAngle,
                   max: maxTurretRotationAngle);
        } 
        else {
            realRotation = Mathf.Clamp (
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
            return;

        foreach (var effect in effectsToCancel)
            Dispatcher.Send(EventId.TankEffectCancelled, new EventInfo_II(data.playerId, effect.Id), Dispatcher.EventTargetType.ToAll);

        effectsToCancel.Clear();
    }

    private void OnReconnect(EventId eid, EventInfo ei)
    {
        CancelAllEffects();
    }

    private void CheckAllBotStats()
    {
        if (PhotonNetwork.room.CustomProperties == null)
            return;

        CheckBotStat(KeyForScore, out statistics.score);
        CheckBotStat(KeyForDeaths, out statistics.deaths);
        CheckBotStat(KeyForKills, out statistics.kills);
    }

    private void CheckBotStat(string propertyKey, out int statField)
    {
        Hashtable properties = PhotonNetwork.room.CustomProperties;
        object value;
        statField = properties.TryGetValue(propertyKey, out value) && value != null ? (int)value : 0;
    }

    private void SetPropertyKeys()
    {
        bool isBot = IsBot;

        KeyForDeaths = isBot ? string.Format("btdt{0}", data.playerId) : "dt";
        KeyForKills = isBot ? string.Format("btkl{0}", data.playerId) : "kl";
        KeyForScore = isBot ? string.Format("btsc{0}", data.playerId) : "sc";
        KeyForHealth = isBot ? string.Format("bthl{0}", data.playerId) : "hl";
        KeyForExistance = isBot ? string.Format("btex{0}", data.playerId) : "ex";

        KeyForAttack = isBot ? string.Format("btat{0}", data.playerId) : "at";
        KeyForRoF = isBot ? string.Format("btrf{0}", data.playerId) : "rf";
        KeyForSpeed = isBot ? string.Format("btsp{0}", data.playerId) : "sp";
        KeyForMaxArmor = isBot ? string.Format("btmar{0}", data.playerId) : "mar";
        KeyForDamageRatio = isBot ? string.Format("bttdr{0}", data.playerId) : "tdr";

        /*        KeyForMaxArmor { get; private set; }

            KeyForRegen { get; private set; }

            public string KeyForShield { get; private set; }

            public string KeyForDamageRatio { get; private set; }*/
    }

    private void ResetMasks()
    {
        if (IsMain)
        {
            EnemyLayerMask = MiscTools.GetLayerMask("Enemy");
        }
        else if (GameData.IsTeamMode)
        {
            EnemyLayerMask = IsMainsFriend
                    ? MiscTools.GetLayerMask("Enemy")
                    : MiscTools.GetLayerMask("Player", "Friend");
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

        if (!GameData.IsGame (Game.BattleOfWarplanes | Game.WingsOfWar | Game.BattleOfHelicopters)) {
            if (GameData.IsGame (Game.MetalForce)) {
                aimingController = new AimingControllerMF (this);
            }
            else {
                aimingController = new AimingController (this);
            }
        }

        othersLayerMask = MiscTools.GetLayerMask(Layer.Key.Enemy, Layer.Key.Friend, Layer.Key.Player) |
                          BotDispatcher.BotsCommonMask;
        othersLayerMask = MiscTools.ExcludeLayerFromMask(othersLayerMask, ownLayer);
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
        Hashtable properties =
            new Hashtable
            {
                { KeyForHealth, MaxArmor },
                { KeyForScore, 0 },
                { KeyForKills, 0 },
                { KeyForDeaths, 0 }
            };
        PhotonNetwork.room.SetCustomProperties(properties);
    }

    private void OnNewLayerInTeamMask(EventId id, EventInfo ei)
    {
        if (GameData.Mode == GameData.GameMode.Deathmatch)
            return;

        EventInfo_II info = ei as EventInfo_II;
        int teamId = info.int1;
        int layer = info.int2;

        if (teamId != data.teamId)
        {
            EnemyLayerMask = EnemyLayerMask | (1 << layer);
        }
    }

    private void OnOffLayerInTeamMask(EventId id, EventInfo ei)
    {
        if (GameData.Mode == GameData.GameMode.Deathmatch)
            return;

        EventInfo_II info = ei as EventInfo_II;
        int teamId = info.int1;
        int layer = info.int2;
        if (teamId != data.teamId)
            EnemyLayerMask = EnemyLayerMask & ~(1 << layer);
    }

    private IEnumerator ResetMasks_Coroutine()
    {
        yield return null;
        ResetMasks();
    }

    private IEnumerator Regenerate()
    {
        if (!PhotonView.isMine)
            yield break;

        WaitForSeconds oneSecondWaiting = new WaitForSeconds(1f);

        while (Regeneration != 0)
        {
            if (Armor != MaxArmor)
            {
                Dispatcher.Send(
                    id:     EventId.TankTakesDamage,
                    info:   new EventInfo_U(
                                /* victimId */      data.playerId,
                                /* damage */        -Regeneration,
                                /* attackerId */    0,
                                /* shellType */     GunShellInfo.ShellType.Usual,
                                /* hits */          1,
                                /* hitPosition */   Vector3.zero));
            }

            yield return oneSecondWaiting;
        }
    }

    private void OnShellHit(EventId eid, EventInfo ei)
    {
        EventInfo_U info = (EventInfo_U)ei;

        int victimId = (int)info[0];
        int damage = (int)info[1];
        int ownerId = (int)info[2];
        GunShellInfo.ShellType shellType = (GunShellInfo.ShellType)(int)info[3];
        int hits = (int)info[4];
        Vector3 hitPosition = (Vector3)info[5];

        if (victimId != data.playerId)
            return;

        Vector3 position = transform.TransformPoint(hitPosition);

        if (shellType == GunShellInfo.ShellType.Usual)
        {
            Shell shell
                = ShellPoolManager.GetShell(
                    shellName:  GunShellInfo.GetShellInfoForType(shellType).shellPrefabName,
                    position:   Vector3.zero,
                    rotation:   Quaternion.identity);

            shell.Explosion(
                position:       position,
                hitsVehicle:    true,
                victim:         this);
        }

        Dispatcher.Send(
            id:     EventId.TankTakesDamage,
            info:   new EventInfo_U(
                        /* victimId */      victimId,
                        /* damage */        damage,
                        /* attackerId */    ownerId,
                        /* shellType */     shellType,
                        /* hits */          hits,
                        /* hitPosition */   position));

        if (!PhotonView.isMine)
            return;

        if (IsBot)
        {
            Hashtable props = new Hashtable {{ KeyForHealth, Armor}};
            PhotonNetwork.room.SetCustomProperties(props);
        }
        else
        {
            Hashtable props = new Hashtable {{ "hl", Armor } };
            Player.SetCustomProperties(props);
        }
    }

    protected virtual void OnTankKilledForMain(EventId eid, EventInfo ei)
    {
        EventInfo_III info = (EventInfo_III) ei;
        if (info.int1 == data.playerId)
            ResetGunsight();
    }

    private void OnVehLeavesForMain(EventId eid, EventInfo ei)
    {
        EventInfo_I info = (EventInfo_I)ei;
        if (Target != null && info.int1 == Target.data.playerId)
            ResetGunsight();
    }

    private void OnBattleSettingsSubmited(EventId eid, EventInfo ei)
    {
        if (GameData.IsGame(Game.Armada | Game.MetalForce)) // У Армады отдельный контроллер для звуков.
            return;

        if (IsMain && turretAudio != null)
            turretAudio.volume = Settings.SoundVolume * SoundControllerBase.TURRET_ROTATION_VOLUME;
    }

    private void OnStopTurretRotation(EventId id, EventInfo ei)
    {
        if (GameData.IsGame(Game.Armada | Game.MetalForce)) // У Армады отдельный контроллер для звуков.
            return;

        EventInfo_I info = (EventInfo_I)ei;

        int playerId = info.int1;

        if (playerId != data.playerId || !IsMain)
            return;

        if (turretAudio.isPlaying)
            turretAudio.Stop();
    }

    private void OnStartTurretRotation(EventId id, EventInfo ei)
    {
        if (GameData.IsGame(Game.Armada | Game.MetalForce)) // У Армады отдельный контроллер для звуков.
            return;

        EventInfo_I info = (EventInfo_I)ei;

        int playerId = info.int1;

        if (playerId != data.playerId || !IsMain)
            return;

        if (!turretAudio.isPlaying)
            turretAudio.Play();
    }
    
    private void CollectBoundColliders()
    {
        List<Collider> boundColliderList = new List<Collider>(8);
        Transform[] objs = { Body, Turret };
        foreach (var t in objs) {
            if (t != null) {
                var colls = t.GetComponentsInChildren<Collider> ();
                if (colls != null) {
                    foreach (var c in colls) {
                        if (c.enabled && c.gameObject.activeInHierarchy && !boundColliderList.Contains (c)) {
                            boundColliderList.Add (c);
                        }
                    }
                }
            }
        }

        boundColliders = boundColliderList.ToArray();

        CritZonePlace = BoundsVertZone.None;
        foreach (Collider col in boundColliders)
        {
            //Debug.Log (string.Format ("{0}: collider {1} {2}", gameObject.name, col.gameObject.name, col.GetType().Name), col.gameObject);
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
            return BoundsVertZone.Top;

        if (colBounds.max.y < entireBounds.center.y)
            return BoundsVertZone.Bottom;

        return BoundsVertZone.Center;
    }

    private void InitConsumables(Dictionary<int, ObscuredInt> consumableDict)
    {
        foreach (var consPair in consumableDict)
        {
            consumables[consPair.Key] = new BattleConsumable(this, GameData.consumableInfos[consPair.Key]);
        }
    }

    protected bool UseConsumableByIndex (int index) {
        if (index < 0 || index >= ConsumablesInventoryPanel.inventoryList.Count) {
            return false;
        }
        if (!consumables.ContainsKey (ConsumablesInventoryPanel.inventoryList[index])) {
            return false;
        }

        return consumables[ConsumablesInventoryPanel.inventoryList[index]].Use ();
    }

    protected bool UseSuperWeaponByIndex (int index) {
        if (index < 0 || index >= SuperWeaponsInventoryPanel.inventoryList.Count) {
            return false;
        }
        if (!consumables.ContainsKey (SuperWeaponsInventoryPanel.inventoryList[index])) {
            return false;
        }

        return consumables[SuperWeaponsInventoryPanel.inventoryList[index]].Use ();
    }



    public virtual Vector3 GetConsumableInstantiatePosition (string prefabName) {
        return transform.position;
    }

#if UNITY_EDITOR
    private void TestConsumable(int index)
    {
        Dictionary<int, BattleConsumable>.Enumerator enumer = consumables.GetEnumerator();
        for (int i = 0; i <= index; i++)
        {
            if (!enumer.MoveNext())
                return;
        }

        BattleConsumable consumable = enumer.Current.Value;
        if (consumable != null)
            consumable.Use();
    }
#endif
#if UNITY_EDITOR
    virtual protected void OnDrawGizmos () {
        Gizmos.color = Color.magenta;
        Gizmos.DrawSphere (center: transform.TransformPoint (centerOfMass), radius: 0.1f);

        if (aimingController != null) {
            aimingController.DrawGizmos ();
        }
    }
#endif
}