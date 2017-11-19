using UnityEngine;

public class MachineGun : SuperWeapon
{
    [Header("Ссылки")]
    public string shellPrefabName;
    public AudioClip[] firstShotSounds;
    public AudioClip[] moreShotSounds;
    public GameObject shotEffect;

    [Header("Прочее")]
    public float shotDelay = 0.25f;

    private const int FIRST_SHOTS_AMOUNT = 3;
    private const GunShellInfo.ShellType SHELL_TYPE = GunShellInfo.ShellType.MachineGun;

    private bool isShooting;
    private int shotCount;
    private float duration;
    private float nextShotTime;
    private Transform shotPoint;

    private bool IsReloaded
    {
        get { return Time.time > nextShotTime; }
    }

    public override void OnPhotonInstantiate(PhotonMessageInfo info)
    {
        base.OnPhotonInstantiate(info);

        ApplyParameters();
        SetPosition();
        StartShooting();

        Dispatcher.Subscribe(EventId.TankKilled, OnTankKilled);
    }

    void OnDestroy()
    {
        Dispatcher.Unsubscribe(EventId.TankKilled, OnTankKilled);

        if (!photonView.isMine)
            Shutdown();
    }

    void Update()
    {
        if (!isShooting)
            return;

        if (IsReloaded)
            Shoot();
    }

    private void OnTankKilled(EventId id, EventInfo ei)
    {
        int victimId = ((EventInfo_III)ei).int1;

        if (victimId == owner.data.playerId && photonView.isMine)
            Shutdown();
    }

    private void ApplyParameters()
    {
        duration = consumableInfo.duration;
    }

    private void SetPosition()
    {
        shotPoint = owner.GetShotPoint(this);

        transform.SetParent(shotPoint);

        transform.localPosition = Vector3.zero;
        transform.localRotation = Quaternion.identity;
        transform.localScale = Vector3.one;
    }

    private void StartShooting()
    {
        isShooting = true;
        SetNextShotTime();
        this.Invoke(Shutdown, duration);
    }

    private void Shoot()
    {
        shotCount++;

        Vector3 shotPosition = transform.position;
        Vector3 shotDirection;
        Quaternion shotRotation;

        AudioClip shotSound = shotCount > FIRST_SHOTS_AMOUNT ? moreShotSounds.GetRandomItem() : firstShotSounds.GetRandomItem();

        int power = CalcPower(); // Мы не знаем, куда врежется пуля, потому не передаём аргумент. Вернёт 0, если context будет target!

        if (owner.IsMain)
        {
            if (owner.TargetAimed)
                shotDirection = (owner.TargetPosition - transform.position).normalized;
            else
                shotDirection = transform.forward;
        }
        else
        {
            shotDirection = GetCloneShotDirection();
        }

        shotRotation = Quaternion.LookRotation(shotDirection);

        Shell shell = ShellPoolManager.GetShell(shellPrefabName, shotPosition, shotRotation);

        shell.OwnerSpeed = Mathf.Abs(owner.CurrentSpeed);

        shell.Activate(
            owner:      owner,
            damage:     power,
            hitMask:    owner.HitMask,
            shellType:  SHELL_TYPE);

        EffectPoolDispatcher.GetFromPool(
            _effect:        shotEffect,
            _position:      transform.position,
            _rotation:      transform.rotation,
            useEffectMover: true,
            moverTarget:    shotPoint);

        AudioDispatcher.PlayClipAtPosition(shotSound, transform.position, SoundControllerBase.SHOT_VOLUME);

        SetNextShotTime();
    }

    private void Shutdown()
    {
        isShooting = false;

        CancelInvoke();

        if (photonView.isMine)
            PhotonNetwork.Destroy(gameObject);
    }

    private void SetNextShotTime()
    {
        nextShotTime = Time.time + shotDelay;
    }

    private Vector3 GetCloneShotDirection()
    {
        if (target == null)
            return transform.forward;

        Vector3 targetDirection = (target.transform.position - transform.position).normalized;

        if (Vector3.Angle(targetDirection, transform.forward) < owner.MaxShootAngle)
            return targetDirection;

        return transform.forward;
    }
}
