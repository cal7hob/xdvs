using UnityEngine;
using Pool;
using System.Collections.Generic;
using System.Collections;

public partial class SoldierController : VehicleController
{
    [Header("Настройки CodeOfWar")]

    [Header("Управление")]
    public float rotationSpeedInZoom = 0.7f;
    [Space]
    [Header("GunPoint")]

    public Animator animator;
    public Transform gunAiming;
    public Transform gunForward;

   // protected SoldierAnimatorController animatorController;
    [SerializeField]
    protected IKController ikController;

    [SerializeField]
    protected WeaponsSpawner weaponSpawner;

    protected float xAxisAcceleration;
    protected float yAxisAcceleration;
    protected float deltaRotationMagnitude;
    protected Vector3 deltaRotationAxis;
    protected Quaternion lastRotation;
    protected Camera camera2d;

    private float atkLim;
    private float rfLim;
    private float speedLim;
    private float relLim;
    private float magazineLim;
    private float armorLim;
    private float armorMaxLim;
    private bool banned;
    private bool readyForRespawn;
    private VehicleStateDispatcher stateDispatcher;

    [SerializeField]
    private CapsuleCollider bodyCollider;
    [SerializeField]
    private BoxCollider headCollider;

    private Transform gunLocation;
    private Transform rightHand;

    private TankData reserveData = new TankData();

    protected override Transform Bumper
    {
        get { return bumper = bumper ?? transform.Find("Bumper"); }
    }

    public override Collider CritCollider
    {
        get { return headCollider; }
    }
    public override Collider BodyCollider
    {
        get { return bodyCollider; }
    }

    public override bool IsVisible
    {
        get { return base.IsVisible; }
        set
        {
            base.IsVisible = value;

            foreach (var weaponRenderer in weaponSpawner.weaponRenderers)
            {
                if (weaponRenderer != null)
                {
                    weaponRenderer.enabled = value;
                }
            }
        }
    }
    
    public WeaponsSpawner WeaponsSpawner { get { return weaponSpawner; } }

    public IKController IkController { get { return ikController; } }

    public override Vector3 AngularVelocity
    {
        get
        {
            if (PhotonView == null)
            {
                return Vector3.zero;
            }

            if (IsMine)
            {
                return rb.angularVelocity;
            }

            return ((deltaRotationAxis * deltaRotationMagnitude) / Time.deltaTime) * Mathf.Deg2Rad;
        }
    }

    public override Transform Turret
    {
        get { return turret; }
    }

    public override Transform Body { get { return animator.transform; } }

    public override Transform ShotPoint
    {
        get { return shotPoint; }
    }

    public override float YAxisAcceleration { get { return yAxisAcceleration; } }

    public override float XAxisAcceleration { get { return xAxisAcceleration; } }

    public override void UpdateBotPrefabs(VehicleController nativeController)
    {

    }
    
    public override Transform CannonEnd
    {
        get { return cannonEnd; }
    }

    protected override float ZoomRotationSpeed
    {
        get
        {
            return Mathf.Min(maxSpeed * rotationSpeedQualifier, rotationSpeedInZoom) * xAxisAcceleration;
        }
    }

    protected override float RotationSpeed
    {
        get { return maxSpeed * rotationSpeedQualifier * xAxisAcceleration; }
    }

    public override float YAxisControl
    {
        get
        {
            return XDevs.Input.GetAxis("Move Forward/Backward");
        }
    }


    public void GetWeaponParams(Transform weapon, Transform leftTarget, Transform rightTarget, Transform lookTarget,Transform cannonEnd, Transform gunLocation, AudioClip reloadOnSound, AudioClip reloadOffSound) 
    {
        this.weapon = weapon;
        this.cannonEnd = cannonEnd;
        this.gunLocation = gunLocation;
        this.reloadOnSound = reloadOnSound;
        this.reloadOffSound = reloadOffSound;
        ikController.SetTargets(leftTarget, rightTarget, lookTarget);
    }

    public void SetDeathAnim(bool on)
    {
        SetDeath(on);
    }
   
    protected override void OnPhotonInstantiate(PhotonMessageInfo info)
    {
        camera2d = BattleGUI.Instance.GuiCamera;
        stateDispatcher = new VehicleStateDispatcher();
        stateDispatcher.Init(this);

        base.OnPhotonInstantiate(info);
        Dispatcher.Subscribe(EventId.MyTankShots, CheckCheat);
        Dispatcher.Subscribe(EventId.MassRespawn, ForceRespawn);
        bonusUse = new FullBonusUse(this);
        rightHand = animator.GetBoneTransform(HumanBodyBones.RightHand);
        if (!IsMine)
        {
            animator.applyRootMotion = false;
        }
        steps = BattleController.Instance.stepSoundItem.steps;
        MakeItEasier();
    }

    protected override IShootable GetTurret(VehicleController vehicle, Animation shootAnimation)
    {
        FirearmsController firearms = gameObject.AddComponent<FirearmsController>();
        firearms.SetParams(this);
        return firearms;
    }

    void Start()
    {
        if (ProfileInfo.IsBattleTutorial)
        {
            data.rof = 450;
            data.attack = 100;
        }
        if (reserveData.magazine == 0 && data.magazine != 0)
        {
            reserveData.magazine = data.magazine;
            reserveData.reloadTime = data.reloadTime;
            reserveData.speed = data.speed;
            reserveData.maxArmor = data.maxArmor;
            reserveData.armor = data.armor;
            reserveData.rof = data.rof;
            reserveData.attack = data.attack;
        }

        atkLim = reserveData.attack;
        rfLim = reserveData.rof;
        speedLim = reserveData.speed;
        relLim = reserveData.reloadTime;
        magazineLim = reserveData.magazine;
        armorLim = reserveData.armor;
        armorMaxLim = reserveData.armor;
        atkLim *= 1.3f;// Расчёт на криты
        rfLim *= 1.3f; //Расчёт на лаги
    }

    protected override void OnDestroy()
    {
        Dispatcher.Unsubscribe(EventId.MyTankShots, CheckCheat);
        Dispatcher.Unsubscribe(EventId.MassRespawn, ForceRespawn);
        base.OnDestroy();
    }

    private void ForceRespawn(EventId _id, EventInfo _info)
    {
        if (gameObject == null)
        {
            return;
        }
        if (readyForRespawn)
        {
            return;
        }
        if (!PhotonNetwork.isMasterClient)
        {
            return;
        }
        readyForRespawn = true;
        Invoke("Respawn", BattleController.TimeForRespawn);
    }

    private void Respawn()
    {
        readyForRespawn = false;
        MakeRespawn(forced: true, restoreLife: true, firstTime: false);
    }
    protected override void Update()//protected override 
    {
        if (!PhotonView)
        {
            return;
        }

        if (!Debug.isDebugBuild)//может эта часть тоже должна выполняться для isMine
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

        if (!IsAvailable)//умерли уже, какое уж там прицеливание
        {
            return;
        }

        if (IsMine)
        {
            //if (aimingController != null)
            //{
            //  //  aimingController.Aiming();
            //}
            UpdateEffects();
        }
        else
        {
            MoveClone();
        }

        turretController.FullRealoadingUpdate();

        if (IsMain)
        {
            //вроде как центрирование пушки у нас теперь не нужно
            /*if (XDevs.Input.GetButtonDown("Center Turret") || (!ProfileInfo.isFireOnDoubleTap && XDevs.Input.GetButtonDown("Double Tap")))
              {
                  turretController.TurretCentering = true;
              }*/

#if UNITY_EDITOR

            if (Input.GetKeyDown(KeyCode.Backspace))
            {
                MakeRespawn(forced: true, restoreLife: false, firstTime: false);
            }
            if (Input.GetKeyDown(KeyCode.B))
            {
                BattleController.EndBattle(BattleController.EndBattleCause.Timeouted);
            }
#endif
            //выполняется для себя самого и всех своих ботов
                Gunsight();
        }
    }

    protected override void FixedUpdate()
    {
        if (!PhotonView || !IsAvailable)
        {
            return;
        }
        if (IsMine)
        {
            MovePlayer();
        }
        else
        {
            StoreCloneRotation();
        }
    }

    private void CheckCheat(EventId _id, EventInfo _info)
    {
        //  Debug.LogError("CHECKCHEAT BEFORE");
        if (IsBot || !IsMine || ProfileInfo.IsBattleTutorial)
        {
            return;
        }
#if UNITY_EDITOR
        if (cheatActivated)
        {
            return;
        }
#endif
        // Debug.LogError("CHECKCHEAT");
        //Подбор ящика с уроном увеличивает урон на 50% от полного. 
        //Подбор ящика с перезарядкой увеличивает её тоже на 50% от полной
        //RoF за голду после смерти - увеличивает его на 100%
        //Урон за голду после смерти - увеличивает его на 50%
        //  Debug.LogFormat("LIMITS {0}  {1}  {2}", speedLim, magazineLim, relLim);
        //  Debug.LogFormat(" DATA {0}  {1}  {2}", data.speed, data.magazine, data.reloadTime);
        var tempAtkLim = atkLim;
        var tempRfLim = rfLim;
        foreach (var effect in Effects.Values)
        {
            if (effect.Type == VehicleEffect.ParameterType.Attack)
            {
                tempAtkLim *= effect.ModValue;
            }
            if (effect.Type == VehicleEffect.ParameterType.RoF)
            {
                tempRfLim *= effect.ModValue;
            }
            //Debug.LogError(effect.ModType.ToString() + effect.ModValue + (effect.Type.ToString()));
            //     Debug.LogError("atklim " + atkLim + " rflim " + rfLim);

        }


        if (data.armor > armorLim)
        {
            Debug.LogError("BAN FOR ARMOR CHEAT");
            Debug.LogError(data.armor + " " + armorLim);
            BanMe();
        }
        if (data.maxArmor > armorMaxLim)
        {
            Debug.LogError("BAN FOR MAX ARMOR CHEAT");
            Debug.LogError(data.maxArmor + " " + armorMaxLim);
            BanMe();
        }
        if (data.attack > tempAtkLim)
        {
            Debug.LogError("BAN FOR ATTACK CHEAT");
            Debug.LogError(data.attack + " " + tempAtkLim);
            BanMe();
        }
        if (data.speed > speedLim)
        {
            Debug.LogError("BAN FOR SPEED CHEAT");
            Debug.LogError(data.speed + " " + speedLim);
            BanMe();
        }
        if (data.reloadTime < relLim)
        {
            Debug.LogError("BAN FOR RELOAD CHEAT");
            Debug.LogError(data.reloadTime + " " + relLim);
            BanMe();
        }
        if (data.magazine > magazineLim)
        {
            Debug.LogError("BAN FOR MAGAZINE CHEAT");
            Debug.LogError(data.magazine + " " + magazineLim);
            BanMe();
        }
        if (data.rof > tempRfLim)
        {
            Debug.LogError("BAN FOR ROF CHEAT");
            Debug.LogError(data.rof + " " + tempRfLim);
            BanMe();
        }
    }

    private void BanMe()
    {
        if (banned)
        {
            return;
        }

        foreach (var effect in Effects.Values)
        {
            Debug.LogError(" Current effects TYPE_MOD_VALUE" + effect.Type.ToString() + effect.ModType.ToString() + effect.ModValue.ToString());

        }

        banned = true;
        BattleController.ProvokeServer();
    }

    [PunRPC]
    public override void Respawn(Vector3 position, Quaternion rotation, bool restoreLife, bool firstTime)
    {
        if (StatTable.instance.exitCounter.IsActive && BattleController.Instance.BattleMode == GameData.GameMode.Team)
        {
            return;
        }
        if (restoreLife)
        {
            Armor = data.maxArmor;
        }
        Dispatcher.Send(EventId.TankHealthChanged, new EventInfo_II(data.playerId, Armor));
        settingSpawnPosition = true;

        if (!firstTime)
        {
            IsAvailable = true;
            IsAiming = true;
            OnForceResp();
        }

        correctPosition = transform.position = position;
        correctRotation = Body.rotation = rotation;
        correctVelocity = Vector3.zero;
        storedVehiclePosition = transform.position;

        Dispatcher.Send(EventId.TankRespawned, new EventInfo_I(data.playerId));
        Dispatcher.Send(StaticContainer.IsFriendOfMain(this) ? EventId.ShowEnemy : EventId.HideEnemy, new EventInfo_I(data.playerId));

        if (IsMain)
        {
            RemoveAllItems();
            turretController.ResetAimingState();

            Dispatcher.Send(EventId.MyTankRespawned, new EventInfo_SimpleEvent());

            if (respawnSound)
            {
                AudioDispatcher.PlayClipAtPosition(respawnSound, transform.position, transform);
            }
            rb.isKinematic = false;
        }
        bodyCollider.enabled = true;
        headCollider.enabled = true;

        transform.rotation = Quaternion.identity;
        turretController.FullInstantReload();
        yAxisAcceleration = 0;
        xAxisAcceleration = 0;
    }


    [PunRPC]
    public override void UpdateAnimatorParams(int playerId, float speed, float reloadTime)
    {
        // if (data.playerId == playerId)
        {
            UpdateAnimatorParams(speed, reloadTime);
        }
    }

    [PunRPC]
    public override void Death(int playerId)
    {
        if (data.playerId == playerId)
        {
            IsAiming = false;
            rb.isKinematic = true;
            bodyCollider.enabled = false;
            headCollider.enabled = false;
            //Debug.LogFormat("Death {0}", data.playerId);
        }
    }

#if UNITY_EDITOR
    /*void OnDrawGizmos()
    {
        if (IsMine)
        {
            if (!IsBot && weapon != null)
            {
                Gizmos.color = Color.magenta;
                Gizmos.DrawSphere(center: gunSightPoint, radius: 0.1f);
                Gizmos.DrawLine(weapon.transform.position, gunSightPoint);
                Gizmos.color = Color.yellow;
                Gizmos.DrawSphere(center: BattleCamera.Instance.Cam.transform.position + BattleCamera.Instance.Cam.transform.forward * 10, radius: 0.1f);
                Gizmos.DrawLine(BattleCamera.Instance.Cam.transform.position, BattleCamera.Instance.Cam.transform.position + BattleCamera.Instance.Cam.transform.forward * 10);
            }
        }
        else
        {
            if (!IsBot)
            {
                Gizmos.color = Color.green;
                Gizmos.DrawSphere(center: correctCamSinghtPoint, radius: 0.1f);
                Gizmos.DrawLine(weapon.transform.position, correctCamSinghtPoint);
            }
        }
    }*/
#endif
}
