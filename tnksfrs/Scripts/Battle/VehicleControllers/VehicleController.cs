using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using CodeStage.AntiCheat.ObscuredTypes;
using Disconnect;
using XDevs.LiteralKeys;
using XD;
using Hashtable = ExitGames.Client.Photon.Hashtable;
using Random = UnityEngine.Random;
using XD.CONSOLE;

public abstract class VehicleController : MonoBehaviour, IVehicleLogic, IUnitBehaviour, IUnitIndicator, ICollider, IPunObservable
{
    [SerializeField]
    protected int                                       frameDifferentcial = 3;

    [Header("Данные")]
    public ObscuredInt                                  id; // TODO: убрать костыльное поле в TankData после поднятия версии комнаты фотона.
    public ObscuredInt                                  tankGroup;
    public TankData                                     data = null;
    [SerializeField]
    private Transform[]                                 transforms = null;
    [Header("Ссылки")]
    public Transform                                    lookPoint = null;
    public Transform                                    forCam = null;

    [Header("Звуки")]
    public AudioClip                                    turretRotationSound;

    [Header("Физика")]
    [SerializeField]
    public Transform                                    COM = null;

    private IHPSystem                                   hpSystem = null;
    private ISight                                      aimer = null;
    private IAimingBehaviour                            aimingBehaviour = null;

    [Header("Прочее")]
    public bool                                         continuousFire;

    public float                                        shotCorrection = 0.3f;
    public float                                        turretRotationSpeedQualifier = 0.03f;

    [SerializeField]
    private float                                       cannonRotationQualifier = 0.5f;

    public float                                        rotationSpeedQualifier = 1.2f;
    public new Rigidbody                                rigidbody = null;

    public Dictionary<GunShellInfo.ShellType, Weapon>   weapons = null;

    [Space]
    [Header("Установленные расходники")]
    [SerializeField]
    private Consumables                                 installedBattleConsumables = null;

    [Header("TEST/DEBUG")]
    public Weapon                                       mainWeapon = null;
    [SerializeField]
    private SettingsContainer                           settingsContainer = null;

    private IUnitBattle                                 unitBattle = null;

    protected const float                               DEFAULT_SOUND_DISTANCE = 65.0f;

    protected float                                     odometer = 0;

    [SerializeField]
    protected float                                     correctTurretAngle = 0;
    [SerializeField]
    protected float                                     cannonSpeed = 50;
    [SerializeField]
    protected Vector3                                   correctPosition;
    [SerializeField]
    protected Vector3                                   correctVelocity;
    [SerializeField]
    protected Quaternion                                correctRotation;
    [SerializeField]
    protected bool                                      isExploded = false;

    [SerializeField]
    protected Transform                                 lowQualityCollider = null;
    [SerializeField]
    protected Transform                                 bumper = null;
    [SerializeField]
    protected Transform                                 indicatorPoint = null;
    [SerializeField]
    protected Transform                                 shotPoint = null;
    [SerializeField]
    protected Transform                                 turret = null;
    [SerializeField]
    protected Transform                                 cannon = null;
    [SerializeField]
    protected Transform                                 cannonEnd = null;
    [SerializeField]
    protected Transform                                 body = null;
    [SerializeField]
    protected Clamper                                   verticalAngles = new Clamper(-60, 0, 15);
    [SerializeField]
    protected LayerMask                                 checkObstacleMask = 0;
    [SerializeField]
    protected float                                     turnSpeedRatio = 0.1f;
    [SerializeField]
    protected float                                     moveSpeedKoefficient = 0.1f;
    [SerializeField]
    protected LayerMask                                 hitMask = 0;
    [SerializeField]
    private Bounds                                      bodyMeshBounds = new Bounds();
    [SerializeField]
    private bool                                        boundsInited = false;
    [SerializeField]
    private MaterialsContainer                          container = null;
    [SerializeField]
    private BodykitController                           bodykitController = null;
    [SerializeField]
    private VehicleMarker                               vehicleMarker = null;

    protected Vector3                                   storedVehiclePosition;        
    protected AudioSource                               turretAudio;
    protected AudioSource                               engineAudio;
    protected PhotonPlayer                              player;

    protected GunShellInfo                              primaryShellInfo;
    protected GunShellInfo                              secondaryShellInfo;

    protected double                                    kickBotAt; // Когда выгнать из комнаты, если бот

    protected bool                                      isAvailable = false;
    protected bool                                      burst = false;
    protected bool                                      isTurretIdleFrameBefore = false;
    protected bool                                      fireButtonPressed = false;
    protected bool                                      settingSpawnPosition = true;
    protected bool                                      backInverse = false;
    
    protected int                                       ownLayer; // Слой своего vehicle 
    protected int                                       turretTouchId = -1;

    protected float                                     lastTurretLocalRotationY;
    protected float                                     lastTouchTurretRotation = 0f;
    protected float                                     lastTouchTurretRotationSpeed = 0f;
    protected float                                     currentCorrection = 0f;
    protected float                                     angleXDelta = 0;

    protected Vector3                                   velocity = new Vector3();

    protected LayerMask                                 othersLayerMask;
    protected static int                                terrainLayer;
    protected int                                       myFrame = -1;
    protected int                                       normalFrame = 0;
    protected int                                       physicFrame = 0;
    protected float                                     deltaTime = 0;

    public IEngineJet                                   drawEngineEff = new NoEngineJet();

    private ISoundSettings[]                            soundsSettings = null;
    private IEffectsSettings[]                          effectsSettings = null;

    private const GunShellInfo.ShellType                DEFAULT_SHELL_TYPE = GunShellInfo.ShellType.Usual;
    private static readonly ObscuredFloat               DEFAULT_ROCKET_FIRE_RATE = 6.0f;
    
    private bool                                        targetAimed = false;
    private bool                                        existanceSynchronized;
    private bool                                        visible;
    private bool                                        slowpokeActivated;
    private bool                                        isMarked = false;
    private bool                                        inWater = false;
    private bool                                        cruiseControl = false;

    private int                                         aimRefreshFrameCounter = 0;

    private float                                       lastTime = 0;
    private float                                       inactivityTime;
    private float                                       xAxis = 0;
    private float                                       yAxis = 0;
    private float                                       angleYDelta = 0;

    private Clamper                                     waterTimer = new Clamper(5);
    private Clamper                                     aimingTimer = null;

    private PlayerStat                                  statistics = null;
    private IEnumerator                                 changingVehicleMaterial = null;
    private Coroutine                                   waterRoutine = null;
    private ICritZoneController                         critZoneController = null;
    private BuffSimple                                  debuffWater = null;

    public new Transform                                transform = null;

    private List<Buff>                                  activeBuffs = null;
    private List<Buff>                                  activeDebuffs = null;
    private List<Setting>                               blockedBuffs = null;
    private List<Setting>                               blockedDebuffs = null;

    private Vector3                                     cannonLocalEulerAngles = new Vector3();
    private Vector3                                     targetPoint = new Vector3();
    private Vector3                                     turretLocalEulerAngles = new Vector3();

    private bool CruiseControl
    {
        get
        {
            return cruiseControl;
        }

        set
        {            
            cruiseControl = value;
            Event(Message.CruiseControleStateChanged, cruiseControl);
        }
    }

    private MaterialsContainer Container
    {
        get
        {
            if (container == null)
            {
                container = GetComponent<MaterialsContainer>();

                if (container == null)
                {
                    container = gameObject.AddComponent<MaterialsContainer>();
                }
            }

            return container;
        }
    }

    public Transform LowQualityCollider
    {
        get
        {
            return lowQualityCollider;
        }
    }

    public Clamper VerticalAngles
    {
        get
        {
            return verticalAngles;
        }
    }

    protected virtual ICritZoneController CritZoneController
    {
        get
        {
            if (critZoneController == null)
            {
                critZoneController = GetComponent<ICritZoneController>();
            }
            return critZoneController;
        }
    }

    public bool IsDetected
    {
        get;
        set;
    }

    public IUnitIndicator Indicator
    {
        get
        {
            return this;
        }
    }

    public IAimingBehaviour AimingBehaviour
    {
        get
        {
            if (aimingBehaviour == null)
            {
                aimingBehaviour = GetComponent<IAimingBehaviour>();
                
                if (aimingBehaviour == null)
                {
                    aimingBehaviour = gameObject.AddComponent<AimingBehaviour>();
                }

                aimingBehaviour.Init(this);
            }

            return aimingBehaviour;
        }
    }

    public Consumables InstalledBattleConsumables
    {
        get
        {
            if (installedBattleConsumables == null)
            {
                installedBattleConsumables = new Consumables();
            }

            return installedBattleConsumables;
        }

        private set
        {
            installedBattleConsumables = value;
        }
    }

    public List<Buff> ActiveBuffs
    {
        get
        {
            if (activeBuffs == null)
            {
                activeBuffs = new List<Buff>();
            }

            return activeBuffs;
        }
    }

    public List<Buff> ActiveDebuffs
    {
        get
        {
            if (activeDebuffs == null)
            {
                activeDebuffs = new List<Buff>();
            }

            return activeDebuffs;
        }
    }

    public List<Setting> BlockedBuffs
    {
        get
        {
            if (blockedBuffs == null)
            {
                blockedBuffs = new List<Setting>();
            }

            return blockedBuffs;
        }
    }

    public List<Setting> BlockedDebuffs
    {
        get
        {
            if (blockedDebuffs == null)
            {
                blockedDebuffs = new List<Setting>();
            }

            return blockedDebuffs;
        }
    }

    public int ID
    {
        get
        {
            return id;
        }
    }

    public string Name
    {
        get
        {
            return name;
        }
    }

    public int ViewID
    {
        get
        {
            return PhotonView.viewID;
        }
    }

    public TankData Data
    {
        get
        {
            return data;
        }
    }

    #region IUnitIndicator
    public string Nick
    {
        get
        {
            return data.playerName;
        }
    }

    public float Percent
    {
        get
        {
            return Settings[Setting.HP].Percent;
        }
    }

    public bool IsMarked
    {
        get
        {
            return isMarked;
        }
    }

    public Vector3 Position
    {
        get
        {
            return transform == null ? Vector3.zero : transform.position;
        }
    }

    public bool IsHidden
    {
        get
        {
            return !IsAvailable;
        }
    }

    public bool IsAlly
    {
        get
        {
            if (!StaticContainer.Profile.BattleTutorialCompleted)
            {
                return !IsBot;
            }

            return data.Team == StaticContainer.GameManager.Team;
        }
    }

    public UnitClass Class
    {
        get
        {
            return unitBattle.UnitClass;
        }
    }

    public string ModelTank
    {
        get
        {
            return UnitBattle.Name;
        }
    }

    public int Level
    {
        get
        {
            return UnitBattle.Level;
        }
    }

    public TypeMapIndicator CurrentTypeMapIndicator
    {
        get
        {
            return IsMine ? TypeMapIndicator.MineUnit : TypeMapIndicator.AnyUnit;
        }
    }
    #endregion

    public virtual float Gravity
    {
        get
        {
            return 0;
        }

        set
        {
        }
    }

    public Vector3 CenterOfMass
    {
        get
        {
            if (COM == null)
            {
                COM = new GameObject("COM").transform;
                COM.SetParent(transform, false);
                COM.localPosition = new Vector3(0, -0.3f, 0);
            }

            return COM.localPosition;
        }

        set
        {
            if (COM == null)
            {
                COM = new GameObject("COM").transform;
                COM.SetParent(transform, false);
            }

            COM.localPosition = value;
        }
    }

    public IUnitWeapon Weapon
    {
        get
        {
            return mainWeapon;
        }
    }

    public IUnitBattle UnitBattle
    {
        get
        {
            if (unitBattle == null)
            {
                unitBattle = StaticContainer.MainData.GetUnitBattle(Data.unitId);
            }

            return unitBattle;
        }

        set
        {
            unitBattle = value;
            InitSettings();
        }
    }

    public LayerMask CheckObstacleMask
    {
        get
        {
            return checkObstacleMask;
        }
    }

    public int HitMask
    {
        get
        {
            return hitMask;
        }
    }

    public XD.Settings Settings
    {
        get
        {
            return settingsContainer.Settings;
        }

        set
        {
            if (settingsContainer == null)
            {
                InitSettingsContainer();
            }

            settingsContainer.Settings = value;
        }
    }

    public float TurretRotationZoomSpeedQualifier
    {
        get 
        {
            return TargetAimed ? Mathf.Clamp(StaticContainer.MainCamera.TurretIndicationZoomSqrDist / Vector3.SqrMagnitude(StaticContainer.BattleController.TargetPosition - transform.position), 0.2f, 1) * turretRotationSpeedQualifier : turretRotationSpeedQualifier * 0.5f;
        }
    }
    
    protected virtual float CannonRotationSpeedQualifier
    {
        get
        {
            return cannonRotationQualifier;
        }
    }
   
    public float TurretRotationSpeedQualifier
    {
        get 
        { 
            return turretRotationSpeedQualifier; 
        }
    }

	public void ChangePlayEngineEffBehaviour(IEngineJet ee)
    {
        drawEngineEff = ee;
    }
    

    public UnityEngine.AI.NavMeshObstacle SelfObstacle
    {
        get; protected set;
    }

    public float LastTurretLocalRotationY
    {
        get
        {
            return lastTurretLocalRotationY;
        }
    }

    public bool IsTurretIdleFrameBefore
    {
        get
        {
            return isTurretIdleFrameBefore;
        }
    }

    public Vector3 CorrectPosition
    {
        get
        {
            return correctPosition;
        }
        set
        {
            correctPosition = value;
        }
    }

    public Vector3 CorrectVelocity
    {
        get
        {
            return correctVelocity;
        }
        set
        {
            correctVelocity = value;
        }
    }

    public Quaternion CorrectRotation
    {
        get
        {
            return correctRotation;
        }
        set
        {
            correctRotation = value;
        }
    }

    public float CorrectCannonAngle
    {
        get;
        set;
    }

    public float CorrectTurretAngle
    {
        get
        {
            return correctTurretAngle;
        }
        set
        {
            correctTurretAngle = value;
        }
    }

    public float CurrentCorrection
    {
        get
        {
            return currentCorrection;
        }
        set
        {
            currentCorrection = value;
        }
    }

    public bool SettingSpawnPosition
    {
        get
        {
            return settingSpawnPosition;
        }
        set
        {
            settingSpawnPosition = value;
        }
    }

    public int TerrainLayer
    {
        get
        {
            return terrainLayer;
        }
    }

    public int OthersLayerMask
    {
        get
        {
            return othersLayerMask;
        }
    }

    public int OwnLayer
    {
        get
        {
            return ownLayer;
        }
    }

    public double KickBotAt
    {
        get
        {
            return kickBotAt;
        }
    }

    public bool Burst
    {
        get
        {
            return burst;
        }
    }

    public ISight Aimer
    {
        get
        {
            if (aimer == null)
            {
                aimer = GetComponent<ISight>();
                if (aimer == null)
                {
                    aimer = gameObject.AddComponent<Aimer>();
                }
            }

            return aimer;
        }
    }    

    public string KeyForBotHealth
    {
        get; private set;
    }

    public string KeyForBotAttack
    {
        get; private set;
    }

    public string KeyForBotRoF
    {
        get; private set;
    }

    public string KeyForBotSpeed
    {
        get; private set;
    }

    public string KeyForBotTurretSpeed
    {
        get; private set;
    }

    public string KeyForBotScore
    {
        get; private set;
    }

    public string KeyForBotKills
    {
        get; private set;
    }

    public string KeyForBotDamage
    {
        get; private set;
    }

    public string KeyForBotDeaths
    {
        get; private set;
    }

    public string KeyForBotExistance
    {
        get; private set;
    }

    public PhotonView PhotonView
    {
        get; protected set;
    }

    public AimPointInfo AimPoint
    {
        get
        {
            return AimingBehaviour.AimPoint;
        }

        set
        {
            AimingBehaviour.AimPoint = value;
        }
    }

    public Rigidbody Rb
    {
        get
        {
            return rigidbody;
        }
    }

    public Bounds BodyMeshBounds
    {
        get
        {
            return bodyMeshBounds;
        }
    }

    public virtual bool IsCrashing
    {
        get
        {
            return false;
        }

        set
        {
        }
    }

    public virtual float WeaponReloadingProgress
    {
        get
        {
            return mainWeapon.ReloadingProgress;
            //return weapons[DEFAULT_SHELL_TYPE].ReloadingProgress;
        }
    }

    public virtual GunShellInfo.ShellType DefaultShellType
    {
        get
        {
            return DEFAULT_SHELL_TYPE;
        }
    }

    public virtual Transform Turret
    {
        get
        {
            return turret;
        }

        set
        {
            turret = value;
        }
    }

    public virtual Transform ShotPoint
    {
        get
        {            
            return shotPoint;
        }

        set
        {
            shotPoint = value;
        }
    }

    public virtual Transform CannonEnd
    {
        get
        {
            return cannonEnd;
        }
    }

    public virtual Transform Cannon
    {
        get
        {
            return cannon;
        }

        set
        {
            cannon = value;
        }
    }

    public virtual Vector3 TargetPoint
    {
        get
        {
            return transform.position;
        }
    }

    public virtual Vector3 Velocity
    {
        get
        {
            return velocity;
        }
    }

    public virtual Vector3 AngularVelocity
    {
        get
        {
            return ((PhotonView == null || PhotonView.isMine) && rigidbody != null) ? rigidbody.angularVelocity : Vector3.zero;
        }
    }

    public bool IsBot
    {
        get
        {
            return PhotonView == null || PhotonView.isSceneView;
        }
    }    

    public bool IsInParallelWorld
    {
        get
        {
            return gameObject.layer == LayerMask.NameToLayer(Layer.Items[Layer.Key.ParallelWorld]);
        }

        set
        {
            int layer = value ? LayerMask.NameToLayer("ParallelWorld") : ownLayer;

            LayerMask ignoreMask = 1 << LayerMask.NameToLayer("ArmorZone");

            for (int i = 0; i < transforms.Length; i++)
            {
                if (isAvailable && ignoreMask.Contains(transforms[i].gameObject.layer))
                {
                    continue;
                }

                transforms[i].gameObject.layer = layer;
            }

            //GameObject.SetLayer(layer, ignoreMask);

            if (Bumper != null)
            {
                Bumper.gameObject.layer = isAvailable ? LayerMask.NameToLayer("Bumper") : LayerMask.NameToLayer("ParallelWorld");
            }

            if (IsMine)
            {
                if (Body != null)
                {
                    Body.gameObject.layer = isAvailable ? LayerMask.NameToLayer("MainCollider") : LayerMask.NameToLayer("ParallelWorld");
                }
            }
        }
    }

    public int VehicleGroup
    {
        get
        {
            return tankGroup;
        }
    }

    public bool IsAvailable
    {
        get
        {
            return isAvailable;
        }

        set
        {
            isAvailable = value;
            if (!value)
            {
                name += "[Dead]";
            }

            ApplyAvailability();
        }
    }

    public bool IsExploded
    {
        get
        {
            return isExploded;
        }
    }

    public bool IsMine
    {
        get
        {
            return (PhotonNetwork.connected && (PhotonView == null || PhotonView.isMine) || (data.playerId == StaticType.BattleController.Instance<IBattleController>().MyPlayerId)) && !IsBot;
        }
    }

    public bool IsMainsFriend
    {
        get
        {
            if (IsMine)
            {
                return false;
            }

            if (StaticContainer.BattleController.CurrentUnit == null)
            {
                return false;
            }

            return false;
            //return AreFriends(StaticContainer.BattleController.CurrentUnit, this);
        }
    }

    public int ExperienceBonus
    {
        get
        {
            return MiscTools.Round((int)(data.maxArmor / 10.5), 5);
        }
    }

    public IHPSystem HPSystem
    {
        get
        {
            if (hpSystem == null)
            {
                hpSystem = GetComponent<IHPSystem>();

                if (hpSystem == null)
                {
                    hpSystem = gameObject.AddComponent<HitPointsSystem>();
                }
            }

            return hpSystem;
        }
    }
    
    public int MaxArmor
    {
        get 
        { 
            return data.maxArmor; 
        }

        set 
        { 
            Settings[Setting.HP].Max = Mathf.Abs(value); 
        }
    }

    public float Damage
    {
        get
        {
            return Settings[Setting.Damage].RandomValue();
        }
    }

    public float Odometer
    {
        get
        {
            return odometer;
        }
    }

    public float MovingSpeed
    {
        get
        {
            return Settings[Setting.MovingSpeed].Current;
        }

        set
        {
            Settings[Setting.MovingSpeed].Current = value;
            //CalculateMaxSpeed();
        }
    }

    public float TurretSpeed
    {
        get 
        { 
            return Settings[Setting.TurretSpeed]; 
        }
        set
        {
            Settings[Setting.TurretSpeed].Current = Mathf.Abs(value);
            Settings[Setting.TurretSpeed].Clamp();
        }
    }

    public float ROF
    {
        get
        {
            return Settings[Setting.RPM].Max;
        }

        set
        {
            Settings[Setting.RPM].Set(new Clamper(value));
        }
    }

    public float IRCMROF
    {
        get
        {
            return data.ircmRof;
        }

        set
        {
            data.ircmRof = Mathf.Clamp(value, 0, float.MaxValue);
        }
    }

    public Vector3 LocalVelocity
    {
        get
        {
            return transform.InverseTransformDirection(Velocity);
        }
    }

    public Vector3 LocalAngularVelocity
    {
        get
        {
            return transform.InverseTransformDirection(AngularVelocity);
        }
    }

    public Vector3 IndicatorPointPosition
    {
        get
        {
            if (IndicatorPoint == null)
            {
                //Debug.LogErrorFormat("(*) (*) (*) Лёха! Исправь ошибку! При уничтожении юнита или выходе из боя, ItemBattleTankIndicator нужно уничтожать сразу, либо удалять у него ссылку на IUnitBehavior!(*)(*)(*)");
                return Vector3.zero;
            }

            return IndicatorPoint.position;
        }
    }

    public PhotonPlayer Player
    {
        get
        {
            return player;
        }
    }

    public PlayerStat Statistics
    {
        get
        {
            return statistics;
        }
    }

    public BodykitController BodykitController
    {
        get
        {
            return bodykitController;
        }
    }    

    public bool TargetAimed
    {
        get
        {
            return targetAimed;
        }

        protected set
        {
            targetAimed = value;
        }            
    }


    public virtual Vector3 TargetPosition
    {
        get
        {
            return AimPoint.Point;
        }
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

    [SerializeField]
    protected LayerMask enemyLayerMask;

    public virtual BotAI BotAI
    {
        get
        {
            return null;
        }
    }

    public virtual Transform Body
    {
        get
        {            
            return body;
        }
    }

    public virtual Transform CameraPoint
    {
        get
        {
            return forCam;
        }
    }

    public float SqrBodyZSize
    {
        get; private set;
    }

    public bool DoubleExperience
    {
        get; private set;
    }

    protected abstract float OdometerRatio
    {
        get;
    }

    protected abstract float SpeedRatio
    {
        get;
    }

    protected abstract float CorrectionTime
    {
        get;
    }
    
    protected virtual bool FireButtonPressed
    {
        get
        {
            return fireButtonPressed;
        }
    }


    protected virtual float CannonAxisControl
    {
        get
        {
            float result = 0;
            angleXDelta = Cannon.forward.AngleSigned(AimingDirection(ShotPoint.position), Cannon.right);
            result = Mathf.Clamp(angleXDelta, -1, 1);

            return result;
        }
    }

    protected virtual float TurretAxisControl
    {
        get
        {            
            float result = 0;            
            angleYDelta = Turret.forward.AngleSigned(AimingDirection(Turret.position), Turret.up);
            result = Mathf.Clamp(angleYDelta, -1, 1);            

            return result;
        }
    }

    private Vector3 AimingDirection(Vector3 position)
    {
        return targetPoint - position;
    }

    public virtual float XAxisControl
    {
        get
        {
            //return Math.Abs(xAxis) > 0.1f ? xAxis : XDevs.Input.GetAxis("Turn Left/Right");
            return xAxis;
        }
    }

    public virtual float YAxisControl
    {
        get
        {
            if (CruiseControl)
            {
                return 1;
            }

            //return Math.Abs(yAxis) > 0.1f ? yAxis : XDevs.Input.GetAxis("Move Forward/Backward");
            return yAxis;
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
            if (CruiseControl)
            {
                return 1;
            }

            return XDevs.Input.GetAxis("Turn Up/Down");
        }
    }

    public virtual bool IsRequirePrimaryFire
    {
        get
        {
            return FireButtonPressed && (!StaticContainer.UI.IsWindowOnScreen || IsBot);
        }
    }

    protected virtual bool IsRequireSecondaryFire
    {
        get
        {
            return false && SecondaryFireIsOn;// ???
        }
    }

    public bool SecondaryFireIsOn
    {
        get; set;
    }

    public virtual float YAxisAcceleration
    {
        get
        {
            return 0;
        }
    }

    public virtual float XAxisAcceleration
    {
        get
        {
            return 0;
        }
    }

    protected virtual Vector3 IndicatorDeltaOffset
    {
        get
        {
            return Vector3.zero;
        }
    }

    protected virtual Transform IndicatorPoint
    {
        get
        {
            return indicatorPoint;
        }
    }
    
    protected virtual Transform Bumper
    {
        get
        {
            return bumper;
        }
    }

    public virtual int Team
    {
        get
        {
            return data.teamId;
        }
    }

    #region IUnitBehavior
    public virtual int OwnerID
    {
        get
        {
            return data.playerId;
        }
    }


    public virtual Transform Transform
    {
        get
        {
            return transform;
        }
    }

    public virtual GameObject GameObject
    {
        get
        {
            return gameObject;
        }
    }
    #endregion
    
    #region ISender
    public string Description
    {
        get
        {
            //return "[StaticPattern]";
            return "[VehicleController] " + name;
        }

        set
        {
            name = value;
        }
    }

    public void Reaction(Message message, params object[] parameters)
    {
        switch (message)
        {
            case Message.HPChanged:
                if (!IsMine)
                {
                    //Debug.LogError("1 Send HIT: " + parameters.Get<Clamper>());
                    Event(Message.Hit, this, parameters.Get<int>(), parameters.Get<int>(1), Position, parameters.Get<Clamper>(), parameters.Get<bool>(), parameters.Get<float>());
                }
                break;

            case Message.Death:
                Death(parameters != null ? parameters.Get<int>() : -1);
                break;

            case Message.Axis:
                switch (parameters.Get<Axis>())
                {
                    case Axis.Vertical:
                        //if (Mathf.Abs(yAxis) > 0.1f)
                        //{
                        //    CruiseControl = false;
                        //}

                        yAxis = parameters.Get<float>();
                        break;

                    case Axis.Horizontal:
                        xAxis = parameters.Get<float>();
                        break;
                }
                break;

            case Message.PrepareToEndBattle:
                IsAvailable = false;
                break;

            case Message.SettingsChanged:
                switch (parameters.Get<GameSettingsParameter>())
                {
                    case GameSettingsParameter.BackingReverse:
                        backInverse = parameters.Get<bool>();
                        break;
                }
                break;

#if UNITY_EDITOR
            case Message.Cheat:
                float val = parameters.Get<float>();

                switch (parameters.Get<Cheat>())
                {
                    case Cheat.Speedhack:
                        Settings[Setting.MovingSpeed].Set(0, val, val);
                        Settings[Setting.Accelerate].Set(10, 3, 30);
                        break;

                    case Cheat.Damage:
                        Settings[Setting.Damage].Set(0, val, val);
                        Settings[Setting.RPM].Set(0, 200, 200);
                        break;

                    case Cheat.HitPoints:
                        Settings[Setting.HP].Set(0, val, val);
                        break;

                    case Cheat.GodMode:
                        Settings[Setting.MovingSpeed].Set(0, 70, 70);
                        Settings[Setting.Accelerate].Set(10, 3, 30);
                        Settings[Setting.TurnSpeed].Set(0, 7, 7);
                        Settings[Setting.TurretSpeed].Set(0, 150, 150);
                        Settings[Setting.Damage].Set(10000, 10000, 10000);
                        Settings[Setting.HP].Set(0, 1000000, 1000000);
                        Settings[Setting.RPM].Set(0, 120, 120);
                        break;
                }
                break;
#endif

            case Message.Button:
                ButtonKey key = parameters.Get<ButtonKey>();
                ButtonMode mode = parameters.Get<ButtonMode>();

                switch (key)
                {
                    case ButtonKey.Fire1:
                        fireButtonPressed = mode != ButtonMode.Up;
                        break;
                }

                if (mode == ButtonMode.Down)
                {
                    switch (key)
                    {
                        case ButtonKey.CruiseControl:                            
                            CruiseControl = !CruiseControl;                            
                            break;

                        case ButtonKey.Slot_1:
                            ConsumableUse(8);
                            break;

                        case ButtonKey.Slot_2:
                            ConsumableUse(9);
                            break;

                        case ButtonKey.Slot_3:
                            ConsumableUse(2);
                            break;

                        case ButtonKey.Slot_4:
                            ConsumableUse(3);
                            break;

                        case ButtonKey.Slot_5:
                            ConsumableUse(4);
                            break;
                    }
                }
                break;

            case Message.CameraChangedPoint:
                targetPoint = parameters.Get<Vector3>();
                break;
        }
    }

    private void ConsumableUse(int slotID)
    {
        IConsumableBattle consumable = (IConsumableBattle)InstalledBattleConsumables.GetBySlot(slotID);
        
        if (consumable == null)
        {
            Debug.LogWarning("Consumable in slot " + slotID + " not found! " + name, gameObject);
            return;
        }

        if (consumable.SlotType == ConsumableSlotType.Ammunition)
        {
            mainWeapon.Charge(consumable);
        }
        else
        {
            consumable.Use();
        }
    }

    private List<ISubscriber> subscribers = null;

    public List<ISubscriber> Subscribers
    {
        get
        {
            if (subscribers == null)
            {
                subscribers = new List<ISubscriber>();
            }
            return subscribers;
        }
    }

    public void AddSubscriber(ISubscriber subscriber)
    {
        if (Subscribers.Contains(subscriber))
        {
            return;
        }
        Subscribers.Add(subscriber);
    }

    public void RemoveSubscriber(ISubscriber subscriber)
    {
        Subscribers.Remove(subscriber);
    }

    public void Event(Message message, params object[] _parameters)
    {
        for (int i = 0; i < Subscribers.Count; i++)
        {
            Subscribers[i].Reaction(message, _parameters);
        }
    }
    #endregion

    protected virtual void Awake()
    {
        if (transform == null)
        {
            transform = GetComponent<Transform>();
        }
    }

    public virtual void SendMessageToFX(Message message, params object[] parameters)
    {
        for (int i = 0; i < effectsSettings.Length; i++)
        {
            effectsSettings[i].Reaction(message, parameters);
        }

        for (int i = 0; i < soundsSettings.Length; i++)
        {
            soundsSettings[i].Reaction(message, parameters);
        }
    }

    public virtual void InitComponents()
    {
        if (rigidbody == null)
        {
            rigidbody = GetComponent<Rigidbody>();
        }

        if (transforms == null || transforms.Length == 0)
        {
            transforms = GetComponentsInChildren<Transform>();
        }

        rigidbody.drag = 2;

        if (transform == null)
        {
            transform = GetComponent<Transform>();
        }

        if (body == null)
        {
            body = transform.Find("Body/Mesh_Body") ?? transform.Find("Mesh_Body") ?? transform.Find("Body");
        }

        if (!boundsInited)
        {
            Mesh bodyMesh = body.GetComponentInChildren<MeshFilter>().sharedMesh;
            if (bodyMesh != null)
            {
                bodyMeshBounds = bodyMesh.bounds;
                boundsInited = true;
            }
        }

        if (settingsContainer == null)
        {
            InitSettingsContainer();
        }

        if (soundsSettings == null)
        {
            soundsSettings = GetComponents<ISoundSettings>();
        }

        if (effectsSettings == null)
        {
            effectsSettings = GetComponents<IEffectsSettings>();
        }

        if (turret == null)
        {
            turret = transform.Find("Body/Turret") ?? transform.Find("Turret");
        }

        if (bumper == null)
        {
            bumper = transform.Find("Body/Bumper");
        }

        if (cannon == null)
        {
            cannon = transform.Find("Body/Turret/Cannon") ?? transform.Find("Cannon");
        }

        if (cannonEnd == null)
        {
            cannonEnd = transform.Find("Body/Turret/CannonEnd");
        }

        if (shotPoint == null)
        {
            shotPoint = transform.Find("Body/Turret/ShotPoint");
            if (shotPoint == null)
            {
                shotPoint = transform.Find("Body/Turret/Cannon/ShotPoint");
            }
        }

        if (indicatorPoint == null)
        {
            indicatorPoint = transform.Find("IndicatorPoint");
        }

        if (forCam == null)
        {
            forCam = turret.Find("ForCamera");
        }

        if (vehicleMarker == null)
        {
            vehicleMarker = GetComponentInChildren<VehicleMarker>();
        }

        if (bodykitController == null)
        {
            bodykitController = GetComponent<BodykitController>();
        }

        if (lowQualityCollider == null)
        {
            lowQualityCollider = transform.Find("Body/LowQualityCollider");
        }

        Container.InitRenderers();

        vehicleMarker.Init(this);
    }

    /* UNITY and EVENT SECTION */
    protected virtual void OnPhotonInstantiate(PhotonMessageInfo info)
    {
        if (info.photonView == null)
        {
            PhotonView = GetComponent<PhotonView>();
        }
        else
        {
            PhotonView = info.photonView;
        }

        InitComponents();

        if (Cannon != null)
        {
            if (ShotPoint != null)
            {
                ShotPoint.SetParent(Cannon);
            }

            if (CannonEnd != null)
            {
                CannonEnd.SetParent(Cannon);
            }
        }

        checkObstacleMask = 1 << LayerMask.NameToLayer("Terrain") | 1 << LayerMask.NameToLayer("Default");

        CameraPoint.SetParent(transform);

        GetNavMeshObstacle();

        SqrBodyZSize = Mathf.Pow(BodyMeshBounds.size.z, 2);
        DoubleExperience = ProfileInfo.doubleExpVehicles.Contains(ProfileInfo.currentVehicle);

        object lastOdometer;
        if (BattleConnectManager.Instance.GetValue("MyOdometer", out lastOdometer))
        {
            odometer = (ObscuredFloat)lastOdometer;
        }

        primaryShellInfo = GunShellInfo.UsualShell;
        secondaryShellInfo = GunShellInfo.UsualShell;

        if (Application.isEditor)
        {
            changingVehicleMaterial = QualityManager.Instance.ChangeObjectMaterials(Container.Renderers);
            QualityManager.Instance.StartCoroutine(changingVehicleMaterial);
        }

        aimingTimer = new Clamper(Random.Range(0.5f, 2f));

        player = PhotonView.owner;
        data = (TankData)PhotonView.instantiationData[0];
        data.playerId = IsBot ? data.playerId : PhotonView.ownerId;

        if (!PhotonView.isMine && data.innerId == ProfileInfo.playerId && !Debug.isDebugBuild)
        {
            var query = new Dictionary<string, string>
            {
                { "tankId", ProfileInfo.currentVehicle.ToString() },
            };

            Http.Manager.ReportStats(
                location: "battle",
                action: "playerWithSameIdAlreadyConnected",
                query: query);

            StaticContainer.Connector.ForcedDisconnect();
            return;
        }

        if (IsBot)
        {
            SetBotKeys();
        }

        if (StaticType.Input.Instance<IInput>().IsMobile && StaticType.Options.Instance<IOptions>().GraphicsQuality == 0)
        {
            myFrame = StaticType.BattleController.Instance<IBattleController>().Units.Count % frameDifferentcial;            
        }

        CorrectPosition = transform.position;
        CorrectRotation = transform.rotation;

        if (Bumper != null)
        {
            Bumper.gameObject.SetActive(true);
            Bumper.gameObject.layer = LayerMask.NameToLayer("Bumper");
        }

        Subscribes();
        
        if (IsMine)
        {
            Dispatcher.Subscribe(EventId.BeforeReconnecting, OnReconnect);
        }

        weapons = new Dictionary<GunShellInfo.ShellType, Weapon>
        {
            { GunShellInfo.ShellType.Usual,             new Weapon(this, GunShellInfo.ShellType.Usual) },
            { GunShellInfo.ShellType.Missile_SACLOS,    new Weapon(this, GunShellInfo.ShellType.Missile_SACLOS) },
            { GunShellInfo.ShellType.IRCM,              new Weapon(this, GunShellInfo.ShellType.IRCM) }
        };

        mainWeapon = new Weapon(this, GunShellInfo.ShellType.Usual);
        mainWeapon.InstantReload();

        if (IsMine && !StaticContainer.Connector.FirstConnect)
        {
            statistics = BattleConnectManager.Instance.MyLastPlayerStat;
            statistics.Team = data.teamId;
            statistics.PlayerID = PhotonNetwork.player.ID;
            StaticType.Input.AddSubscriber(this);
        }
        else
        {
            statistics = new PlayerStat(data.playerId, data.teamId, data.playerLevel, data.playerName, data.vip, data.innerId, data.clanName, data.UnitBattle != null ? data.UnitBattle.ID : 2);
        }

        StaticContainer.BattleController.AddData(data.playerId, this, data, statistics);
        rigidbody.isKinematic = !PhotonView.isMine;
        rigidbody.collisionDetectionMode = PhotonView.isMine ? CollisionDetectionMode.ContinuousDynamic : CollisionDetectionMode.Discrete;
        
        if (IsMine)
        {
            tag = Tag.Items[Tag.Key.Player];
            rigidbody.centerOfMass = CenterOfMass;
            transform.position = SpawnPoints.instance.GetCorrectPosition(this);
            storedVehiclePosition = transform.position;

            if (StaticContainer.Connector.FirstConnect)
            {
                HPSystem.SetArmor(MaxArmor, -1);
            }
        }
        else
        {
            Respawn(transform.position, transform.rotation, true, true);
        }
        
        InitSettings();

        if (!IsBot)
        {
            FillVehicleData();
        }

        SetBodyArt();
        SetEngineAudio();
        SetTurretAudio();

        if (IsBot)
        {
            kickBotAt = (double)PhotonView.instantiationData[2];

            if (PhotonView.isMine)
            {
                SetStartBotProperties();
            }
            else
            {
                CheckAllBotStats();
                TryGetBotParams();
            }
        }
        
        SecondaryFireIsOn = true;
        RegisterForPhoton();

        if (PhotonView.isMine && IsBot)
        {
            ReanimateBot();
            BotDispatcher.Instance.RegisterBotAI(BotAI);
        }

        SetMembership();
        if (BattleController.Instance.IsTeamMode && StaticContainer.BattleController.CurrentUnit != this && !IsMine)
        {
            Dispatcher.Subscribe(EventId.MainTankAppeared, OnMainVehicleAppearedForTM);
        }

        if (PhotonView.isMine && !IsBot)
        {
            List<IConsumableBattle> cons = unitBattle.InstalledConsumables.GetBattleList();

            for (int i = 0; i < cons.Count; i++)
            {
                cons[i].Reset();
            }

            if (PhotonNetwork.offlineMode)
            {
                IBattleTutorial tutor = StaticType.BattleTutorial.Instance<IBattleTutorial>();

                for (int i = 0; i < tutor.ConsumableIds.Length; i++)
                {
                    IConsumable consumable = StaticContainer.MainData.GetConsumableByID(tutor.ConsumableIds[i]).GetWorkConsumable();

                    if (consumable.SlotType == ConsumableSlotType.Ammunition)
                    {
                        consumable.Amount.Current = 100000;
                    }

                    unitBattle.InstalledConsumables.AddBySlot(StaticContainer.MainData.GetSlotByType(consumable.SlotType), consumable);
                }

                InitConsumables(unitBattle.InstalledConsumables);
            }
            else
            {
                //КОСТЫЛЬ?
                InitConsumables(unitBattle.InstalledConsumables);
            }

            backInverse = StaticType.Options.Instance<IOptions>().BackingReverse;
        }

        Settings[Setting.MovingSpeed].Multiply(moveSpeedKoefficient); 
        Settings[Setting.TurnSpeed].Multiply(turnSpeedRatio); 

        Dispatcher.Send(EventId.TankJoinedBattle, new EventInfo_I(data.playerId));
        StaticType.TeamManager.Instance<ITeamManager>().UnitCreated(this);
        StaticType.BaseDispatcher.Instance<IBaseDispatcher>().AddCapturer(this);

        SendToContainer();

        //Подписываемся на систему управлением прочностью
        HPSystem.AddSubscriber(this);

        AddSubscriber(AimingBehaviour);

        if (IsMine)
        {
            SendMessageToFX(Message.EffectRequest, EffectTarget.Permanent);
        }
    }

    public void SendToContainer()
    {
        StaticType.CollidersContainer.Instance<ICollidersContainer>().AddCollidable(GetComponentsInChildren<Collider>(), this);
    }

    public void AddActiveBuff(Buff buff)
    {
        //Debug.LogError(Name + " AddActiveBuff: " + buff.Description + ", isNegative: " + buff.isNegative);
        if (buff.isNegative)
        {
            ActiveDebuffs.Add(buff);
        }
        else
        {
            ActiveBuffs.Add(buff);
        }
    }

    public void RemoveActiveBuff(Buff buff, bool isNegative)
    {
        if (isNegative)
        {
            ActiveDebuffs.Remove(buff);
        }
        else
        {
            ActiveBuffs.Remove(buff);
        }
    }

    public Buff GetActiveBuff(Setting buffType, bool isNegative)
    {
        if (isNegative)
        {
            for (int i = 0; i < ActiveDebuffs.Count; i++)
            {
                if (ActiveDebuffs[i].Type == buffType)
                {
                    return ActiveDebuffs[i];
                }
            }
        }
        else
        {
            for (int i = 0; i < ActiveBuffs.Count; i++)
            {
                if (ActiveBuffs[i].Type == buffType)
                {
                    return ActiveBuffs[i];
                }
            }
        }

        return null;
    }

    private void InitSettings()
    {
        if (UnitBattle == null)
        {
            Debug.LogError("UnitBattle == null, " + name, gameObject);
            return;
        }
        
        if (IsMine && !IsBot)
        {
            //unitBattle.ApplyCrewParameters(); КАМИНСУН
            Settings = UnitBattle.Settings.Clone();
        }
        else
        {
            SettingsFromData();
            Setting paramName;

            for (int i = 0; i < unitBattle.BaseSettings.Count; i++)
            {
                paramName = unitBattle.BaseSettings.GetName(i);

                if (!Settings.Contains(paramName))
                {
                    Settings.Add(paramName, unitBattle.BaseSettings[paramName].Clone());
                }
            }
        }
        
        Settings.Init(true);

        if (Settings[Setting.TurnSpeed].IsZero)
        {
            Settings[Setting.TurnSpeed] = Settings[Setting.MovingSpeed].Clone();
        }
    }

    /// <summary>
    /// Берем параметры из сети для клонов других игроков
    /// </summary>
    private void SettingsFromData()
    {
        Settings.Add(Setting.HP, new Clamper(data.maxArmor));
        Settings.Init();
    }

    private void InitSettingsContainer()
    {
        if (Transform == null)
        {
            return;
        }

        settingsContainer = GetComponent<SettingsContainer>();

        if (settingsContainer == null)
        {
            settingsContainer = gameObject.AddComponent<SettingsContainer>();
        }

    //    settingsContainer.SetSetting(Setting.MovingSpeed, new Clamper(data.movingSpeed));
    }

    public virtual void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        if (isExploded)
        {
            return;
        }

        if (stream.isWriting)
        {
            if (!info.photonView.isMine)
            {
                return;
            }

            CorrectPosition = transform.position;
            stream.SendNext(CorrectPosition);
            CorrectRotation = transform.rotation;
            stream.SendNext(CorrectRotation);

            if (rigidbody == null)
            {
                Debug.Log(name + " rigidbody is null", this);
                correctVelocity = Vector3.zero;
            }
            else
            {
                correctVelocity = rigidbody.velocity;
            }

            stream.SendNext(correctVelocity);

            if (Turret != null)
            {
                CorrectTurretAngle = Turret.localEulerAngles.y;
                stream.SendNext(CorrectTurretAngle);
            }

            if (Cannon != null)
            {
                CorrectCannonAngle = Cannon.localEulerAngles.x;
                stream.SendNext(CorrectCannonAngle);
            }
        }
        else
        {
            if (info.photonView.isMine)
            {
                return;
            }

            MarkActivity();

            int streamCount = stream.Count - stream.currentItem;

            if (streamCount > 0)
            {
                CorrectPosition = (Vector3) stream.ReceiveNext();
            }

            if (streamCount > 1)
            {
                CorrectRotation = (Quaternion) stream.ReceiveNext();
            }

            if (streamCount > 2)
            {
                CorrectVelocity = (Vector3) stream.ReceiveNext();
            }

            if (streamCount > 3)
            {
                CorrectTurretAngle = (float) stream.ReceiveNext();
            }

            if (streamCount > 4)
            {
                CorrectCannonAngle = (float)stream.ReceiveNext();
            }

            if (settingSpawnPosition)
            {
                transform.position = CorrectPosition;
                transform.rotation = CorrectRotation;

                if (Turret != null)
                {
                    Turret.localEulerAngles = new Vector3(0, CorrectTurretAngle, 0);
                }

                if (Cannon != null)
                {
                    Cannon.localEulerAngles = new Vector3(CorrectCannonAngle, 0, 0);
                }

                settingSpawnPosition = false;
            }

            currentCorrection = 0;
        }
    }

    private void FixedUpdate()
    {
        if (myFrame >= 0)
        {
            physicFrame++;
            if (!IsMine && (physicFrame % frameDifferentcial != myFrame))
            {
                return;
            }
        }

        PhysicsUpdate();
    }

    private void Update()
    {
        if (myFrame >= 0)
        {
            normalFrame++;
            if (!IsMine && (normalFrame % frameDifferentcial != myFrame))
            {
                return;
            }
        }

        deltaTime = Time.time - lastTime;
        lastTime = Time.time;
        NormalUpdate();

        ////Тут тестовый функц
        if (!Application.isEditor)
        {
            return;
        }

        if (StaticContainer.BattleController.CurrentUnit == this && Input.GetKeyDown(KeyCode.B))
        {
            Buff buff = ((BattleController) StaticType.BattleController.Instance()).buffsForMyVeh[1].CreateNew();
            buff.SetPerformerUnit(this);
            StaticType.BuffDispatcher.Instance<IBuffDispatcher>().AddBuff(this, buff);
        }
    }

    protected virtual void PhysicsUpdate()
    {
        if (aimingTimer != null)
        {
            if (aimingTimer.Current > 0)
            {
                aimingTimer.Current -= Time.deltaTime;
                return;
            }

            aimingTimer.Refresh();
        }

        if (PhotonView.isMine && isAvailable)
        {
            AimingBehaviour.Aiming();
        }
    }   

    protected virtual void NormalUpdate()
    {
        if (PhotonView == null)
        {
            return;
        }

        //if (!Debug.isDebugBuild)
        //{
        //    inactivityTime += Time.deltaTime;
        //
        //    if (inactivityTime > GameData.maxInactivityTime)
        //    {
        //        if (PhotonView.isMine)
        //        {
        //            StaticType.BattleController.Instance<IBattleController>().EndBattle(EndBattleCause.Inactivity);
        //            return;
        //        }
        //
        //        if (PhotonNetwork.isMasterClient)
        //        {
        //            PhotonNetwork.CloseConnection(PhotonView.owner);
        //        }
        //    }
        //}

        if (!IsAvailable)
        {
            return;
        }

        if (PhotonView.isMine && IsAvailable)
        {
            TurretRotation();
        }
        else
        {
            MoveClone();
        }

        mainWeapon.UpdateReloadingProgress(deltaTime);

        if (IsMine)
        {
            if (IsRequirePrimaryFire && PrimaryFire(shotPoint.rotation))
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
        }
    }

    /// <summary>
    /// Установка расходников на технику.
    /// </summary>
    /// <param name="hangarConsumables">Расходники, установленные в ангаре.</param>
    public void InitConsumables(Consumables hangarConsumables)
    {
        InstalledBattleConsumables = hangarConsumables.Clone();
        InstalledBattleConsumables.Init(this);
        ChargeWeapons();
        AutoConsumablesApply();
        //Debug.LogError("InitConsumables: " + unitBattle.Name + ", cons: " + installedBattleConsumables.GetList().Count);
    }

    /// <summary>
    /// Применение автоматических расходок в начале боя.
    /// </summary>
    public void AutoConsumablesApply()
    {
        // Применение провизии. КАМИНСУН
        /*List<IConsumableBattle> provisions = InstalledBattleConsumables.GetListBySlotType<IConsumableBattle>(ConsumableSlotType.Provisions);
        for (int i = 0; i < provisions.Count; i++)
        {
            provisions[i].Use();
        }*/

        // Применение оборудования.
        List<IConsumableBattle> equipments = InstalledBattleConsumables.GetListBySlotType<IConsumableBattle>(ConsumableSlotType.Equipment);
        for (int i = 0; i < equipments.Count; i++)
        {
            equipments[i].Use();
        }

        // Применение камуфляжа.
        IConsumableBattle camo = InstalledBattleConsumables.GetBySlotType<IConsumableBattle>(ConsumableSlotType.Camouflages);
        if (camo != null)
        {
            camo.Use();
        }

        // Применение стикера.
        IConsumableBattle decal = InstalledBattleConsumables.GetBySlotType<IConsumableBattle>(ConsumableSlotType.Decals);
        if (decal != null)
        {
            decal.Use();
        }
    }

    /// <summary>
    /// Зарядка всех пушек (аременно)
    /// </summary>
    private void ChargeWeapons()
    {
        mainWeapon.FirstCharge();
    }

    /// <summary>
    /// Уствновка наклеек и камуфляжей
    /// </summary>
    private void SetBodyArt()
    {     
        bodykitController.SetShadowPlane();

        if (UnitBattle != null) //КОСТЫЛЬ
        {
            IConsumable cam = null;
            IConsumable dec = null;

            if (PhotonView.isMine)
            {
                if (IsBot)
                {
                    if (Random.Range(0, 1f) <= 0.5f)
                    {
                        List<IConsumableEntity> cams = StaticContainer.MainData.GetConsumablesBySlotType(ConsumableSlotType.Camouflages);
                        ICamouflage cur;
                        for (int i = 0; i < cams.Count; i++)
                        {
                            cur = (ICamouflage) cams[i];

                            if (cur.IsNational || cur.IsVip || !cams[i].Units.Contains(unitBattle.ID))
                            {
                                cams.RemoveAt(i);
                                i--;
                            }
                        }

                        if (cams.Count > 0)
                        {
                            cam = cams[Random.Range(0, cams.Count)].GetWorkConsumable();
                        }
                    }

                    if (Random.Range(0, 1f) <= 0.1f)
                    {
                        List<IConsumableEntity> decs = StaticContainer.MainData.GetConsumablesBySlotType(ConsumableSlotType.Decals);
                        IDecal cur;

                        for (int i = 0; i < decs.Count; i++)
                        {
                            cur = (IDecal)decs[i];
                            if (cur.IsVip || !decs[i].Units.Contains(unitBattle.ID))
                            {
                                decs.RemoveAt(i);
                                i--;
                            }
                        }

                        if (decs.Count > 0)
                        {
                            dec = decs[Random.Range(0, decs.Count)].GetWorkConsumable();
                        }
                    }
                }
                else
                {
                    cam = UnitBattle.InstalledConsumables.GetBySlot(0);
                    dec = UnitBattle.InstalledConsumables.GetBySlot(1);
                }
            }
            else
            {
                //Debug.LogError(Name + ", data.patternId: " + data.patternId);
                if (data.patternId > 0)
                {
                    cam = StaticContainer.MainData.GetConsumableByID(data.patternId).GetWorkConsumable();
                }

                if (data.decalId > 0)
                {
                    dec = StaticContainer.MainData.GetConsumableByID(data.decalId).GetWorkConsumable();
                }
            }

            if (cam != null)
            {
                bodykitController.DrawCamouflage(StaticContainer.MainData.GetCamouflageByID(cam.ID), StaticContainer.SceneManager.CurrentMap.Location);
            }

            if (dec != null)
            {
                bodykitController.DrawDecal(StaticContainer.MainData.GetDecalByID(dec.ID));
            }
        }
    }

    public virtual void ChangeHitPoints(int attackerID, int delta, Vector3 hitPoint, GunShellInfo.ShellType shellType, bool crit = false)
    {        
        ShellHitDispatcher.AccumulateDamage(
                        victimId: data.playerId,
                        attackerId: attackerID,
                        damage: delta,
                        position: transform.InverseTransformPoint(hitPoint),
                        shellType: shellType);

        if (HPSystem.Armor > 0 && !PhotonView.isMine)
        {
            Dispatcher.Send(EventId.TankTakesDamage, new EventInfo_U(data.playerId, delta, attackerID, (int)shellType, hitPoint, crit));
        }        
    }

    protected virtual void OnDrawGizmos()
    {
        if (BotAI != null)
        {
            BotAI.Draw();
        }
    }

    protected virtual void OnDestroy()
    {
        if (data.playerId == StaticType.BattleController.Instance<IBattleController>().MyPlayerId)
        {
            BattleConnectManager.Instance.SetValue("MyOdometer", odometer);
        }  

        Event(Message.UnitBattleDestroyed, this);        

        Unsubscribes();
        StopCoroutine(changingVehicleMaterial);
        
        // Чтобы результаты бота сохранились
        if (IsBot)
        {
            Dispatcher.Send(EventId.OffLayerInTeamMask, new EventInfo_II(data.teamId, ownLayer));
            Dispatcher.Send(EventId.TankOutOfTime, new EventInfo_I(data.playerId));
        }

        Dispatcher.Send(EventId.TankLeftTheGame, new EventInfo_I(data.playerId));

        //TODO: 2 шаг, поменять все на вызов 1 метода гейм контроллера, на который будут подписаны все остальные     
        StaticType.Connector.Instance<IConnector>().RemovePhotonMessageTarget(gameObject);
        StaticType.TeamManager.Instance().Reaction(Message.UnitBattleDestroyed, this);
        StaticType.GameController.Instance().Reaction(Message.UnitBattleDestroyed, this);
        StaticType.BaseDispatcher.Instance<IBaseDispatcher>().RemoveCapturer(this);

        StaticType.Input.RemoveSubscriber(this);
    }

    /* PUBLIC SECTION */

    public virtual void AnimateClone()
    {
    }

    public virtual void StoreCloneRotation()
    {
    }

    /// <summary>
    /// Друзья ли владельцы Vehicl'ов в данном бою.
    /// Внимание! Если транспорт сравнивается сам с собой, возвращается FALSE.
    /// </summary>
    /// <returns></returns>
    public static bool AreFriends(VehicleController player1, VehicleController player2)
    {
        if (player1 == null || player2 == null || player1 == player2)
        {
            return false;
        }

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
        {
            return false;
        }

        string clan1 = player1.data.clanName;
        string clan2 = player2.data.clanName;

        return !string.IsNullOrEmpty(clan1) && clan1 == clan2;
    }

    public abstract void MovePlayer();
    

    public bool ShotPrepare(GunShellInfo.ShellType shellType, ref Weapon weapon, bool checkIsReady = false) //
    {
        MarkActivity();

        //weapon = weapons[shellType];
        weapon = mainWeapon;

        if(checkIsReady && !weapon.IsReady)
        { 
            return false; 
        }

        weapon.RegisterShot();

        return true;
    }

    public virtual bool PrimaryFire(Quaternion rotation) 
    {
        return false;
    }        

    public virtual void SecondaryFire(GunShellInfo.ShellType shellType, int targetId)
    {
        throw new NotImplementedException();
    }

    public void SyncExistance(bool newExistance)
    {
        if (existanceSynchronized || PhotonView.isMine)
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
            HPSystem.SetArmor(data.maxArmor, -1);
        }

        Dispatcher.Send(EventId.TankHealthChanged, new EventInfo_II(data.playerId, HPSystem.Armor));

        settingSpawnPosition = true;
        if (!firstTime)
        {
            IsAvailable = true;
        }

        CorrectPosition = transform.position = position;
        CorrectRotation = transform.rotation = rotation;

        if (Turret)
        {
            Turret.localRotation = Quaternion.identity;
        }

        lastTurretLocalRotationY = 0;
        CorrectVelocity = Vector3.zero;
        storedVehiclePosition = transform.position;

        Dispatcher.Send(EventId.TankRespawned, new EventInfo_I(data.playerId));

        if (IsMine)
        {
            RemoveAllItems();
            Dispatcher.Send(EventId.MyTankRespawned, new EventInfo_SimpleEvent());            
        }
    }

    private void ResetConsumables()
    {
        List<IConsumable> consumables = InstalledBattleConsumables.GetList();

        for (int i = 0; i < consumables.Count; i++)
        {
            consumables[i].Reset();
        }
    }

    public virtual void Death(int attackerId = -1)
    {
        StaticType.BaseDispatcher.Instance<IBaseDispatcher>().RemoveCapturer(this);
        Explode();

        if (IsMine)
        {
            Event(Message.Death);

            if (UnitBattle != null && data.playerId == StaticType.BattleController.Instance<IBattleController>().MyPlayerId)
            {
                UnitBattle.IsDestroyed = true;
            }

            if (AimingBehaviour != null)
            {
                AimingBehaviour.ResetGunsight();
                AimingBehaviour.SetNewAimPointInfo(new AimPointInfo());
            }

            ResetConsumables();
        }
        else
        {
            if (attackerId == PhotonNetwork.player.ID)
            {
                StaticType.UI.Instance().Reaction(Message.BattleCentralNotification, "UI_Battle_UnitDestroyed", data.Nick, "Destroy");
            }
        }

        StaticType.BuffDispatcher.Instance<IBuffDispatcher>().StopBuffsByVehicle(this);
        Unsubscribes();
    }

    public virtual void Explode()
    {
        if (!isAvailable)
        {
            return;
        }

        SendMessageToFX(Message.EffectRequest, EffectTarget.Explosion);

        IsAvailable = false;

        if (!PhotonView.isMine)
        {
            return;
        }

        mainWeapon.InstantReload();
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

    public void SetMarkedStatus(bool marked, bool byMe = false)
    {
        if (isMarked == marked)
        {
            return;
        }

        if (vehicleMarker != null)
        {
            vehicleMarker.SetMarkedStatus(marked);
            isMarked = marked;
        }

        if (byMe)
        {
            StaticType.UI.Instance().Reaction(Message.UnitAimed, marked);
        }
    }

    public virtual void MakeRespawn(bool forced, bool restoreLife, bool firstTime)
    {
        if (!StaticContainer.Master.Respawn && !firstTime)
        {
            if (IsMine && !IsBot)
            {
                StaticType.GameController.Instance<IGameController>().RequestUnitSelection(WindowShowCause.UnitSelection);
            }
            return;
        }

        Transform spawnPoint = SpawnPoints.instance.GetRandomPoint(this, data.teamId, forced);
        PhotonView.RPC("Respawn", PhotonTargets.All, spawnPoint.position, spawnPoint.rotation, restoreLife, firstTime);
    }   
   
#if UNITY_EDITOR
    //public void Cheat()
    //{
    //    if (!Debug.isDebugBuild)
    //    {
    //        return;
    //    }
    //
    //    cheatActivated = !cheatActivated;
    //
    //    if (cheatActivated)
    //    {
    //        MovingSpeed *= 2f;
    //        ROF *= 17;
    //        maxArmorBeforeCheat = MaxArmor;
    //        Armor = MaxArmor = 200000;
    //        Dispatcher.Send(EventId.TankHealthChanged, new EventInfo_II(data.playerId, Armor));
    //        Debug.Log("*** Cheat activated ***");
    //    }
    //    else
    //    {
    //        MovingSpeed /= 2f;
    //        ROF /= 17;
    //        MaxArmor = maxArmorBeforeCheat;
    //        Armor = (int)(MaxArmor * 0.9);
    //        Dispatcher.Send(EventId.TankHealthChanged, new EventInfo_II(data.playerId, Armor));
    //        Debug.Log("*** Cheat disactivated ***");
    //    }
    //}

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

    public virtual int CalcDamage(int attack, Collider collider)
    {
        float result = attack;
        result *= CritZoneController.GetCoefficient(collider);
        return (int)result;
    }

    public Weapon GetWeapon(GunShellInfo.ShellType shellType)
    {
        if (shellType == GunShellInfo.ShellType.Usual)
        {
            return mainWeapon;
        }

        return weapons[shellType];
    }

    /* PRIVATE SECTION */

    private void GetNavMeshObstacle()
    {
        SelfObstacle = GetComponent<UnityEngine.AI.NavMeshObstacle>();

        //if (SelfObstacle != null)
        //{
        //    var col = Bumper.GetComponent<BoxCollider>();
        //    SelfObstacle.size = col.size;
        //    SelfObstacle.center = transform.InverseTransformPoint(Bumper.position);
        //}
    }

    private void Subscribes()
    {
        Dispatcher.Subscribe(EventId.TargetAimed, OnTargetAimed);
        Dispatcher.Subscribe(EventId.TankTakesDamage, OnVehicleTakesDamage);
        Dispatcher.Subscribe(EventId.SettingsSubmited, OnSettingsSubmitted);
        Dispatcher.Subscribe(EventId.NowImMaster, OnIamMaster);
        Dispatcher.Subscribe(EventId.OffLayerInTeamMask, OnOffLayerInTeamMask);
        Dispatcher.Subscribe(EventId.StartBuff, OnBuffStart);
        Dispatcher.Subscribe(EventId.StopBuff, OnBuffStop);
        Dispatcher.Subscribe(EventId.UseBuffBlocker, OnBlockBuffs);

        if (IsBot)
        {
            Dispatcher.Subscribe(EventId.PhotonRoomCustomPropertiesChanged, RoomPropsChanged);
        }

        if (IsMine)
        {
            //StaticContainer.UI.AddSubscriber(this);
            AddSubscriber(StaticType.UI.Instance());
            AddSubscriber(StaticType.Statistics.Instance());

            if (!IsBot)
            {
                StaticType.Input.AddSubscriber(this);
                StaticType.Console.AddSubscriber(this);
                StaticType.Options.AddSubscriber(this);
            }
        }
    }

    protected virtual void Unsubscribes()
    {
        Dispatcher.Unsubscribe(EventId.MainTankAppeared, OnMainVehicleAppearedForTM);
        Dispatcher.Unsubscribe(EventId.TargetAimed, OnTargetAimed);
        Dispatcher.Unsubscribe(EventId.TankTakesDamage, OnVehicleTakesDamage);
        Dispatcher.Unsubscribe(EventId.SettingsSubmited, OnSettingsSubmitted);
        Dispatcher.Unsubscribe(EventId.BeforeReconnecting, OnReconnect);
        Dispatcher.Unsubscribe(EventId.NowImMaster, OnIamMaster);
        Dispatcher.Unsubscribe(EventId.PhotonRoomCustomPropertiesChanged, RoomPropsChanged);
        Dispatcher.Unsubscribe(EventId.NewLayerInTeamMask, OnNewLayerInTeamMask);
        Dispatcher.Unsubscribe(EventId.OffLayerInTeamMask, OnOffLayerInTeamMask);
        Dispatcher.Unsubscribe(EventId.StartBuff, OnBuffStart);
        Dispatcher.Unsubscribe(EventId.StopBuff, OnBuffStop);
        Dispatcher.Unsubscribe(EventId.UseBuffBlocker, OnBlockBuffs);

        StaticType.UI.RemoveSubscriber(this);
        StaticType.MainCamera.RemoveSubscriber(this);
        StaticType.Input.RemoveSubscriber(this);
        StaticType.Console.RemoveSubscriber(this);
    }

    private void TryGetBotParams()
    {
        object botHealthObj;
        object botExistanceObj;

        if (PhotonNetwork.room.CustomProperties.TryGetValue(KeyForBotHealth, out botHealthObj) && botHealthObj != null)
        {
            BattleController.Instance.OnPlayerPropertiesChanged(data.playerId, new Hashtable { { "hl", (int)botHealthObj } });
        }

        if (PhotonNetwork.room.CustomProperties.TryGetValue(KeyForBotExistance, out botExistanceObj) && botExistanceObj != null)
        {
            BattleController.Instance.OnPlayerPropertiesChanged(data.playerId, new Hashtable { { "ex", (bool)botExistanceObj } });
        }
    }

    protected virtual void OnIamMaster(EventId id, EventInfo ei)
    {
        if (IsBot)
        {
            if (rigidbody == null)
            {
                return;
            }

            rigidbody.isKinematic = false;
            rigidbody.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            ReanimateBot();
            BotDispatcher.Instance.RegisterBotAI(BotAI);
        }
    }

    private void RegisterForPhoton()
    {
        StaticContainer.Connector.AddPhotonMessageTarget(gameObject);
    }

    private int CheckIfDoubleExperience(int amount)
    {
        if (DoubleExperience)
        {
            amount *= 2;
        }

        return amount;
    }

    private void OnBlockBuffs(EventId eid, EventInfo ei)
    {
        EventInfo_U info = (EventInfo_U)ei;

        if ((int)info[0] != data.playerId)
        {
            return;
        }
        
        int[] intList = (int[])info[1];

        //Debug.LogError("id: " + (int)info[0] + ", count: " + intList.Length + ", " + (bool)info[2]);

        if ((bool) info[2])
        {
            for (int i = 0; i < intList.Length; i++)
            {
                if (!BlockedDebuffs.Contains((Setting) intList[i]))
                {
                    BlockedDebuffs.Add((Setting) intList[i]);
                }
            }
        }
        else
        {
            for (int i = 0; i < intList.Length; i++)
            {
                BlockedDebuffs.Remove((Setting)intList[i]);
            }
        }
    }

    /// <summary>
    /// Навешивание баффа по сети.
    /// </summary>
    private void OnBuffStart(EventId eid, EventInfo ei)
    {
        /*
        0 - на кого вешаем бафф,
        1 - кто вешает бафф,
        2 - ID расходки, содержащей бафф и ID(тип) баффа (new []{myConsumable.ID, myConsumable.Buffs[i].ID}),
        */
        
        EventInfo_U info = (EventInfo_U)ei;

        if ((int)info[0] != data.playerId)
        {
            return;
        }
        
        int consumableID = ((int[])info[2])[0];

        Buff buff = BuffDispatcher.GetNewBuff(consumableID, (Setting)((int[])info[2])[1], ((int[])info[2])[2]);
        buff.Settings[Setting.Duration].Set(((int[])info[2])[3]);
        //Debug.LogError(buff.name + ", timer: " + buff.Time);
        buff.performerID = (int) info[1];
        buff.forShells = false;//КОСТЫЛЬ?
        buff.ConsumableOwnerID = consumableID;
        StaticContainer.Get<IBuffDispatcher>(StaticType.BuffDispatcher).AddBuff(this, buff, buff.IsLocal);
    }

    /// <summary>
    /// Навешивание баффа по сети.
    /// </summary>
    private void OnBuffStop(EventId eid, EventInfo ei)
    {
        /*
        [0] - с кого снимаем бафф,
        [1][0] - id расходки, которая хранит бафф
        [1][1] = тип баффа
        [1][2] = id расходки, с которой применили бафф
        */

        EventInfo_U info = (EventInfo_U)ei;

        if ((int)info[0] != data.playerId)
        {
            return;
        }

        Buff buff = BuffDispatcher.GetNewBuff(((int[])info[1])[0], (Setting)((int[])info[1])[1], ((int[])info[1])[2]);
        StaticContainer.Get<IBuffDispatcher>(StaticType.BuffDispatcher).RemoveBuff(this, buff.Type, buff.isNegative);
        //Debug.LogError("NET Buff: " + buff.name + ", victim: " + name + ", playerID: " + data.playerId, gameObject);
    }

    public bool HasActiveBuff(Setting buffType, bool isNegative)
    {
        if (StaticType.BuffDispatcher.Instance<IBuffDispatcher>().HasFakeBuff(this, buffType, isNegative))
        {
            return true;
        }

        if (isNegative)
        {
            for (int i = 0; i < activeDebuffs.Count; i++)
            {
                if (activeDebuffs[i].Type == buffType)
                {
                    return true;
                }
            }
        }
        else
        {
            for (int i = 0; i < activeBuffs.Count; i++)
            {
                if (activeBuffs[i].Type == buffType)
                {
                    return true;
                }
            }
        }

        return false;
    }

    public virtual void ReanimateBot()
    {
        Dispatcher.Subscribe(EventId.TankKilled, BotAI.CurrentBehaviour.OnVehicleKilled);
        Dispatcher.Subscribe(EventId.TankTakesDamage, BotAI.CurrentBehaviour.OnVehicleTakesDamage);
        Dispatcher.Subscribe(EventId.TankLeftTheGame, BotAI.CurrentBehaviour.OnVehicleLeftTheGame, 4);
        Dispatcher.Subscribe(EventId.BonusDestroyed, BotAI.CurrentBehaviour.OnBonusDestroyed, 4);
    }

    protected virtual void OnTriggerEnter(Collider collider)
    {
        if (!IsMine)
        {
            return;
        }

        if (!IsAvailable)
        {
            return;
        }
        
        if (waterRoutine == null && collider.CompareTag("Water"))
        {
            inWater = true;
            waterRoutine = StartCoroutine(WaterRoutine());            
        }
    }

    private IEnumerator WaterRoutine()
    {
        WaitForSeconds waiter = new WaitForSeconds(1);
        yield return waiter;

        if (!inWater)
        {
            waterTimer.Refresh();
            waterRoutine = null;
            yield break;
        }

        if (debuffWater == null)
        {
            debuffWater = new BuffSimple(Setting.Water, - 1, "UI_Debuff_Water", "debaf_icon_5_1", waterTimer.Current, Indicator, true);
        }
        else
        {
            debuffWater.Time = waterTimer.Current;
        }

        Event(Message.DeBuff, debuffWater, true);
        bool breaked = false;

        //Debug.LogErrorFormat("'{0}' is in water! Run!", Data.Nick);

        while (waterTimer.Current > 0)
        {
            if (!inWater)
            {
                float delayTimer = 1.5f;
                while (!inWater)
                {
                    if (delayTimer > 0)
                    {
                        delayTimer -= Time.deltaTime;
                        yield return new WaitForEndOfFrame();
                        continue;
                    }

                    breaked = true;
                    break;
                }

                if (breaked)
                {
                    break;
                }
            }


            waterTimer.Current--;
            yield return waiter;
        }

        Event(Message.DeBuff, debuffWater, false);

        if (!breaked && inWater)
        {
            ChangeHitPoints(StaticType.BattleController.Instance<IBattleController>().MyPlayerId, HPSystem.Armor, transform.position, GunShellInfo.ShellType.Buff);
            //Dispatcher.Send(EventId.TankKilled, new EventInfo_II(Data.playerId, Data.playerId));
        }

        waterRoutine = null;
        waterTimer.Refresh();
    }

    protected virtual void OnTriggerExit(Collider collider)
    {
        if (!IsMine)
        {
            return;
        }

        if (!IsAvailable)
        {
            return;
        }

        if (debuffWater != null && waterRoutine != null && collider.CompareTag("Water"))
        {
            inWater = false;
            //StopCoroutine(waterRoutine);
            //waterRoutine = null;
            //Event(Message.DeBuff, debuffWater, false);
        }
    }

    public abstract void UpdateBotAssets(VehicleController nativeController);

    protected virtual void OnTargetAimed(EventId id, EventInfo ei)
    {
        var info = (EventInfo_IIB)ei;

        if (info.int1 == data.playerId)
        {
            TargetAimed = info.bool1;
        }
    }

    protected virtual void OnVehicleTakesDamage(EventId id, EventInfo ei)
    {
        EventInfo_U info = (EventInfo_U)ei;
        int damage = (int)info[1];
        int attackerId = (int)info[2];

        //if (!PhotonView.isMine)
        //{
        //    Debug.LogError(name + " get damage: " + damage, this);
        //    //if (attackerId == PhotonNetwork.player.ID)
        //    //{
        //    //    StaticContainer.BattleController.Units[attackerId].Event(Message.StatisticUpdate, StatisticParameter.Kills, vehicleBattle.ID, 1f, StatisticUpdateMode.Addition);
        //    //}
        //    Event(Message.Hit, damage);
        //    return;
        //}

        int victimId = (int)info[0];

        if (victimId != data.playerId)
        {
            return;
        }
        
        //Debug.LogError((name + " OnVehicleTakesDamage: " + HPSystem.Armor + "; Delta: " + damage).FormatString("color:orange"), this);

        if (!IsBot)
        {
            if (attackerId > 0)
            {
                Debug.LogWarning("TakeDamage! Attacker: " + StaticContainer.BattleController.Units[attackerId].UnitBattle.Name + ", dmg: " + damage);
            }

            //Debug.LogError("2 Send HIT: " + (Vector3)info[4]);
            Event(Message.Hit, this, damage, attackerId, (Vector3)info[4], Settings[Setting.HP], 0.05f);
        }

        if (isAvailable && HPSystem.Armor <= 0)
        {
            Dispatcher.Send(EventId.TankKilled, new EventInfo_II(data.playerId, attackerId), Dispatcher.EventTargetType.ToAll);

            if (IsMine)
            {
                Event(Message.StatisticUpdate, StatisticParameter.Deaths, 1f);
            }

            if (attackerId == PhotonNetwork.player.ID && attackerId != victimId)
            {
                StaticContainer.BattleController.Units[attackerId].Event(Message.StatisticUpdate, StatisticParameter.Kills, unitBattle.ID, 1f, damage);
            }
        }

        //Event(Message.Hit, Settings[Setting.HP], ((Vector3)info[4] - transform.position).normalized, this);
        

        switch ((GunShellInfo.ShellType)(int)info[3])
        {
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

    protected virtual void SetEngineNoise(float t)
    {
        if (IsBot || !engineAudio)
        {
            return;
        }

        engineAudio.pitch = Mathf.Lerp(1f, 1.15f, t);
        engineAudio.volume = global::Settings.SoundVolume * Mathf.Lerp(0.5f, 1, t);
    }

    public virtual void StartBurst()
    {
    }

    public virtual void StopBurst()
    {
    }

    protected virtual void ApplyAvailability()
    {
        IsInParallelWorld = !isAvailable;
        isExploded = !isAvailable;
        rigidbody.isKinematic = !PhotonView.isMine || !isAvailable;
        Dispatcher.Send(EventId.TankAvailabilityChanged, new EventInfo_II(data.playerId, isAvailable ? 1 : 0));

        if (engineAudio)
        {
            if (isAvailable)
            {
                engineAudio.Play();
            }
            else
            {
                engineAudio.Stop();
            }
        }

        if (turretAudio != null && !IsAvailable && turretAudio.isPlaying)
        {
            turretAudio.Stop();
        }

        if (PhotonView.isMine)
        {
            if (!IsBot)
            {
                if (player != null && player.IsLocal && (!player.CustomProperties.ContainsKey("ex") ||
                                                         (player.CustomProperties["ex"] != null &&
                                                          (bool)player.CustomProperties["ex"] != IsAvailable)))
                {
                    player.SetCustomProperties(new Hashtable { { "ex", IsAvailable } });
                }
            }
            else
            {
                if (PhotonNetwork.room != null &&
                    (!PhotonNetwork.room.CustomProperties.ContainsKey(KeyForBotExistance) ||
                     (PhotonNetwork.room.CustomProperties[KeyForBotExistance] != null &&
                      (bool)PhotonNetwork.room.CustomProperties[KeyForBotExistance] != IsAvailable)))
                {
                    PhotonNetwork.room.SetCustomProperties(new Hashtable { { KeyForBotExistance, IsAvailable } });
                }
            }
        }

        //if (indicator != null)
        //{
        //    indicator.Hidden = !IsAvailable;
        //}
    }

    protected virtual void SetEngineAudio()
    {
        if (!PhotonView.isMine || IsBot)
        {
            return;
        }

        engineAudio = gameObject.AddComponent<AudioSource>();
        engineAudio.Play();
    }

    protected virtual void SetTurretAudio()
    {
        if (!PhotonView.isMine || turretRotationSound == null)
        {
            return;
        }
        turretAudio = gameObject.AddComponent<AudioSource>();
        SetAudioParams(ref turretAudio, turretRotationSound, true, AudioRolloffMode.Linear, 25, 0, global::Settings.SoundVolume * 0.55f);
    }

    protected void SetAudioParams(ref AudioSource audio, AudioClip clip, bool loop, AudioRolloffMode rolloffMode, float maxDistance, float dopplerLevel, float volume = -1, float spatialBlend = -1) 
    {
        audio.clip = clip;
        audio.loop = loop;
        audio.rolloffMode = rolloffMode;
        audio.maxDistance = maxDistance;
        audio.dopplerLevel = dopplerLevel; 
        if (volume > -1) 
        { 
            audio.volume = volume; 
        }
        if (spatialBlend > -1) 
        {
            audio.spatialBlend = spatialBlend;
        }
 
    }

    protected void StoreVehiclePosition()
    {
        if (transform.position != storedVehiclePosition)
        {
            odometer += Vector3.Distance(rigidbody.position, storedVehiclePosition)*OdometerRatio;
        }

        storedVehiclePosition = transform.position;
    }

    public void MarkActivity()
    {
        inactivityTime = 0;
    }
    
    protected void RemoveAllItems()
    {
    }

    private void OnMainVehicleAppearedForTM(EventId id, EventInfo ei)
    {
        SetMembership();
    }

    private void OnSettingsSubmitted(EventId id, EventInfo ei)
    {
        //foreach (AudioSource audioSource in GetComponents<AudioSource>())
        //{
        //    audioSource.volume = global::Settings.SoundVolume;
        //}
    }

    private void SetMembership()
    {
        if (IsMine)
        {
            ownLayer = LayerMask.NameToLayer("Player");
        }
        else
        {
            ownLayer = IsBot ? BotDispatcher.GetNewBotLayer(Team) : LayerMask.NameToLayer(IsMainsFriend ? "Friend" : "Enemy");
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
                {
                    IsAvailable = false;
                }
            }
            else
            {
                //Бот по сети
                Hashtable roomProperties = PhotonNetwork.room.CustomProperties;
                if (roomProperties.ContainsKey(KeyForBotExistance) && roomProperties[KeyForBotExistance] != null)
                {
                    IsAvailable = (bool) roomProperties[KeyForBotExistance];
                    existanceSynchronized = true;
                }
                else
                {
                    IsAvailable = false;
                }
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
        
        if (GameData.Mode == GameData.GameMode.Team)
        {
            Dispatcher.Send(EventId.TeamChange, new EventInfo_SimpleEvent());
        }

        if (!IsMine)
        {
            tag = Tag.Items[IsMainsFriend ? Tag.Key.Friend : Tag.Key.Enemy];
        }

        StartCoroutine(ResetMasks_Coroutine());
    }    

    private void FillVehicleData()
    {
        BattleController.Instance.OnPlayerPropertiesChanged(player.ID, player.CustomProperties);
    }

    public void MoveClone()
    {
        if (isExploded)
        {
            transform.position = CorrectPosition;
            transform.rotation = CorrectRotation;

            if (Turret)
            {
                Turret.localEulerAngles = Vector3.zero;
            }

            return;
        }

        if (transform.position == CorrectPosition && transform.rotation == CorrectRotation)
        {
            transform.Translate(correctVelocity * deltaTime);
        }
        else
        {
            currentCorrection = Mathf.Clamp01(currentCorrection + deltaTime / 0.2f);
            transform.position = Vector3.Lerp(transform.position, CorrectPosition, currentCorrection);
            transform.rotation = Quaternion.Lerp(transform.rotation, CorrectRotation, currentCorrection);
        }

        if (Turret != null)
        {
            Turret.localEulerAngles = new Vector3(0, Mathf.MoveTowardsAngle(Turret.localEulerAngles.y, CorrectTurretAngle, TurretSpeed * TurretRotationSpeedQualifier * deltaTime), 0);
        }

        if (Cannon != null)
        {
            Cannon.localEulerAngles = new Vector3(Mathf.MoveTowardsAngle(Cannon.localEulerAngles.x, CorrectCannonAngle, TurretSpeed * CannonRotationSpeedQualifier * deltaTime), 0, 0);
        }
    }

    protected virtual void TakeExperienceBonus(int amount)
    {
        if (PhotonView.isMine)
        {
            ScoreCounter.ScoreInto(this, amount);
            if (IsMine)
            {
                Dispatcher.Send(EventId.ExperienceAcquired, new EventInfo_I(amount));
            }
        }
    }

    protected virtual void TakeGoldBonus(int amount)
    {
        if (IsMine)
        {
            Dispatcher.Send(EventId.GoldAcquired, new EventInfo_I(amount));
        }
    }

    protected virtual void TakeSilverBonus(int amount)
    {
        if (IsMine)
        {
            Dispatcher.Send(EventId.SilverAcquired, new EventInfo_I(amount));
        }
    }

    protected virtual void TakeHealthBonus()
    {
        if (!PhotonView.isMine)
        {
            return;
        }

        HPSystem.SetArmor(MaxArmor, -1);
        if (!IsBot)
        {
            player.SetCustomProperties(new Hashtable {{"hl", HPSystem.Armor } });
        }
        else
        {
            Hashtable properties = new Hashtable {{KeyForBotHealth, HPSystem.Armor } };
            PhotonNetwork.room.SetCustomProperties(properties);
        }
    }

    protected virtual void TakeFuelBonus()
    {
        if (IsMine)
        {
            Dispatcher.Send(EventId.FuelAcquired, new EventInfo_I(1));
        }
    }

    protected virtual void TakeGoldRushBonus()
    {
        if (!PhotonView.isMine || !GameData.IsGoldRushEnabled)
        {
            return;
        }

        if (BattleController.TimeRemaining < GameData.GoldRushMinTime)
        {
            BattleController.Instance.ProlongGameForFree(GameData.GoldRushMinTime - BattleController.TimeRemaining);
            //TopPanelValues.ShowCriticalTime(false);
        }

        GoldRush.AwardPermission = true;
        Hashtable properties = new Hashtable();
        properties.Add("goldLeader", data.playerId);
        PhotonNetwork.room.SetCustomProperties(properties);

        MakeRespawn(false, false, false);
    }

    protected virtual void CannonRotation()
    {
        if (Cannon == null)
        {
            return;
        }

        float delta = CannonAxisControl;
        if (Mathf.Abs(delta) >= 1)
        {
            delta = Mathf.Clamp(delta * cannonSpeed * deltaTime, -Mathf.Abs(angleXDelta), Mathf.Abs(angleXDelta));
        }

        cannonLocalEulerAngles.x += delta;
        if (cannonLocalEulerAngles.x > 180)
        {
            cannonLocalEulerAngles.x -= 360;
        }

        if (cannonLocalEulerAngles.x > verticalAngles.Max)
        {
            cannonLocalEulerAngles.x = verticalAngles.Max;
        }

        if (cannonLocalEulerAngles.x < verticalAngles.Min)
        {
            cannonLocalEulerAngles.x = verticalAngles.Min;
        }

        Cannon.localEulerAngles = cannonLocalEulerAngles;
    }

    private void PlayTurretSounds(bool turretIdle)
    {
        if (IsBot)
        {
            return;
        }

        if (turretIdle && !isTurretIdleFrameBefore)
        {
            Dispatcher.Send(EventId.StopTurretRotation, new EventInfo_I(data.playerId));
        }
        else if (!turretIdle && isTurretIdleFrameBefore)
        {
            Dispatcher.Send(EventId.StartTurretRotation, new EventInfo_I(data.playerId));
        }

        if (turretAudio == null)
        {
            return;
        }

        if (turretIdle)
        {
            if (turretAudio.isPlaying)
            {
                turretAudio.Stop();
            }
        }
        else
        {

            if (!turretAudio.isPlaying)
            {
                turretAudio.Play();
            }
        }
    }

    public virtual void TurretRotation()
    {
        if (Turret == null)
        {
            return;
        }

        if (IsBot)
        {
            targetPoint = TargetPosition;
        }

        CannonRotation();

        float deltaRotation = 0;

        float currentTurretRotationY = turretLocalEulerAngles.y;
        bool turretIdle = HelpTools.Approximately(lastTurretLocalRotationY, currentTurretRotationY);

        PlayTurretSounds(turretIdle);

        isTurretIdleFrameBefore = turretIdle;
        lastTurretLocalRotationY = turretLocalEulerAngles.y;

        float turretAxisControl = TurretAxisControl;

        if (!HelpTools.Approximately(turretAxisControl, 0))
        {
            deltaRotation = turretAxisControl;
        }

        if (HelpTools.Approximately(deltaRotation, 0))
        {
            return;
        }

        float targetAngle = Mathf.Abs(angleYDelta);
        float maxRotationAngle = Mathf.Min(targetAngle, TurretSpeed * TurretRotationSpeedQualifier * deltaTime);

        float realRotation = turretAxisControl;
        if (Mathf.Abs(realRotation) >= 1)
        {
            realRotation = Mathf.Clamp(deltaRotation * maxRotationAngle, -maxRotationAngle, maxRotationAngle);
        }

        turretLocalEulerAngles.y += realRotation;
        Turret.localEulerAngles = turretLocalEulerAngles;
    }

    private void OnReconnect(EventId eid, EventInfo ei)
    {
    }

    private void CheckAllBotStats()
    {
        if (PhotonNetwork.room.CustomProperties == null)
        {
            return;
        }
        int param = 0;

        CheckBotStat(KeyForBotScore, out param);
        statistics.Stats[StatisticParameter.Experience] = param;

        CheckBotStat(KeyForBotDeaths, out param);
        statistics.Stats[StatisticParameter.Deaths] = param;

        CheckBotStat(KeyForBotKills, out param);
        statistics.Stats[StatisticParameter.Kills] = param;

        CheckBotStat(KeyForBotDamage, out param);
        statistics.Stats[StatisticParameter.Damage] = param;
    }

    private void CheckBotStat(string propertyKey, out int statField)
    {
        Hashtable properties = PhotonNetwork.room.CustomProperties;
        object value;
        statField = properties.TryGetValue(propertyKey, out value) && value != null ? (int)value : 0;
    }

    private void SetBotKeys()
    {
        KeyForBotDamage = string.Format("btdm{0}", data.playerId); 
        KeyForBotDeaths = string.Format("btdt{0}", data.playerId);
        KeyForBotKills = string.Format("btkl{0}", data.playerId);
        KeyForBotScore = string.Format("btsc{0}", data.playerId);
        KeyForBotHealth = string.Format("bthl{0}", data.playerId);
        KeyForBotExistance = string.Format("btex{0}", data.playerId);
        KeyForBotSpeed = string.Format("btsp{0}", data.playerId);
        KeyForBotAttack = string.Format("btat{0}", data.playerId);
        KeyForBotRoF = string.Format("btrf{0}", data.playerId);
        KeyForBotTurretSpeed = string.Format("bttsp{0}", data.playerId);
    }

    private void ResetMasks()
    {
        if (StaticType.BattleController.Instance<IBattleController>().FriendlyFire)
        {
            EnemyLayerMask = MiscTools.GetLayerMask("Player", "Friend", "Enemy");
        }
        else
        {
            EnemyLayerMask = IsMine || IsMainsFriend ? MiscTools.GetLayerMask("Enemy") : MiscTools.GetLayerMask("Player", "Friend");
        }

        EnemyLayerMask |= (GameData.Mode == GameData.GameMode.Team)
                ? BotDispatcher.GetBotsLayerMaskForTeam(1 - data.teamId)
                : MiscTools.ExcludeLayerFromMask(BotDispatcher.BotsCommonMask, ownLayer); //Exclude на тот случай, если сам - бот.
        
        if (IsMine)
        {
            hitMask = MiscTools.ExcludeLayersFromMask(StaticContainer.BattleController.HitMask, "Player");
            //aimingObstacleMask = hitMask;
        }
        else
        {
            hitMask = IsBot ? (LayerMask)MiscTools.ExcludeLayerFromMask(StaticContainer.BattleController.HitMask, ownLayer) : StaticContainer.BattleController.HitMask;
            //aimingObstacleMask = hitMask;
        }

        othersLayerMask = MiscTools.GetLayerMask(Layer.Key.Enemy, Layer.Key.Friend, Layer.Key.Player) | BotDispatcher.BotsCommonMask;
        othersLayerMask = MiscTools.ExcludeLayerFromMask(othersLayerMask, ownLayer);
    }

    private void RoomPropsChanged(EventId id, EventInfo ei)
    {
        CheckAllBotStats();
        Dispatcher.Unsubscribe(EventId.PhotonRoomCustomPropertiesChanged, RoomPropsChanged);
    }

    private void SetStartBotProperties()
    {
        Hashtable properties =
            new Hashtable
            {
                {KeyForBotDamage, 0 },
                {KeyForBotHealth, (int)data.maxArmor},
                {KeyForBotSpeed, data.movingSpeed},
                {KeyForBotScore, 0},
                {KeyForBotKills, 0},
                {KeyForBotDeaths, 0},
                {KeyForBotAttack, (int)data.attack},
                {KeyForBotRoF, ROF},
                {KeyForBotTurretSpeed, data.turretSpeed}
            };

        PhotonNetwork.room.SetCustomProperties(properties);
    }

    public void GiveDamage(int victimId, float damage, int consumableId)
    {
        if (!IsBot)
        {
            Player.SetCustomProperties(new Hashtable { { "dm", Statistics.Stats[StatisticParameter.Damage] + (int)damage } });
            Event(Message.StatisticUpdate, StatisticParameter.Damage, victimId, damage, consumableId);
        }
        else
        {
            Hashtable properties = new Hashtable { { KeyForBotDamage, Statistics.Stats[StatisticParameter.Damage] + (int)damage } };
            PhotonNetwork.room.SetCustomProperties(properties);
        }
    }

    private void OnNewLayerInTeamMask(EventId id, EventInfo ei)
    {
        if (GameData.Mode == GameData.GameMode.Deathmatch)
        {
            return;
        }

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
        {
            return;
        }

        EventInfo_II info = ei as EventInfo_II;
        int teamId = info.int1;
        int layer = info.int2;

        if (teamId != data.teamId)
        {
            EnemyLayerMask = EnemyLayerMask & ~(1 << layer);
        }
    }

    private IEnumerator ResetMasks_Coroutine()
    {
        yield return null;
        ResetMasks();
    }

   /* protected ParticleSystem[] FindEffects(Transform baseRoot, string parentName)
    {
        Transform root = baseRoot.Find(parentName);
        if (root == null)
        {
            return null;
        }
        ParticleSystem[] particleSystems = root.GetComponentsInChildren<ParticleSystem>();

        if (particleSystems == null)
        {
            DT.LogError("Cannot collect '{0}' objects in {1}", parentName, name);
            return null;
        }

        return particleSystems;
    }*/
}