using CodeStage.AntiCheat.ObscuredTypes;
using UnityEngine;

public class Weapon
{
    public int shotCounter;
    private float reloadingStartTime;
    private float lastShotTime;
    private float timeInReload;
    private bool reloading;
    public int correctOwnerMagazine;
    private float correctOwnerReload;

    private readonly ShellType shellType;
    protected VehicleController owner;
    private static readonly ObscuredFloat FIRE_RATE_SECONDS = 60.0f;


    public float Progress
    {
        get
        {
            return (float)shotCounter / owner.data.magazine;
        }
    }

    /* public Weapon(VehicleController owner)//weapon without shell
     {
         this.owner = owner;
         this.shellType = ShellType.Empty;
     }*/

    void ForceReload(EventId _id, EventInfo _info)
    {
        if (reloading)
        {
            return;
        }
        if (shotCounter >= correctOwnerMagazine) //полный боезапас
        {
            Debug.LogFormat("shotCounter = {0} correctOwnerMagazine = {1}",shotCounter, correctOwnerMagazine);
            return;
        }

        shotCounter = 0;
        ReloadingProgress = 0;
        reloading = true;
        reloadingStartTime = Time.time;
    }

    public Weapon(VehicleController owner, ShellType shellType)
    {
        Dispatcher.Subscribe(EventId.ForceReload, ForceReload, 1); //Подписываемся на обращение к нам
        Dispatcher.Subscribe(EventId.TankKilled, TankKilledHandler);
        this.owner = owner;
        this.shellType = shellType;
    }

    void TankKilledHandler(EventId _id, EventInfo _info)
    {
        var info = (EventInfo_II)_info;
        if (info.int1 == owner.data.playerId)
        {
            Dispatcher.Unsubscribe(EventId.TankKilled, TankKilledHandler); //Отписываемся если танчик сдох
            Dispatcher.Unsubscribe(EventId.ForceReload, ForceReload);
        }
    }
    public void InstantReload()
    {
        reloading = false;
        ReloadingProgress = 1;
    }


    //=================================================
    public void FillReloadingData(TankData data) 
    {
        if (correctOwnerMagazine == 0 || HelpTools.Approximately(correctOwnerReload, 0))
        {
            //Debug.LogFormat("Update weapon  correctOwnerMagazine {0} correctOwnerReload {1}", correctOwnerMagazine, correctOwnerReload);
        
            correctOwnerReload = data.reloadTime;
            correctOwnerMagazine = data.magazine;
            shotCounter = correctOwnerMagazine;
            getdata = true;
        }

    }

    //=================================================
    bool getdata = false;
    public void UpdateWeapon()//выполняется в Update
    {
        if(!getdata)
        {
            return;
        }

        if (!reloading)
        {
            return;
        }

        timeInReload = Time.time - reloadingStartTime;

        ReloadingProgress = timeInReload / correctOwnerReload;
        if (timeInReload >= correctOwnerReload)
        {
            shotCounter = correctOwnerMagazine;
        }
        if (shotCounter < correctOwnerMagazine)
        {
            return;
        }

        reloading = false;

        if (owner.IsMain)
        {
            Dispatcher.Send(EventId.MainVehWeaponReloaded, new EventInfo_SimpleEvent());
        }
    }

    public void RegisterShot()//вызывается в PrimaryFire и SecondaryFire
    {

        if (reloading)
        {
            return;
        }

        ReloadingProgress = Progress;

        lastShotTime = Time.time;
        --shotCounter;
        ReloadingProgress = Progress;
        if (shotCounter > 0 || !owner.PhotonView.isMine)
        {
            return;
        }
        if (owner.IsBot) 
        {
            Reload();
        }
        /*
        reloading = true;
        reloadingStartTime = Time.time;*/
    }
    
    public void Reload() 
    {
        shotCounter = 0;
        reloading = true;
        reloadingStartTime = Time.time;
    }

    public bool IsReady
    {
        get
        {
            return Time.time - lastShotTime >= ReloadingTimeSeconds && (!owner.PhotonView.isMine || !reloading); // Клоны не перезаряжают сами
        }
    }

    public int Counter
    {
        get
        {
            return shotCounter;
        }
    }

    public float ReloadRemainingSeconds
    {
        get { return ReloadingTimeSeconds * (1 - ReloadingProgress); }
    }

    public ObscuredFloat ReloadingProgress
    {
        get; private set;
    }

    public ObscuredFloat ReloadingTimeSeconds
    {
        get { return FIRE_RATE_SECONDS / owner.GetROF(shellType); }
    }





























}
