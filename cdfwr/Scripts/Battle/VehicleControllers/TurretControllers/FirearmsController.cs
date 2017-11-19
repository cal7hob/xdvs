using System.Collections;
using System.Collections.Generic;
using CodeStage.AntiCheat.ObscuredTypes;
using UnityEngine;
using AimingStates;
using Rewired;
using Vkontakte;

public class FirearmsController : MonoBehaviour, IShootable
{
    private static readonly ObscuredFloat FIRE_RATE_SECONDS = 60.0f;
    public const string autoAimingAxis = "AutoAimingAxis";
    public const int autoAimingSpeedQual = 3;

    private SoldierController soldier;
    private CustomController rewiredConroller;

   // private int currentVictimId;
    private string shotPrefab;
    private Vector3 targetPosition;

    private float autoAimingAxisValue;
    private SelfTankProgressBars progressBar;

    private int shotCounter;
    private float reloadingStartTime;
    private float lastShotTime;
    private float timeInReload;
    private bool reloading = false;
    private int correctOwnerMagazine = 0;
   // private float correctOwnerReload = 0f;
    private bool getdata = false;
    public bool critHit { get; private set; }
    //===================================================================

    public float WeaponReloadingProgress
    {
        get { return ReloadingProgress; }
    }
    public Transform CannonEnd { get; set; }
    public bool TurretCentering
    {
        get;
        set;
    }
    public float MaxShootAngleCos { get; set; }
    protected LayerMask HitMask
    {
        get { return soldier.HitMask; }
    }
    //===================================================================

    void Awake()
    {
        Dispatcher.Subscribe(EventId.ForceReload, ForceReload, 1);
    }
    public FirearmsController(VehicleController vehicle)
    {
        SetParams(vehicle);
    }

    public void SetProgressBar(SelfTankProgressBars progressBar)
    {
        this.progressBar = progressBar;
    }
    public void SetParams(VehicleController vehicle)
    {
        this.soldier = vehicle as SoldierController;

        if (!(vehicle.IsMain && !vehicle.IsBot))
        {
            return;
        }

#if UNITY_WEBGL
        currentAimingState = withoutAutoAimingState;
#else
        if (BattleCamera.Instance.IsMouseControlled)
        {
        }
        else
        {
            rewiredConroller = XDevs.Input.TouchController;
            ReInput.InputSourceUpdateEvent += RewiredInputUpdateHandler;
        }
#endif
    }

    private void RewiredInputUpdateHandler()
    {
        rewiredConroller.SetAxisValue(autoAimingAxis, autoAimingAxisValue);
    }

/*    public void SetVictimId(int id)
    {
        currentVictimId = id;
    }*/

    public void FullInstantReload()
    {
        InstantReload();
    }

    public float GetReloadingTime()
    {
        return ReloadingTimeSeconds;
    }

    public float Progress
    {
        get
        {
            return (float)shotCounter / soldier.data.magazine;
        }
    }

    public bool IsReady
    {
        get
        {
            return Time.time - lastShotTime >= ReloadingTimeSeconds && (!soldier.PhotonView.isMine || !reloading); // Клоны не перезаряжают сами
        }
    }

    public int Counter
    {
        get
        {
            return shotCounter;
        }
    }

    public ObscuredFloat ReloadingTimeSeconds
    {
        get { return FIRE_RATE_SECONDS / soldier.ROF; }
    }

    public void InstantReload()
    {
        reloading = false;
        ReloadingProgress = 1;
        shotCounter = correctOwnerMagazine;
        if (progressBar != null)
        {
            progressBar.UpdateBar(WeaponReloadingProgress, shotCounter);
        }
    }

    int victimId = 0;
    int damage = 0;
    Collider collider_;
    VehicleController victim;

    public bool Fire()//Is this the Visual part? It desables because of low graphic level 
    {
        if (!IsReady) //чтобы был перерыв между выстрелами
        {
            return false;
        }
        if (shotCounter < 1)
        {
            return false;
        }

        soldier.MarkActivity();
        RegisterShot();

        victimId = 0;
        damage = 0;
        collider_ = soldier.AimingHit.collider;

        if (soldier.HasHit && collider_ != null)
        {
           // collider_ = soldier.AimingHit.collider;
            critHit = false;
            victim = null;

            if (collider_.tag.Equals("Landmine"))
            {
                PhotonView view = collider_.GetComponent<PhotonView>();
                Dispatcher.Send
                (
                    EventId.DestroyThisGameObject,
                    new EventInfo_I(view.instantiationId), 
                    Dispatcher.EventTargetType.ToSpecific,
                    view.owner.ID
                );
            }
            else
            {
                victim = collider_.GetComponentInParent<VehicleController>();
                
                if (collider_.tag.Equals("CritZone"))
                {
                    critHit = true;
                }
                /*else //if ((HitMask & 1 << collider_.gameObject.layer) > 0)
                {
                    //victim = collider_.transform.GetComponentInParent<SoldierController>();
                    if (victim == null && collider_.transform.parent != null)
                    {
                        victim = collider_.transform.parent.GetComponentInParent<SoldierController>();
                    }
                }
                    */
                if (victim)
                {
                    victimId = victim.data.playerId;
                    damage = victim.CalcDamage(soldier.data.attack, critHit);
                }
            }
        }
        //вызов сетевого события
        soldier.PhotonView.RPC("Shoot", PhotonTargets.All, victimId, soldier.data.playerId, soldier.gunSightPoint, soldier.AimingHit.normal, damage, soldier.HasHit);

        if (!soldier.IsBot)
        {
            Dispatcher.Send(EventId.MyTankShots, new EventInfo_I());
        }
        return soldier.HasHit;
    }
    //============================================================================================

    public void FullRealoadingUpdate() { }
    public void FillReloadingData(TankData data)
    {
        //correctOwnerReload = data.reloadTime;
        correctOwnerMagazine = data.magazine;
        shotCounter = correctOwnerMagazine;
        getdata = true;
    }

    public void RegisterShot()//вызывается в PrimaryFire и SecondaryFire
    {
        if (!getdata)
        {
            return;
        }

        if (reloading)
        {
            return;
        }

        lastShotTime = Time.time;
        --shotCounter;
        ReloadingProgress = Progress;

        if (progressBar != null)
        {
            progressBar.UpdateBar(Progress, shotCounter);
        }

        if (shotCounter <= 0)
        {
            soldier.ForceReload();
        }
    }

    public void ForceReload(EventId id, EventInfo ei)
    {
        if (!reloading && shotCounter != correctOwnerMagazine)
        {
            soldier.ForceReload();
        }
    }

    public void Reload(float time)
    {
        reloading = true;
        StartCoroutine(Reloading(time));
    }

    public IEnumerator Reloading(float reloadTime)
    {
        soldier.SetOnReloadParams(true);
        shotCounter = 0;
        timeInReload = 0;
        reloadingStartTime = Time.time;
        soldier.PlayReloadSound(true);
        while (timeInReload < reloadTime)//WeaponReloadingProgress < 1
        {
            timeInReload = Time.time - reloadingStartTime;
            ReloadingProgress = timeInReload / reloadTime;
            if (progressBar != null)
            {
                progressBar.UpdateBar(WeaponReloadingProgress, shotCounter);
            }
            yield return null;
        }
        shotCounter = correctOwnerMagazine;
        if (progressBar != null)
        {
            progressBar.UpdateBar(WeaponReloadingProgress, shotCounter);
        }
        soldier.SetOnReloadParams(false);
        reloading = false;
        yield break;
    }

    public float ReloadRemainingSeconds(ShellType shell)
    {
        return ReloadingTimeSeconds * (1 - ReloadingProgress);
    }

    public ObscuredFloat ReloadingProgress
    {
        get; private set;
    }

    public void OnDestroy()
    {
        ReInput.InputSourceUpdateEvent -= RewiredInputUpdateHandler;
        Dispatcher.Unsubscribe(EventId.ForceReload, ForceReload);
    }


    protected AutoAimingState defaultAutoAimingState;
    protected AutoAimingState fullAutoAimingState;
    protected AutoAimingState withoutAutoAimingState;
    protected AutoAimingState currentAimingState;
    protected AutoAimingState onStartAimingState;


    protected void SetAutoAimingState(AutoAimingState state)
    {
        currentAimingState = state;
    }

    public void ResetAimingState()
    {
        SetAutoAimingState(onStartAimingState);
    }
    public void SetFullAutoAiming()
    {
        SetAutoAimingState(fullAutoAimingState);
    }

    //=================================================

    public void TurretRotation() { }
    public void StopTurretAudio() { }
    public void SetTurretAudio() { }
    public void ResetLocalRotation() { }
    public void SetAudioVolume(float volume) { }
    public void SetAnimation(Animation aimation) { }

}
