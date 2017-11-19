using System.Collections;
using System.Collections.Generic;
using Rewired;
using Pool;
using AimingStates;
using UnityEngine;

public class WeaponController : TurretController
{
    protected SoldierController soldier;
    

    public WeaponController(VehicleController vehicle, Animation shootAnimation)
        : base(vehicle, shootAnimation)
    {
        this.soldier = vehicle as SoldierController;
        //this.camera = BattleCamera.Instance.Cam.transform;
    }
/*
    protected Transform turret;
    protected AudioClip turretRotationSound;
    protected Animation shootAnimation;
    protected CustomController rewiredConroller;

    private string shotPrefab;
    private VehicleController target;
    private Vector3 targetPosition;
    private float autoAimingAxisValue;

    private readonly Dictionary<int, VehicleEffect> effects = new Dictionary<int, VehicleEffect>(4);
    private readonly RaycastHit[] aimingHits = new RaycastHit[32];
    private const ShellType DEFAULT_SHELL_TYPE = ShellType.Usual;

    protected int currentVictimId;
    protected float lastTurretLocalRotationY;
    protected int currentRocketLaunchPointIndex;
    protected int currentShotPointIndex;
    protected AimPointInfo aimPointInfo;
    protected AudioSource turretAudio;
    protected bool isTurretIdleFrameBefore;
    protected readonly List<AimPointInfo> aimPoints = new List<AimPointInfo>(50);
    //protected Transform turret;
    protected float lastTouchTurretRotation = 0f;

    //-------------------------------------
    private RaycastHit gunsightHit;
    private bool hasHit;
    
    //--------------------------------------
    private Transform camera;

    public Dictionary<ShellType, Weapon> weapons;

    public AimPointInfo AimPoint
    {
        get { return aimPointInfo; }
        set { aimPointInfo = value; }
    }

    public virtual float WeaponReloadingProgress
    {
        get { return weapons[DefaultShellType].ReloadingProgress; }
    }

    public override Vector3 TargetPoint
    {
        get { return soldier.transform.position; }
    }

    protected override float TurretAxisControl
    {
        get { return soldier.TurretAxisControl; }
    }

    protected LayerMask HitMask { get { return soldier.HitMask; } }

    public float TurretRotationZoomSpeedQualifier
    {
        get
        {
            return TargetAimed ? Mathf.Clamp(BattleCamera.Instance.TurretIndicationZoomSqrDist / Vector3.SqrMagnitude(soldier.TargetPosition - soldier.transform.position), 0.2f, 1) * soldier.turretRotationSpeedQualifier : soldier.turretRotationSpeedQualifier * 0.5f;
        }
    }

    public virtual float TurretRotationSpeedQualifier
    {
        get
        {
            return BattleCamera.Instance.IsZoomed ? TurretRotationZoomSpeedQualifier : soldier.turretRotationSpeedQualifier;
        }
    }

    public float MaxShootAngleCos { get; private set; }

    public virtual ShellType DefaultShellType
    {
        get { return DEFAULT_SHELL_TYPE; }
    }

    public bool IsReady()
    {
        return false;
    }

    private void RewiredInputUpdateHandler()
    {
        rewiredConroller.SetAxisValue(autoAimingAxis, autoAimingAxisValue);
    }

    public void OnDestroy()
    {
        ReInput.InputSourceUpdateEvent -= RewiredInputUpdateHandler;
    }

    public void SetAutoAimingTarget(VehicleController target)
    {
        if (!IsAutoAimingOn)
        {
            return;
        }

        autoAimingTarget = target;
        SetFullAutoAiming();
    }

    public void SetVictimId(int id)
    {
        currentVictimId = id;
    }

    public void SetAnimation(Animation animation)
    {
        shootAnimation = animation;
    }

    protected override void FireWithoutShell(bool isBotCheck) 
    {
       /* if (vehicle.aimObject.transform.parent != null) 
        {
            vehicle.aimObject.transform.SetParent(null);
        }* /

        int victimId = 0;        
        int damage = 0;
        bool critHit = false;
        Vector3 hitPoint = Vector3.zero;

        if (hasHit) 
        {
            VehicleController victim = null;
            //для эффекта попадания и только
         /*   vehicle.aimObject.transform.position = GunsightPoint;
            vehicle.aimObject.transform.rotation = Quaternion.LookRotation(gunsightHit.normal);* /
            //

            if (gunsightHit.collider.tag == "CritZone")
            {
                victim = gunsightHit.collider.transform.GetComponentInParent<VehicleController>();
                critHit = true;

                if (isBotCheck && victim.IsBot)
                {
                    victim.BotAI.CurrentBehaviour.OnCritHit(soldier);
                }
            }
            else if ((HitMask & 1 << gunsightHit.collider.gameObject.layer) > 0)
            {
                victim = gunsightHit.collider.transform.GetComponentInParent<SoldierController>();
                if (victim ==null && gunsightHit.collider.transform.parent != null)
                {
                    victim = gunsightHit.collider.transform.parent.GetComponentInParent<SoldierController>();
                }
                
            }

            if (victim /*&& Physics.Raycast(gunsightHit.point - camera.forward * 0.1f, - camera.forward, gunsightHit.distance, BattleController.HitMask)* /)
            {
                hitPoint = victim.transform.InverseTransformPoint(gunsightHit.point);
                victimId = victim.data.playerId;
                damage = victim.CalcDamage(soldier.data.attack, critHit);
            }
            else
            {
                hitPoint = gunsightHit.point;
            }
        }
        //вызов сетевого события
        soldier.PhotonView.RPC("Shoot", PhotonTargets.All, victimId, soldier.data.playerId, gunsightHit.point, gunsightHit.normal, damage, hasHit);
    }
    */
    public override bool Fire()
    {
        return false;
        /* if (!BaseFire())
         {
             return false;
         }
         soldier.MarkActivity();
         BattleGUI.FireButtons[StaticContainer.DefaultShellType].SimulateReloading();
         FireWithoutShell(true);
         if (soldier.IsMain)
         {
             Dispatcher.Send(EventId.MyTankShots, new EventInfo_I((int)StaticContainer.DefaultShellType));
         }
         return true;*/
    }
    /*

/*
    private IEnumerator Reloading()
    {
        BattleController.MyVehicle.Reloading(true);
        while (percentage < 1)
        {
            UpdateBar(ref percentage);
            yield return null;
        }
        reloadingRoutine = null;
        BattleController.MyVehicle.Reloading(false);
        yield break;
    }
    
    private void UpdateBar(ref float percentage)
    {
        percentage = WeaponReloadingProgress;
        if(vehicle.IsMine)
        {
            weaponReloadProgressBar.Percentage = percentage;
            if (turretController.weapons != null && magazine != null)
            {
                magazine.text = turretController.weapons[ShellType.Usual].shotCounter.ToString();
            }
        }
    }* /


    public virtual void SecondaryFire(ShellType shellType, int targetId)
    {
        throw new System.NotImplementedException();
    }
    /*
    public void SetMaxAngleCos(float angleCos)
    {
        MaxShootAngleCos = angleCos;
    }
    * /
    public void FullAutoAim()
    {
        if (!BattleController.visibleEnemies.ContainsKey(autoAimingTarget.data.playerId))
        {
            ResetAimingState();
            return;
        }

        var aimDir = (autoAimingTarget.transform.position - turret.position).normalized;
        autoAimingAxisValue = Vector3.Dot(turret.right, aimDir) * autoAimingSpeedQual;
    }

    public void DefaultAutoAim()
    {
        var aimDir = (soldier.TargetPosition - turret.position).normalized;
        autoAimingAxisValue = Vector3.Dot(turret.right, aimDir) * autoAimingSpeedQual;
    }
    
   
    public virtual void TurretRotation()
    {
        return;
    }*/
}
