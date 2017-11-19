using System.Collections.Generic;
using UnityEngine;

public class GrenadeLauncher : SuperWeapon
{
    [Header("Ссылки")]
    public Grenade grenadePrefab;
    public AudioClip[] shotSounds;
    public GameObject shotEffect;

    [Header("Прочее")]
    public int grenadesAmount = 3;
    public float throwDelay = 0.33f;

    private const GunShellInfo.ShellType SHELL_TYPE = GunShellInfo.ShellType.Grenade_AGS;

    private Vector3 aimPosition;
    private Transform shotPoint;
    private Queue<Grenade> initialGrenades;
    private List<Grenade> throwedGrenades; 

    public VehicleController Owner
    {
        get { return owner; }
    }

    public override void OnPhotonInstantiate(PhotonMessageInfo info)
    {
        base.OnPhotonInstantiate(info);

        SetPosition();
        SetTargetPosition();
        ThrowGrenades();

        Dispatcher.Subscribe(EventId.TankKilled, OnTankKilled);
    }

    void OnDestroy()
    {
        Dispatcher.Unsubscribe(EventId.TankKilled, OnTankKilled);
    }

    public void SendDamage(Collider other, Grenade grenade)
    {
        if (!photonView.isMine)
        {
            DestroyIfDamageSent(grenade);
            return;
        }

        Collider[] vehColliders = Physics.OverlapSphere(grenade.transform.position, consumableInfo.radius, BattleController.CommonVehicleMask);

        HashSet<VehicleController> victims = new HashSet<VehicleController>();

        foreach (Collider vehCollider in vehColliders)
        {
            VehicleController vehicle = vehCollider.GetComponentInParent<VehicleController>();

            if (vehicle == null || !victims.Add(vehicle))
                continue;

            int playerId = vehicle.data.playerId;
            int power = CalcPower(vehicle);

            Dispatcher.Send(
                id:         EventId.TankTakesDamage,
                info:       new EventInfo_U(
                                /* victimId */      playerId,
                                /* damage */        power,
                                /* attackerId */    owner.data.playerId,
                                /* shellType */     SHELL_TYPE,
                                /* hits */          1,
                                /* hitPosition */   Vector3.zero),
                target:     vehicle.IsBot ? Dispatcher.EventTargetType.ToMaster : Dispatcher.EventTargetType.ToSpecific,
                specificId: playerId);
        }

        DestroyIfDamageSent(grenade);
    }

    private void OnTankKilled(EventId id, EventInfo ei)
    {
        int victimId = ((EventInfo_III)ei).int1;

        if (victimId == owner.data.playerId && photonView.isMine)
            PhotonNetwork.Destroy(gameObject);
    }

    private void SetPosition()
    {
        shotPoint = owner.GetShotPoint(this);

        transform.SetParent(shotPoint);

        transform.localPosition = Vector3.zero;
        transform.localRotation = Quaternion.identity;
        transform.localScale = Vector3.one;
    }

    private void SetTargetPosition()
    {
        if (target == null)
        {
            Debug.LogError("Cannot guide grenade launcher. Target is null!");
            aimPosition = transform.position + transform.forward * 75.0f;
            return;
        }

        aimPosition = target.transform.TransformPoint(aimPointLocalToTarget);
    }

    private void ThrowGrenades()
    {
        initialGrenades = new Queue<Grenade>();
        throwedGrenades = new List<Grenade>();

        for (int i = 0; i < grenadesAmount; i++)
        {
            Grenade grenade = Instantiate(grenadePrefab);
            grenade.Init(this, consumableInfo, aimPosition);
            initialGrenades.Enqueue(grenade);
        }

        this.InvokeRepeating(Throwing, 0.0f, throwDelay);
    }

    private void Throwing()
    {
        if (initialGrenades.Count == 0)
        {
            CancelInvoke();
            return;
        }

        Grenade grenade = initialGrenades.Dequeue();

        throwedGrenades.Add(grenade);
        grenade.Throw(transform.position, transform.rotation, consumableInfo.activationTime);

        EffectPoolDispatcher.GetFromPool(
            _effect:        shotEffect,
            _position:      transform.position,
            _rotation:      transform.rotation,
            useEffectMover: true,
            moverTarget:    shotPoint);

        AudioDispatcher.PlayClipAtPosition(shotSounds.GetRandomItem(), transform.position);

        if (owner.IsMain)
            Dispatcher.Send(EventId.MyTankShots, new EventInfo_I((int)SHELL_TYPE));
    }

    private void DestroyIfDamageSent(Grenade grenade)
    {
        throwedGrenades.Remove(grenade);

        if (gameObject != null && photonView.isMine && throwedGrenades.Count == 0 && initialGrenades.Count == 0)
            PhotonNetwork.Destroy(gameObject);
    }
}
