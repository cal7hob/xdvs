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

#if UNITY_EDITOR
using UnityEditor;
#endif

using Hashtable = ExitGames.Client.Photon.Hashtable;
using Random = UnityEngine.Random;

public abstract class VehicleController : MonoBehaviour, IPunObservable, IDamageable
{
    protected const float DEFAULT_SOUND_DISTANCE = 65.0f;
    protected const float MAX_SYNC_TIME = 0.2f;

    [Header("Данные")]
    public ObscuredInt id;
    public ObscuredInt tankGroup;
    public VehicleData data;

    [Header("Эффекты и префабы")]
    public FXInfo shotFXInfo;
    public FXInfo explosionFxInfo;
    public int shellId = 1;
    public string crashModelResource;

    [Header("Ссылки")]
    public Transform lookPoint;
    public Transform cameraEndPoint;
    public Transform forCam;
    public Transform[] shootEffectPoints = null;

    [Header("Звуки")]
    public AudioClip engineSound;
    public AudioClip turretRotationSound;
    public AudioClip shotSound;
    public AudioClip explosionSound;
    public AudioClip respawnSound;
    public AudioClip[] collisionSounds;

    [Header("Физика")]
    public ObscuredFloat maxSpeed = 5;
    public Vector3 centerOfMass;

    [Header("Прочее")]
    public bool continuousFire;
    public float turretRotationSpeedQualifier = 0.03f; 
    public float rotationSpeedQualifier = 1.2f;

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

    protected Transform cannonEnd;
    protected bool isAvailable;
    protected bool isExploded;
    protected bool burst;
    protected bool settingSpawnPosition = true;
    protected ObscuredFloat odometer;

    // *** Для синхрона по сети
    protected Vector3 correctPosition; // Финальная позиция для синхрона
    protected Quaternion correctRotation; // Финальный поворот для синхрона
    protected Vector3 posSyncVelocity = Vector3.zero; // Текущая скорость для синхрона позиции
    protected float syncTime; // Отведенное на синхрон время
    protected float rotSyncSpeed; // Скорость синхрона поворота (рад/сек.)
                                  // ***

    protected DPool<Shell> shellPool;
    protected Vector3 storedVehiclePosition;
    protected Vector3 fixatedTurretDir;
    protected Rigidbody rb;
    protected Animator animator;
    protected AudioSource turretAudio;
    protected AudioSource engineAudio;
    protected new Renderer renderer;
    protected PhotonPlayer player;
    protected VehicleInfo vehicleInfo;
    protected float correctTurretAngle;
    protected double kickBotAt; // Когда выгнать из комнаты, если бот
    protected int hitMask;
    [SerializeField]
    protected LayerMask realHitMask;
    protected int ownLayer; // Слой своего vehicle
    protected Transform aimingPoint;
    [SerializeField]
    protected Transform turret;
    protected Transform critZones;
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
    protected WeaponController weaponController;
    public WeaponController WeaponController { get { return weaponController; } }
    protected string shellPath;
    protected VehicleEffectManager effectManager;
    public VehicleEffectManager EffectManager { get { return effectManager;} }

    private bool onGround;
    private int groundContactCount = 0;

    private int currentEffectPointInd = 0;

    private bool visible;
    private bool cheatActivated;
    private float inactivityTime;
    private Vector3 indicatorDelta;
    [SerializeField]
    private Transform body;
    [SerializeField]
    private Transform bodyMeshTransform;
    private PlayerStat statistics;
    private BodykitController bodykitController;
    private Renderer[] renderers;
    protected Collider entireCollider;
    protected VehicleFXShaper fxShaper;
    private int maxArmorBeforeCheat;
    private IEnumerator changingVehicleMaterial;
    private bool existanceSynchronized;
    private Dictionary<int, BattleConsumable> consumables = new Dictionary<int, BattleConsumable>();

    public event Action<bool> OnAvailabilityChanged;

    public float MaxShootAngleCos { get; private set; } // Косинус максимального угла прицеливания

    public bool OnGround
    {
        get { return onGround; }
    }

    public BattleConsumable GetConsumable(int consumableId)
    {
        BattleConsumable consumable;
        consumables.TryGetValue(consumableId, out consumable);

        return consumable;
    }

    public UnityEngine.AI.NavMeshObstacle SelfObstacle { get; protected set; }

    public bool TurretCentering { get; protected set; }

    public float LastTurretLocalRotationY { get { return lastTurretLocalRotationY; } }

    public bool IsTurretIdleFrameBefore { get { return isTurretIdleFrameBefore; } }

    public Vector3 CameraTranslationAxis { get; protected set; }

    public Vector3 CorrectPosition { get { return correctPosition; } }

    public Quaternion CorrectRotation { get { return correctRotation; } }

    public float CorrectTurretAngle { get { return correctTurretAngle; } }

    public bool SettingSpawnPosition { get { return settingSpawnPosition; } }

    public int OthersLayerMask { get { return othersLayerMask; } }

    public int OwnLayer { get { return ownLayer; } }

    public double KickBotAt { get { return kickBotAt; } }


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

    public string KeyForEffect { get; private set; }

    public PhotonView PhotonView { get; protected set; }

    public Rigidbody Rb { get { return rb; } }

    public float GetParameterForCalc(VehicleEffect.ParameterType parameter)
    {
        switch (parameter)
        {
            case VehicleEffect.ParameterType.Armor:
                return data.maxArmor;
            case VehicleEffect.ParameterType.Attack:
                return data.attack;
            case VehicleEffect.ParameterType.MaxArmor:
                return data.maxArmor;
            case VehicleEffect.ParameterType.Regeneration:
                return data.regeneration;
            case VehicleEffect.ParameterType.RoF:
                return data.roF;
            case VehicleEffect.ParameterType.Speed:
                return Speed;
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
            case VehicleEffect.ParameterType.MaxArmor:
                MaxArmor = Mathf.RoundToInt(value);
                break;
            case VehicleEffect.ParameterType.Regeneration:
                Regeneration = Mathf.RoundToInt(value);
                break;
            case VehicleEffect.ParameterType.RoF:
                RoF = value;
                break;
            case VehicleEffect.ParameterType.Speed:
                Speed = value;
                break;
            case VehicleEffect.ParameterType.TakenDamageRatio:
                TakenDamageRatio = value;
                break;
        }
    }

    private Mesh bodyMesh;
    public Mesh BodyMesh
    {
        get
        {
            if (bodyMesh == null)
                bodyMesh = BodyMeshTransform.GetComponentInChildren<MeshFilter>(true).sharedMesh;

            return bodyMesh;
        }
    }

    public Collider EntireCollider
    {
        get { return entireCollider; }
    }

    public Bounds EntireBounds
    {
        get { return entireCollider.bounds; }
    }

    public VehicleFXShaper FXShaper
    {
        get
        {
            if (fxShaper == null)
            {
                fxShaper = GetComponentInChildren<VehicleFXShaper>(true);
            }

            return fxShaper;
        }
    }

    public float WeaponReloadingProgress
    {
        get
        {
            return weaponController.Progress;
        }
    }

    public Transform Turret
    {
        get
        {
            if (turret == null)
                turret = transform.FindInHierarchy("Turret");

            return turret;
        }
    }

    public Transform AimingPoint
    {
        get
        {
            if (aimingPoint == null)
                aimingPoint = transform.FindInHierarchy("ShotPoint");

            return aimingPoint;
        }
        set { aimingPoint = value; }
    }

    public Transform CannonEnd
    {
        get
        {
            if (cannonEnd == null)
                cannonEnd = transform.FindInHierarchy("CannonEnd");

            return cannonEnd;
        }
    }

    public virtual Vector3 TargetPoint
    {
        get { return transform.position; }
    }

    public virtual Vector3 Velocity
    {
        get
        {
            return PhotonView.isMine ? rb.velocity : posSyncVelocity;
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

    public bool IsExploded
    {
        get { return isExploded; }
    }

    public bool IsMain
    {
        get
        {
            return 
                ((PhotonNetwork.connected
                    && PhotonView.isMine)
                    || (data.playerId == BattleController.MyPlayerId))
                && !IsBot; }
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
                return false; //TODO Обратить внимание! Возможно, в дальнейшем стоит переделать.

            if (!BattleController.MyVehicle)
                return false;

            return AreFriends(BattleController.MyVehicle, this);
        }
    }

    public bool Stunned { get; set; }

    public bool Blinded { get; set; }

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
            
            /*StopCoroutine("Regenerate");
            if (value != 0)
                StartCoroutine("Regenerate");*/
        }
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

    public float RoF
    {
        get { return data.roF; }
        set { data.roF = Mathf.Clamp(value, 0, float.MaxValue); }
    }

    public float CurrentSpeed {
        get {
            return LocalVelocity.z;
        }
    }

    public float CurrentSpeedRatio {
        get {
            return LocalVelocity.z / maxSpeed;
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

    public bool TargetAimed { get { return Target != null; } }

    public VehicleController Target
    {
        get { return aimingController.Target; }
    }

    public Vector3 TargetPosition
    {
        get { return aimingController.TargetPosition; }
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
    [SerializeField] protected int enemyLayerMask;

    public virtual BotAI BotAI { get { return null; } }

    public abstract VehicleInfo.VehicleType VehicleType { get; }

    public virtual Transform Body
    {
        get
        {
            if (body == null)
                body = transform.FindInHierarchy("Body") ?? transform.FindInHierarchy("Mesh");
            return body;
        }
    }

    public virtual Transform BodyMeshTransform
    {
        get
        {
            if (bodyMeshTransform == null)
                bodyMeshTransform = transform.Find("Mesh_Body") ?? Body;

            return bodyMeshTransform;
        }
    }

    public virtual Transform ForCam
    {
        get
        {
            return forCam = forCam ?? turret.FindInHierarchy("ForCamera");
        }
    }

    public bool DoubleExperience { get; private set; }

    public BoundsVertZone CritZonePlace { get; private set; }

    protected abstract float OdometerRatio { get; }

    protected abstract float SpeedRatio { get; }

    protected abstract float MaxShootAngle { get; }

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
            return !BattleGUI.SomeWindowOnScreen ? XDevs.Input.GetAxis("Turret Rotation") : 0;
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

    public bool IsRequirePrimaryFire
    {
        get
        {
            return PrimaryFireIsOn && FireButtonPressed && (!BattleGUI.SomeWindowOnScreen || IsBot);
        }
    }

    public virtual bool IsRequireSecondaryFire
    {
        get { return false; /*&& SecondaryFireIsOn;*/ }
    }

    public bool PrimaryFireIsOn { get; set; }

    public int HitMask
    {
        get { return hitMask; }
        set
        {
            hitMask = value;
            realHitMask = hitMask;
        }
    }

    protected virtual Vector3 IndicatorDeltaOffset
    {
        get { return Vector3.zero; }
    }

    protected virtual Transform IndicatorPoint
    {
        get { return null; }
    }

    protected Transform CritZones
    {
        get { return critZones = critZones ?? transform.FindInHierarchy("CritZones"); }
    }

    protected Transform bumper;
    protected Transform Bumper
    {
        get
        {
            if (!bumper)
                bumper = transform.FindInHierarchy("Bumper");

            return bumper;
        }
    }

    protected Transform groundChecker;
    protected Transform GroundChecker
    {
        get
        {
            if (!groundChecker)
                groundChecker = transform.FindInHierarchy("GroundChecker");

            return groundChecker;
        }
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

    /* UNITY and EVENT SECTION */

    protected virtual void OnPhotonInstantiate(PhotonMessageInfo info)
    {
#if UNITY_EDITOR
        SceneView.onSceneGUIDelegate += OnSceneView;
#endif

        SetupComponents();
        InitProperties();
        StartCoroutines();
        AnotherInitializations();
        
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

        if (Bumper)
            Bumper.gameObject.layer = LayerMask.NameToLayer(Layer.Items[Layer.Key.TankBumper]);

        if (GroundChecker)
            GroundChecker.gameObject.layer = LayerMask.NameToLayer(Layer.Items[Layer.Key.GroundChecker]);

        Subscriptions();
        weaponController.InstantReload();

		CalculateMaxSpeed();

        if (!BattleController.allVehicles.ContainsKey(data.playerId))
            BattleController.allVehicles.Add(data.playerId, this);
		
        if(!BattleController.vehicleData.ContainsKey(data.playerId))
		    BattleController.vehicleData.Add(data.playerId, data);

        if(!BattleController.GameStat.ContainsKey(data.playerId))
		    BattleController.GameStat.Add(data.playerId, statistics);

        if (IsMain)
		{
            tag = Tag.Items[Tag.Key.Player];
            //rb.centerOfMass = centerOfMass;
            transform.position = SpawnPoints.instance.CheckSpawnPosition(this, transform.position);
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

        visible = renderers[0].enabled;
        SetEngineAudio();
        SetTurretAudio();

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

        RegisterForPhoton();

        SetMembership();
        FindCritZonePlace();

        if (!BattleController.MyVehicle && !IsMain)
            Messenger.Subscribe(EventId.MainTankAppeared, OnMainVehicleAppeared);

        if (JoystickManager.Instance.joysticks.Length > (int)JoystickManager.Joystics.left)
            leftJoystick = JoystickManager.Instance.joysticks[(int)JoystickManager.Joystics.left];

        if (JoystickManager.Instance.joysticks.Length > (int)JoystickManager.Joystics.right)
            rightJoystick = JoystickManager.Instance.joysticks[(int)JoystickManager.Joystics.right];

        if (terrainLayer == 0)
            terrainLayer = LayerMask.NameToLayer(Layer.Items[Layer.Key.Terrain]);

        if (PhotonView.isMine && IsBot)
        {
            ReanimateBot();
            BotDispatcher.Instance.RegisterBotAI(BotAI);
        }

        Messenger.Send(EventId.TankJoinedBattle, new EventInfo_I(data.playerId));
    }

#if UNITY_EDITOR
    protected virtual void OnSceneView(SceneView sceneView) {}
#endif

    protected virtual void OnCollisionEnter(Collision collision)
    {
        PlayCollisionSound(collision);
    }

    protected virtual void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.layer != terrainLayer)
            return;

        groundContactCount++;
        onGround = true;
    }

    protected virtual void OnTriggerExit(Collider other)
    {
        if (other.gameObject.layer != terrainLayer)
            return;

        groundContactCount--;
        if (groundContactCount == 0)
            onGround = false;
    }

    public virtual void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        if (stream.isWriting)
        {
            if (IsExploded)
                return;

            stream.SendNext(transform.position);
            stream.SendNext(transform.rotation);

            if (Turret)
                stream.SendNext(Turret.localEulerAngles.y);
        }
        else
        {
            MarkActivity();
            int itemCount = stream.Count - stream.currentItem;

            correctPosition = (Vector3)stream.ReceiveNext();
            correctRotation = (Quaternion)stream.ReceiveNext();
            if (itemCount > 2)
                correctTurretAngle = (float)stream.ReceiveNext();

            if (settingSpawnPosition)
            {
                transform.position = correctPosition;
                transform.rotation = correctRotation;

                if (Turret)
                    Turret.localEulerAngles = new Vector3(0, correctTurretAngle, 0);

                settingSpawnPosition = false;
            }

            syncTime = Mathf.Min(PhotonNetwork.GetPing() * 0.001f, MAX_SYNC_TIME);
            rotSyncSpeed = Quaternion.Angle(transform.rotation, correctRotation) / syncTime;
        }
    }

    protected virtual void MoveClone()
    {
        if (!isAvailable)
        {
            transform.position = correctPosition;
            transform.rotation = correctRotation;

            if (Turret)
                Turret.localEulerAngles = Vector3.zero;

            return;
        }

        transform.position = Vector3.SmoothDamp(transform.position, correctPosition, ref posSyncVelocity, syncTime);
        transform.rotation = Quaternion.RotateTowards(transform.rotation, correctRotation, rotSyncSpeed * Time.deltaTime);

        if (Turret)
            Turret.localEulerAngles = new Vector3(0, Mathf.MoveTowardsAngle(Turret.localEulerAngles.y, correctTurretAngle, Speed * TurretRotationSpeedQualifier * Time.deltaTime), 0);
    }

    private void PlayCollisionSound(Collision collision)
    {
        if (collision.relativeVelocity.sqrMagnitude > 36.0f &&
            MiscTools.CheckIfLayerInMask(othersLayerMask, collision.gameObject.layer))
        {
            AudioClip collisionSound = CollisionSound;

            if (collisionSound != null)
                AudioDispatcher.PlayClipAtPosition(
                    clip: collisionSound,
                    position: transform.position,
                    volume: Settings.SoundVolume * SoundSettings.COLLISION_VOLUME);
        }
    }

    private AudioClip CollisionSound
    {
        get { return collisionSounds == null || collisionSounds.Length == 0 ? null : collisionSounds.GetRandomItem(); }
    }

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

                if (PhotonNetwork.isMasterClient)
                    PhotonNetwork.CloseConnection(PhotonView.owner);
            }
        }

        if (!isAvailable)
            return;

        if (!Stunned)
        {
            weaponController.UpdateWeapon();
        }

        effectManager.Update();

        if (PhotonView.isMine)
        {
            aimingController.Aiming();
        }
        else
            MoveClone();
        
        CheckBurst();
        if (IsMain)
        {
            if (XDevs.Input.GetButtonDown("Center Turret") || (!ProfileInfo.isFireOnDoubleTap && XDevs.Input.GetButtonDown("Double Tap")))
                TurretCentering = true;

            TurretRotation();
            CheckConsumableKeys();

#if UNITY_EDITOR
            /*if (Input.GetButtonDown("ForceRespawn"))
                MakeRespawn(forced: true, restoreLife: false, firstTime: false);*/

            if (Input.GetKeyDown(KeyCode.B))
                BattleController.EndBattle(BattleController.EndBattleCause.Timeouted);

            if (Input.GetKeyDown(KeyCode.T))
            {
                /*Landmine landmine = FindObjectOfType<Landmine>();
                if (landmine != null)
                {
                    landmine.Explode();
                }*/

            }
#endif
        }
	}

    protected virtual IEnumerator ParametersRegistration()
    {
        yield break;
    }

    protected virtual void OnDestroy()
    {
#if UNITY_EDITOR
        SceneView.onSceneGUIDelegate -= OnSceneView;
#endif

        if (data.playerId == BattleController.MyPlayerId)
        {
            BattleConnectManager.Instance.StoreValue("MyOdometer", odometer);
        }
            
        BattleConnectManager.RemovePhotonMessageTarget(gameObject);
        TankIndicators.RemoveIndicator(this);

        aimingController.Dispose();
        effectManager.Dispose();

        Unsubscriptions();
        StopCoroutine(changingVehicleMaterial);

        // Чтобы результаты бота сохранились
        if (IsBot)
        {
            Messenger.Send(EventId.OffLayerInTeamMask, new EventInfo_II(data.teamId, ownLayer));
            Messenger.Send(EventId.TankOutOfTime, new EventInfo_I(data.playerId));
        }

        PoolManager.ReleasePool(explosionFxInfo.GetResourcePath(IsMain), this);

        Messenger.Send(EventId.TankLeftTheGame, new EventInfo_I(data.playerId));
    }

    void FixedUpdate()
    {
        MovePlayer();
    }

    void LateUpdate()
    {
        data.clanName.RenewCache();
        data.country.RenewCache();
    }

    /* PUBLIC SECTION */

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

    public static bool AreClanmates(VehicleController player1, VehicleController player2)
    {
        if (player1 == null || player2 == null || player1 == player2)
            return false;

        string clan1 = player1.data.clanName;
        string clan2 = player2.data.clanName;

        return !string.IsNullOrEmpty(clan1) && clan1 == clan2;
    }

    protected abstract void MovePlayer();

    protected bool PrimaryFire()
    {
        if (!weaponController.IsReady || Stunned)
            return false;

        MarkActivity();
        weaponController.RegisterShot();

/*        if (PhotonView.isMine)
            BattleGUI.FireButtons[DefaultShellType].SimulateReloading();*/

        Transform shootPoint = GetNextShotPoint();
        
        if (!IsMain || !BattleCamera.Instance.IsZoomed)
        {
            DrawShoot(shootPoint ?? CannonEnd);
        }

        if (shootPoint == null)
        {
            shootPoint = AimingPoint;
        }

        Shell shell
            = shellPool.GetObject(shootPoint.position,
                TargetAimed && PhotonView.isMine
                                ? Quaternion.LookRotation((TargetPosition - shootPoint.position).normalized, shootPoint.up)
                                : shootPoint.rotation);

        shell.Activate(this, data.attack, HitMask, DamageSource.VehicleDoes);

        if (IsMain)
        {
            Messenger.Send(EventId.MyTankShoots, new EventInfo_I(shellId));
        }

        AudioDispatcher.PlayClipAtPosition(shotSound, shootPoint.position, Settings.SoundVolume * SoundSettings.SHOT_VOLUME);

        return true;
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
            Armor = MaxArmor;

        Messenger.Send(EventId.TankHealthChanged, new EventInfo_II(data.playerId, Armor));

        settingSpawnPosition = true;
        if (!firstTime)
            IsAvailable = true;

        correctPosition = transform.position = position;
        correctRotation = transform.rotation = rotation;

        if (Turret)
            Turret.localRotation = Quaternion.identity;

        lastTurretLocalRotationY = 0;
        storedVehiclePosition = transform.position;

        if (PhotonView.isMine)
        {
            ReloadConsumables();
        }

        Messenger.Send(EventId.VehicleRespawned, new EventInfo_I(data.playerId));

        if (IsMain)
	    {
	        Messenger.Send(EventId.MyTankRespawned, new EventInfo_SimpleEvent());

            if (respawnSound)
                AudioDispatcher.PlayClipAtPosition(
                    /* clip:     */ respawnSound,
                    /* position: */ transform.position,
                    /* parent:   */ transform);
        }
        else
        {
            indicator.Hidden = !IsAvailable;
        }
    }

    public virtual void Explode()
    {
        if (!IsAvailable)
            return;

        PoolManager.GetObject<PoolEffect>(
            explosionFxInfo.GetResourcePath(IsMain), transform.position,
            Quaternion.LookRotation((Camera.main.transform.position - transform.position).normalized, Vector3.up));

        PlayExplosionSound();

        IsAvailable = false;

        if (!PhotonView.isMine)
            return;

        weaponController.InstantReload();
    }

    public void SetMarkedStatus(bool marked)
    {
        /*if (vehicleMarker != null)
            vehicleMarker.SetMarkedStatus(marked);*/
    }

    public virtual void MakeRespawn(bool forced, bool restoreLife, bool firstTime)
	{
		SpawnPoints.SpawnData spawnPoint = SpawnPoints.instance.GetRandomPoint(this, data.teamId, forced);
        PhotonView.RPC("Respawn", PhotonTargets.All, spawnPoint.position, spawnPoint.rotation, restoreLife, firstTime);
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

    public void RequestEffect(VehicleEffectData effectData)
    {
        if (!IsAvailable)
            return;

        effectManager.RequestEffect(effectData);
    }

#if UNITY_EDITOR
    public void Cheat()
    {
        if (!Debug.isDebugBuild)
            return;

        cheatActivated = !cheatActivated;

        if (cheatActivated)
        {
            Speed *= 1.5f;
            RoF *= 3;
            maxArmorBeforeCheat = MaxArmor;
            Armor = MaxArmor = 200000;
            Messenger.Send(EventId.TankHealthChanged, new EventInfo_II(data.playerId, Armor));
            Debug.Log("*** Cheat activated ***");
        }
        else
        {
            Speed /= 1.5f;
            RoF /= 3;
            MaxArmor = maxArmorBeforeCheat;
	        Armor = (int)(MaxArmor * 0.9);
            Messenger.Send(EventId.TankHealthChanged, new EventInfo_II(data.playerId, Armor));
            Debug.Log("*** Cheat disactivated ***");
        }
    }
#endif

    public virtual int CalcDamage(int attack, bool critHit = false)
    {
        float result = attack;

        if (vehicleInfo != null && vehicleInfo.isIgnoringCritHits)
            critHit = false;

        if (data.newbie)
            result *= GameManager.NEWBIE_DAMAGE_RATIO;

        result *= Random.Range(GameManager.RANDOM_DAMAGE_RATIO_LOWER_BOUND, GameManager.RANDOM_DAMAGE_RATIO_UPPER_BOUND);

        result *= critHit ? GameData.critDamageRatio : GameData.normDamageRatio;

        return (int)result;
    }

    private void SetNavMeshObstacle()
    {
        SelfObstacle = GetComponent<UnityEngine.AI.NavMeshObstacle>();

        if (SelfObstacle != null && Bumper != null)
        {
            var col = Bumper.GetComponent<BoxCollider>();
            SelfObstacle.size = col.size;
            SelfObstacle.center = transform.InverseTransformPoint(Bumper.position);
        } 
    }

    private void Subscriptions()
    {
        Messenger.Subscribe(EventId.VehicleTakesDamage, OnVehicleTakesDamage, 2);
        Messenger.Subscribe(EventId.NowImMaster, OnIamMaster);
        Messenger.Subscribe(EventId.OffLayerInTeamMask, OnOffLayerInTeamMask);
        Messenger.Subscribe(EventId.ShellHit, OnShellHit);
        Messenger.Subscribe(EventId.BattleSettingsSubmited, OnBattleSettingsSubmited);
        Messenger.Subscribe(EventId.StartTurretRotation, OnStartTurretRotation);
        Messenger.Subscribe(EventId.StopTurretRotation, OnStopTurretRotation);
        Messenger.Subscribe(EventId.BurstFireStateChanged, OnBurstFireStateChanged);

        if (IsBot)
            Messenger.Subscribe(EventId.PhotonRoomCustomPropertiesChanged, RoomPropsChanged);
    }

    private void Unsubscriptions()
    {
        Messenger.Unsubscribe(EventId.MainTankAppeared, OnMainVehicleAppeared);
        Messenger.Unsubscribe(EventId.VehicleTakesDamage, OnVehicleTakesDamage);
        Messenger.Unsubscribe(EventId.NowImMaster, OnIamMaster);
        Messenger.Unsubscribe(EventId.PhotonRoomCustomPropertiesChanged, RoomPropsChanged);
        Messenger.Unsubscribe(EventId.NewLayerInTeamMask, OnNewLayerInTeamMask);
        Messenger.Unsubscribe(EventId.OffLayerInTeamMask, OnOffLayerInTeamMask);
        Messenger.Unsubscribe(EventId.ShellHit, OnShellHit);
        Messenger.Unsubscribe(EventId.BattleSettingsSubmited, OnBattleSettingsSubmited);
        Messenger.Unsubscribe(EventId.StartTurretRotation, OnStartTurretRotation);
        Messenger.Unsubscribe(EventId.StopTurretRotation, OnStopTurretRotation);
        Messenger.Unsubscribe(EventId.BurstFireStateChanged, OnBurstFireStateChanged);
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

    private void OnIamMaster(EventId id, EventInfo ei)
    {
        if (IsBot)
        {
            rb.isKinematic = false;
            rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
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
        Messenger.Subscribe(EventId.VehicleKilled, BotAI.OnBotBehaviourVehicleKilled);
        Messenger.Subscribe(EventId.VehicleTakesDamage, BotAI.OnBotBehaviourTakesDamage);
        Messenger.Subscribe(EventId.TankLeftTheGame, BotAI.OnBotBehaviourVehicleLeft, 4);
        Messenger.Subscribe(EventId.BonusDestroyed, BotAI.OnBotBehaviourBonusDestroyed, 4);

        StartCoroutine(BotAI.DelayAndStartBot());

        BotAI.StartBotAICoroutines();
    }

    public virtual void UpdateBotPrefabs(VehicleController nativeController) {}

    public virtual void BoundPointsToList(List<Vector3> points) { }

    private void OnVehicleTakesDamage(EventId id, EventInfo ei)
    {
        if (!PhotonView.isMine)
            return;

        EventInfo_U info = (EventInfo_U)ei;

        int victimId = (int)info[0];
        if (victimId != data.playerId)
            return;

        int damage = (int)info[1];
        damage = (int)(damage * TakenDamageRatio);
        info[1] = damage;
        int attackerId = (int)info[2];
        Armor -= damage;

        if (!IsBot)
            PhotonNetwork.player.SetCustomProperties(new Hashtable { { "hl", Armor } });
        else
            PhotonNetwork.room.SetCustomProperties(new Hashtable { { KeyForHealth, Armor } });

        if (IsAvailable && Armor <= 0)
        {
            Messenger.Send(
                id:     EventId.VehicleKilled,
                info:   new EventInfo_II(data.playerId, attackerId),
                target: Messenger.EventTargetType.ToAll);
            SetMarkedStatus(false);
        }
    }

    protected virtual void SetEngineNoise(float t)
    {
        if (IsBot || !engineAudio)
            return;

        engineAudio.pitch = Mathf.Lerp(SoundSettings.DYNAMIC_ENGINE_PITCH_MIN, SoundSettings.DYNAMIC_ENGINE_PITCH_MAX, t);
        engineAudio.volume = Settings.SoundVolume * Mathf.Lerp(SoundSettings.DYNAMIC_ENGINE_VOLUME_MIN, SoundSettings.DYNAMIC_ENGINE_VOLUME_MAX, t);
    }

    protected virtual void PlayExplosionSound()
    {
        AudioDispatcher.PlayClipAtPosition(
            explosionSound,
            Settings.SoundVolume * SoundSettings.EXPLOSION_VOLUME,
            AudioPlayer.Channel.Important,
            transform.position,
            false,
            transform);
    }

    protected virtual void ApplyAvailability()
    {
        int layer = isAvailable ? ownLayer : LayerMask.NameToLayer("ParallelWorld");
        Transform[] children = GetComponentsInChildren<Transform>(true);
        foreach (Transform child in children)
        {
            if (child != Bumper && child != GroundChecker)
                child.gameObject.layer = layer;
        }

        if (Bumper)
            Bumper.gameObject.layer =
                LayerMask.NameToLayer(Layer.Items[isAvailable ? Layer.Key.TankBumper : Layer.Key.ParallelWorld]);

        if (GroundChecker)
            GroundChecker.gameObject.layer =
                LayerMask.NameToLayer(Layer.Items[isAvailable ? Layer.Key.GroundChecker : Layer.Key.ParallelWorld]);

        isExploded = !isAvailable;

        if (SelfObstacle != null)
            SelfObstacle.enabled = isAvailable;

        rb.isKinematic = !PhotonView.isMine || !isAvailable;

        Messenger.Send(EventId.TankAvailabilityChanged, new EventInfo_II(data.playerId, isAvailable ? 1 : 0));

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

        if (OnAvailabilityChanged != null)
            OnAvailabilityChanged(isAvailable);
    }

    protected virtual void SetEngineAudio()
    {
        if (!IsMain || engineSound == null)
            return;

        engineAudio = gameObject.AddComponent<AudioSource>();

        engineAudio.clip = engineSound;
        engineAudio.loop = true;
        engineAudio.rolloffMode = AudioRolloffMode.Linear;
        engineAudio.maxDistance = 65;
        engineAudio.dopplerLevel = 0;
        engineAudio.volume = Settings.SoundVolume * SoundSettings.DYNAMIC_ENGINE_VOLUME_MIN;
        engineAudio.pitch = SoundSettings.DYNAMIC_ENGINE_PITCH_MIN;
    }

    protected virtual void SetTurretAudio()
    {
        if (!IsMain || turretRotationSound == null || GameData.IsGame(Game.Armada) || GameData.IsGame(Game.SpaceJet))
            return;

        turretAudio = gameObject.AddComponent<AudioSource>();

        turretAudio.clip = turretRotationSound;
        turretAudio.loop = true;
        turretAudio.rolloffMode = AudioRolloffMode.Linear;
        turretAudio.volume = Settings.SoundVolume * SoundSettings.TURRET_ROTATION_VOLUME;
        turretAudio.maxDistance = 25;
        turretAudio.dopplerLevel = 0;
    }

    protected void StoreVehiclePosition()
    {
        if (transform.position != storedVehiclePosition)
            odometer += Vector3.Distance(rb.position, storedVehiclePosition) * OdometerRatio;

        storedVehiclePosition = transform.position;
    }

    public void MarkActivity()
    {
        inactivityTime = 0;
    }

    /// <summary>
    /// Отменить все эффекты связанные с опред. UI_ID
    /// </summary>
    /// <param name="uiId"></param>

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
        
        Messenger.Subscribe(EventId.NewLayerInTeamMask, OnNewLayerInTeamMask);
        if (IsBot)
            Messenger.Send(EventId.NewLayerInTeamMask, new EventInfo_II(data.teamId, ownLayer));
        if (CritZones)
            CritZones.tag = Tag.Items[Tag.Key.CritZone];

        if (GameData.Mode == GameData.GameMode.Team)
            Messenger.Send(EventId.TeamChange, new EventInfo_SimpleEvent());

        if (!IsMain)
        {
            tag = Tag.Items[IsMainsFriend ? Tag.Key.Friend : Tag.Key.Enemy];
        }
        StartCoroutine(ResetMasks_Coroutine());
    }

    private void FillVehicleData()
    {
        BattleController.Instance.OnPlayerPropertiesChanged(player.ID, player.CustomProperties);
    }

    private void CalculateMaxSpeed()
    {
        maxSpeed = data.speed / SpeedRatio;
    }

	protected virtual void TakeExperienceBonus(int amount)
	{
		if (PhotonView.isMine)
		{
            ScoreCounter.ScoreInto(this, amount);
            if (IsMain)
                Messenger.Send(EventId.ExperienceAcquired, new EventInfo_I(amount));
        }
    }

	protected virtual void TakeGoldBonus(int amount)
	{
		if (IsMain)
			Messenger.Send(EventId.GoldAcquired, new EventInfo_I(amount));
	}

	protected virtual void TakeSilverBonus(int amount)
	{
		if (IsMain)
			Messenger.Send(EventId.SilverAcquired, new EventInfo_I(amount));
    }

	protected virtual void TakeHealthBonus()
	{
		if (!PhotonView.isMine)
			return;

        RequestEffect(new VehicleEffectData(VehicleEffect.ParameterType.Armor, VehicleEffect.ModifierType.Product, 1f, -1, -1));
	}

	protected virtual void TakeFuelBonus()
	{
		if (IsMain)
			Messenger.Send(EventId.FuelAcquired, new EventInfo_I(1));
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
		if (Stunned || !Turret)
			return;

        float deltaForRotation = 0;

        float currentTurretRotationY = Turret.localEulerAngles.y;
        bool turretIdle = Mathf.Abs(lastTurretLocalRotationY - currentTurretRotationY) < 0.1f;

        if (turretIdle && !isTurretIdleFrameBefore)
        {
            Messenger.Send(EventId.StopTurretRotation, new EventInfo_I(data.playerId));
        }
        else if (!turretIdle && isTurretIdleFrameBefore)
        {
            Messenger.Send(EventId.StartTurretRotation, new EventInfo_I(data.playerId));
        }

        isTurretIdleFrameBefore = turretIdle;

        lastTurretLocalRotationY = Turret.localEulerAngles.y;

        if (!HelpTools.Approximately(TurretAxisControl, 0))
        {
            deltaForRotation = TurretAxisControl;
            TurretCentering = false;
        }
        else
        {
            // Джойстик поворота башни в спокойном положении
            if (TurretCentering)
            {
                if (Mathf.Abs(Turret.localEulerAngles.y) < 0.01f)
                {
                    TurretCentering = false;
                    return;
                }

                deltaForRotation = Mathf.Clamp(Mathf.DeltaAngle(Turret.localEulerAngles.y, 0), -1, 1);
            }
            else if (BattleSettings.Instance.FixateTurretDirection)
            {
                Vector3 up = Turret.up;
                Vector3 dir = Vector3.ProjectOnPlane(fixatedTurretDir, up).normalized;
                if (dir != Vector3.zero)
                    Turret.rotation = Quaternion.LookRotation(dir, up);
                return;
            }
        }

        fixatedTurretDir = Turret.forward;

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

        if (TurretCentering && Mathf.Abs(realRotation) > Mathf.Abs(Mathf.DeltaAngle(Turret.localEulerAngles.y, 0f)))
        {
            TurretCentering = false;
            Turret.localEulerAngles = Vector3.zero;
        }
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
        if (Stunned)
            return false;

        BattleConsumable consumable;
        return consumables.TryGetValue(consumableId, out consumable) && consumable.Use();
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

        KeyForDeaths = isBot ? string.Format("bt{0}dt", data.playerId) : "dt";
        KeyForKills = isBot ? string.Format("bt{0}kl", data.playerId) : "kl";
        KeyForScore = isBot ? string.Format("bt{0}sc", data.playerId) : "sc";
        KeyForHealth = isBot ? string.Format("bt{0}hl", data.playerId) : "hl";
        KeyForExistance = isBot ? string.Format("bt{0}ex", data.playerId) : "ex";

        KeyForAttack = isBot ? string.Format("bt{0}at", data.playerId) : "at";
        KeyForRoF = isBot ? string.Format("bt{0}rt", data.playerId) : "rt";
        KeyForSpeed = isBot ? string.Format("bt{0}sp", data.playerId) : "sp";
        KeyForMaxArmor = isBot ? string.Format("bt{0}ma", data.playerId) : "ma";
        KeyForShield = isBot ? string.Format("bt{0}sh", data.playerId) : "sh";
        KeyForDamageRatio = isBot ? string.Format("bt{0}dr", data.playerId) : "dr";
        KeyForRegen = isBot ? string.Format("bt{0}rg", data.playerId) : "rg";
        KeyForEffect = isBot ? string.Format("bt{0}ef", data.playerId) : "ef";
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
            HitMask = MiscTools.ExcludeLayersFromMask(BattleController.HitMask, "Player");
        }
        else
        {
            HitMask = IsBot ? MiscTools.ExcludeLayerFromMask(BattleController.HitMask, ownLayer) : BattleController.HitMask;
        }

        othersLayerMask = MiscTools.GetLayerMask(Layer.Key.Enemy, Layer.Key.Friend, Layer.Key.Player) |
                          BotDispatcher.BotsCommonMask;
        othersLayerMask = MiscTools.ExcludeLayerFromMask(othersLayerMask, ownLayer);
    }

    private void RoomPropsChanged(EventId eid, EventInfo ei)
    {
        CheckAllBotStats();
        StartCoroutine(ForgetPropsChanged());
    }

    private IEnumerator ForgetPropsChanged()
    {
        yield return null;
        Messenger.Unsubscribe(EventId.PhotonRoomCustomPropertiesChanged, RoomPropsChanged);
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

    private void OnNewLayerInTeamMask(EventId eid, EventInfo ei)
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

    private void OnOffLayerInTeamMask(EventId eid, EventInfo ei)
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

    private void OnShellHit(EventId eid, EventInfo ei)
    {
        EventInfo_IIIIV info = (EventInfo_IIIIV)ei;

        if (info.int1 != data.playerId)
            return;

        Vector3 position = transform.TransformPoint(info.vector);
        int damage = info.int2;

        Messenger.Send(
            id: EventId.VehicleTakesDamage,
            info: new EventInfo_U(
                        info.int1,
                        damage,
                        info.int3,
                        info.int4,
                        position));

        if (!PhotonView.isMine)
            return;

        Hashtable props = new Hashtable { { KeyForHealth, Armor } };

        if (IsBot)
        {
            PhotonNetwork.room.SetCustomProperties(props);
        }
        else
        {
            Player.SetCustomProperties(props);
        }
    }

    private void OnBattleSettingsSubmited(EventId eid, EventInfo ei)
    {
        if (GameData.IsGame(Game.Armada)) // У Армады отдельный контроллер для звуков.
            return;

        if (IsMain)
            turretAudio.volume = Settings.SoundVolume * SoundSettings.TURRET_ROTATION_VOLUME;
    }

    private void OnStopTurretRotation(EventId eid, EventInfo ei)
    {
        if (BattleSettings.Instance.FixateTurretDirection)
            return;

        EventInfo_I info = (EventInfo_I)ei;

        int playerId = info.int1;

        if (playerId != data.playerId || !IsMain)
            return;

        if (turretAudio.isPlaying)
            turretAudio.Stop();
    }

    private void OnStartTurretRotation(EventId eid, EventInfo ei)
    {
        if (BattleSettings.Instance.FixateTurretDirection)
            return;

        EventInfo_I info = (EventInfo_I)ei;

        int playerId = info.int1;

        if (playerId != data.playerId || !IsMain)
            return;

        if (!turretAudio.isPlaying)
            turretAudio.Play();
    }
    
    private void FindCritZonePlace()
    {
        List<Collider> boundColliderList = new List<Collider>(8);
        if (Body != null)
            boundColliderList.AddRange(Body.GetComponentsInChildren<Collider>());
        if (Turret != null)
            boundColliderList.AddRange(Turret.GetComponentsInChildren<Collider>());

        boundColliderList.RemoveAll(
            col =>
                col.isTrigger
                || !MiscTools.CheckIfLayerInMask(BattleController.CommonVehicleMask, col.gameObject.layer));

        CritZonePlace = BoundsVertZone.None;
        foreach (Collider col in boundColliderList)
        {
            if (col.gameObject.CompareTag("CritZone"))
            {
                CritZonePlace = DetermineColliderPlace(col);
                return;
            }
        }
    }

    private BoundsVertZone DetermineColliderPlace(Collider collider)
    {
        Bounds entireBounds = EntireBounds;
        Bounds colBounds = collider.bounds;
        if (colBounds.min.y > entireBounds.center.y)
            return BoundsVertZone.Top;

        if (colBounds.max.y < entireBounds.center.y)
            return BoundsVertZone.Bottom;

        return BoundsVertZone.Center;
    }

    private void InitConsumables(Dictionary<int, ObscuredInt> consumableDict)
    {
        string mapId = ((GameManager.MapId)(int) PhotonNetwork.room.CustomProperties["mp"]).ToString();
        foreach (var consPair in consumableDict)
        {
            consumables[consPair.Key] = new BattleConsumable(this, GameData.consumableInfos[consPair.Key], mapId);
        }
    }

    protected bool UseConsumableByIndex(int index)
    {
        if (index < 0 || index >= ConsumablesInventory.battleInventoryList.Count)
        {
            return false;
        }
        
        if (!consumables.ContainsKey(ConsumablesInventory.battleInventoryList[index]))
        {
            return false;
        }

        return consumables[ConsumablesInventory.battleInventoryList[index]].Use();
    }

    private void ReloadConsumables()
    {
        foreach (BattleConsumable consumable in consumables.Values)
        {
            consumable.Reload();
        }
    }

    private void DrawShoot(Transform effectPoint)
    {
        try
        {
            PoolManager.GetObject<PoolEffect>(
                shotFXInfo.GetResourcePath(IsMain), effectPoint.position,
                effectPoint.rotation);
        }
        catch (Exception)
        {
            Debug.LogErrorFormat(gameObject, "NO SHOOT in {0} : {1}", name, shotFXInfo.GetResourcePath(IsMain), effectPoint.position,
                effectPoint.rotation);
        }
    }

    private Transform GetNextShotPoint()
    {
        if (shootEffectPoints == null || shootEffectPoints.Length == 0)
            return null;

        currentEffectPointInd = (currentEffectPointInd + 1) % shootEffectPoints.Length;
        int i = currentEffectPointInd;
        Transform shootPoint = shootEffectPoints[i];

        do
        {
            if (
                !IsMain
                || !TargetAimed
                ||!Physics.Linecast(shootEffectPoints[i].position, TargetPosition, BattleController.ObstacleMask))
            {
                shootPoint = shootEffectPoints[i];
                currentEffectPointInd = i;
                break;
            }
            i = (i + 1) % shootEffectPoints.Length;
        }
        while (i != currentEffectPointInd);
        
        return shootPoint;
    }

    private void CheckConsumableKeys()
    {
        if (XDevs.Input.GetButtonDown("Use Consumable 1"))
        {
            UseConsumableByIndex(0);
        }

        if (XDevs.Input.GetButtonDown("Use Consumable 2"))
        {
            UseConsumableByIndex(1);
        }

        if (XDevs.Input.GetButtonDown("Use Consumable 3"))
        {
            UseConsumableByIndex(2);
        }
    }

    private void CheckBurst()
    {
        if (Stunned)
            return;

        if (PhotonView.isMine)
        {
            if (IsRequirePrimaryFire && weaponController.IsReady)
            {
                if (!burst)
                {
                    Messenger.Send(
                        EventId.BurstFireStateChanged,
                        new EventInfo_IB(data.playerId, true),
                        Messenger.EventTargetType.ToAll);
                }
            }
            else
            {
                if (burst)
                    Messenger.Send(
                        EventId.BurstFireStateChanged,
                        new EventInfo_IB(data.playerId, false),
                        Messenger.EventTargetType.ToAll);
            }
        }

        if (burst)
        {
            PrimaryFire();
        }
    }

    private void OnBurstFireStateChanged(EventId eid, EventInfo ei)
    {
        EventInfo_IB info = (EventInfo_IB)ei;
        if (info.int1 != data.playerId)
            return;

        burst = info.bool1;
    }

    private IEnumerator LoadCrashModelRoutine()
    {
        if (string.IsNullOrEmpty(crashModelResource))
            yield break;

        ResourceRequest resRequest = Resources.LoadAsync<VehicleCrashModel>(string.Format("{0}/CrashModels/{1}", GameManager.CurrentResourcesFolder, crashModelResource));
        yield return resRequest;

        VehicleCrashModel crashModel = (VehicleCrashModel) resRequest.asset;
        crashModel = Instantiate(crashModel);
        crashModel.Init(data.playerId);
    }

    private void SetupComponents()
    {
        PhotonView = GetComponent<PhotonView>();
        entireCollider = Bumper.GetComponent<Collider>();

        #region TankData
        if (BattleController.vehicleData.ContainsKey(PhotonView.ownerId))
        {
            data = BattleController.vehicleData[PhotonView.ownerId];
        }
        else
        {
            data = (VehicleData)PhotonView.instantiationData[0];
            data.playerId = IsBot ? data.playerId : PhotonView.ownerId;
        }
        #endregion

        vehicleInfo = VehiclePool.Instance.GetItemById(id); // TODO: убрать после переноса id танка в TankData.

        #region Rigidbody
        rb = GetComponent<Rigidbody>();
        rb.centerOfMass = centerOfMass;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.isKinematic = !PhotonView.isMine;
        rb.collisionDetectionMode = PhotonView.isMine ? CollisionDetectionMode.Continuous : CollisionDetectionMode.Discrete;
        #endregion

        #region BodykitController
        bodykitController = GetComponent<BodykitController>();
        if (data.patternId != 0)
            bodykitController.DrawCamouflage(PatternPool.Instance.GetItemById(data.patternId), vehicleInfo.id);

        if (data.decalId != 0)
            bodykitController.DrawDecal(DecalPool.Instance.GetItemById(data.decalId));

        bodykitController.SetShadowPlane();
        #endregion

        #region Visible optimizations
        if (!IsMain)
        {
            ShadowPlane shadowPlane = GetComponentInChildren<ShadowPlane>(true);
            if (shadowPlane != null)
                shadowPlane.EngageOptimization();
        }
        #endregion

        weaponController = new SingleShotReloader(this);
        aimingController = new AimingController(this, IsBot ? 0.14f : 0f, IsBot ? (float)MiscTools.random.NextDouble(): 0f);

        renderers = GetComponentsInChildren<Renderer>(true);
        animator = gameObject.GetComponent<Animator>();
        //vehicleMarker = GetComponentInChildren<VehicleMarker>();
        
        SetNavMeshObstacle();
    }

    private void InitProperties()
    {
        MaxShootAngleCos = Mathf.Cos(Mathf.Deg2Rad * MaxShootAngle);
        player = PhotonView.owner;

        effectManager = new VehicleEffectManager(this);
        shellPath = GameSettings.Instance.GetShellInfo(shellId).GetResourcePath(IsMain);
        DoubleExperience = !IsBot && ProfileInfo.doubleExpVehicles.Contains(ProfileInfo.currentVehicle);

        object lastOdometer;
        if (BattleConnectManager.Instance.GetStoredValue("MyOdometer", out lastOdometer))
            odometer = (ObscuredFloat)lastOdometer;

        correctPosition = transform.position;
        correctRotation = transform.rotation;
        indicatorDelta = IndicatorPoint ? IndicatorPoint.localPosition : IndicatorDeltaOffset;

        #region Own battle statistics
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
                    countryCode: data.hideMyFlag ? string.Empty : (string)data.country,
                    vip: data.vip,
                    profileId: data.profileId,
                    clanName: data.clanName);
        #endregion

        SetPropertyKeys();
    }

    private void StartCoroutines()
    {
        StartCoroutine(LoadCrashModelRoutine()); // TODO: Demetri Перенести на функционал пула
        changingVehicleMaterial = QualityManager.Instance.ChangeObjectMaterials(gameObject);
        QualityManager.Instance.StartCoroutine(changingVehicleMaterial);
    }

    private void AnotherInitializations()
    {
        shellPool = PoolManager.GetPool<Shell>(shellPath);
        fixatedTurretDir = Turret.forward;
        string explosionFXPath = explosionFxInfo.GetResourcePath(IsMain);
        PoolManager.GetPoolAsync<PoolEffect>(explosionFXPath);
        PoolManager.MarkPoolAsCustom(explosionFXPath, this);
    }

    public void TakeDamage(int potentialDamage, IDamageInflicter damageInflicter, Vector3 position)
    {
        if (!damageInflicter.IsLocal)
            return;

        int attackerId = damageInflicter.OwnerId;
        VehicleController attacker;
        if (!BattleController.allVehicles.TryGetValue(attackerId, out attacker)
            || AreFriends(this, attacker))
        {
            return;
        }

        int damage = CalcDamage(potentialDamage);
        Vector3 localPos = transform.InverseTransformPoint(position);
        Messenger.Send(EventId.PortionOfDamage,
                new EventInfo_IIIIV(
                    data.playerId,
                    damage,
                    attackerId,
                    (int)damageInflicter.DamageSource,
                    localPos
                    ));

        if (!PhotonView.isMine)
        {
            Armor -= (int)(damage * TakenDamageRatio);
            if (Armor > 0)
            {
                Messenger.Send(
                    id: EventId.VehicleTakesDamage,
                    info: new EventInfo_U(
                        data.playerId,
                        damage,
                        attackerId,
                        (int)damageInflicter.DamageSource,
                        localPos));
            }
            Messenger.Send(EventId.TankHealthChanged, new EventInfo_II(data.playerId, Armor));
        }
    }

    #region IDamageable
    public bool Solid { get { return true; } }

    public Bounds Bounds { get { return EntireBounds; } }

    public int Health { get { return data.armor; } }
    #endregion
}